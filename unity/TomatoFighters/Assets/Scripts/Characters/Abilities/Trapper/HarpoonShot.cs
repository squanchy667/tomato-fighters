using TomatoFighters.Combat.Projectiles;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Trapper
{
    /// <summary>
    /// Trapper T1 active: fires a harpoon projectile that immobilizes the first enemy hit for 2s.
    /// Costs 15 mana, 6s cooldown. Spawns a <see cref="HarpoonProjectile"/>.
    /// </summary>
    public class HarpoonShot : IPathAbility
    {
        private const string ID = "Trapper_HarpoonShot";
        private const float MANA_COST = 15f;
        private const float COOLDOWN = 6f;

        private readonly PathAbilityContext _ctx;
        private readonly GameObject _vfxPrefab;
        private float _cooldownRemaining;

        public HarpoonShot(PathAbilityContext ctx)
        {
            _ctx = ctx;
            _vfxPrefab = ctx.VfxPrefab;
        }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Active;
        public float ManaCost => MANA_COST;
        public float Cooldown => COOLDOWN;
        public bool IsActive => false;
        public float CooldownRemaining => _cooldownRemaining;

        public bool TryActivate()
        {
            // Spawn harpoon projectile in facing direction
            var spawnPos = _ctx.PlayerTransform.position;
            bool facingRight = _ctx.Motor != null && _ctx.Motor.FacingRight;
            Vector2 direction = facingRight ? Vector2.right : Vector2.left;

            var projectileGO = new GameObject("HarpoonProjectile");
            projectileGO.transform.position = spawnPos;

            var rb = projectileGO.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var col = projectileGO.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;

            var harpoon = projectileGO.AddComponent<HarpoonProjectile>();
            harpoon.SetSource(_ctx.Motor != null ? _ctx.Motor.CharacterType : CharacterType.Viper);
            harpoon.Initialize(direction);

            // Projectile trail VFX — yellow chain-link trail at launch point
            if (_vfxPrefab != null)
                Object.Destroy(
                    Object.Instantiate(_vfxPrefab, spawnPos, Quaternion.identity),
                    0.4f);

            _cooldownRemaining = COOLDOWN;
            Debug.Log($"[HarpoonShot] Fired harpoon {(facingRight ? "right" : "left")}");
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
