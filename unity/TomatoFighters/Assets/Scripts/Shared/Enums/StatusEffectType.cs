namespace TomatoFighters.Shared.Enums
{
    /// <summary>
    /// Types of status effects that can be applied to entities.
    /// Used by abilities (T028) and tracked by StatusEffectTracker (World pillar).
    /// </summary>
    public enum StatusEffectType
    {
        Mark,
        Immobilize,
        Slow,
        Taunt
    }
}
