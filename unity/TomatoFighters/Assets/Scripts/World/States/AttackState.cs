using System.Collections;
using TomatoFighters.Shared.Components;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.World.States
{
    /// <summary>
    /// Picks an attack from EnemyData.attacks[], runs the telegraph -> clash window ->
    /// hitbox -> cooldown sequence via coroutine, then transitions back to Chase.
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

            var attacks = Context.Data.attacks;
            if (attacks == null || attacks.Length == 0)
            {
                Debug.LogWarning("[AttackState] No attacks configured on EnemyData.", Context);
                _attackFinished = true;
                return;
            }

            // Pick a random attack
            var attack = attacks[Random.Range(0, attacks.Length)];
            Debug.Log($"[AttackState] Starting attack: {attack.attackName}, hitboxId={attack.hitboxId}");
            _attackCoroutine = Context.StartCoroutine(PerformAttack(attack));
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

            Context.SetActiveAttack(null);
        }

        private IEnumerator PerformAttack(AttackData attack)
        {
            Context.SetActiveAttack(attack);

            // Open clash window during telegraph (mirrors TestDummyEnemy)
            var defenseProvider = Context.EnemyBase.GetComponent<IDefenseProvider>();
            float telegraphDuration = Context.Data.telegraphDuration;

            if (defenseProvider != null)
            {
                Vector2 facingDir = Context.DirectionToTarget();
                defenseProvider.OpenClashWindow(telegraphDuration, facingDir);
            }

            // Telegraph phase — visual warning
            var sprite = Context.EnemyBase.GetComponentInChildren<SpriteRenderer>();
            Color originalColor = sprite != null ? sprite.color : Color.white;

            bool isUnstoppable = attack.telegraphType == TelegraphType.Unstoppable;
            yield return TelegraphPhase(sprite, originalColor, telegraphDuration, isUnstoppable);

            // Hitbox activation phase
            var hitbox = FindHitbox(attack);
            Debug.Log($"[AttackState] Hitbox lookup: {(hitbox != null ? hitbox.name : "NULL")}");
            if (hitbox != null)
            {
                // Clear clash immunity for new attack
                var clashTracker = Context.EnemyBase.GetComponent<ClashTracker>();
                clashTracker?.ClearImmunities();

                hitbox.OnHitDetected += OnHitDetected;
                hitbox.gameObject.SetActive(true);

                if (sprite != null)
                    sprite.color = new Color(1f, 0.2f, 0f); // Red-orange during swing

                // Active frames
                float activeDuration = attack.hitboxActiveFrames / (60f * attack.animationSpeed);
                yield return new WaitForSeconds(activeDuration);

                hitbox.gameObject.SetActive(false);
                hitbox.OnHitDetected -= OnHitDetected;
            }

            // Recovery
            Context.SetActiveAttack(null);
            if (sprite != null)
                sprite.color = originalColor;

            // Cooldown before next action
            yield return new WaitForSeconds(Context.Data.attackCooldown);

            _attackFinished = true;
        }

        private IEnumerator TelegraphPhase(SpriteRenderer sprite, Color originalColor,
            float duration, bool isUnstoppable)
        {
            if (sprite != null)
            {
                // Unstoppable = red tint, Normal = yellow tint
                sprite.color = isUnstoppable
                    ? new Color(1f, 0.3f, 0.3f)  // Red warning
                    : new Color(1f, 1f, 0.5f);    // Yellow warning
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // Ramp color intensity during telegraph
                if (sprite != null)
                {
                    float t = elapsed / duration;
                    if (isUnstoppable)
                    {
                        // Red gets more intense
                        sprite.color = Color.Lerp(new Color(1f, 0.3f, 0.3f), new Color(1f, 0f, 0f), t);
                    }
                    else
                    {
                        // Yellow → orange
                        sprite.color = Color.Lerp(new Color(1f, 1f, 0.5f), new Color(1f, 0.6f, 0f), t);
                    }
                }

                yield return null;
            }

            // Flash white before strike
            if (sprite != null)
                sprite.color = Color.white;

            yield return null;
        }

        private void OnHitDetected(IDamageable target, Vector2 hitPoint)
        {
            var attack = Context.ActiveAttack;
            if (attack == null) return;

            // Clash immunity check
            var clashTracker = Context.EnemyBase.GetComponent<ClashTracker>();
            if (clashTracker != null && clashTracker.HasClashImmunity(target))
                return;

            bool isUnstoppable = attack.telegraphType == TelegraphType.Unstoppable;
            var response = target.ResolveIncoming(Context.transform.position, isUnstoppable);

            float damage = attack.damageMultiplier * 10f; // Base enemy ATK placeholder
            float stunFill = damage * 0.1f; // Base stun contribution

            var packet = new DamagePacket(
                type: DamageType.Physical,
                amount: damage,
                isPunishDamage: false,
                knockbackForce: attack.knockbackForce,
                launchForce: attack.launchForce,
                source: CharacterType.Brutor, // Enemies use default
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

            // Notify defense system for visual feedback
            if (response != DamageResponse.Hit && target is MonoBehaviour defMb)
            {
                var defProv = defMb.GetComponent<IDefenseProvider>();
                defProv?.NotifyDefenseSuccess(response, damage, DamageType.Physical);
            }
        }

        private HitboxDamage FindHitbox(AttackData attack)
        {
            // Look for child named Hitbox_{hitboxId}, falling back to first HitboxDamage
            if (!string.IsNullOrEmpty(attack.hitboxId))
            {
                var hitboxT = Context.transform.Find($"Hitbox_{attack.hitboxId}");
                if (hitboxT != null)
                {
                    var hd = hitboxT.GetComponent<HitboxDamage>();
                    if (hd != null) return hd;
                }
            }

            // Fallback: find any HitboxDamage child
            return Context.GetComponentInChildren<HitboxDamage>(includeInactive: true);
        }
    }
}
