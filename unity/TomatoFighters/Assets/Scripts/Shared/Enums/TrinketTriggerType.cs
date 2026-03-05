namespace TomatoFighters.Shared.Enums
{
    /// <summary>
    /// Condition under which a trinket's stat modifier activates.
    /// <c>Always</c> trinkets are permanently active; all others activate as timed buffs.
    /// </summary>
    public enum TrinketTriggerType
    {
        /// <summary>Modifier is always active while the trinket is equipped.</summary>
        Always,

        /// <summary>Activates when the player dodges an attack.</summary>
        OnDodge,

        /// <summary>Activates when the player kills an enemy.</summary>
        OnKill,

        /// <summary>Activates when the player successfully deflects.</summary>
        OnDeflect,

        /// <summary>Activates when the player lands a finisher.</summary>
        OnFinisher
    }
}
