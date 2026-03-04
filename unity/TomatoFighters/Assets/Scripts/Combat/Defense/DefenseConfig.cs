using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Per-entity defense timing configuration. Assigned to <see cref="DefenseSystem"/>
    /// via Inspector. Different characters/enemies can have different window timings.
    /// </summary>
    [CreateAssetMenu(menuName = "TomatoFighters/Combat/DefenseConfig")]
    public class DefenseConfig : ScriptableObject
    {
        [Header("Deflect (Dash Toward)")]
        [Tooltip("Duration in seconds from dash start where deflect is active.")]
        [Range(0f, 0.3f)]
        public float deflectWindowDuration = 0.15f;

        [Header("Clash (Heavy Attack Facing Toward)")]
        [Tooltip("Start delay in seconds from heavy attack start.")]
        [Range(0f, 2.0f)]
        public float clashWindowStart = 0.02f;

        [Tooltip("End time in seconds from heavy attack start.")]
        [Range(0f, 3.0f)]
        public float clashWindowEnd = 0.08f;

        [Header("Dodge (Dash Vertical)")]
        [Tooltip("Start delay in seconds from dash start.")]
        [Range(0f, 0.1f)]
        public float dodgeIFrameStart = 0.05f;

        [Tooltip("End time in seconds from dash start.")]
        [Range(0f, 0.5f)]
        public float dodgeIFrameEnd = 0.3f;

        [Header("Bonus")]
        [Tooltip("Character-specific defense bonus (null = none).")]
        public DefenseBonus defenseBonus;
    }
}
