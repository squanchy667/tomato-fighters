using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Guardian
{
    /// <summary>
    /// Guardian T2 passive aura: allies near Brutor gain +10 DEF and regen 2% max HP/s.
    /// Brutor heals self for 1% max HP/s (solo fallback). Co-op: applies to nearby allies.
    /// </summary>
    public class RallyingPresence : IPathAbility
    {
        private const string ID = "Guardian_RallyingPresence";
        private const float SELF_HEAL_RATE = 0.01f; // 1% max HP/s
        private const float DEF_BONUS = 10f;

        private readonly PathAbilityContext _ctx;
        private bool _isActive;

        public RallyingPresence(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Passive;
        public float ManaCost => 0f;
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        /// <summary>DEF bonus for allies in aura range. Combat pipeline queries this.</summary>
        public float DefenseBonus => _isActive ? DEF_BONUS : 0f;

        public bool TryActivate()
        {
            _isActive = true;
            Debug.Log($"[RallyingPresence] Aura active — +{DEF_BONUS} DEF to nearby allies, self-heal {SELF_HEAL_RATE * 100}%/s");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!_isActive) return;

            // Solo: heal self
            if (_ctx.PlayerDamageable != null)
            {
                float healAmount = _ctx.PlayerDamageable.MaxHealth * SELF_HEAL_RATE * deltaTime;
                _ctx.PlayerDamageable.Heal(healAmount);
            }
        }

        public void Cleanup()
        {
            _isActive = false;
        }
    }
}
