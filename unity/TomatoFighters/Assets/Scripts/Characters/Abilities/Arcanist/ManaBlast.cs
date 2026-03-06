using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Arcanist
{
    /// <summary>
    /// Arcanist T2 passive: releasing Mana Charge (T1) fires a piercing beam instead
    /// of restoring mana. Damage scales with charge percentage:
    /// 25% = 100% ATK, 50% = 250% ATK, 75% = 400% ATK, 100% = 600% ATK.
    /// At 100% charge, beam also stuns for 1.5s.
    /// </summary>
    public class ManaBlast : IPathAbility
    {
        private const string ID = "Arcanist_ManaBlast";
        private const float BEAM_RANGE = 12f;
        private const float STUN_DURATION = 1.5f;
        private const float STUN_CHARGE_THRESHOLD = 100f;

        private readonly PathAbilityContext _ctx;
        private bool _isActive;

        public ManaBlast(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Passive;
        public float ManaCost => 0f;
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        /// <summary>Whether ManaCharge release should fire a beam instead of restoring mana.</summary>
        public bool BlastEnabled => _isActive;

        /// <summary>
        /// Returns beam damage based on charge percentage. Called by ManaCharge on release.
        /// </summary>
        public float GetDamageForCharge(float chargePercent)
        {
            if (chargePercent <= 25f) return 10f;       // 100% ATK base
            if (chargePercent <= 50f) return 25f;       // 250% ATK base
            if (chargePercent <= 75f) return 40f;       // 400% ATK base
            return 60f;                                  // 600% ATK base
        }

        /// <summary>
        /// Fires the piercing beam on ManaCharge release. Called by ManaCharge.
        /// </summary>
        public void FireBeam(float chargePercent)
        {
            if (!_isActive) return;

            bool facingRight = _ctx.Motor != null && _ctx.Motor.FacingRight;
            Vector2 dir = facingRight ? Vector2.right : Vector2.left;
            Vector2 origin = (Vector2)_ctx.PlayerTransform.position;

            float damage = GetDamageForCharge(chargePercent);
            bool shouldStun = chargePercent >= STUN_CHARGE_THRESHOLD;

            var hits = Physics2D.RaycastAll(origin, dir, BEAM_RANGE, _ctx.EnemyLayer);
            foreach (var hit in hits)
            {
                var damageable = hit.collider.GetComponent<IDamageable>()
                    ?? hit.collider.GetComponentInParent<IDamageable>();

                if (damageable != null && !damageable.IsInvulnerable)
                {
                    var packet = new DamagePacket(
                        type: DamageType.Physical,
                        amount: damage,
                        isPunishDamage: false,
                        knockbackForce: Vector2.zero,
                        launchForce: Vector2.zero,
                        source: CharacterType.Viper,
                        stunFillAmount: shouldStun ? 100f : 0f);
                    damageable.TakeDamage(packet);
                }

                if (shouldStun)
                {
                    var statusEffectable = hit.collider.GetComponent<IStatusEffectable>()
                        ?? hit.collider.GetComponentInParent<IStatusEffectable>();
                    if (statusEffectable != null)
                    {
                        statusEffectable.AddEffect(new StatusEffect(
                            StatusEffectType.Immobilize, STUN_DURATION, 0f, _ctx.PlayerTransform));
                    }
                }
            }

            Debug.Log($"[ManaBlast] Beam fired at {chargePercent:F0}% — {damage:F0} damage" +
                (shouldStun ? " + STUN" : ""));
        }

        public bool TryActivate()
        {
            _isActive = true;
            Debug.Log("[ManaBlast] Blast mode active — ManaCharge release fires beam");
            return true;
        }

        public void Tick(float deltaTime) { }
        public void Cleanup() { _isActive = false; }
    }
}
