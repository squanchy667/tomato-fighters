using System.Collections.Generic;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Shared.Components
{
    /// <summary>
    /// Tracks per-activation clash immunity. When entity A clashes with entity B,
    /// B registers A as immune on its own ClashTracker. When B's hitbox later fires,
    /// B's hit resolver checks HasClashImmunity(A) and skips the hit.
    /// </summary>
    public class ClashTracker : MonoBehaviour
    {
        private readonly HashSet<IDamageable> _clashImmune = new();

        /// <summary>
        /// Register a target as immune to this entity's current attack due to a clash.
        /// </summary>
        public void AddClashImmunity(IDamageable target) => _clashImmune.Add(target);

        /// <summary>
        /// Check if a target has clash immunity against this entity's attack.
        /// </summary>
        public bool HasClashImmunity(IDamageable target) => _clashImmune.Contains(target);

        /// <summary>
        /// Clear all clash immunities. Call when a new attack cycle begins.
        /// </summary>
        public void ClearImmunities() => _clashImmune.Clear();
    }
}
