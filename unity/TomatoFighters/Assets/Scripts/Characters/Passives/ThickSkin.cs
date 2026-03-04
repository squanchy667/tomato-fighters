using TomatoFighters.Shared.Data;

namespace TomatoFighters.Characters.Passives
{
    /// <summary>
    /// Brutor passive — "Thick Skin".
    /// Always-on flat damage reduction and knockback reduction.
    /// No state management — constant values from config.
    /// </summary>
    public class ThickSkin : IPassiveAbility
    {
        private readonly float _defenseMultiplier;
        private readonly float _knockbackMultiplier;

        public ThickSkin(PassiveConfig config)
        {
            _defenseMultiplier = 1f - config.thickSkinDamageReduction;
            _knockbackMultiplier = 1f - config.thickSkinKnockbackReduction;
        }

        public float GetDamageMultiplier(HitContext context) => 1f;
        public float GetDefenseMultiplier() => _defenseMultiplier;
        public float GetKnockbackMultiplier() => _knockbackMultiplier;
        public float GetSpeedMultiplier() => 1f;
        public void Tick(float deltaTime) { }
        public void OnHitLanded() { }
        public void OnAttackPerformed() { }
    }
}
