using TomatoFighters.Characters;
using TomatoFighters.Combat;
using TomatoFighters.Shared.Components;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TomatoFighters.Editor.Prefabs
{
    /// <summary>
    /// Generic player prefab builder. Accepts a <see cref="CharacterPrefabConfig"/>
    /// and produces a complete prefab with physics, components, hitbox children,
    /// and HitboxManager wiring. Character-specific creators (e.g. MysticaCharacterCreator)
    /// populate the config and delegate here.
    /// Safe to re-run — loads existing prefab and updates in place.
    /// </summary>
    public static class PlayerPrefabCreator
    {
        private const string PLAYER_HURTBOX_LAYER = "PlayerHurtbox";
        private const string PLAYER_HITBOX_LAYER = "PlayerHitbox";

        /// <summary>
        /// Creates or updates a player prefab from the given config.
        /// </summary>
        public static GameObject CreatePlayerPrefab(CharacterPrefabConfig config)
        {
            EnsureFolderExists(System.IO.Path.GetDirectoryName(config.prefabPath).Replace("\\", "/"));

            bool isNew = AssetDatabase.LoadAssetAtPath<GameObject>(config.prefabPath) == null;

            var prefab = SetupPrefab(config);

            string verb = isNew ? "Created" : "Updated";
            Debug.Log($"[PlayerPrefabCreator] {verb} prefab at {config.prefabPath}");
            Selection.activeObject = prefab;

            return prefab;
        }

        private static GameObject SetupPrefab(CharacterPrefabConfig config)
        {
            GameObject root;
            bool isExisting = AssetDatabase.LoadAssetAtPath<GameObject>(config.prefabPath) != null;

            if (isExisting)
                root = PrefabUtility.LoadPrefabContents(config.prefabPath);
            else
                root = new GameObject("Player");

            // -- Layer: root is PlayerHurtbox (enemy attacks detect this) --
            int hurtboxLayer = LayerMask.NameToLayer(PLAYER_HURTBOX_LAYER);
            if (hurtboxLayer >= 0)
                root.layer = hurtboxLayer;
            else
                Debug.LogWarning(
                    $"[PlayerPrefabCreator] Layer '{PLAYER_HURTBOX_LAYER}' not found. " +
                    "Add it in Edit > Project Settings > Tags and Layers.");

            // -- Rigidbody2D --
            var rb = EnsureComponent<Rigidbody2D>(root);
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // -- BoxCollider2D (standing character proportions) --
            var col = EnsureComponent<BoxCollider2D>(root);
            col.size = new Vector2(0.8f, 1.2f);
            col.offset = new Vector2(0f, 0.6f);

            // -- Sprite child (preserved if exists) --
            var spriteChild = FindOrCreateChild(root, "Sprite");
            var sr = EnsureComponent<SpriteRenderer>(spriteChild);
            sr.sortingOrder = 1;

            if (sr.sprite == null)
            {
                var idleSprites = AssetDatabase.LoadAllAssetsAtPath(
                    "Assets/animations/tomato_fighter_animations/Sprites/tomato_fighter_idle.png");
                foreach (var asset in idleSprites)
                {
                    if (asset is Sprite sprite && sprite.name.Contains("_0"))
                    {
                        sr.sprite = sprite;
                        break;
                    }
                }
            }

            // -- Animator on Sprite child --
            var animator = EnsureComponent<Animator>(spriteChild);
            if (config.animatorController != null)
            {
                // Direct assignment — SerializedObject can lose the reference
                // when used with PrefabUtility.LoadPrefabContents
                animator.runtimeAnimatorController = config.animatorController;
                EditorUtility.SetDirty(animator);
            }

            // -- Shadow child (preserved if exists) --
            var shadowChild = FindOrCreateChild(root, "Shadow");
            shadowChild.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            shadowChild.transform.localScale = new Vector3(0.9f, 0.2f, 1f);
            var shadowSR = EnsureComponent<SpriteRenderer>(shadowChild);
            shadowSR.color = new Color(0f, 0f, 0f, 0.3f);
            shadowSR.sortingOrder = 0;

            // -- CharacterMotor --
            var motor = EnsureComponent<CharacterMotor>(root);
            var motorSO = new SerializedObject(motor);
            motorSO.FindProperty("config").objectReferenceValue = config.movementConfig;
            motorSO.FindProperty("characterType").enumValueIndex = (int)config.characterType;
            motorSO.FindProperty("spriteTransform").objectReferenceValue = spriteChild.transform;
            motorSO.ApplyModifiedPropertiesWithoutUndo();

            // -- ComboController --
            var comboController = EnsureComponent<ComboController>(root);
            var comboSO = new SerializedObject(comboController);
            comboSO.FindProperty("comboDefinition").objectReferenceValue = config.comboDefinition;
            comboSO.FindProperty("characterType").enumValueIndex = (int)config.characterType;
            comboSO.FindProperty("animator").objectReferenceValue = animator;
            comboSO.FindProperty("motor").objectReferenceValue = motor;
            comboSO.ApplyModifiedPropertiesWithoutUndo();

            // -- ComboDebugUI --
            EnsureComponent<ComboDebugUI>(root);

            // -- CharacterAnimationBridge --
            var animBridge = EnsureComponent<CharacterAnimationBridge>(root);
            var bridgeSO = new SerializedObject(animBridge);
            bridgeSO.FindProperty("animator").objectReferenceValue = animator;
            bridgeSO.FindProperty("motor").objectReferenceValue = motor;
            bridgeSO.ApplyModifiedPropertiesWithoutUndo();

            // -- CharacterInputHandler --
            var inputHandler = EnsureComponent<CharacterInputHandler>(root);
            var inputSO = new SerializedObject(inputHandler);
            inputSO.FindProperty("motor").objectReferenceValue = motor;
            inputSO.FindProperty("comboController").objectReferenceValue = comboController;

            if (config.inputActions != null)
            {
                WireInputAction(config.inputActions, inputSO, "moveAction", "Player/Move");
                WireInputAction(config.inputActions, inputSO, "jumpAction", "Player/Jump");
                WireInputAction(config.inputActions, inputSO, "dashAction", "Player/Sprint");
                WireInputAction(config.inputActions, inputSO, "lightAttackAction", "Player/Attack");
                WireInputAction(config.inputActions, inputSO, "heavyAttackAction", "Player/Crouch");

                var runAction = config.inputActions.FindAction("Player/Run");
                if (runAction != null)
                    WireInputAction(config.inputActions, inputSO, "runAction", "Player/Run");
                else
                    Debug.LogWarning("[PlayerPrefabCreator] 'Player/Run' action not found. Add a 'Run' action bound to Left Ctrl.");
            }
            else
            {
                Debug.LogWarning("[PlayerPrefabCreator] InputSystem_Actions not found. Input actions not wired.");
            }

            inputSO.ApplyModifiedPropertiesWithoutUndo();

            // -- DefenseSystem --
            var defenseSystem = EnsureComponent<DefenseSystem>(root);
            var defSO = new SerializedObject(defenseSystem);
            defSO.FindProperty("motor").objectReferenceValue = motor;
            defSO.FindProperty("comboController").objectReferenceValue = comboController;
            if (config.defenseConfig != null)
                defSO.FindProperty("config").objectReferenceValue = config.defenseConfig;
            defSO.ApplyModifiedPropertiesWithoutUndo();

            // -- PlayerDamageable (stub for bidirectional damage) --
            var playerDamageable = EnsureComponent<PlayerDamageable>(root);
            var pdSO = new SerializedObject(playerDamageable);
            pdSO.FindProperty("defenseSystem").objectReferenceValue = defenseSystem;
            pdSO.ApplyModifiedPropertiesWithoutUndo();

            // -- HitboxManager.ownerDefenseSystem --
            // (Deferred to after HitboxManager is created below)

            // -- DebugHealthBar (temp HP bar, replaced by T025 HUD) --
            var healthBar = EnsureComponent<DebugHealthBar>(root);
            var hbSO = new SerializedObject(healthBar);
            var fillColorProp = hbSO.FindProperty("fillColor");
            if (fillColorProp != null)
                fillColorProp.colorValue = new Color(0.2f, 0.8f, 0.3f); // Green for player
            hbSO.ApplyModifiedPropertiesWithoutUndo();

            // -- Hitbox children from config --
            CreateHitboxChildren(root, config);

            // -- ClashTracker (per-activation clash immunity) --
            var clashTracker = EnsureComponent<ClashTracker>(root);

            // -- HitboxManager --
            var hitboxManager = EnsureComponent<HitboxManager>(root);
            var hmSO = new SerializedObject(hitboxManager);
            hmSO.FindProperty("comboController").objectReferenceValue = comboController;
            hmSO.FindProperty("ownerDefenseSystem").objectReferenceValue = defenseSystem;
            hmSO.FindProperty("ownerClashTracker").objectReferenceValue = clashTracker;
            hmSO.FindProperty("baseAttack").floatValue = config.baseAttack;
            hmSO.FindProperty("useTimerFallback").boolValue = config.useTimerFallback;
            hmSO.FindProperty("fallbackActiveDuration").floatValue = config.fallbackActiveDuration;
            hmSO.ApplyModifiedPropertiesWithoutUndo();

            // -- Clean up missing scripts (e.g. old HitboxDamage refs after move to Shared) --
            RemoveMissingScripts(root);

            // -- Re-add HitboxDamage from Shared on any Hitbox_* children that lost it --
            RepairHitboxChildren(root);

            // -- Save --
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

            // Force reimport to ensure Library cache matches the .prefab on disk
            AssetDatabase.ImportAsset(config.prefabPath, ImportAssetOptions.ForceUpdate);

            return savedPrefab;
        }

        // ── Hitbox Child Creation ─────────────────────────────────────────

        private static void CreateHitboxChildren(GameObject root, CharacterPrefabConfig config)
        {
            if (config.hitboxes == null) return;

            int hitboxLayer = LayerMask.NameToLayer(PLAYER_HITBOX_LAYER);
            var whiteSquare = TestDummyPrefabCreator.GetOrCreateWhiteSquareSprite();

            foreach (var def in config.hitboxes)
            {
                string childName = $"Hitbox_{def.hitboxId}";
                var child = FindOrCreateChild(root, childName);

                if (hitboxLayer >= 0)
                    child.layer = hitboxLayer;

                // Collider setup based on shape
                if (def.shape == HitboxShape.Circle)
                {
                    var circle = EnsureComponent<CircleCollider2D>(child);
                    circle.isTrigger = true;
                    circle.radius = def.circleRadius;
                    circle.offset = def.offset;

                    float diameter = def.circleRadius * 2f;
                    TestDummyPrefabCreator.AddHitboxDebugVisual(
                        child, new Vector2(diameter, diameter), def.offset, whiteSquare);
                }
                else // Box
                {
                    var box = EnsureComponent<BoxCollider2D>(child);
                    box.isTrigger = true;
                    box.size = def.boxSize;
                    box.offset = def.offset;

                    TestDummyPrefabCreator.AddHitboxDebugVisual(
                        child, def.boxSize, def.offset, whiteSquare);
                }

                EnsureComponent<HitboxDamage>(child);
                child.SetActive(false);
            }
        }

        // ── Public Helpers ────────────────────────────────────────────────

        /// <summary>
        /// Returns the existing component on the GameObject, or adds one if missing.
        /// </summary>
        public static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        /// <summary>
        /// Finds a direct child by name, or creates a new empty child if not found.
        /// Preserves existing children and their sub-hierarchy.
        /// </summary>
        public static GameObject FindOrCreateChild(GameObject parent, string childName)
        {
            var t = parent.transform.Find(childName);
            if (t != null)
                return t.gameObject;

            var child = new GameObject(childName);
            child.transform.SetParent(parent.transform);
            child.transform.localPosition = Vector3.zero;
            return child;
        }

        public static void WireInputAction(InputActionAsset asset, SerializedObject so,
            string propertyName, string actionPath)
        {
            var action = asset.FindAction(actionPath);
            if (action == null)
            {
                Debug.LogWarning($"[PlayerPrefabCreator] Input action '{actionPath}' not found.");
                return;
            }
            so.FindProperty(propertyName).objectReferenceValue = InputActionReference.Create(action);
        }

        /// <summary>
        /// Ensures all Hitbox_* children have the <see cref="HitboxDamage"/> component.
        /// Repairs hitbox children that lost their script reference after the move to Shared.
        /// </summary>
        public static void RepairHitboxChildren(GameObject root)
        {
            int hitboxLayer = LayerMask.NameToLayer(PLAYER_HITBOX_LAYER);

            foreach (Transform child in root.transform)
            {
                if (!child.name.StartsWith("Hitbox_")) continue;

                if (child.GetComponent<HitboxDamage>() == null)
                {
                    child.gameObject.AddComponent<HitboxDamage>();
                    Debug.Log($"[PlayerPrefabCreator] Re-added HitboxDamage to '{child.name}'.");
                }

                if (hitboxLayer >= 0 && child.gameObject.layer != hitboxLayer)
                {
                    child.gameObject.layer = hitboxLayer;
                    Debug.Log($"[PlayerPrefabCreator] Set '{child.name}' layer to {PLAYER_HITBOX_LAYER}.");
                }
            }
        }

        /// <summary>
        /// Removes all MonoBehaviours with missing scripts from a GameObject and its children.
        /// Prevents "Prefab with a missing script" save errors.
        /// </summary>
        public static void RemoveMissingScripts(GameObject root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                if (removed > 0)
                    Debug.Log($"[PlayerPrefabCreator] Removed {removed} missing script(s) from '{t.name}'.");
            }
        }

        public static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parts = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
