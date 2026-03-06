using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

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

        private readonly PathAbilityContext _ctx;
        private readonly GameObject _vfxPrefab;
        private bool _isActive;

        public CleavingStrikes(PathAbilityContext ctx)
        {
            _ctx = ctx;
            _vfxPrefab = ctx.VfxPrefab;
        }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Passive;
        public float ManaCost => 0f;
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        public bool TryActivate()
        {
            _isActive = true;

            // On-hit burst VFX — red arc/slash trail
            if (_vfxPrefab != null)
                Object.Destroy(
                    Object.Instantiate(_vfxPrefab, _ctx.PlayerTransform.position, Quaternion.identity),
                    0.3f);

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
