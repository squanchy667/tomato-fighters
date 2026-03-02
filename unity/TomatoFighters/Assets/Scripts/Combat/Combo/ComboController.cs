using System;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Drives the combo system by wiring player input to the <see cref="ComboStateMachine"/>,
    /// triggering animations, and firing events for downstream systems (Motor, Defense, Pressure).
    /// Animation events on attack clips call <see cref="OnComboWindowOpen"/> and <see cref="OnFinisherEnd"/>.
    /// </summary>
    public class ComboController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ComboDefinition comboDefinition;
        [SerializeField] private CharacterType characterType;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        private ComboStateMachine stateMachine;

        /// <summary>Fired when an attack step begins. Args: attack type, step index.</summary>
        public event Action<AttackType, int> AttackStarted;

        /// <summary>Fired when the combo drops (window expired or no valid branch).</summary>
        public event Action ComboDropped;

        /// <summary>Fired when a finisher step begins. Arg: total combo length.</summary>
        public event Action<int> FinisherStarted;

        /// <summary>Fired when the finisher completes and combo resets cleanly.</summary>
        public event Action ComboEnded;

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

        private void Awake()
        {
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
            TriggerStepAnimation(stepIndex);
            var step = comboDefinition.steps[stepIndex];
            AttackStarted?.Invoke(step.attackType, stepIndex);
        }

        private void HandleComboDropped()
        {
            ComboDropped?.Invoke();
        }

        private void HandleFinisherTriggered(int comboLength)
        {
            TriggerStepAnimation(stateMachine.CurrentStepIndex);
            FinisherStarted?.Invoke(comboLength);
        }

        private void HandleFinisherEnded()
        {
            ComboEnded?.Invoke();
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
