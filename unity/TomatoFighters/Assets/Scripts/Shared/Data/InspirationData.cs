using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// ScriptableObject defining a single inspiration — a character-specific,
    /// path-locked unlock that grants either a stat bonus or an ability enhancement.
    /// 24 total: 4 characters x 3 paths x 2 per path (1 stat + 1 ability).
    /// </summary>
    [CreateAssetMenu(menuName = "TomatoFighters/Inspiration")]
    public class InspirationData : ScriptableObject
    {
        [Header("Identity")]
        public string inspirationId;
        public string displayName;
        [TextArea(2, 4)] public string description;

        [Header("Ownership")]
        public CharacterType character;
        public PathType path;

        [Header("Effect")]
        public InspirationEffectType effectType;

        [Header("Stat Modifier (if StatModifier type)")]
        public StatType statType;
        public ModifierType modifierType;
        public float value;

        [Header("Ability Modifier (if AbilityModifier type)")]
        public string abilityModifierId;

        [Header("Permanent Unlock")]
        [Tooltip("Primordial Seeds cost. 0 = not permanently unlockable.")]
        [Min(0)] public int permanentUnlockCost;

        [Header("UI")]
        public Sprite icon;
    }
}
