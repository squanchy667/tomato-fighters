using UnityEngine;

namespace TomatoFighters.Characters.Passives
{
    /// <summary>
    /// Tunable values for all 4 character passives.
    /// Editable from the Inspector — logic classes receive this as input.
    /// </summary>
    [CreateAssetMenu(fileName = "PassiveConfig", menuName = "TomatoFighters/PassiveConfig")]
    public class PassiveConfig : ScriptableObject
    {
        [Header("Thick Skin (Brutor)")]
        [Tooltip("Percentage of incoming damage reduced (0.15 = 15% DR).")]
        [Range(0f, 0.5f)]
        public float thickSkinDamageReduction = 0.15f;

        [Tooltip("Percentage of knockback force reduced (0.40 = 40% less knockback).")]
        [Range(0f, 0.8f)]
        public float thickSkinKnockbackReduction = 0.40f;

        [Header("Bloodlust (Slasher)")]
        [Tooltip("ATK multiplier bonus per stack (0.03 = +3% per stack).")]
        [Range(0f, 0.1f)]
        public float bloodlustAtkPerStack = 0.03f;

        [Tooltip("Maximum number of stacks (10 = +30% max ATK).")]
        [Range(1, 20)]
        public int bloodlustMaxStacks = 10;

        [Tooltip("Seconds without landing a hit before stacks reset to 0.")]
        [Range(1f, 10f)]
        public float bloodlustDecayTime = 3f;

        [Header("Arcane Resonance (Mystica)")]
        [Tooltip("Damage multiplier per stack (0.05 = +5% per stack, multiplicative).")]
        [Range(0f, 0.2f)]
        public float arcaneResonanceDmgPerStack = 0.05f;

        [Tooltip("Maximum concurrent stacks.")]
        [Range(1, 10)]
        public int arcaneResonanceMaxStacks = 3;

        [Tooltip("Duration of each individual stack before expiry (seconds).")]
        [Range(1f, 10f)]
        public float arcaneResonanceStackDuration = 3f;

        [Header("Distance Bonus (Viper)")]
        [Tooltip("Damage bonus per unit of distance (0.02 = +2% per unit).")]
        [Range(0f, 0.1f)]
        public float distanceBonusPerUnit = 0.02f;

        [Tooltip("Maximum damage bonus percentage (0.30 = +30% cap).")]
        [Range(0f, 1f)]
        public float distanceBonusMaxPercent = 0.30f;
    }
}
