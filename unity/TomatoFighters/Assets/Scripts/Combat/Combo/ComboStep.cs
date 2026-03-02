using System;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// One node in a branching combo tree.
    /// Stored in a flat array on <see cref="ComboDefinition"/>; branches use index pointers.
    /// </summary>
    [Serializable]
    public struct ComboStep
    {
        [Tooltip("Whether this is a light or heavy attack.")]
        public AttackType attackType;

        [Tooltip("Animator trigger name for this step's animation.")]
        public string animationTrigger;

        [Tooltip("Damage multiplier applied to base attack damage.")]
        public float damageMultiplier;

        [Tooltip("Combo window duration override in seconds. 0 = use definition default.")]
        public float comboWindowDuration;

        [Tooltip("Index of next step on light input. -1 = no branch.")]
        public int nextOnLight;

        [Tooltip("Index of next step on heavy input. -1 = no branch.")]
        public int nextOnHeavy;

        [Tooltip("Whether this step is a combo finisher with bonus effects.")]
        public bool isFinisher;
    }
}
