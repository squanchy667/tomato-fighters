using TomatoFighters.Shared.Data;

namespace TomatoFighters.Characters.Passives
{
    /// <summary>
    /// Slasher passive — "Bloodlust".
    /// +3% ATK per hit landed, stacks up to 10 (max +30%).
    /// Stacks reset to 0 after 3 seconds without landing a hit.
    /// Combo drops do NOT reset stacks — only the timer.
    /// </summary>
    public class Bloodlust : IPassiveAbility
    {
        private readonly float _atkPerStack;
        private readonly int _maxStacks;
        private readonly float _decayTime;

        private int _stacks;
        private float _timeSinceLastHit;

        /// <summary>Current stack count (0 to maxStacks).</summary>
        public int Stacks => _stacks;

        public Bloodlust(PassiveConfig config)
        {
            _atkPerStack = config.bloodlustAtkPerStack;
            _maxStacks = config.bloodlustMaxStacks;
            _decayTime = config.bloodlustDecayTime;
            _timeSinceLastHit = 0f;
        }

        public float GetDamageMultiplier(HitContext context)
        {
            return 1f + (_atkPerStack * _stacks);
        }

        public float GetDefenseMultiplier() => 1f;
        public float GetKnockbackMultiplier() => 1f;
        public float GetSpeedMultiplier() => 1f;

        public void Tick(float deltaTime)
        {
            if (_stacks <= 0) return;

            _timeSinceLastHit += deltaTime;
            if (_timeSinceLastHit >= _decayTime)
            {
                _stacks = 0;
            }
        }

        public void OnHitLanded()
        {
            if (_stacks < _maxStacks)
                _stacks++;

            _timeSinceLastHit = 0f;
        }

        public void OnAttackPerformed() { }
    }
}
