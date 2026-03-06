using System.Collections.Generic;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Interfaces;
using UnityEngine;

namespace TomatoFighters.Characters.Abilities.Conjurer
{
    /// <summary>
    /// Conjurer T1 active: spawns a Sproutling companion that attacks enemies.
    /// Max 2 active sproutlings. Costs 30 mana, 12s cooldown.
    /// Sproutling: HP 40, ATK 0.6, lifetime 20s, targets enemy layer (DD-7).
    /// Loads prefab from Resources to avoid cross-pillar dependency on World.
    /// </summary>
    public class SummonSproutling : IPathAbility
    {
        private const string ID = "Conjurer_SummonSproutling";
        private const float MANA_COST = 30f;
        private const float COOLDOWN = 12f;
        private const int MAX_ACTIVE = 2;
        private const string PREFAB_PATH = "Sproutling";

        private readonly PathAbilityContext _ctx;
        private readonly GameObject _vfxPrefab;
        private readonly List<GameObject> _activeSproutlings = new();
        private float _cooldownRemaining;
        private GameObject _prefab;

        public SummonSproutling(PathAbilityContext ctx)
        {
            _ctx = ctx;
            _vfxPrefab = ctx.VfxPrefab;
            _prefab = Resources.Load<GameObject>(PREFAB_PATH);
        }

        public string AbilityId => ID;
        public AbilityActivationType ActivationType => AbilityActivationType.Active;
        public float ManaCost => MANA_COST;
        public float Cooldown => COOLDOWN;
        public bool IsActive => false;
        public float CooldownRemaining => _cooldownRemaining;

        public bool TryActivate()
        {
            // Clean up destroyed sproutlings
            _activeSproutlings.RemoveAll(s => s == null);

            if (_activeSproutlings.Count >= MAX_ACTIVE)
            {
                Debug.Log($"[SummonSproutling] Max {MAX_ACTIVE} sproutlings active.");
                return false;
            }

            // Spawn offset from player
            bool facingRight = _ctx.Motor != null && _ctx.Motor.FacingRight;
            Vector3 offset = new Vector3(facingRight ? 1.5f : -1.5f, 0f, 0f);
            Vector3 spawnPos = _ctx.PlayerTransform.position + offset;

            GameObject sproutGO;
            if (_prefab != null)
            {
                sproutGO = Object.Instantiate(_prefab, spawnPos, Quaternion.identity);
            }
            else
            {
                // Fallback: minimal runtime creation (no prefab available)
                sproutGO = new GameObject($"Sproutling_{_activeSproutlings.Count + 1}");
                sproutGO.transform.position = spawnPos;

                var rb = sproutGO.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;

                sproutGO.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 0.5f);

                var sr = sproutGO.AddComponent<SpriteRenderer>();
                sr.color = new Color(0.3f, 0.8f, 0.3f);

                Debug.LogWarning("[SummonSproutling] Sproutling prefab not in Resources. " +
                    "Copy from Assets/Prefabs/Companions/ to Assets/Resources/.");
            }

            // Spawn burst VFX — green leafy burst at sproutling spawn position
            if (_vfxPrefab != null)
                Object.Destroy(
                    Object.Instantiate(_vfxPrefab, spawnPos, Quaternion.identity),
                    0.5f);

            _activeSproutlings.Add(sproutGO);
            _cooldownRemaining = COOLDOWN;

            Debug.Log($"[SummonSproutling] Spawned sproutling ({_activeSproutlings.Count}/{MAX_ACTIVE})");
            return true;
        }

        public void Tick(float deltaTime)
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= deltaTime;
        }

        public void Cleanup()
        {
            foreach (var s in _activeSproutlings)
            {
                if (s != null) Object.Destroy(s);
            }
            _activeSproutlings.Clear();
            _cooldownRemaining = 0f;
        }
    }
}
