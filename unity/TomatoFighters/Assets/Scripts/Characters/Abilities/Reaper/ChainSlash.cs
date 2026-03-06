using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Reaper
{
    /// <summary>
    /// Reaper T2 passive: when any enemy dies from Slasher's attacks,
    /// automatically dash to the nearest enemy within 4 units and deal 80% ATK.
    /// Resets once per kill.
    /// </summary>
    public class ChainSlash : IPathAbility
    {
        private const string ID = "Reaper_ChainSlash";
        private const float CHAIN_RANGE = 4f;
        private const float CHAIN_DAMAGE_MULT = 0.8f;
        private const float CHAIN_DAMAGE_BASE = 10f;

        private readonly PathAbilityContext _ctx;
        private bool _isActive;

        public ChainSlash(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Passive;
        public float ManaCost => 0f;
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        /// <summary>
        /// Called by the kill event pipeline when Slasher kills an enemy.
        /// Finds nearest living enemy and chain-dashes to deal damage.
        /// </summary>
        public void OnEnemyKilled(Vector2 killPosition)
        {
            if (!_isActive) return;

            var hits = Physics2D.OverlapCircleAll(killPosition, CHAIN_RANGE, _ctx.EnemyLayer);
            if (hits.Length == 0) return;

            float bestDist = float.MaxValue;
            Collider2D bestTarget = null;

            foreach (var hit in hits)
            {
                var dmgTarget = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (dmgTarget == null || dmgTarget.IsInvulnerable || dmgTarget.CurrentHealth <= 0f) continue;

                float dist = Vector2.Distance(killPosition, hit.transform.position);
                if (dist < bestDist) { bestDist = dist; bestTarget = hit; }
            }

            if (bestTarget == null) return;

            var damageable = bestTarget.GetComponent<IDamageable>()
                ?? bestTarget.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                float damage = CHAIN_DAMAGE_BASE * CHAIN_DAMAGE_MULT;
                var packet = new DamagePacket(
                    type: DamageType.Physical,
                    amount: damage,
                    isPunishDamage: false,
                    knockbackForce: Vector2.zero,
                    launchForce: Vector2.zero,
                    source: _ctx.Motor != null ? _ctx.Motor.CharacterType : CharacterType.Slasher,
                    stunFillAmount: 0f);
                damageable.TakeDamage(packet);
                Debug.Log($"[ChainSlash] Chain dash to {bestTarget.name} — {damage:F0} damage");
            }
        }

        public bool TryActivate()
        {
            _isActive = true;
            return true;
        }

        public void Tick(float deltaTime) { }
        public void Cleanup() { _isActive = false; }
    }
}
