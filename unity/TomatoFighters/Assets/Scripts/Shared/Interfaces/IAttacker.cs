using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;

namespace TomatoFighters.Shared.Interfaces
{
    /// <summary>
    /// World pillar implements this on enemies; Combat pillar reads it to determine defensive options.
    /// Exposes the attacker's current state for deflect/clash/punish decisions.
    /// </summary>
    public interface IAttacker
    {
        /// <summary>The currently active attack data. Null if not attacking.</summary>
        object CurrentAttack { get; } // TODO: Replace with AttackData when T005 lands

        /// <summary>Whether the current attack is unstoppable (cannot be deflected).</summary>
        bool IsCurrentAttackUnstoppable { get; }

        /// <summary>The telegraph type of the current attack (Normal or Unstoppable).</summary>
        TelegraphType CurrentTelegraphType { get; }

        /// <summary>Duration in seconds of the punish window after a deflect.</summary>
        float PunishWindowDuration { get; }

        /// <summary>Whether the attacker is currently in a punishable state.</summary>
        bool IsInPunishableState { get; }
    }
}
