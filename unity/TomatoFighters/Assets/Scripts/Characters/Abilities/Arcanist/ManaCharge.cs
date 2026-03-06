using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Arcanist
{
    /// <summary>
    /// Arcanist T1 channeled: hold to fill a charge meter (0-100%).
    /// While channeling: stationary + 30% damage reduction.
    /// Release to discharge — damage/effect scales with charge percentage.
    /// Full charge takes ~3 seconds.
    /// </summary>
    public class ManaCharge : IChanneledAbility
    {
        private const string ID = "Arcanist_ManaCharge";
        private const float CHARGE_RATE = 33.33f; // % per second (3s to full)
        private const float DR_WHILE_CHANNELING = 0.3f;

        private readonly PathAbilityContext _ctx;
        private readonly GameObject _vfxPrefab;
        private GameObject _activeVfx;
        private ParticleSystem _vfxParticleSystem;
        private float _vfxBaseEmissionRate;
        private bool _isChanneling;
        private float _chargePercent;

        public ManaCharge(PathAbilityContext ctx)
        {
            _ctx = ctx;
            _vfxPrefab = ctx.VfxPrefab;
        }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Channeled;
        public float ManaCost => 0f; // No upfront cost — generates charge
        public float Cooldown => 0f;
        public bool IsActive => _isChanneling;
        public float CooldownRemaining => 0f;

        /// <summary>Current charge percentage (0-100).</summary>
        public float ChargePercent => _chargePercent;

        public bool TryActivate()
        {
            _isChanneling = true;
            _chargePercent = 0f;

            // Sustained charge VFX — purple energy spiral, parented to player
            if (_vfxPrefab != null)
            {
                _activeVfx = Object.Instantiate(_vfxPrefab, _ctx.PlayerTransform);
                _vfxParticleSystem = _activeVfx.GetComponent<ParticleSystem>();
                if (_vfxParticleSystem != null)
                {
                    var emission = _vfxParticleSystem.emission;
                    _vfxBaseEmissionRate = emission.rateOverTime.constant;
                    // Start at minimal emission — scales with charge
                    emission.rateOverTime = _vfxBaseEmissionRate * 0.1f;
                }
            }

            // Lock movement while channeling
            if (_ctx.Motor != null)
                _ctx.Motor.SetAttackLock(true);

            Debug.Log("[ManaCharge] Channeling started — hold to charge");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!_isChanneling) return;

            _chargePercent = Mathf.Min(_chargePercent + CHARGE_RATE * deltaTime, 100f);

            // Scale VFX emission with charge percentage
            if (_vfxParticleSystem != null)
            {
                var emission = _vfxParticleSystem.emission;
                emission.rateOverTime = _vfxBaseEmissionRate * Mathf.Lerp(0.1f, 1f, _chargePercent / 100f);
            }

            if (_chargePercent >= 100f)
            {
                Debug.Log("[ManaCharge] Fully charged!");
            }
        }

        public void Release()
        {
            if (!_isChanneling) return;

            // Unlock movement
            if (_ctx.Motor != null)
                _ctx.Motor.SetAttackLock(false);

            // Destroy sustained VFX on release
            if (_activeVfx != null)
            {
                Object.Destroy(_activeVfx);
                _vfxParticleSystem = null;
            }

            // Restore mana proportional to charge
            float manaRestored = _ctx.ManaTracker.MaxMana * (_chargePercent / 100f) * 0.5f;
            _ctx.ManaTracker.Restore(manaRestored);

            Debug.Log($"[ManaCharge] Released at {_chargePercent:F0}% — restored {manaRestored:F1} mana");

            _isChanneling = false;
            _chargePercent = 0f;
        }

        public void Cleanup()
        {
            if (_isChanneling && _ctx.Motor != null)
                _ctx.Motor.SetAttackLock(false);

            if (_activeVfx != null)
            {
                Object.Destroy(_activeVfx);
                _vfxParticleSystem = null;
            }

            _isChanneling = false;
            _chargePercent = 0f;
        }

        /// <summary>DR while channeling. Defense pipeline queries this.</summary>
        public float GetDamageReduction() => _isChanneling ? DR_WHILE_CHANNELING : 0f;
    }
}
