using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Shared.Interfaces
{
    /// <summary>
    /// Combat pillar defines this contract; World pillar implements it on enemies and destructibles.
    /// Any entity that can receive damage implements this interface.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Resolve an incoming attack against this entity's current defense state.
        /// Called by HitboxManager before deciding whether to apply damage.
        /// </summary>
        /// <param name="attackerPosition">World-space position of the attacker.</param>
        /// <param name="isUnstoppable">Whether the incoming attack has Unstoppable telegraph.</param>
        /// <returns>How the entity responded: Hit, Deflected, Clashed, or Dodged.</returns>
        DamageResponse ResolveIncoming(Vector2 attackerPosition, bool isUnstoppable);

        /// <summary>Process an incoming damage packet (apply damage, knockback, stun, etc.).</summary>
        void TakeDamage(DamagePacket damage);

        /// <summary>Current health points.</summary>
        float CurrentHealth { get; }

        /// <summary>Maximum health points.</summary>
        float MaxHealth { get; }

        /// <summary>Fill the hidden stun meter. When full, the entity becomes stunned.</summary>
        void AddStun(float amount);

        /// <summary>Whether the entity is currently stunned and vulnerable.</summary>
        bool IsStunned { get; }

        /// <summary>Apply a horizontal knockback force via Rigidbody2D.</summary>
        void ApplyKnockback(Vector2 force);

        /// <summary>Apply a vertical launch force via Rigidbody2D.</summary>
        void ApplyLaunch(Vector2 force);

        /// <summary>Whether the entity is currently invulnerable (i-frames, shields, etc.).</summary>
        bool IsInvulnerable { get; }
    }
}
