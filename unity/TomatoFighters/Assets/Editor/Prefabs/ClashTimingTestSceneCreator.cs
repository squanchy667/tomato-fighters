using TomatoFighters.Characters;
using TomatoFighters.Combat;
using TomatoFighters.Combat.Diagnostics;
using TomatoFighters.Shared.Components;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TomatoFighters.Editor.Prefabs
{
    /// <summary>
    /// Creates a dedicated clash timing test scene. Reuses the arena/player/enemy
    /// setup from <see cref="MovementTestSceneCreator"/> and adds a
    /// <see cref="ClashTimingAutoTest"/> that auto-fires heavy attacks during
    /// enemy telegraphs to validate clash timing.
    ///
    /// <para>Run via menu: <b>TomatoFighters > Tests > Create Clash Timing Test Scene</b></para>
    ///
    /// <para><b>Controls in scene:</b></para>
    /// <list type="bullet">
    ///   <item>T = toggle auto-fire on/off</item>
    ///   <item>Y = cycle through timing offsets (0.1-1.5s)</item>
    ///   <item>Normal movement/attack controls still work for manual testing</item>
    /// </list>
    /// </summary>
    public static class ClashTimingTestSceneCreator
    {
        private const string SCENE_FOLDER = "Assets/Scenes";
        private const string SCENE_PATH = SCENE_FOLDER + "/ClashTimingTest.unity";
        private const string SLASHER_PREFAB_PATH = "Assets/Prefabs/Player/Slasher.prefab";
        private const string DUMMY_PREFAB_PATH = "Assets/Prefabs/Enemies/TestDummy.prefab";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";

        private const float ARENA_WIDTH = 20f;
        private const float ARENA_HEIGHT = 10f;
        private const float WALL_THICKNESS = 1f;

        [MenuItem("TomatoFighters/Tests/Create Clash Timing Test Scene")]
        public static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLayerCollisionMatrix();
            SetupCamera();
            CreateArenaBackground();
            CreateArenaWalls();

            var player = CreatePlayer();
            CreateTestDummy();
            CreateClashTimingAutoTest(player);
            CreateControlsHint();

            PlayerPrefabCreator.EnsureFolderExists(SCENE_FOLDER);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);

            Debug.Log($"[ClashTimingTestScene] Scene created at {SCENE_PATH}");
            Debug.Log("[ClashTimingTestScene] T = toggle auto-fire | Y = cycle timings | C = manual heavy attack");
        }

        // ── Player ──────────────────────────────────────────────────────

        private static GameObject CreatePlayer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SLASHER_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError(
                    "[ClashTimingTestScene] Slasher prefab not found. " +
                    "Run 'TomatoFighters > Characters > Create Slasher' first.");
                return null;
            }

            AssetDatabase.ImportAsset(SLASHER_PREFAB_PATH, ImportAssetOptions.ForceUpdate);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.transform.position = new Vector3(-3f, 0f, 0f);

            // Wire input actions on the scene instance
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

            // PlayerDamageable for bidirectional damage
            if (player.GetComponent<PlayerDamageable>() == null)
                player.AddComponent<PlayerDamageable>();

            // Wire PlayerDamageable.defenseSystem
            var damageable = player.GetComponent<PlayerDamageable>();
            var defenseSystem = player.GetComponent<DefenseSystem>();
            if (damageable != null && defenseSystem != null)
            {
                var dmgSO = new SerializedObject(damageable);
                dmgSO.FindProperty("defenseSystem").objectReferenceValue = defenseSystem;
                dmgSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // Debug HP bar
            if (player.GetComponent<DebugHealthBar>() == null)
            {
                var hpBar = player.AddComponent<DebugHealthBar>();
                var hbSO = new SerializedObject(hpBar);
                var fillColorProp = hbSO.FindProperty("fillColor");
                if (fillColorProp != null)
                    fillColorProp.colorValue = new Color(0.2f, 0.8f, 0.3f);
                hbSO.ApplyModifiedPropertiesWithoutUndo();
            }

            // Defense debug UI (floating CLASHED!/DEFLECTED!/DODGED! text)
            if (player.GetComponent<DefenseDebugUI>() == null)
            {
                var debugUI = player.AddComponent<DefenseDebugUI>();
                if (defenseSystem != null)
                {
                    var debugSO = new SerializedObject(debugUI);
                    debugSO.FindProperty("defenseSystem").objectReferenceValue = defenseSystem;
                    debugSO.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            Debug.Log("[ClashTimingTestScene] Slasher player placed at (-3, 0).");
            return player;
        }

        // ── Enemy ───────────────────────────────────────────────────────

        private static void CreateTestDummy()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DUMMY_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogError(
                    "[ClashTimingTestScene] TestDummy prefab not found. " +
                    "Run 'TomatoFighters > Create TestDummy Prefab' first.");
                return;
            }

            var dummy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            dummy.transform.position = new Vector3(3f, 0f, 0f);

            if (dummy.GetComponent<DebugHealthBar>() == null)
                dummy.AddComponent<DebugHealthBar>();

            Debug.Log("[ClashTimingTestScene] TestDummy placed at (3, 0).");
        }

        // ── Auto-Tester ─────────────────────────────────────────────────

        private static void CreateClashTimingAutoTest(GameObject player)
        {
            var go = new GameObject("ClashTimingAutoTest");
            var tester = go.AddComponent<ClashTimingAutoTest>();

            if (player != null)
            {
                var so = new SerializedObject(tester);

                var combo = player.GetComponent<ComboController>();
                if (combo != null)
                    so.FindProperty("playerCombo").objectReferenceValue = combo;

                var defense = player.GetComponent<DefenseSystem>();
                if (defense != null)
                    so.FindProperty("playerDefense").objectReferenceValue = defense;

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log("[ClashTimingTestScene] ClashTimingAutoTest added.");
        }

        // ── Arena ───────────────────────────────────────────────────────

        private static void SetupLayerCollisionMatrix()
        {
            int playerHitbox = LayerMask.NameToLayer("PlayerHitbox");
            int playerHurtbox = LayerMask.NameToLayer("PlayerHurtbox");
            int enemyHitbox = LayerMask.NameToLayer("EnemyHitbox");
            int enemyHurtbox = LayerMask.NameToLayer("EnemyHurtbox");

            if (playerHitbox < 0 || playerHurtbox < 0 || enemyHitbox < 0 || enemyHurtbox < 0)
            {
                Debug.LogError(
                    "[ClashTimingTestScene] Missing physics layers. " +
                    "Add PlayerHitbox, PlayerHurtbox, EnemyHitbox, EnemyHurtbox in Tags and Layers.");
                return;
            }

            Physics2D.IgnoreLayerCollision(playerHitbox, enemyHurtbox, false);
            Physics2D.IgnoreLayerCollision(enemyHitbox, playerHurtbox, false);
            Physics2D.IgnoreLayerCollision(playerHitbox, playerHurtbox, true);
            Physics2D.IgnoreLayerCollision(enemyHitbox, enemyHurtbox, true);
            Physics2D.IgnoreLayerCollision(playerHitbox, playerHitbox, true);
            Physics2D.IgnoreLayerCollision(enemyHitbox, enemyHitbox, true);
            Physics2D.IgnoreLayerCollision(playerHurtbox, playerHurtbox, true);
            Physics2D.IgnoreLayerCollision(enemyHurtbox, enemyHurtbox, true);
        }

        private static void SetupCamera()
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.18f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGO.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateArenaBackground()
        {
            var ground = new GameObject("Ground");
            var sr = ground.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRectSprite();
            sr.color = new Color(0.2f, 0.22f, 0.28f);
            ground.transform.localScale = new Vector3(ARENA_WIDTH, ARENA_HEIGHT, 1f);
            ground.transform.position = Vector3.zero;
            sr.sortingOrder = -10;
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

        private static void CreateControlsHint()
        {
            var textGO = new GameObject("ControlsHint");
            var tm = textGO.AddComponent<TextMesh>();
            tm.text = "CLASH TIMING TEST | C: Heavy Attack | T: Toggle Auto | Y: Cycle Timings";
            tm.fontSize = 24;
            tm.characterSize = 0.12f;
            tm.anchor = TextAnchor.UpperCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 1f, 0.6f, 0.7f);
            textGO.transform.position = new Vector3(0f, ARENA_HEIGHT / 2f - 0.5f, 0f);
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static void WireAction(InputActionAsset asset, SerializedObject so,
            string propertyName, string actionPath)
        {
            var action = asset.FindAction(actionPath);
            if (action == null) return;
            so.FindProperty(propertyName).objectReferenceValue = InputActionReference.Create(action);
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
