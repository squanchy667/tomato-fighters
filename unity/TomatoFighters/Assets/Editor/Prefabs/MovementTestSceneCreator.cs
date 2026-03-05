using TomatoFighters.Characters;
using TomatoFighters.Combat;
using TomatoFighters.Shared.Components;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Events;
using TomatoFighters.World;
using TomatoFighters.World.UI;
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
    ///   <item>Player — from prefab, with <see cref="PlayerDamageable"/> + <see cref="PlayerManaTracker"/> added</item>
    ///   <item>5 BasicMeleeEnemy (AI) — with state machine, spread across the arena</item>
    ///   <item>PlayerHUD — screen-space overlay (health, mana, combo, path indicator)</item>
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
        private const string PREFAB_PATH = "Assets/Prefabs/Player/Mystica.prefab";
        private const string BASIC_MELEE_PREFAB_PATH = "Assets/Prefabs/Enemies/BasicMeleeEnemy.prefab";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";
        private const string HUD_PREFAB_PATH = "Assets/Prefabs/UI/PlayerHUD.prefab";
        private const string ENEMY_HEALTH_BAR_PREFAB_PATH = "Assets/Prefabs/UI/EnemyHealthBar.prefab";

        private const float ARENA_WIDTH = 20f;
        private const float ARENA_HEIGHT = 10f;
        private const float WALL_THICKNESS = 1f;

        // Walkable floor strip — characters are constrained to this vertical band
        private const float FLOOR_HEIGHT = ARENA_HEIGHT * 0.2f;                  // 2 units (matches floor sprite)
        private const float FLOOR_BOTTOM_Y = -ARENA_HEIGHT / 2f;                 // -5
        private const float FLOOR_TOP_Y = FLOOR_BOTTOM_Y + FLOOR_HEIGHT;         // -3
        private const float FLOOR_MID_Y = (FLOOR_BOTTOM_Y + FLOOR_TOP_Y) / 2f;  // -4

        // SO event channel asset paths (created by WaveManagerAssetsCreator)
        private const string EVENTS_ROOT = "Assets/ScriptableObjects/Events";

        /// <summary>
        /// Creates a movement test scene for a specific character prefab.
        /// Called by per-character scene creators.
        /// </summary>
        public static void CreateTestScene(string prefabPath, string scenePath, CharacterType characterType)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLayerCollisionMatrix();
            var camGO = SetupCamera();
            CreateArenaBackground();
            CreateArenaWalls();
            var player = CreatePlayerFromPrefab(prefabPath, characterType);
            CreateBasicMeleeEnemies();
            CreateWaveManager();
            CreateLevelBounds();
            CreatePlayerHUD();
            CreateDebugCanvas();

            // Wire SO event channels to player components (health, mana, combo → HUD)
            WirePlayerSOEvents(player, characterType);

            // Wire CameraController2D → player follow target
            WireCameraToPlayer(camGO, player);

            PlayerPrefabCreator.EnsureFolderExists(SCENE_FOLDER);
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log($"[MovementTestScene] Scene created at {scenePath} for {characterType}");
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

            // Enable wall (Default layer) collisions with player/enemy bodies
            int defaultLayer = 0;
            Physics2D.IgnoreLayerCollision(defaultLayer, playerHurtbox, false);
            Physics2D.IgnoreLayerCollision(defaultLayer, enemyHurtbox, false);

            // Disable same-team and self collisions
            Physics2D.IgnoreLayerCollision(playerHitbox, playerHurtbox, true);
            Physics2D.IgnoreLayerCollision(enemyHitbox, enemyHurtbox, true);
            Physics2D.IgnoreLayerCollision(playerHitbox, playerHitbox, true);
            Physics2D.IgnoreLayerCollision(enemyHitbox, enemyHitbox, true);
            Physics2D.IgnoreLayerCollision(playerHurtbox, playerHurtbox, true);
            Physics2D.IgnoreLayerCollision(enemyHurtbox, enemyHurtbox, true);

            Debug.Log("[MovementTestScene] Layer collision matrix configured.");
        }

        private static GameObject SetupCamera()
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGO.transform.position = new Vector3(0f, 0f, -10f);

            // CameraController2D — smooth follow with bounds and stun zoom
            var camController = camGO.AddComponent<CameraController2D>();
            var camSO = new SerializedObject(camController);
            camSO.FindProperty("boundsMinX").floatValue = -ARENA_WIDTH / 2f;
            camSO.FindProperty("boundsMaxX").floatValue = ARENA_WIDTH / 2f;
            camSO.FindProperty("boundsMinY").floatValue = FLOOR_BOTTOM_Y;
            camSO.FindProperty("boundsMaxY").floatValue = FLOOR_TOP_Y;
            camSO.FindProperty("defaultOrthoSize").floatValue = 7f;

            // Wire SO event channels for camera lock/unlock and stun zoom
            var onCameraLock = AssetDatabase.LoadAssetAtPath<VoidEventChannel>($"{EVENTS_ROOT}/OnCameraLock.asset");
            var onCameraUnlock = AssetDatabase.LoadAssetAtPath<VoidEventChannel>($"{EVENTS_ROOT}/OnCameraUnlock.asset");
            if (onCameraLock != null)
                camSO.FindProperty("onCameraLock").objectReferenceValue = onCameraLock;
            if (onCameraUnlock != null)
                camSO.FindProperty("onCameraUnlock").objectReferenceValue = onCameraUnlock;

            // Stun zoom events (from PressureSystem T026)
            var onStunTriggered = AssetDatabase.LoadAssetAtPath<VoidEventChannel>($"{EVENTS_ROOT}/OnStunTriggered.asset");
            var onStunRecovered = AssetDatabase.LoadAssetAtPath<VoidEventChannel>($"{EVENTS_ROOT}/OnStunRecovered.asset");
            if (onStunTriggered != null)
                camSO.FindProperty("onStunTriggered").objectReferenceValue = onStunTriggered;
            if (onStunRecovered != null)
                camSO.FindProperty("onStunRecovered").objectReferenceValue = onStunRecovered;

            camSO.ApplyModifiedPropertiesWithoutUndo();

            // Runtime diagnostic — validates damage pipeline on Play
            var diagType = System.Type.GetType("DamagePipelineDiagnostic, Assembly-CSharp");
            if (diagType != null)
                camGO.AddComponent(diagType);
            else
                Debug.LogWarning("[MovementTestScene] DamagePipelineDiagnostic not found — skipping.");

            Debug.Log("[MovementTestScene] Camera with CameraController2D configured.");
            return camGO;
        }

        private const string ART_FOLDER = "Assets/Art/Environment/TestArena";

        private static void CreateArenaBackground()
        {
            var environment = new GameObject("Environment");

            // Background layers — full arena, stacked by sorting order
            CreateArtLayer(environment.transform, $"{ART_FOLDER}/bg_forest_distant.png",
                "BG_Distant", -100, Vector3.zero, ARENA_WIDTH, ARENA_HEIGHT);
            CreateArtLayer(environment.transform, $"{ART_FOLDER}/bg_forest_midground.png",
                "BG_Midground", -90, Vector3.zero, ARENA_WIDTH, ARENA_HEIGHT);
            CreateArtLayer(environment.transform, $"{ART_FOLDER}/bg_forest_foreground.png",
                "BG_Foreground", -80, Vector3.zero, ARENA_WIDTH, ARENA_HEIGHT);

            // Ground floor — anchored at bottom of arena
            var floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART_FOLDER}/ground_forest_floor.png");
            if (floorSprite != null)
            {
                float floorScaleY = ARENA_HEIGHT * 0.2f / floorSprite.bounds.size.y;
                float floorHeight = floorSprite.bounds.size.y * floorScaleY;
                CreateArtLayer(environment.transform, $"{ART_FOLDER}/ground_forest_floor.png",
                    "Ground_Floor", -50,
                    new Vector3(0f, -ARENA_HEIGHT / 2f + floorHeight / 2f, 0f),
                    ARENA_WIDTH, ARENA_HEIGHT * 0.2f);
            }

            // Stone wall visuals — visual only, no colliders (DD-6)
            var wallLeftSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART_FOLDER}/wall_left_stone.png");
            if (wallLeftSprite != null)
            {
                float wallScaleX = ARENA_WIDTH * 0.05f / wallLeftSprite.bounds.size.x;
                float wallWidth = wallLeftSprite.bounds.size.x * wallScaleX;
                CreateArtLayer(environment.transform, $"{ART_FOLDER}/wall_left_stone.png",
                    "Wall_Left_Visual", -40,
                    new Vector3(-ARENA_WIDTH / 2f + wallWidth / 2f, 0f, 0f),
                    ARENA_WIDTH * 0.05f, ARENA_HEIGHT);
            }

            var wallRightSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART_FOLDER}/wall_right_stone.png");
            if (wallRightSprite != null)
            {
                float wallScaleX = ARENA_WIDTH * 0.05f / wallRightSprite.bounds.size.x;
                float wallWidth = wallRightSprite.bounds.size.x * wallScaleX;
                CreateArtLayer(environment.transform, $"{ART_FOLDER}/wall_right_stone.png",
                    "Wall_Right_Visual", -40,
                    new Vector3(ARENA_WIDTH / 2f - wallWidth / 2f, 0f, 0f),
                    ARENA_WIDTH * 0.05f, ARENA_HEIGHT);
            }

            Debug.Log("[MovementTestScene] Art layer background created (6 sprites).");
        }

        /// <summary>
        /// Creates a SpriteRenderer child scaled to fit the target dimensions using sprite.bounds.size (DD-7).
        /// </summary>
        private static void CreateArtLayer(Transform parent, string spritePath, string goName,
            int sortingOrder, Vector3 position, float targetWidth, float targetHeight)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[MovementTestScene] Sprite not found: {spritePath} — using fallback color.");
                var fallback = new GameObject(goName);
                fallback.transform.SetParent(parent);
                fallback.transform.position = position;
                var fallbackSR = fallback.AddComponent<SpriteRenderer>();
                fallbackSR.sprite = CreateRectSprite();
                fallbackSR.color = new Color(0.2f, 0.25f, 0.15f);
                fallback.transform.localScale = new Vector3(targetWidth, targetHeight, 1f);
                fallbackSR.sortingOrder = sortingOrder;
                return;
            }

            var go = new GameObject(goName);
            go.transform.SetParent(parent);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;

            // Scale sprite to fit target dimensions (DD-7)
            float scaleX = targetWidth / sprite.bounds.size.x;
            float scaleY = targetHeight / sprite.bounds.size.y;
            go.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        private static void CreateArenaWalls()
        {
            var walls = new GameObject("Walls");

            // Left/right walls — full arena height (catches knockback/launch above floor)
            CreateWall("Wall_Left", walls.transform,
                new Vector3(-ARENA_WIDTH / 2f - WALL_THICKNESS / 2f, 0f, 0f),
                new Vector2(WALL_THICKNESS, ARENA_HEIGHT + WALL_THICKNESS * 2));

            CreateWall("Wall_Right", walls.transform,
                new Vector3(ARENA_WIDTH / 2f + WALL_THICKNESS / 2f, 0f, 0f),
                new Vector2(WALL_THICKNESS, ARENA_HEIGHT + WALL_THICKNESS * 2));

            // Top wall — caps walkable area at floor top (characters can't walk into background)
            CreateWall("Wall_Top", walls.transform,
                new Vector3(0f, FLOOR_TOP_Y + WALL_THICKNESS / 2f, 0f),
                new Vector2(ARENA_WIDTH + WALL_THICKNESS * 2, WALL_THICKNESS));

            // Bottom wall — floor bottom
            CreateWall("Wall_Bottom", walls.transform,
                new Vector3(0f, FLOOR_BOTTOM_Y - WALL_THICKNESS / 2f, 0f),
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

        private static GameObject CreatePlayerFromPrefab(string prefabPath, CharacterType characterType)
        {
            // Force reimport to ensure the Library cache is current
            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning("[MovementTestScene] Player prefab not found. Run 'Create Player Prefab' first. Building inline fallback.");
                return BuildInlineFallbackPlayer();
            }

            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.transform.position = new Vector3(-3f, FLOOR_MID_Y, 0f);

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

            // PlayerManaTracker — runtime mana tracking + HUD event firing
            if (player.GetComponent<PlayerManaTracker>() == null)
                player.AddComponent<PlayerManaTracker>();

            // Defense debug UI — floating text on deflect/clash/dodge
            if (player.GetComponent<DefenseDebugUI>() == null)
            {
                var debugUI = player.AddComponent<DefenseDebugUI>();
                var defenseSystem = player.GetComponent<DefenseSystem>();
                if (defenseSystem != null)
                {
                    var debugSO = new SerializedObject(debugUI);
                    debugSO.FindProperty("defenseSystem").objectReferenceValue = defenseSystem;
                    debugSO.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Debug.Log("[MovementTestScene] Player instantiated with input actions + PlayerDamageable + PlayerManaTracker + DefenseDebugUI.");
            return player;
        }

        private static GameObject BuildInlineFallbackPlayer()
        {
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);

            var root = new GameObject("Player");
            root.transform.position = new Vector3(-3f, FLOOR_MID_Y, 0f);

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
            root.AddComponent<PlayerManaTracker>();

            return root;
        }

        private static void WireAction(InputActionAsset asset, SerializedObject so,
            string propertyName, string actionPath)
        {
            var action = asset.FindAction(actionPath);
            if (action == null) return;
            so.FindProperty(propertyName).objectReferenceValue = InputActionReference.Create(action);
        }

        // ── Camera → Player Wiring ───────────────────────────────────────

        private static void WireCameraToPlayer(GameObject camGO, GameObject player)
        {
            if (camGO == null || player == null) return;

            var camController = camGO.GetComponent<CameraController2D>();
            if (camController == null) return;

            var camSO = new SerializedObject(camController);
            var targetsProp = camSO.FindProperty("targets");
            if (targetsProp != null)
            {
                targetsProp.arraySize = 1;
                targetsProp.GetArrayElementAtIndex(0).objectReferenceValue = player.transform;
                camSO.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log("[MovementTestScene] CameraController2D wired to player follow target.");
        }

        // ── WaveManager ─────────────────────────────────────────────────

        private static void CreateWaveManager()
        {
            var wmGO = new GameObject("WaveManager");
            var wm = wmGO.AddComponent<WaveManager>();
            var wmSO = new SerializedObject(wm);

            // Create 3 spawn points spread across the arena
            var spawnRoot = new GameObject("SpawnPoints");
            spawnRoot.transform.SetParent(wmGO.transform);

            var spawnPositions = new Vector3[]
            {
                new Vector3(-5f, FLOOR_MID_Y, 0f),  // Left
                new Vector3(0f, FLOOR_MID_Y, 0f),   // Center
                new Vector3(5f, FLOOR_MID_Y, 0f)    // Right
            };

            var spawnTransforms = new Transform[spawnPositions.Length];
            for (int i = 0; i < spawnPositions.Length; i++)
            {
                var sp = new GameObject($"SpawnPoint_{i}");
                sp.transform.SetParent(spawnRoot.transform);
                sp.transform.position = spawnPositions[i];
                spawnTransforms[i] = sp.transform;
            }

            // Wire spawn points array
            var spawnPointsProp = wmSO.FindProperty("spawnPoints");
            spawnPointsProp.arraySize = spawnTransforms.Length;
            for (int i = 0; i < spawnTransforms.Length; i++)
                spawnPointsProp.GetArrayElementAtIndex(i).objectReferenceValue = spawnTransforms[i];

            // Configure 1 test wave with 3 AI enemy spawns (1 per spawn point)
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BASIC_MELEE_PREFAB_PATH);
            var wavesProp = wmSO.FindProperty("waves");
            wavesProp.arraySize = 1;
            var wave0 = wavesProp.GetArrayElementAtIndex(0);
            wave0.FindPropertyRelative("waveName").stringValue = "Test Wave 1";
            wave0.FindPropertyRelative("isOptional").boolValue = false;

            var groupsProp = wave0.FindPropertyRelative("enemyGroups");
            groupsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var group = groupsProp.GetArrayElementAtIndex(i);
                group.FindPropertyRelative("enemyPrefab").objectReferenceValue = enemyPrefab;
                group.FindPropertyRelative("spawnCount").intValue = 1;
                group.FindPropertyRelative("spawnDelay").floatValue = 0.3f;
                group.FindPropertyRelative("spawnPointIndex").intValue = i;
            }

            // Wire SO event channels
            var onWaveStart = AssetDatabase.LoadAssetAtPath<IntEventChannel>($"{EVENTS_ROOT}/OnWaveStart.asset");
            var onWaveCleared = AssetDatabase.LoadAssetAtPath<VoidEventChannel>($"{EVENTS_ROOT}/OnWaveCleared.asset");
            var onAreaComplete = AssetDatabase.LoadAssetAtPath<VoidEventChannel>($"{EVENTS_ROOT}/OnAreaComplete.asset");
            var onCameraLock = AssetDatabase.LoadAssetAtPath<VoidEventChannel>($"{EVENTS_ROOT}/OnCameraLock.asset");
            var onCameraUnlock = AssetDatabase.LoadAssetAtPath<VoidEventChannel>($"{EVENTS_ROOT}/OnCameraUnlock.asset");
            var onBoundReached = AssetDatabase.LoadAssetAtPath<VoidEventChannel>($"{EVENTS_ROOT}/OnBoundReached.asset");

            if (onWaveStart != null) wmSO.FindProperty("onWaveStart").objectReferenceValue = onWaveStart;
            if (onWaveCleared != null) wmSO.FindProperty("onWaveCleared").objectReferenceValue = onWaveCleared;
            if (onAreaComplete != null) wmSO.FindProperty("onAreaComplete").objectReferenceValue = onAreaComplete;
            if (onCameraLock != null) wmSO.FindProperty("onCameraLock").objectReferenceValue = onCameraLock;
            if (onCameraUnlock != null) wmSO.FindProperty("onCameraUnlock").objectReferenceValue = onCameraUnlock;
            if (onBoundReached != null) wmSO.FindProperty("onBoundReached").objectReferenceValue = onBoundReached;

            wmSO.ApplyModifiedPropertiesWithoutUndo();

            if (enemyPrefab == null)
                Debug.LogWarning("[MovementTestScene] BasicMeleeEnemy prefab not found — WaveManager wave has null enemy prefab.");

            Debug.Log("[MovementTestScene] WaveManager created with 1 test wave (3 AI enemy spawns) and SO event channels.");
        }

        // ── Level Bounds ────────────────────────────────────────────────

        private static void CreateLevelBounds()
        {
            var onBoundReached = AssetDatabase.LoadAssetAtPath<VoidEventChannel>($"{EVENTS_ROOT}/OnBoundReached.asset");

            var boundsRoot = new GameObject("LevelBounds");

            // Left bound — trigger collider just inside the left wall
            CreateLevelBound("LevelBound_Left", boundsRoot.transform,
                new Vector3(-ARENA_WIDTH / 2f + 1.5f, 0f, 0f),
                new Vector2(0.5f, ARENA_HEIGHT),
                onBoundReached);

            // Right bound — trigger collider just inside the right wall
            CreateLevelBound("LevelBound_Right", boundsRoot.transform,
                new Vector3(ARENA_WIDTH / 2f - 1.5f, 0f, 0f),
                new Vector2(0.5f, ARENA_HEIGHT),
                onBoundReached);

            Debug.Log("[MovementTestScene] LevelBounds created (L/R trigger colliders).");
        }

        private static void CreateLevelBound(string name, Transform parent,
            Vector3 position, Vector2 size, VoidEventChannel onBoundReached)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;

            var bound = go.AddComponent<LevelBound>();
            if (onBoundReached != null)
            {
                var boundSO = new SerializedObject(bound);
                boundSO.FindProperty("onBoundReached").objectReferenceValue = onBoundReached;
                boundSO.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ── Player HUD ──────────────────────────────────────────────────

        private static void CreatePlayerHUD()
        {
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HUD_PREFAB_PATH);
            if (hudPrefab == null)
            {
                Debug.LogWarning(
                    "[MovementTestScene] PlayerHUD prefab not found. " +
                    "Run 'TomatoFighters > Create HUD Assets' first. Skipping HUD.");
                return;
            }

            var hud = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
            Debug.Log("[MovementTestScene] PlayerHUD instantiated in scene.");
        }

        // ── SO Event Wiring (Player → HUD) ─────────────────────────────

        private static void WirePlayerSOEvents(GameObject player, CharacterType characterType)
        {
            if (player == null) return;

            // Load SO event channels
            var onHealthChanged = AssetDatabase.LoadAssetAtPath<FloatEventChannel>(
                $"{EVENTS_ROOT}/OnPlayerHealthChanged.asset");
            var onManaChanged = AssetDatabase.LoadAssetAtPath<FloatEventChannel>(
                $"{EVENTS_ROOT}/OnPlayerManaChanged.asset");
            var onComboHit = AssetDatabase.LoadAssetAtPath<IntEventChannel>(
                $"{EVENTS_ROOT}/OnComboHitConfirmed.asset");

            // Wire PlayerDamageable → OnPlayerHealthChanged
            var damageable = player.GetComponent<PlayerDamageable>();
            if (damageable != null && onHealthChanged != null)
            {
                var so = new SerializedObject(damageable);
                so.FindProperty("onHealthChanged").objectReferenceValue = onHealthChanged;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Wire PlayerManaTracker → OnPlayerManaChanged + CharacterBaseStats
            var manaTracker = player.GetComponent<PlayerManaTracker>();
            if (manaTracker != null)
            {
                var so = new SerializedObject(manaTracker);

                if (onManaChanged != null)
                    so.FindProperty("onManaChanged").objectReferenceValue = onManaChanged;

                // Load character stats for mana values
                string statsName = characterType.ToString() + "Stats";
                var baseStats = AssetDatabase.LoadAssetAtPath<CharacterBaseStats>(
                    $"Assets/ScriptableObjects/Characters/{statsName}.asset");
                if (baseStats != null)
                    so.FindProperty("baseStats").objectReferenceValue = baseStats;
                else
                    Debug.LogWarning($"[MovementTestScene] CharacterBaseStats not found: {statsName}");

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Wire ComboController → OnComboHitConfirmed
            var comboController = player.GetComponent<ComboController>();
            if (comboController != null && onComboHit != null)
            {
                var so = new SerializedObject(comboController);
                so.FindProperty("onComboHitConfirmed").objectReferenceValue = onComboHit;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log("[MovementTestScene] Player SO event channels wired (health, mana, combo → HUD).");
        }

        // ── BasicMeleeEnemy (AI) ────────────────────────────────────────

        private static void CreateBasicMeleeEnemies()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BASIC_MELEE_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogWarning(
                    "[MovementTestScene] BasicMeleeEnemy prefab not found. " +
                    "Run the BasicMeleeEnemy creator first. Skipping AI enemy placement.");
                return;
            }

            var enemiesRoot = new GameObject("Enemies");

            // 5 AI enemies spread across the arena
            PlaceBasicMeleeEnemy(prefab, enemiesRoot.transform,
                "Enemy_Center",
                new Vector3(0f, FLOOR_MID_Y, 0f));

            PlaceBasicMeleeEnemy(prefab, enemiesRoot.transform,
                "Enemy_NearRight",
                new Vector3(3f, FLOOR_TOP_Y - 0.5f, 0f));

            PlaceBasicMeleeEnemy(prefab, enemiesRoot.transform,
                "Enemy_FarRight",
                new Vector3(6f, FLOOR_MID_Y, 0f));

            PlaceBasicMeleeEnemy(prefab, enemiesRoot.transform,
                "Enemy_NearLeft",
                new Vector3(-5f, FLOOR_BOTTOM_Y + 0.5f, 0f));

            PlaceBasicMeleeEnemy(prefab, enemiesRoot.transform,
                "Enemy_FarLeft",
                new Vector3(-7f, FLOOR_TOP_Y - 0.3f, 0f));

            Debug.Log("[MovementTestScene] Placed 5 BasicMeleeEnemies with AI.");
        }

        private static void PlaceBasicMeleeEnemy(GameObject prefab, Transform parent,
            string name, Vector3 position)
        {
            var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            enemy.name = name;
            enemy.transform.SetParent(parent);
            enemy.transform.position = position;

            // Wire EnemyHealthBar prefab
            WireEnemyHealthBarPrefab(enemy);

            // Wire playerLayer mask on EnemyAI so it can detect the player
            var ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
            {
                int playerHurtboxLayer = LayerMask.NameToLayer("PlayerHurtbox");
                if (playerHurtboxLayer >= 0)
                {
                    var aiSO = new SerializedObject(ai);
                    aiSO.FindProperty("playerLayer").intValue = 1 << playerHurtboxLayer;
                    aiSO.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Floating label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(enemy.transform);
            labelGO.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            var tm = labelGO.AddComponent<TextMesh>();
            tm.text = "AI MELEE";
            tm.fontSize = 24;
            tm.characterSize = 0.1f;
            tm.anchor = TextAnchor.LowerCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.3f, 0.3f);
        }

        // ── EnemyHealthBar Wiring ───────────────────────────────────────

        private static void WireEnemyHealthBarPrefab(GameObject enemy)
        {
            var enemyBase = enemy.GetComponent<EnemyBase>();
            if (enemyBase == null) return;

            var healthBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_HEALTH_BAR_PREFAB_PATH);
            if (healthBarPrefab == null)
            {
                Debug.LogWarning(
                    "[MovementTestScene] EnemyHealthBar prefab not found. " +
                    "Run 'TomatoFighters > Create HUD Assets' first.");
                return;
            }

            var so = new SerializedObject(enemyBase);
            so.FindProperty("healthBarPrefab").objectReferenceValue = healthBarPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Debug Canvas ────────────────────────────────────────────────

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
