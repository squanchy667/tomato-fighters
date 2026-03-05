using TomatoFighters.Shared.Data;
using TomatoFighters.World;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.Prefabs
{
    /// <summary>
    /// Creates the Sproutling companion prefab and its EnemyData SO.
    /// Sproutling is a summoned companion that targets the enemy layer (DD-7).
    /// Run via menu: <b>TomatoFighters > Create Sproutling Prefab</b>.
    /// </summary>
    public static class SproutlingPrefabCreator
    {
        private const string PREFAB_FOLDER = "Assets/Prefabs/Companions";
        private const string PREFAB_PATH = PREFAB_FOLDER + "/Sproutling.prefab";
        private const string SO_FOLDER = "Assets/ScriptableObjects/Companions";
        private const string ENEMY_DATA_PATH = SO_FOLDER + "/Sproutling_EnemyData.asset";

        [MenuItem("TomatoFighters/Create Sproutling Prefab")]
        public static void CreateSproutlingPrefab()
        {
            PlayerPrefabCreator.EnsureFolderExists(PREFAB_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(SO_FOLDER);

            var enemyData = CreateOrLoadEnemyData();
            var whiteSquare = TestDummyPrefabCreator.GetOrCreateWhiteSquareSprite();

            bool isNew = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) == null;
            var prefab = SetupPrefab(enemyData, whiteSquare);

            string verb = isNew ? "Created" : "Updated";
            Debug.Log($"[SproutlingPrefab] {verb} Sproutling prefab at {PREFAB_PATH}");
            Selection.activeObject = prefab;
        }

        private static EnemyData CreateOrLoadEnemyData()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyData>(ENEMY_DATA_PATH);
            var data = existing != null ? existing : ScriptableObject.CreateInstance<EnemyData>();

            data.maxHealth = 40f;
            data.pressureThreshold = 999f; // Cannot be stunned
            data.stunDuration = 0f;
            data.invulnerabilityDuration = 0f;
            data.knockbackResistance = 0.5f;
            data.movementSpeed = 4f;

            // AI behavior — targets enemies
            data.aggroRange = 6f;
            data.attackRange = 1.2f;
            data.patrolRadius = 2f;
            data.leashRange = 10f;
            data.idleDuration = 0.5f;
            data.attackCooldown = 1.5f;
            data.aggression = 0.9f;
            data.hitReactDuration = 0.15f;
            data.telegraphDuration = 0.2f;

            if (existing == null)
                AssetDatabase.CreateAsset(data, ENEMY_DATA_PATH);
            else
                EditorUtility.SetDirty(data);

            AssetDatabase.SaveAssets();
            return data;
        }

        private static GameObject SetupPrefab(EnemyData enemyData, Sprite whiteSquare)
        {
            bool isExisting = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null;
            GameObject root;

            if (isExisting)
                root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            else
                root = new GameObject("Sproutling");

            // Rigidbody2D
            var rb = PlayerPrefabCreator.EnsureComponent<Rigidbody2D>(root);
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Collider
            var col = PlayerPrefabCreator.EnsureComponent<BoxCollider2D>(root);
            col.size = new Vector2(0.5f, 0.5f);

            // Visual
            var spriteChild = PlayerPrefabCreator.FindOrCreateChild(root, "Sprite");
            spriteChild.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            var sr = PlayerPrefabCreator.EnsureComponent<SpriteRenderer>(spriteChild);
            sr.sprite = whiteSquare;
            sr.color = new Color(0.3f, 0.8f, 0.3f); // Green sproutling
            sr.sortingOrder = 1;

            // SproutlingEnemy component
            var sproutling = PlayerPrefabCreator.EnsureComponent<SproutlingEnemy>(root);
            var sproutSO = new SerializedObject(sproutling);
            var dataProp = sproutSO.FindProperty("enemyData");
            if (dataProp != null)
                dataProp.objectReferenceValue = enemyData;
            sproutSO.ApplyModifiedPropertiesWithoutUndo();

            // EnemyAI — targets enemy layer instead of player layer
            var ai = PlayerPrefabCreator.EnsureComponent<EnemyAI>(root);
            var aiSO = new SerializedObject(ai);
            var aiDataProp = aiSO.FindProperty("enemyData");
            if (aiDataProp != null)
                aiDataProp.objectReferenceValue = enemyData;

            // Target enemy hurtbox layer (inverted targeting)
            int enemyLayer = LayerMask.NameToLayer("EnemyHurtbox");
            if (enemyLayer >= 0)
            {
                var layerProp = aiSO.FindProperty("playerLayer");
                if (layerProp != null)
                    layerProp.intValue = 1 << enemyLayer;
            }
            aiSO.ApplyModifiedPropertiesWithoutUndo();

            // Save
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
    }
}
