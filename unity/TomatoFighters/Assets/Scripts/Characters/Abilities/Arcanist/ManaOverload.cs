using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Arcanist
{
    /// <summary>
    /// Arcanist T3 signature (Main only): instantly fill Mana Charge to 100% AND gain
    /// Overcharged state for 10 seconds. While Overcharged: all abilities deal 2x damage
    /// but cost double mana. Mana Blast can fire at 150% charge (900% ATK, 2.5s stun).
    /// When Overcharged ends: mana drained to 0, no regen for 3 seconds. Cooldown: 50s.
    /// </summary>
    public class ManaOverload : IPathAbility
    {
        private const string ID = "Arcanist_ManaOverload";
        private const float COOLDOWN = 50f;
        private const float OVERCHARGED_DURATION = 10f;
        private const float DAMAGE_MULTIPLIER = 2f;
        private const float MANA_COST_MULTIPLIER = 2f;
        private const float REGEN_LOCKOUT = 3f;

        private readonly PathAbilityContext _ctx;
        private float _cooldownRemaining;
        private float _overchargedTimeRemaining;
        private float _regenLockoutRemaining;
        private bool _isOvercharged;

        public ManaOverload(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Active;
        public float ManaCost => 0f;
        public float Cooldown => COOLDOWN;
        public bool IsActive => _isOvercharged;
        public float CooldownRemaining => _cooldownRemaining;

        /// <summary>Damage multiplier during Overcharged state. Combat pipeline queries this.</summary>
        public float DamageMultiplier => _isOvercharged ? DAMAGE_MULTIPLIER : 1f;

        /// <summary>Mana cost multiplier during Overcharged state.</summary>
        public float ManaCostMultiplier => _isOvercharged ? MANA_COST_MULTIPLIER : 1f;

        /// <summary>Whether mana regen is locked out after Overcharged ends.</summary>
        public bool RegenLocked => _regenLockoutRemaining > 0f;

        public bool TryActivate()
        {
            _isOvercharged = true;
            _overchargedTimeRemaining = OVERCHARGED_DURATION;
            _cooldownRemaining = COOLDOWN;

            Debug.Log($"[ManaOverload] OVERCHARGED! {DAMAGE_MULTIPLIER}x damage, " +
                $"{MANA_COST_MULTIPLIER}x mana cost for {OVERCHARGED_DURATION}s");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= deltaTime;

            if (_regenLockoutRemaining > 0f)
                _regenLockoutRemaining -= deltaTime;

            if (!_isOvercharged) return;

            _overchargedTimeRemaining -= deltaTime;
            if (_overchargedTimeRemaining <= 0f)
            {
                EndOvercharge();
            }
        }

        public void Cleanup()
        {
            _isOvercharged = false;
            _overchargedTimeRemaining = 0f;
            _cooldownRemaining = 0f;
            _regenLockoutRemaining = 0f;
        }

        private void EndOvercharge()
        {
            _isOvercharged = false;

            // Drain all mana
            if (_ctx.ManaTracker != null)
                _ctx.ManaTracker.TryConsume(_ctx.ManaTracker.CurrentMana);

            _regenLockoutRemaining = REGEN_LOCKOUT;

            Debug.Log($"[ManaOverload] Overcharged ended — mana drained, regen locked for {REGEN_LOCKOUT}s");
        }
    }
}
