using System.Collections;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Stub <see cref="IDamageable"/> implementation on the player for bidirectional damage testing.
    /// Logs damage, flashes the sprite red, and applies knockback via Rigidbody2D.
    /// Delegates defense resolution to <see cref="DefenseSystem"/> when assigned.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerDamageable : MonoBehaviour, IDamageable
    {
        [Header("Stub Health")]
        [SerializeField] private float maxHealth = 100f;

        [Header("Defense")]
        [SerializeField] private DefenseSystem defenseSystem;

        [Header("Hit Flash")]
        [SerializeField] private Color flashColor = Color.red;
        [SerializeField] private float flashDuration = 0.15f;

        private float _currentHealth;
        private Rigidbody2D _rb;
        private SpriteRenderer _sprite;
        private Coroutine _flashRoutine;

        /// <inheritdoc/>
        public float CurrentHealth => _currentHealth;

        /// <inheritdoc/>
        public float MaxHealth => maxHealth;

        /// <inheritdoc/>
        public bool IsStunned => false;

        /// <inheritdoc/>
        public bool IsInvulnerable => false;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            // Use child SpriteRenderer (the visible one) — root SR has no sprite assigned
            _sprite = GetComponentInChildren<SpriteRenderer>();
            _currentHealth = maxHealth;
        }

        /// <inheritdoc/>
        public DamageResponse ResolveIncoming(Vector2 attackerPosition, bool isUnstoppable)
        {
            if (defenseSystem == null)
            {
                Debug.LogWarning("[PlayerDamageable] defenseSystem is NULL — cannot resolve defense. Always returning Hit.");
                return DamageResponse.Hit;
            }

            var response = defenseSystem.Resolve(attackerPosition, isUnstoppable);
            Debug.Log($"[PlayerDamageable] ResolveIncoming → {response} (state={defenseSystem.CurrentState}, unstoppable={isUnstoppable})");
            return response;
        }

        /// <inheritdoc/>
        public void TakeDamage(DamagePacket damage)
        {
            _currentHealth -= damage.amount;

            Debug.Log(
                $"[PlayerDamageable] Took {damage.amount:F1} {damage.type} damage. " +
                $"HP: {_currentHealth:F1}/{maxHealth}");

            ApplyKnockback(damage.knockbackForce);
            ApplyLaunch(damage.launchForce);

            Flash();

            if (_currentHealth <= 0f)
            {
                _currentHealth = 0f;
                Debug.Log("[PlayerDamageable] Player would be dead (stub — no death logic yet).");
            }
        }

        /// <inheritdoc/>
        public void AddStun(float amount)
        {
            // TODO: Player stun — requires input lock, combo cancel, defense reset, UI. Separate task.
        }

        /// <inheritdoc/>
        public void ApplyKnockback(Vector2 force)
        {
            if (force == Vector2.zero) return;
            _rb.AddForce(force, ForceMode2D.Impulse);
        }

        /// <inheritdoc/>
        public void ApplyLaunch(Vector2 force)
        {
            if (force == Vector2.zero) return;
            _rb.AddForce(force, ForceMode2D.Impulse);
        }

        private void Flash()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            Color originalColor = _sprite.color;
            _sprite.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            _sprite.color = originalColor;
            _flashRoutine = null;
        }
    }
}
