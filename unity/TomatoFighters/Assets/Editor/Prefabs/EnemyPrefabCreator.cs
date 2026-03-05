using TomatoFighters.Combat;
using TomatoFighters.Shared.Components;
using TomatoFighters.World;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.Prefabs
{
    /// <summary>
    /// Generic enemy prefab builder. Accepts an <see cref="EnemyPrefabConfig"/>
    /// and produces a complete prefab with physics, Animator, EnemyBase, hitbox children.
    /// Per-enemy creator scripts populate the config and delegate here.
    /// Parallel to <see cref="PlayerPrefabCreator"/> for players.
    /// </summary>
    public static class EnemyPrefabCreator
    {
        private const string ENEMY_HURTBOX_LAYER = "EnemyHurtbox";
        private const string ENEMY_HITBOX_LAYER = "EnemyHitbox";

        /// <summary>
        /// Creates or updates an enemy prefab from the given config.
        /// </summary>
        public static GameObject CreateEnemyPrefab(EnemyPrefabConfig config)
        {
            PlayerPrefabCreator.EnsureFolderExists(
                System.IO.Path.GetDirectoryName(config.prefabPath).Replace("\\", "/"));

            bool isNew = AssetDatabase.LoadAssetAtPath<GameObject>(config.prefabPath) == null;
            var prefab = SetupPrefab(config);

            string verb = isNew ? "Created" : "Updated";
            Debug.Log($"[EnemyPrefabCreator] {verb} {config.enemyType} prefab at {config.prefabPath}");
            Selection.activeObject = prefab;

            return prefab;
        }

        private static GameObject SetupPrefab(EnemyPrefabConfig config)
        {
            int hurtboxLayer = LayerMask.NameToLayer(ENEMY_HURTBOX_LAYER);
            int hitboxLayer = LayerMask.NameToLayer(ENEMY_HITBOX_LAYER);

            bool isExisting = AssetDatabase.LoadAssetAtPath<GameObject>(config.prefabPath) != null;
            GameObject root;

            if (isExisting)
                root = PrefabUtility.LoadPrefabContents(config.prefabPath);
            else
                root = new GameObject(config.enemyType);

            // Layer
            if (hurtboxLayer >= 0)
                root.layer = hurtboxLayer;

            // Rigidbody2D
            var rb = PlayerPrefabCreator.EnsureComponent<Rigidbody2D>(root);
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Body collider (hurtbox)
            var bodyCol = PlayerPrefabCreator.EnsureComponent<BoxCollider2D>(root);
            bodyCol.size = config.bodySize;
            bodyCol.offset = config.bodyOffset;
            bodyCol.isTrigger = false;

            // Sprite child
            var spriteChild = PlayerPrefabCreator.FindOrCreateChild(root, "Sprite");
            spriteChild.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            var sr = PlayerPrefabCreator.EnsureComponent<SpriteRenderer>(spriteChild);
            if (config.bodySprite != null)
                sr.sprite = config.bodySprite;
            sr.color = config.spriteColor;
            sr.sortingOrder = 1;

            // Animator on Sprite child (DD-4 from T024B)
            var animator = PlayerPrefabCreator.EnsureComponent<Animator>(spriteChild);
            if (config.animatorController != null)
            {
                animator.runtimeAnimatorController = config.animatorController;
                EditorUtility.SetDirty(animator);
            }

            // AnimationEventRelay on Sprite child (bridges animation events to root)
            PlayerPrefabCreator.EnsureComponent<AnimationEventRelay>(spriteChild);

            // BasicMeleeEnemy (concrete EnemyBase subclass — EnemyBase is abstract)
            // RequireComponent auto-adds EnemyAI
            var enemy = PlayerPrefabCreator.EnsureComponent<BasicMeleeEnemy>(root);
            if (config.enemyDataAsset != null)
            {
                // Wire enemyData on EnemyBase (inherited private field)
                var enemySO = new SerializedObject(enemy);
                var dataProp = enemySO.FindProperty("enemyData");
                if (dataProp != null)
                    dataProp.objectReferenceValue = config.enemyDataAsset;
                enemySO.ApplyModifiedPropertiesWithoutUndo();

                // Wire enemyData on the auto-added EnemyAI too
                var ai = root.GetComponent<EnemyAI>();
                if (ai != null)
                {
                    var aiSO = new SerializedObject(ai);
                    var aiDataProp = aiSO.FindProperty("enemyData");
                    if (aiDataProp != null)
                        aiDataProp.objectReferenceValue = config.enemyDataAsset;

                    // Wire playerLayer mask
                    int playerHurtbox = LayerMask.NameToLayer("PlayerHurtbox");
                    if (playerHurtbox >= 0)
                    {
                        var layerProp = aiSO.FindProperty("playerLayer");
                        if (layerProp != null)
                            layerProp.intValue = 1 << playerHurtbox;
                    }
                    aiSO.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // DefenseSystem + ClashTracker
            if (config.defenseConfig != null)
            {
                var defenseSys = PlayerPrefabCreator.EnsureComponent<DefenseSystem>(root);
                var defSO = new SerializedObject(defenseSys);
                defSO.FindProperty("config").objectReferenceValue = config.defenseConfig;
                defSO.ApplyModifiedPropertiesWithoutUndo();

                PlayerPrefabCreator.EnsureComponent<ClashTracker>(root);
            }

            // TelegraphVisualController — wired to sprite child's SpriteRenderer
            var telegraphCtrl = PlayerPrefabCreator.EnsureComponent<TelegraphVisualController>(root);
            var telegraphSO = new SerializedObject(telegraphCtrl);
            var spriteProp = telegraphSO.FindProperty("_sprite");
            if (spriteProp != null)
                spriteProp.objectReferenceValue = sr;
            telegraphSO.ApplyModifiedPropertiesWithoutUndo();

            // Hitbox children
            if (config.hitboxDefinitions != null)
            {
                var whiteSquare = TestDummyPrefabCreator.GetOrCreateWhiteSquareSprite();
                foreach (var def in config.hitboxDefinitions)
                {
                    string childName = $"Hitbox_{def.hitboxId}";
                    var hitboxChild = PlayerPrefabCreator.FindOrCreateChild(root, childName);
                    if (hitboxLayer >= 0)
                        hitboxChild.layer = hitboxLayer;

                    var hitboxCol = PlayerPrefabCreator.EnsureComponent<BoxCollider2D>(hitboxChild);
                    hitboxCol.isTrigger = true;
                    hitboxCol.size = def.boxSize;
                    hitboxCol.offset = def.offset;

                    PlayerPrefabCreator.EnsureComponent<HitboxDamage>(hitboxChild);
                    TestDummyPrefabCreator.AddHitboxDebugVisual(hitboxChild, def.boxSize, def.offset, whiteSquare);
                    hitboxChild.SetActive(false);
                }
            }

            // Clean up missing scripts
            PlayerPrefabCreator.RemoveMissingScripts(root);

            // Save prefab
            GameObject savedPrefab;
            if (isExisting)
            {
                savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, config.prefabPath);
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, config.prefabPath);
                Object.DestroyImmediate(root);
            }

            AssetDatabase.ImportAsset(config.prefabPath, ImportAssetOptions.ForceUpdate);
            return savedPrefab;
        }
    }
}
