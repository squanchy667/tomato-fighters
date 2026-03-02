using System;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Belt-scroll character movement using Rigidbody2D.
    /// Handles XY ground plane movement, simulated jump height with sprite offset,
    /// and dashing. No physics gravity — jump arc is computed manually.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class CharacterMotor : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private MovementConfig config;
        [SerializeField] private CharacterType characterType;

        [Header("Visual")]
        [SerializeField] private Transform spriteTransform;

        private Rigidbody2D rb;
        private MovementStateMachine stateMachine;

        private Vector2 moveInput;
        private bool facingRight = true;
        private float jumpHeight;
        private float jumpVelocity;
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

        /// <summary>Fired when the character lands from a jump.</summary>
        public event Action<CharacterType> Landed;

        /// <summary>Whether the character is currently on the ground (not jumping).</summary>
        public bool IsGrounded => jumpHeight <= 0f && jumpVelocity <= 0f;

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

        /// <summary>Current simulated jump height above the ground plane.</summary>
        public float JumpHeight => jumpHeight;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            stateMachine = new MovementStateMachine();
            rb.gravityScale = 0f;
        }

        private void FixedUpdate()
        {
            UpdateTimers();
            UpdateJump();
            UpdateState();
            ApplyMovement();
            UpdateSpriteOffset();
        }

        /// <summary>Set ground plane movement input (X = horizontal, Y = depth).</summary>
        public void SetMoveInput(Vector2 input)
        {
            moveInput = new Vector2(
                Mathf.Clamp(input.x, -1f, 1f),
                Mathf.Clamp(input.y, -1f, 1f));
        }

        /// <summary>Request a jump. Buffered if pressed slightly before landing.</summary>
        public void RequestJump()
        {
            jumpBufferCounter = config.jumpBufferTime;
        }

        /// <summary>
        /// Request a dash in the given direction on the ground plane.
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
        }

        private void UpdateJump()
        {
            if (jumpHeight <= 0f && jumpVelocity <= 0f)
            {
                // On the ground — check for buffered jump
                if (jumpBufferCounter > 0f)
                {
                    ExecuteJump();
                    jumpBufferCounter = 0f;
                }
                return;
            }

            // Simulate jump arc
            float dt = Time.fixedDeltaTime;
            jumpVelocity -= config.jumpGravity * dt;
            jumpHeight += jumpVelocity * dt;

            // Land
            if (jumpHeight <= 0f)
            {
                jumpHeight = 0f;
                jumpVelocity = 0f;
                Landed?.Invoke(characterType);
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
            }
            else
            {
                stateMachine.TransitionTo(MovementState.Airborne);
            }
        }

        private void ExecuteJump()
        {
            if (stateMachine.CurrentState == MovementState.Dashing) return;

            jumpVelocity = config.jumpForce;
            jumpHeight = 0.01f; // nudge off ground
            stateMachine.TransitionTo(MovementState.Airborne);

            Jumped?.Invoke(characterType, true);
        }

        private void ApplyMovement()
        {
            if (!stateMachine.CanMove())
            {
                // During dash, override velocity entirely on ground plane
                rb.linearVelocity = dashDirection * config.dashSpeed;
                return;
            }

            float speedMultiplier = buffProvider?.GetSpeedMultiplier() ?? 1f;
            float targetX = moveInput.x * config.moveSpeed * speedMultiplier;
            float targetY = moveInput.y * config.depthSpeed * speedMultiplier;

            float accel = IsGrounded ? config.groundAcceleration : config.airAcceleration;
            float dt = Time.fixedDeltaTime;

            float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, accel * dt);
            float newY = Mathf.MoveTowards(rb.linearVelocity.y, targetY, accel * dt);
            rb.linearVelocity = new Vector2(newX, newY);

            UpdateFacing();
        }

        private void UpdateSpriteOffset()
        {
            if (spriteTransform == null) return;
            spriteTransform.localPosition = new Vector3(0f, jumpHeight, 0f);
        }

        private void UpdateFacing()
        {
            if (moveInput.x > 0.01f && !facingRight)
                Flip();
            else if (moveInput.x < -0.01f && facingRight)
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
