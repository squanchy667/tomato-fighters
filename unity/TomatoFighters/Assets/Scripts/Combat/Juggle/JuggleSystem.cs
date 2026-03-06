using System;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Combat.Juggle
{
    /// <summary>
    /// Tracks airborne state for damageable entities using belt-scroll simulated height.
    /// Manages the full juggle lifecycle: Launch → Airborne → Falling → OTG → TechRecover → Grounded.
    /// Queries <see cref="IBuffProvider"/> for Gale gravity multiplier to extend airtime.
    /// Attach to any entity that can be launched (players, enemies).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class JuggleSystem : MonoBehaviour, IJuggleTarget
    {
        [Header("Configuration")]
        [SerializeField] private JuggleConfig config;

        [Header("Visual")]
        [Tooltip("Child transform that moves up/down to show simulated air height.")]
        [SerializeField] private Transform spriteTransform;

        // ── Cached Components ───────────────────────────────────────────
        private Rigidbody2D _rb;
        private IBuffProvider _buffProvider;
        private WallBounceHandler _wallBounceHandler;

        // ── Juggle State ────────────────────────────────────────────────
        private JuggleState _state = JuggleState.Grounded;
        private float _airHeight;
        private float _airVelocity;
        private float _stateTimer;

        // ── Knockback Tracking ──────────────────────────────────────────
        private bool _isInKnockback;
        private float _knockbackTimer;

        // ── Deferred Invulnerability ────────────────────────────────────
        private Action _pendingLandCallback;

        // ── IJuggleTarget Properties ────────────────────────────────────

        /// <inheritdoc/>
        public bool IsAirborne => _state == JuggleState.Airborne || _state == JuggleState.Falling;

        /// <inheritdoc/>
        public JuggleState CurrentJuggleState => _state;

        /// <inheritdoc/>
        public float AirHeight => _airHeight;

        /// <inheritdoc/>
        public bool IsInKnockback => _isInKnockback;

        /// <inheritdoc/>
        public bool IsInOTG => _state == JuggleState.OTG;

        // ── Events ──────────────────────────────────────────────────────

        /// <inheritdoc/>
        public event Action OnLanded;

        /// <inheritdoc/>
        public event Action OnOTGEnd;

        /// <inheritdoc/>
        public event Action OnTechRecoverStart;

        /// <inheritdoc/>
        public event Action OnTechRecoverEnd;

        /// <inheritdoc/>
        public event Action<Vector2, float> OnWallBounced;

        /// <inheritdoc/>
        public event Action OnBlockedHit;

        // ── Unity Lifecycle ─────────────────────────────────────────────

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _wallBounceHandler = GetComponent<WallBounceHandler>();
        }

        private void OnEnable()
        {
            if (_wallBounceHandler != null)
                _wallBounceHandler.BounceDetected += HandleWallBounce;
        }

        private void OnDisable()
        {
            if (_wallBounceHandler != null)
                _wallBounceHandler.BounceDetected -= HandleWallBounce;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            UpdateKnockback(dt);
            UpdateJuggle(dt);
            UpdateSpriteOffset();
        }

        // ── Public API ──────────────────────────────────────────────────

        /// <summary>
        /// Wire the buff provider for Gale gravity multiplier.
        /// Called during entity setup to avoid singleton pattern.
        /// </summary>
        public void SetBuffProvider(IBuffProvider provider)
        {
            _buffProvider = provider;
        }

        /// <inheritdoc/>
        public void Launch(Vector2 force)
        {
            float upwardSpeed = Mathf.Abs(force.y);
            if (upwardSpeed < config.minLaunchSpeed) return;

            // Horizontal component applied to Rigidbody2D for ground-plane travel
            if (Mathf.Abs(force.x) > 0.01f)
            {
                _rb.AddForce(new Vector2(force.x, 0f), ForceMode2D.Impulse);
            }

            _airVelocity = upwardSpeed;
            _airHeight = 0.01f; // Nudge off ground
            TransitionTo(JuggleState.Airborne);

            Debug.Log($"[JuggleSystem] Launched: upSpeed={upwardSpeed:F1}, hForce={force.x:F1}");
        }

        /// <inheritdoc/>
        public void NotifyKnockback(Vector2 force)
        {
            _isInKnockback = true;
            _knockbackTimer = config.knockbackRecoveryTime;
        }

        /// <inheritdoc/>
        public void NotifyBlockedHit()
        {
            OnBlockedHit?.Invoke();
            Debug.Log("[JuggleSystem] Blocked hit — OTG/TechRecover gating");
        }

        /// <inheritdoc/>
        public void RequestInvulnerabilityOnLanding(Action onLanded)
        {
            if (!IsAirborne)
            {
                // Already grounded — fire immediately
                onLanded?.Invoke();
                return;
            }

            _pendingLandCallback = onLanded;
        }

        // ── Knockback Update ────────────────────────────────────────────

        private void UpdateKnockback(float dt)
        {
            if (!_isInKnockback) return;

            _knockbackTimer -= dt;
            if (_knockbackTimer <= 0f)
            {
                _isInKnockback = false;

                // Only zero horizontal velocity if grounded (airborne entities keep momentum)
                if (!IsAirborne)
                {
                    _rb.linearVelocity = Vector2.zero;
                }
            }
        }

        // ── Juggle State Machine ────────────────────────────────────────

        private void UpdateJuggle(float dt)
        {
            switch (_state)
            {
                case JuggleState.Grounded:
                    // Nothing to simulate
                    break;

                case JuggleState.Airborne:
                    SimulateAirPhysics(dt);
                    if (_airVelocity <= 0f)
                    {
                        TransitionTo(JuggleState.Falling);
                    }
                    break;

                case JuggleState.Falling:
                    SimulateAirPhysics(dt);
                    if (_airHeight <= 0f)
                    {
                        Land();
                    }
                    break;

                case JuggleState.OTG:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0f)
                    {
                        TransitionTo(JuggleState.TechRecover);
                        _stateTimer = config.techRecoverDuration;
                        OnTechRecoverStart?.Invoke();
                    }
                    break;

                case JuggleState.TechRecover:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0f)
                    {
                        TransitionTo(JuggleState.Grounded);
                        OnTechRecoverEnd?.Invoke();
                    }
                    break;
            }
        }

        private void SimulateAirPhysics(float dt)
        {
            float gravityMult = _buffProvider?.GetJuggleGravityMultiplier() ?? 1f;
            float gravity = config.juggleGravity * gravityMult;

            _airVelocity -= gravity * dt;

            // Clamp fall speed
            if (_airVelocity < -config.terminalFallSpeed)
                _airVelocity = -config.terminalFallSpeed;

            _airHeight += _airVelocity * dt;

            // Clamp to ground
            if (_airHeight < 0f)
                _airHeight = 0f;
        }

        private void Land()
        {
            _airHeight = 0f;
            _airVelocity = 0f;

            // Stop horizontal momentum on landing
            _rb.linearVelocity = Vector2.zero;
            _isInKnockback = false;

            // Transition to OTG (knocked down on the ground)
            TransitionTo(JuggleState.OTG);
            _stateTimer = config.otgDuration;

            OnLanded?.Invoke();
            Debug.Log("[JuggleSystem] Landed → OTG");

            // Fire deferred invulnerability callback if pending
            if (_pendingLandCallback != null)
            {
                _pendingLandCallback.Invoke();
                _pendingLandCallback = null;
            }
        }

        /// <summary>
        /// Re-launch an entity that is already airborne or in OTG (juggle extension).
        /// Allows combos to keep enemies airborne with additional launch attacks.
        /// </summary>
        public void Relaunch(Vector2 force)
        {
            float upwardSpeed = Mathf.Abs(force.y);
            if (upwardSpeed < config.minLaunchSpeed) return;

            // Add horizontal force for continued ground-plane travel
            if (Mathf.Abs(force.x) > 0.01f)
            {
                _rb.AddForce(new Vector2(force.x, 0f), ForceMode2D.Impulse);
            }

            _airVelocity = upwardSpeed;
            if (_airHeight < 0.01f)
                _airHeight = 0.01f;

            TransitionTo(JuggleState.Airborne);
            Debug.Log($"[JuggleSystem] Relaunched: upSpeed={upwardSpeed:F1}");
        }

        private void TransitionTo(JuggleState newState)
        {
            if (_state == newState) return;
            var oldState = _state;
            _state = newState;

            if (oldState == JuggleState.OTG)
                OnOTGEnd?.Invoke();
        }

        private void UpdateSpriteOffset()
        {
            if (spriteTransform == null) return;
            spriteTransform.localPosition = new Vector3(0f, _airHeight, 0f);
        }

        // ── Wall Bounce Integration ─────────────────────────────────────

        private void HandleWallBounce(Vector2 position, float damage, Vector2 reflectedVelocity)
        {
            OnWallBounced?.Invoke(position, damage);
        }
    }
}
