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
    /// Creates a Tomato enemy prefab with Animator, EnemyData, EnemyAI, and attack patterns.
    /// Run via menu: <b>TomatoFighters > Create Tomato Enemy Prefab</b>.
    /// </summary>
    public static class TomatoEnemyCreator
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Enemies/Tomato.prefab";
        private const string SO_FOLDER = "Assets/ScriptableObjects/Enemies";
        private const string ENEMY_DATA_PATH = SO_FOLDER + "/Tomato_EnemyData.asset";
        private const string ATTACK_FOLDER = "Assets/ScriptableObjects/Attacks/Enemy/Tomato";
        private const string SMASH_ATTACK_PATH = ATTACK_FOLDER + "/TomatoSmash.asset";
        private const string OVERRIDE_PATH = "Assets/Animations/Enemies/Tomato/Tomato_Override.overrideController";

        [MenuItem("TomatoFighters/Create Tomato Enemy Prefab")]
        public static void Create()
        {
            PlayerPrefabCreator.EnsureFolderExists(SO_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(ATTACK_FOLDER);

            var enemyData = CreateOrLoadEnemyData();
            var smashAttack = CreateOrLoadSmashAttack();

            // Wire attack into EnemyData
            var dataSO = new SerializedObject(enemyData);
            var attacksProp = dataSO.FindProperty("attacks");
            attacksProp.arraySize = 1;
            attacksProp.GetArrayElementAtIndex(0).objectReferenceValue = smashAttack;
            dataSO.ApplyModifiedPropertiesWithoutUndo();

            // Load override controller (built by animation pipeline)
            var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(OVERRIDE_PATH);
            if (overrideController == null)
                Debug.LogWarning($"[TomatoEnemyCreator] Override controller not found at {OVERRIDE_PATH}. " +
                    "Run 'TomatoFighters > Build Animations > All Characters' first. Prefab will have no animator.");

            // Load first idle sprite for body visual
            Sprite bodySprite = null;
            var idleSprites = AssetDatabase.LoadAllAssetsAtPath(
                "Assets/animations/tomato_animations/Sprites/tomato_idle.png");
            foreach (var asset in idleSprites)
            {
                if (asset is Sprite s && s.name.Contains("_0"))
                {
                    bodySprite = s;
                    break;
                }
            }
            if (bodySprite == null)
                bodySprite = TestDummyPrefabCreator.GetOrCreateWhiteSquareSprite();

            var config = new EnemyPrefabConfig
            {
                prefabPath = PREFAB_PATH,
                enemyType = "Tomato",
                enemyDataAsset = enemyData,
                animatorController = overrideController,
                bodySprite = bodySprite,
                spriteColor = Color.white, // Use actual sprite colors
                bodySize = new Vector2(0.7f, 1.2f),
                bodyOffset = new Vector2(0f, 0.1f),
                hitboxDefinitions = new[]
                {
                    new HitboxDefinition
                    {
                        hitboxId = "Punch",
                        shape = HitboxShape.Box,
                        boxSize = new Vector2(0.9f, 0.7f),
                        offset = new Vector2(-0.6f, 0.1f),
                    }
                },
            };

            EnemyPrefabCreator.CreateEnemyPrefab(config);

            // Wire EnemyAI (auto-added by BasicMeleeEnemy's RequireComponent)
            WireEnemyAI(enemyData);
        }

        private static EnemyData CreateOrLoadEnemyData()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyData>(ENEMY_DATA_PATH);
            var data = existing != null ? existing : ScriptableObject.CreateInstance<EnemyData>();

            data.maxHealth = 60f;
            data.pressureThreshold = 30f;
            data.stunDuration = 1.5f;
            data.invulnerabilityDuration = 0.6f;
            data.knockbackResistance = 0.1f;
            data.movementSpeed = 4.5f;

            data.aggroRange = 8f;
            data.attackRange = 1.5f;
            data.patrolRadius = 2.5f;
            data.leashRange = 12f;
            data.idleDuration = 1.0f;
            data.attackCooldown = 1.2f;
            data.aggression = 0.5f;
            data.hitReactDuration = 0.25f;
            data.telegraphDuration = 0.3f;

            if (existing == null)
                AssetDatabase.CreateAsset(data, ENEMY_DATA_PATH);
            else
                EditorUtility.SetDirty(data);

            AssetDatabase.SaveAssets();
            return data;
        }

        private static AttackData CreateOrLoadSmashAttack()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AttackData>(SMASH_ATTACK_PATH);
            var attack = existing != null ? existing : ScriptableObject.CreateInstance<AttackData>();

            attack.attackId = "tomato_smash";
            attack.attackName = "Tomato Smash";
            attack.damageMultiplier = 0.8f;
            attack.knockbackForce = new Vector2(3f, 0.5f);
            attack.launchForce = Vector2.zero;
            attack.hitboxId = "Punch";
            attack.hitboxStartFrame = 2;
            attack.hitboxActiveFrames = 3;
            attack.totalFrames = 6;
            attack.animationSpeed = 1f;
            attack.telegraphType = TelegraphType.Normal;

            if (existing == null)
                AssetDatabase.CreateAsset(attack, SMASH_ATTACK_PATH);
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
