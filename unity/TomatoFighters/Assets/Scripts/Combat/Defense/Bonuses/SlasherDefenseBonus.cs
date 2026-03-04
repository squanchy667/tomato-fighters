using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Slasher's defense bonus: grants critical hit on the next attack after a successful defense.
    /// Sets a flag that the combo/damage system can query.
    /// </summary>
    [CreateAssetMenu(menuName = "TomatoFighters/Combat/DefenseBonus/Slasher")]
    public class SlasherDefenseBonus : DefenseBonus
    {
        /// <summary>
        /// Whether a guaranteed crit is pending. Consumed by the damage pipeline on next hit.
        /// Reset to false after consumption.
        /// </summary>
        [System.NonSerialized]
        public bool guaranteedCritPending;

        /// <inheritdoc/>
        public override void Apply(DefenseContext context, DamageResponse responseType)
        {
            guaranteedCritPending = true;
            Debug.Log("[SlasherDefenseBonus] Guaranteed crit on next attack.");
        }
    }
}
