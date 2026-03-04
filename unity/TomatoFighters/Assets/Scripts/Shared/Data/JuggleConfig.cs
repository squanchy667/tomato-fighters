using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Tuning parameters for the juggle system. All values configurable in Inspector.
    /// Covers gravity simulation, wall bounce physics, and OTG/Tech recovery timing.
    /// </summary>
    [CreateAssetMenu(fileName = "New JuggleConfig", menuName = "TomatoFighters/Combat/JuggleConfig")]
    public class JuggleConfig : ScriptableObject
    {
        [Header("Gravity")]
        [Tooltip("Downward acceleration applied to airborne entities (units/s^2).")]
        public float juggleGravity = 25f;

        [Tooltip("Maximum downward velocity during fall.")]
        public float terminalFallSpeed = 20f;

        [Header("Launch")]
        [Tooltip("Minimum upward velocity required to enter airborne state.")]
        public float minLaunchSpeed = 3f;

        [Header("Wall Bounce")]
        [Tooltip("Fraction of velocity retained after bouncing off a wall (0-1).")]
        [Range(0f, 1f)]
        public float bounceVelocityRetention = 0.7f;

        [Tooltip("Minimum Rigidbody2D velocity magnitude to trigger a wall bounce.")]
        public float minBounceVelocity = 3f;

        [Tooltip("Minor damage dealt on each wall bounce (no pressure fill).")]
        public float wallBounceDamage = 2f;

        [Header("OTG / Tech")]
        [Tooltip("Duration in seconds the entity stays in OTG (knocked down) state.")]
        public float otgDuration = 1.0f;

        [Tooltip("Duration in seconds of the tech recovery window.")]
        public float techRecoverDuration = 0.4f;

        [Header("Knockback")]
        [Tooltip("Duration in seconds before knockback velocity is cleared.")]
        public float knockbackRecoveryTime = 0.5f;
    }
}
