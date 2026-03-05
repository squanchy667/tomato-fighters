using System;
using TomatoFighters.Shared.Data;
using UnityEngine;

namespace TomatoFighters.World
{
    /// <summary>
    /// A named, flat sequence of 1–3 AttackData steps with selection conditions.
    /// EnemyAI evaluates range, weight, and cooldown to pick a pattern.
    /// AttackState executes the steps in order.
    /// </summary>
    [CreateAssetMenu(menuName = "TomatoFighters/Data/EnemyAttackPattern")]
    public class EnemyAttackPattern : ScriptableObject
    {
        [Header("Identity")]
        public string patternName;

        [Header("Steps")]
        public AttackPatternStep[] steps;

        [Header("Selection")]
        [Tooltip("Higher weight = more likely to be chosen")]
        public float selectionWeight = 1f;

        [Tooltip("Min distance to target for this pattern to be valid")]
        public float minRange;

        [Tooltip("Max distance to target for this pattern to be valid")]
        public float maxRange = 99f;

        [Tooltip("Seconds before this pattern can be selected again")]
        public float patternCooldown;
    }

    /// <summary>
    /// A single step in an attack pattern: an AttackData reference
    /// plus an optional pause before the step begins.
    /// </summary>
    [Serializable]
    public struct AttackPatternStep
    {
        public AttackData attack;

        [Tooltip("Pause in seconds before this step begins")]
        public float delayBeforeStep;
    }
}
