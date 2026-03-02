using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Pure data container holding the 8 base stats and passive identity for one character.
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
        // attack     → StatType.Attack       (melee)
        // rangedAttack → StatType.RangedAttack (projectiles + throwables)
        //
        // All characters have both. Non-specialist characters have lower rangedAttack
        // base values but can still use throwable items — rangedAttack scales that damage.

        [Header("Attack")]
        [Range(0.1f, 2.0f)]
        [Tooltip("Melee damage multiplier. Used for all physical close-range attacks.")]
        public float attack = 1.0f;

        [Range(0.1f, 2.0f)]
        [Tooltip("Ranged/throwable damage multiplier. Used by projectile attacks AND " +
                 "throwable items for all characters (future). Viper is the specialist; " +
                 "others have lower defaults but the stat is always present.")]
        public float rangedAttack = 0.5f;

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
    }
}
