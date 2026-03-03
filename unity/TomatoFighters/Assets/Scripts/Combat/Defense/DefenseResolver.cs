using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Pure testable resolution logic for defense actions.
    /// No MonoBehaviour, no Unity lifecycle dependencies.
    /// Determines the <see cref="DamageResponse"/> based on the defender's current state,
    /// action direction, and whether the incoming attack is unstoppable.
    /// </summary>
    public class DefenseResolver
    {
        /// <summary>
        /// Angle threshold in degrees for "facing toward" checks.
        /// If the angle between the action direction and to-attacker direction
        /// is less than this, the defender is considered facing the attacker.
        /// </summary>
        private const float FACING_THRESHOLD_DEGREES = 90f;

        /// <summary>
        /// Threshold in degrees for vertical dash detection.
        /// If the absolute angle from the Y-axis is less than this,
        /// the dash is considered vertical (up/down).
        /// </summary>
        private const float VERTICAL_THRESHOLD_DEGREES = 45f;

        /// <summary>
        /// Resolves what happens when an attack reaches a defender.
        /// </summary>
        /// <param name="currentState">The defender's current defense posture.</param>
        /// <param name="actionDirection">
        /// Direction of the defensive action:
        /// dash direction (for Dashing) or facing direction (for HeavyStartup).
        /// </param>
        /// <param name="toAttackerDirection">
        /// Normalized direction from defender to attacker.
        /// </param>
        /// <param name="isAttackUnstoppable">
        /// Whether the incoming attack has <see cref="TelegraphType.Unstoppable"/>.
        /// Unstoppable attacks bypass deflect and clash, but not dodge.
        /// </param>
        /// <returns>The resulting <see cref="DamageResponse"/>.</returns>
        public DamageResponse Resolve(
            DefenseState currentState,
            Vector2 actionDirection,
            Vector2 toAttackerDirection,
            bool isAttackUnstoppable)
        {
            if (currentState == DefenseState.None)
                return DamageResponse.Hit;

            if (currentState == DefenseState.Dashing)
            {
                // Dodge: vertical dash — always works, even against unstoppable
                if (IsVertical(actionDirection))
                    return DamageResponse.Dodged;

                // Unstoppable attacks bypass deflect
                if (isAttackUnstoppable)
                    return DamageResponse.Hit;

                // Deflect: dash toward the attacker
                if (IsFacing(actionDirection, toAttackerDirection))
                    return DamageResponse.Deflected;

                // Dashing away — no defensive benefit
                return DamageResponse.Hit;
            }

            if (currentState == DefenseState.HeavyStartup)
            {
                // Unstoppable attacks bypass clash
                if (isAttackUnstoppable)
                    return DamageResponse.Hit;

                // Clash: facing toward the attacker during heavy startup
                if (IsFacing(actionDirection, toAttackerDirection))
                    return DamageResponse.Clashed;

                // Heavy attack facing away — no defensive benefit
                return DamageResponse.Hit;
            }

            return DamageResponse.Hit;
        }

        /// <summary>
        /// Checks whether <paramref name="direction"/> is approximately vertical (up or down).
        /// </summary>
        public static bool IsVertical(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f) return false;

            // Angle from the Y-axis (up). Both up and down count as vertical.
            float angleFromUp = Vector2.Angle(direction, Vector2.up);
            float angleFromDown = Vector2.Angle(direction, Vector2.down);
            float minAngle = Mathf.Min(angleFromUp, angleFromDown);

            return minAngle <= VERTICAL_THRESHOLD_DEGREES;
        }

        /// <summary>
        /// Checks whether <paramref name="actionDir"/> points approximately toward
        /// <paramref name="toAttackerDir"/> (within <see cref="FACING_THRESHOLD_DEGREES"/>).
        /// </summary>
        public static bool IsFacing(Vector2 actionDir, Vector2 toAttackerDir)
        {
            if (actionDir.sqrMagnitude < 0.001f || toAttackerDir.sqrMagnitude < 0.001f)
                return false;

            float angle = Vector2.Angle(actionDir, toAttackerDir);
            return angle <= FACING_THRESHOLD_DEGREES;
        }
    }
}
