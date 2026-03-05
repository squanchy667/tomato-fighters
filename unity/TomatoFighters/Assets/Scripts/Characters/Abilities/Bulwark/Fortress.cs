using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Bulwark
{
    /// <summary>
    /// Bulwark T3 signature (Main only): 3 seconds of full immunity to damage and stagger.
    /// All blocked/absorbed damage is stored. When Fortress ends, releases stored damage
    /// as a 360-degree shockwave. Max stored: 300% ATK. Cooldown: 45s.
    /// </summary>
    public class Fortress : IPathAbility
    {
        private const string ID = "Bulwark_Fortress";
        private const float DURATION = 3f;
        private const float COOLDOWN = 45f;
        private const float MAX_STORED_DAMAGE = 300f; // 300% ATK cap
        private const float SHOCKWAVE_RANGE = 4f;

        private readonly PathAbilityContext _ctx;
        private float _cooldownRemaining;
        private float _timeRemaining;
        private bool _isFortressActive;
        private float _storedDamage;

        public Fortress(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Active;
        public float ManaCost => 0f;
        public float Cooldown => COOLDOWN;
        public bool IsActive => _isFortressActive;
        public float CooldownRemaining => _cooldownRemaining;

        /// <summary>Full immunity during Fortress. Defense pipeline queries this.</summary>
        public bool IsImmune => _isFortressActive;

        /// <summary>Current stored damage for shockwave release.</summary>
        public float StoredDamage => _storedDamage;

        /// <summary>
        /// Called by the damage pipeline when damage would be taken during Fortress.
        /// Stores the damage instead of applying it.
        /// </summary>
        public void StoreDamage(float amount)
        {
            if (!_isFortressActive) return;
            _storedDamage = Mathf.Min(_storedDamage + amount, MAX_STORED_DAMAGE);
        }

        public bool TryActivate()
        {
            _isFortressActive = true;
            _timeRemaining = DURATION;
            _storedDamage = 0f;
            _cooldownRemaining = COOLDOWN;

            Debug.Log($"[Fortress] ACTIVATED — {DURATION}s immunity, storing damage for shockwave");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= deltaTime;

            if (_isFortressActive)
            {
                _timeRemaining -= deltaTime;
                if (_timeRemaining <= 0f)
                {
                    ReleaseShockwave();
                    _isFortressActive = false;
                }
            }
        }

        public void Cleanup()
        {
            _isFortressActive = false;
            _timeRemaining = 0f;
            _cooldownRemaining = 0f;
            _storedDamage = 0f;
        }

        private void ReleaseShockwave()
        {
            float damage = Mathf.Max(_storedDamage, 10f); // Minimum shockwave damage

            var hits = Physics2D.OverlapCircleAll(
                _ctx.PlayerTransform.position, SHOCKWAVE_RANGE, _ctx.EnemyLayer);

            foreach (var hit in hits)
            {
                var damageable = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsInvulnerable)
                {
                    Vector2 knockDir = ((Vector2)hit.transform.position - (Vector2)_ctx.PlayerTransform.position).normalized;
                    var packet = new DamagePacket(
                        type: DamageType.Physical,
                        amount: damage,
                        isPunishDamage: false,
                        knockbackForce: knockDir * 8f,
                        launchForce: Vector2.zero,
                        source: CharacterType.Brutor,
                        stunFillAmount: 10f);
                    damageable.TakeDamage(packet);
                }
            }

            Debug.Log($"[Fortress] Shockwave released! {damage:F0} damage to {hits.Length} enemies");
        }
    }
}
