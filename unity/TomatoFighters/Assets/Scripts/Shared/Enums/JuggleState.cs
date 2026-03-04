namespace TomatoFighters.Shared.Enums
{
    /// <summary>
    /// Tracks an entity's airborne/grounded state for the juggle system.
    /// Used by JuggleSystem (Combat) and queried by EnemyBase (World) via IJuggleTarget.
    /// </summary>
    public enum JuggleState
    {
        /// <summary>On the ground, normal state.</summary>
        Grounded,

        /// <summary>Launched into the air, rising or at apex.</summary>
        Airborne,

        /// <summary>Falling back toward the ground after apex.</summary>
        Falling,

        /// <summary>On The Ground — knocked down, vulnerable to OTG-capable attacks.</summary>
        OTG,

        /// <summary>Recovery window where the entity can "tech" (get up with brief invulnerability).</summary>
        TechRecover
    }
}
