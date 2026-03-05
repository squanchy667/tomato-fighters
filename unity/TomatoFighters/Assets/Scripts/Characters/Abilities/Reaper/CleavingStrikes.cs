using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;

namespace TomatoFighters.Characters.Abilities.Reaper
{
    /// <summary>
    /// Reaper T1 passive: attacks hit 2 additional targets at 60% damage.
    /// Implements <see cref="IPathAbilityModifier"/> — HitboxManager queries this.
    /// </summary>
    public class CleavingStrikes : IPathAbility, IPathAbilityModifier
    {
        private const string ID = "Reaper_CleavingStrikes";
        private const int ADDITIONAL_TARGETS = 2;
        private const float ADDITIONAL_TARGET_SCALE = 0.6f;

        private bool _isActive;

        public CleavingStrikes(PathAbilityContext ctx) { }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Passive;
        public float ManaCost => 0f;
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        public bool TryActivate()
        {
            _isActive = true;
            return true;
        }

        public void Tick(float deltaTime) { }
        public void Cleanup() { _isActive = false; }

        // IPathAbilityModifier
        public int GetAdditionalTargetCount() => _isActive ? ADDITIONAL_TARGETS : 0;
        public float GetAdditionalTargetDamageScale() => _isActive ? ADDITIONAL_TARGET_SCALE : 1f;
        public bool DoProjectilesPierce() => false;
        public float GetPierceDamageFalloff() => 1f;
    }
}
