namespace TomatoFighters.Shared.Enums
{
    /// <summary>
    /// How a path ability is activated and sustained.
    /// </summary>
    public enum AbilityActivationType
    {
        /// <summary>Press to activate, runs for a duration or instant effect.</summary>
        Active,

        /// <summary>Press to toggle on/off. Drains resource while active.</summary>
        Toggle,

        /// <summary>Hold to charge/channel. Effect scales with charge time.</summary>
        Channeled,

        /// <summary>Always active when unlocked. Modifies combat calculations.</summary>
        Passive
    }
}
