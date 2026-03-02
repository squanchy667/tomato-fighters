using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Tuning data for character movement. One per character archetype.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMovementConfig", menuName = "TomatoFighters/Combat/Movement Config")]
    public class MovementConfig : ScriptableObject
    {
        [Header("Horizontal Movement")]

        /// <summary>Max horizontal speed in units/sec.</summary>
        [Tooltip("Max horizontal speed in units/sec")]
        public float moveSpeed = 8f;

        /// <summary>Ground acceleration in units/sec² toward target speed.</summary>
        [Tooltip("Ground acceleration (units/sec² toward target speed)")]
        public float groundAcceleration = 60f;

        /// <summary>Air acceleration in units/sec² — lower for floatier air control.</summary>
        [Tooltip("Air acceleration (units/sec² — lower for floatier air control)")]
        public float airAcceleration = 30f;

        [Header("Jump")]

        /// <summary>Vertical velocity applied on jump.</summary>
        [Tooltip("Vertical velocity applied on jump")]
        public float jumpForce = 14f;

        /// <summary>Seconds after leaving ground where jump is still allowed.</summary>
        [Tooltip("Seconds after leaving ground where jump is still allowed")]
        public float coyoteTime = 0.1f;

        /// <summary>Seconds before landing where jump input is buffered.</summary>
        [Tooltip("Seconds before landing where jump input is buffered")]
        public float jumpBufferTime = 0.12f;

        [Header("Dash")]

        /// <summary>Dash velocity in units/sec.</summary>
        [Tooltip("Dash velocity in units/sec")]
        public float dashSpeed = 20f;

        /// <summary>How long the dash lasts in seconds.</summary>
        [Tooltip("How long the dash lasts in seconds")]
        public float dashDuration = 0.15f;

        /// <summary>Cooldown between dashes in seconds.</summary>
        [Tooltip("Cooldown between dashes in seconds")]
        public float dashCooldown = 0.6f;

        /// <summary>Whether dashing grants invincibility frames.</summary>
        [Tooltip("Whether dashing grants invincibility frames")]
        public bool dashHasIFrames = true;

        [Header("Gravity")]

        /// <summary>Default Rigidbody2D gravity scale.</summary>
        [Tooltip("Default Rigidbody2D gravity scale")]
        public float defaultGravityScale = 3f;

        /// <summary>Gravity scale while falling — higher means snappier fall.</summary>
        [Tooltip("Gravity scale while falling (higher = snappier fall)")]
        public float fallGravityMultiplier = 5f;
    }
}
