namespace TomatoFighters.Combat
{
    /// <summary>
    /// The active defense posture of an entity at the moment of impact.
    /// Used by <see cref="DefenseResolver"/> to determine the <see cref="Shared.Enums.DamageResponse"/>.
    /// </summary>
    public enum DefenseState
    {
        /// <summary>No defensive action — takes full damage.</summary>
        None,

        /// <summary>Currently dashing — may deflect (toward) or dodge (vertical).</summary>
        Dashing,

        /// <summary>In the startup frames of a heavy attack — may clash if facing attacker.</summary>
        HeavyStartup
    }
}
