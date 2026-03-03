using System;
using System.Collections.Generic;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Combat
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
            // Fresh set at the start of each swing — tracks by IDamageable
            // to prevent double-damage from entities with multiple colliders
            _hitThisActivation.Clear();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var target = other.GetComponentInParent<IDamageable>();
            if (target == null) return;
            if (!_hitThisActivation.Add(target)) return;

            OnHitDetected?.Invoke(target, other.ClosestPoint(transform.position));
        }
    }
}
