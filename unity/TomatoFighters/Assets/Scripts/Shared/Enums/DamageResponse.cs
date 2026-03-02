namespace TomatoFighters.Shared.Enums
{
    /// <summary>
    /// How a character responded to incoming damage. Used in TakeDamageEventData.
    /// </summary>
    public enum DamageResponse
    {
        Hit,
        Deflected,
        Clashed,
        Dodged
    }
}
