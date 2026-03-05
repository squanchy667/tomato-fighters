using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Sage
{
    /// <summary>
    /// Sage T3 signature (Main only): channel for 3 seconds to revive a downed ally at 50% HP.
    /// Once per ally per run. Mystica gains 50% DR during channel. If interrupted, goes on
    /// half cooldown (30s). Cooldown: 60s. Solo: stub (no allies to revive).
    /// </summary>
    public class Resurrection : IChanneledAbility
    {
        private const string ID = "Sage_Resurrection";
        private const float CHANNEL_DURATION = 3f;
        private const float COOLDOWN = 60f;
        private const float HALF_COOLDOWN = 30f;
        private const float DR_WHILE_CHANNELING = 0.5f;

        private readonly PathAbilityContext _ctx;
        private float _cooldownRemaining;
        private float _channelTimeRemaining;
        private bool _isChanneling;

        public Resurrection(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Channeled;
        public float ManaCost => 0f;
        public float Cooldown => COOLDOWN;
        public bool IsActive => _isChanneling;
        public float CooldownRemaining => _cooldownRemaining;

        /// <summary>DR during channel. Defense pipeline queries this.</summary>
        public float GetDamageReduction() => _isChanneling ? DR_WHILE_CHANNELING : 0f;

        /// <summary>
        /// Called by damage pipeline during channel — interrupts resurrection.
        /// </summary>
        public void OnDamageTaken()
        {
            if (!_isChanneling) return;
            CancelChannel();
            _cooldownRemaining = HALF_COOLDOWN;
            Debug.Log("[Resurrection] Interrupted! Half cooldown.");
        }

        public bool TryActivate()
        {
            _isChanneling = true;
            _channelTimeRemaining = CHANNEL_DURATION;

            if (_ctx.Motor != null)
                _ctx.Motor.SetAttackLock(true);

            Debug.Log($"[Resurrection] Channeling... {CHANNEL_DURATION}s (solo: stub — no allies to revive)");
            return true;
        }

        public void Release()
        {
            // Early release cancels the channel
            if (_isChanneling)
            {
                CancelChannel();
                _cooldownRemaining = HALF_COOLDOWN;
                Debug.Log("[Resurrection] Released early — half cooldown");
            }
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= deltaTime;

            if (!_isChanneling) return;

            _channelTimeRemaining -= deltaTime;
            if (_channelTimeRemaining <= 0f)
            {
                CompleteResurrection();
            }
        }

        public void Cleanup()
        {
            if (_isChanneling)
                CancelChannel();
            _cooldownRemaining = 0f;
        }

        private void CompleteResurrection()
        {
            _isChanneling = false;
            _cooldownRemaining = COOLDOWN;

            if (_ctx.Motor != null)
                _ctx.Motor.SetAttackLock(false);

            // Solo: no allies to revive — stub for co-op
            Debug.Log("[Resurrection] Channel complete (solo: no effect — awaiting co-op T051)");
        }

        private void CancelChannel()
        {
            _isChanneling = false;
            _channelTimeRemaining = 0f;

            if (_ctx.Motor != null)
                _ctx.Motor.SetAttackLock(false);
        }
    }
}
