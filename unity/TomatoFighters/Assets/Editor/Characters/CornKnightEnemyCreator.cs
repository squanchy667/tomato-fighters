using TomatoFighters.Editor.Prefabs;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.World;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TomatoFighters.Editor.Characters
{
    /// <summary>
    /// Creates a Corn Knight enemy prefab with Animator, EnemyData, EnemyAI, and attack patterns.
    /// Run via menu: <b>TomatoFighters > Create CornKnight Enemy Prefab</b>.
    /// </summary>
    public static class CornKnightEnemyCreator
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Enemies/CornKnight.prefab";
        private const string SO_FOLDER = "Assets/ScriptableObjects/Enemies";
        private const string ENEMY_DATA_PATH = SO_FOLDER + "/CornKnight_EnemyData.asset";
        private const string ATTACK_FOLDER = "Assets/ScriptableObjects/Attacks/Enemy/CornKnight";
        private const string SLASH_ATTACK_PATH = ATTACK_FOLDER + "/CornKnightSlash.asset";
        private const string OVERRIDE_PATH = "Assets/Animations/Enemies/CornKnight/CornKnight_Override.overrideController";

        [MenuItem("TomatoFighters/Create CornKnight Enemy Prefab")]
        public static void Create()
        {
            PlayerPrefabCreator.EnsureFolderExists(SO_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(ATTACK_FOLDER);

            var enemyData = CreateOrLoadEnemyData();
            var slashAttack = CreateOrLoadSlashAttack();

            // Wire attack into EnemyData
            var dataSO = new SerializedObject(enemyData);
            var attacksProp = dataSO.FindProperty("attacks");
            attacksProp.arraySize = 1;
            attacksProp.GetArrayElementAtIndex(0).objectReferenceValue = slashAttack;
            dataSO.ApplyModifiedPropertiesWithoutUndo();

            // Load override controller
            var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(OVERRIDE_PATH);
            if (overrideController == null)
                Debug.LogWarning($"[CornKnightEnemyCreator] Override controller not found at {OVERRIDE_PATH}. " +
                    "Run 'TomatoFighters > Build Animations > All Characters' first. Prefab will have no animator.");

            var whiteSquare = TestDummyPrefabCreator.GetOrCreateWhiteSquareSprite();

            var config = new EnemyPrefabConfig
            {
                prefabPath = PREFAB_PATH,
                enemyType = "CornKnight",
                enemyDataAsset = enemyData,
                animatorController = overrideController,
                bodySprite = whiteSquare,
                spriteColor = new Color(1f, 0.85f, 0.2f), // Yellow-corn color
                bodySize = new Vector2(0.8f, 1.4f),
                bodyOffset = new Vector2(0f, 0.1f),
                hitboxDefinitions = new[]
                {
                    new HitboxDefinition
                    {
                        hitboxId = "Punch",
                        shape = HitboxShape.Box,
                        boxSize = new Vector2(0.8f, 0.6f),
                        offset = new Vector2(-0.6f, 0.1f),
                    }
                },
            };

            var prefab = EnemyPrefabCreator.CreateEnemyPrefab(config);

            // Add EnemyAI on top of what EnemyPrefabCreator built
            WireEnemyAI(enemyData);
        }

        private static EnemyData CreateOrLoadEnemyData()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyData>(ENEMY_DATA_PATH);
            var data = existing != null ? existing : ScriptableObject.CreateInstance<EnemyData>();

            data.maxHealth = 90f;
            data.pressureThreshold = 45f;
            data.stunDuration = 1.2f;
            data.invulnerabilityDuration = 0.7f;
            data.knockbackResistance = 0.3f;
            data.movementSpeed = 3.5f;

            data.aggroRange = 7f;
            data.attackRange = 1.6f;
            data.patrolRadius = 2f;
            data.leashRange = 12f;
            data.idleDuration = 0.8f;
            data.attackCooldown = 1.0f;
            data.aggression = 0.6f;
            data.hitReactDuration = 0.2f;
            data.telegraphDuration = 0.35f;

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

            attack.attackId = "cornknight_slash";
            attack.attackName = "Corn Knight Slash";
            attack.damageMultiplier = 0.9f;
            attack.knockbackForce = new Vector2(3.5f, 0f);
            attack.launchForce = Vector2.zero;
            attack.hitboxId = "Punch";
            attack.hitboxStartFrame = 3;
            attack.hitboxActiveFrames = 4;
            attack.totalFrames = 10;
            attack.animationSpeed = 1f;
            attack.telegraphType = TelegraphType.Normal;

            if (existing == null)
                AssetDatabase.CreateAsset(attack, SLASH_ATTACK_PATH);
            else
                EditorUtility.SetDirty(attack);

            AssetDatabase.SaveAssets();
            return attack;
        }

        private static void WireEnemyAI(EnemyData enemyData)
        {
            var root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);

            var ai = root.GetComponent<EnemyAI>();
            if (ai == null)
                ai = root.AddComponent<EnemyAI>();

            var aiSO = new SerializedObject(ai);
            var aiDataProp = aiSO.FindProperty("enemyData");
            if (aiDataProp != null)
                aiDataProp.objectReferenceValue = enemyData;

            int playerHurtbox = LayerMask.NameToLayer("PlayerHurtbox");
            if (playerHurtbox >= 0)
            {
                var playerLayerProp = aiSO.FindProperty("playerLayer");
                if (playerLayerProp != null)
                    playerLayerProp.intValue = 1 << playerHurtbox;
            }
            aiSO.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
