using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Pure data container holding the base stats and passive identity for one character.
    /// No logic lives here — this is fed into <c>CharacterStatCalculator</c> (T007) which
    /// layers path bonuses, ritual multipliers, trinket modifiers, and soul tree bonuses on top.
    ///
    /// Referenced via [SerializeField] injection only. Never use as a singleton.
    /// All 3 pillars read stats through IPathProvider / IBuffProvider, not directly.
    /// </summary>
    [CreateAssetMenu(menuName = "TomatoFighters/Data/Character Base Stats",
                     fileName = "NewCharacterBaseStats")]
    public class CharacterBaseStats : ScriptableObject
    {
        // ── Identity ─────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Which playable character these stats belong to.")]
        public CharacterType characterType;

        [Tooltip("Key used to locate this character's passive ability class " +
                 "(e.g. 'ThickSkin', 'Bloodlust', 'ArcaneResonance', 'DistanceBonus').")]
        public string passiveAbilityId;

        [TextArea(2, 4)]
        [Tooltip("Designer notes about this character's stat identity. Not used at runtime.")]
        public string description;

        // ── Vitals ───────────────────────────────────────────────────────────

        [Header("Vitals")]
        [Range(50, 200)]
        [Tooltip("Total hit points.")]
        public int health = 100;

        [Range(0, 30)]
        [Tooltip("Flat damage reduction applied per hit received.")]
        public int defense = 10;

        // ── Attack ───────────────────────────────────────────────────────────
        // attack          → StatType.Attack          (melee)
        // rangedAttack    → StatType.RangedAttack    (Viper projectiles ONLY; -1 for non-Viper)
        // throwableAttack → StatType.ThrowableAttack (ground-pickup items, ALL characters)

        [Header("Attack")]
        [Range(0.1f, 2.0f)]
        [Tooltip("Melee damage multiplier. Used for all physical close-range attacks.")]
        public float attack = 1.0f;

        [Range(-1f, 2.0f)]
        [Tooltip("Ranged projectile damage multiplier. VIPER ONLY.\n" +
                 "Set to -1 for all non-Viper characters to mark this stat as inactive.\n" +
                 "The stat calculator skips calculation when this value is negative.\n" +
                 "Maps to StatType.RangedAttack.")]
        public float rangedAttack = -1f;

        [Range(0.5f, 1.5f)]
        [Tooltip("Throwable item damage multiplier. Used by ALL characters when picking up " +
                 "and throwing ground items (bottles, rocks, crates, barrels, etc.).\n" +
                 "Independent of both melee and ranged attack stats.\n" +
                 "Maps to StatType.ThrowableAttack.")]
        public float throwableAttack = 0.9f;

        // ── Mobility ─────────────────────────────────────────────────────────

        [Header("Mobility")]
        [Range(0.7f, 1.5f)]
        [Tooltip("Movement speed and dash distance multiplier.")]
        public float speed = 1.0f;

        // ── Mana ─────────────────────────────────────────────────────────────

        [Header("Mana")]
        [Range(30, 150)]
        [Tooltip("Total mana pool for Arcana abilities and mana-gated path skills.")]
        public int mana = 60;

        [Range(1f, 8f)]
        [Tooltip("Mana restored per second passively.")]
        public float manaRegen = 3f;

        // ── Combat ───────────────────────────────────────────────────────────

        [Header("Combat")]
        [Range(0f, 0.25f)]
        [Tooltip("Chance to deal 1.5x damage on any hit. Stored as decimal: 0.05 = 5%.\n" +
                 "Maps to StatType.CritChance.")]
        public float critChance = 0.05f;

        [Range(0.5f, 2.0f)]
        [Tooltip("How fast this character fills enemy stun/pressure meters. " +
                 "1.0 = baseline. Slasher (1.5) staggers quickly; Mystica (0.5) barely.\n" +
                 "Maps to StatType.StunRate. Design docs label this 'PRS' (Pressure Rate).")]
        public float stunRate = 1.0f;

        /// <summary>
        /// Returns the base value for the given <paramref name="stat"/>.
        /// Used by <c>TrinketStackCalculator</c> to convert flat modifiers into multipliers.
        /// Returns 0f for <c>StatType.CancelWindow</c> (no base value defined).
        /// </summary>
        public float GetStat(StatType stat)
        {
            return stat switch
            {
                StatType.Health         => health,
                StatType.Defense        => defense,
                StatType.Attack         => attack,
                StatType.RangedAttack   => rangedAttack,
                StatType.ThrowableAttack => throwableAttack,
                StatType.Speed          => speed,
                StatType.Mana           => mana,
                StatType.ManaRegen      => manaRegen,
                StatType.CritChance     => critChance,
                StatType.StunRate       => stunRate,
                _                       => 0f
            };
        }
    }
}
