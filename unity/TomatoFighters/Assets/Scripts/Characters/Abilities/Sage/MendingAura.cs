using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Sage
{
    /// <summary>
    /// Sage T1 toggle: drains 3 mana/s and heals self 3% max HP/s.
    /// Self-target fallback (DD-6) — heals self only until co-op exists.
    /// Toggle on/off with ability key.
    /// </summary>
    public class MendingAura : IPathAbility
    {
        private const string ID = "Sage_MendingAura";
        private const float MANA_DRAIN_PER_SECOND = 3f;
        private const float HEAL_PERCENT_PER_SECOND = 0.03f;

        private readonly PathAbilityContext _ctx;
        private readonly GameObject _vfxPrefab;
        private GameObject _activeVfx;
        private bool _isActive;

        public MendingAura(PathAbilityContext ctx)
        {
            _ctx = ctx;
            _vfxPrefab = ctx.VfxPrefab;
        }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Toggle;
        public float ManaCost => 0f; // Drained per-second in Tick, not per-activation
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        public bool TryActivate()
        {
            // Need enough mana to sustain at least briefly
            if (_ctx.ManaTracker.CurrentMana < MANA_DRAIN_PER_SECOND * 0.5f)
            {
                Debug.Log("[MendingAura] Not enough mana to activate.");
                return false;
            }

            _isActive = true;

            // Sustained aura VFX — green heal particles, parented to player
            if (_vfxPrefab != null)
                _activeVfx = Object.Instantiate(_vfxPrefab, _ctx.PlayerTransform);

            Debug.Log("[MendingAura] Aura activated — healing self, draining mana");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (!_isActive) return;

            // Drain mana
            float manaCost = MANA_DRAIN_PER_SECOND * deltaTime;
            if (!_ctx.ManaTracker.TryConsume(manaCost))
            {
                // Out of mana — auto-deactivate
                Cleanup();
                Debug.Log("[MendingAura] Out of mana — deactivated");
                return;
            }

            // Heal self (restore via PlayerDamageable placeholder — uses negative damage concept)
            if (_ctx.PlayerDamageable != null)
            {
                float healAmount = _ctx.PlayerDamageable.MaxHealth * HEAL_PERCENT_PER_SECOND * deltaTime;
                // PlayerDamageable doesn't have a Heal method yet — log for now
                Debug.Log($"[MendingAura] Would heal {healAmount:F2} HP (awaiting heal API)");
            }
        }

        public void Cleanup()
        {
            _isActive = false;
            if (_activeVfx != null)
                Object.Destroy(_activeVfx);
        }
    }
}
