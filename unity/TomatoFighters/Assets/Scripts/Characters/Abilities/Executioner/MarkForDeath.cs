using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Executioner
{
    /// <summary>
    /// Executioner T1 active: marks the nearest enemy for +25% damage taken, 8s duration.
    /// Applies Mark status via <see cref="IStatusEffectable"/>.
    /// Cooldown: 10s.
    /// </summary>
    public class MarkForDeath : IPathAbility
    {
        private const string ID = "Executioner_MarkForDeath";
        private const float MARK_DURATION = 8f;
        private const float MARK_BONUS = 0.25f;
        private const float MARK_RANGE = 8f;
        private const float COOLDOWN = 10f;

        private readonly PathAbilityContext _ctx;
        private float _cooldownRemaining;

        public MarkForDeath(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Active;
        public float ManaCost => 0f;
        public float Cooldown => COOLDOWN;
        public bool IsActive => false;
        public float CooldownRemaining => _cooldownRemaining;

        public bool TryActivate()
        {
            // Find nearest enemy
            var hits = Physics2D.OverlapCircleAll(
                _ctx.PlayerTransform.position, MARK_RANGE, _ctx.EnemyLayer);

            if (hits.Length == 0)
            {
                Debug.Log("[MarkForDeath] No enemies in range.");
                return false;
            }

            float bestDist = float.MaxValue;
            Collider2D bestTarget = null;

            foreach (var hit in hits)
            {
                float dist = Vector2.Distance(_ctx.PlayerTransform.position, hit.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestTarget = hit;
                }
            }

            if (bestTarget == null) return false;

            var statusEffectable = bestTarget.GetComponent<IStatusEffectable>();
            if (statusEffectable == null)
                statusEffectable = bestTarget.GetComponentInParent<IStatusEffectable>();

            if (statusEffectable == null)
            {
                Debug.Log("[MarkForDeath] Target has no IStatusEffectable.");
                return false;
            }

            statusEffectable.AddEffect(new StatusEffect(
                StatusEffectType.Mark, MARK_DURATION, MARK_BONUS, _ctx.PlayerTransform));

            _cooldownRemaining = COOLDOWN;
            Debug.Log($"[MarkForDeath] Marked {bestTarget.name} for +{MARK_BONUS * 100}% damage, {MARK_DURATION}s");
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
