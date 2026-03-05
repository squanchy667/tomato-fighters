using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Enchanter
{
    /// <summary>
    /// Enchanter T2 passive: when Empower (T1) is used, the target's attacks
    /// are also infused with the dominant ritual element for the buff duration.
    /// Infused attacks trigger that element's ritual effects.
    /// Requires RitualSystem integration for element resolution.
    /// </summary>
    public class ElementalInfusion : IPathAbility
    {
        private const string ID = "Enchanter_ElementalInfusion";

        private bool _isActive;
        private bool _infusionActive;

        public ElementalInfusion(PathAbilityContext ctx) { }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Passive;
        public float ManaCost => 0f;
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        /// <summary>Whether elemental infusion is currently active on the buff target.</summary>
        public bool InfusionActive => _infusionActive;

        /// <summary>
        /// Called by Empower when the buff is applied. Activates elemental infusion
        /// for the buff duration. RitualSystem determines the dominant element.
        /// </summary>
        public void OnEmpowerActivated(float duration)
        {
            if (!_isActive) return;
            _infusionActive = true;
            Debug.Log($"[ElementalInfusion] Infusion active for {duration:F1}s (element from dominant ritual family)");
        }

        /// <summary>Called when the Empower buff expires.</summary>
        public void OnEmpowerExpired()
        {
            _infusionActive = false;
        }

        public bool TryActivate()
        {
            _isActive = true;
            Debug.Log("[ElementalInfusion] Passive ready — Empower will infuse attacks with ritual element");
            return true;
        }

        public void Tick(float deltaTime) { }

        public void Cleanup()
        {
            _isActive = false;
            _infusionActive = false;
        }
    }
}
