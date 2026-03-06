using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Bulwark
{
    /// <summary>
    /// Bulwark T2 passive: every 3rd hit blocked triggers an automatic counter-strike
    /// dealing 150% ATK that ignores enemy defense and staggers the attacker.
    /// </summary>
    public class Retaliation : IPathAbility
    {
        private const string ID = "Bulwark_Retaliation";
        private const int BLOCKS_PER_COUNTER = 3;
        private const float COUNTER_DAMAGE_MULT = 1.5f;
        private const float COUNTER_RANGE = 2f;

        private readonly PathAbilityContext _ctx;
        private bool _isActive;
        private int _blockCount;

        public Retaliation(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Passive;
        public float ManaCost => 0f;
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        /// <summary>Current block count toward next counter.</summary>
        public int BlockCount => _blockCount;

        /// <summary>
        /// Called by the defense pipeline when a block/deflect succeeds.
        /// Increments counter and fires retaliation on threshold.
        /// </summary>
        public void OnBlockSuccess()
        {
            if (!_isActive) return;

            _blockCount++;
            if (_blockCount >= BLOCKS_PER_COUNTER)
            {
                _blockCount = 0;
                FireCounterStrike();
            }
        }

        public bool TryActivate()
        {
            _isActive = true;
            _blockCount = 0;
            return true;
        }

        public void Tick(float deltaTime) { }

        public void Cleanup()
        {
            _isActive = false;
            _blockCount = 0;
        }

        private void FireCounterStrike()
        {
            var hits = Physics2D.OverlapCircleAll(
                _ctx.PlayerTransform.position, COUNTER_RANGE, _ctx.EnemyLayer);

            if (hits.Length == 0) return;

            // Find nearest enemy
            float bestDist = float.MaxValue;
            Collider2D bestTarget = null;
            foreach (var hit in hits)
            {
                float dist = Vector2.Distance(_ctx.PlayerTransform.position, hit.transform.position);
                if (dist < bestDist) { bestDist = dist; bestTarget = hit; }
            }

            if (bestTarget == null) return;

            var damageable = bestTarget.GetComponent<IDamageable>()
                ?? bestTarget.GetComponentInParent<IDamageable>();

            if (damageable != null && !damageable.IsInvulnerable)
            {
                float damage = COUNTER_DAMAGE_MULT * 10f; // Base damage scaled by ATK via combat pipeline
                var packet = new DamagePacket(
                    type: DamageType.Physical,
                    amount: damage,
                    isPunishDamage: false,
                    knockbackForce: Vector2.zero,
                    launchForce: Vector2.zero,
                    source: _ctx.Motor != null ? _ctx.Motor.CharacterType : CharacterType.Brutor,
                    stunFillAmount: 5f);
                damageable.TakeDamage(packet);
                Debug.Log($"[Retaliation] Counter-strike! {damage:F0} damage to {bestTarget.name}");
            }
        }
    }
}
