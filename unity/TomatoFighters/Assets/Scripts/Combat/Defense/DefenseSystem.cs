using System;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Per-entity defense resolution component. Tracks defense state via event
    /// subscriptions to <see cref="CharacterMotor"/> and <see cref="ComboController"/>,
    /// manages window timers in <c>FixedUpdate</c>, and delegates resolution to
    /// <see cref="DefenseResolver"/>.
    /// <para>
    /// Both player and enemies can have a DefenseSystem. Enemy DefenseConfig SOs
    /// can have zeroed windows (no defense) or real windows (boss that can clash).
    /// </para>
    /// <para>
    /// Implements <see cref="IDefenseProvider"/> so World pillar entities (EnemyBase)
    /// can reference it through the Shared interface without cross-pillar imports.
    /// </para>
    /// </summary>
    public class DefenseSystem : MonoBehaviour, IDefenseProvider
    {
        [Header("Configuration")]
        [SerializeField] private DefenseConfig config;

        [Header("References")]
        [SerializeField] private CharacterMotor motor;
        [SerializeField] private ComboController comboController;

        /// <summary>Fired on a successful deflect.</summary>
        public event Action<DeflectEventData> OnDeflect;

        /// <summary>Fired on a successful clash.</summary>
        public event Action<ClashEventData> OnClash;

        /// <summary>Fired on a successful dodge.</summary>
        public event Action<DodgeEventData> OnDodge;

        private readonly DefenseResolver _resolver = new();

        private DefenseState _currentState = DefenseState.None;
        private Vector2 _actionDirection;
        private float _windowTimer;

        // Clash has a start delay before the window opens
        private bool _isClashPending;
        private float _clashDelayRemaining;
        private float _pendingClashDuration;

        /// <summary>Current defense posture.</summary>
        public DefenseState CurrentState => _currentState;

        /// <inheritdoc/>
        public bool IsInClashWindow => _currentState == DefenseState.HeavyStartup;

        /// <summary>The assigned defense configuration.</summary>
        public DefenseConfig Config => config;

        private void OnEnable()
        {
            if (motor != null)
            {
                motor.Dashed += HandleDashStarted;
            }

            if (comboController != null)
            {
                comboController.AttackStarted += HandleAttackStarted;
            }
        }

        private void OnDisable()
        {
            if (motor != null)
            {
                motor.Dashed -= HandleDashStarted;
            }

            if (comboController != null)
            {
                comboController.AttackStarted -= HandleAttackStarted;
            }
        }

        private void FixedUpdate()
        {
            if (config == null) return;

            float dt = Time.fixedDeltaTime;

            // Handle clash start delay
            if (_isClashPending)
            {
                _clashDelayRemaining -= dt;
                if (_clashDelayRemaining <= 0f)
                {
                    _isClashPending = false;
                    _currentState = DefenseState.HeavyStartup;
                    _windowTimer = _pendingClashDuration;
                }
                return;
            }

            // Tick window timer
            if (_currentState != DefenseState.None)
            {
                _windowTimer -= dt;
                if (_windowTimer <= 0f)
                {
                    CloseWindow();
                }
            }
        }

        /// <inheritdoc/>
        DamageResponse IDefenseProvider.ResolveDefense(Vector2 attackerPosition, bool isUnstoppable)
        {
            return Resolve(attackerPosition, isUnstoppable);
        }

        /// <summary>
        /// Resolve an incoming attack against this entity's current defense state.
        /// Called by the entity's <see cref="Shared.Interfaces.IDamageable"/> implementation.
        /// </summary>
        /// <param name="attackerPosition">World-space position of the attacker.</param>
        /// <param name="isUnstoppable">Whether the incoming attack is unstoppable.</param>
        /// <returns>The defense outcome.</returns>
        public DamageResponse Resolve(Vector2 attackerPosition, bool isUnstoppable = false)
        {
            Vector2 toAttacker = ((Vector3)attackerPosition - transform.position).normalized;

            var result = _resolver.Resolve(
                _currentState,
                _actionDirection,
                toAttacker,
                isUnstoppable);

            Debug.Log(
                $"[DefenseSystem] Resolve: state={_currentState}, actionDir={_actionDirection}, " +
                $"toAttacker={toAttacker}, timer={_windowTimer:F3}, " +
                $"isFacing={DefenseResolver.IsFacing(_actionDirection, toAttacker)}, " +
                $"isVertical={DefenseResolver.IsVertical(_actionDirection)} → {result}");

            return result;
        }

        /// <summary>
        /// Process the result of a defense resolution. Applies bonuses and fires events.
        /// Called by the entity's HitboxManager integration after resolving.
        /// </summary>
        /// <param name="response">The resolved defense outcome.</param>
        /// <param name="context">Context for bonus application.</param>
        /// <param name="characterType">The defending character's type.</param>
        /// <param name="incomingDamage">The raw incoming damage amount.</param>
        /// <param name="incomingType">The incoming damage type.</param>
        public void ProcessDefenseResult(
            DamageResponse response,
            DefenseContext context,
            CharacterType characterType,
            float incomingDamage,
            DamageType incomingType)
        {
            if (response == DamageResponse.Hit) return;

            // Apply character-specific bonus
            if (config != null && config.defenseBonus != null)
            {
                config.defenseBonus.Apply(context, response);
            }

            // Fire ICombatEvents
            switch (response)
            {
                case DamageResponse.Deflected:
                    OnDeflect?.Invoke(new DeflectEventData(
                        characterType, incomingDamage, incomingType));
                    break;

                case DamageResponse.Clashed:
                    OnClash?.Invoke(new ClashEventData(
                        characterType, incomingDamage, incomingType));
                    break;

                case DamageResponse.Dodged:
                    OnDodge?.Invoke(new DodgeEventData(
                        characterType, _actionDirection));
                    break;
            }
        }

        /// <inheritdoc/>
        public void NotifyDefenseSuccess(DamageResponse response, float incomingDamage, DamageType incomingType)
        {
            switch (response)
            {
                case DamageResponse.Deflected:
                    OnDeflect?.Invoke(new DeflectEventData(default, incomingDamage, incomingType));
                    break;
                case DamageResponse.Clashed:
                    OnClash?.Invoke(new ClashEventData(default, incomingDamage, incomingType));
                    break;
                case DamageResponse.Dodged:
                    OnDodge?.Invoke(new DodgeEventData(default, _actionDirection));
                    break;
            }
        }

        /// <inheritdoc/>
        public void OpenClashWindow(float duration, Vector2 facingDirection)
        {
            _actionDirection = facingDirection;
            _currentState = DefenseState.HeavyStartup;
            _windowTimer = duration;
            _isClashPending = false;
            Debug.Log($"[DefenseSystem] Manual clash window opened: duration={duration:F3}s, dir={facingDirection}");
        }

        private void HandleDashStarted(CharacterType charType, Vector2 dashDirection, bool hasIFrames)
        {
            if (config == null)
            {
                Debug.LogWarning("[DefenseSystem] HandleDashStarted — config is NULL, ignoring.");
                return;
            }

            Debug.Log($"[DefenseSystem] HandleDashStarted: dir={dashDirection}, isVertical={DefenseResolver.IsVertical(dashDirection)}, deflectWindow={config.deflectWindowDuration}s");

            _actionDirection = dashDirection;

            // Determine if this is a vertical dash (dodge) or horizontal (potential deflect)
            if (DefenseResolver.IsVertical(dashDirection))
            {
                // Dodge window — may have a start delay for i-frames
                _isClashPending = false;
                if (config.dodgeIFrameStart > 0f)
                {
                    _currentState = DefenseState.None;
                    StartCoroutine(OpenDodgeWindowAfterDelay());
                }
                else
                {
                    _currentState = DefenseState.Dashing;
                    _windowTimer = config.dodgeIFrameEnd;
                }
            }
            else
            {
                // Deflect window opens immediately on dash
                _currentState = DefenseState.Dashing;
                _windowTimer = config.deflectWindowDuration;
                _isClashPending = false;
            }
        }

        private System.Collections.IEnumerator OpenDodgeWindowAfterDelay()
        {
            yield return new WaitForSeconds(config.dodgeIFrameStart);

            // Only open if we haven't been interrupted
            if (_currentState == DefenseState.None && !_isClashPending)
            {
                _currentState = DefenseState.Dashing;
                _windowTimer = config.dodgeIFrameEnd - config.dodgeIFrameStart;
            }
        }

        private void HandleAttackStarted(AttackType attackType, int stepIndex)
        {
            // Read clash window from the attack's own data
            var attackData = GetAttackData(stepIndex);
            if (attackData == null || !attackData.HasClashWindow) return;

            // Get facing direction from motor
            if (motor != null)
            {
                _actionDirection = motor.FacingRight ? Vector2.right : Vector2.left;
            }

            // Clash window has a start delay
            if (attackData.clashWindowStart > 0f)
            {
                _isClashPending = true;
                _clashDelayRemaining = attackData.clashWindowStart;
                // Store window duration for when the delay finishes
                _pendingClashDuration = attackData.clashWindowEnd - attackData.clashWindowStart;
            }
            else
            {
                _currentState = DefenseState.HeavyStartup;
                _windowTimer = attackData.clashWindowEnd;
                _isClashPending = false;
            }

            Debug.Log($"[DefenseSystem] Clash window opened for '{attackData.attackName}': " +
                $"start={attackData.clashWindowStart:F3}s, end={attackData.clashWindowEnd:F3}s");
        }

        private AttackData GetAttackData(int stepIndex)
        {
            if (comboController == null) return null;
            var def = comboController.Definition;
            if (def == null || !def.IsValidStep(stepIndex)) return null;
            return def.steps[stepIndex].attackData;
        }

        private void CloseWindow()
        {
            Debug.Log($"[DefenseSystem] Window CLOSED (was {_currentState})");
            _currentState = DefenseState.None;
            _windowTimer = 0f;
            _isClashPending = false;
        }
    }
}
