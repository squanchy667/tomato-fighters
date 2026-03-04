using System;
using System.Collections;
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
    /// forwards hit-confirms to <see cref="ComboController"/>, and resolves defense
    /// outcomes via <see cref="IDamageable.ResolveIncoming"/>.
    /// </summary>
    public class HitboxManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ComboController comboController;
        [SerializeField] private DefenseSystem ownerDefenseSystem;

        [Header("Timer Fallback (no Animation Events)")]
        [Tooltip("Auto-activate hitboxes on combo step start. Disable when animation events are set up.")]
        [SerializeField] private bool useTimerFallback;

        [Tooltip("How long hitbox stays active per attack in fallback mode.")]
        [SerializeField] private float fallbackActiveDuration = 0.3f;

        [Header("Damage")]
        [Tooltip("Base attack value for damage calculation until stat system is wired.")]
        [SerializeField] private float baseAttack = 10f;

        /// <summary>Fired when a hit is detected and processed. Downstream systems can subscribe.</summary>
        public event Action<HitDetectionData> OnHitProcessed;

        // Hitbox child lookup: hitboxId → (GameObject, HitboxDamage)
        private readonly Dictionary<string, HitboxDamage> _hitboxMap = new();
        private HitboxDamage _activeHitbox;
        private Coroutine _fallbackCoroutine;

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

                if (useTimerFallback)
                    comboController.AttackStarted += OnAttackStartedFallback;
            }
        }

        private void OnDisable()
        {
            if (comboController != null)
            {
                comboController.ComboDropped -= DeactivateActiveHitbox;
                comboController.ComboEnded -= DeactivateActiveHitbox;

                if (useTimerFallback)
                    comboController.AttackStarted -= OnAttackStartedFallback;
            }

            StopFallbackCoroutine();
            DeactivateActiveHitbox();
        }

        // ── Timer Fallback (temporary until animation events are set up) ──

        private void OnAttackStartedFallback(AttackType attackType, int stepIndex)
        {
            Debug.Log($"[HitboxManager] FALLBACK: AttackStarted({attackType}, step={stepIndex})");
            StopFallbackCoroutine();
            ActivateHitbox();
            _fallbackCoroutine = StartCoroutine(FallbackDeactivateAfterDelay());
        }

        private IEnumerator FallbackDeactivateAfterDelay()
        {
            yield return new WaitForSeconds(fallbackActiveDuration);
            DeactivateActiveHitbox();
            _fallbackCoroutine = null;
        }

        private void StopFallbackCoroutine()
        {
            if (_fallbackCoroutine != null)
            {
                StopCoroutine(_fallbackCoroutine);
                _fallbackCoroutine = null;
            }
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
            Debug.Log($"[HitboxManager] Activated hitbox '{hitbox.name}' (id='{attackData.hitboxId}', attack='{attackData.attackName}')");
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

                Debug.Log($"[HitboxManager] Cached hitbox '{hitboxId}' → {child.name} (layer={child.gameObject.layer})");
            }

            Debug.Log($"[HitboxManager] CacheHitboxChildren done — {_hitboxMap.Count} hitboxes cached on '{gameObject.name}'");
        }

        // ── Hit Resolution ──────────────────────────────────────────────

        /// <summary>
        /// Resolves an incoming hit through the target's defense system,
        /// then applies damage, fires events, and forwards hit-confirm.
        /// </summary>
        private void HandleHitDetected(IDamageable target, Vector2 hitPoint)
        {
            Debug.Log($"[HitboxManager] HandleHitDetected ENTRY — target={target}, hitPoint={hitPoint}, " +
                $"state={comboController?.CurrentState}, stepIndex={comboController?.CurrentStepIndex}");

            var attackData = GetCurrentAttackData();
            if (attackData == null)
            {
                Debug.LogWarning($"[HitboxManager] HandleHitDetected — no current AttackData. " +
                    $"State={comboController?.CurrentState}, StepIndex={comboController?.CurrentStepIndex}");
                return;
            }

            var characterType = comboController != null
                ? comboController.CharacterType
                : CharacterType.Brutor;

            bool isUnstoppable = attackData.telegraphType == TelegraphType.Unstoppable;

            // Ask the target how it responds to this attack
            var response = target.ResolveIncoming(transform.position, isUnstoppable);

            var packet = BuildDamagePacket(attackData);

            switch (response)
            {
                case DamageResponse.Hit:
                    if (!target.IsInvulnerable)
                    {
                        Debug.Log($"[HitboxManager] APPLYING DAMAGE: {packet.amount:F1} to {target} (type={packet.type})");
                        target.TakeDamage(packet);
                    }
                    else
                    {
                        Debug.Log($"[HitboxManager] BLOCKED by IsInvulnerable on {target}");
                    }
                    break;

                case DamageResponse.Deflected:
                    // No damage on deflect. Notify the target's DefenseSystem.
                    NotifyTargetDefense(target, response, packet, characterType);
                    break;

                case DamageResponse.Clashed:
                    // Reduced damage on clash — both parties stagger
                    if (!target.IsInvulnerable)
                    {
                        var clashPacket = new DamagePacket(
                            type: packet.type,
                            amount: packet.amount * 0.5f,
                            isPunishDamage: false,
                            knockbackForce: packet.knockbackForce * 0.3f,
                            launchForce: Vector2.zero,
                            source: packet.source);
                        target.TakeDamage(clashPacket);
                    }
                    NotifyTargetDefense(target, response, packet, characterType);
                    break;

                case DamageResponse.Dodged:
                    // No damage, attack passes through
                    NotifyTargetDefense(target, response, packet, characterType);
                    break;
            }

            // Forward hit-confirm to combo system (enables cancel windows)
            comboController?.OnHitConfirmed();

            var detectionData = new HitDetectionData(target, attackData, hitPoint, characterType);
            OnHitProcessed?.Invoke(detectionData);
        }

        /// <summary>
        /// Notifies the target's <see cref="DefenseSystem"/> about a successful defense
        /// so it can fire events and apply character-specific bonuses.
        /// </summary>
        private void NotifyTargetDefense(
            IDamageable target,
            DamageResponse response,
            DamagePacket packet,
            CharacterType attackerType)
        {
            // Try to get DefenseSystem from the target's GameObject
            if (target is MonoBehaviour mb)
            {
                var targetDefense = mb.GetComponent<DefenseSystem>();
                if (targetDefense != null)
                {
                    var context = new DefenseContext
                    {
                        defender = mb.gameObject,
                        attacker = gameObject,
                        hitPoint = mb.transform.position,
                        incomingPacket = packet
                    };

                    // Use the target's CharacterType if available via motor
                    var targetMotor = mb.GetComponent<CharacterMotor>();
                    var defenderType = targetMotor != null
                        ? targetMotor.CharacterType
                        : CharacterType.Brutor;

                    targetDefense.ProcessDefenseResult(
                        response, context, defenderType, packet.amount, packet.type);
                }
            }
        }

        /// <summary>
        /// Builds a DamagePacket from the current AttackData.
        /// Uses baseAttack as a temporary stand-in until the stat system is wired.
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
