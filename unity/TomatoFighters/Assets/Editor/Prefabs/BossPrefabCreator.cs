using TomatoFighters.Shared.Components;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Events;
using TomatoFighters.World;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.Prefabs
{
    /// <summary>
    /// Creates the TestBoss prefab with BossEnemy, EnemyAI, BossAI,
    /// BossData SO (3 phases), EnemyData SO, and 6 boss AttackData SOs.
    /// Run via menu: <b>TomatoFighters > Create TestBoss Prefab</b>.
    /// Idempotent — safe to re-run.
    /// </summary>
    public static class BossPrefabCreator
    {
        private const string PREFAB_FOLDER = "Assets/Prefabs/Enemies";
        private const string PREFAB_PATH = PREFAB_FOLDER + "/TestBoss.prefab";
        private const string SO_FOLDER = "Assets/ScriptableObjects/Enemies";
        private const string ENEMY_DATA_PATH = SO_FOLDER + "/TestBoss_EnemyData.asset";
        private const string BOSS_DATA_PATH = SO_FOLDER + "/TestBoss_BossData.asset";
        private const string ATTACK_FOLDER = "Assets/ScriptableObjects/Attacks/Boss";
        private const string EVENT_FOLDER = "Assets/ScriptableObjects/Events";
        private const string PHASE_EVENT_PATH = EVENT_FOLDER + "/OnBossPhaseChanged.asset";

        private const string ENEMY_HURTBOX_LAYER = "EnemyHurtbox";
        private const string ENEMY_HITBOX_LAYER = "EnemyHitbox";

        [MenuItem("TomatoFighters/Create TestBoss Prefab")]
        public static void CreateTestBossPrefab()
        {
            PlayerPrefabCreator.EnsureFolderExists(PREFAB_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(SO_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(ATTACK_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(EVENT_FOLDER);

            // Create attack SOs
            var bossSlash = CreateOrLoadAttack("BossSlash", "Boss Slash", 0.8f,
                new Vector2(2.5f, 0f), TelegraphType.Normal, false, 0f);
            var bossOverhead = CreateOrLoadAttack("BossOverhead", "Boss Overhead", 1.2f,
                new Vector2(3f, 0.5f), TelegraphType.Normal, false, 0f);
            var bossLunge = CreateOrLoadAttack("BossLunge", "Boss Lunge", 1.0f,
                new Vector2(4f, 0f), TelegraphType.Normal, false, 0f);
            var bossUnstoppableSlam = CreateOrLoadAttack("BossUnstoppableSlam", "Boss Unstoppable Slam", 1.5f,
                new Vector2(3f, 1f), TelegraphType.Unstoppable, false, 0f);
            var bossGroundPound = CreateOrLoadAttack("BossGroundPound", "Boss Ground Pound", 2.0f,
                new Vector2(5f, 1.5f), TelegraphType.Unstoppable, true, 1.5f);
            var bossEnragedSlash = CreateOrLoadAttack("BossEnragedSlash", "Boss Enraged Slash", 1.0f,
                new Vector2(3f, 0f), TelegraphType.Normal, false, 0f);

            // Create EnemyData
            var enemyData = CreateOrLoadEnemyData(bossSlash, bossOverhead, bossLunge,
                bossUnstoppableSlam, bossGroundPound, bossEnragedSlash);

            // Create phase change event
            var phaseEvent = CreateOrLoadPhaseEvent();

            // Create BossData with 3 phases
            var bossData = CreateOrLoadBossData(
                bossSlash, bossOverhead, bossLunge,
                bossUnstoppableSlam, bossGroundPound, bossEnragedSlash);

            var whiteSquare = TestDummyPrefabCreator.GetOrCreateWhiteSquareSprite();

            bool isNew = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) == null;
            var prefab = SetupPrefab(enemyData, bossData, phaseEvent, whiteSquare);

            string verb = isNew ? "Created" : "Updated";
            Debug.Log($"[BossPrefabCreator] {verb} TestBoss prefab at {PREFAB_PATH}");
            Selection.activeObject = prefab;
        }

        // ── Attack SO Creation ──────────────────────────────────────────

        private static AttackData CreateOrLoadAttack(string id, string displayName,
            float damageMult, Vector2 knockback, TelegraphType telegraph,
            bool hasPunish, float punishDuration)
        {
            string path = $"{ATTACK_FOLDER}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AttackData>(path);
            var attack = existing != null ? existing : ScriptableObject.CreateInstance<AttackData>();

            attack.attackId = $"boss_{id.ToLower()}";
            attack.attackName = displayName;
            attack.damageMultiplier = damageMult;
            attack.knockbackForce = knockback;
            attack.launchForce = Vector2.zero;
            attack.hitboxId = "Punch";
            attack.hitboxStartFrame = 10;
            attack.hitboxActiveFrames = 8;
            attack.totalFrames = 40;
            attack.animationSpeed = 1f;
            attack.telegraphType = telegraph;
            attack.hasPunishWindow = hasPunish;
            attack.punishWindowDuration = punishDuration;

            if (existing == null)
                AssetDatabase.CreateAsset(attack, path);
            else
                EditorUtility.SetDirty(attack);

            AssetDatabase.SaveAssets();
            return attack;
        }

        // ── EnemyData SO ────────────────────────────────────────────────

        private static EnemyData CreateOrLoadEnemyData(params AttackData[] allAttacks)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyData>(ENEMY_DATA_PATH);
            var data = existing != null ? existing : ScriptableObject.CreateInstance<EnemyData>();

            data.maxHealth = 300f;
            data.pressureThreshold = 80f;
            data.stunDuration = 2f;
            data.invulnerabilityDuration = 1f;
            data.knockbackResistance = 0.5f;
            data.movementSpeed = 4.5f;
            data.aggroRange = 12f;
            data.attackRange = 1.8f;
            data.patrolRadius = 2f;
            data.leashRange = 18f;
            data.idleDuration = 0.5f;
            data.attackCooldown = 1.2f;
            data.aggression = 0.8f;
            data.hitReactDuration = 0.15f;
            data.telegraphDuration = 0.35f;

            if (existing == null)
                AssetDatabase.CreateAsset(data, ENEMY_DATA_PATH);
            else
                EditorUtility.SetDirty(data);

            // Wire all attacks via SerializedObject
            var dataSO = new SerializedObject(data);
            var attacksProp = dataSO.FindProperty("attacks");
            attacksProp.arraySize = allAttacks.Length;
            for (int i = 0; i < allAttacks.Length; i++)
                attacksProp.GetArrayElementAtIndex(i).objectReferenceValue = allAttacks[i];
            dataSO.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            return data;
        }

        // ── BossData SO ─────────────────────────────────────────────────

        private static BossData CreateOrLoadBossData(
            AttackData bossSlash, AttackData bossOverhead,
            AttackData bossLunge, AttackData bossUnstoppableSlam,
            AttackData bossGroundPound, AttackData bossEnragedSlash)
        {
            var existing = AssetDatabase.LoadAssetAtPath<BossData>(BOSS_DATA_PATH);
            var data = existing != null ? existing : ScriptableObject.CreateInstance<BossData>();

            data.phaseTransitionDuration = 1.5f;
            data.transitionBlinkCount = 5;
            data.enrageColor = new Color(1f, 0.15f, 0.15f);

            data.phases = new BossPhaseData[]
            {
                // Phase 1: 100%–60% HP
                new BossPhaseData
                {
                    phaseName = "Phase 1 — Cautious",
                    hpThreshold = 1.0f,
                    attacks = new[] { bossSlash, bossOverhead },
                    tempoMultiplier = 1.0f,
                    attackCooldownOverride = -1f,
                    enableEnrage = false
                },
                // Phase 2: 60%–30% HP
                new BossPhaseData
                {
                    phaseName = "Phase 2 — Aggressive",
                    hpThreshold = 0.6f,
                    attacks = new[] { bossSlash, bossOverhead, bossLunge, bossUnstoppableSlam },
                    tempoMultiplier = 1.3f,
                    attackCooldownOverride = -1f,
                    enableEnrage = false
                },
                // Phase 3: 30%–0% HP
                new BossPhaseData
                {
                    phaseName = "Phase 3 — Enraged",
                    hpThreshold = 0.3f,
                    attacks = new[] { bossEnragedSlash, bossLunge, bossUnstoppableSlam, bossGroundPound },
                    tempoMultiplier = 1.6f,
                    attackCooldownOverride = -1f,
                    enableEnrage = true
                }
            };

            if (existing == null)
                AssetDatabase.CreateAsset(data, BOSS_DATA_PATH);
            else
                EditorUtility.SetDirty(data);

            AssetDatabase.SaveAssets();
            return data;
        }

        // ── Phase Change Event ──────────────────────────────────────────

        private static VoidEventChannel CreateOrLoadPhaseEvent()
        {
            var existing = AssetDatabase.LoadAssetAtPath<VoidEventChannel>(PHASE_EVENT_PATH);
            if (existing != null) return existing;

            var channel = ScriptableObject.CreateInstance<VoidEventChannel>();
            AssetDatabase.CreateAsset(channel, PHASE_EVENT_PATH);
            AssetDatabase.SaveAssets();
            return channel;
        }

        // ── Prefab Setup ────────────────────────────────────────────────

        private static GameObject SetupPrefab(EnemyData enemyData, BossData bossData,
            VoidEventChannel phaseEvent, Sprite whiteSquare)
        {
            int hurtboxLayer = LayerMask.NameToLayer(ENEMY_HURTBOX_LAYER);
            int hitboxLayer = LayerMask.NameToLayer(ENEMY_HITBOX_LAYER);

            if (hurtboxLayer < 0)
                Debug.LogWarning($"[BossPrefabCreator] Layer '{ENEMY_HURTBOX_LAYER}' not found.");
            if (hitboxLayer < 0)
                Debug.LogWarning($"[BossPrefabCreator] Layer '{ENEMY_HITBOX_LAYER}' not found.");

            bool isExisting = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null;
            GameObject root;

            if (isExisting)
                root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            else
                root = new GameObject("TestBoss");

            if (hurtboxLayer >= 0)
                root.layer = hurtboxLayer;

            // Rigidbody2D
            var rb = EnsureComponent<Rigidbody2D>(root);
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Sprite — 1.2x scale, darker red than BasicMeleeEnemy
            var spriteChild = FindOrCreateChild(root, "Sprite");
            spriteChild.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            spriteChild.transform.localScale = new Vector3(0.84f, 1.2f, 1f);
            var sr = EnsureComponent<SpriteRenderer>(spriteChild);
            sr.sprite = whiteSquare;
            sr.color = new Color(0.6f, 0.1f, 0.1f);
            sr.sortingOrder = 1;

            // Body collider (hurtbox)
            var bodyCol = EnsureComponent<BoxCollider2D>(root);
            bodyCol.size = new Vector2(0.84f, 1.2f);
            bodyCol.offset = new Vector2(0f, 0.15f);
            bodyCol.isTrigger = false;

            // BossEnemy (extends EnemyBase)
            var enemy = EnsureComponent<BossEnemy>(root);
            var enemySO = new SerializedObject(enemy);
            var enemyDataProp = enemySO.FindProperty("enemyData");
            if (enemyDataProp != null)
                enemyDataProp.objectReferenceValue = enemyData;
            enemySO.ApplyModifiedPropertiesWithoutUndo();

            // EnemyAI
            var ai = EnsureComponent<EnemyAI>(root);
            var aiSO = new SerializedObject(ai);
            var aiDataProp = aiSO.FindProperty("enemyData");
            if (aiDataProp != null)
                aiDataProp.objectReferenceValue = enemyData;

            int playerLayerIndex = LayerMask.NameToLayer("Player");
            if (playerLayerIndex >= 0)
            {
                var playerLayerProp = aiSO.FindProperty("playerLayer");
                if (playerLayerProp != null)
                    playerLayerProp.intValue = 1 << playerLayerIndex;
            }
            aiSO.ApplyModifiedPropertiesWithoutUndo();

            // BossAI
            var bossAI = EnsureComponent<BossAI>(root);
            var bossAISO = new SerializedObject(bossAI);
            var bossDataProp = bossAISO.FindProperty("bossData");
            if (bossDataProp != null)
                bossDataProp.objectReferenceValue = bossData;
            var eventProp = bossAISO.FindProperty("onBossPhaseChanged");
            if (eventProp != null)
                eventProp.objectReferenceValue = phaseEvent;
            bossAISO.ApplyModifiedPropertiesWithoutUndo();

            // TelegraphVisualController
            var telegraphCtrl = EnsureComponent<TelegraphVisualController>(root);
            var telegraphSO = new SerializedObject(telegraphCtrl);
            var spriteProp = telegraphSO.FindProperty("_sprite");
            if (spriteProp != null)
                spriteProp.objectReferenceValue = sr;
            telegraphSO.ApplyModifiedPropertiesWithoutUndo();

            // Hitbox child (Hitbox_Punch — matches hitboxId on boss attacks)
            var hitboxChild = FindOrCreateChild(root, "Hitbox_Punch");
            if (hitboxLayer >= 0)
                hitboxChild.layer = hitboxLayer;

            var hitboxCol = EnsureComponent<BoxCollider2D>(hitboxChild);
            hitboxCol.isTrigger = true;
            hitboxCol.size = new Vector2(1.0f, 0.8f);
            hitboxCol.offset = new Vector2(-0.7f, 0.15f);

            EnsureComponent<HitboxDamage>(hitboxChild);

            TestDummyPrefabCreator.AddHitboxDebugVisual(hitboxChild, hitboxCol.size, hitboxCol.offset, whiteSquare);

            hitboxChild.SetActive(false);

            // Save prefab
            GameObject savedPrefab;
            if (isExisting)
            {
                savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
                Object.DestroyImmediate(root);
            }

            return savedPrefab;
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        private static GameObject FindOrCreateChild(GameObject parent, string childName)
        {
            var t = parent.transform.Find(childName);
            if (t != null)
                return t.gameObject;

            var child = new GameObject(childName);
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = Vector3.zero;
            return child;
        }
    }
}
