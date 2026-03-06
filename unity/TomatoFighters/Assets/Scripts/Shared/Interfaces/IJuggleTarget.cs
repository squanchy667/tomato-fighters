using System;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Shared.Interfaces
{
    /// <summary>
    /// Cross-pillar interface for the juggle system. Implemented by JuggleSystem (Combat),
    /// queried by EnemyBase (World) via GetComponent. Enables airborne tracking, wall bounce
    /// integration, and post-stun invulnerable landing without cross-pillar imports.
    /// </summary>
    public interface IJuggleTarget
    {
        /// <summary>Whether the entity is currently airborne (Airborne or Falling state).</summary>
        bool IsAirborne { get; }

        /// <summary>Current juggle state (Grounded, Airborne, Falling, OTG, TechRecover).</summary>
        JuggleState CurrentJuggleState { get; }

        /// <summary>Current simulated air height above the ground plane.</summary>
        float AirHeight { get; }

        /// <summary>Whether the entity is in knockback (moving from a hit). Used by wall bounce.</summary>
        bool IsInKnockback { get; }

        /// <summary>Whether the entity is in OTG state (knocked down, hittable by OTG attacks).</summary>
        bool IsInOTG { get; }

        /// <summary>
        /// Launch the entity into the air. Y component of force becomes upward velocity;
        /// X component is applied to Rigidbody2D for horizontal travel.
        /// </summary>
        void Launch(Vector2 force);

        /// <summary>
        /// Notify the juggle system that a knockback impulse was applied.
        /// Enables wall bounce detection for this knockback.
        /// </summary>
        void NotifyKnockback(Vector2 force);

        /// <summary>
        /// Request that invulnerability blink starts when the entity lands.
        /// Used by stun recovery to defer invulnerability until grounded.
        /// </summary>
        /// <param name="onLanded">Callback fired when the entity lands (start invuln blink).</param>
        void RequestInvulnerabilityOnLanding(Action onLanded);

        /// <summary>Fired when the entity transitions from airborne to grounded/OTG.</summary>
        event Action OnLanded;

        /// <summary>Fired when OTG state ends for any reason (tech recover, relaunch, etc.).</summary>
        event Action OnOTGEnd;

        /// <summary>Fired when OTG state ends and the entity begins tech recovery.</summary>
        event Action OnTechRecoverStart;

        /// <summary>Fired when tech recovery completes and the entity returns to grounded.</summary>
        event Action OnTechRecoverEnd;

        /// <summary>Fired when a wall bounce occurs. Provides bounce position and minor damage dealt.</summary>
        event Action<Vector2, float> OnWallBounced;

        /// <summary>
        /// Notify the juggle system that a hit was blocked by OTG/TechRecover gating.
        /// The entity handles its own visual feedback (e.g. "immune" indicator).
        /// </summary>
        void NotifyBlockedHit();

        /// <summary>Fired when a hit is blocked by OTG/TechRecover gating. Used for "immune" visual feedback.</summary>
        event Action OnBlockedHit;
    }
}
