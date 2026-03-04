using System;
using System.Collections;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
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

        [Header("Defense")]
        [Tooltip("Assign a DefenseSystem component (implements IDefenseProvider). Null = no defense.")]
        [SerializeField] private Component defenseProviderComponent;

        /// <summary>Fired when this enemy dies. WaveManager subscribes to track alive count.</summary>
        public event Action OnDied;

        // ── Cached Components ─────────────────────────────────────────────

        protected Rigidbody2D Rb { get; private set; }
        protected SpriteRenderer Sprite { get; private set; }
        protected Collider2D[] Colliders { get; private set; }
        protected EnemyData Data => enemyData;

        private IDefenseProvider _defenseProvider;

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

        /// <inheritdoc/>
        public bool IsStunned => _isStunned;

        // ── Invulnerability ───────────────────────────────────────────────

        private bool _isInvulnerable;

        /// <inheritdoc/>
        public bool IsInvulnerable => _isInvulnerable;

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

            _currentHealth = enemyData.maxHealth;
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
            Debug.Log($"[EnemyBase] TakeDamage: {damage.amount:F1} dmg → HP: {_currentHealth:F1}/{enemyData.maxHealth}");

            // Pressure fills faster on punish hits
            float pressureAmount = damage.amount * (damage.isPunishDamage ? 2f : 1f);
            AddStun(pressureAmount);

            ApplyKnockback(damage.knockbackForce);
            ApplyLaunch(damage.launchForce);

            OnDamaged(damage);

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

            Vector2 reducedForce = force * (1f - enemyData.knockbackResistance);
            Rb.AddForce(reducedForce, ForceMode2D.Impulse);

            if (_knockbackCoroutine != null)
                StopCoroutine(_knockbackCoroutine);
            _knockbackCoroutine = StartCoroutine(KnockbackRecovery());
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

            Vector2 reducedForce = force * (1f - enemyData.knockbackResistance);
            Rb.AddForce(reducedForce, ForceMode2D.Impulse);
        }

        // ── Stun / Recovery ───────────────────────────────────────────────

        private IEnumerator StunRoutine()
        {
            _isStunned = true;
            OnStunned();

            yield return new WaitForSeconds(enemyData.stunDuration);

            _isStunned = false;
            _pressureFill = 0f;

            OnRecovery();

            // Post-stun invulnerability blink
            yield return InvulnerabilityBlink();
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

        // ── Virtual Hooks ─────────────────────────────────────────────────

        /// <summary>Called after damage is applied but before death check.</summary>
        protected virtual void OnDamaged(DamagePacket damage) { }

        /// <summary>Called when pressure meter fills and stun begins.</summary>
        protected virtual void OnStunned() { }

        /// <summary>Called when the enemy dies (health reaches zero).</summary>
        protected virtual void OnDeath() { }

        /// <summary>Called when stun ends and recovery begins (before invulnerability blink).</summary>
        protected virtual void OnRecovery() { }
    }
}
