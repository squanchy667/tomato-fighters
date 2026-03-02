namespace TomatoFighters.Shared.Enums
{
    /// <summary>
    /// Combat actions that can trigger ritual effects via ICombatEvents.
    /// </summary>
    public enum RitualTrigger
    {
        OnStrike,
        OnSkill,
        OnDash,
        OnDeflect,
        OnClash,
        OnFinisher,
        OnKill,
        OnArcana,
        OnJump,
        OnDodge,
        OnTakeDamage
    }
}
