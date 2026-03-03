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
    /// Creates a fully-wired Player prefab with all gameplay and animation components.
    /// Run via menu: <b>TomatoFighters &gt; Create Player Prefab</b>.
    ///
    /// <para><b>Prefab hierarchy:</b></para>
    /// <code>
    /// Player (root)
    ///   ├── Rigidbody2D (gravity=0, continuous collision, interpolate)
    ///   ├── BoxCollider2D (0.8×0.6, body collider on ground plane)
    ///   ├── CharacterMotor (wired to Mystica_MovementConfig)
    ///   ├── ComboController (wired to Mystica_ComboDefinition + Animator)
    ///   ├── ComboDebugUI (auto-advances combo windows when no attack anims exist)
    ///   ├── CharacterAnimationBridge (feeds motor state → Animator)
    ///   ├── CharacterInputHandler (Move/Jump/Dash/LightAttack/HeavyAttack/Run)
    ///   ├── Sprite (child)
    ///   │     ├── SpriteRenderer (idle frame as default sprite)
    ///   │     └── Animator (TomatoFighter_Controller)
    ///   └── Shadow (child)
    ///         └── SpriteRenderer (black 30% alpha, sorting order -1)
    /// </code>
    ///
    /// <para><b>Creates on first run:</b> Mystica_MovementConfig (SPD=1.0 mage tuning)
    /// and Mystica_ComboDefinition (placeholder L-L-L + H-H chain).</para>
    ///
    /// <para><b>Input action note:</b> InputActionReferences created via
    /// <c>InputActionReference.Create()</c> don't survive prefab serialization.
    /// The <see cref="MovementTestSceneCreator"/> re-wires them on scene instances.</para>
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
                Debug.LogWarning("[PlayerPrefabCreator] AnimatorController not found. Run 'Build Animations' first. Prefab will be created without Animator.");

            var prefab = BuildPrefab(config, comboDef, controller, inputActions);

            Debug.Log($"[PlayerPrefabCreator] Player prefab created at {PREFAB_PATH}");
            Selection.activeObject = prefab;
        }

        private static MovementConfig CreateOrLoadMysticaMovementConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MovementConfig>(MYSTICA_CONFIG_PATH);
            if (existing != null)
            {
                Debug.Log("[PlayerPrefabCreator] Using existing Mystica_MovementConfig.");
                return existing;
            }

            var config = ScriptableObject.CreateInstance<MovementConfig>();

            // Mystica: SPD 1.0 (vs Brutor 0.7), agile mage archetype
            config.moveSpeed = 8f;              // base speed * 1.0 SPD
            config.depthSpeed = 5f;
            config.groundAcceleration = 65f;    // slightly snappier than Brutor
            config.airAcceleration = 35f;
            config.jumpForce = 14f;             // lighter = higher jump
            config.jumpGravity = 38f;
            config.coyoteTime = 0.1f;
            config.jumpBufferTime = 0.12f;
            config.dashSpeed = 20f;             // faster dash for agile mage
            config.dashDuration = 0.14f;        // shorter, snappier dash
            config.dashCooldown = 0.5f;         // lower cooldown
            config.dashHasIFrames = true;
            config.runSpeedMultiplier = 1.5f;   // mage runs 50% faster than walk

            AssetDatabase.CreateAsset(config, MYSTICA_CONFIG_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log("[PlayerPrefabCreator] Created Mystica_MovementConfig.");
            return config;
        }

        private static ComboDefinition CreateOrLoadMysticaComboDefinition()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ComboDefinition>(MYSTICA_COMBO_PATH);
            if (existing != null)
            {
                Debug.Log("[PlayerPrefabCreator] Using existing Mystica_ComboDefinition.");
                return existing;
            }

            var def = ScriptableObject.CreateInstance<ComboDefinition>();
            def.defaultComboWindow = 0.5f;
            def.rootLightIndex = 0;
            def.rootHeavyIndex = 3;

            // Placeholder Mystica combo tree — light spell chain + heavy arcane blast
            def.steps = new ComboStep[]
            {
                // [0] Light 1 — Arcane Bolt
                new ComboStep
                {
                    attackType = AttackType.Light,
                    animationTrigger = "Light1",
                    damageMultiplier = 1.0f,
                    comboWindowDuration = 0f,
                    nextOnLight = 1,
                    nextOnHeavy = -1,
                    canDashCancelOnHit = false,
                    canJumpCancelOnHit = false,
                    isFinisher = false
                },
                // [1] Light 2 — Arcane Wave
                new ComboStep
                {
                    attackType = AttackType.Light,
                    animationTrigger = "Light2",
                    damageMultiplier = 1.1f,
                    comboWindowDuration = 0f,
                    nextOnLight = 2,
                    nextOnHeavy = -1,
                    canDashCancelOnHit = true,
                    canJumpCancelOnHit = false,
                    isFinisher = false
                },
                // [2] Light 3 finisher — Arcane Burst
                new ComboStep
                {
                    attackType = AttackType.Light,
                    animationTrigger = "LightFinisher",
                    damageMultiplier = 1.4f,
                    comboWindowDuration = 0f,
                    nextOnLight = -1,
                    nextOnHeavy = -1,
                    canDashCancelOnHit = false,
                    canJumpCancelOnHit = false,
                    isFinisher = true
                },
                // [3] Heavy 1 — Arcane Blast (root heavy)
                new ComboStep
                {
                    attackType = AttackType.Heavy,
                    animationTrigger = "Heavy1",
                    damageMultiplier = 1.8f,
                    comboWindowDuration = 0.6f,
                    nextOnLight = -1,
                    nextOnHeavy = 4,
                    canDashCancelOnHit = true,
                    canJumpCancelOnHit = true,
                    isFinisher = false
                },
                // [4] Heavy 2 finisher — Arcane Nova
                new ComboStep
                {
                    attackType = AttackType.Heavy,
                    animationTrigger = "HeavyFinisher",
                    damageMultiplier = 2.5f,
                    comboWindowDuration = 0f,
                    nextOnLight = -1,
                    nextOnHeavy = -1,
                    canDashCancelOnHit = false,
                    canJumpCancelOnHit = false,
                    isFinisher = true
                },
            };

            AssetDatabase.CreateAsset(def, MYSTICA_COMBO_PATH);
            AssetDatabase.SaveAssets();

            Debug.Log("[PlayerPrefabCreator] Created Mystica_ComboDefinition (placeholder).");
            return def;
        }

        private static GameObject BuildPrefab(MovementConfig config, ComboDefinition comboDef,
            AnimatorController controller, InputActionAsset inputActions)
        {
            // Check for existing prefab
            var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (existingPrefab != null)
            {
                if (!EditorUtility.DisplayDialog(
                    "Overwrite Prefab?",
                    $"A Player prefab already exists at {PREFAB_PATH}. Overwrite it?",
                    "Overwrite", "Cancel"))
                {
                    return existingPrefab;
                }
            }

            // -- Root GameObject --
            var root = new GameObject("Player");

            // -- Rigidbody2D (no gravity — belt-scroll) --
            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // -- BoxCollider2D (body, stays on ground plane) --
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.6f);
            col.offset = Vector2.zero;

            // -- Sprite child (offset by jumpHeight at runtime) --
            var spriteChild = new GameObject("Sprite");
            spriteChild.transform.SetParent(root.transform);
            spriteChild.transform.localPosition = Vector3.zero;
            var sr = spriteChild.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;

            // Try to assign the first idle sprite as default
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

            // -- Animator on Sprite child (drives SpriteRenderer) --
            var animator = spriteChild.AddComponent<Animator>();
            if (controller != null)
            {
                var animatorSO = new SerializedObject(animator);
                animatorSO.FindProperty("m_Controller").objectReferenceValue = controller;
                animatorSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // -- Shadow child (stays at feet) --
            var shadowChild = new GameObject("Shadow");
            shadowChild.transform.SetParent(root.transform);
            shadowChild.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            var shadowSR = shadowChild.AddComponent<SpriteRenderer>();
            shadowSR.color = new Color(0f, 0f, 0f, 0.3f);
            shadowChild.transform.localScale = new Vector3(0.9f, 0.2f, 1f);
            shadowSR.sortingOrder = 0;

            // -- CharacterMotor --
            var motor = root.AddComponent<CharacterMotor>();
            var motorSO = new SerializedObject(motor);
            motorSO.FindProperty("config").objectReferenceValue = config;
            motorSO.FindProperty("characterType").enumValueIndex = (int)CharacterType.Mystica;
            motorSO.FindProperty("spriteTransform").objectReferenceValue = spriteChild.transform;
            motorSO.ApplyModifiedPropertiesWithoutUndo();

            // -- ComboController --
            var comboController = root.AddComponent<ComboController>();
            var comboSO = new SerializedObject(comboController);
            comboSO.FindProperty("comboDefinition").objectReferenceValue = comboDef;
            comboSO.FindProperty("characterType").enumValueIndex = (int)CharacterType.Mystica;
            comboSO.FindProperty("animator").objectReferenceValue = animator;
            comboSO.FindProperty("motor").objectReferenceValue = motor;
            comboSO.ApplyModifiedPropertiesWithoutUndo();

            // -- ComboDebugUI --
            root.AddComponent<ComboDebugUI>();

            // -- CharacterAnimationBridge --
            var animBridge = root.AddComponent<CharacterAnimationBridge>();
            var bridgeSO = new SerializedObject(animBridge);
            bridgeSO.FindProperty("animator").objectReferenceValue = animator;
            bridgeSO.FindProperty("motor").objectReferenceValue = motor;
            bridgeSO.ApplyModifiedPropertiesWithoutUndo();

            // -- CharacterInputHandler --
            var inputHandler = root.AddComponent<CharacterInputHandler>();
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

                // Run action — may not exist yet in the input actions asset
                var runAction = inputActions.FindAction("Player/Run");
                if (runAction != null)
                    WireInputAction(inputActions, inputSO, "runAction", "Player/Run");
                else
                    Debug.LogWarning("[PlayerPrefabCreator] 'Player/Run' action not found in InputActions. Add a 'Run' action bound to Left Ctrl in the Player action map.");
            }
            else
            {
                Debug.LogWarning("[PlayerPrefabCreator] InputSystem_Actions not found. Input actions not wired.");
            }

            inputSO.ApplyModifiedPropertiesWithoutUndo();

            // Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);

            return prefab;
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
