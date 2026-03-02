using System;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Plain C# state machine for combo chain tracking.
    /// Manages state transitions, input buffering, and combo window timing.
    /// Testable without Unity runtime.
    /// </summary>
    public class ComboStateMachine
    {
        /// <summary>Current combo state.</summary>
        public ComboState CurrentState { get; private set; } = ComboState.Idle;

        /// <summary>Index of the current step in the ComboDefinition's step array.</summary>
        public int CurrentStepIndex { get; private set; } = -1;

        /// <summary>Number of hits landed in the current combo chain.</summary>
        public int ComboLength { get; private set; }

        /// <summary>Buffered attack input waiting to be consumed when the combo window opens.</summary>
        public AttackType? BufferedInput { get; private set; }

        private ComboDefinition definition;
        private float windowTimer;

        /// <summary>Fired when a new combo step begins. Arg: step index.</summary>
        public event Action<int> StepStarted;

        /// <summary>Fired when the combo drops (window expired or no valid branch).</summary>
        public event Action ComboDropped;

        /// <summary>Fired when a finisher step begins. Arg: total combo length.</summary>
        public event Action<int> FinisherTriggered;

        /// <summary>Fired when the finisher animation completes and combo resets cleanly.</summary>
        public event Action FinisherEnded;

        /// <summary>Assign the combo definition that drives branching and timing.</summary>
        public void SetDefinition(ComboDefinition def)
        {
            definition = def;
            Reset();
        }

        /// <summary>
        /// Tick the combo window timer. Call from MonoBehaviour.Update() with Time.deltaTime.
        /// Only active during <see cref="ComboState.ComboWindow"/>.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (CurrentState != ComboState.ComboWindow) return;

            windowTimer -= deltaTime;
            if (windowTimer <= 0f)
            {
                Reset();
                ComboDropped?.Invoke();
            }
        }

        /// <summary>
        /// Receive an attack input. Starts a combo from Idle, buffers during Attacking,
        /// or advances the chain during ComboWindow.
        /// </summary>
        public void ReceiveInput(AttackType type)
        {
            if (definition == null) return;

            switch (CurrentState)
            {
                case ComboState.Idle:
                    StartCombo(type);
                    break;
                case ComboState.Attacking:
                    BufferedInput = type;
                    break;
                case ComboState.ComboWindow:
                    TryAdvanceCombo(type);
                    break;
                case ComboState.Finisher:
                    // Locked — ignore input
                    break;
            }
        }

        /// <summary>
        /// Called by animation event when attack active frames end and the combo window opens.
        /// Consumes any buffered input immediately.
        /// </summary>
        public void OnComboWindowOpen()
        {
            if (CurrentState != ComboState.Attacking) return;

            CurrentState = ComboState.ComboWindow;
            windowTimer = definition.GetComboWindow(CurrentStepIndex);

            if (BufferedInput.HasValue)
            {
                var input = BufferedInput.Value;
                BufferedInput = null;
                TryAdvanceCombo(input);
            }
        }

        /// <summary>
        /// Called by animation event when the finisher animation completes.
        /// </summary>
        public void OnFinisherEnd()
        {
            if (CurrentState != ComboState.Finisher) return;

            Reset();
            FinisherEnded?.Invoke();
        }

        /// <summary>Reset combo state to Idle. Clears chain position, length, and buffer.</summary>
        public void Reset()
        {
            CurrentState = ComboState.Idle;
            CurrentStepIndex = -1;
            ComboLength = 0;
            BufferedInput = null;
            windowTimer = 0f;
        }

        private void StartCombo(AttackType type)
        {
            int rootIndex = type == AttackType.Light
                ? definition.rootLightIndex
                : definition.rootHeavyIndex;

            if (!definition.IsValidStep(rootIndex)) return;

            CurrentStepIndex = rootIndex;
            ComboLength = 1;
            BufferedInput = null;

            var step = definition.steps[rootIndex];
            if (step.isFinisher)
            {
                CurrentState = ComboState.Finisher;
                FinisherTriggered?.Invoke(ComboLength);
            }
            else
            {
                CurrentState = ComboState.Attacking;
                StepStarted?.Invoke(CurrentStepIndex);
            }
        }

        private void TryAdvanceCombo(AttackType type)
        {
            int nextIndex = definition.GetNextStep(CurrentStepIndex, type);

            if (!definition.IsValidStep(nextIndex))
            {
                // No valid branch for this input — drop the combo
                Reset();
                ComboDropped?.Invoke();
                return;
            }

            CurrentStepIndex = nextIndex;
            ComboLength++;
            BufferedInput = null;

            var step = definition.steps[nextIndex];
            if (step.isFinisher)
            {
                CurrentState = ComboState.Finisher;
                FinisherTriggered?.Invoke(ComboLength);
            }
            else
            {
                CurrentState = ComboState.Attacking;
                StepStarted?.Invoke(CurrentStepIndex);
            }
        }
    }
}
