using System.Collections.Generic;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Conjurer
{
    /// <summary>
    /// Conjurer T3 signature (Main only): summon a massive Golem (HP 300, ATK 1.5, DEF 15, 20s).
    /// Golem taunts enemies and slams for AoE damage. Inherits 50% of Mystica's ritual effects.
    /// While Golem is alive, Sproutling max increases by 1 (to 3 total). Cooldown: 60s.
    /// </summary>
    public class SummonGolem : IPathAbility
    {
        private const string ID = "Conjurer_SummonGolem";
        private const float MANA_COST = 50f;
        private const float COOLDOWN = 60f;
        private const float GOLEM_LIFETIME = 20f;
        private const string PREFAB_PATH = "Golem";

        private readonly PathAbilityContext _ctx;
        private float _cooldownRemaining;
        private GameObject _golem;

        public SummonGolem(PathAbilityContext ctx) { _ctx = ctx; }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Active;
        public float ManaCost => MANA_COST;
        public float Cooldown => COOLDOWN;
        public bool IsActive => _golem != null;
        public float CooldownRemaining => _cooldownRemaining;

        /// <summary>Whether Golem is alive. SummonSproutling checks this for +1 max.</summary>
        public bool GolemAlive => _golem != null;

        public bool TryActivate()
        {
            if (_golem != null)
            {
                Debug.Log("[SummonGolem] Golem already active.");
                return false;
            }

            bool facingRight = _ctx.Motor != null && _ctx.Motor.FacingRight;
            Vector3 offset = new Vector3(facingRight ? 2f : -2f, 0f, 0f);
            Vector3 spawnPos = _ctx.PlayerTransform.position + offset;

            var prefab = Resources.Load<GameObject>(PREFAB_PATH);
            if (prefab != null)
            {
                _golem = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // Fallback: minimal runtime creation
                _golem = new GameObject("Golem");
                _golem.transform.position = spawnPos;

                var rb = _golem.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;

                _golem.AddComponent<BoxCollider2D>().size = new Vector2(1.5f, 2f);

                var sr = _golem.AddComponent<SpriteRenderer>();
                sr.color = new Color(0.5f, 0.35f, 0.2f);

                Debug.LogWarning("[SummonGolem] Golem prefab not in Resources. " +
                    "Copy from Assets/Prefabs/Companions/ to Assets/Resources/.");
            }

            Object.Destroy(_golem, GOLEM_LIFETIME);
            _cooldownRemaining = COOLDOWN;

            Debug.Log($"[SummonGolem] GOLEM SUMMONED — HP 300, ATK 1.5, DEF 15, {GOLEM_LIFETIME}s");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= deltaTime;
        }

        public void Cleanup()
        {
            if (_golem != null)
                Object.Destroy(_golem);
            _golem = null;
            _cooldownRemaining = 0f;
        }
    }
}
