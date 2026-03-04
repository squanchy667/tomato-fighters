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

        [Header("Test Dummy — Clash")]
        [SerializeField] private ClashTracker clashTracker;

        [Header("Test Dummy — Telegraph Visual")]
        [Tooltip("Duration of the telegraph warning before the hitbox activates.")]
        [SerializeField] private float telegraphDuration = 0.4f;

        // IAttacker state — only valid during active attack window
        private bool _isAttacking;
        private Coroutine _attackLoop;
        private GameObject _telegraphVisual;
        private SpriteRenderer _telegraphSR;

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
                CreateTelegraphVisual();
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
                hitbox.gameObject.SetActive(false);

            if (_telegraphVisual != null)
                _telegraphVisual.SetActive(false);
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

            clashTracker?.ClearImmunities();

            // Telegraph: show danger zone filling up, then flash white at the end
            Color originalColor = Sprite.color;

            // Open clash window on DefenseSystem so incoming attacks resolve as Clashed
            if (DefenseProvider != null)
            {
                Vector2 facingDir = FindPlayerDirection();
                DefenseProvider.OpenClashWindow(telegraphDuration, facingDir);
            }

            yield return TelegraphFill();

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

        // ── Telegraph Visual ────────────────────────────────────────────────

        /// <summary>
        /// Creates a child sprite that matches the hitbox area — used as the
        /// telegraph warning zone. Stays hidden until PerformAttack activates it.
        /// </summary>
        private void CreateTelegraphVisual()
        {
            var col = hitbox.GetComponent<Collider2D>();
            if (col == null) return;

            _telegraphVisual = new GameObject("TelegraphWarning");
            _telegraphVisual.transform.SetParent(hitbox.transform.parent);
            _telegraphVisual.transform.localPosition = hitbox.transform.localPosition;

            _telegraphSR = _telegraphVisual.AddComponent<SpriteRenderer>();
            _telegraphSR.sortingOrder = 5;

            // Match hitbox size
            Vector2 size;
            Vector2 offset;
            if (col is BoxCollider2D box)
            {
                size = box.size;
                offset = box.offset;
            }
            else if (col is CircleCollider2D circle)
            {
                float d = circle.radius * 2f;
                size = new Vector2(d, d);
                offset = circle.offset;
            }
            else
            {
                size = col.bounds.size;
                offset = Vector2.zero;
            }

            // Grab WhiteSquare from the hitbox's DebugVisual child (set by TestDummyPrefabCreator)
            var debugVisualT = hitbox.transform.Find("DebugVisual");
            if (debugVisualT != null)
            {
                var debugSR = debugVisualT.GetComponent<SpriteRenderer>();
                if (debugSR != null && debugSR.sprite != null)
                    _telegraphSR.sprite = debugSR.sprite;
            }
            _telegraphVisual.transform.localScale = size;
            _telegraphVisual.transform.localPosition += (Vector3)offset;

            _telegraphVisual.SetActive(false);
        }

        /// <summary>
        /// Animates the telegraph zone: starts transparent yellow, fills to opaque
        /// over the telegraph duration, then flashes white right before the hit.
        /// </summary>
        private IEnumerator TelegraphFill()
        {
            if (_telegraphVisual != null)
            {
                _telegraphVisual.SetActive(true);
                _telegraphSR.color = new Color(1f, 1f, 0f, 0f);
            }

            Sprite.color = new Color(1f, 1f, 0.5f); // Pale yellow during telegraph

            float elapsed = 0f;
            while (elapsed < telegraphDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / telegraphDuration;

                if (_telegraphSR != null)
                {
                    // Yellow → orange, alpha ramps from 0.1 to 0.6
                    float alpha = Mathf.Lerp(0.1f, 0.6f, t);
                    float r = 1f;
                    float g = Mathf.Lerp(1f, 0.4f, t); // yellow → orange
                    _telegraphSR.color = new Color(r, g, 0f, alpha);
                }

                yield return null;
            }

            // Final flash: bright white on sprite for 1 frame
            Sprite.color = Color.white;
            if (_telegraphSR != null)
                _telegraphSR.color = new Color(1f, 0.2f, 0f, 0.8f); // Bright red flash

            yield return null;

            // Hide telegraph — hitbox is about to go live
            if (_telegraphVisual != null)
                _telegraphVisual.SetActive(false);
        }

        /// <summary>Returns normalized direction toward the nearest IDamageable (player).</summary>
        private Vector2 FindPlayerDirection()
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is IDamageable && mb.gameObject != gameObject)
                {
                    return ((Vector2)(mb.transform.position - transform.position)).normalized;
                }
            }
            return Vector2.left; // Default fallback
        }

        // ── Hit Handling ──────────────────────────────────────────────────

        private void HandleHitDetected(IDamageable target, Vector2 hitPoint)
        {
            if (attackData == null)
            {
                Debug.LogWarning("[TestDummyEnemy] HandleHitDetected — attackData is null!");
                return;
            }

            // Clash immunity: skip targets that were already part of a clash resolution
            if (clashTracker != null && clashTracker.HasClashImmunity(target))
            {
                Debug.Log($"[TestDummyEnemy] Skipping {target} — clash immunity active");
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
                    // No damage on clash — mutual cancel
                    // Register reciprocal immunity: target's future hitbox should skip this entity
                    if (target is MonoBehaviour tmb)
                    {
                        var targetTracker = tmb.GetComponentInChildren<ClashTracker>();
                        targetTracker?.AddClashImmunity(this); // EnemyBase : IDamageable
                    }
                    break;

                case DamageResponse.Deflected:
                case DamageResponse.Dodged:
                    // No damage applied
                    break;
            }

            // Notify target's defense system so visual feedback (DefenseDebugUI) fires
            if (response != DamageResponse.Hit && target is MonoBehaviour defMb)
            {
                var defenseProvider = defMb.GetComponent<IDefenseProvider>();
                defenseProvider?.NotifyDefenseSuccess(response, damage, DamageType.Physical);
            }

            Debug.Log($"[TestDummyEnemy] Hit resolved: {response} ({damage:F1} base damage)");
        }

        // ── Virtual Hook Overrides ────────────────────────────────────────

        protected override void OnStunned()
        {
            if (hitbox != null)
                hitbox.gameObject.SetActive(false);

            if (_telegraphVisual != null)
                _telegraphVisual.SetActive(false);

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
