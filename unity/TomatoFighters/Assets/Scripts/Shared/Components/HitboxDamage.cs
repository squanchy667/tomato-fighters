using System;
using System.Collections.Generic;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Shared.Components
{
    /// <summary>
    /// Placed on each hitbox child GameObject (e.g. Hitbox_Jab, Hitbox_Sweep).
    /// Detects trigger collisions with <see cref="IDamageable"/> targets and reports them.
    /// Uses a HashSet to prevent double-hits per activation while allowing multi-target hits.
    /// Pure detection — does NOT resolve damage or defense state.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HitboxDamage : MonoBehaviour
    {
        /// <summary>
        /// Fired when a new valid target is detected during this activation.
        /// Args: (target, hitPoint).
        /// </summary>
        public event Action<IDamageable, Vector2> OnHitDetected;

        private readonly HashSet<IDamageable> _hitThisActivation = new();

        private void OnEnable()
        {
            _hitThisActivation.Clear();
            Debug.Log($"[HitboxDamage] '{name}' ENABLED — layer={gameObject.layer}, subscribers={(OnHitDetected != null ? OnHitDetected.GetInvocationList().Length : 0)}");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log($"[HitboxDamage] '{name}' OnTriggerEnter2D with '{other.name}' (layer={other.gameObject.layer})");
            ProcessTrigger(other);
        }

        // Fallback: OnTriggerEnter2D may not fire when a disabled trigger is
        // re-enabled while already overlapping. Stay fires on subsequent frames.
        private void OnTriggerStay2D(Collider2D other)
        {
            ProcessTrigger(other);
        }

        private void ProcessTrigger(Collider2D other)
        {
            var target = other.GetComponentInParent<IDamageable>();
            if (target == null)
            {
                Debug.Log($"[HitboxDamage] '{name}' — no IDamageable on '{other.name}' or parents");
                return;
            }
            if (!_hitThisActivation.Add(target)) return; // Already hit this activation

            Debug.Log($"[HitboxDamage] '{name}' HIT '{other.name}' → firing OnHitDetected (subscribers={(OnHitDetected != null ? OnHitDetected.GetInvocationList().Length : 0)})");
            OnHitDetected?.Invoke(target, other.ClosestPoint(transform.position));
        }
    }
}
