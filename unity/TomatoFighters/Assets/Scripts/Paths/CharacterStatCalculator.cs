using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Paths
{
    /// <summary>
    /// Pure C# class that calculates final character stats by layering path bonuses,
    /// ritual multipliers, trinket modifiers, and soul tree bonuses on top of base stats.
    ///
    /// <para>This is the single source of truth for "what are this character's current
    /// stats?" at any point during a run. Queried by all 3 pillars:
    /// <list type="bullet">
    ///   <item><description>Combat — base ATK for damage calculation</description></item>
    ///   <item><description>World (HUD) — displays all stats to the player (T025)</description></item>
    ///   <item><description>Roguelite — checks stats for passive and buff effects (T017)</description></item>
    /// </list></para>
    ///
    /// <para><b>Formula per stat:</b> (Base + PathBonus) × RitualMultiplier × TrinketMultiplier × SoulTreeBonus</para>
    ///
    /// <para><b>Stateless</b> — all inputs are passed via parameters, making this class
    /// thread-safe and unit-testable without a Unity scene.</para>
    /// </summary>
    public class CharacterStatCalculator
    {
        /// <summary>
        /// Calculates all 10 character stats for the current run state.
        ///
        /// <para>Formula per stat: (Base + PathBonus) × Ritual × Trinket × SoulTree</para>
        ///
        /// <para>Integer stats (HP, DEF, MNA) are rounded to nearest int.
        /// CritChance is clamped to [0, 1].
        /// RangedAttack returns -1f for non-Viper characters without applying any modifiers.</para>
        /// </summary>
        /// <param name="input">All modifier sources. Use <see cref="StatModifierInput.Default"/>
        /// to start with no modifiers active.</param>
        /// <returns>Fully calculated <see cref="FinalStats"/> ready for use by all pillars.</returns>
        public FinalStats Calculate(StatModifierInput input)
        {
            var b = input.baseStats;

            return new FinalStats
            {
                health        = Mathf.RoundToInt(CalcFloat(StatType.Health,        b.health,        input)),
                defense       = Mathf.RoundToInt(CalcFloat(StatType.Defense,       b.defense,       input)),
                attack        = CalcFloat(StatType.Attack,                          b.attack,        input),
                rangedAttack  = b.rangedAttack < 0f
                                    ? -1f
                                    : CalcFloat(StatType.RangedAttack,             b.rangedAttack,  input),
                throwableAttack = CalcFloat(StatType.ThrowableAttack,              b.throwableAttack, input),
                speed         = CalcFloat(StatType.Speed,                           b.speed,         input),
                mana          = Mathf.RoundToInt(CalcFloat(StatType.Mana,          b.mana,          input)),
                manaRegen     = CalcFloat(StatType.ManaRegen,                       b.manaRegen,     input),
                critChance    = Mathf.Clamp01(CalcFloat(StatType.CritChance,       b.critChance,    input)),
                stunRate      = CalcFloat(StatType.StunRate,                        b.stunRate,      input),
            };
        }

        /// <summary>
        /// Calculates a single stat on demand.
        /// Useful when only one value is needed and a full <see cref="Calculate"/> call
        /// would recalculate stats that aren't required.
        ///
        /// <para>Special cases:
        /// <list type="bullet">
        ///   <item><description>Integer stats (Health, Defense, Mana) — returns rounded float.</description></item>
        ///   <item><description>RangedAttack on non-Viper — returns -1f without applying modifiers.</description></item>
        ///   <item><description>CancelWindow — returns 0f (no base value in CharacterBaseStats).</description></item>
        /// </list></para>
        /// </summary>
        /// <param name="stat">The stat to calculate.</param>
        /// <param name="input">All modifier sources.</param>
        /// <returns>The calculated final value for the requested stat.</returns>
        public float CalculateSingleStat(StatType stat, StatModifierInput input)
        {
            var b = input.baseStats;

            switch (stat)
            {
                case StatType.Health:
                    return Mathf.RoundToInt(CalcFloat(stat, b.health, input));

                case StatType.Defense:
                    return Mathf.RoundToInt(CalcFloat(stat, b.defense, input));

                case StatType.Attack:
                    return CalcFloat(stat, b.attack, input);

                case StatType.RangedAttack:
                    // Non-Viper characters store -1 as sentinel — never apply modifiers to it
                    return b.rangedAttack < 0f ? -1f : CalcFloat(stat, b.rangedAttack, input);

                case StatType.ThrowableAttack:
                    return CalcFloat(stat, b.throwableAttack, input);

                case StatType.Speed:
                    return CalcFloat(stat, b.speed, input);

                case StatType.Mana:
                    return Mathf.RoundToInt(CalcFloat(stat, b.mana, input));

                case StatType.ManaRegen:
                    return CalcFloat(stat, b.manaRegen, input);

                case StatType.CritChance:
                    return Mathf.Clamp01(CalcFloat(stat, b.critChance, input));

                case StatType.StunRate:
                    return CalcFloat(stat, b.stunRate, input);

                case StatType.CancelWindow:
                    // No base value defined in CharacterBaseStats — combat-derived concept
                    return 0f;

                default:
                    return 0f;
            }
        }

        // ── Internal ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Core formula for a single stat.
        /// (baseValue + pathBonus) × ritualMultiplier × trinketMultiplier × soulTreeBonus
        /// </summary>
        private static float CalcFloat(StatType stat, float baseValue, StatModifierInput input)
        {
            int i = (int)stat;
            return (baseValue + input.pathBonuses[i])
                   * input.ritualMultipliers[i]
                   * input.trinketMultipliers[i]
                   * input.soulTreeBonuses[i];
        }
    }
}
