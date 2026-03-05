using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Effect data returned by <see cref="TomatoFighters.Shared.Interfaces.IBuffProvider"/>.
    /// Combat applies this alongside or after an action — the list it comes from determines
    /// when it fires: every hit (<c>GetAdditionalOnHitEffects</c>) or a specific trigger
    /// (<c>GetTriggerEffects</c>).
    ///
    /// <para>All damage is instant bonus elemental damage in the same frame (Phase 2).
    /// DoT ticks are deferred to Phase 3 / T029 — see TODO comments in RitualSystem handlers.</para>
    /// </summary>
    public class RitualEffect
    {
        /// <summary>Elemental damage type to apply as a bonus hit.</summary>
        public DamageType damageType;

        /// <summary>
        /// Bonus damage expressed as a fraction of the triggering hit's base damage.
        /// e.g. 0.20 = deal an extra 20% of the hit's damage as this element.
        /// </summary>
        public float damageMultiplier;

        /// <summary>Optional VFX prefab spawned at the hit position. Null = no VFX.</summary>
        public GameObject vfxPrefab;

        /// <summary>Temporary speed multiplier applied this frame. 1.0 = no change.</summary>
        public float speedMultiplier;
    }

    /// <summary>
    /// Path ability runtime data. Represents an unlocked path ability for combat execution.
    /// Used by <see cref="TomatoFighters.Shared.Interfaces.IBuffProvider.GetActivePathAbilities"/>.
    /// Fully implemented by PathAbilityExecutor (T028).
    /// </summary>
    public class PathAbility
    {
        /// <summary>Ability identifier matching PathData.tierXAbilityId.</summary>
        public string abilityId;

        /// <summary>Which path type this ability belongs to.</summary>
        public Enums.PathType pathType;

        /// <summary>Whether the ability is currently active (toggle on, channeling, etc.).</summary>
        public bool isActive;
    }
}
