using System.Collections;
using TomatoFighters.Shared.Components;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.World.States
{
    /// <summary>
    /// Executes attacks via either multi-step patterns (EnemyAttackPattern) or
    /// single random attacks from EnemyData.attacks[] as fallback.
    /// Delegates telegraph visuals to <see cref="TelegraphVisualController"/> when available.
    /// Handles clean early-exit on stun/death mid-pattern.
    /// </summary>
    public class AttackState : EnemyStateBase
    {
        private Coroutine _attackCoroutine;
        private bool _attackFinished;

        public AttackState(EnemyAI context) : base(context) { }

        public override void Enter()
        {
            _attackFinished = false;
            Context.Rb.linearVelocity = Vector2.zero;

            // Try pattern-based execution first
            var pattern = Context.SelectPattern();
            if (pattern != null && pattern.steps != null && pattern.steps.Length > 0)
            {
                Context.RecordPatternUsed(pattern);
                _attackCoroutine = Context.StartCoroutine(PerformPattern(pattern));
                return;
            }

            // Fallback: single random attack from attacks[]
            var attacks = Context.Data.attacks;
            if (attacks == null || attacks.Length == 0)
            {
                Debug.LogWarning("[AttackState] No attacks or patterns configured.", Context);
                _attackFinished = true;
                return;
            }

            var attack = attacks[Random.Range(0, attacks.Length)];
            _attackCoroutine = Context.StartCoroutine(PerformSingleAttack(attack));
        }

        public override void Tick(float dt)
        {
            if (_attackFinished)
            {
                Context.TransitionTo(new ChaseState(Context));
            }
        }

        public override void Exit()
        {
            if (_attackCoroutine != null)
            {
                Context.StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }

            // Clean up telegraph visuals and attack state
            Context.TelegraphVisual?.CancelTelegraph();
            Context.SetActiveAttack(null);
        }

        // ── Pattern Execution ───────────────────────────────────────────

        private IEnumerator PerformPattern(EnemyAttackPattern pattern)
        {
            for (int i = 0; i < pattern.steps.Length; i++)
            {
                // Early-exit check between steps
                if (ShouldAbort()) yield break;

                var step = pattern.steps[i];
                if (step.attack == null) continue;

                // Per-step delay
                if (step.delayBeforeStep > 0f)
                {
                    yield return WaitWithAbortCheck(step.delayBeforeStep);
                    if (ShouldAbort()) yield break;
                }

                yield return ExecuteAttack(step.attack);

                if (ShouldAbort()) yield break;
            }

            // Cooldown after full pattern
            yield return WaitWithAbortCheck(Context.Data.attackCooldown);
            _attackFinished = true;
        }

        private IEnumerator PerformSingleAttack(AttackData attack)
        {
            yield return ExecuteAttack(attack);

            // Cooldown before next action
            yield return WaitWithAbortCheck(Context.Data.attackCooldown);
            _attackFinished = true;
        }

        // ── Core Attack Execution (shared by both paths) ────────────────

        private IEnumerator ExecuteAttack(AttackData attack)
        {
            Context.SetActiveAttack(attack);

            // Fire animator trigger based on position in EnemyData.attacks[]
            var animator = Context.GetComponent<Animator>();
            if (animator != null)
            {
                int triggerIndex = Context.GetAttackTriggerIndex(attack);
                string triggerName = $"attack_{triggerIndex + 1}Trigger";
                animator.SetTrigger(triggerName);
            }

            // Open clash window during telegraph
            var defenseProvider = Context.EnemyBase.GetComponent<IDefenseProvider>();
            float telegraphDuration = Context.Data.telegraphDuration;

            if (defenseProvider != null)
            {
                Vector2 facingDir = Context.DirectionToTarget();
                defenseProvider.OpenClashWindow(telegraphDuration, facingDir);
            }

            // Telegraph phase — delegate to TelegraphVisualController if available
            bool isUnstoppable = attack.telegraphType == TelegraphType.Unstoppable;
            var telegraphCtrl = Context.TelegraphVisual;

            if (telegraphCtrl != null)
            {
                if (isUnstoppable)
                    telegraphCtrl.PlayUnstoppableTelegraph(telegraphDuration);
                else
                    telegraphCtrl.PlayNormalTelegraph(telegraphDuration);

                yield return new WaitForSeconds(telegraphDuration);
            }
            else
            {
                // Inline fallback for enemies without TelegraphVisualController
                yield return InlineTelegraphFallback(telegraphDuration, isUnstoppable);
            }

            if (ShouldAbort()) yield break;

            // Hitbox activation phase
            var hitbox = FindHitbox(attack);
            if (hitbox != null)
            {
                var clashTracker = Context.EnemyBase.GetComponent<ClashTracker>();
                clashTracker?.ClearImmunities();

                hitbox.OnHitDetected += OnHitDetected;
                hitbox.gameObject.SetActive(true);

                // Visual: red-orange during active swing
                if (telegraphCtrl != null)
                    telegraphCtrl.SetActiveSwingColor();
                else
                {
                    var sprite = Context.EnemyBase.GetComponentInChildren<SpriteRenderer>();
                    if (sprite != null) sprite.color = new Color(1f, 0.2f, 0f);
                }

                // Active frames
                float activeDuration = attack.hitboxActiveFrames / (60f * attack.animationSpeed);
                yield return new WaitForSeconds(activeDuration);

                hitbox.gameObject.SetActive(false);
                hitbox.OnHitDetected -= OnHitDetected;
            }

            // Recovery — restore visuals
            Context.SetActiveAttack(null);
            if (telegraphCtrl != null)
                telegraphCtrl.RestoreColor();
            else
            {
                var sprite = Context.EnemyBase.GetComponentInChildren<SpriteRenderer>();
                if (sprite != null) sprite.color = Color.white;
            }
        }

        // ── Telegraph Fallback (for enemies without TelegraphVisualController) ──

        private IEnumerator InlineTelegraphFallback(float duration, bool isUnstoppable)
        {
            var sprite = Context.EnemyBase.GetComponentInChildren<SpriteRenderer>();
            if (sprite == null)
            {
                yield return new WaitForSeconds(duration);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                if (isUnstoppable)
                    sprite.color = Color.Lerp(new Color(1f, 0.3f, 0.3f), new Color(1f, 0f, 0f), t);
                else
                    sprite.color = Color.Lerp(new Color(1f, 1f, 0.5f), new Color(1f, 0.6f, 0f), t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            sprite.color = Color.white;
            yield return null;
        }

        // ── Abort Checks ────────────────────────────────────────────────

        /// <summary>Check if the attack should abort due to stun or death.</summary>
        private bool ShouldAbort()
        {
            return Context.EnemyBase.IsStunned || Context.EnemyBase.CurrentHealth <= 0f;
        }

        /// <summary>Wait for a duration, checking for abort each frame.</summary>
        private IEnumerator WaitWithAbortCheck(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (ShouldAbort()) yield break;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // ── Hit Detection ───────────────────────────────────────────────

        private void OnHitDetected(IDamageable target, Vector2 hitPoint)
        {
            var attack = Context.ActiveAttack;
            if (attack == null) return;

            var clashTracker = Context.EnemyBase.GetComponent<ClashTracker>();
            if (clashTracker != null && clashTracker.HasClashImmunity(target))
                return;

            bool isUnstoppable = attack.telegraphType == TelegraphType.Unstoppable;
            var response = target.ResolveIncoming(Context.transform.position, isUnstoppable);

            float damage = attack.damageMultiplier * 10f;
            float stunFill = damage * 0.1f;

            var packet = new DamagePacket(
                type: DamageType.Physical,
                amount: damage,
                isPunishDamage: false,
                knockbackForce: attack.knockbackForce,
                launchForce: attack.launchForce,
                source: CharacterType.Brutor,
                stunFillAmount: stunFill
            );

            switch (response)
            {
                case DamageResponse.Hit:
                    if (!target.IsInvulnerable)
                        target.TakeDamage(packet);
                    break;

                case DamageResponse.Clashed:
                    if (target is MonoBehaviour tmb)
                    {
                        var targetTracker = tmb.GetComponentInChildren<ClashTracker>();
                        targetTracker?.AddClashImmunity(Context.EnemyBase);
                    }
                    break;

                case DamageResponse.Deflected:
                case DamageResponse.Dodged:
                    break;
            }

            if (response != DamageResponse.Hit && target is MonoBehaviour defMb)
            {
                var defProv = defMb.GetComponent<IDefenseProvider>();
                defProv?.NotifyDefenseSuccess(response, damage, DamageType.Physical);
            }
        }

        // ── Hitbox Lookup ───────────────────────────────────────────────

        private HitboxDamage FindHitbox(AttackData attack)
        {
            if (!string.IsNullOrEmpty(attack.hitboxId))
            {
                var hitboxT = Context.transform.Find($"Hitbox_{attack.hitboxId}");
                if (hitboxT != null)
                {
                    var hd = hitboxT.GetComponent<HitboxDamage>();
                    if (hd != null) return hd;
                }
            }

            return Context.GetComponentInChildren<HitboxDamage>(includeInactive: true);
        }
    }
}
