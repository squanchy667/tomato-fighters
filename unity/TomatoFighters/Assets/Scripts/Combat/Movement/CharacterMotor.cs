using System;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Physics-based character movement using Rigidbody2D.
    /// Handles horizontal movement, jumping, and dashing.
    /// Fires local C# events for combat system integration.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class CharacterMotor : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private MovementConfig config;
        [SerializeField] private GroundDetector groundDetector;
        [SerializeField] private CharacterType characterType;

        private Rigidbody2D rb;
        private MovementStateMachine stateMachine;

        private float moveInput;
        private bool facingRight = true;
        private float coyoteTimeCounter;
        private float jumpBufferCounter;
        private float dashTimeRemaining;
        private float dashCooldownRemaining;
        private Vector2 dashDirection;
        private bool dashInvulnerable;

        // Optional buff integration — wired at runtime, avoids singleton
        private IBuffProvider buffProvider;

        /// <summary>Fired when the character jumps. Args: characterType, isAirborne.</summary>
        public event Action<CharacterType, bool> Jumped;

        /// <summary>Fired when the character dashes. Args: characterType, direction, hasIFrames.</summary>
        public event Action<CharacterType, Vector2, bool> Dashed;

        /// <summary>Whether the character is currently grounded.</summary>
        public bool IsGrounded => groundDetector != null && groundDetector.IsGrounded;

        /// <summary>Whether the character is currently dashing.</summary>
        public bool IsDashing => stateMachine.CurrentState == MovementState.Dashing;

        /// <summary>Whether the character has i-frames from dashing.</summary>
        public bool IsDashInvulnerable => dashInvulnerable;

        /// <summary>Current facing direction. True = right, false = left.</summary>
        public bool FacingRight => facingRight;

        /// <summary>Current movement state.</summary>
        public MovementState CurrentState => stateMachine.CurrentState;

        /// <summary>Which character archetype this motor belongs to.</summary>
        public CharacterType CharacterType => characterType;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stateMachine = new MovementStateMachine();
            rb.gravityScale = config.defaultGravityScale;
        }

        private void FixedUpdate()
        {
            UpdateTimers();
            UpdateState();
            ApplyMovement();
            ApplyGravityModifiers();
        }

        /// <summary>Set horizontal movement input (-1 to 1).</summary>
        public void SetMoveInput(float direction)
        {
            moveInput = Mathf.Clamp(direction, -1f, 1f);
        }

        /// <summary>Request a jump. Buffered if pressed slightly before landing.</summary>
        public void RequestJump()
        {
            jumpBufferCounter = config.jumpBufferTime;
        }

        /// <summary>
        /// Request a dash in the given direction.
        /// Returns true if the dash started, false if on cooldown or state doesn't allow it.
        /// </summary>
        public bool RequestDash(Vector2 direction)
        {
            if (dashCooldownRemaining > 0f) return false;
            if (!stateMachine.CanDash()) return false;

            // Default to facing direction if no directional input
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = facingRight ? Vector2.right : Vector2.left;
            }

            dashDirection = direction.normalized;
            dashTimeRemaining = config.dashDuration;
            dashCooldownRemaining = config.dashCooldown;
            dashInvulnerable = config.dashHasIFrames;

            stateMachine.TransitionTo(MovementState.Dashing);
            Dashed?.Invoke(characterType, dashDirection, dashInvulnerable);

            return true;
        }

        /// <summary>
        /// Wire the buff provider for speed multipliers.
        /// Called during character setup to avoid singleton pattern.
        /// </summary>
        public void SetBuffProvider(IBuffProvider provider)
        {
            buffProvider = provider;
        }

        private void UpdateTimers()
        {
            float dt = Time.fixedDeltaTime;

            if (dashCooldownRemaining > 0f)
                dashCooldownRemaining -= dt;

            if (jumpBufferCounter > 0f)
                jumpBufferCounter -= dt;

            // Coyote time: brief grace period after leaving ground
            if (IsGrounded)
            {
                coyoteTimeCounter = config.coyoteTime;
            }
            else
            {
                coyoteTimeCounter -= dt;
            }
        }

        private void UpdateState()
        {
            if (stateMachine.CurrentState == MovementState.Dashing)
            {
                dashTimeRemaining -= Time.fixedDeltaTime;
                if (dashTimeRemaining <= 0f)
                {
                    dashInvulnerable = false;
                    stateMachine.TransitionTo(IsGrounded ? MovementState.Grounded : MovementState.Airborne);
                }
                return;
            }

            if (IsGrounded)
            {
                stateMachine.TransitionTo(MovementState.Grounded);
                TryConsumeJumpBuffer();
            }
            else
            {
                stateMachine.TransitionTo(MovementState.Airborne);
                TryCoyoteJump();
            }
        }

        private void TryConsumeJumpBuffer()
        {
            if (jumpBufferCounter > 0f)
            {
                ExecuteJump();
                jumpBufferCounter = 0f;
            }
        }

        private void TryCoyoteJump()
        {
            if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
            {
                ExecuteJump();
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
            }
        }

        private void ExecuteJump()
        {
            // Zero out vertical velocity before applying jump force for consistent height
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            rb.velocity = new Vector2(rb.velocity.x, config.jumpForce);
            stateMachine.TransitionTo(MovementState.Airborne);

            Jumped?.Invoke(characterType, true);
        }

        private void ApplyMovement()
        {
            if (!stateMachine.CanMove())
            {
                // During dash, override velocity entirely
                rb.velocity = dashDirection * config.dashSpeed;
                return;
            }

            float speedMultiplier = buffProvider?.GetSpeedMultiplier() ?? 1f;
            float targetSpeed = moveInput * config.moveSpeed * speedMultiplier;

            float accel = IsGrounded ? config.groundAcceleration : config.airAcceleration;
            float newX = Mathf.MoveTowards(rb.velocity.x, targetSpeed, accel * Time.fixedDeltaTime);
            rb.velocity = new Vector2(newX, rb.velocity.y);

            UpdateFacing();
        }

        private void ApplyGravityModifiers()
        {
            if (stateMachine.CurrentState == MovementState.Dashing) return;

            // Higher gravity while falling for snappier game feel
            rb.gravityScale = rb.velocity.y < 0f
                ? config.fallGravityMultiplier
                : config.defaultGravityScale;
        }

        private void UpdateFacing()
        {
            if (moveInput > 0.01f && !facingRight)
                Flip();
            else if (moveInput < -0.01f && facingRight)
                Flip();
        }

        private void Flip()
        {
            facingRight = !facingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1f;
            transform.localScale = scale;
        }
    }
}
