using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Enchanter
{
    /// <summary>
    /// Enchanter T1 active: self-buff +30% ATK and +20% SPD for 8 seconds.
    /// Self-target fallback (DD-6). Costs 25 mana. Cooldown: 10s.
    /// </summary>
    public class Empower : IPathAbility
    {
        private const string ID = "Enchanter_Empower";
        private const float BUFF_DURATION = 8f;
        private const float ATK_BONUS = 0.3f;
        private const float SPD_BONUS = 0.2f;
        private const float MANA_COST = 25f;
        private const float COOLDOWN = 10f;

        private readonly PathAbilityContext _ctx;
        private readonly GameObject _vfxPrefab;
        private GameObject _activeVfx;
        private float _cooldownRemaining;
        private float _buffTimeRemaining;
        private bool _isActive;

        public Empower(PathAbilityContext ctx)
        {
            _ctx = ctx;
            _vfxPrefab = ctx.VfxPrefab;
        }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Active;
        public float ManaCost => MANA_COST;
        public float Cooldown => COOLDOWN;
        public bool IsActive => _isActive;
        public float CooldownRemaining => _cooldownRemaining;

        /// <summary>Current ATK bonus while buff is active.</summary>
        public float AttackBonus => _isActive ? ATK_BONUS : 0f;

        /// <summary>Current SPD bonus while buff is active.</summary>
        public float SpeedBonus => _isActive ? SPD_BONUS : 0f;

        public bool TryActivate()
        {
            _isActive = true;
            _buffTimeRemaining = BUFF_DURATION;
            _cooldownRemaining = COOLDOWN;

            // Sustained aura VFX — cyan/teal buff glow, parented to player
            if (_vfxPrefab != null)
                _activeVfx = Object.Instantiate(_vfxPrefab, _ctx.PlayerTransform);

            Debug.Log($"[Empower] Self-buff active — +{ATK_BONUS * 100}% ATK, +{SPD_BONUS * 100}% SPD for {BUFF_DURATION}s");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= deltaTime;

            if (_isActive)
            {
                _buffTimeRemaining -= deltaTime;
                if (_buffTimeRemaining <= 0f)
                {
                    _isActive = false;
                    if (_activeVfx != null)
                        Object.Destroy(_activeVfx);
                    Debug.Log("[Empower] Buff expired");
                }
            }
        }

        public void Cleanup()
        {
            _isActive = false;
            _buffTimeRemaining = 0f;
            _cooldownRemaining = 0f;
            if (_activeVfx != null)
                Object.Destroy(_activeVfx);
        }
    }
}
