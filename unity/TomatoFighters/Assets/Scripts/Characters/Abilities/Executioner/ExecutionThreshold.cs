using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;

namespace TomatoFighters.Characters.Abilities.Executioner
{
    /// <summary>
    /// Executioner T2 passive: enemies below 30% HP take 50% more damage from Slasher.
    /// Crits against low-HP enemies deal 2.5x instead of 1.5x.
    /// Combat pipeline queries <see cref="GetBonusDamageMultiplier"/> during damage calculation.
    /// </summary>
    public class ExecutionThreshold : IPathAbility
    {
        private const string ID = "Executioner_ExecutionThreshold";
        private const float HP_THRESHOLD = 0.3f;
        private const float BONUS_DAMAGE_MULT = 1.5f; // +50% = 1.5x
        private const float ENHANCED_CRIT_MULT = 2.5f;

        private bool _isActive;

        public ExecutionThreshold(PathAbilityContext ctx) { }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Passive;
        public float ManaCost => 0f;
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        /// <summary>HP threshold below which enemies take bonus damage (0.3 = 30%).</summary>
        public float HpThreshold => HP_THRESHOLD;

        /// <summary>
        /// Returns the damage multiplier to apply against a target.
        /// Call with the target's health ratio (current/max).
        /// </summary>
        public float GetBonusDamageMultiplier(float targetHealthRatio)
        {
            if (!_isActive) return 1f;
            return targetHealthRatio <= HP_THRESHOLD ? BONUS_DAMAGE_MULT : 1f;
        }

        /// <summary>
        /// Returns the crit multiplier for low-HP targets. 2.5x instead of default 1.5x.
        /// </summary>
        public float GetCritMultiplier(float targetHealthRatio)
        {
            if (!_isActive) return 1.5f;
            return targetHealthRatio <= HP_THRESHOLD ? ENHANCED_CRIT_MULT : 1.5f;
        }

        public bool TryActivate()
        {
            _isActive = true;
            return true;
        }

        public void Tick(float deltaTime) { }
        public void Cleanup() { _isActive = false; }
    }
}
