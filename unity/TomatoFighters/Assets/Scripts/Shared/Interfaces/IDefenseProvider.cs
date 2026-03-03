using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Shared.Interfaces
{
    /// <summary>
    /// Cross-pillar interface for defense resolution.
    /// Combat pillar implements this on <c>DefenseSystem</c>;
    /// World pillar queries it on enemies via this interface to avoid cross-pillar imports.
    /// </summary>
    public interface IDefenseProvider
    {
        /// <summary>
        /// Resolve an incoming attack against the entity's current defense state.
        /// </summary>
        /// <param name="attackerPosition">World-space position of the attacker.</param>
        /// <param name="isUnstoppable">Whether the incoming attack is unstoppable.</param>
        /// <returns>The defense outcome.</returns>
        DamageResponse ResolveDefense(Vector2 attackerPosition, bool isUnstoppable);
    }
}
