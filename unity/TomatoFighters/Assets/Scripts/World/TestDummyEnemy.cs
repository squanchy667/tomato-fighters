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
                Debug.Log($"[TestDummyEnemy] Awake — hitbox='{hitbox.name}', attackData={(attackData != null ? attackData.attackName : "NULL")}");
            }
            else
            {
                Debug.LogError("[TestDummyEnemy] Awake — hitbox is NULL! Enemy attacks won't work.");
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
            var hbCol = hitbox.GetComponent<Collider2D>();
            // Find player via IDamageable (Shared) to avoid cross-pillar reference
            Transform playerT = null;
            Bounds playerBounds = default;
            foreach (var dmg in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (dmg is IDamageable && dmg.gameObject != gameObject)
                {
                    playerT = dmg.transform;
                    var pCol = dmg.GetComponent<Collider2D>();
                    if (pCol != null) playerBounds = pCol.bounds;
                    break;
                }
            }
            Debug.Log($"[TestDummyEnemy] Hitbox ENABLED — '{hitbox.name}', layer={hitbox.gameObject.layer}" +
                $"\n  Enemy pos={transform.position}, Hitbox bounds={hbCol?.bounds}" +
                $"\n  Player pos={playerT?.position}, Player bounds={playerBounds}" +
                $"\n  Distance={Vector2.Distance(transform.position, playerT != null ? (Vector2)playerT.position : (Vector2)transform.position):F2}");

            yield return new WaitForSeconds(attackActiveDuration);

            hitbox.gameObject.SetActive(false);
            _isAttacking = false;

            // Recovery: return to original color
            Sprite.color = originalColor;
        }

        // ── Hit Handling ──────────────────────────────────────────────────

        private void HandleHitDetected(IDamageable target, Vector2 hitPoint)
        {
            if (attackData == null)
            {
                Debug.LogWarning("[TestDummyEnemy] HandleHitDetected — attackData is null!");
                return;
            }

            bool isUnstoppable = attackData.telegraphType == TelegraphType.Unstoppable;
            var response = target.ResolveIncoming(transform.position, isUnstoppable);

            float damage = attackData.damageMultiplier * 10f; // Base enemy ATK placeholder

            var packet = new DamagePacket(
                type: DamageType.Physical,
                amount: damage,
                isPunishDamage: false,
                knockbackForce: attackData.knockbackForce,
                launchForce: attackData.launchForce,
                source: CharacterType.Brutor // Enemies don't have a CharacterType; use default
            );

            switch (response)
            {
                case DamageResponse.Hit:
                    if (!target.IsInvulnerable)
                    {
                        target.TakeDamage(packet);
                    }
                    break;

                case DamageResponse.Clashed:
                    if (!target.IsInvulnerable)
                    {
                        var clashPacket = new DamagePacket(
                            type: packet.type,
                            amount: packet.amount * 0.5f,
                            isPunishDamage: false,
                            knockbackForce: packet.knockbackForce * 0.3f,
                            launchForce: Vector2.zero,
                            source: packet.source);
                        target.TakeDamage(clashPacket);
                    }
                    break;

                case DamageResponse.Deflected:
                case DamageResponse.Dodged:
                    // No damage applied
                    break;
            }

            Debug.Log($"[TestDummyEnemy] Hit resolved: {response} ({damage:F1} base damage)");
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

        protected override void OnDamaged(DamagePacket damage)
        {
            // Brief white flash on hit for visual feedback
            if (_damageFlash != null) StopCoroutine(_damageFlash);
            _damageFlash = StartCoroutine(DamageFlashRoutine());
        }

        private Coroutine _damageFlash;

        private IEnumerator DamageFlashRoutine()
        {
            Color original = Sprite.color;
            Sprite.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            Sprite.color = original;
            _damageFlash = null;
        }
    }
}
