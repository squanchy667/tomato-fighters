using TomatoFighters.Characters;
using TomatoFighters.Combat;
using TomatoFighters.Shared.Components;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TomatoFighters.Editor.Prefabs
{
    /// <summary>
    /// Creates a test scene for verifying movement, combo system, animations, and combat.
    /// Run via menu: <b>TomatoFighters &gt; Create Movement Test Scene</b>.
    ///
    /// <para><b>Scene contents:</b></para>
    /// <list type="bullet">
    ///   <item>Orthographic camera (size 7, dark background)</item>
    ///   <item>20x10 arena with 4 invisible wall colliders</item>
    ///   <item>Dark green ground plane with grid lines for depth perception</item>
    ///   <item>Player — from prefab, with <see cref="PlayerDamageable"/> added for bidirectional damage</item>
    ///   <item>TestDummy enemy — from prefab, positioned right of center for combat testing</item>
    ///   <item>Controls hint text at top of arena</item>
    /// </list>
    ///
    /// <para><b>Input wiring:</b> InputActionReferences don't survive prefab serialization,
    /// so this script re-wires them on the scene instance after instantiation:
    /// Move=WASD, Jump=Space, Dash=L-Shift, Light=LMB, Heavy=C, Run=L-Ctrl.</para>
    ///
    /// <para><b>Fallback:</b> If the prefab doesn't exist, builds the player inline
    /// (without Animator or CharacterAnimationBridge). Run <c>Create Player Prefab</c> first.</para>
    /// </summary>
    public static class MovementTestSceneCreator
    {
        private const string SCENE_FOLDER = "Assets/Scenes";
        private const string SCENE_PATH = SCENE_FOLDER + "/MovementTest.unity";
        private const string PREFAB_PATH = "Assets/Prefabs/Player/Player.prefab";
        private const string DUMMY_PREFAB_PATH = "Assets/Prefabs/Enemies/TestDummy.prefab";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";

        private const float ARENA_WIDTH = 20f;
        private const float ARENA_HEIGHT = 10f;
        private const float WALL_THICKNESS = 1f;

        [MenuItem("TomatoFighters/Create Movement Test Scene")]
        public static void CreateTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLayerCollisionMatrix();
            SetupCamera();
            CreateArenaBackground();
            CreateArenaWalls();
            CreatePlayerFromPrefab();
            CreateTestDummyFromPrefab();
            CreateDebugCanvas();

            PlayerPrefabCreator.EnsureFolderExists(SCENE_FOLDER);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);

            Debug.Log($"[MovementTestScene] Scene created at {SCENE_PATH}");
            Debug.Log("[MovementTestScene] Controls: WASD = move, Space = jump, L-Shift = dash, LMB = light, C = heavy, L-Ctrl = run");
        }

        private static void SetupLayerCollisionMatrix()
        {
            int playerHitbox = LayerMask.NameToLayer("PlayerHitbox");
            int playerHurtbox = LayerMask.NameToLayer("PlayerHurtbox");
            int enemyHitbox = LayerMask.NameToLayer("EnemyHitbox");
            int enemyHurtbox = LayerMask.NameToLayer("EnemyHurtbox");

            if (playerHitbox < 0 || playerHurtbox < 0 || enemyHitbox < 0 || enemyHurtbox < 0)
            {
                Debug.LogError(
                    "[MovementTestScene] Missing physics layers. " +
                    "Add PlayerHitbox, PlayerHurtbox, EnemyHitbox, EnemyHurtbox in Tags and Layers.");
                return;
            }

            // Enable cross-team collisions
            Physics2D.IgnoreLayerCollision(playerHitbox, enemyHurtbox, false);
            Physics2D.IgnoreLayerCollision(enemyHitbox, playerHurtbox, false);

            // Disable same-team and self collisions
            Physics2D.IgnoreLayerCollision(playerHitbox, playerHurtbox, true);
            Physics2D.IgnoreLayerCollision(enemyHitbox, enemyHurtbox, true);
            Physics2D.IgnoreLayerCollision(playerHitbox, playerHitbox, true);
            Physics2D.IgnoreLayerCollision(enemyHitbox, enemyHitbox, true);
            Physics2D.IgnoreLayerCollision(playerHurtbox, playerHurtbox, true);
            Physics2D.IgnoreLayerCollision(enemyHurtbox, enemyHurtbox, true);

            Debug.Log("[MovementTestScene] Layer collision matrix configured.");
        }

        private static void SetupCamera()
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGO.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateArenaBackground()
        {
            var ground = new GameObject("Ground");
            var sr = ground.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRectSprite();
            sr.color = new Color(0.25f, 0.3f, 0.2f);
            ground.transform.localScale = new Vector3(ARENA_WIDTH, ARENA_HEIGHT, 1f);
            ground.transform.position = Vector3.zero;
            sr.sortingOrder = -10;

            for (int i = -4; i <= 4; i++)
            {
                var line = new GameObject($"GridLine_H_{i}");
                var lineSR = line.AddComponent<SpriteRenderer>();
                lineSR.sprite = CreateRectSprite();
                lineSR.color = new Color(1f, 1f, 1f, 0.05f);
                line.transform.localScale = new Vector3(ARENA_WIDTH, 0.02f, 1f);
                line.transform.position = new Vector3(0f, i * 1.2f, 0f);
                lineSR.sortingOrder = -9;
                line.transform.SetParent(ground.transform);
            }
        }

        private static void CreateArenaWalls()
        {
            var walls = new GameObject("Walls");

            CreateWall("Wall_Left", walls.transform,
                new Vector3(-ARENA_WIDTH / 2f - WALL_THICKNESS / 2f, 0f, 0f),
                new Vector2(WALL_THICKNESS, ARENA_HEIGHT + WALL_THICKNESS * 2));

            CreateWall("Wall_Right", walls.transform,
                new Vector3(ARENA_WIDTH / 2f + WALL_THICKNESS / 2f, 0f, 0f),
                new Vector2(WALL_THICKNESS, ARENA_HEIGHT + WALL_THICKNESS * 2));

            CreateWall("Wall_Top", walls.transform,
                new Vector3(0f, ARENA_HEIGHT / 2f + WALL_THICKNESS / 2f, 0f),
                new Vector2(ARENA_WIDTH + WALL_THICKNESS * 2, WALL_THICKNESS));

            CreateWall("Wall_Bottom", walls.transform,
                new Vector3(0f, -ARENA_HEIGHT / 2f - WALL_THICKNESS / 2f, 0f),
                new Vector2(ARENA_WIDTH + WALL_THICKNESS * 2, WALL_THICKNESS));
        }

        private static void CreateWall(string name, Transform parent, Vector3 position, Vector2 size)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent);
            wall.transform.position = position;
            var col = wall.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        private static void CreatePlayerFromPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogWarning("[MovementTestScene] Player prefab not found. Run 'Create Player Prefab' first. Building inline fallback.");
                BuildInlineFallbackPlayer();
                return;
            }

            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.transform.position = new Vector3(-3f, 0f, 0f);

            // InputActionReferences created via InputActionReference.Create() don't survive
            // prefab serialization — wire them on the scene instance directly.
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);
            var inputHandler = player.GetComponent<CharacterInputHandler>();
            if (inputActions != null && inputHandler != null)
            {
                var inputSO = new SerializedObject(inputHandler);
                WireAction(inputActions, inputSO, "moveAction", "Player/Move");
                WireAction(inputActions, inputSO, "jumpAction", "Player/Jump");
                WireAction(inputActions, inputSO, "dashAction", "Player/Sprint");
                WireAction(inputActions, inputSO, "lightAttackAction", "Player/Attack");
                WireAction(inputActions, inputSO, "heavyAttackAction", "Player/Crouch");
                WireAction(inputActions, inputSO, "runAction", "Player/Run");
                inputSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // Add PlayerDamageable so enemy attacks can hit the player
            if (player.GetComponent<PlayerDamageable>() == null)
                player.AddComponent<PlayerDamageable>();

            // Temp debug HP bar (replaced by T025 HUD)
            if (player.GetComponent<DebugHealthBar>() == null)
            {
                var hpBar = player.AddComponent<DebugHealthBar>();
                var hbSO = new SerializedObject(hpBar);
                var fillColorProp = hbSO.FindProperty("fillColor");
                if (fillColorProp != null)
                    fillColorProp.colorValue = new Color(0.2f, 0.8f, 0.3f); // Green for player
                hbSO.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log("[MovementTestScene] Player instantiated with input actions + PlayerDamageable + DebugHealthBar.");
        }

        private static void CreateTestDummyFromPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DUMMY_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogWarning(
                    "[MovementTestScene] TestDummy prefab not found. " +
                    "Run 'TomatoFighters > Create TestDummy Prefab' first. Skipping enemy placement.");
                return;
            }

            var dummy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            dummy.transform.position = new Vector3(3f, 0f, 0f);

            // Ensure debug HP bar on scene instance (prefab may already have it)
            if (dummy.GetComponent<DebugHealthBar>() == null)
                dummy.AddComponent<DebugHealthBar>();

            Debug.Log("[MovementTestScene] TestDummy enemy placed at (3, 0) with DebugHealthBar.");
        }

        private static void BuildInlineFallbackPlayer()
        {
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);

            var root = new GameObject("Player");
            root.transform.position = new Vector3(-3f, 0f, 0f);

            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.6f);
            col.offset = Vector2.zero;

            var spriteChild = new GameObject("Sprite");
            spriteChild.transform.SetParent(root.transform);
            spriteChild.transform.localPosition = Vector3.zero;
            var sr = spriteChild.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRectSprite();
            sr.color = new Color(0.85f, 0.2f, 0.2f);
            spriteChild.transform.localScale = new Vector3(0.8f, 1.2f, 1f);
            sr.sortingOrder = 1;

            var shadowChild = new GameObject("Shadow");
            shadowChild.transform.SetParent(root.transform);
            shadowChild.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            var shadowSR = shadowChild.AddComponent<SpriteRenderer>();
            shadowSR.sprite = CreateRectSprite();
            shadowSR.color = new Color(0f, 0f, 0f, 0.3f);
            shadowChild.transform.localScale = new Vector3(0.9f, 0.2f, 1f);
            shadowSR.sortingOrder = 0;

            // Load Mystica config, fall back to any available config
            var config = AssetDatabase.LoadAssetAtPath<MovementConfig>("Assets/ScriptableObjects/MovementConfigs/Mystica_MovementConfig.asset")
                      ?? AssetDatabase.LoadAssetAtPath<MovementConfig>("Assets/ScriptableObjects/MovementConfigs/Brutor_MovementConfig.asset");

            var comboDef = AssetDatabase.LoadAssetAtPath<ComboDefinition>("Assets/ScriptableObjects/ComboDefinitions/Mystica_ComboDefinition.asset")
                        ?? AssetDatabase.LoadAssetAtPath<ComboDefinition>("Assets/ScriptableObjects/ComboDefinitions/Brutor_ComboDefinition.asset");

            var motor = root.AddComponent<CharacterMotor>();
            var motorSO = new SerializedObject(motor);
            motorSO.FindProperty("config").objectReferenceValue = config;
            motorSO.FindProperty("characterType").enumValueIndex = (int)CharacterType.Mystica;
            motorSO.FindProperty("spriteTransform").objectReferenceValue = spriteChild.transform;
            motorSO.ApplyModifiedPropertiesWithoutUndo();

            var comboController = root.AddComponent<ComboController>();
            var comboSO = new SerializedObject(comboController);
            comboSO.FindProperty("comboDefinition").objectReferenceValue = comboDef;
            comboSO.FindProperty("characterType").enumValueIndex = (int)CharacterType.Mystica;
            comboSO.FindProperty("motor").objectReferenceValue = motor;
            comboSO.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<ComboDebugUI>();

            var inputHandler = root.AddComponent<CharacterInputHandler>();
            var inputSO = new SerializedObject(inputHandler);
            inputSO.FindProperty("motor").objectReferenceValue = motor;
            inputSO.FindProperty("comboController").objectReferenceValue = comboController;

            if (inputActions != null)
            {
                WireAction(inputActions, inputSO, "moveAction", "Player/Move");
                WireAction(inputActions, inputSO, "jumpAction", "Player/Jump");
                WireAction(inputActions, inputSO, "dashAction", "Player/Sprint");
                WireAction(inputActions, inputSO, "lightAttackAction", "Player/Attack");
                WireAction(inputActions, inputSO, "heavyAttackAction", "Player/Crouch");

                if (inputActions.FindAction("Player/Run") != null)
                    WireAction(inputActions, inputSO, "runAction", "Player/Run");
            }

            inputSO.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<PlayerDamageable>();

            // Temp debug HP bar
            var hpBar = root.AddComponent<DebugHealthBar>();
            var hpBarSO = new SerializedObject(hpBar);
            var fcProp = hpBarSO.FindProperty("fillColor");
            if (fcProp != null)
                fcProp.colorValue = new Color(0.2f, 0.8f, 0.3f);
            hpBarSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireAction(InputActionAsset asset, SerializedObject so,
            string propertyName, string actionPath)
        {
            var action = asset.FindAction(actionPath);
            if (action == null) return;
            so.FindProperty(propertyName).objectReferenceValue = InputActionReference.Create(action);
        }

        private static void CreateDebugCanvas()
        {
            var textGO = new GameObject("ControlsHint");
            var tm = textGO.AddComponent<TextMesh>();
            tm.text = "WASD: Move | Space: Jump | L-Shift: Dash | LMB: Light | C: Heavy | L-Ctrl: Run";
            tm.fontSize = 24;
            tm.characterSize = 0.15f;
            tm.anchor = TextAnchor.UpperCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 1f, 1f, 0.5f);
            textGO.transform.position = new Vector3(0f, ARENA_HEIGHT / 2f - 0.5f, 0f);
        }

        private static Sprite CreateRectSprite()
        {
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }
    }
}
