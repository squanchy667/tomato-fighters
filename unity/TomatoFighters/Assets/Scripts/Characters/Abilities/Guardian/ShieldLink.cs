using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Guardian
{
    /// <summary>
    /// Guardian T1 active: links to an ally to redirect damage.
    /// Self-target fallback (DD-6): no meaningful effect in solo.
    /// Validates the cooldown/mana/event pipeline for when co-op (T051) arrives.
    /// Cooldown: 12s.
    /// </summary>
    public class ShieldLink : IPathAbility
    {
        private const string ID = "Guardian_ShieldLink";
        private const float COOLDOWN = 12f;

        private float _cooldownRemaining;

        public ShieldLink(PathAbilityContext ctx) { }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Active;
        public float ManaCost => 0f;
        public float Cooldown => COOLDOWN;
        public bool IsActive => false;
        public float CooldownRemaining => _cooldownRemaining;

        public bool TryActivate()
        {
            // No meaningful solo effect — stub for co-op system
            _cooldownRemaining = COOLDOWN;
            Debug.Log("[ShieldLink] Activated (no effect in solo — awaiting co-op T051)");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= deltaTime;
        }

        public void Cleanup()
        {
            _cooldownRemaining = 0f;
        }
    }
}
