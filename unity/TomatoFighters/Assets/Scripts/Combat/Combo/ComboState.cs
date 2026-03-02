namespace TomatoFighters.Combat
{
    /// <summary>
    /// States for the combo state machine.
    /// Transitions are animation-event-driven except ComboWindow timeout.
    /// </summary>
    public enum ComboState
    {
        /// <summary>Not attacking. Combo chain is reset.</summary>
        Idle,

        /// <summary>Attack animation playing. Input is buffered.</summary>
        Attacking,

        /// <summary>Brief window after an attack where the next input chains.</summary>
        ComboWindow,

        /// <summary>Finisher animation playing. Locked until complete.</summary>
        Finisher
    }
}
