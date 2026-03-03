using System.Collections;
using TomatoFighters.Shared.Components;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// Concrete test enemy for validating the bidirectional damage pipeline.
    /// Attacks on a configurable timer — no AI state machine.
    /// Dev 3 replaces this with <c>BasicEnemyAI</c> (T022).
    /// </summary>
    public class TestDummyEnemy : EnemyBase
    {
        [Header("Test Dummy — Attack")]
        [SerializeField] private float attackInterval = 3f;
        [SerializeField] private AttackData attackData;
        [SerializeField] private float attackActiveDuration = 0.6f;

        [Header("Test Dummy — Hitbox")]
        [Tooltip("Child GameObject with HitboxDamage + trigger Collider2D on EnemyHitbox layer.")]
        [SerializeField] private HitboxDamage hitbox;

        // IAttacker state — only valid during active attack window
        private bool _isAttacking;
        private Coroutine _attackLoop;

        // ── IAttacker Overrides ───────────────────────────────────────────

        /// <inheritdoc/>
        public override AttackData CurrentAttack => _isAttacking ? attackData : null;

        /// <inheritdoc/>
        public override bool IsCurrentAttackUnstoppable =>
            _isAttacking && attackData != null && attackData.telegraphType == TelegraphType.Unstoppable;

        /// <inheritdoc/>
        public override TelegraphType CurrentTelegraphType =>
            _isAttacking && attackData != null ? attackData.telegraphType : TelegraphType.Normal;

        // ── Unity Lifecycle ───────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();

            if (hitbox != null)
            {
                hitbox.OnHitDetected += HandleHitDetected;
                hitbox.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            _attackLoop = StartCoroutine(AttackLoop());
        }

        private void OnDisable()
        {
            if (_attackLoop != null)
            {
                StopCoroutine(_attackLoop);
                _attackLoop = null;
            }

            _isAttacking = false;

            if (hitbox != null)
            {
                hitbox.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (hitbox != null)
            {
                hitbox.OnHitDetected -= HandleHitDetected;
            }
        }

        // ── Attack Timer ──────────────────────────────────────────────────

        private IEnumerator AttackLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(attackInterval);

                if (IsStunned) continue;

                yield return PerformAttack();
            }
        }

        private IEnumerator PerformAttack()
        {
            if (attackData == null)
            {
                Debug.LogWarning("[TestDummyEnemy] attackData is null — assign an AttackData SO.", this);
                yield break;
            }

            if (hitbox == null)
            {
                Debug.LogWarning("[TestDummyEnemy] hitbox is null — assign the Hitbox_Punch child.", this);
                yield break;
            }

            // Telegraph: flash white before the hit
            Color originalColor = Sprite.color;
            Sprite.color = Color.white;
            yield return new WaitForSeconds(0.4f);

            // Active frames: enable hitbox + show attack color
            _isAttacking = true;
            Sprite.color = new Color(1f, 0.2f, 0f); // Bright red-orange during swing
            hitbox.gameObject.SetActive(true);

            yield return new WaitForSeconds(attackActiveDuration);

            hitbox.gameObject.SetActive(false);
            _isAttacking = false;

            // Recovery: return to original color
            Sprite.color = originalColor;
        }

        // ── Hit Handling ──────────────────────────────────────────────────

        private void HandleHitDetected(IDamageable target, Vector2 hitPoint)
        {
            if (attackData == null) return;

            float damage = attackData.damageMultiplier * 10f; // Base enemy ATK placeholder

            var packet = new DamagePacket(
                type: DamageType.Physical,
                amount: damage,
                isPunishDamage: false,
                knockbackForce: attackData.knockbackForce,
                launchForce: attackData.launchForce,
                source: CharacterType.Brutor // Enemies don't have a CharacterType; use default
            );

            target.TakeDamage(packet);
        }

        // ── Virtual Hook Overrides ────────────────────────────────────────

        protected override void OnStunned()
        {
            // Stop attacking while stunned — hitbox disabled
            if (hitbox != null)
            {
                hitbox.gameObject.SetActive(false);
            }

            _isAttacking = false;
        }
    }
}
