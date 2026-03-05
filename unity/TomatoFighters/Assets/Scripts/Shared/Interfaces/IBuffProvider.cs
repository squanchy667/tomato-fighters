using System.Collections.Generic;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;

namespace TomatoFighters.Shared.Interfaces
{
    /// <summary>
    /// Roguelite pillar provides buff data; Combat pillar queries it for damage/stat multipliers.
    /// Default returns when no buffs active: 1.0f for multipliers, empty lists, false for overrides.
    /// </summary>
    public interface IBuffProvider
    {
        /// <summary>Get the total damage multiplier for a given damage type (base 1.0).</summary>
        float GetDamageMultiplier(DamageType type);

        /// <summary>Get the total speed multiplier from all active buffs (base 1.0).</summary>
        float GetSpeedMultiplier();

        /// <summary>Get the total defense multiplier from all active buffs (base 1.0).</summary>
        float GetDefenseMultiplier();

        /// <summary>Get all on-hit effects from active rituals to apply on each hit.</summary>
        List<RitualEffect> GetAdditionalOnHitEffects();

        /// <summary>Get all trigger-based effects for a specific ritual trigger.</summary>
        List<RitualEffect> GetTriggerEffects(RitualTrigger trigger);

        /// <summary>Whether the repetitive action penalty is overridden by a buff or ritual.</summary>
        bool IsRepetitivePenaltyOverridden();

        /// <summary>Get the path-specific damage multiplier (base 1.0).</summary>
        float GetPathDamageMultiplier();

        /// <summary>Get the path-specific defense multiplier (base 1.0).</summary>
        float GetPathDefenseMultiplier();

        /// <summary>Get the path-specific speed multiplier (base 1.0).</summary>
        float GetPathSpeedMultiplier();

        /// <summary>Get all currently active path abilities for combat execution.</summary>
        List<PathAbility> GetActivePathAbilities();

        /// <summary>
        /// Gravity multiplier for the juggle system (base 1.0).
        /// Gale element rituals reduce this to extend airtime (e.g. 0.7 = 30% slower fall).
        /// </summary>
        float GetJuggleGravityMultiplier();
    }
}
