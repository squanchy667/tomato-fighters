using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Mystica's defense bonus: restores mana on successful defense.
    /// Amount is configurable via the Inspector on the SO asset.
    /// </summary>
    [CreateAssetMenu(menuName = "TomatoFighters/Combat/DefenseBonus/Mystica")]
    public class MysticaDefenseBonus : DefenseBonus
    {
        [Tooltip("Flat mana restored per successful defense.")]
        [Range(1f, 50f)]
        [SerializeField] private float manaRestored = 15f;

        /// <summary>Amount of mana restored per defense.</summary>
        public float ManaRestored => manaRestored;

        /// <inheritdoc/>
        public override void Apply(DefenseContext context, DamageResponse responseType)
        {
            // Mana system integration will consume this value.
            // For now, log the intent — the mana system (T017+) will wire this.
            Debug.Log($"[MysticaDefenseBonus] Restore {manaRestored} mana on {responseType}.");
        }
    }
}
