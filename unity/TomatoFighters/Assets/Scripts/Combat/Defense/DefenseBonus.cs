using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Abstract base for character-specific defense bonuses.
    /// Each character gets one <see cref="DefenseBonus"/> SO assigned in their <see cref="DefenseConfig"/>.
    /// <see cref="DefenseSystem"/> calls <see cref="Apply"/> after a successful defense action.
    /// </summary>
    public abstract class DefenseBonus : ScriptableObject
    {
        /// <summary>
        /// Called after a successful defense action. Override to apply character-specific effects.
        /// </summary>
        /// <param name="context">Who defended, who attacked, and what damage was incoming.</param>
        /// <param name="responseType">Which defense succeeded (Deflected, Clashed, or Dodged).</param>
        public abstract void Apply(DefenseContext context, DamageResponse responseType);
    }
}
