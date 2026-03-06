using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Marksman
{
    /// <summary>
    /// Marksman T3 signature (Main only): 2s charge (cannot move), then 800% RATK
    /// ultra-charged sniper shot. Guaranteed crit. Ignores DEF. If Killshot kills the target,
    /// the projectile continues through ALL enemies for 400% each. Cooldown: 30s.
    /// </summary>
    public class Killshot : IChanneledAbility
    {
        private const string ID = "Marksman_Killshot";
        private const float CHARGE_DURATION = 2f;
        private const float COOLDOWN = 30f;
        private const float PRIMARY_DAMAGE = 80f;     // 800% RATK
        private const float PASSTHROUGH_DAMAGE = 40f;  // 400% RATK
        private const float RANGE = 20f;

        private readonly PathAbilityContext _ctx;
        private float _cooldownRemaining;
        private float _chargeRemaining;
        private bool _isCharging;

        public Killshot(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Channeled;
        public float ManaCost => 0f;
        public float Cooldown => COOLDOWN;
        public bool IsActive => _isCharging;
        public float CooldownRemaining => _cooldownRemaining;

        public bool TryActivate()
        {
            _isCharging = true;
            _chargeRemaining = CHARGE_DURATION;

            if (_ctx.Motor != null)
                _ctx.Motor.SetAttackLock(true);

            Debug.Log($"[Killshot] Charging... {CHARGE_DURATION}s");
            return true;
        }

        public void Release()
        {
            if (!_isCharging) return;

            if (_chargeRemaining > 0f)
            {
                // Released too early — cancel without firing
                _isCharging = false;
                if (_ctx.Motor != null)
                    _ctx.Motor.SetAttackLock(false);
                Debug.Log("[Killshot] Cancelled — released before fully charged");
                return;
            }

            Fire();
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= deltaTime;

            if (!_isCharging) return;

            _chargeRemaining -= deltaTime;
            if (_chargeRemaining <= 0f)
            {
                _chargeRemaining = 0f;
                // Auto-fire on full charge
                Fire();
            }
        }

        public void Cleanup()
        {
            if (_isCharging && _ctx.Motor != null)
                _ctx.Motor.SetAttackLock(false);
            _isCharging = false;
            _cooldownRemaining = 0f;
        }

        private void Fire()
        {
            _isCharging = false;
            _cooldownRemaining = COOLDOWN;

            if (_ctx.Motor != null)
                _ctx.Motor.SetAttackLock(false);

            bool facingRight = _ctx.Motor != null && _ctx.Motor.FacingRight;
            Vector2 dir = facingRight ? Vector2.right : Vector2.left;
            Vector2 origin = (Vector2)_ctx.PlayerTransform.position;

            var hits = Physics2D.RaycastAll(origin, dir, RANGE, _ctx.EnemyLayer);

            bool primaryKilled = false;
            for (int i = 0; i < hits.Length; i++)
            {
                var damageable = hits[i].collider.GetComponent<IDamageable>()
                    ?? hits[i].collider.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsInvulnerable) continue;

                float damage = (i == 0 && !primaryKilled) ? PRIMARY_DAMAGE : PASSTHROUGH_DAMAGE;

                var packet = new DamagePacket(
                    type: DamageType.Physical,
                    amount: damage,
                    isPunishDamage: false,
                    knockbackForce: dir * 6f,
                    launchForce: Vector2.zero,
                    source: CharacterType.Viper,
                    stunFillAmount: 10f);
                damageable.TakeDamage(packet);

                // Check if primary target was killed for passthrough logic
                if (i == 0 && damageable.CurrentHealth <= 0f)
                    primaryKilled = true;
                else if (i == 0 && !primaryKilled)
                    break; // Primary survived — no passthrough
            }

            Debug.Log($"[Killshot] FIRED! {PRIMARY_DAMAGE:F0} primary{(primaryKilled ? " + passthrough!" : "")}");
        }
    }
}
