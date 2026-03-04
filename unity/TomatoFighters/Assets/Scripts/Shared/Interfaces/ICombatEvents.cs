using System;
using TomatoFighters.Shared.Data;

namespace TomatoFighters.Shared.Interfaces
{
    /// <summary>
    /// Combat pillar fires these events; Roguelite subscribes to trigger rituals and track stats.
    /// Implemented by the combat system, consumed by ritual/buff systems.
    /// </summary>
    public interface ICombatEvents
    {
        /// <summary>Fired when a basic strike lands in a combo chain.</summary>
        event Action<StrikeEventData> OnStrike;

        /// <summary>Fired when a skill ability hits.</summary>
        event Action<SkillEventData> OnSkill;

        /// <summary>Fired when an arcana (mana ultimate) activates.</summary>
        event Action<ArcanaEventData> OnArcana;

        /// <summary>Fired when the player dashes.</summary>
        event Action<DashEventData> OnDash;

        /// <summary>Fired when the player successfully deflects an attack.</summary>
        event Action<DeflectEventData> OnDeflect;

        /// <summary>Fired when the player clashes with an enemy attack.</summary>
        event Action<ClashEventData> OnClash;

        /// <summary>Fired when the player lands a punish hit during an enemy's vulnerable window.</summary>
        event Action<PunishEventData> OnPunish;

        /// <summary>Fired when an enemy is killed.</summary>
        event Action<KillEventData> OnKill;

        /// <summary>Fired when a finisher (combo ender) lands.</summary>
        event Action<FinisherEventData> OnFinisher;

        /// <summary>Fired when the player jumps.</summary>
        event Action<JumpEventData> OnJump;

        /// <summary>Fired when the player dodges.</summary>
        event Action<DodgeEventData> OnDodge;

        /// <summary>Fired when the player takes damage, including the response type.</summary>
        event Action<TakeDamageEventData> OnTakeDamage;

        /// <summary>Fired when a path ability is used in combat.</summary>
        event Action<PathAbilityEventData> OnPathAbilityUsed;

        /// <summary>Fired when an enemy's pressure meter fills and they become stunned.</summary>
        event Action<StunEventData> OnStun;

        /// <summary>Fired when an enemy recovers from stun (before invulnerability blink).</summary>
        event Action<StunRecoveredEventData> OnStunRecovered;
    }
}
