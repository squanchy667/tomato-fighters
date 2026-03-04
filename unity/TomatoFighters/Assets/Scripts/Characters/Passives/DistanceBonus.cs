using UnityEngine;
using TomatoFighters.Shared.Data;

namespace TomatoFighters.Characters.Passives
{
    /// <summary>
    /// Viper passive — "Distance Bonus".
    /// +2% damage per unit distance to target at hit time, max +30%.
    /// Distance is read from <see cref="HitContext.distanceToTarget"/> — passive
    /// does not access transforms directly.
    /// </summary>
    public class DistanceBonus : IPassiveAbility
    {
        private readonly float _bonusPerUnit;
        private readonly float _maxBonus;

        public DistanceBonus(PassiveConfig config)
        {
            _bonusPerUnit = config.distanceBonusPerUnit;
            _maxBonus = config.distanceBonusMaxPercent;
        }

        public float GetDamageMultiplier(HitContext context)
        {
            float bonus = Mathf.Min(_bonusPerUnit * context.distanceToTarget, _maxBonus);
            return 1f + bonus;
        }

        public float GetDefenseMultiplier() => 1f;
        public float GetKnockbackMultiplier() => 1f;
        public float GetSpeedMultiplier() => 1f;
        public void Tick(float deltaTime) { }
        public void OnHitLanded() { }
        public void OnAttackPerformed() { }
    }
}
