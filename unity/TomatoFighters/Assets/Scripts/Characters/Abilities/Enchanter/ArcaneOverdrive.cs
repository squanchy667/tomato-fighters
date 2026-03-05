using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Enchanter
{
    /// <summary>
    /// Enchanter T3 signature (Main only): for 8 seconds, ALL active buffs on ALL allies
    /// are doubled in effect. Cooldowns tick 50% faster. Drains 10 MNA/s.
    /// Cooldown: 50s.
    /// </summary>
    public class ArcaneOverdrive : IPathAbility
    {
        private const string ID = "Enchanter_ArcaneOverdrive";
        private const float DURATION = 8f;
        private const float COOLDOWN = 50f;
        private const float MANA_DRAIN_PER_SEC = 10f;
        private const float BUFF_MULTIPLIER = 2f;
        private const float COOLDOWN_SPEED_MULT = 1.5f;

        private readonly PathAbilityContext _ctx;
        private float _cooldownRemaining;
        private float _timeRemaining;
        private bool _isOverdriveActive;

        public ArcaneOverdrive(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Active;
        public float ManaCost => 0f; // Uses drain instead of upfront cost
        public float Cooldown => COOLDOWN;
        public bool IsActive => _isOverdriveActive;
        public float CooldownRemaining => _cooldownRemaining;

        /// <summary>Buff multiplier while Overdrive is active. Buff pipeline queries this.</summary>
        public float BuffMultiplier => _isOverdriveActive ? BUFF_MULTIPLIER : 1f;

        /// <summary>Cooldown speed multiplier. Ability tick pipeline queries this.</summary>
        public float CooldownSpeedMultiplier => _isOverdriveActive ? COOLDOWN_SPEED_MULT : 1f;

        public bool TryActivate()
        {
            _isOverdriveActive = true;
            _timeRemaining = DURATION;
            _cooldownRemaining = COOLDOWN;

            Debug.Log($"[ArcaneOverdrive] ACTIVATED — {BUFF_MULTIPLIER}x buffs, " +
                $"{COOLDOWN_SPEED_MULT}x CD speed for {DURATION}s");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= deltaTime;

            if (!_isOverdriveActive) return;

            // Drain mana
            float manaDrain = MANA_DRAIN_PER_SEC * deltaTime;
            if (_ctx.ManaTracker != null && !_ctx.ManaTracker.TryConsume(manaDrain))
            {
                _isOverdriveActive = false;
                Debug.Log("[ArcaneOverdrive] Out of mana — ended early");
                return;
            }

            _timeRemaining -= deltaTime;
            if (_timeRemaining <= 0f)
            {
                _isOverdriveActive = false;
                Debug.Log("[ArcaneOverdrive] Overdrive expired");
            }
        }

        public void Cleanup()
        {
            _isOverdriveActive = false;
            _timeRemaining = 0f;
            _cooldownRemaining = 0f;
        }
    }
}
