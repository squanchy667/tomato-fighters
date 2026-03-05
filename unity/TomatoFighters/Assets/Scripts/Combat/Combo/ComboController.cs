using System;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Events;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Drives the combo system by wiring player input to the <see cref="ComboStateMachine"/>,
    /// triggering animations, managing movement lock, and firing events for downstream systems.
    /// Animation events on attack clips call <see cref="OnComboWindowOpen"/> and <see cref="OnFinisherEnd"/>.
    /// </summary>
    public class ComboController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ComboDefinition comboDefinition;
        [SerializeField] private ComboInteractionConfig interactionConfig;
        [SerializeField] private CharacterType characterType;

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterMotor motor;

        [Header("HUD Events")]
        [SerializeField]
        [Tooltip("Fires with current combo length on every hit-confirm. HUD subscribes.")]
        private IntEventChannel onComboHitConfirmed;

        [Header("Cancel Buffering")]
        [SerializeField]
        [Tooltip("How long a cancel input stays buffered before expiring.")]
        [Range(0.05f, 0.3f)]
        private float cancelBufferWindow = 0.15f;

        private ComboStateMachine stateMachine;

        private float lastDashCancelTime = -1f;
        private Vector2 lastDashCancelDirection;
        private float lastJumpCancelTime = -1f;

        /// <summary>Fired when an attack step begins. Args: attack type, step index.</summary>
        public event Action<AttackType, int> AttackStarted;

        /// <summary>Fired when the combo drops (window expired or no valid branch).</summary>
        public event Action ComboDropped;

        /// <summary>Fired when a finisher step begins. Arg: total combo length.</summary>
        public event Action<int> FinisherStarted;

        /// <summary>Fired when the finisher completes and combo resets cleanly.</summary>
        public event Action ComboEnded;

        /// <summary>Fired when a dash-cancel is executed.</summary>
        public event Action DashCancelTriggered;

        /// <summary>Fired when a jump-cancel is executed.</summary>
        public event Action JumpCancelTriggered;

        /// <summary>Current combo state.</summary>
        public ComboState CurrentState => stateMachine.CurrentState;

        /// <summary>Index of the current step in the definition's step array, or -1 if idle.</summary>
        public int CurrentStepIndex => stateMachine.CurrentStepIndex;

        /// <summary>Number of hits in the current combo chain.</summary>
        public int ComboLength => stateMachine.ComboLength;

        /// <summary>The combo definition driving this controller.</summary>
        public ComboDefinition Definition => comboDefinition;

        /// <summary>Which character archetype this controller belongs to.</summary>
        public CharacterType CharacterType => characterType;

        /// <summary>Whether a combo is currently active (not idle).</summary>
        public bool IsComboActive => stateMachine.CurrentState != ComboState.Idle;

        /// <summary>Whether dash-cancel is currently available.</summary>
        public bool CanDashCancel => stateMachine.CanDashCancel;

        /// <summary>Whether jump-cancel is currently available.</summary>
        public bool CanJumpCancel => stateMachine.CanJumpCancel;

        private void Awake()
        {
            if (animator == null)
            {
                Debug.LogError(
                    $"[ComboController] No Animator assigned on {gameObject.name}. " +
                    "Combo animations will not play.", this);
            }

            stateMachine = new ComboStateMachine();
            stateMachine.SetDefinition(comboDefinition);

            stateMachine.StepStarted += HandleStepStarted;
            stateMachine.ComboDropped += HandleComboDropped;
            stateMachine.FinisherTriggered += HandleFinisherTriggered;
            stateMachine.FinisherEnded += HandleFinisherEnded;
        }

        private void OnDestroy()
        {
            stateMachine.StepStarted -= HandleStepStarted;
            stateMachine.ComboDropped -= HandleComboDropped;
            stateMachine.FinisherTriggered -= HandleFinisherTriggered;
            stateMachine.FinisherEnded -= HandleFinisherEnded;
        }

        private void Update()
        {
            stateMachine.Tick(Time.deltaTime);
        }

        /// <summary>Request a light attack. Called by <see cref="Characters.CharacterInputHandler"/>.</summary>
        public void RequestLightAttack()
        {
            stateMachine.ReceiveInput(AttackType.Light);
        }

        /// <summary>Request a heavy attack. Called by <see cref="Characters.CharacterInputHandler"/>.</summary>
        public void RequestHeavyAttack()
        {
            stateMachine.ReceiveInput(AttackType.Heavy);
        }

        /// <summary>
        /// Signal that the current attack hit a target. Enables cancel windows.
        /// Called by HitboxManager (T015, future) or debug key.
        /// </summary>
        public void OnHitConfirmed()
        {
            stateMachine.OnHitConfirmed();
            TryConsumeBufferedCancels();

            if (onComboHitConfirmed != null)
            {
                onComboHitConfirmed.Raise(ComboLength);
            }
        }

        /// <summary>
        /// Attempt a dash-cancel. Succeeds if hit-confirmed and current step allows it.
        /// Returns true if the cancel was executed.
        /// </summary>
        public bool RequestDashCancel()
        {
            if (!stateMachine.CanDashCancel) return false;

            bool resetsCombo = interactionConfig == null || interactionConfig.dashCancelResetsCombo;
            stateMachine.CancelPerformed();
            UnlockMovement();
            DashCancelTriggered?.Invoke();

            return true;
        }

        /// <summary>
        /// Attempt a jump-cancel. Succeeds if hit-confirmed and current step allows it.
        /// Returns true if the cancel was executed.
        /// </summary>
        public bool RequestJumpCancel()
        {
            if (!stateMachine.CanJumpCancel) return false;

            bool resetsCombo = interactionConfig == null || interactionConfig.jumpCancelResetsCombo;
            stateMachine.CancelPerformed();
            UnlockMovement();
            JumpCancelTriggered?.Invoke();

            return true;
        }

        /// <summary>
        /// Attempt a dash-cancel or buffer the input for later consumption on hit-confirm.
        /// Called by <see cref="Characters.CharacterInputHandler"/> during active combos.
        /// Returns true only if the cancel executed immediately.
        /// </summary>
        public bool TryDashCancel(Vector2 direction)
        {
            if (CanDashCancel)
            {
                RequestDashCancel();
                motor?.RequestDash(direction);
                return true;
            }

            // Buffer for consumption when the cancel window opens
            lastDashCancelTime = Time.time;
            lastDashCancelDirection = direction;
            return false;
        }

        /// <summary>
        /// Attempt a jump-cancel or buffer the input for later consumption on hit-confirm.
        /// Called by <see cref="Characters.CharacterInputHandler"/> during active combos.
        /// Returns true only if the cancel executed immediately.
        /// </summary>
        public bool TryJumpCancel()
        {
            if (CanJumpCancel)
            {
                RequestJumpCancel();
                motor?.RequestJump();
                return true;
            }

            lastJumpCancelTime = Time.time;
            return false;
        }

        /// <summary>
        /// Force-reset the combo from an external system (stagger, death, etc.).
        /// </summary>
        public void ForceResetCombo()
        {
            if (!IsComboActive) return;

            ClearCancelBuffers();
            stateMachine.Reset();
            UnlockMovement();
            ComboDropped?.Invoke();
        }

        /// <summary>
        /// Animation event callback. Place on attack clips at the frame where
        /// active hitbox ends and the combo window should open.
        /// </summary>
        public void OnComboWindowOpen()
        {
            stateMachine.OnComboWindowOpen();
        }

        /// <summary>
        /// Animation event callback. Place on finisher clips at the last frame
        /// to signal the finisher is complete.
        /// </summary>
        public void OnFinisherEnd()
        {
            stateMachine.OnFinisherEnd();
        }

        private void HandleStepStarted(int stepIndex)
        {
            LockMovementIfConfigured(isFinisher: false);
            TriggerStepAnimation(stepIndex);
            var step = comboDefinition.steps[stepIndex];
            AttackStarted?.Invoke(step.attackType, stepIndex);
        }

        private void HandleComboDropped()
        {
            ClearCancelBuffers();
            UnlockMovement();
            ComboDropped?.Invoke();
        }

        private void HandleFinisherTriggered(int comboLength)
        {
            LockMovementIfConfigured(isFinisher: true);
            TriggerStepAnimation(stateMachine.CurrentStepIndex);
            FinisherStarted?.Invoke(comboLength);
        }

        private void HandleFinisherEnded()
        {
            UnlockMovement();
            ComboEnded?.Invoke();
        }

        /// <summary>
        /// Checks whether a buffered dash or jump cancel should fire now that the
        /// cancel window has opened (after hit-confirm). Respects cancelPriority
        /// when both are buffered.
        /// </summary>
        private void TryConsumeBufferedCancels()
        {
            float now = Time.time;
            bool dashBuffered = lastDashCancelTime >= 0f && (now - lastDashCancelTime) <= cancelBufferWindow;
            bool jumpBuffered = lastJumpCancelTime >= 0f && (now - lastJumpCancelTime) <= cancelBufferWindow;

            if (!dashBuffered && !jumpBuffered) return;

            // When both are buffered, use config priority to pick one
            bool preferDash = interactionConfig == null
                || interactionConfig.cancelPriority == CancelPriority.DashOverJump;

            if (dashBuffered && jumpBuffered)
            {
                if (preferDash)
                    jumpBuffered = false;
                else
                    dashBuffered = false;
            }

            if (dashBuffered && CanDashCancel)
            {
                Vector2 dir = lastDashCancelDirection;
                ClearCancelBuffers();
                RequestDashCancel();
                motor?.RequestDash(dir);
                return;
            }

            if (jumpBuffered && CanJumpCancel)
            {
                ClearCancelBuffers();
                RequestJumpCancel();
                motor?.RequestJump();
                return;
            }
        }

        private void ClearCancelBuffers()
        {
            lastDashCancelTime = -1f;
            lastJumpCancelTime = -1f;
        }

        private void LockMovementIfConfigured(bool isFinisher)
        {
            if (interactionConfig == null) return;

            bool shouldLock = isFinisher
                ? interactionConfig.lockMovementDuringFinisher
                : interactionConfig.lockMovementDuringAttack;

            if (shouldLock)
            {
                motor?.SetAttackLock(true);
            }
        }

        private void UnlockMovement()
        {
            motor?.SetAttackLock(false);
        }

        private void TriggerStepAnimation(int stepIndex)
        {
            if (animator == null) return;
            if (!comboDefinition.IsValidStep(stepIndex)) return;

            string trigger = comboDefinition.steps[stepIndex].animationTrigger;
            if (!string.IsNullOrEmpty(trigger))
            {
                animator.SetTrigger(trigger);
            }
        }
    }
}
