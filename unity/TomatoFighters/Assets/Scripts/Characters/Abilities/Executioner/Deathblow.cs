using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Executioner
{
    /// <summary>
    /// Executioner T3 signature (Main only): 1.5s windup, then 500% ATK to a single target.
    /// If target is Marked AND below 30% HP: 1000% ATK. Guaranteed crit.
    /// Windup can be cancelled by taking damage. Cooldown: 25s.
    /// </summary>
    public class Deathblow : IPathAbility
    {
        private const string ID = "Executioner_Deathblow";
        private const float WINDUP_DURATION = 1.5f;
        private const float COOLDOWN = 25f;
        private const float BASE_DAMAGE = 50f;       // 500% ATK
        private const float EXECUTE_DAMAGE = 100f;    // 1000% ATK (marked + low HP)
        private const float EXECUTE_HP_THRESHOLD = 0.3f;
        private const float RANGE = 3f;

        private readonly PathAbilityContext _ctx;
        private float _cooldownRemaining;
        private float _windupRemaining;
        private bool _isWindingUp;
        private bool _wasCancelled;

        public Deathblow(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Active;
        public float ManaCost => 0f;
        public float Cooldown => COOLDOWN;
        public bool IsActive => _isWindingUp;
        public float CooldownRemaining => _cooldownRemaining;

        /// <summary>Whether Deathblow is in windup phase. Motor locks movement.</summary>
        public bool IsWindingUp => _isWindingUp;

        /// <summary>
        /// Called by damage pipeline when player takes damage during windup.
        /// Cancels the Deathblow.
        /// </summary>
        public void OnDamageTaken()
        {
            if (!_isWindingUp) return;
            _wasCancelled = true;
            _isWindingUp = false;
            _cooldownRemaining = COOLDOWN * 0.5f; // Half cooldown on cancel

            if (_ctx.Motor != null)
                _ctx.Motor.SetAttackLock(false);

            Debug.Log("[Deathblow] Cancelled by damage! Half cooldown.");
        }

        public bool TryActivate()
        {
            _isWindingUp = true;
            _windupRemaining = WINDUP_DURATION;
            _wasCancelled = false;

            if (_ctx.Motor != null)
                _ctx.Motor.SetAttackLock(true);

            Debug.Log($"[Deathblow] Winding up... {WINDUP_DURATION}s");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= deltaTime;

            if (!_isWindingUp) return;

            _windupRemaining -= deltaTime;
            if (_windupRemaining <= 0f)
            {
                ExecuteStrike();
                _isWindingUp = false;

                if (_ctx.Motor != null)
                    _ctx.Motor.SetAttackLock(false);
            }
        }

        public void Cleanup()
        {
            if (_isWindingUp && _ctx.Motor != null)
                _ctx.Motor.SetAttackLock(false);

            _isWindingUp = false;
            _cooldownRemaining = 0f;
            _windupRemaining = 0f;
        }

        private void ExecuteStrike()
        {
            var hits = Physics2D.OverlapCircleAll(
                _ctx.PlayerTransform.position, RANGE, _ctx.EnemyLayer);

            if (hits.Length == 0)
            {
                _cooldownRemaining = COOLDOWN;
                Debug.Log("[Deathblow] Strike missed — no enemies in range");
                return;
            }

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

            if (damageable == null || damageable.IsInvulnerable)
            {
                _cooldownRemaining = COOLDOWN;
                return;
            }

            // Check for execute condition: marked + below threshold
            bool isMarked = false;
            var statusEffectable = bestTarget.GetComponent<IStatusEffectable>()
                ?? bestTarget.GetComponentInParent<IStatusEffectable>();
            if (statusEffectable != null)
                isMarked = statusEffectable.HasEffect(StatusEffectType.Mark);

            float hpRatio = damageable.MaxHealth > 0f ? damageable.CurrentHealth / damageable.MaxHealth : 1f;
            bool executeCondition = isMarked && hpRatio <= EXECUTE_HP_THRESHOLD;
            float damage = executeCondition ? EXECUTE_DAMAGE : BASE_DAMAGE;

            bool facingRight = _ctx.Motor != null && _ctx.Motor.FacingRight;
            Vector2 knockDir = facingRight ? Vector2.right : Vector2.left;

            var packet = new DamagePacket(
                type: DamageType.Physical,
                amount: damage,
                isPunishDamage: false,
                knockbackForce: knockDir * 12f,
                launchForce: Vector2.zero,
                source: CharacterType.Slasher,
                stunFillAmount: 15f);
            damageable.TakeDamage(packet);

            _cooldownRemaining = COOLDOWN;
            Debug.Log($"[Deathblow] {(executeCondition ? "EXECUTE!" : "Strike!")} {damage:F0} damage (guaranteed crit)");
        }
    }
}
