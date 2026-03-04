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

        /// <summary>Whether the entity is currently in a clash window (HeavyStartup state).</summary>
        bool IsInClashWindow { get; }

        /// <summary>
        /// Opens a clash window manually. Used by entities without a ComboController
        /// (e.g. enemies open this during telegraph).
        /// </summary>
        void OpenClashWindow(float duration, Vector2 facingDirection);

        /// <summary>
        /// Notify that a defense was successful. Fires defense events (OnDeflect/OnClash/OnDodge)
        /// so visual feedback systems can respond. Called by hit resolvers after
        /// <see cref="ResolveDefense"/> returns a non-Hit result.
        /// </summary>
        void NotifyDefenseSuccess(DamageResponse response, float incomingDamage, DamageType incomingType);
    }
}
