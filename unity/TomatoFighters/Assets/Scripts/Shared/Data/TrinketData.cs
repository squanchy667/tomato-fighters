using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Defines a single trinket — a stat modifier item found during runs.
    /// Each trinket targets one stat with either a flat or percentage modifier,
    /// optionally gated behind a trigger condition with a timed buff duration.
    /// </summary>
    [CreateAssetMenu(menuName = "TomatoFighters/Data/Trinket Data",
                     fileName = "NewTrinketData")]
    public class TrinketData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name shown in UI.")]
        public string displayName;

        [TextArea(2, 4)]
        [Tooltip("Flavour/mechanical description for the player.")]
        public string description;

        [Tooltip("Icon shown in trinket slots and reward screens.")]
        public Sprite icon;

        [Header("Modifier")]
        [Tooltip("Which stat this trinket modifies.")]
        public StatType affectedStat;

        [Tooltip("The modifier value. For Percent: 0.1 = +10%. For Flat: raw additive amount.")]
        public float modifierValue;

        [Tooltip("Whether this is a flat additive or percentage multiplier.")]
        public ModifierType modifierType;

        [Header("Trigger")]
        [Tooltip("When does the modifier activate? Always = permanent while equipped.")]
        public TrinketTriggerType triggerType;

        [Min(0f)]
        [Tooltip("How long (seconds) the buff lasts after triggering. Ignored for Always trinkets.")]
        public float buffDuration = 5f;
    }
}
