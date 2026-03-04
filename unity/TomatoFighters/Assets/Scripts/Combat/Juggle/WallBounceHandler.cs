using System;
using TomatoFighters.Shared.Data;
using UnityEngine;

namespace TomatoFighters.Combat.Juggle
{
    /// <summary>
    /// Detects wall collisions during knockback and reflects velocity for a bounce effect.
    /// Unlimited bounces per combo. Applies minor damage with no pressure fill.
    /// Uses velocity magnitude as a proxy for knockback state — only bounces above
    /// <see cref="JuggleConfig.minBounceVelocity"/> threshold.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class WallBounceHandler : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private JuggleConfig config;

        [Header("Wall Detection")]
        [Tooltip("Layer mask for walls that can trigger bounces.")]
        [SerializeField] private LayerMask wallLayer;

        private Rigidbody2D _rb;
        private JuggleSystem _juggleSystem;

        /// <summary>
        /// Fired when a wall bounce occurs. Args: bounce position, damage dealt, reflected velocity.
        /// JuggleSystem subscribes to forward through IJuggleTarget.OnWallBounced.
        /// </summary>
        public event Action<Vector2, float, Vector2> BounceDetected;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _juggleSystem = GetComponent<JuggleSystem>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsWallCollision(collision)) return;

            // Only bounce if moving fast enough (knockback proxy)
            float speed = _rb.linearVelocity.magnitude;
            if (speed < config.minBounceVelocity) return;

            // Also check explicit knockback flag if JuggleSystem is present
            if (_juggleSystem != null && !_juggleSystem.IsInKnockback) return;

            Vector2 contactNormal = collision.GetContact(0).normal;
            Vector2 currentVelocity = _rb.linearVelocity;
            Vector2 reflected = Vector2.Reflect(currentVelocity, contactNormal);

            // Apply velocity retention (energy loss on bounce)
            reflected *= config.bounceVelocityRetention;
            _rb.linearVelocity = reflected;

            Vector2 bouncePos = collision.GetContact(0).point;
            float damage = config.wallBounceDamage;

            Debug.Log($"[WallBounce] Bounce at {bouncePos}, speed={speed:F1}, " +
                      $"reflected={reflected}, damage={damage:F1}");

            BounceDetected?.Invoke(bouncePos, damage, reflected);
        }

        private bool IsWallCollision(Collision2D collision)
        {
            // Check against wall layer mask
            return (wallLayer.value & (1 << collision.gameObject.layer)) != 0;
        }
    }
}
