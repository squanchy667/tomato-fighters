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
    /// routes Q/E input to main/secondary abilities, manages cooldowns, ticks active
    /// abilities, and fires <see cref="PathAbilityEventData"/> through combat events.
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

        [Header("Targeting")]
        [SerializeField] private LayerMask enemyLayer;

        private IPathProvider _pathProvider;
        private PathAbilityContext _context;

        private IPathAbility _mainAbility;
        private IPathAbility _secondaryAbility;
        private PathData _mainPath;
        private PathData _secondaryPath;

        // All active abilities (main + secondary + any passives) for ticking
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
            // Tick all active abilities (toggles, passives, channels)
            float dt = Time.deltaTime;
            for (int i = 0; i < _activeAbilities.Count; i++)
            {
                _activeAbilities[i].Tick(dt);
            }

            // Tick cooldowns for main/secondary
            TickCooldown(_mainAbility);
            TickCooldown(_secondaryAbility);
        }

        private void TickCooldown(IPathAbility ability)
        {
            // Cooldown is managed internally by each ability via Tick
        }

        // ── Input Routing (called by CharacterInputHandler) ──────────────

        /// <summary>Activate the main path ability (Q key).</summary>
        public void ActivateMainAbility()
        {
            TryActivateAbility(_mainAbility, _mainPath);
        }

        /// <summary>Activate the secondary path ability (E key).</summary>
        public void ActivateSecondaryAbility()
        {
            TryActivateAbility(_secondaryAbility, _secondaryPath);
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
        /// Set up abilities for the given paths. Called when paths are selected during a run.
        /// </summary>
        public void SetPaths(PathData mainPath, PathData secondaryPath)
        {
            CleanupAll();

            _mainPath = mainPath;
            _secondaryPath = secondaryPath;

            if (mainPath != null)
            {
                _mainAbility = AbilityFactory.Create(mainPath.tier1AbilityId, _context);
                RegisterModifier(_mainAbility);
            }

            if (secondaryPath != null)
            {
                _secondaryAbility = AbilityFactory.Create(secondaryPath.tier1AbilityId, _context);
                RegisterModifier(_secondaryAbility);
            }

            // Auto-activate passives
            ActivatePassives();

            Debug.Log($"[PathAbilityExecutor] Paths set — Main: {mainPath?.pathType}, " +
                $"Secondary: {secondaryPath?.pathType}");
        }

        /// <summary>
        /// Force-set abilities by ID for debug/testing without a PathData reference.
        /// </summary>
        public void DebugSetAbility(string abilityId, bool isMain)
        {
            var ability = AbilityFactory.Create(abilityId, _context);
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

            if (ability.ActivationType == AbilityActivationType.Passive)
            {
                ability.TryActivate();
                if (!_activeAbilities.Contains(ability))
                    _activeAbilities.Add(ability);
            }
        }

        private void ActivatePassives()
        {
            TryAutoActivatePassive(_mainAbility);
            TryAutoActivatePassive(_secondaryAbility);
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
            _mainAbility = null;
            _secondaryAbility = null;
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
            // Use the first modifier that provides a non-1.0 scale
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
