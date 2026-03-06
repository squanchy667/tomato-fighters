using TomatoFighters.Combat;
using TomatoFighters.Shared.Components;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities
{
    /// <summary>
    /// Dependency bundle passed to all path abilities on creation.
    /// Avoids each ability needing [SerializeField] injection or GetComponent calls.
    /// </summary>
    public class PathAbilityContext
    {
        /// <summary>Character motor for movement modifications (PhaseDash, IronGuard).</summary>
        public CharacterMotor Motor { get; set; }

        /// <summary>Combo controller for attack state queries.</summary>
        public ComboController ComboController { get; set; }

        /// <summary>Hitbox manager for passive modifier integration.</summary>
        public HitboxManager HitboxManager { get; set; }

        /// <summary>Mana tracker for consumption checks.</summary>
        public PlayerManaTracker ManaTracker { get; set; }

        /// <summary>Path provider for tier/unlock queries.</summary>
        public IPathProvider PathProvider { get; set; }

        /// <summary>Player transform for AoE origins and projectile spawning.</summary>
        public Transform PlayerTransform { get; set; }

        /// <summary>Player damageable for self-heal abilities (MendingAura).</summary>
        public PlayerDamageable PlayerDamageable { get; set; }

        /// <summary>Layer mask for enemy hurtbox detection (Provoke AoE, etc.).</summary>
        public LayerMask EnemyLayer { get; set; }

        /// <summary>
        /// VFX prefab for the ability being created. Set by PathAbilityExecutor
        /// before each AbilityFactory.Create() call. Abilities should copy this
        /// to a local field in their constructor since the context is shared.
        /// </summary>
        public GameObject VfxPrefab { get; set; }
    }
}
