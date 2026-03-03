using TomatoFighters.Shared.Data;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Context passed to <see cref="DefenseBonus.Apply"/> after a successful defense.
    /// Contains everything a bonus needs to apply character-specific effects.
    /// </summary>
    public struct DefenseContext
    {
        /// <summary>The entity that successfully defended.</summary>
        public GameObject defender;

        /// <summary>The entity that attacked (may be null for projectiles).</summary>
        public GameObject attacker;

        /// <summary>World-space point where the attack landed.</summary>
        public Vector2 hitPoint;

        /// <summary>The incoming damage packet that was defended against.</summary>
        public DamagePacket incomingPacket;
    }
}
