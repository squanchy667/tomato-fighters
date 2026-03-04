using TomatoFighters.Characters.Passives;
using TomatoFighters.Combat;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters
{
    /// <summary>
    /// Holds the active passive ability for this character, subscribes to combat events,
    /// and implements <see cref="IPassiveProvider"/> for the damage pipeline to query.
    /// One per player prefab — character type determines which passive is instantiated.
    /// </summary>
    public class PassiveAbilitySystem : MonoBehaviour, IPassiveProvider
    {
        [Header("Configuration")]
        [SerializeField] private PassiveConfig passiveConfig;
        [SerializeField] private CharacterType characterType;

        [Header("References")]
        [SerializeField] private HitboxManager hitboxManager;
        [SerializeField] private ComboController comboController;

        private IPassiveAbility _passive;

        /// <summary>The active passive ability instance. Null if not initialized.</summary>
        public IPassiveAbility ActivePassive => _passive;

        private void Awake()
        {
            if (passiveConfig == null)
            {
                Debug.LogWarning($"[PassiveAbilitySystem] No PassiveConfig assigned on {gameObject.name}.", this);
                return;
            }

            _passive = CreatePassive(characterType, passiveConfig);
        }

        private void OnEnable()
        {
            if (hitboxManager != null)
                hitboxManager.OnHitProcessed += HandleHitProcessed;

            if (comboController != null)
                comboController.AttackStarted += HandleAttackStarted;
        }

        private void OnDisable()
        {
            if (hitboxManager != null)
                hitboxManager.OnHitProcessed -= HandleHitProcessed;

            if (comboController != null)
                comboController.AttackStarted -= HandleAttackStarted;
        }

        private void Update()
        {
            _passive?.Tick(Time.deltaTime);
        }

        // ── IPassiveProvider ─────────────────────────────────────────────

        public float GetDamageMultiplier(HitContext context)
        {
            return _passive?.GetDamageMultiplier(context) ?? 1f;
        }

        public float GetDefenseMultiplier()
        {
            return _passive?.GetDefenseMultiplier() ?? 1f;
        }

        public float GetKnockbackMultiplier()
        {
            return _passive?.GetKnockbackMultiplier() ?? 1f;
        }

        public float GetSpeedMultiplier()
        {
            return _passive?.GetSpeedMultiplier() ?? 1f;
        }

        public void Tick(float deltaTime)
        {
            _passive?.Tick(deltaTime);
        }

        // ── Event Handlers ───────────────────────────────────────────────

        private void HandleHitProcessed(HitDetectionData data)
        {
            _passive?.OnHitLanded();
        }

        private void HandleAttackStarted(AttackType attackType, int stepIndex)
        {
            _passive?.OnAttackPerformed();
        }

        // ── Factory ──────────────────────────────────────────────────────

        /// <summary>
        /// Creates the appropriate passive ability for the given character type.
        /// </summary>
        public static IPassiveAbility CreatePassive(CharacterType type, PassiveConfig config)
        {
            switch (type)
            {
                case CharacterType.Brutor:
                    return new ThickSkin(config);
                case CharacterType.Slasher:
                    return new Bloodlust(config);
                case CharacterType.Mystica:
                    return new ArcaneResonance(config);
                case CharacterType.Viper:
                    return new DistanceBonus(config);
                default:
                    Debug.LogWarning($"[PassiveAbilitySystem] Unknown CharacterType: {type}");
                    return null;
            }
        }
    }
}
