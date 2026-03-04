using System;
using TomatoFighters.Shared.Data;

namespace TomatoFighters.Characters.Passives
{
    /// <summary>
    /// Mystica passive — "Arcane Resonance".
    /// +5% damage per cast (self-buff), stacks up to 3 times.
    /// Each stack has an independent 3-second expiry timer.
    /// Multiplicative stacking: 1.05^activeStacks (3 stacks ~ 1.157x).
    /// Triggers on any attack event (light, heavy, or ability).
    /// </summary>
    public class ArcaneResonance : IPassiveAbility
    {
        private readonly float _dmgPerStack;
        private readonly int _maxStacks;
        private readonly float _stackDuration;

        // Each element tracks remaining time for that stack
        private readonly float[] _stackTimers;

        /// <summary>Number of active (non-expired) stacks.</summary>
        public int ActiveStacks
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _stackTimers.Length; i++)
                {
                    if (_stackTimers[i] > 0f) count++;
                }
                return count;
            }
        }

        public ArcaneResonance(PassiveConfig config)
        {
            _dmgPerStack = config.arcaneResonanceDmgPerStack;
            _maxStacks = config.arcaneResonanceMaxStacks;
            _stackDuration = config.arcaneResonanceStackDuration;
            _stackTimers = new float[_maxStacks];
        }

        public float GetDamageMultiplier(HitContext context)
        {
            int stacks = ActiveStacks;
            if (stacks <= 0) return 1f;

            // Multiplicative: (1 + dmgPerStack)^stacks
            return (float)Math.Pow(1f + _dmgPerStack, stacks);
        }

        public float GetDefenseMultiplier() => 1f;
        public float GetKnockbackMultiplier() => 1f;
        public float GetSpeedMultiplier() => 1f;

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < _stackTimers.Length; i++)
            {
                if (_stackTimers[i] > 0f)
                    _stackTimers[i] -= deltaTime;
            }
        }

        public void OnHitLanded() { }

        public void OnAttackPerformed()
        {
            // Find the oldest stack slot (smallest remaining time) to refresh/add
            int slotIndex = -1;
            float minTime = float.MaxValue;

            for (int i = 0; i < _stackTimers.Length; i++)
            {
                if (_stackTimers[i] <= 0f)
                {
                    // Empty slot — use immediately
                    slotIndex = i;
                    break;
                }

                if (_stackTimers[i] < minTime)
                {
                    minTime = _stackTimers[i];
                    slotIndex = i;
                }
            }

            if (slotIndex >= 0)
                _stackTimers[slotIndex] = _stackDuration;
        }
    }
}
