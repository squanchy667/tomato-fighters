using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Defines a character's combo tree as a flat array of <see cref="ComboStep"/> nodes.
    /// Each step has index pointers to the next light/heavy branch.
    /// One asset per character archetype.
    /// </summary>
    [CreateAssetMenu(fileName = "NewComboDefinition", menuName = "TomatoFighters/Combat/Combo Definition")]
    public class ComboDefinition : ScriptableObject
    {
        [Tooltip("All steps in the combo tree. Referenced by index.")]
        public ComboStep[] steps;

        [Tooltip("Index of the first step when light attack is pressed from idle.")]
        public int rootLightIndex;

        [Tooltip("Index of the first step when heavy attack is pressed from idle.")]
        public int rootHeavyIndex;

        [Tooltip("Default combo window duration if a step doesn't override (seconds).")]
        public float defaultComboWindow = 0.3f;

        /// <summary>
        /// Get the effective combo window duration for a step.
        /// Uses the step's override if set, otherwise the definition default.
        /// </summary>
        public float GetComboWindow(int stepIndex)
        {
            if (!IsValidStep(stepIndex)) return 0f;

            float stepWindow = steps[stepIndex].comboWindowDuration;
            return stepWindow > 0f ? stepWindow : defaultComboWindow;
        }

        /// <summary>
        /// Get the next step index for the given input type, or -1 if no branch exists.
        /// </summary>
        public int GetNextStep(int currentStepIndex, AttackType input)
        {
            if (!IsValidStep(currentStepIndex)) return -1;

            var step = steps[currentStepIndex];
            return input == AttackType.Light ? step.nextOnLight : step.nextOnHeavy;
        }

        /// <summary>Whether the given index points to a valid step in the array.</summary>
        public bool IsValidStep(int stepIndex)
        {
            return stepIndex >= 0 && steps != null && stepIndex < steps.Length;
        }
    }
}
