using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Shadow
{
    /// <summary>
    /// Shadow T2 passive: after dashing, Slasher leaves an afterimage (1.5s).
    /// If an enemy attacks the afterimage, Slasher appears behind the enemy and deals
    /// 120% ATK as a guaranteed crit (backstab). 4s internal cooldown per afterimage trigger.
    /// </summary>
    public class Afterimage : IPathAbility
    {
        private const string ID = "Shadow_Afterimage";
        private const float AFTERIMAGE_LIFETIME = 1.5f;
        private const float BACKSTAB_DAMAGE_MULT = 1.2f;
        private const float BACKSTAB_DAMAGE_BASE = 10f;
        private const float INTERNAL_COOLDOWN = 4f;

        private readonly PathAbilityContext _ctx;
        private bool _isActive;
        private float _icdRemaining;
        private GameObject _currentAfterimage;

        public Afterimage(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Passive;
        public float ManaCost => 0f;
        public float Cooldown => 0f;
        public bool IsActive => _isActive;
        public float CooldownRemaining => 0f;

        public bool TryActivate()
        {
            _isActive = true;

            if (_ctx.Motor != null)
                _ctx.Motor.Dashed += OnDashed;

            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_icdRemaining > 0f)
                _icdRemaining -= deltaTime;
        }

        public void Cleanup()
        {
            _isActive = false;
            if (_ctx.Motor != null)
                _ctx.Motor.Dashed -= OnDashed;

            if (_currentAfterimage != null)
                Object.Destroy(_currentAfterimage);
        }

        private void OnDashed(CharacterType character, Vector2 direction, bool hasIFrames)
        {
            if (!_isActive || _icdRemaining > 0f) return;

            // Clean up previous afterimage
            if (_currentAfterimage != null)
                Object.Destroy(_currentAfterimage);

            // Spawn afterimage at dash origin
            _currentAfterimage = new GameObject("Afterimage");
            _currentAfterimage.transform.position = _ctx.PlayerTransform.position;

            var sr = _currentAfterimage.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.5f, 0.3f, 0.8f, 0.5f); // Semi-transparent purple
            sr.sortingOrder = -1;

            // Collider to detect enemy attacks (trigger)
            var col = _currentAfterimage.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.8f;

            Object.Destroy(_currentAfterimage, AFTERIMAGE_LIFETIME);

            Debug.Log("[Afterimage] Afterimage placed");
        }

        /// <summary>
        /// Called when an enemy attacks the afterimage position.
        /// Triggers backstab on the attacking enemy.
        /// </summary>
        public void OnAfterimageTriggered(Transform attacker)
        {
            if (!_isActive || _icdRemaining > 0f || attacker == null) return;

            _icdRemaining = INTERNAL_COOLDOWN;

            var damageable = attacker.GetComponent<IDamageable>()
                ?? attacker.GetComponentInParent<IDamageable>();

            if (damageable != null && !damageable.IsInvulnerable)
            {
                float damage = BACKSTAB_DAMAGE_BASE * BACKSTAB_DAMAGE_MULT;
                var packet = new DamagePacket(
                    type: DamageType.Physical,
                    amount: damage,
                    isPunishDamage: false,
                    knockbackForce: Vector2.zero,
                    launchForce: Vector2.zero,
                    source: CharacterType.Slasher,
                    stunFillAmount: 0f);
                damageable.TakeDamage(packet);
                Debug.Log($"[Afterimage] Backstab! {damage:F0} damage (guaranteed crit)");
            }
        }
    }
}
