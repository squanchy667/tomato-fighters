using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Trapper
{
    /// <summary>
    /// Trapper T3 signature (Main only): fire a massive anchor that chains to the ground.
    /// All enemies within large radius are pulled toward the anchor and slowed 50% for 4 seconds.
    /// Chained enemies take 30% more damage from all sources. Anchor persists for 6 seconds.
    /// Cooldown: 40s.
    /// </summary>
    public class AnchorChain : IPathAbility
    {
        private const string ID = "Trapper_AnchorChain";
        private const float COOLDOWN = 40f;
        private const float ANCHOR_DURATION = 6f;
        private const float PULL_RANGE = 6f;
        private const float SLOW_MAGNITUDE = 0.5f;
        private const float SLOW_DURATION = 4f;
        private const float DAMAGE_AMP = 0.3f;  // +30% via Mark
        private const float PULL_FORCE = 8f;
        private const float PULL_TICK_INTERVAL = 0.5f;

        private readonly PathAbilityContext _ctx;
        private float _cooldownRemaining;
        private float _anchorTimeRemaining;
        private bool _isAnchorActive;
        private Vector2 _anchorPosition;
        private float _pullTimer;
        private GameObject _anchorVisual;

        public AnchorChain(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Active;
        public float ManaCost => 0f;
        public float Cooldown => COOLDOWN;
        public bool IsActive => _isAnchorActive;
        public float CooldownRemaining => _cooldownRemaining;

        public bool TryActivate()
        {
            bool facingRight = _ctx.Motor != null && _ctx.Motor.FacingRight;
            float offsetX = facingRight ? 4f : -4f;
            _anchorPosition = (Vector2)_ctx.PlayerTransform.position + new Vector2(offsetX, 0f);

            _isAnchorActive = true;
            _anchorTimeRemaining = ANCHOR_DURATION;
            _pullTimer = 0f;
            _cooldownRemaining = COOLDOWN;

            // Visual placeholder
            _anchorVisual = new GameObject("AnchorChain");
            _anchorVisual.transform.position = (Vector3)_anchorPosition;
            var sr = _anchorVisual.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.4f, 0.4f, 0.4f);

            // Initial pull + debuffs
            ApplyPullAndDebuffs();

            Debug.Log($"[AnchorChain] DEPLOYED at {_anchorPosition} — {ANCHOR_DURATION}s, " +
                $"{SLOW_MAGNITUDE * 100}% slow, +{DAMAGE_AMP * 100}% damage taken");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= deltaTime;

            if (!_isAnchorActive) return;

            _anchorTimeRemaining -= deltaTime;
            _pullTimer += deltaTime;

            // Re-pull enemies that try to leave
            if (_pullTimer >= PULL_TICK_INTERVAL)
            {
                _pullTimer -= PULL_TICK_INTERVAL;
                ApplyPullAndDebuffs();
            }

            if (_anchorTimeRemaining <= 0f)
            {
                DestroyAnchor();
            }
        }

        public void Cleanup()
        {
            DestroyAnchor();
            _cooldownRemaining = 0f;
        }

        private void ApplyPullAndDebuffs()
        {
            var hits = Physics2D.OverlapCircleAll(_anchorPosition, PULL_RANGE, _ctx.EnemyLayer);
            foreach (var hit in hits)
            {
                // Pull toward anchor via knockback
                var damageable = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsInvulnerable)
                {
                    Vector2 pullDir = (_anchorPosition - (Vector2)hit.transform.position).normalized;
                    damageable.ApplyKnockback(pullDir * PULL_FORCE);
                }

                // Apply slow + damage amplify (using Mark for damage amp)
                var statusEffectable = hit.GetComponent<IStatusEffectable>()
                    ?? hit.GetComponentInParent<IStatusEffectable>();
                if (statusEffectable != null)
                {
                    statusEffectable.AddEffect(new StatusEffect(
                        StatusEffectType.Slow, SLOW_DURATION, SLOW_MAGNITUDE, _ctx.PlayerTransform));
                    statusEffectable.AddEffect(new StatusEffect(
                        StatusEffectType.Mark, SLOW_DURATION, DAMAGE_AMP, _ctx.PlayerTransform));
                }
            }
        }

        private void DestroyAnchor()
        {
            _isAnchorActive = false;
            _anchorTimeRemaining = 0f;
            if (_anchorVisual != null)
            {
                Object.Destroy(_anchorVisual);
                _anchorVisual = null;
            }
            Debug.Log("[AnchorChain] Anchor expired");
        }
    }
}
