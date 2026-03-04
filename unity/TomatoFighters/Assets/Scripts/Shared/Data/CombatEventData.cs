using TomatoFighters.Shared.Enums;
using UnityEngine;

namespace TomatoFighters.Shared.Data
{
    /// <summary>Data for a basic strike hit in a combo chain.</summary>
    public readonly struct StrikeEventData
    {
        public readonly CharacterType character;
        public readonly float damage;
        public readonly DamageType damageType;
        public readonly int comboIndex;
        public readonly Vector2 hitPosition;

        public StrikeEventData(CharacterType character, float damage, DamageType damageType, int comboIndex, Vector2 hitPosition)
        {
            this.character = character;
            this.damage = damage;
            this.damageType = damageType;
            this.comboIndex = comboIndex;
            this.hitPosition = hitPosition;
        }
    }

    /// <summary>Data for a skill ability hit.</summary>
    public readonly struct SkillEventData
    {
        public readonly CharacterType character;
        public readonly float damage;
        public readonly DamageType damageType;
        public readonly Vector2 hitPosition;

        public SkillEventData(CharacterType character, float damage, DamageType damageType, Vector2 hitPosition)
        {
            this.character = character;
            this.damage = damage;
            this.damageType = damageType;
            this.hitPosition = hitPosition;
        }
    }

    /// <summary>Data for an arcana (mana-based ultimate) activation.</summary>
    public readonly struct ArcanaEventData
    {
        public readonly CharacterType character;
        public readonly string arcanaId;
        public readonly float damage;
        public readonly DamageType damageType;
        public readonly float manaCost;

        public ArcanaEventData(CharacterType character, string arcanaId, float damage, DamageType damageType, float manaCost)
        {
            this.character = character;
            this.arcanaId = arcanaId;
            this.damage = damage;
            this.damageType = damageType;
            this.manaCost = manaCost;
        }
    }

    /// <summary>Data for a dash action.</summary>
    public readonly struct DashEventData
    {
        public readonly CharacterType character;
        public readonly Vector2 dashDirection;
        public readonly bool hasIFrames;

        public DashEventData(CharacterType character, Vector2 dashDirection, bool hasIFrames)
        {
            this.character = character;
            this.dashDirection = dashDirection;
            this.hasIFrames = hasIFrames;
        }
    }

    /// <summary>Data for a successful deflect against an incoming attack.</summary>
    public readonly struct DeflectEventData
    {
        public readonly CharacterType character;
        public readonly float incomingDamage;
        public readonly DamageType incomingType;

        public DeflectEventData(CharacterType character, float incomingDamage, DamageType incomingType)
        {
            this.character = character;
            this.incomingDamage = incomingDamage;
            this.incomingType = incomingType;
        }
    }

    /// <summary>Data for a clash (simultaneous hit exchange).</summary>
    public readonly struct ClashEventData
    {
        public readonly CharacterType character;
        public readonly float incomingDamage;
        public readonly DamageType incomingType;

        public ClashEventData(CharacterType character, float incomingDamage, DamageType incomingType)
        {
            this.character = character;
            this.incomingDamage = incomingDamage;
            this.incomingType = incomingType;
        }
    }

    /// <summary>Data for a punish hit during an enemy's vulnerable window.</summary>
    public readonly struct PunishEventData
    {
        public readonly CharacterType character;
        public readonly float damage;
        public readonly DamageType damageType;
        public readonly Vector2 hitPosition;

        public PunishEventData(CharacterType character, float damage, DamageType damageType, Vector2 hitPosition)
        {
            this.character = character;
            this.damage = damage;
            this.damageType = damageType;
            this.hitPosition = hitPosition;
        }
    }

    /// <summary>Data for an enemy kill.</summary>
    public readonly struct KillEventData
    {
        public readonly CharacterType character;
        public readonly Vector2 killPosition;
        public readonly DamageType killingDamageType;

        public KillEventData(CharacterType character, Vector2 killPosition, DamageType killingDamageType)
        {
            this.character = character;
            this.killPosition = killPosition;
            this.killingDamageType = killingDamageType;
        }
    }

    /// <summary>Data for a finisher (combo ender with bonus damage).</summary>
    public readonly struct FinisherEventData
    {
        public readonly CharacterType character;
        public readonly float damage;
        public readonly DamageType damageType;
        public readonly int comboLength;
        public readonly Vector2 hitPosition;

        public FinisherEventData(CharacterType character, float damage, DamageType damageType, int comboLength, Vector2 hitPosition)
        {
            this.character = character;
            this.damage = damage;
            this.damageType = damageType;
            this.comboLength = comboLength;
            this.hitPosition = hitPosition;
        }
    }

    /// <summary>Data for a jump action.</summary>
    public readonly struct JumpEventData
    {
        public readonly CharacterType character;
        public readonly bool isAirborne;

        public JumpEventData(CharacterType character, bool isAirborne)
        {
            this.character = character;
            this.isAirborne = isAirborne;
        }
    }

    /// <summary>Data for a dodge action.</summary>
    public readonly struct DodgeEventData
    {
        public readonly CharacterType character;
        public readonly Vector2 dodgeDirection;

        public DodgeEventData(CharacterType character, Vector2 dodgeDirection)
        {
            this.character = character;
            this.dodgeDirection = dodgeDirection;
        }
    }

    /// <summary>Data for when a character takes damage, including how they responded.</summary>
    public readonly struct TakeDamageEventData
    {
        public readonly CharacterType character;
        public readonly float damageAmount;
        public readonly DamageType damageType;
        public readonly DamageResponse response;

        public TakeDamageEventData(CharacterType character, float damageAmount, DamageType damageType, DamageResponse response)
        {
            this.character = character;
            this.damageAmount = damageAmount;
            this.damageType = damageType;
            this.response = response;
        }
    }

    /// <summary>Data for when a path ability is used in combat.</summary>
    public readonly struct PathAbilityEventData
    {
        public readonly CharacterType character;
        public readonly string abilityId;
        public readonly PathType pathType;
        public readonly int tier;
        public readonly float manaCost;

        public PathAbilityEventData(CharacterType character, string abilityId, PathType pathType, int tier, float manaCost)
        {
            this.character = character;
            this.abilityId = abilityId;
            this.pathType = pathType;
            this.tier = tier;
            this.manaCost = manaCost;
        }
    }

    /// <summary>Data for when an enemy becomes stunned (pressure meter full).</summary>
    public readonly struct StunEventData
    {
        public readonly CharacterType lastHitBy;
        public readonly Vector2 stunnedPosition;
        public readonly float stunDuration;

        public StunEventData(CharacterType lastHitBy, Vector2 stunnedPosition, float stunDuration)
        {
            this.lastHitBy = lastHitBy;
            this.stunnedPosition = stunnedPosition;
            this.stunDuration = stunDuration;
        }
    }

    /// <summary>Data for when an enemy recovers from stun.</summary>
    public readonly struct StunRecoveredEventData
    {
        public readonly Vector2 recoveredPosition;

        public StunRecoveredEventData(Vector2 recoveredPosition)
        {
            this.recoveredPosition = recoveredPosition;
        }
    }

    /// <summary>Data for a wall bounce during knockback.</summary>
    public readonly struct WallBounceEventData
    {
        public readonly Vector2 bouncePosition;
        public readonly float damage;
        public readonly Vector2 reflectedVelocity;

        public WallBounceEventData(Vector2 bouncePosition, float damage, Vector2 reflectedVelocity)
        {
            this.bouncePosition = bouncePosition;
            this.damage = damage;
            this.reflectedVelocity = reflectedVelocity;
        }
    }

    /// <summary>Data for when an entity lands from airborne state.</summary>
    public readonly struct JuggleLandEventData
    {
        public readonly Vector2 landPosition;
        public readonly Enums.JuggleState landedIntoState;

        public JuggleLandEventData(Vector2 landPosition, Enums.JuggleState landedIntoState)
        {
            this.landPosition = landPosition;
            this.landedIntoState = landedIntoState;
        }
    }
}
