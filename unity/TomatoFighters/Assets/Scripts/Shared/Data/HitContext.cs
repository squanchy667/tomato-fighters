using TomatoFighters.Shared.Enums;

namespace TomatoFighters.Shared.Data
{
    /// <summary>
    /// Per-hit context passed to passive abilities for damage calculation.
    /// Keeps passives decoupled from transforms and scene queries — the hit
    /// handler calculates distance and passes context.
    /// </summary>
    public struct HitContext
    {
        /// <summary>Element type of the damage dealt.</summary>
        public DamageType damageType;

        /// <summary>Distance from attacker to target at hit time (units).</summary>
        public float distanceToTarget;

        /// <summary>Whether this hit occurred during a punish window.</summary>
        public bool isPunishHit;
    }
}
