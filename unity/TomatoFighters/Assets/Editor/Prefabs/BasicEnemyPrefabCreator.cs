using TomatoFighters.Shared.Components;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.World;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.Prefabs
{
    /// <summary>
    /// Creates the BasicMeleeEnemy prefab with EnemyAI state machine,
    /// hitbox child, EnemyData SO with AI behavior fields, and attack data.
    /// Run via menu: <b>TomatoFighters > Create BasicMeleeEnemy Prefab</b>.
    /// Idempotent — safe to re-run.
    /// </summary>
    public static class BasicEnemyPrefabCreator
    {
        private const string PREFAB_FOLDER = "Assets/Prefabs/Enemies";
        private const string PREFAB_PATH = PREFAB_FOLDER + "/BasicMeleeEnemy.prefab";
        private const string SO_FOLDER = "Assets/ScriptableObjects/Enemies";
        private const string ENEMY_DATA_PATH = SO_FOLDER + "/BasicMelee_EnemyData.asset";
        private const string ATTACK_FOLDER = "Assets/ScriptableObjects/Attacks/Enemy";
        private const string SLASH_ATTACK_PATH = ATTACK_FOLDER + "/BasicSlash.asset";
        private const string HEAVY_ATTACK_PATH = ATTACK_FOLDER + "/BasicHeavy.asset";

        private const string PATTERN_FOLDER = "Assets/ScriptableObjects/Enemies/Patterns";
        private const string QUICK_JABS_PATH = PATTERN_FOLDER + "/BasicMelee_QuickJabs.asset";
        private const string SLASH_PATTERN_PATH = PATTERN_FOLDER + "/BasicMelee_Slash.asset";
        private const string HEAVY_SLAM_PATH = PATTERN_FOLDER + "/BasicMelee_HeavySlam.asset";

        private const string ENEMY_HURTBOX_LAYER = "EnemyHurtbox";
        private const string ENEMY_HITBOX_LAYER = "EnemyHitbox";

        [MenuItem("TomatoFighters/Create BasicMeleeEnemy Prefab")]
        public static void CreateBasicMeleeEnemyPrefab()
        {
            PlayerPrefabCreator.EnsureFolderExists(PREFAB_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(SO_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(ATTACK_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(PATTERN_FOLDER);

            var enemyData = CreateOrLoadEnemyData();
            var slashAttack = CreateOrLoadSlashAttack();
            var heavyAttack = CreateOrLoadHeavyAttack();

            // Wire attacks into EnemyData
            var dataSO = new SerializedObject(enemyData);
            var attacksProp = dataSO.FindProperty("attacks");
            attacksProp.arraySize = 2;
            attacksProp.GetArrayElementAtIndex(0).objectReferenceValue = slashAttack;
            attacksProp.GetArrayElementAtIndex(1).objectReferenceValue = heavyAttack;
            dataSO.ApplyModifiedPropertiesWithoutUndo();

            // Create and wire attack patterns
            var quickJabs = CreateOrLoadQuickJabsPattern(slashAttack);
            var slashPattern = CreateOrLoadSlashPattern(slashAttack);
            var heavySlam = CreateOrLoadHeavySlamPattern(heavyAttack);

            var dataSO2 = new SerializedObject(enemyData);
            var patternsProp = dataSO2.FindProperty("attackPatterns");
            patternsProp.arraySize = 3;
            patternsProp.GetArrayElementAtIndex(0).objectReferenceValue = quickJabs;
            patternsProp.GetArrayElementAtIndex(1).objectReferenceValue = slashPattern;
            patternsProp.GetArrayElementAtIndex(2).objectReferenceValue = heavySlam;
            dataSO2.ApplyModifiedPropertiesWithoutUndo();

            var whiteSquare = TestDummyPrefabCreator.GetOrCreateWhiteSquareSprite();

            bool isNew = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) == null;
            var prefab = SetupPrefab(enemyData, whiteSquare);

            string verb = isNew ? "Created" : "Updated";
            Debug.Log($"[BasicEnemyPrefab] {verb} BasicMeleeEnemy prefab at {PREFAB_PATH}");
            Selection.activeObject = prefab;
        }

        // ── ScriptableObject Creation ────────────────────────────────────

        private static EnemyData CreateOrLoadEnemyData()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyData>(ENEMY_DATA_PATH);
            var data = existing != null ? existing : ScriptableObject.CreateInstance<EnemyData>();

            // Combat stats
            data.maxHealth = 80f;
            data.pressureThreshold = 40f;
            data.stunDuration = 1.5f;
            data.invulnerabilityDuration = 0.8f;
            data.knockbackResistance = 0.2f;
            data.movementSpeed = 5.5f;

            // AI behavior
            data.aggroRange = 9f;
            data.attackRange = 1.4f;
            data.patrolRadius = 3f;
            data.leashRange = 14f;
            data.idleDuration = 0.8f;
            data.attackCooldown = 0.8f;
            data.aggression = 0.7f;
            data.hitReactDuration = 0.2f;
            data.telegraphDuration = 0.25f;

            if (existing == null)
                AssetDatabase.CreateAsset(data, ENEMY_DATA_PATH);
            else
                EditorUtility.SetDirty(data);

            AssetDatabase.SaveAssets();
            return data;
        }

        private static AttackData CreateOrLoadSlashAttack()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AttackData>(SLASH_ATTACK_PATH);
            var attack = existing != null ? existing : ScriptableObject.CreateInstance<AttackData>();

            attack.attackId = "basic_slash";
            attack.attackName = "Basic Slash";
            attack.damageMultiplier = 0.6f;
            attack.knockbackForce = new Vector2(2f, 0f);
            attack.launchForce = Vector2.zero;
            attack.hitboxId = "Punch";
            attack.hitboxStartFrame = 8;
            attack.hitboxActiveFrames = 6;
            attack.totalFrames = 30;
            attack.animationSpeed = 1f;
            attack.telegraphType = TelegraphType.Normal;

            if (existing == null)
                AssetDatabase.CreateAsset(attack, SLASH_ATTACK_PATH);
            else
                EditorUtility.SetDirty(attack);

            AssetDatabase.SaveAssets();
            return attack;
        }

        private static AttackData CreateOrLoadHeavyAttack()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AttackData>(HEAVY_ATTACK_PATH);
            var attack = existing != null ? existing : ScriptableObject.CreateInstance<AttackData>();

            attack.attackId = "basic_heavy";
            attack.attackName = "Basic Heavy Slam";
            attack.damageMultiplier = 1.2f;
            attack.knockbackForce = new Vector2(4f, 0.5f);
            attack.launchForce = Vector2.zero;
            attack.hitboxId = "Punch";
            attack.hitboxStartFrame = 14;
            attack.hitboxActiveFrames = 8;
            attack.totalFrames = 45;
            attack.animationSpeed = 1f;
            attack.telegraphType = TelegraphType.Unstoppable;

            if (existing == null)
                AssetDatabase.CreateAsset(attack, HEAVY_ATTACK_PATH);
            else
                EditorUtility.SetDirty(attack);

            AssetDatabase.SaveAssets();
            return attack;
        }

        // ── Attack Pattern Creation ────────────────────────────────────────

        private static EnemyAttackPattern CreateOrLoadQuickJabsPattern(AttackData slashAttack)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>(QUICK_JABS_PATH);
            var pattern = existing != null ? existing : ScriptableObject.CreateInstance<EnemyAttackPattern>();

            pattern.patternName = "Quick Jabs";
            pattern.selectionWeight = 2.0f;
            pattern.minRange = 0f;
            pattern.maxRange = 1.5f;
            pattern.patternCooldown = 1.5f;
            pattern.steps = new AttackPatternStep[]
            {
                new AttackPatternStep { attack = slashAttack, delayBeforeStep = 0f },
                new AttackPatternStep { attack = slashAttack, delayBeforeStep = 0.2f }
            };

            if (existing == null)
                AssetDatabase.CreateAsset(pattern, QUICK_JABS_PATH);
            else
                EditorUtility.SetDirty(pattern);

            AssetDatabase.SaveAssets();
            return pattern;
        }

        private static EnemyAttackPattern CreateOrLoadSlashPattern(AttackData slashAttack)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>(SLASH_PATTERN_PATH);
            var pattern = existing != null ? existing : ScriptableObject.CreateInstance<EnemyAttackPattern>();

            pattern.patternName = "Slash";
            pattern.selectionWeight = 1.5f;
            pattern.minRange = 0f;
            pattern.maxRange = 2.0f;
            pattern.patternCooldown = 1.0f;
            pattern.steps = new AttackPatternStep[]
            {
                new AttackPatternStep { attack = slashAttack, delayBeforeStep = 0f }
            };

            if (existing == null)
                AssetDatabase.CreateAsset(pattern, SLASH_PATTERN_PATH);
            else
                EditorUtility.SetDirty(pattern);

            AssetDatabase.SaveAssets();
            return pattern;
        }

        private static EnemyAttackPattern CreateOrLoadHeavySlamPattern(AttackData heavyAttack)
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyAttackPattern>(HEAVY_SLAM_PATH);
            var pattern = existing != null ? existing : ScriptableObject.CreateInstance<EnemyAttackPattern>();

            pattern.patternName = "Heavy Slam";
            pattern.selectionWeight = 0.5f;
            pattern.minRange = 0f;
            pattern.maxRange = 1.2f;
            pattern.patternCooldown = 4.0f;
            pattern.steps = new AttackPatternStep[]
            {
                new AttackPatternStep { attack = heavyAttack, delayBeforeStep = 0f }
            };

            if (existing == null)
                AssetDatabase.CreateAsset(pattern, HEAVY_SLAM_PATH);
            else
                EditorUtility.SetDirty(pattern);

            AssetDatabase.SaveAssets();
            return pattern;
        }

        // ── Prefab Setup ─────────────────────────────────────────────────

        private static GameObject SetupPrefab(EnemyData enemyData, Sprite whiteSquare)
        {
            int hurtboxLayer = LayerMask.NameToLayer(ENEMY_HURTBOX_LAYER);
            int hitboxLayer = LayerMask.NameToLayer(ENEMY_HITBOX_LAYER);

            if (hurtboxLayer < 0)
                Debug.LogWarning($"[BasicEnemyPrefab] Layer '{ENEMY_HURTBOX_LAYER}' not found.");
            if (hitboxLayer < 0)
                Debug.LogWarning($"[BasicEnemyPrefab] Layer '{ENEMY_HITBOX_LAYER}' not found.");

            bool isExisting = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null;
            GameObject root;

            if (isExisting)
                root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            else
                root = new GameObject("BasicMeleeEnemy");

            // Layer
            if (hurtboxLayer >= 0)
                root.layer = hurtboxLayer;

            // Rigidbody2D
            var rb = EnsureComponent<Rigidbody2D>(root);
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Body visual
            var spriteChild = FindOrCreateChild(root, "Sprite");
            spriteChild.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            spriteChild.transform.localScale = new Vector3(0.7f, 1.0f, 1f);
            var sr = EnsureComponent<SpriteRenderer>(spriteChild);
            sr.sprite = whiteSquare;
            sr.color = new Color(0.8f, 0.2f, 0.2f); // Dark red enemy
            sr.sortingOrder = 1;

            // Body collider (hurtbox)
            var bodyCol = EnsureComponent<BoxCollider2D>(root);
            bodyCol.size = new Vector2(0.7f, 1.0f);
            bodyCol.offset = new Vector2(0f, 0.1f);
            bodyCol.isTrigger = false;

            // BasicMeleeEnemy component
            var enemy = EnsureComponent<BasicMeleeEnemy>(root);
            var enemySO = new SerializedObject(enemy);
            var enemyDataProp = enemySO.FindProperty("enemyData");
            if (enemyDataProp != null)
                enemyDataProp.objectReferenceValue = enemyData;
            enemySO.ApplyModifiedPropertiesWithoutUndo();

            // EnemyAI component
            var ai = EnsureComponent<EnemyAI>(root);
            var aiSO = new SerializedObject(ai);

            // Wire EnemyData on EnemyAI (same SO as on EnemyBase)
            var aiDataProp = aiSO.FindProperty("enemyData");
            if (aiDataProp != null)
                aiDataProp.objectReferenceValue = enemyData;

            // Set player layer mask
            int playerLayerIndex = LayerMask.NameToLayer("Player");
            if (playerLayerIndex >= 0)
            {
                var playerLayerProp = aiSO.FindProperty("playerLayer");
                if (playerLayerProp != null)
                    playerLayerProp.intValue = 1 << playerLayerIndex;
            }
            else
            {
                Debug.LogWarning("[BasicEnemyPrefab] Layer 'Player' not found. " +
                    "Set playerLayer on EnemyAI manually.");
            }
            aiSO.ApplyModifiedPropertiesWithoutUndo();

            // StatusEffectTracker (T028) — enables abilities to apply status effects
            EnsureComponent<StatusEffectTracker>(root);

            // TelegraphVisualController — wired to the sprite child's SpriteRenderer
            var telegraphCtrl = EnsureComponent<TelegraphVisualController>(root);
            var telegraphSO = new SerializedObject(telegraphCtrl);
            var spriteProp = telegraphSO.FindProperty("_sprite");
            if (spriteProp != null)
                spriteProp.objectReferenceValue = sr;
            telegraphSO.ApplyModifiedPropertiesWithoutUndo();

            // Hitbox child (Hitbox_Punch — matches hitboxId on attacks)
            var hitboxChild = FindOrCreateChild(root, "Hitbox_Punch");
            if (hitboxLayer >= 0)
                hitboxChild.layer = hitboxLayer;

            var hitboxCol = EnsureComponent<BoxCollider2D>(hitboxChild);
            hitboxCol.isTrigger = true;
            hitboxCol.size = new Vector2(0.8f, 0.6f);
            hitboxCol.offset = new Vector2(-0.6f, 0.1f);

            EnsureComponent<HitboxDamage>(hitboxChild);

            // Debug visual on hitbox
            TestDummyPrefabCreator.AddHitboxDebugVisual(hitboxChild, hitboxCol.size, hitboxCol.offset, whiteSquare);

            hitboxChild.SetActive(false);

            // Remove legacy DebugHealthBar if present (replaced by EnemyHealthBarUI)
            var legacyBar = root.GetComponent<DebugHealthBar>();
            if (legacyBar != null)
                Object.DestroyImmediate(legacyBar);

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

        // ── Helpers ──────────────────────────────────────────────────────

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
