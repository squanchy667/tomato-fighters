using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Sage
{
    /// <summary>
    /// Sage T2 passive: MendingAura also cleanses one negative status effect per tick
    /// from each ally in range. Solo: cleanses one status per tick from self.
    /// Requires MendingAura (T1) to be active.
    /// </summary>
    public class PurifyingBurst : IPathAbility
    {
        private const string ID = "Sage_PurifyingPresence";
        private const float CLEANSE_INTERVAL = 1f;

        private readonly PathAbilityContext _ctx;
        private bool _isActive;
        private float _timer;

        public PurifyingBurst(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Passive;
        public float ManaCost => 0f;
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        public bool TryActivate()
        {
            _isActive = true;
            Debug.Log("[PurifyingBurst] Purifying presence active — cleanses 1 status/s");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!_isActive) return;

            _timer += deltaTime;
            if (_timer >= CLEANSE_INTERVAL)
            {
                _timer -= CLEANSE_INTERVAL;
                // Solo: cleanse self (stub — player status effects not yet implemented)
                Debug.Log("[PurifyingBurst] Cleanse tick (stub — player status tracking pending)");
            }
        }

        public void Cleanup()
        {
            _isActive = false;
            _timer = 0f;
        }
    }
}
