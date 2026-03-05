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
    /// Creates an Eggplant Wizard enemy prefab with Animator, EnemyData, EnemyAI, and attack.
    /// Run via menu: <b>TomatoFighters > Create EggplantWizard Enemy Prefab</b>.
    /// </summary>
    public static class EggplantWizardEnemyCreator
    {
        private const string PREFAB_PATH = "Assets/Prefabs/Enemies/EggplantWizard.prefab";
        private const string SO_FOLDER = "Assets/ScriptableObjects/Enemies";
        private const string ENEMY_DATA_PATH = SO_FOLDER + "/EggplantWizard_EnemyData.asset";
        private const string ATTACK_FOLDER = "Assets/ScriptableObjects/Attacks/Enemy/EggplantWizard";
        private const string SPELL_ATTACK_PATH = ATTACK_FOLDER + "/EggplantWizardSpell.asset";
        private const string OVERRIDE_PATH = "Assets/Animations/Enemies/EggplantWizard/EggplantWizard_Override.overrideController";

        [MenuItem("TomatoFighters/Create EggplantWizard Enemy Prefab")]
        public static void Create()
        {
            PlayerPrefabCreator.EnsureFolderExists(SO_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(ATTACK_FOLDER);

            var enemyData = CreateOrLoadEnemyData();
            var spellAttack = CreateOrLoadSpellAttack();

            // Wire attack into EnemyData
            var dataSO = new SerializedObject(enemyData);
            var attacksProp = dataSO.FindProperty("attacks");
            attacksProp.arraySize = 1;
            attacksProp.GetArrayElementAtIndex(0).objectReferenceValue = spellAttack;
            dataSO.ApplyModifiedPropertiesWithoutUndo();

            // Load override controller
            var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(OVERRIDE_PATH);
            if (overrideController == null)
                Debug.LogWarning($"[EggplantWizardEnemyCreator] Override controller not found at {OVERRIDE_PATH}. " +
                    "Run 'TomatoFighters > Build Animations > All Characters' first.");

            // Load first idle sprite for body visual
            Sprite bodySprite = null;
            var idleSprites = AssetDatabase.LoadAllAssetsAtPath(
                "Assets/animations/eggplant_wizard_animations/Sprites/eggplant_wizard_idle.png");
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
                enemyType = "EggplantWizard",
                enemyDataAsset = enemyData,
                animatorController = overrideController,
                bodySprite = bodySprite,
                spriteColor = Color.white,
                bodySize = new Vector2(0.7f, 1.3f),
                bodyOffset = new Vector2(0f, 0.1f),
                hitboxDefinitions = new[]
                {
                    new HitboxDefinition
                    {
                        hitboxId = "Punch",
                        shape = HitboxShape.Box,
                        boxSize = new Vector2(1.0f, 0.6f),
                        offset = new Vector2(-0.7f, 0.2f),
                    }
                },
            };

            EnemyPrefabCreator.CreateEnemyPrefab(config);
            WireEnemyAI(enemyData);
        }

        private static EnemyData CreateOrLoadEnemyData()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemyData>(ENEMY_DATA_PATH);
            var data = existing != null ? existing : ScriptableObject.CreateInstance<EnemyData>();

            data.maxHealth = 50f;
            data.pressureThreshold = 25f;
            data.stunDuration = 2.0f;
            data.invulnerabilityDuration = 0.5f;
            data.knockbackResistance = 0.05f;
            data.movementSpeed = 3.0f;

            data.aggroRange = 10f;
            data.attackRange = 2.0f;
            data.patrolRadius = 3f;
            data.leashRange = 14f;
            data.idleDuration = 1.2f;
            data.attackCooldown = 1.5f;
            data.aggression = 0.4f;
            data.hitReactDuration = 0.3f;
            data.telegraphDuration = 0.4f;

            if (existing == null)
                AssetDatabase.CreateAsset(data, ENEMY_DATA_PATH);
            else
                EditorUtility.SetDirty(data);

            AssetDatabase.SaveAssets();
            return data;
        }

        private static AttackData CreateOrLoadSpellAttack()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AttackData>(SPELL_ATTACK_PATH);
            var attack = existing != null ? existing : ScriptableObject.CreateInstance<AttackData>();

            attack.attackId = "eggplant_spell";
            attack.attackName = "Eggplant Spell";
            attack.damageMultiplier = 0.7f;
            attack.knockbackForce = new Vector2(2.5f, 1f);
            attack.launchForce = Vector2.zero;
            attack.hitboxId = "Punch";
            attack.hitboxStartFrame = 2;
            attack.hitboxActiveFrames = 3;
            attack.totalFrames = 6;
            attack.animationSpeed = 1f;
            attack.telegraphType = TelegraphType.Normal;

            if (existing == null)
                AssetDatabase.CreateAsset(attack, SPELL_ATTACK_PATH);
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
