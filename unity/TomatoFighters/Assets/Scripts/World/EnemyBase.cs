using System;
using System.Collections;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using TomatoFighters.World.UI;
using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// Abstract base class for all enemies. Implements <see cref="IDamageable"/> fully
    /// and <see cref="IAttacker"/> as virtual stubs for subclasses to override.
    /// All stat values come from <see cref="EnemyData"/> — no hardcoded numbers.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable, IAttacker
    {
        [Header("Data")]
        [SerializeField] private EnemyData enemyData;

        [Header("HUD")]
        [SerializeField]
        [Tooltip("Prefab for the world-space health bar. Spawned above the enemy. " +
                 "Null is safe — no health bar will be shown.")]
        private GameObject healthBarPrefab;

        [SerializeField]
        [Tooltip("Offset above the enemy sprite for the health bar.")]
        private Vector3 healthBarOffset = new Vector3(0f, 1.2f, 0f);

        [Header("Defense")]
        [Tooltip("Assign a DefenseSystem component (implements IDefenseProvider). Null = no defense.")]
        [SerializeField] private Component defenseProviderComponent;

        /// <summary>Fired when this enemy dies. WaveManager subscribes to track alive count.</summary>
        public event Action OnDied;

        /// <summary>Fired when this enemy becomes stunned. Camera/UI/Roguelite subscribe.</summary>
        public event Action<StunEventData> StunTriggered;

        /// <summary>Fired when this enemy recovers from stun.</summary>
        public event Action<StunRecoveredEventData> StunRecovered;

        // ── Cached Components ─────────────────────────────────────────────

        protected Rigidbody2D Rb { get; private set; }
        protected SpriteRenderer Sprite { get; private set; }
        protected Collider2D[] Colliders { get; private set; }
        protected EnemyData Data => enemyData;

        private IDefenseProvider _defenseProvider;
        private IJuggleTarget _juggleTarget;

        /// <summary>The entity's defense provider, if any. Subclasses use this to open clash windows.</summary>
        protected IDefenseProvider DefenseProvider => _defenseProvider;

        // ── Health ────────────────────────────────────────────────────────

        private float _currentHealth;
        private bool _isDead;
        private Coroutine _knockbackCoroutine;

        /// <inheritdoc/>
        public float CurrentHealth => _currentHealth;

        /// <inheritdoc/>
        public float MaxHealth => enemyData.maxHealth;

        // ── Pressure / Stun ───────────────────────────────────────────────

        private float _pressureFill;
        private bool _isStunned;
        private CharacterType _lastHitBy;

        /// <inheritdoc/>
        public bool IsStunned => _isStunned;

        /// <summary>Pressure fill as a normalized ratio (0-1) for UI display.</summary>
        public float PressureRatio => enemyData.pressureThreshold > 0f
            ? Mathf.Clamp01(_pressureFill / enemyData.pressureThreshold)
            : 0f;

        // ── Invulnerability ───────────────────────────────────────────────

        private bool _isInvulnerable;

        /// <inheritdoc/>
        public bool IsInvulnerable => _isInvulnerable;

        /// <summary>
        /// Allows external systems (e.g. BossPhaseTransitionState) to toggle invulnerability.
        /// Does not affect the stun-recovery blink — that uses its own internal flow.
        /// </summary>
        public void SetInvulnerableExternal(bool invulnerable)
        {
            _isInvulnerable = invulnerable;
        }

        // ── IAttacker (virtual stubs) ─────────────────────────────────────

        /// <inheritdoc/>
        public virtual AttackData CurrentAttack => null;

        /// <inheritdoc/>
        public virtual bool IsCurrentAttackUnstoppable => false;

        /// <inheritdoc/>
        public virtual TelegraphType CurrentTelegraphType => TelegraphType.Normal;

        /// <inheritdoc/>
        public virtual float PunishWindowDuration => 0f;

        /// <inheritdoc/>
        public virtual bool IsInPunishableState => false;

        /// <inheritdoc/>
        public virtual bool IsInClashWindow => _defenseProvider?.IsInClashWindow ?? false;

        // ── Unity Lifecycle ───────────────────────────────────────────────

        protected virtual void Awake()
        {
            Rb = GetComponent<Rigidbody2D>();
            Sprite = GetComponentInChildren<SpriteRenderer>();
            Colliders = GetComponentsInChildren<Collider2D>();
            _defenseProvider = defenseProviderComponent as IDefenseProvider;
            _juggleTarget = GetComponent<IJuggleTarget>();

            if (_juggleTarget != null)
            {
                _juggleTarget.OnWallBounced += HandleWallBounce;
            }

            _currentHealth = enemyData.maxHealth;

            SpawnHealthBar();
        }

        private void SpawnHealthBar()
        {
            if (healthBarPrefab == null) return;

            var barGO = Instantiate(healthBarPrefab, transform);
            barGO.transform.localPosition = healthBarOffset;

            var barUI = barGO.GetComponent<EnemyHealthBarUI>();
            if (barUI != null)
            {
                barUI.Initialize(this);
            }
        }

        // ── IDamageable Implementation ────────────────────────────────────

        /// <inheritdoc/>
        public DamageResponse ResolveIncoming(Vector2 attackerPosition, bool isUnstoppable)
        {
            if (_defenseProvider == null) return DamageResponse.Hit;
            return _defenseProvider.ResolveDefense(attackerPosition, isUnstoppable);
        }

        /// <inheritdoc/>
        public void TakeDamage(DamagePacket damage)
        {
            if (_isDead || _isInvulnerable)
            {
                Debug.Log($"[EnemyBase] TakeDamage BLOCKED — dead={_isDead}, invulnerable={_isInvulnerable}");
                return;
            }

            _currentHealth -= damage.amount;
            _lastHitBy = damage.source;
            Debug.Log($"[EnemyBase] TakeDamage: {damage.amount:F1} dmg → HP: {_currentHealth:F1}/{enemyData.maxHealth}");

            // Use pre-calculated stun fill from attacker's stats (DD-1)
            AddStun(damage.stunFillAmount);

            // State transition first (zeros velocity via Exit/Enter), then knockback
            OnDamaged(damage);
            ApplyKnockback(damage.knockbackForce);
            ApplyLaunch(damage.launchForce);

            if (_currentHealth <= 0f)
            {
                _currentHealth = 0f;
                Die();
            }
        }

        /// <inheritdoc/>
        public void AddStun(float amount)
        {
            if (_isDead || _isStunned) return;

            _pressureFill += amount;

            if (_pressureFill >= enemyData.pressureThreshold)
            {
                StartCoroutine(StunRoutine());
            }
        }

        /// <inheritdoc/>
        public void ApplyKnockback(Vector2 force)
        {
            if (_isDead) return;
            if (force == Vector2.zero) return;

            Vector2 reducedForce = force * (1f - enemyData.knockbackResistance);
            Rb.AddForce(reducedForce, ForceMode2D.Impulse);

            // Notify juggle system for wall bounce tracking
            _juggleTarget?.NotifyKnockback(reducedForce);

            // Only run knockback recovery if juggle system isn't managing state
            if (_juggleTarget == null)
            {
                if (_knockbackCoroutine != null)
                    StopCoroutine(_knockbackCoroutine);
                _knockbackCoroutine = StartCoroutine(KnockbackRecovery());
            }
        }

        private IEnumerator KnockbackRecovery()
        {
            yield return new WaitForSeconds(0.5f);
            Rb.linearVelocity = Vector2.zero;
            _knockbackCoroutine = null;
        }

        /// <inheritdoc/>
        public void ApplyLaunch(Vector2 force)
        {
            if (_isDead) return;
            if (force == Vector2.zero) return;

            Vector2 reducedForce = force * (1f - enemyData.knockbackResistance);

            if (_juggleTarget != null)
            {
                // Delegate to juggle system — it handles simulated height and horizontal force
                _juggleTarget.Launch(reducedForce);
            }
            else
            {
                // Fallback: apply as physics impulse (pre-juggle behavior)
                Rb.AddForce(reducedForce, ForceMode2D.Impulse);
            }
        }

        // ── Stun / Recovery ───────────────────────────────────────────────

        private IEnumerator StunRoutine()
        {
            _isStunned = true;
            OnStunned();

            StunTriggered?.Invoke(new StunEventData(
                _lastHitBy, transform.position, enemyData.stunDuration));

            yield return new WaitForSeconds(enemyData.stunDuration);

            _isStunned = false;
            _pressureFill = 0f;

            OnRecovery();

            StunRecovered?.Invoke(new StunRecoveredEventData(transform.position));

            // Defer invulnerability blink until landing if currently airborne
            if (_juggleTarget != null && _juggleTarget.IsAirborne)
            {
                _juggleTarget.RequestInvulnerabilityOnLanding(
                    () => StartCoroutine(InvulnerabilityBlink()));
            }
            else
            {
                yield return InvulnerabilityBlink();
            }
        }

        private IEnumerator InvulnerabilityBlink()
        {
            _isInvulnerable = true;

            Color originalColor = Sprite.color;
            float elapsed = 0f;
            const float blinkInterval = 0.1f;
            bool isWhite = false;

            while (elapsed < enemyData.invulnerabilityDuration)
            {
                isWhite = !isWhite;
                Sprite.color = isWhite ? Color.white : originalColor;
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            Sprite.color = originalColor;
            _isInvulnerable = false;
        }

        // ── Death ─────────────────────────────────────────────────────────

        private void Die()
        {
            _isDead = true;

            // Disable colliders so nothing can hit the corpse
            foreach (var col in Colliders)
            {
                col.enabled = false;
            }

            OnDeath();
            OnDied?.Invoke();

            // Trigger death animation if Animator is present
            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Death");
            }

            Destroy(gameObject, 1f);
        }

        // ── Wall Bounce ──────────────────────────────────────────────────

        /// <summary>
        /// Handles wall bounce damage from IJuggleTarget. Applies minor damage
        /// directly to health — no knockback, no pressure fill.
        /// </summary>
        private void HandleWallBounce(Vector2 bouncePosition, float damage)
        {
            if (_isDead || _isInvulnerable) return;
            if (damage <= 0f) return;

            _currentHealth -= damage;
            Debug.Log($"[EnemyBase] Wall bounce damage: {damage:F1} → HP: {_currentHealth:F1}/{enemyData.maxHealth}");

            OnWallBounce(bouncePosition, damage);

            if (_currentHealth <= 0f)
            {
                _currentHealth = 0f;
                Die();
            }
        }

        // ── Virtual Hooks ─────────────────────────────────────────────────

        /// <summary>Called after damage is applied but before death check.</summary>
        protected virtual void OnDamaged(DamagePacket damage) { }

        /// <summary>Called when pressure meter fills and stun begins.</summary>
        protected virtual void OnStunned() { }

        /// <summary>Called when the enemy dies (health reaches zero).</summary>
        protected virtual void OnDeath() { }

        /// <summary>Called when stun ends and recovery begins (before invulnerability blink).</summary>
        protected virtual void OnRecovery() { }

        /// <summary>Called when a wall bounce deals minor damage. Override for visual effects.</summary>
        protected virtual void OnWallBounce(Vector2 position, float damage) { }
    }
}
