using System.IO;
using TomatoFighters.Combat;
using TomatoFighters.Shared.Components;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.World;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.Prefabs
{
    /// <summary>
    /// Creates the TestDummy enemy prefab with all required components, hitbox child,
    /// EnemyData SO, and a DummyPunch AttackData SO.
    /// Run via menu: <b>TomatoFighters > Create TestDummy Prefab</b>.
    /// Idempotent — safe to re-run.
    /// </summary>
    public static class TestDummyPrefabCreator
    {
        private const string PREFAB_FOLDER = "Assets/Prefabs/Enemies";
        private const string PREFAB_PATH = PREFAB_FOLDER + "/TestDummy.prefab";
        private const string SO_FOLDER = "Assets/ScriptableObjects/Enemies";
        private const string ENEMY_DATA_PATH = SO_FOLDER + "/TestDummy_EnemyData.asset";
        private const string ATTACK_FOLDER = "Assets/ScriptableObjects/Attacks/Enemy";
        private const string ATTACK_DATA_PATH = ATTACK_FOLDER + "/DummyPunch.asset";
        private const string SPRITE_FOLDER = "Assets/Sprites/Debug";
        private const string DEFENSE_CONFIG_FOLDER = "Assets/ScriptableObjects/DefenseConfigs";
        private const string DEFENSE_CONFIG_PATH = DEFENSE_CONFIG_FOLDER + "/TestDummy_DefenseConfig.asset";

        private const string ENEMY_HURTBOX_LAYER = "EnemyHurtbox";
        private const string ENEMY_HITBOX_LAYER = "EnemyHitbox";

        [MenuItem("TomatoFighters/Create TestDummy Prefab")]
        public static void CreateTestDummyPrefab()
        {
            PlayerPrefabCreator.EnsureFolderExists(PREFAB_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(SO_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(ATTACK_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(SPRITE_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(DEFENSE_CONFIG_FOLDER);

            var enemyData = CreateOrLoadEnemyData();
            var attackData = CreateOrLoadDummyPunchAttack();
            var defenseConfig = CreateOrLoadDefenseConfig();

            // Create persistent sprite assets before building prefab
            var whiteSquare = GetOrCreateWhiteSquareSprite();

            bool isNew = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) == null;

            var prefab = SetupPrefab(enemyData, attackData, defenseConfig, whiteSquare);

            string verb = isNew ? "Created" : "Updated";
            Debug.Log($"[TestDummyPrefab] {verb} TestDummy prefab at {PREFAB_PATH}");
            Selection.activeObject = prefab;
        }

        private static EnemyData CreateOrLoadEnemyData()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyData>(ENEMY_DATA_PATH);
            if (existing != null)
                return existing;

            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.maxHealth = 100f;
            data.pressureThreshold = 50f;
            data.stunDuration = 2f;
            data.invulnerabilityDuration = 1f;
            data.knockbackResistance = 0.3f;
            data.movementSpeed = 0f; // Dummy stands still

            AssetDatabase.CreateAsset(data, ENEMY_DATA_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log("[TestDummyPrefab] Created TestDummy_EnemyData.");
            return data;
        }

        private static AttackData CreateOrLoadDummyPunchAttack()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AttackData>(ATTACK_DATA_PATH);
            var attack = existing != null ? existing : ScriptableObject.CreateInstance<AttackData>();

            // Large hitbox window for testing clash timing
            attack.attackId = "dummy_punch";
            attack.attackName = "Dummy Punch";
            attack.damageMultiplier = 0.5f;
            attack.knockbackForce = new Vector2(3f, 0f);
            attack.launchForce = Vector2.zero;
            attack.hitboxId = "Punch";
            attack.hitboxStartFrame = 12;   // ~200ms startup at 60fps — gives player time to react
            attack.hitboxActiveFrames = 18; // ~300ms active window
            attack.totalFrames = 45;        // ~750ms total animation
            attack.animationSpeed = 1f;
            attack.telegraphType = Shared.Enums.TelegraphType.Normal;

            if (existing == null)
                AssetDatabase.CreateAsset(attack, ATTACK_DATA_PATH);
            else
                EditorUtility.SetDirty(attack);
            AssetDatabase.SaveAssets();

            Debug.Log("[TestDummyPrefab] Created/Updated DummyPunch AttackData (large windows for clash testing).");
            return attack;
        }

        private static DefenseConfig CreateOrLoadDefenseConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DefenseConfig>(DEFENSE_CONFIG_PATH);
            if (existing != null)
                return existing;

            // TestDummy only uses clash (during telegraph). No deflect/dodge.
            var config = ScriptableObject.CreateInstance<DefenseConfig>();
            config.deflectWindowDuration = 0f;
            config.clashWindowStart = 0f;
            config.clashWindowEnd = 0f; // Clash window is opened manually via OpenClashWindow
            config.dodgeIFrameStart = 0f;
            config.dodgeIFrameEnd = 0f;

            AssetDatabase.CreateAsset(config, DEFENSE_CONFIG_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log("[TestDummyPrefab] Created TestDummy_DefenseConfig.");
            return config;
        }

        // ── Sprite Asset Creation ─────────────────────────────────────────

        /// <summary>
        /// Creates (or loads) a 1x1 world-unit white square PNG sprite asset.
        /// Used as the base sprite for body and hitbox visuals — tinted via SpriteRenderer.color,
        /// sized via transform.localScale.
        /// </summary>
        public static Sprite GetOrCreateWhiteSquareSprite()
        {
            const string path = SPRITE_FOLDER + "/WhiteSquare.png";

            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
                return existing;

            PlayerPrefabCreator.EnsureFolderExists(SPRITE_FOLDER);

            // Create a 10x10 white texture → at 10 PPU renders as 1x1 world unit
            const int size = 10;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            string fullPath = Path.Combine(Application.dataPath, path.Substring("Assets/".Length));
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);

            // Configure as sprite
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 10;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Debug.Log($"[TestDummyPrefab] Created WhiteSquare sprite at {path}");
            return sprite;
        }

        // ── Attack Data Factory ───────────────────────────────────────────

        /// <summary>
        /// Creates or updates an AttackData SO at the given path.
        /// Used by MovementTestSceneCreator to set up tiered dummies.
        /// </summary>
        public static AttackData CreateOrLoadAttackData(
            string path, string attackId, string attackName,
            float damageMultiplier, Vector2 knockbackForce, Vector2 launchForce,
            TelegraphType telegraphType)
        {
            PlayerPrefabCreator.EnsureFolderExists(ATTACK_FOLDER);

            var existing = AssetDatabase.LoadAssetAtPath<AttackData>(path);
            var attack = existing != null ? existing : ScriptableObject.CreateInstance<AttackData>();

            attack.attackId = attackId;
            attack.attackName = attackName;
            attack.damageMultiplier = damageMultiplier;
            attack.knockbackForce = knockbackForce;
            attack.launchForce = launchForce;
            attack.hitboxId = "Punch";
            attack.hitboxStartFrame = 12;
            attack.hitboxActiveFrames = 18;
            attack.totalFrames = 45;
            attack.animationSpeed = 1f;
            attack.telegraphType = telegraphType;

            if (existing == null)
                AssetDatabase.CreateAsset(attack, path);
            else
                EditorUtility.SetDirty(attack);
            AssetDatabase.SaveAssets();

            return attack;
        }

        // ── Prefab Setup ──────────────────────────────────────────────────

        private static GameObject SetupPrefab(EnemyData enemyData, AttackData attackData,
            DefenseConfig defenseConfig, Sprite whiteSquare)
        {
            int hurtboxLayer = LayerMask.NameToLayer(ENEMY_HURTBOX_LAYER);
            int hitboxLayer = LayerMask.NameToLayer(ENEMY_HITBOX_LAYER);

            if (hurtboxLayer < 0)
                Debug.LogWarning(
                    $"[TestDummyPrefab] Layer '{ENEMY_HURTBOX_LAYER}' not found. " +
                    "Add it in Edit > Project Settings > Tags and Layers.");

            if (hitboxLayer < 0)
                Debug.LogWarning(
                    $"[TestDummyPrefab] Layer '{ENEMY_HITBOX_LAYER}' not found. " +
                    "Add it in Edit > Project Settings > Tags and Layers.");

            GameObject root;
            bool isExisting = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null;

            if (isExisting)
                root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            else
                root = new GameObject("TestDummy");

            // -- Layer --
            if (hurtboxLayer >= 0)
                root.layer = hurtboxLayer;

            // -- Rigidbody2D --
            var rb = EnsureComponent<Rigidbody2D>(root);
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // -- Body visual (child, found by EnemyBase.GetComponentInChildren<SpriteRenderer>) --
            // White square sprite tinted orange, scaled to body collider size (0.8 x 1.2)
            var spriteChild = FindOrCreateChild(root, "Sprite");
            spriteChild.transform.localPosition = new Vector3(0f, 0.1f, 0f); // Match collider offset
            spriteChild.transform.localScale = new Vector3(0.8f, 1.2f, 1f);
            var sr = EnsureComponent<SpriteRenderer>(spriteChild);
            sr.sprite = whiteSquare;
            sr.color = new Color(1f, 0.6f, 0.15f); // Orange enemy
            sr.sortingOrder = 1;

            // -- Body collider (hurtbox — can be hit by player) --
            var bodyCol = EnsureComponent<BoxCollider2D>(root);
            bodyCol.size = new Vector2(0.8f, 1.2f);
            bodyCol.offset = new Vector2(0f, 0.1f);
            bodyCol.isTrigger = false;

            // -- TestDummyEnemy component --
            var dummy = EnsureComponent<TestDummyEnemy>(root);
            var dummySO = new SerializedObject(dummy);

            // Wire EnemyData (on EnemyBase's private field)
            var enemyDataProp = dummySO.FindProperty("enemyData");
            if (enemyDataProp != null)
                enemyDataProp.objectReferenceValue = enemyData;

            // Wire attack settings
            var intervalProp = dummySO.FindProperty("attackInterval");
            if (intervalProp != null)
                intervalProp.floatValue = 3f;

            var attackDataProp = dummySO.FindProperty("attackData");
            if (attackDataProp != null)
                attackDataProp.objectReferenceValue = attackData;

            // Large windows for clash testing:
            // 1s telegraph (react window) → 1.5s hitbox active (overlap window)
            var activeDurProp = dummySO.FindProperty("attackActiveDuration");
            if (activeDurProp != null)
                activeDurProp.floatValue = 1.5f;

            var telegraphProp = dummySO.FindProperty("telegraphDuration");
            if (telegraphProp != null)
                telegraphProp.floatValue = 1.0f;

            // -- Hitbox_Punch child --
            var hitboxChild = FindOrCreateChild(root, "Hitbox_Punch");
            if (hitboxLayer >= 0)
                hitboxChild.layer = hitboxLayer;

            var hitboxCol = EnsureComponent<BoxCollider2D>(hitboxChild);
            hitboxCol.isTrigger = true;
            hitboxCol.size = new Vector2(0.8f, 0.6f);
            hitboxCol.offset = new Vector2(-0.6f, 0.1f);

            var hitboxDmg = EnsureComponent<HitboxDamage>(hitboxChild);

            // Debug visual — red sprite matching collider area, visible when hitbox is active
            AddHitboxDebugVisual(hitboxChild, hitboxCol.size, hitboxCol.offset, whiteSquare);

            hitboxChild.SetActive(false);

            // Wire hitbox reference on TestDummyEnemy
            var hitboxProp = dummySO.FindProperty("hitbox");
            if (hitboxProp != null)
                hitboxProp.objectReferenceValue = hitboxDmg;

            // -- DefenseSystem (enables clash during telegraph) --
            var defenseSystem = EnsureComponent<DefenseSystem>(root);
            var defSO = new SerializedObject(defenseSystem);
            defSO.FindProperty("config").objectReferenceValue = defenseConfig;
            // Enemy has no motor or comboController — clash window opened manually
            defSO.ApplyModifiedPropertiesWithoutUndo();

            // -- ClashTracker (per-activation clash immunity) --
            var clashTracker = EnsureComponent<ClashTracker>(root);

            // Wire defenseProviderComponent on EnemyBase to the DefenseSystem
            dummySO.FindProperty("defenseProviderComponent").objectReferenceValue = defenseSystem;
            dummySO.FindProperty("clashTracker").objectReferenceValue = clashTracker;
            dummySO.ApplyModifiedPropertiesWithoutUndo();

            // -- DebugHealthBar (temp HP bar, replaced by T025 HUD) --
            var healthBar = EnsureComponent<DebugHealthBar>(root);
            var hbSO = new SerializedObject(healthBar);
            var offsetProp = hbSO.FindProperty("offset");
            if (offsetProp != null)
                offsetProp.vector3Value = new Vector3(0f, 1.2f, 0f);
            var fillColorProp = hbSO.FindProperty("fillColor");
            if (fillColorProp != null)
                fillColorProp.colorValue = new Color(1f, 0.3f, 0.2f); // Red for enemies
            hbSO.ApplyModifiedPropertiesWithoutUndo();

            // Also wire the EnemyData attacks array to include this attack
            var dataSO = new SerializedObject(enemyData);
            var attacksProp = dataSO.FindProperty("attacks");
            if (attacksProp != null && attacksProp.arraySize == 0)
            {
                attacksProp.arraySize = 1;
                attacksProp.GetArrayElementAtIndex(0).objectReferenceValue = attackData;
                dataSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // -- Save --
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

        // ── Hitbox Debug Visual ───────────────────────────────────────────

        /// <summary>
        /// Adds a child sprite to a hitbox GameObject that shows a semi-transparent red area
        /// matching the collider size and offset. Visible whenever the hitbox parent is active.
        /// Uses a persistent sprite asset so the visual survives prefab serialization.
        /// </summary>
        public static void AddHitboxDebugVisual(GameObject hitboxGO, Vector2 colliderSize,
            Vector2 colliderOffset, Sprite whiteSquare = null)
        {
            if (whiteSquare == null)
                whiteSquare = GetOrCreateWhiteSquareSprite();

            const string visualName = "DebugVisual";
            var existingT = hitboxGO.transform.Find(visualName);

            GameObject visual;
            SpriteRenderer sr;

            if (existingT != null)
            {
                // Update existing — fixes stale null sprites from previous runs
                visual = existingT.gameObject;
                sr = visual.GetComponent<SpriteRenderer>();
                if (sr == null)
                    sr = visual.AddComponent<SpriteRenderer>();
            }
            else
            {
                visual = new GameObject(visualName);
                visual.transform.SetParent(hitboxGO.transform, false);
                sr = visual.AddComponent<SpriteRenderer>();
            }

            visual.transform.localPosition = new Vector3(colliderOffset.x, colliderOffset.y, 0f);
            visual.transform.localScale = new Vector3(colliderSize.x, colliderSize.y, 1f);
            sr.sprite = whiteSquare;
            sr.color = new Color(1f, 0f, 0f, 0.4f); // Semi-transparent red
            sr.sortingOrder = 10;
        }

        // ── Helpers ───────────────────────────────────────────────────────

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
