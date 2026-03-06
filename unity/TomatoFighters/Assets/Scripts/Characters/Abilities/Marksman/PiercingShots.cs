using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Marksman
{
    /// <summary>
    /// Marksman T1 passive: projectiles pierce through targets with 20% damage falloff per target.
    /// Implements <see cref="IPathAbilityModifier"/> — projectile systems query this.
    /// </summary>
    public class PiercingShots : IPathAbility, IPathAbilityModifier
    {
        private const string ID = "Marksman_PiercingShots";
        private const float PIERCE_FALLOFF = 0.8f;

        private readonly PathAbilityContext _ctx;
        private readonly GameObject _vfxPrefab;
        private GameObject _activeVfx;
        private bool _isActive;

        public PiercingShots(PathAbilityContext ctx)
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

            // Sustained passive indicator VFX — yellow arrow trail, parented to player
            if (_vfxPrefab != null)
                _activeVfx = Object.Instantiate(_vfxPrefab, _ctx.PlayerTransform);

            return true;
        }

        public void Tick(float deltaTime) { }

        public void Cleanup()
        {
            _isActive = false;
            if (_activeVfx != null)
                Object.Destroy(_activeVfx);
        }

        // IPathAbilityModifier
        public int GetAdditionalTargetCount() => 0;
        public float GetAdditionalTargetDamageScale() => 1f;
        public bool DoProjectilesPierce() => _isActive;
        public float GetPierceDamageFalloff() => _isActive ? PIERCE_FALLOFF : 1f;
    }
}
