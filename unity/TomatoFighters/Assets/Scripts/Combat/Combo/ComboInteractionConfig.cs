using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Configures how the combo system interacts with external systems:
    /// cancel behavior on hit-confirm, combo reset triggers, and movement lock rules.
    /// One asset per character archetype for tuning in Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "NewComboInteractionConfig", menuName = "TomatoFighters/Combat/Combo Interaction Config")]
    public class ComboInteractionConfig : ScriptableObject
    {
        [Header("Cancel on Hit-Confirm")]
        [Tooltip("Dash cancel priority over jump cancel when both are available.")]
        public CancelPriority cancelPriority = CancelPriority.DashOverJump;

        [Tooltip("Whether dash-cancelling resets the combo chain.")]
        public bool dashCancelResetsCombo = true;

        [Tooltip("Whether jump-cancelling resets the combo chain.")]
        public bool jumpCancelResetsCombo;

        [Header("Combo Reset Triggers")]
        [Tooltip("Reset combo when the character takes a stagger hit.")]
        public bool resetOnStagger = true;

        [Tooltip("Reset combo when the character dies.")]
        public bool resetOnDeath = true;

        [Header("Movement Lock")]
        [Tooltip("Lock movement during normal attack steps.")]
        public bool lockMovementDuringAttack = true;

        [Tooltip("Lock movement during finisher animations.")]
        public bool lockMovementDuringFinisher = true;
    }

    /// <summary>
    /// When both dash-cancel and jump-cancel are available and both inputs arrive,
    /// which one takes priority.
    /// </summary>
    public enum CancelPriority
    {
        DashOverJump,
        JumpOverDash
    }
}
