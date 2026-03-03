using TomatoFighters.Characters;
using TomatoFighters.Combat;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TomatoFighters.Editor.Prefabs
{
    /// <summary>
    /// Creates or updates a Player prefab with all gameplay and animation components.
    /// Safe to re-run — loads existing prefab and updates components in place,
    /// preserving any manually added children (attack colliders, VFX anchors, etc.).
    /// Run via menu: <b>TomatoFighters &gt; Create Player Prefab</b>.
    /// </summary>
    public static class PlayerPrefabCreator
    {
        private const string PREFAB_FOLDER = "Assets/Prefabs/Player";
        private const string CONFIG_FOLDER = "Assets/ScriptableObjects/MovementConfigs";
        private const string COMBO_FOLDER = "Assets/ScriptableObjects/ComboDefinitions";
        private const string PREFAB_PATH = PREFAB_FOLDER + "/Player.prefab";
        private const string MYSTICA_CONFIG_PATH = CONFIG_FOLDER + "/Mystica_MovementConfig.asset";
        private const string MYSTICA_COMBO_PATH = COMBO_FOLDER + "/Mystica_ComboDefinition.asset";
        private const string CONTROLLER_PATH = "Assets/Animations/TomatoFighter/TomatoFighter_Controller.controller";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";

        [MenuItem("TomatoFighters/Create Player Prefab")]
        public static void CreatePlayerPrefab()
        {
            EnsureFolderExists(PREFAB_FOLDER);
            EnsureFolderExists(CONFIG_FOLDER);
            EnsureFolderExists(COMBO_FOLDER);

            var config = CreateOrLoadMysticaMovementConfig();
            var comboDef = CreateOrLoadMysticaComboDefinition();
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);

            if (controller == null)
                Debug.LogWarning("[PlayerPrefabCreator] AnimatorController not found. Run 'Build Animations' first.");

            bool isNew = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) == null;

            var prefab = SetupPrefab(config, comboDef, controller, inputActions);

            string verb = isNew ? "Created" : "Updated";
            Debug.Log($"[PlayerPrefabCreator] {verb} Player prefab at {PREFAB_PATH}");
            Selection.activeObject = prefab;
        }

        private static MovementConfig CreateOrLoadMysticaMovementConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MovementConfig>(MYSTICA_CONFIG_PATH);
            if (existing != null)
                return existing;

            var config = ScriptableObject.CreateInstance<MovementConfig>();

            config.moveSpeed = 8f;
            config.depthSpeed = 5f;
            config.groundAcceleration = 65f;
            config.airAcceleration = 35f;
            config.jumpForce = 14f;
            config.jumpGravity = 38f;
            config.coyoteTime = 0.1f;
            config.jumpBufferTime = 0.12f;
            config.dashSpeed = 20f;
            config.dashDuration = 0.14f;
            config.dashCooldown = 0.5f;
            config.dashHasIFrames = true;
            config.runSpeedMultiplier = 1.5f;

            AssetDatabase.CreateAsset(config, MYSTICA_CONFIG_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log("[PlayerPrefabCreator] Created Mystica_MovementConfig.");
            return config;
        }

        private static ComboDefinition CreateOrLoadMysticaComboDefinition()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ComboDefinition>(MYSTICA_COMBO_PATH);
            if (existing != null)
                return existing;

            var def = ScriptableObject.CreateInstance<ComboDefinition>();
            def.defaultComboWindow = 0.5f;
            def.rootLightIndex = 0;
            def.rootHeavyIndex = 3;

            def.steps = new ComboStep[]
            {
                new ComboStep
                {
                    attackType = AttackType.Light,
                    animationTrigger = "Light1",
                    damageMultiplier = 1.0f,
                    nextOnLight = 1, nextOnHeavy = -1,
                    isFinisher = false
                },
                new ComboStep
                {
                    attackType = AttackType.Light,
                    animationTrigger = "Light2",
                    damageMultiplier = 1.1f,
                    nextOnLight = 2, nextOnHeavy = -1,
                    canDashCancelOnHit = true,
                    isFinisher = false
                },
                new ComboStep
                {
                    attackType = AttackType.Light,
                    animationTrigger = "LightFinisher",
                    damageMultiplier = 1.4f,
                    nextOnLight = -1, nextOnHeavy = -1,
                    isFinisher = true
                },
                new ComboStep
                {
                    attackType = AttackType.Heavy,
                    animationTrigger = "Heavy1",
                    damageMultiplier = 1.8f,
                    comboWindowDuration = 0.6f,
                    nextOnLight = -1, nextOnHeavy = 4,
                    canDashCancelOnHit = true, canJumpCancelOnHit = true,
                    isFinisher = false
                },
                new ComboStep
                {
                    attackType = AttackType.Heavy,
                    animationTrigger = "HeavyFinisher",
                    damageMultiplier = 2.5f,
                    nextOnLight = -1, nextOnHeavy = -1,
                    isFinisher = true
                },
            };

            AssetDatabase.CreateAsset(def, MYSTICA_COMBO_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log("[PlayerPrefabCreator] Created Mystica_ComboDefinition (placeholder).");
            return def;
        }

        /// <summary>
        /// Loads the existing prefab for editing, or creates a new root GameObject.
        /// Ensures all required components and children exist, wires references,
        /// then saves back. Preserves any manually added children.
        /// </summary>
        private static GameObject SetupPrefab(MovementConfig config, ComboDefinition comboDef,
            AnimatorController controller, InputActionAsset inputActions)
        {
            GameObject root;
            bool isExisting = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null;

            if (isExisting)
                root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            else
                root = new GameObject("Player");

            // -- Rigidbody2D --
            var rb = EnsureComponent<Rigidbody2D>(root);
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // -- BoxCollider2D --
            var col = EnsureComponent<BoxCollider2D>(root);
            col.size = new Vector2(0.8f, 0.6f);
            col.offset = Vector2.zero;

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
            if (controller != null)
            {
                var animatorSO = new SerializedObject(animator);
                animatorSO.FindProperty("m_Controller").objectReferenceValue = controller;
                animatorSO.ApplyModifiedPropertiesWithoutUndo();
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
            motorSO.FindProperty("config").objectReferenceValue = config;
            motorSO.FindProperty("characterType").enumValueIndex = (int)CharacterType.Mystica;
            motorSO.FindProperty("spriteTransform").objectReferenceValue = spriteChild.transform;
            motorSO.ApplyModifiedPropertiesWithoutUndo();

            // -- ComboController --
            var comboController = EnsureComponent<ComboController>(root);
            var comboSO = new SerializedObject(comboController);
            comboSO.FindProperty("comboDefinition").objectReferenceValue = comboDef;
            comboSO.FindProperty("characterType").enumValueIndex = (int)CharacterType.Mystica;
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

            if (inputActions != null)
            {
                WireInputAction(inputActions, inputSO, "moveAction", "Player/Move");
                WireInputAction(inputActions, inputSO, "jumpAction", "Player/Jump");
                WireInputAction(inputActions, inputSO, "dashAction", "Player/Sprint");
                WireInputAction(inputActions, inputSO, "lightAttackAction", "Player/Attack");
                WireInputAction(inputActions, inputSO, "heavyAttackAction", "Player/Crouch");

                var runAction = inputActions.FindAction("Player/Run");
                if (runAction != null)
                    WireInputAction(inputActions, inputSO, "runAction", "Player/Run");
                else
                    Debug.LogWarning("[PlayerPrefabCreator] 'Player/Run' action not found. Add a 'Run' action bound to Left Ctrl.");
            }
            else
            {
                Debug.LogWarning("[PlayerPrefabCreator] InputSystem_Actions not found. Input actions not wired.");
            }

            inputSO.ApplyModifiedPropertiesWithoutUndo();

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

        /// <summary>
        /// Returns the existing component on the GameObject, or adds one if missing.
        /// </summary>
        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        /// <summary>
        /// Finds a direct child by name, or creates a new empty child if not found.
        /// Preserves existing children and their sub-hierarchy.
        /// </summary>
        private static GameObject FindOrCreateChild(GameObject parent, string childName)
        {
            var t = parent.transform.Find(childName);
            if (t != null)
                return t.gameObject;

            var child = new GameObject(childName);
            child.transform.SetParent(parent.transform);
            child.transform.localPosition = Vector3.zero;
            return child;
        }

        private static void WireInputAction(InputActionAsset asset, SerializedObject so,
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
