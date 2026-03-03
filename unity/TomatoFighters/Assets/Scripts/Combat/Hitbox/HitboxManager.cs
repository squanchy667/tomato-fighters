using System;
using System.Collections.Generic;
using TomatoFighters.Shared.Components;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Combat
{
    /// <summary>
    /// Orchestrates hitbox activation/deactivation via Animation Events.
    /// Subscribes to <see cref="HitboxDamage"/> events on child hitbox GameObjects,
    /// forwards hit-confirms to <see cref="ComboController"/>, and applies a temporary
    /// damage shim until T016 (DefenseSystem) handles proper resolution.
    /// </summary>
    public class HitboxManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ComboController comboController;

        [Header("Temporary Damage Shim")]
        [Tooltip("TEMPORARY: Base attack value for damage calculation until stat system is wired.")]
        [SerializeField] private float baseAttack = 10f;

        /// <summary>Fired when a hit is detected and processed. Downstream systems can subscribe.</summary>
        public event Action<HitDetectionData> OnHitProcessed;

        // Hitbox child lookup: hitboxId → (GameObject, HitboxDamage)
        private readonly Dictionary<string, HitboxDamage> _hitboxMap = new();
        private HitboxDamage _activeHitbox;

        private void Awake()
        {
            if (comboController == null)
            {
                Debug.LogError(
                    $"[HitboxManager] No ComboController assigned on {gameObject.name}. " +
                    "Hit-confirm will not propagate.", this);
            }

            CacheHitboxChildren();
        }

        private void OnEnable()
        {
            if (comboController != null)
            {
                comboController.ComboDropped += DeactivateActiveHitbox;
                comboController.ComboEnded += DeactivateActiveHitbox;
            }
        }

        private void OnDisable()
        {
            if (comboController != null)
            {
                comboController.ComboDropped -= DeactivateActiveHitbox;
                comboController.ComboEnded -= DeactivateActiveHitbox;
            }

            DeactivateActiveHitbox();
        }

        /// <summary>
        /// Animation Event callback — no arguments. Reads the current combo step's
        /// AttackData to determine which hitbox to activate.
        /// Place on attack animation clips at the hitbox start frame.
        /// </summary>
        public void ActivateHitbox()
        {
            DeactivateActiveHitbox();

            var attackData = GetCurrentAttackData();
            if (attackData == null)
            {
                Debug.LogWarning(
                    $"[HitboxManager] ActivateHitbox called but no current AttackData " +
                    $"(state: {comboController?.CurrentState}). Ignoring.", this);
                return;
            }

            if (string.IsNullOrEmpty(attackData.hitboxId))
            {
                Debug.LogError(
                    $"[HitboxManager] AttackData '{attackData.attackName}' has no hitboxId set. " +
                    "Cannot activate hitbox.", this);
                return;
            }

            if (!_hitboxMap.TryGetValue(attackData.hitboxId, out var hitbox))
            {
                Debug.LogError(
                    $"[HitboxManager] No hitbox found with id '{attackData.hitboxId}' " +
                    $"on {gameObject.name}. Check AttackData '{attackData.attackName}' " +
                    "or add a child GameObject named 'Hitbox_{hitboxId}'.", this);
                return;
            }

            _activeHitbox = hitbox;
            hitbox.gameObject.SetActive(true);
        }

        /// <summary>
        /// Animation Event callback. Deactivates the currently active hitbox.
        /// Place on attack animation clips at the hitbox end frame.
        /// </summary>
        public void DeactivateHitbox()
        {
            DeactivateActiveHitbox();
        }

        private void DeactivateActiveHitbox()
        {
            if (_activeHitbox != null)
            {
                _activeHitbox.gameObject.SetActive(false);
                _activeHitbox = null;
            }
        }

        private AttackData GetCurrentAttackData()
        {
            if (comboController == null) return null;
            if (comboController.Definition == null) return null;

            int stepIndex = comboController.CurrentStepIndex;
            if (!comboController.Definition.IsValidStep(stepIndex)) return null;

            return comboController.Definition.steps[stepIndex].attackData;
        }

        /// <summary>
        /// Discovers all child GameObjects named "Hitbox_{id}" and caches their
        /// <see cref="HitboxDamage"/> components. Subscribes to their hit events.
        /// Children start disabled.
        /// </summary>
        private void CacheHitboxChildren()
        {
            _hitboxMap.Clear();
            const string prefix = "Hitbox_";

            foreach (Transform child in transform)
            {
                if (!child.name.StartsWith(prefix)) continue;

                string hitboxId = child.name.Substring(prefix.Length);

                var hitboxDamage = child.GetComponent<HitboxDamage>();
                if (hitboxDamage == null)
                {
                    Debug.LogWarning(
                        $"[HitboxManager] Child '{child.name}' matches hitbox naming " +
                        "but has no HitboxDamage component. Adding one.", this);
                    hitboxDamage = child.gameObject.AddComponent<HitboxDamage>();
                }

                _hitboxMap[hitboxId] = hitboxDamage;
                hitboxDamage.OnHitDetected += (target, hitPoint) =>
                    HandleHitDetected(target, hitPoint);

                // Hitbox children start disabled
                child.gameObject.SetActive(false);
            }
        }

        // ── TEMPORARY DAMAGE SHIM (T016 replaces this with DefenseSystem) ────

        /// <summary>
        /// TEMPORARY: Applies damage directly and forwards hit-confirm to combo system.
        /// T016 (DefenseSystem) will replace this with proper resolution
        /// (Hit/Deflected/Clashed/Dodged) where the target decides the outcome.
        /// </summary>
        private void HandleHitDetected(IDamageable target, Vector2 hitPoint)
        {
            var attackData = GetCurrentAttackData();
            if (attackData == null) return;

            var characterType = comboController != null
                ? comboController.CharacterType
                : CharacterType.Brutor;

            var detectionData = new HitDetectionData(target, attackData, hitPoint, characterType);

            // TEMPORARY: Skip invulnerable targets, apply damage directly
            if (!target.IsInvulnerable)
            {
                var packet = BuildDamagePacket(attackData);
                target.TakeDamage(packet);
            }

            // Forward hit-confirm to combo system (enables cancel windows)
            comboController?.OnHitConfirmed();

            OnHitProcessed?.Invoke(detectionData);
        }

        /// <summary>
        /// TEMPORARY: Builds a basic DamagePacket from AttackData.
        /// T016 will add buff multipliers, defense resolution, and elemental modifiers.
        /// </summary>
        private DamagePacket BuildDamagePacket(AttackData attackData)
        {
            float damage = baseAttack * attackData.damageMultiplier;

            return new DamagePacket(
                type: DamageType.Physical,
                amount: damage,
                isPunishDamage: false,
                knockbackForce: attackData.knockbackForce,
                launchForce: attackData.launchForce,
                source: comboController != null ? comboController.CharacterType : CharacterType.Brutor
            );
        }
    }
}
