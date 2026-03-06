using System;
using System.Collections.Generic;
using TomatoFighters.Characters.Abilities;
using TomatoFighters.Combat;
using TomatoFighters.Shared.Components;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters
{
    /// <summary>
    /// Lives on the player prefab. Creates path abilities when paths are selected,
    /// routes Q/E input to main/secondary T1 abilities, R input to T3 signature,
    /// manages cooldowns, ticks active abilities, and fires
    /// <see cref="PathAbilityEventData"/> through combat events.
    /// T2 passives auto-activate on tier-up. T3 signatures are Main-path only.
    /// </summary>
    public class PathAbilityExecutor : MonoBehaviour, IPathAbilityModifier
    {
        [Header("References")]
        [SerializeField] private CharacterMotor motor;
        [SerializeField] private ComboController comboController;
        [SerializeField] private HitboxManager hitboxManager;
        [SerializeField] private PlayerManaTracker manaTracker;
        [SerializeField] private PlayerDamageable playerDamageable;

        [Header("Path Provider")]
        [Tooltip("Component implementing IPathProvider (e.g. PathSystem).")]
        [SerializeField] private Component pathProviderComponent;

        [Header("VFX")]
        [Tooltip("Maps abilityId → VFX prefab. Created by AbilityVfxCreator.")]
        [SerializeField] private AbilityVfxLookup vfxLookup;

        [Header("Targeting")]
        [SerializeField] private LayerMask enemyLayer;

        private IPathProvider _pathProvider;
        private PathAbilityContext _context;

        // T1 abilities — activated via Q (main) / E (secondary)
        private IPathAbility _mainAbility;
        private IPathAbility _secondaryAbility;

        // T2 abilities — passives that auto-activate on tier 2
        private IPathAbility _mainT2Ability;
        private IPathAbility _secondaryT2Ability;

        // T3 signature — Main path only, activated via R key
        private IPathAbility _mainT3Ability;

        private PathData _mainPath;
        private PathData _secondaryPath;

        // All active abilities (T1 + T2 + T3 + passives) for ticking
        private readonly List<IPathAbility> _activeAbilities = new();

        // Passive modifier aggregation
        private readonly List<IPathAbilityModifier> _modifiers = new();

        /// <summary>Fired when a path ability is used. Wire to ICombatEvents relay.</summary>
        public event Action<PathAbilityEventData> AbilityUsed;

        private void Awake()
        {
            _pathProvider = pathProviderComponent as IPathProvider;

            _context = new PathAbilityContext
            {
                Motor = motor,
                ComboController = comboController,
                HitboxManager = hitboxManager,
                ManaTracker = manaTracker,
                PathProvider = _pathProvider,
                PlayerTransform = transform,
                PlayerDamageable = playerDamageable,
                EnemyLayer = enemyLayer
            };
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _activeAbilities.Count; i++)
            {
                _activeAbilities[i].Tick(dt);
            }
        }

        // ── Input Routing (called by CharacterInputHandler) ──────────────

        /// <summary>Activate the main path T1 ability (Q key).</summary>
        public void ActivateMainAbility()
        {
            TryActivateAbility(_mainAbility, _mainPath);
        }

        /// <summary>Activate the secondary path T1 ability (E key).</summary>
        public void ActivateSecondaryAbility()
        {
            TryActivateAbility(_secondaryAbility, _secondaryPath);
        }

        /// <summary>Activate the T3 signature ability (R key). Main path only.</summary>
        public void ActivateMainSignature()
        {
            if (_mainT3Ability == null) return;
            TryActivateAbility(_mainT3Ability, _mainPath);
        }

        /// <summary>Release input for channeled abilities (Q key released).</summary>
        public void ReleaseMainAbility()
        {
            if (_mainAbility is IChanneledAbility channeled)
                channeled.Release();
        }

        /// <summary>Release input for channeled abilities (E key released).</summary>
        public void ReleaseSecondaryAbility()
        {
            if (_secondaryAbility is IChanneledAbility channeled)
                channeled.Release();
        }

        /// <summary>Release input for channeled T3 abilities (R key released).</summary>
        public void ReleaseMainSignature()
        {
            if (_mainT3Ability is IChanneledAbility channeled)
                channeled.Release();
        }

        private void TryActivateAbility(IPathAbility ability, PathData path)
        {
            if (ability == null) return;

            // Cooldown check
            if (ability.CooldownRemaining > 0f)
            {
                Debug.Log($"[PathAbilityExecutor] {ability.AbilityId} on cooldown ({ability.CooldownRemaining:F1}s)");
                return;
            }

            // Toggle off if already active
            if (ability.ActivationType == AbilityActivationType.Toggle && ability.IsActive)
            {
                ability.Cleanup();
                _activeAbilities.Remove(ability);
                return;
            }

            // Mana check (per-activation cost)
            if (ability.ManaCost > 0f && !manaTracker.TryConsume(ability.ManaCost))
            {
                Debug.Log($"[PathAbilityExecutor] Not enough mana for {ability.AbilityId} " +
                    $"(need {ability.ManaCost}, have {manaTracker.CurrentMana:F1})");
                return;
            }

            if (!ability.TryActivate()) return;

            // Track active abilities for ticking
            if (!_activeAbilities.Contains(ability))
                _activeAbilities.Add(ability);

            // Fire event
            if (path != null)
            {
                int tier = path == _mainPath
                    ? (_pathProvider?.MainPathTier ?? 1)
                    : (_pathProvider?.SecondaryPathTier ?? 1);

                AbilityUsed?.Invoke(new PathAbilityEventData(
                    _pathProvider?.Character ?? CharacterType.Brutor,
                    ability.AbilityId,
                    path.pathType,
                    tier,
                    ability.ManaCost));
            }
        }

        // ── Path Setup ──────────────────────────────────────────────────

        /// <summary>
        /// Set up T1 abilities for the given paths. Called when paths are selected during a run.
        /// </summary>
        public void SetPaths(PathData mainPath, PathData secondaryPath)
        {
            CleanupAll();

            _mainPath = mainPath;
            _secondaryPath = secondaryPath;

            if (mainPath != null)
            {
                _mainAbility = CreateAbilityWithVfx(mainPath.tier1AbilityId);
                RegisterModifier(_mainAbility);
            }

            if (secondaryPath != null)
            {
                _secondaryAbility = CreateAbilityWithVfx(secondaryPath.tier1AbilityId);
                RegisterModifier(_secondaryAbility);
            }

            // Auto-activate passives
            TryAutoActivatePassive(_mainAbility);
            TryAutoActivatePassive(_secondaryAbility);

            Debug.Log($"[PathAbilityExecutor] Paths set — Main: {mainPath?.pathType}, " +
                $"Secondary: {secondaryPath?.pathType}");
        }

        /// <summary>
        /// Called when a path reaches a new tier (boss defeat). Creates and activates the tier's ability.
        /// T2 passives auto-activate. T3 signatures wait for manual activation (R key).
        /// </summary>
        /// <param name="isMain">True for main path, false for secondary.</param>
        /// <param name="newTier">The new tier (2 or 3). T3 is main-path only.</param>
        public void OnPathTierUp(bool isMain, int newTier)
        {
            var path = isMain ? _mainPath : _secondaryPath;
            if (path == null) return;

            // T3 only for main path
            if (newTier == 3 && !isMain) return;

            string abilityId = path.GetAbilityIdForTier(newTier);
            if (string.IsNullOrEmpty(abilityId)) return;

            var ability = CreateAbilityWithVfx(abilityId);
            if (ability == null) return;

            // Store in the appropriate slot
            if (isMain)
            {
                if (newTier == 2) { _mainT2Ability?.Cleanup(); _mainT2Ability = ability; }
                else if (newTier == 3) { _mainT3Ability?.Cleanup(); _mainT3Ability = ability; }
            }
            else
            {
                if (newTier == 2) { _secondaryT2Ability?.Cleanup(); _secondaryT2Ability = ability; }
            }

            RegisterModifier(ability);
            TryAutoActivatePassive(ability);

            Debug.Log($"[PathAbilityExecutor] Tier {newTier} ability unlocked: {abilityId}");
        }

        /// <summary>
        /// Force-set abilities by ID for debug/testing without a PathData reference.
        /// </summary>
        public void DebugSetAbility(string abilityId, bool isMain)
        {
            var ability = CreateAbilityWithVfx(abilityId);
            if (ability == null) return;

            if (isMain)
            {
                _mainAbility?.Cleanup();
                _mainAbility = ability;
            }
            else
            {
                _secondaryAbility?.Cleanup();
                _secondaryAbility = ability;
            }

            RegisterModifier(ability);
            TryAutoActivatePassive(ability);
        }

        /// <summary>
        /// Sets the context VfxPrefab from the lookup before creating the ability,
        /// so the ability constructor can capture its own VFX reference.
        /// </summary>
        private IPathAbility CreateAbilityWithVfx(string abilityId)
        {
            _context.VfxPrefab = vfxLookup != null ? vfxLookup.GetVfxPrefab(abilityId) : null;
            return AbilityFactory.Create(abilityId, _context);
        }

        private void TryAutoActivatePassive(IPathAbility ability)
        {
            if (ability == null) return;
            if (ability.ActivationType != AbilityActivationType.Passive) return;

            ability.TryActivate();
            if (!_activeAbilities.Contains(ability))
                _activeAbilities.Add(ability);
        }

        private void RegisterModifier(IPathAbility ability)
        {
            if (ability is IPathAbilityModifier modifier)
                _modifiers.Add(modifier);
        }

        private void CleanupAll()
        {
            _mainAbility?.Cleanup();
            _secondaryAbility?.Cleanup();
            _mainT2Ability?.Cleanup();
            _secondaryT2Ability?.Cleanup();
            _mainT3Ability?.Cleanup();

            _mainAbility = null;
            _secondaryAbility = null;
            _mainT2Ability = null;
            _secondaryT2Ability = null;
            _mainT3Ability = null;

            _activeAbilities.Clear();
            _modifiers.Clear();
            _mainPath = null;
            _secondaryPath = null;
        }

        private void OnDestroy()
        {
            CleanupAll();
        }

        // ── IPathAbilityModifier (aggregate) ─────────────────────────────

        /// <inheritdoc/>
        public int GetAdditionalTargetCount()
        {
            int count = 0;
            for (int i = 0; i < _modifiers.Count; i++)
                count += _modifiers[i].GetAdditionalTargetCount();
            return count;
        }

        /// <inheritdoc/>
        public float GetAdditionalTargetDamageScale()
        {
            for (int i = 0; i < _modifiers.Count; i++)
            {
                float scale = _modifiers[i].GetAdditionalTargetDamageScale();
                if (scale < 1f) return scale;
            }
            return 1f;
        }

        /// <inheritdoc/>
        public bool DoProjectilesPierce()
        {
            for (int i = 0; i < _modifiers.Count; i++)
            {
                if (_modifiers[i].DoProjectilesPierce()) return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public float GetPierceDamageFalloff()
        {
            for (int i = 0; i < _modifiers.Count; i++)
            {
                float falloff = _modifiers[i].GetPierceDamageFalloff();
                if (falloff < 1f) return falloff;
            }
            return 1f;
        }
    }
}
