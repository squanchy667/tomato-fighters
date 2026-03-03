using System;
using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Roguelite
{
    /// <summary>
    /// Per-level scaling values for a ritual.
    /// Formula used by RitualStackCalculator:
    ///   finalEffect = baseValue × levelMultiplier × (stackingMultiplier ^ currentStacks) × ritualPower
    /// Level multipliers are fixed constants in RitualStackCalculator: 1.0 / 1.5 / 2.0.
    /// </summary>
    [Serializable]
    public struct RitualLevelData
    {
        [Tooltip("Raw effect magnitude at this level (damage, heal, stat delta, etc.).")]
        public float baseValue;

        [Tooltip("Maximum simultaneous stacks of this ritual's effect.")]
        public int maxStacks;

        [Tooltip("Multiplier applied per additional stack (e.g. 1.2 = each stack adds 20%).")]
        public float stackingMultiplier;

        [Tooltip("Designer tuning knob — scales the entire output without changing the base numbers.")]
        public float ritualPower;
    }

    /// <summary>
    /// ScriptableObject defining a single ritual's identity, trigger, effect, and three-level scaling.
    ///
    /// <para>Rituals are the core roguelite modifier: each run the player accumulates rituals
    /// from 8 elemental families. When a matching <see cref="RitualTrigger"/> fires via
    /// <c>ICombatEvents</c>, RitualSystem resolves the effect using <see cref="effectId"/>.</para>
    ///
    /// <para>Placement: <c>Assets/ScriptableObjects/Rituals/{Family}/{Name}Ritual.asset</c></para>
    /// </summary>
    [CreateAssetMenu(menuName = "TomatoFighters/Data/RitualData", fileName = "NewRitual")]
    public class RitualData : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Display name shown in UI.")]
        public string ritualName;

        [TextArea(2, 4)]
        [Tooltip("Flavour description shown in the reward selector.")]
        public string description;

        [Tooltip("Primary elemental family — determines family synergy bonus tracking.")]
        public RitualFamily family;

        [Tooltip("Rarity / slot category within the family (Core, General, Enhancement, Twin).")]
        public RitualCategory category;

        [Tooltip("Combat action that fires this ritual's effect.")]
        public RitualTrigger trigger;

        // ── Effect ────────────────────────────────────────────────────────────

        [Header("Effect")]
        [Tooltip("Handler key used by RitualSystem dispatch table (e.g. \"Fire_Burn\"). " +
                 "Must match a registered handler in RitualSystem._handlers.")]
        public string effectId;

        [Tooltip("Optional VFX prefab spawned at the effect origin. Null = no VFX.")]
        public GameObject effectPrefab;

        // ── Twin Ritual ───────────────────────────────────────────────────────

        [Header("Twin Ritual")]
        [Tooltip("True if this ritual belongs to two elemental families simultaneously.")]
        public bool isTwin;

        [Tooltip("Second elemental family — only relevant when isTwin is true.")]
        public RitualFamily secondFamily;

        // ── Level Scaling ─────────────────────────────────────────────────────

        [Header("Level 1")]
        public RitualLevelData level1;

        [Header("Level 2")]
        public RitualLevelData level2;

        [Header("Level 3 (max)")]
        public RitualLevelData level3;

        // ── Accessors ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the scaling data for the given level (1–3).
        /// Clamps out-of-range values to the nearest valid level.
        /// </summary>
        public RitualLevelData GetLevelData(int level)
        {
            return level switch
            {
                1 => level1,
                2 => level2,
                _ => level3
            };
        }

        /// <summary>
        /// Returns true if this ritual belongs to <paramref name="queryFamily"/>
        /// (checks both primary and, for Twin rituals, secondary family).
        /// </summary>
        public bool BelongsToFamily(RitualFamily queryFamily)
        {
            if (family == queryFamily) return true;
            return isTwin && secondFamily == queryFamily;
        }
    }
}
