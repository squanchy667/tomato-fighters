using TomatoFighters.Shared.Enums;

namespace TomatoFighters.Shared.Interfaces
{
    /// <summary>
    /// Common interface for all path abilities. PathAbilityExecutor manages these.
    /// Implementations live in Combat pillar (Characters/Abilities/).
    /// </summary>
    public interface IPathAbility
    {
        /// <summary>Unique ability identifier matching PathData.tierXAbilityId.</summary>
        string AbilityId { get; }

        /// <summary>How this ability is activated (Active, Toggle, Channeled, Passive).</summary>
        AbilityActivationType ActivationType { get; }

        /// <summary>Mana cost per activation (or per-second for toggles/channels).</summary>
        float ManaCost { get; }

        /// <summary>Cooldown duration in seconds. 0 for passives.</summary>
        float Cooldown { get; }

        /// <summary>Whether this ability is currently active (toggled on, channeling, etc.).</summary>
        bool IsActive { get; }

        /// <summary>Remaining cooldown in seconds. 0 when ready.</summary>
        float CooldownRemaining { get; }

        /// <summary>
        /// Attempt to activate the ability. Returns true if activation succeeded.
        /// Mana check is done by PathAbilityExecutor before calling this.
        /// </summary>
        bool TryActivate();

        /// <summary>
        /// Called every frame while the ability is active. Handles toggles, channels, passives.
        /// </summary>
        void Tick(float deltaTime);

        /// <summary>
        /// Force-deactivate the ability. Called on path change, death, etc.
        /// </summary>
        void Cleanup();
    }
}
