using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Trapper
{
    /// <summary>
    /// Trapper T2 passive: Harpoon Shot (T1) also deploys an invisible trap at the
    /// impact point. The trap snares the first enemy to walk over it for 2 seconds.
    /// </summary>
    public class TrapNet : IPathAbility
    {
        private const string ID = "Trapper_TrapDeployment";
        private const float SNARE_DURATION = 2f;
        private const float TRAP_LIFETIME = 10f;
        private const float TRAP_RADIUS = 1f;

        private readonly PathAbilityContext _ctx;
        private bool _isActive;
        private GameObject _currentTrap;

        public TrapNet(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Passive;
        public float ManaCost => 0f;
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        /// <summary>Whether trap deployment is enabled. HarpoonProjectile queries this.</summary>
        public bool TrapEnabled => _isActive;

        /// <summary>
        /// Called by HarpoonProjectile on impact to deploy a trap at the hit location.
        /// </summary>
        public void DeployTrapAt(Vector2 position)
        {
            if (!_isActive) return;

            // Clean up previous trap
            if (_currentTrap != null)
                Object.Destroy(_currentTrap);

            _currentTrap = new GameObject("TrapNet");
            _currentTrap.transform.position = position;

            var col = _currentTrap.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = TRAP_RADIUS;

            // Trap auto-destructs after lifetime
            Object.Destroy(_currentTrap, TRAP_LIFETIME);

            Debug.Log($"[TrapNet] Trap deployed at {position}");
        }

        /// <summary>
        /// Called when an enemy enters the trap trigger. Applies Immobilize status.
        /// </summary>
        public void OnTrapTriggered(Collider2D enemy)
        {
            if (!_isActive) return;

            var statusEffectable = enemy.GetComponent<IStatusEffectable>()
                ?? enemy.GetComponentInParent<IStatusEffectable>();

            if (statusEffectable != null)
            {
                statusEffectable.AddEffect(new StatusEffect(
                    StatusEffectType.Immobilize, SNARE_DURATION, 0f, null));
                Debug.Log($"[TrapNet] {enemy.name} snared for {SNARE_DURATION}s!");
            }

            // Trap consumed — destroy it
            if (_currentTrap != null)
                Object.Destroy(_currentTrap);
        }

        public bool TryActivate()
        {
            _isActive = true;
            Debug.Log("[TrapNet] Trap deployment active — Harpoon impacts deploy snare traps");
            return true;
        }

        public void Tick(float deltaTime) { }

        public void Cleanup()
        {
            _isActive = false;
            if (_currentTrap != null)
                Object.Destroy(_currentTrap);
        }
    }
}
