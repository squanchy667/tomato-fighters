using TomatoFighters.Characters;
using TomatoFighters.Combat;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TomatoFighters.Editor.Prefabs
{
    /// <summary>
    /// Creates a minimal test scene for verifying belt-scroll movement.
    /// Run via menu: TomatoFighters > Create Movement Test Scene.
    /// </summary>
    public static class MovementTestSceneCreator
    {
        private const string SCENE_FOLDER = "Assets/Scenes";
        private const string SCENE_PATH = SCENE_FOLDER + "/MovementTest.unity";
        private const string CONFIG_FOLDER = "Assets/ScriptableObjects/MovementConfigs";
        private const string CONFIG_PATH = CONFIG_FOLDER + "/Brutor_MovementConfig.asset";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";

        private const float ARENA_WIDTH = 20f;
        private const float ARENA_HEIGHT = 10f;
        private const float WALL_THICKNESS = 1f;

        [MenuItem("TomatoFighters/Create Movement Test Scene")]
        public static void CreateTestScene()
        {
            // Create a new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Setup camera
            SetupCamera();

            // Create arena
            CreateArenaBackground();
            CreateArenaWalls();

            // Create player
            var config = CreateOrLoadMovementConfig();
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(INPUT_ACTIONS_PATH);
            CreatePlayer(config, inputActions);

            // Create debug UI
            CreateDebugCanvas();

            // Save scene
            PlayerPrefabCreator_EnsureFolderExists(SCENE_FOLDER);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);

            Debug.Log($"[MovementTestScene] Scene created at {SCENE_PATH}");
            Debug.Log("[MovementTestScene] Controls: WASD = move, Space = jump, Left Shift = dash");
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
            // Ground plane visual
            var ground = new GameObject("Ground");
            var sr = ground.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRectSprite();
            sr.color = new Color(0.25f, 0.3f, 0.2f); // dark green
            ground.transform.localScale = new Vector3(ARENA_WIDTH, ARENA_HEIGHT, 1f);
            ground.transform.position = Vector3.zero;
            sr.sortingOrder = -10;

            // Grid lines for depth perception
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

            // Left wall
            CreateWall("Wall_Left", walls.transform,
                new Vector3(-ARENA_WIDTH / 2f - WALL_THICKNESS / 2f, 0f, 0f),
                new Vector2(WALL_THICKNESS, ARENA_HEIGHT + WALL_THICKNESS * 2));

            // Right wall
            CreateWall("Wall_Right", walls.transform,
                new Vector3(ARENA_WIDTH / 2f + WALL_THICKNESS / 2f, 0f, 0f),
                new Vector2(WALL_THICKNESS, ARENA_HEIGHT + WALL_THICKNESS * 2));

            // Top wall
            CreateWall("Wall_Top", walls.transform,
                new Vector3(0f, ARENA_HEIGHT / 2f + WALL_THICKNESS / 2f, 0f),
                new Vector2(ARENA_WIDTH + WALL_THICKNESS * 2, WALL_THICKNESS));

            // Bottom wall
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

        private static void CreatePlayer(MovementConfig config, InputActionAsset inputActions)
        {
            var root = new GameObject("Player");
            root.transform.position = Vector3.zero;

            // Rigidbody2D — no gravity (belt-scroll)
            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Body collider (stays on ground plane)
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.6f);
            col.offset = Vector2.zero;

            // Sprite child (offset by jumpHeight at runtime)
            var spriteChild = new GameObject("Sprite");
            spriteChild.transform.SetParent(root.transform);
            spriteChild.transform.localPosition = Vector3.zero;
            var sr = spriteChild.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRectSprite();
            sr.color = new Color(0.85f, 0.2f, 0.2f); // tomato red
            spriteChild.transform.localScale = new Vector3(0.8f, 1.2f, 1f);
            sr.sortingOrder = 1;

            // Shadow child (stays at feet)
            var shadowChild = new GameObject("Shadow");
            shadowChild.transform.SetParent(root.transform);
            shadowChild.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            var shadowSR = shadowChild.AddComponent<SpriteRenderer>();
            shadowSR.sprite = CreateRectSprite();
            shadowSR.color = new Color(0f, 0f, 0f, 0.3f);
            shadowChild.transform.localScale = new Vector3(0.9f, 0.2f, 1f);
            shadowSR.sortingOrder = 0;

            // CharacterMotor
            var motor = root.AddComponent<CharacterMotor>();
            var motorSO = new SerializedObject(motor);
            motorSO.FindProperty("config").objectReferenceValue = config;
            motorSO.FindProperty("characterType").enumValueIndex = (int)CharacterType.Brutor;
            motorSO.FindProperty("spriteTransform").objectReferenceValue = spriteChild.transform;
            motorSO.ApplyModifiedPropertiesWithoutUndo();

            // CharacterInputHandler with input action wiring
            var inputHandler = root.AddComponent<CharacterInputHandler>();
            var inputSO = new SerializedObject(inputHandler);
            inputSO.FindProperty("motor").objectReferenceValue = motor;

            if (inputActions != null)
            {
                var playerMap = inputActions.FindActionMap("Player");
                if (playerMap != null)
                {
                    var moveRef = FindActionReference(inputActions, "Player/Move");
                    var jumpRef = FindActionReference(inputActions, "Player/Jump");
                    var dashRef = FindActionReference(inputActions, "Player/Sprint"); // Sprint = Dash for testing

                    if (moveRef != null) inputSO.FindProperty("moveAction").objectReferenceValue = moveRef;
                    if (jumpRef != null) inputSO.FindProperty("jumpAction").objectReferenceValue = jumpRef;
                    if (dashRef != null) inputSO.FindProperty("dashAction").objectReferenceValue = dashRef;
                }
            }
            else
            {
                Debug.LogWarning("[MovementTestScene] InputSystem_Actions.inputactions not found at " + INPUT_ACTIONS_PATH);
            }

            inputSO.ApplyModifiedPropertiesWithoutUndo();
        }

        private static InputActionReference FindActionReference(InputActionAsset asset, string actionPath)
        {
            var action = asset.FindAction(actionPath);
            if (action == null)
            {
                Debug.LogWarning($"[MovementTestScene] Input action '{actionPath}' not found.");
                return null;
            }
            return InputActionReference.Create(action);
        }

        private static void CreateDebugCanvas()
        {
            // Simple world-space text showing controls
            var textGO = new GameObject("ControlsHint");
            var tm = textGO.AddComponent<TextMesh>();
            tm.text = "WASD: Move | Space: Jump | L-Shift: Dash";
            tm.fontSize = 24;
            tm.characterSize = 0.15f;
            tm.anchor = TextAnchor.UpperCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 1f, 1f, 0.5f);
            textGO.transform.position = new Vector3(0f, ARENA_HEIGHT / 2f - 0.5f, 0f);
        }

        private static MovementConfig CreateOrLoadMovementConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MovementConfig>(CONFIG_PATH);
            if (existing != null) return existing;

            PlayerPrefabCreator_EnsureFolderExists(CONFIG_FOLDER);

            var config = ScriptableObject.CreateInstance<MovementConfig>();
            config.moveSpeed = 5.6f;
            config.depthSpeed = 3.5f;
            config.groundAcceleration = 60f;
            config.airAcceleration = 30f;
            config.jumpForce = 12f;
            config.jumpGravity = 35f;
            config.coyoteTime = 0.1f;
            config.jumpBufferTime = 0.12f;
            config.dashSpeed = 16f;
            config.dashDuration = 0.18f;
            config.dashCooldown = 0.7f;
            config.dashHasIFrames = true;

            AssetDatabase.CreateAsset(config, CONFIG_PATH);
            AssetDatabase.SaveAssets();
            return config;
        }

        /// <summary>Creates a simple 1x1 white pixel sprite for shapes.</summary>
        private static Sprite CreateRectSprite()
        {
            var tex = new Texture2D(4, 4);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }

        private static void PlayerPrefabCreator_EnsureFolderExists(string path)
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
