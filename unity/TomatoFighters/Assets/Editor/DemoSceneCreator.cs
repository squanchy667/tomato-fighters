using TomatoFighters.Characters;
using TomatoFighters.Editor.Prefabs;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using TomatoFighters.Shared.Events;
using TomatoFighters.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// Creates the Phase 1 demo scene showcasing everything built so far:
    /// all 4 characters (switchable via 1-4), forest background art, 5 AI enemies,
    /// PlayerHUD, CameraController2D, WaveManager, and LevelBounds.
    /// Run via menu: <b>TomatoFighters &gt; Create Phase 1 Demo Scene</b>.
    /// </summary>
    public static class DemoSceneCreator
    {
        private const string SCENE_FOLDER = "Assets/Scenes";
        private const string SCENE_PATH = SCENE_FOLDER + "/Phase1Demo.unity";
        private const string ENEMY_PREFAB_PATH = "Assets/Prefabs/Enemies/BasicMeleeEnemy.prefab";
        private const string TOMATO_PREFAB_PATH = "Assets/Prefabs/Enemies/Tomato.prefab";
        private const string CORNKNIGHT_PREFAB_PATH = "Assets/Prefabs/Enemies/CornKnight.prefab";
        private const string EGGPLANT_PREFAB_PATH = "Assets/Prefabs/Enemies/EggplantWizard.prefab";
        private const string HUD_PREFAB_PATH = "Assets/Prefabs/UI/PlayerHUD.prefab";
        private const string ENEMY_HEALTH_BAR_PATH = "Assets/Prefabs/UI/EnemyHealthBar.prefab";
        private const string ART_FOLDER = "Assets/Art/Environment/TestArena";
        private const string EVENTS_ROOT = "Assets/ScriptableObjects/Events";
        private const string REGISTRY_FOLDER = "Assets/ScriptableObjects/Characters";
        private const string REGISTRY_PATH = REGISTRY_FOLDER + "/CharacterRegistry.asset";
        private const string RESOURCES_FOLDER = "Assets/Resources";
        private const string RESOURCES_REGISTRY_PATH = RESOURCES_FOLDER + "/CharacterRegistry.asset";

        private const float ARENA_WIDTH = 20f;
        private const float ARENA_HEIGHT = 10f;
        private const float WALL_THICKNESS = 1f;
        private const float FLOOR_HEIGHT = ARENA_HEIGHT * 0.2f;
        private const float FLOOR_BOTTOM_Y = -ARENA_HEIGHT / 2f;
        private const float FLOOR_TOP_Y = FLOOR_BOTTOM_Y + FLOOR_HEIGHT;
        private const float FLOOR_MID_Y = (FLOOR_BOTTOM_Y + FLOOR_TOP_Y) / 2f;

        private static readonly (CharacterType type, string prefabPath, string statsPath)[] CHARACTER_DEFS =
        {
            (CharacterType.Brutor,  "Assets/Prefabs/Player/Brutor.prefab",  "Assets/ScriptableObjects/Characters/BrutorStats.asset"),
            (CharacterType.Slasher, "Assets/Prefabs/Player/Slasher.prefab", "Assets/ScriptableObjects/Characters/SlasherStats.asset"),
            (CharacterType.Mystica, "Assets/Prefabs/Player/Mystica.prefab", "Assets/ScriptableObjects/Characters/MysticaStats.asset"),
            (CharacterType.Viper,   "Assets/Prefabs/Player/Viper.prefab",   "Assets/ScriptableObjects/Characters/ViperStats.asset"),
        };

        [MenuItem("TomatoFighters/Create Phase 1 Demo Scene")]
        public static void CreateDemoScene()
        {
            var registry = CreateOrUpdateRegistry();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLayerCollisionMatrix();
            var camGO = SetupCamera();
            CreateForestBackground();
            CreateArenaWalls();
            var spawnerGO = CreateCharacterSpawner(registry);
            CreateAIEnemies();
            CreateWaveManager();
            CreateLevelBounds();
            CreatePlayerHUD();
            // Controls hint removed — CharacterSelectUI shows controls on the selection screen

            // Wire CameraController2D to follow spawner's spawn point
            // (camera will track the spawned player at runtime)
            WireCameraToSpawner(camGO, spawnerGO);

            PlayerPrefabCreator.EnsureFolderExists(SCENE_FOLDER);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);

            Debug.Log($"[DemoScene] Phase 1 Demo created at {SCENE_PATH}");
            Debug.Log("[DemoScene] 1-4=Switch chars | WASD=Move | Space=Jump | Shift=Dash | LMB=Light | C=Heavy | Ctrl=Run");
        }

        // ── Character Registry ──────────────────────────────────────────

        private static CharacterRegistry CreateOrUpdateRegistry()
        {
            PlayerPrefabCreator.EnsureFolderExists(REGISTRY_FOLDER);

            var registry = AssetDatabase.LoadAssetAtPath<CharacterRegistry>(REGISTRY_PATH);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<CharacterRegistry>();
                AssetDatabase.CreateAsset(registry, REGISTRY_PATH);
            }

            // Use SerializedObject to ensure changes persist to disk
            var so = new SerializedObject(registry);
            var charsProp = so.FindProperty("characters");
            charsProp.arraySize = CHARACTER_DEFS.Length;

            for (int i = 0; i < CHARACTER_DEFS.Length; i++)
            {
                var (type, prefabPath, statsPath) = CHARACTER_DEFS[i];
                var element = charsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("characterType").enumValueIndex = (int)type;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                element.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                element.FindPropertyRelative("baseStats").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<CharacterBaseStats>(statsPath);

                if (prefab == null)
                    Debug.LogWarning($"[DemoScene] Prefab not found for {type} at {prefabPath}. Run 'Create {type}' first.");
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            // Copy to Resources so CharacterSpawner can load at runtime as fallback
            PlayerPrefabCreator.EnsureFolderExists(RESOURCES_FOLDER);
            AssetDatabase.CopyAsset(REGISTRY_PATH, RESOURCES_REGISTRY_PATH);
            AssetDatabase.Refresh();

            Debug.Log($"[DemoScene] CharacterRegistry at {REGISTRY_PATH} — {CHARACTER_DEFS.Length} characters wired.");
            return registry;
        }

        // ── Layer Collision Matrix ──────────────────────────────────────

        private static void SetupLayerCollisionMatrix()
        {
            int playerHitbox = LayerMask.NameToLayer("PlayerHitbox");
            int playerHurtbox = LayerMask.NameToLayer("PlayerHurtbox");
            int enemyHitbox = LayerMask.NameToLayer("EnemyHitbox");
            int enemyHurtbox = LayerMask.NameToLayer("EnemyHurtbox");

            if (playerHitbox < 0 || playerHurtbox < 0 || enemyHitbox < 0 || enemyHurtbox < 0)
            {
                Debug.LogError("[DemoScene] Missing physics layers. Add PlayerHitbox, PlayerHurtbox, EnemyHitbox, EnemyHurtbox.");
                return;
            }

            Physics2D.IgnoreLayerCollision(playerHitbox, enemyHurtbox, false);
            Physics2D.IgnoreLayerCollision(enemyHitbox, playerHurtbox, false);
            Physics2D.IgnoreLayerCollision(playerHurtbox, enemyHurtbox, false); // Bodies block each other
            Physics2D.IgnoreLayerCollision(0, playerHurtbox, false);
            Physics2D.IgnoreLayerCollision(0, enemyHurtbox, false);
            Physics2D.IgnoreLayerCollision(playerHitbox, playerHurtbox, true);
            Physics2D.IgnoreLayerCollision(enemyHitbox, enemyHurtbox, true);
            Physics2D.IgnoreLayerCollision(playerHitbox, playerHitbox, true);
            Physics2D.IgnoreLayerCollision(enemyHitbox, enemyHitbox, true);
            Physics2D.IgnoreLayerCollision(playerHurtbox, playerHurtbox, true);
            Physics2D.IgnoreLayerCollision(enemyHurtbox, enemyHurtbox, true);
        }

        // ── Camera ──────────────────────────────────────────────────────

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

            var camController = camGO.AddComponent<CameraController2D>();
            var camSO = new SerializedObject(camController);
            camSO.FindProperty("boundsMinX").floatValue = -ARENA_WIDTH / 2f;
            camSO.FindProperty("boundsMaxX").floatValue = ARENA_WIDTH / 2f;
            camSO.FindProperty("boundsMinY").floatValue = FLOOR_BOTTOM_Y;
            camSO.FindProperty("boundsMaxY").floatValue = FLOOR_TOP_Y;
            camSO.FindProperty("defaultOrthoSize").floatValue = 7f;

            // Wire camera SO events
            WireSORef<VoidEventChannel>(camSO, "onCameraLock", $"{EVENTS_ROOT}/OnCameraLock.asset");
            WireSORef<VoidEventChannel>(camSO, "onCameraUnlock", $"{EVENTS_ROOT}/OnCameraUnlock.asset");
            WireSORef<VoidEventChannel>(camSO, "onStunTriggered", $"{EVENTS_ROOT}/OnStunTriggered.asset");
            WireSORef<VoidEventChannel>(camSO, "onStunRecovered", $"{EVENTS_ROOT}/OnStunRecovered.asset");
            camSO.ApplyModifiedPropertiesWithoutUndo();

            return camGO;
        }

        private static void WireCameraToSpawner(GameObject camGO, GameObject spawnerGO)
        {
            // The spawner's SpawnPoint child is where the character appears
            var spawnPoint = spawnerGO.transform.Find("SpawnPoint");
            if (spawnPoint == null || camGO == null) return;

            var camController = camGO.GetComponent<CameraController2D>();
            if (camController == null) return;

            // Wire the spawn point as initial camera target
            // At runtime, CharacterSpawner.CurrentPlayer becomes the actual target
            var camSO = new SerializedObject(camController);
            var targetsProp = camSO.FindProperty("targets");
            if (targetsProp != null)
            {
                targetsProp.arraySize = 1;
                targetsProp.GetArrayElementAtIndex(0).objectReferenceValue = spawnPoint;
                camSO.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ── Forest Background ───────────────────────────────────────────

        private static void CreateForestBackground()
        {
            var environment = new GameObject("Environment");

            CreateArtLayer(environment.transform, $"{ART_FOLDER}/bg_forest_distant.png",
                "BG_Distant", -100, Vector3.zero, ARENA_WIDTH, ARENA_HEIGHT);
            CreateArtLayer(environment.transform, $"{ART_FOLDER}/bg_forest_midground.png",
                "BG_Midground", -90, Vector3.zero, ARENA_WIDTH, ARENA_HEIGHT);
            CreateArtLayer(environment.transform, $"{ART_FOLDER}/bg_forest_foreground.png",
                "BG_Foreground", -80, Vector3.zero, ARENA_WIDTH, ARENA_HEIGHT);

            // Ground floor
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

            // Stone walls (visual only)
            var wallLeftSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART_FOLDER}/wall_left_stone.png");
            if (wallLeftSprite != null)
            {
                float wallWidth = ARENA_WIDTH * 0.05f;
                float halfWall = wallWidth / 2f;
                CreateArtLayer(environment.transform, $"{ART_FOLDER}/wall_left_stone.png",
                    "Wall_Left_Visual", -40,
                    new Vector3(-ARENA_WIDTH / 2f + halfWall, 0f, 0f), wallWidth, ARENA_HEIGHT);
            }

            var wallRightSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART_FOLDER}/wall_right_stone.png");
            if (wallRightSprite != null)
            {
                float wallWidth = ARENA_WIDTH * 0.05f;
                float halfWall = wallWidth / 2f;
                CreateArtLayer(environment.transform, $"{ART_FOLDER}/wall_right_stone.png",
                    "Wall_Right_Visual", -40,
                    new Vector3(ARENA_WIDTH / 2f - halfWall, 0f, 0f), wallWidth, ARENA_HEIGHT);
            }
        }

        private static void CreateArtLayer(Transform parent, string spritePath, string goName,
            int sortingOrder, Vector3 position, float targetWidth, float targetHeight)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            var go = new GameObject(goName);
            go.transform.SetParent(parent);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;

            if (sprite != null)
            {
                sr.sprite = sprite;
                float scaleX = targetWidth / sprite.bounds.size.x;
                float scaleY = targetHeight / sprite.bounds.size.y;
                go.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            }
            else
            {
                sr.color = new Color(0.2f, 0.25f, 0.15f);
                go.transform.localScale = new Vector3(targetWidth, targetHeight, 1f);
            }
        }

        // ── Arena Walls ─────────────────────────────────────────────────

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
                new Vector3(0f, FLOOR_TOP_Y + WALL_THICKNESS / 2f, 0f),
                new Vector2(ARENA_WIDTH + WALL_THICKNESS * 2, WALL_THICKNESS));
            CreateWall("Wall_Bottom", walls.transform,
                new Vector3(0f, FLOOR_BOTTOM_Y - WALL_THICKNESS / 2f, 0f),
                new Vector2(ARENA_WIDTH + WALL_THICKNESS * 2, WALL_THICKNESS));
        }

        private static void CreateWall(string name, Transform parent, Vector3 pos, Vector2 size)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.AddComponent<BoxCollider2D>().size = size;
        }

        // ── Character Spawner (1-4 switching) ───────────────────────────

        private static GameObject CreateCharacterSpawner(CharacterRegistry registry)
        {
            var spawnerGO = new GameObject("CharacterSpawner");

            var spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(spawnerGO.transform);
            spawnPoint.transform.localPosition = new Vector3(-3f, FLOOR_MID_Y, 0f);

            // CharacterSpawner — all properties in one SerializedObject pass
            var spawner = spawnerGO.AddComponent<CharacterSpawner>();
            var so = new SerializedObject(spawner);
            so.FindProperty("registry").objectReferenceValue = registry;
            so.FindProperty("selectedCharacter").enumValueIndex = (int)CharacterType.Brutor;
            so.FindProperty("deferSpawn").boolValue = true;
            so.FindProperty("spawnPoint").objectReferenceValue = spawnPoint.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Character select screen — pauses game until player picks
            var selectUI = spawnerGO.AddComponent<CharacterSelectUI>();
            var selectSO = new SerializedObject(selectUI);
            selectSO.FindProperty("spawner").objectReferenceValue = spawner;
            selectSO.FindProperty("registry").objectReferenceValue = registry;
            selectSO.ApplyModifiedPropertiesWithoutUndo();

            // Keep debug switch UI for runtime character switching (1-4 after selection)
            spawnerGO.AddComponent<CharacterSwitchDebugUI>();
            return spawnerGO;
        }

        // ── AI Enemies ──────────────────────────────────────────────────

        private static void CreateAIEnemies()
        {
            var tomatoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TOMATO_PREFAB_PATH);
            var cornKnightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CORNKNIGHT_PREFAB_PATH);
            var eggplantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EGGPLANT_PREFAB_PATH);
            var basicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_PREFAB_PATH);

            if (basicPrefab == null && tomatoPrefab == null && cornKnightPrefab == null && eggplantPrefab == null)
            {
                Debug.LogWarning("[DemoScene] No enemy prefabs found. Run enemy creator scripts first.");
                return;
            }

            var root = new GameObject("Enemies");
            int placed = 0;

            // 2 Tomatoes front and center
            if (tomatoPrefab != null)
            {
                PlaceEnemy(tomatoPrefab, root.transform, "Tomato_1", new Vector3(3f, FLOOR_MID_Y, 0f));
                PlaceEnemy(tomatoPrefab, root.transform, "Tomato_2", new Vector3(6f, FLOOR_MID_Y, 0f));
                placed += 2;
            }

            Debug.Log($"[DemoScene] Placed {placed} AI enemies.");
        }

        private static void PlaceEnemy(GameObject prefab, Transform parent, string name, Vector3 position)
        {
            var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            enemy.name = name;
            enemy.transform.SetParent(parent);
            enemy.transform.position = position;

            // Wire EnemyHealthBar prefab
            var enemyBase = enemy.GetComponent<EnemyBase>();
            if (enemyBase != null)
            {
                var healthBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_HEALTH_BAR_PATH);
                if (healthBarPrefab != null)
                {
                    var so = new SerializedObject(enemyBase);
                    so.FindProperty("healthBarPrefab").objectReferenceValue = healthBarPrefab;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Wire playerLayer on EnemyAI
            var ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
            {
                int playerHurtbox = LayerMask.NameToLayer("PlayerHurtbox");
                if (playerHurtbox >= 0)
                {
                    var aiSO = new SerializedObject(ai);
                    aiSO.FindProperty("playerLayer").intValue = 1 << playerHurtbox;
                    aiSO.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        // ── WaveManager ─────────────────────────────────────────────────

        private static void CreateWaveManager()
        {
            var wmGO = new GameObject("WaveManager");
            var wm = wmGO.AddComponent<WaveManager>();
            var wmSO = new SerializedObject(wm);

            var spawnRoot = new GameObject("SpawnPoints");
            spawnRoot.transform.SetParent(wmGO.transform);

            var positions = new[]
            {
                new Vector3(-5f, FLOOR_MID_Y, 0f),
                new Vector3(0f, FLOOR_MID_Y, 0f),
                new Vector3(5f, FLOOR_MID_Y, 0f)
            };

            var spawnPointsProp = wmSO.FindProperty("spawnPoints");
            spawnPointsProp.arraySize = positions.Length;
            for (int i = 0; i < positions.Length; i++)
            {
                var sp = new GameObject($"SpawnPoint_{i}");
                sp.transform.SetParent(spawnRoot.transform);
                sp.transform.position = positions[i];
                spawnPointsProp.GetArrayElementAtIndex(i).objectReferenceValue = sp.transform;
            }

            // 1 test wave with Tomato enemies
            var tomatoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TOMATO_PREFAB_PATH);
            var basicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_PREFAB_PATH);
            var wavePrefab = tomatoPrefab ?? basicPrefab;

            var wavesProp = wmSO.FindProperty("waves");
            wavesProp.arraySize = 1;
            var wave0 = wavesProp.GetArrayElementAtIndex(0);
            wave0.FindPropertyRelative("waveName").stringValue = "Demo Wave";
            wave0.FindPropertyRelative("isOptional").boolValue = false;
            var groupsProp = wave0.FindPropertyRelative("enemyGroups");
            groupsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var group = groupsProp.GetArrayElementAtIndex(i);
                group.FindPropertyRelative("enemyPrefab").objectReferenceValue = wavePrefab;
                group.FindPropertyRelative("spawnCount").intValue = 1;
                group.FindPropertyRelative("spawnDelay").floatValue = 0.5f;
                group.FindPropertyRelative("spawnPointIndex").intValue = i;
            }

            WireSORef<IntEventChannel>(wmSO, "onWaveStart", $"{EVENTS_ROOT}/OnWaveStart.asset");
            WireSORef<VoidEventChannel>(wmSO, "onWaveCleared", $"{EVENTS_ROOT}/OnWaveCleared.asset");
            WireSORef<VoidEventChannel>(wmSO, "onAreaComplete", $"{EVENTS_ROOT}/OnAreaComplete.asset");
            WireSORef<VoidEventChannel>(wmSO, "onCameraLock", $"{EVENTS_ROOT}/OnCameraLock.asset");
            WireSORef<VoidEventChannel>(wmSO, "onCameraUnlock", $"{EVENTS_ROOT}/OnCameraUnlock.asset");
            WireSORef<VoidEventChannel>(wmSO, "onBoundReached", $"{EVENTS_ROOT}/OnBoundReached.asset");
            wmSO.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Level Bounds ────────────────────────────────────────────────

        private static void CreateLevelBounds()
        {
            var onBound = AssetDatabase.LoadAssetAtPath<VoidEventChannel>($"{EVENTS_ROOT}/OnBoundReached.asset");
            var root = new GameObject("LevelBounds");

            CreateBound("LevelBound_Left", root.transform,
                new Vector3(-ARENA_WIDTH / 2f + 1.5f, 0f, 0f),
                new Vector2(0.5f, ARENA_HEIGHT), onBound);
            CreateBound("LevelBound_Right", root.transform,
                new Vector3(ARENA_WIDTH / 2f - 1.5f, 0f, 0f),
                new Vector2(0.5f, ARENA_HEIGHT), onBound);
        }

        private static void CreateBound(string name, Transform parent, Vector3 pos,
            Vector2 size, VoidEventChannel onBound)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = true;
            var bound = go.AddComponent<LevelBound>();
            if (onBound != null)
            {
                var so = new SerializedObject(bound);
                so.FindProperty("onBoundReached").objectReferenceValue = onBound;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ── Player HUD ──────────────────────────────────────────────────

        private static void CreatePlayerHUD()
        {
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HUD_PREFAB_PATH);
            if (hudPrefab == null)
            {
                Debug.LogWarning("[DemoScene] PlayerHUD prefab not found. Run 'Create HUD Assets' first.");
                return;
            }

            PrefabUtility.InstantiatePrefab(hudPrefab);
            Debug.Log("[DemoScene] PlayerHUD instantiated.");
        }

        // ── Helpers ─────────────────────────────────────────────────────

        private static void WireSORef<T>(SerializedObject so, string propName, string assetPath) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
                so.FindProperty(propName).objectReferenceValue = asset;
        }
    }
}
