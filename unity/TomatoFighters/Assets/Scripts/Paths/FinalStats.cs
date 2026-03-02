using TomatoFighters.Shared.Enums;

namespace TomatoFighters.Paths
{
    /// <summary>
    /// Fully calculated character stats for the current run state.
    /// Produced by <see cref="CharacterStatCalculator.Calculate"/>.
    ///
    /// <para>All values have had path bonuses, ritual multipliers, trinket modifiers,
    /// and soul tree bonuses applied. Integer stats (HP, DEF, MNA) are already rounded.
    /// CritChance is already clamped to [0, 1].</para>
    ///
    /// <para>Read by all 3 pillars. Use <see cref="GetAttackForMode"/> to retrieve
    /// the correct attack value — never read <c>rangedAttack</c> directly, as it
    /// is -1f for non-Viper characters.</para>
    /// </summary>
    public struct FinalStats
    {
        // ── Vitals ───────────────────────────────────────────────────────────

        /// <summary>Total hit points after all modifiers. Already rounded to nearest int.</summary>
        public int health;

        /// <summary>Flat damage reduction per hit after all modifiers. Already rounded.</summary>
        public int defense;

        // ── Attack ───────────────────────────────────────────────────────────

        /// <summary>Melee damage multiplier after all modifiers.</summary>
        public float attack;

        /// <summary>
        /// Ranged projectile damage multiplier after all modifiers.
        /// <b>Viper only.</b> -1f for Brutor, Slasher, and Mystica.
        /// Use <see cref="GetAttackForMode"/> instead of reading this field directly.
        /// </summary>
        public float rangedAttack;

        /// <summary>
        /// Throwable item damage multiplier after all modifiers.
        /// Applies to all characters when using ground-pickup items.
        /// </summary>
        public float throwableAttack;

        // ── Mobility ─────────────────────────────────────────────────────────

        /// <summary>Movement speed and dash distance multiplier after all modifiers.</summary>
        public float speed;

        // ── Mana ─────────────────────────────────────────────────────────────

        /// <summary>Total mana pool after all modifiers. Already rounded to nearest int.</summary>
        public int mana;

        /// <summary>Mana restored per second after all modifiers.</summary>
        public float manaRegen;

        // ── Combat ───────────────────────────────────────────────────────────

        /// <summary>
        /// Crit chance after all modifiers. Clamped to [0, 1].
        /// Multiply by 100 to get the percentage displayed in UI.
        /// </summary>
        public float critChance;

        /// <summary>
        /// How fast this character fills enemy stun/pressure meters after all modifiers.
        /// Known as PRS (Pressure Rate) in design docs; always StunRate in code.
        /// </summary>
        public float stunRate;

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the correct attack multiplier for the given attack mode.
        ///
        /// <para>Handles the Viper-only <c>rangedAttack</c> sentinel (-1f) internally.
        /// Non-Viper characters automatically fall back to <c>attack</c> for
        /// <see cref="AttackMode.Ranged"/> queries — callers never see -1f.</para>
        /// </summary>
        /// <param name="mode">The type of attack being executed.</param>
        /// <returns>The final damage multiplier to use for this attack type.</returns>
        public float GetAttackForMode(AttackMode mode) => mode switch
        {
            AttackMode.Melee     => attack,
            AttackMode.Ranged    => rangedAttack >= 0f ? rangedAttack : attack,
            AttackMode.Throwable => throwableAttack,
            _                    => attack,
        };
    }
}
