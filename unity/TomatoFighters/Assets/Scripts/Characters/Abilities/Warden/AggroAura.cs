using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Warden
{
    /// <summary>
    /// Warden T2 passive: while 3+ enemies are within range targeting the player,
    /// grants +25% ATK and +15% SPD. Combat pipeline queries bonuses.
    /// </summary>
    public class AggroAura : IPathAbility
    {
        private const string ID = "Warden_AggroAura";
        private const float DETECT_RANGE = 6f;
        private const int ENEMY_THRESHOLD = 3;
        private const float ATK_BONUS = 0.25f;
        private const float SPD_BONUS = 0.15f;

        private readonly PathAbilityContext _ctx;
        private bool _isActive;
        private bool _buffActive;

        public AggroAura(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Passive;
        public float ManaCost => 0f;
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        /// <summary>ATK bonus when buff is active. Combat pipeline queries this.</summary>
        public float AttackBonus => _buffActive ? ATK_BONUS : 0f;

        /// <summary>SPD bonus when buff is active. Motor queries this.</summary>
        public float SpeedBonus => _buffActive ? SPD_BONUS : 0f;

        public bool TryActivate()
        {
            _isActive = true;
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!_isActive || _ctx.PlayerTransform == null) return;

            var hits = Physics2D.OverlapCircleAll(
                _ctx.PlayerTransform.position, DETECT_RANGE, _ctx.EnemyLayer);

            bool wasBuff = _buffActive;
            _buffActive = hits.Length >= ENEMY_THRESHOLD;

            if (_buffActive && !wasBuff)
                Debug.Log($"[AggroAura] Buff active — {hits.Length} enemies nearby (+{ATK_BONUS * 100}% ATK, +{SPD_BONUS * 100}% SPD)");
            else if (!_buffActive && wasBuff)
                Debug.Log("[AggroAura] Buff deactivated — fewer than 3 enemies nearby");
        }

        public void Cleanup()
        {
            _isActive = false;
            _buffActive = false;
        }
    }
}
