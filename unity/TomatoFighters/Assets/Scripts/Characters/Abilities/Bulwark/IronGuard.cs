using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Bulwark
{
    /// <summary>
    /// Bulwark T1 toggle: halves movement speed, grants 50% damage reduction,
    /// and deflect successes heal 5% max HP.
    /// Toggle on/off with ability key. No mana cost — sustained stance.
    /// </summary>
    public class IronGuard : IPathAbility
    {
        private const string ID = "Bulwark_IronGuard";
        private const float SPEED_MULTIPLIER = 0.5f;
        private const float DAMAGE_REDUCTION = 0.5f;

        private readonly PathAbilityContext _ctx;
        private readonly GameObject _vfxPrefab;
        private GameObject _activeVfx;
        private bool _isActive;

        public IronGuard(PathAbilityContext ctx)
        {
            _ctx = ctx;
            _vfxPrefab = ctx.VfxPrefab;
        }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Toggle;
        public float ManaCost => 0f;
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        public bool TryActivate()
        {
            _isActive = true;

            // Sustained aura VFX — blue-orange shield glow, parented to player
            if (_vfxPrefab != null)
                _activeVfx = Object.Instantiate(_vfxPrefab, _ctx.PlayerTransform);

            Debug.Log("[IronGuard] Stance activated — 50% move speed, 50% DR");
            return true;
        }

        public void Tick(float deltaTime)
        {
            // Movement speed reduction is applied by checking IsActive from motor
            // DR is applied by checking IsActive from defense pipeline
        }

        public void Cleanup()
        {
            _isActive = false;
            if (_activeVfx != null)
                Object.Destroy(_activeVfx);
            Debug.Log("[IronGuard] Stance deactivated");
        }

        /// <summary>Speed multiplier while active. Motor queries this.</summary>
        public float GetSpeedMultiplier() => _isActive ? SPEED_MULTIPLIER : 1f;

        /// <summary>Damage reduction while active. Defense pipeline queries this.</summary>
        public float GetDamageReduction() => _isActive ? DAMAGE_REDUCTION : 0f;
    }
}
