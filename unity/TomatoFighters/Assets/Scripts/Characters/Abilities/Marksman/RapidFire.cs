using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Marksman
{
    /// <summary>
    /// Marksman T2 passive: every 5th ranged attack fires a 3-shot burst at 60% RATK each.
    /// Combined with Piercing Shots (T1), shreds packed groups.
    /// </summary>
    public class RapidFire : IPathAbility
    {
        private const string ID = "Marksman_RapidVolleys";
        private const int ATTACKS_PER_BURST = 5;
        private const int BURST_COUNT = 3;
        private const float BURST_DAMAGE_MULT = 0.6f;
        private const float BURST_DAMAGE_BASE = 8f;
        private const float BURST_RANGE = 10f;

        private readonly PathAbilityContext _ctx;
        private bool _isActive;
        private int _attackCount;

        public RapidFire(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Passive;
        public float ManaCost => 0f;
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        /// <summary>Current attack count toward next burst.</summary>
        public int AttackCount => _attackCount;

        /// <summary>
        /// Called by the ranged attack pipeline on each shot.
        /// Tracks count and fires burst on every 5th attack.
        /// </summary>
        public void OnRangedAttack()
        {
            if (!_isActive) return;

            _attackCount++;
            if (_attackCount >= ATTACKS_PER_BURST)
            {
                _attackCount = 0;
                FireBurst();
            }
        }

        private void FireBurst()
        {
            bool facingRight = _ctx.Motor != null && _ctx.Motor.FacingRight;
            Vector2 dir = facingRight ? Vector2.right : Vector2.left;
            Vector2 origin = (Vector2)_ctx.PlayerTransform.position;

            var hits = Physics2D.RaycastAll(origin, dir, BURST_RANGE, _ctx.EnemyLayer);

            foreach (var hit in hits)
            {
                var damageable = hit.collider.GetComponent<IDamageable>()
                    ?? hit.collider.GetComponentInParent<IDamageable>();

                if (damageable != null && !damageable.IsInvulnerable)
                {
                    float damage = BURST_DAMAGE_BASE * BURST_DAMAGE_MULT * BURST_COUNT;
                    var packet = new DamagePacket(
                        type: DamageType.Physical,
                        amount: damage,
                        isPunishDamage: false,
                        knockbackForce: Vector2.zero,
                        launchForce: Vector2.zero,
                        source: CharacterType.Viper,
                        stunFillAmount: 0f);
                    damageable.TakeDamage(packet);
                }
            }

            Debug.Log($"[RapidFire] Burst fired! {BURST_COUNT} shots at {BURST_DAMAGE_MULT * 100}% RATK");
        }

        public bool TryActivate()
        {
            _isActive = true;
            _attackCount = 0;
            return true;
        }

        public void Tick(float deltaTime) { }

        public void Cleanup()
        {
            _isActive = false;
            _attackCount = 0;
        }
    }
}
