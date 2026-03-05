using TomatoFighters.Characters;
using TomatoFighters.Editor.Prefabs;
using TomatoFighters.Paths;
using TomatoFighters.Roguelite;
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
    /// Creates the Phase 3 demo scene showcasing the full roguelite loop:
    /// character select → wave 1 (fight) → ritual reward pick → wave 2 (fight)
    /// → path selection shrine → wave 3 (boss fight, 3 phases).
    ///
    /// <para>Run via menu: <b>TomatoFighters &gt; Create Phase 3 Demo Scene</b>.</para>
    ///
    /// <para><b>Prerequisites:</b> Run the following creators first (this script calls
    /// them automatically if assets are missing):
    /// Create BasicMeleeEnemy, Create TestBoss, Create RewardSelectorUI,
    /// Create All Path Assets, Create Ritual Assets, Create HUD Assets.</para>
    ///
    /// <para><b>Demo limitations:</b> Ritual effects won't trigger (needs runtime
    /// ICombatEvents wiring). Path abilities (Q/E) require manual wiring of
    /// IPathProvider on the player. Path options shown are Brutor's 3 paths
    /// regardless of selected character.</para>
    /// </summary>
    public static class Phase3DemoSceneCreator
    {
        // ── Asset Paths ─────────────────────────────────────────────────────

        private const string SCENE_FOLDER = "Assets/Scenes";
        private const string SCENE_PATH = SCENE_FOLDER + "/Phase3Demo.unity";

        private const string ENEMY_PREFAB_PATH = "Assets/Prefabs/Enemies/BasicMeleeEnemy.prefab";
        private const string BOSS_PREFAB_PATH = "Assets/Prefabs/Enemies/TestBoss.prefab";
        private const string HUD_PREFAB_PATH = "Assets/Prefabs/UI/PlayerHUD.prefab";
        private const string ENEMY_HEALTH_BAR_PATH = "Assets/Prefabs/UI/EnemyHealthBar.prefab";
        private const string REWARD_UI_PREFAB_PATH = "Assets/Prefabs/UI/RewardSelectorUI.prefab";

        private const string ART_FOLDER = "Assets/Art/Environment/TestArena";
        private const string EVENTS_ROOT = "Assets/ScriptableObjects/Events";
        private const string REGISTRY_FOLDER = "Assets/ScriptableObjects/Characters";
        private const string REGISTRY_PATH = REGISTRY_FOLDER + "/CharacterRegistry.asset";
        private const string RESOURCES_FOLDER = "Assets/Resources";
        private const string RESOURCES_REGISTRY_PATH = RESOURCES_FOLDER + "/CharacterRegistry.asset";
        private const string REWARD_CONFIG_PATH = "Assets/ScriptableObjects/Roguelite/RewardConfig.asset";
        private const string PATH_SO_ROOT = "Assets/ScriptableObjects/Paths/Brutor";
        private const string RITUAL_SO_ROOT = "Assets/ScriptableObjects/Rituals";

        // Event channel paths
        private const string EVT_WAVE_START = EVENTS_ROOT + "/OnWaveStart.asset";
        private const string EVT_WAVE_CLEARED = EVENTS_ROOT + "/OnWaveCleared.asset";
        private const string EVT_AREA_COMPLETE = EVENTS_ROOT + "/OnAreaComplete.asset";
        private const string EVT_CAMERA_LOCK = EVENTS_ROOT + "/OnCameraLock.asset";
        private const string EVT_CAMERA_UNLOCK = EVENTS_ROOT + "/OnCameraUnlock.asset";
        private const string EVT_BOUND_REACHED = EVENTS_ROOT + "/OnBoundReached.asset";
        private const string EVT_STUN_TRIGGERED = EVENTS_ROOT + "/OnStunTriggered.asset";
        private const string EVT_STUN_RECOVERED = EVENTS_ROOT + "/OnStunRecovered.asset";
        private const string EVT_SHOW_REWARD = EVENTS_ROOT + "/OnShowRewardSelector.asset";
        private const string EVT_REWARD_SELECTED = EVENTS_ROOT + "/OnRewardSelected.asset";
        private const string EVT_SHOW_PATH = EVENTS_ROOT + "/OnShowPathSelection.asset";

        // ── Arena Geometry ──────────────────────────────────────────────────

        private const float ARENA_WIDTH = 24f;   // Slightly wider than Phase 1 for boss room
        private const float ARENA_HEIGHT = 10f;
        private const float WALL_THICKNESS = 1f;
        private const float FLOOR_HEIGHT = ARENA_HEIGHT * 0.2f;
        private const float FLOOR_BOTTOM_Y = -ARENA_HEIGHT / 2f;
        private const float FLOOR_TOP_Y = FLOOR_BOTTOM_Y + FLOOR_HEIGHT;
        private const float FLOOR_MID_Y = (FLOOR_BOTTOM_Y + FLOOR_TOP_Y) / 2f;

        // ── Character Definitions ───────────────────────────────────────────

        private static readonly (CharacterType type, string prefabPath, string statsPath)[] CHARACTER_DEFS =
        {
            (CharacterType.Brutor,  "Assets/Prefabs/Player/Brutor.prefab",  "Assets/ScriptableObjects/Characters/BrutorStats.asset"),
            (CharacterType.Slasher, "Assets/Prefabs/Player/Slasher.prefab", "Assets/ScriptableObjects/Characters/SlasherStats.asset"),
            (CharacterType.Mystica, "Assets/Prefabs/Player/Mystica.prefab", "Assets/ScriptableObjects/Characters/MysticaStats.asset"),
            (CharacterType.Viper,   "Assets/Prefabs/Player/Viper.prefab",   "Assets/ScriptableObjects/Characters/ViperStats.asset"),
        };

        // ── Entry Point ─────────────────────────────────────────────────────

        [MenuItem("TomatoFighters/Create Phase 3 Demo Scene")]
        public static void CreatePhase3DemoScene()
        {
            EnsurePrerequisites();

            var registry = CreateOrUpdateRegistry();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLayerCollisionMatrix();
            var camGO = SetupCamera();
            CreateForestBackground();
            CreateArenaWalls();
            var spawnerGO = CreateCharacterSpawner(registry);
            CreateWaveManager();
            CreateLevelBounds();
            CreatePlayerHUD();
            var pathSystem = CreatePathSystem();
            var ritualSystem = CreateRitualSystem();
            CreateRewardSelectorUI(ritualSystem);
            CreatePathSelectionUI(pathSystem);
            CreatePhase3DemoMediator();
            WireHealthBarOnPrefab(ENEMY_PREFAB_PATH);
            WireHealthBarOnPrefab(BOSS_PREFAB_PATH);
            WireCameraToSpawner(camGO, spawnerGO);

            PlayerPrefabCreator.EnsureFolderExists(SCENE_FOLDER);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);

            Debug.Log($"[Phase3Demo] Scene created at {SCENE_PATH}");
            Debug.Log("[Phase3Demo] Flow: Select char → Walk right → Wave 1 → Pick ritual → Wave 2 → Pick path → Boss fight");
            Debug.Log("[Phase3Demo] Controls: WASD=Move | Space=Jump | Shift=Dash | LMB=Light | C=Heavy | Q/E=Path abilities");
        }

        // ── Prerequisites ───────────────────────────────────────────────────

        private static void EnsurePrerequisites()
        {
            // Ensure ritual SOs exist
            if (AssetDatabase.LoadAssetAtPath<RitualData>($"{RITUAL_SO_ROOT}/Fire/BurnRitual.asset") == null)
            {
                Debug.Log("[Phase3Demo] Ritual assets missing — creating...");
                RitualDataCreator.CreateAllRitualAssets();
            }

            // Ensure path SOs exist
            if (AssetDatabase.LoadAssetAtPath<PathData>($"{PATH_SO_ROOT}/WardenPath.asset") == null)
            {
                Debug.Log("[Phase3Demo] Path assets missing — creating...");
                PathDataCreator.CreateAllPathAssets();
            }

            // Ensure RewardSelectorUI prefab exists
            if (AssetDatabase.LoadAssetAtPath<GameObject>(REWARD_UI_PREFAB_PATH) == null)
            {
                Debug.Log("[Phase3Demo] RewardSelectorUI prefab missing — creating...");
                RewardSelectorUICreator.Create();
            }

            // Ensure boss prefab exists
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BOSS_PREFAB_PATH) == null)
            {
                Debug.Log("[Phase3Demo] TestBoss prefab missing — creating...");
                BossPrefabCreator.CreateTestBossPrefab();
            }

            // Ensure enemy prefab exists
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_PREFAB_PATH) == null)
            {
                Debug.LogWarning("[Phase3Demo] BasicMeleeEnemy prefab not found. Run 'Create BasicMeleeEnemy Prefab' first.");
            }
        }

        // ── Character Registry ──────────────────────────────────────────────

        private static CharacterRegistry CreateOrUpdateRegistry()
        {
            PlayerPrefabCreator.EnsureFolderExists(REGISTRY_FOLDER);

            var registry = AssetDatabase.LoadAssetAtPath<CharacterRegistry>(REGISTRY_PATH);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<CharacterRegistry>();
                AssetDatabase.CreateAsset(registry, REGISTRY_PATH);
            }

            var so = new SerializedObject(registry);
            var charsProp = so.FindProperty("characters");
            charsProp.arraySize = CHARACTER_DEFS.Length;

            for (int i = 0; i < CHARACTER_DEFS.Length; i++)
            {
                var (type, prefabPath, statsPath) = CHARACTER_DEFS[i];
                var element = charsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("characterType").enumValueIndex = (int)type;
                element.FindPropertyRelative("prefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                element.FindPropertyRelative("baseStats").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<CharacterBaseStats>(statsPath);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            PlayerPrefabCreator.EnsureFolderExists(RESOURCES_FOLDER);
            AssetDatabase.CopyAsset(REGISTRY_PATH, RESOURCES_REGISTRY_PATH);
            AssetDatabase.Refresh();
            return registry;
        }

        // ── Layer Collision Matrix ──────────────────────────────────────────

        private static void SetupLayerCollisionMatrix()
        {
            int playerHitbox = LayerMask.NameToLayer("PlayerHitbox");
            int playerHurtbox = LayerMask.NameToLayer("PlayerHurtbox");
            int enemyHitbox = LayerMask.NameToLayer("EnemyHitbox");
            int enemyHurtbox = LayerMask.NameToLayer("EnemyHurtbox");

            if (playerHitbox < 0 || playerHurtbox < 0 || enemyHitbox < 0 || enemyHurtbox < 0)
            {
                Debug.LogError("[Phase3Demo] Missing physics layers. Add PlayerHitbox, PlayerHurtbox, EnemyHitbox, EnemyHurtbox.");
                return;
            }

            Physics2D.IgnoreLayerCollision(playerHitbox, enemyHurtbox, false);
            Physics2D.IgnoreLayerCollision(enemyHitbox, playerHurtbox, false);
            Physics2D.IgnoreLayerCollision(0, playerHurtbox, false);
            Physics2D.IgnoreLayerCollision(0, enemyHurtbox, false);
            Physics2D.IgnoreLayerCollision(playerHitbox, playerHurtbox, true);
            Physics2D.IgnoreLayerCollision(enemyHitbox, enemyHurtbox, true);
            Physics2D.IgnoreLayerCollision(playerHitbox, playerHitbox, true);
            Physics2D.IgnoreLayerCollision(enemyHitbox, enemyHitbox, true);
            Physics2D.IgnoreLayerCollision(playerHurtbox, playerHurtbox, true);
            Physics2D.IgnoreLayerCollision(enemyHurtbox, enemyHurtbox, true);
        }

        // ── Camera ──────────────────────────────────────────────────────────

        private static GameObject SetupCamera()
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.backgroundColor = new Color(0.12f, 0.1f, 0.18f); // Darker, more dramatic
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGO.transform.position = new Vector3(0f, 0f, -10f);

            var camController = camGO.AddComponent<CameraController2D>();
            var camSO = new SerializedObject(camController);
            camSO.FindProperty("boundsMinX").floatValue = -ARENA_WIDTH / 2f;
            camSO.FindProperty("boundsMaxX").floatValue = ARENA_WIDTH / 2f;
            camSO.FindProperty("boundsMinY").floatValue = FLOOR_BOTTOM_Y;
            camSO.FindProperty("boundsMaxY").floatValue = FLOOR_TOP_Y;
            camSO.FindProperty("defaultOrthoSize").floatValue = 7f;

            WireSORef<VoidEventChannel>(camSO, "onCameraLock", EVT_CAMERA_LOCK);
            WireSORef<VoidEventChannel>(camSO, "onCameraUnlock", EVT_CAMERA_UNLOCK);
            WireSORef<VoidEventChannel>(camSO, "onStunTriggered", EVT_STUN_TRIGGERED);
            WireSORef<VoidEventChannel>(camSO, "onStunRecovered", EVT_STUN_RECOVERED);
            camSO.ApplyModifiedPropertiesWithoutUndo();

            return camGO;
        }

        private static void WireCameraToSpawner(GameObject camGO, GameObject spawnerGO)
        {
            var spawnPoint = spawnerGO.transform.Find("SpawnPoint");
            if (spawnPoint == null || camGO == null) return;

            var camController = camGO.GetComponent<CameraController2D>();
            if (camController == null) return;

            var camSO = new SerializedObject(camController);
            var targetsProp = camSO.FindProperty("targets");
            if (targetsProp != null)
            {
                targetsProp.arraySize = 1;
                targetsProp.GetArrayElementAtIndex(0).objectReferenceValue = spawnPoint;
                camSO.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ── Forest Background ───────────────────────────────────────────────

        private static void CreateForestBackground()
        {
            var environment = new GameObject("Environment");

            CreateArtLayer(environment.transform, $"{ART_FOLDER}/bg_forest_distant.png",
                "BG_Distant", -100, Vector3.zero, ARENA_WIDTH, ARENA_HEIGHT);
            CreateArtLayer(environment.transform, $"{ART_FOLDER}/bg_forest_midground.png",
                "BG_Midground", -90, Vector3.zero, ARENA_WIDTH, ARENA_HEIGHT);
            CreateArtLayer(environment.transform, $"{ART_FOLDER}/bg_forest_foreground.png",
                "BG_Foreground", -80, Vector3.zero, ARENA_WIDTH, ARENA_HEIGHT);

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

            var wallLeftSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART_FOLDER}/wall_left_stone.png");
            if (wallLeftSprite != null)
            {
                float wallWidth = ARENA_WIDTH * 0.04f;
                float halfWall = wallWidth / 2f;
                CreateArtLayer(environment.transform, $"{ART_FOLDER}/wall_left_stone.png",
                    "Wall_Left_Visual", -40,
                    new Vector3(-ARENA_WIDTH / 2f + halfWall, 0f, 0f), wallWidth, ARENA_HEIGHT);
            }

            var wallRightSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART_FOLDER}/wall_right_stone.png");
            if (wallRightSprite != null)
            {
                float wallWidth = ARENA_WIDTH * 0.04f;
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
                sr.color = new Color(0.18f, 0.12f, 0.22f);
                go.transform.localScale = new Vector3(targetWidth, targetHeight, 1f);
            }
        }

        // ── Arena Walls ─────────────────────────────────────────────────────

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

        // ── Character Spawner ───────────────────────────────────────────────

        private static GameObject CreateCharacterSpawner(CharacterRegistry registry)
        {
            var spawnerGO = new GameObject("CharacterSpawner");

            var spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(spawnerGO.transform);
            spawnPoint.transform.localPosition = new Vector3(-6f, FLOOR_MID_Y, 0f);

            var spawner = spawnerGO.AddComponent<CharacterSpawner>();
            var so = new SerializedObject(spawner);
            so.FindProperty("registry").objectReferenceValue = registry;
            so.FindProperty("selectedCharacter").enumValueIndex = (int)CharacterType.Brutor;
            so.FindProperty("deferSpawn").boolValue = true;
            so.FindProperty("spawnPoint").objectReferenceValue = spawnPoint.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            var selectUI = spawnerGO.AddComponent<CharacterSelectUI>();
            var selectSO = new SerializedObject(selectUI);
            selectSO.FindProperty("spawner").objectReferenceValue = spawner;
            selectSO.FindProperty("registry").objectReferenceValue = registry;
            selectSO.ApplyModifiedPropertiesWithoutUndo();

            spawnerGO.AddComponent<CharacterSwitchDebugUI>();
            return spawnerGO;
        }

        // ── Wave Manager (3 waves) ─────────────────────────────────────────

        private static void CreateWaveManager()
        {
            var wmGO = new GameObject("WaveManager");
            var wm = wmGO.AddComponent<WaveManager>();
            var wmSO = new SerializedObject(wm);

            // Spawn points
            var spawnRoot = new GameObject("SpawnPoints");
            spawnRoot.transform.SetParent(wmGO.transform);

            var positions = new[]
            {
                new Vector3(-3f, FLOOR_MID_Y, 0f),
                new Vector3(2f, FLOOR_MID_Y, 0f),
                new Vector3(7f, FLOOR_MID_Y, 0f),
                new Vector3(0f, FLOOR_MID_Y, 0f),  // Center — boss spawn
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

            // Prefabs
            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_PREFAB_PATH);
            var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BOSS_PREFAB_PATH);

            // 3 waves
            var wavesProp = wmSO.FindProperty("waves");
            wavesProp.arraySize = 3;

            // Wave 1: Scout — 3 basic enemies
            {
                var wave = wavesProp.GetArrayElementAtIndex(0);
                wave.FindPropertyRelative("waveName").stringValue = "Scout Wave";
                wave.FindPropertyRelative("isOptional").boolValue = false;
                var groups = wave.FindPropertyRelative("enemyGroups");
                groups.arraySize = 3;
                for (int i = 0; i < 3; i++)
                {
                    var g = groups.GetArrayElementAtIndex(i);
                    g.FindPropertyRelative("enemyPrefab").objectReferenceValue = enemyPrefab;
                    g.FindPropertyRelative("spawnCount").intValue = 1;
                    g.FindPropertyRelative("spawnDelay").floatValue = 0.5f;
                    g.FindPropertyRelative("spawnPointIndex").intValue = i;
                }
            }

            // Wave 2: Assault — 4 basic enemies (2 groups of 2)
            {
                var wave = wavesProp.GetArrayElementAtIndex(1);
                wave.FindPropertyRelative("waveName").stringValue = "Assault Wave";
                wave.FindPropertyRelative("isOptional").boolValue = false;
                var groups = wave.FindPropertyRelative("enemyGroups");
                groups.arraySize = 2;

                var g0 = groups.GetArrayElementAtIndex(0);
                g0.FindPropertyRelative("enemyPrefab").objectReferenceValue = enemyPrefab;
                g0.FindPropertyRelative("spawnCount").intValue = 2;
                g0.FindPropertyRelative("spawnDelay").floatValue = 0.8f;
                g0.FindPropertyRelative("spawnPointIndex").intValue = 0;

                var g1 = groups.GetArrayElementAtIndex(1);
                g1.FindPropertyRelative("enemyPrefab").objectReferenceValue = enemyPrefab;
                g1.FindPropertyRelative("spawnCount").intValue = 2;
                g1.FindPropertyRelative("spawnDelay").floatValue = 0.8f;
                g1.FindPropertyRelative("spawnPointIndex").intValue = 2;
            }

            // Wave 3: Boss — 1 TestBoss at center
            {
                var wave = wavesProp.GetArrayElementAtIndex(2);
                wave.FindPropertyRelative("waveName").stringValue = "Boss Wave";
                wave.FindPropertyRelative("isOptional").boolValue = false;
                var groups = wave.FindPropertyRelative("enemyGroups");
                groups.arraySize = 1;

                var g0 = groups.GetArrayElementAtIndex(0);
                g0.FindPropertyRelative("enemyPrefab").objectReferenceValue = bossPrefab;
                g0.FindPropertyRelative("spawnCount").intValue = 1;
                g0.FindPropertyRelative("spawnDelay").floatValue = 0f;
                g0.FindPropertyRelative("spawnPointIndex").intValue = 3; // Center spawn
            }

            // Timing
            wmSO.FindProperty("waveStartDelay").floatValue = 1.5f;
            wmSO.FindProperty("waveClearDelay").floatValue = 0.8f;

            // Event channels
            WireSORef<IntEventChannel>(wmSO, "onWaveStart", EVT_WAVE_START);
            WireSORef<VoidEventChannel>(wmSO, "onWaveCleared", EVT_WAVE_CLEARED);
            WireSORef<VoidEventChannel>(wmSO, "onAreaComplete", EVT_AREA_COMPLETE);
            WireSORef<VoidEventChannel>(wmSO, "onCameraLock", EVT_CAMERA_LOCK);
            WireSORef<VoidEventChannel>(wmSO, "onCameraUnlock", EVT_CAMERA_UNLOCK);
            WireSORef<VoidEventChannel>(wmSO, "onBoundReached", EVT_BOUND_REACHED);
            wmSO.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[Phase3Demo] WaveManager: 3 waves (Scout 3x, Assault 4x, Boss 1x).");
        }

        // ── Level Bounds ────────────────────────────────────────────────────

        private static void CreateLevelBounds()
        {
            var onBound = AssetDatabase.LoadAssetAtPath<VoidEventChannel>(EVT_BOUND_REACHED);
            var root = new GameObject("LevelBounds");

            // Trigger bound on the right side (player walks right to start)
            CreateBound("LevelBound_Right", root.transform,
                new Vector3(ARENA_WIDTH / 2f - 2f, 0f, 0f),
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

        // ── Player HUD ──────────────────────────────────────────────────────

        private static void CreatePlayerHUD()
        {
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HUD_PREFAB_PATH);
            if (hudPrefab == null)
            {
                Debug.LogWarning("[Phase3Demo] PlayerHUD prefab not found. Run 'Create HUD Assets' first.");
                return;
            }

            PrefabUtility.InstantiatePrefab(hudPrefab);
            Debug.Log("[Phase3Demo] PlayerHUD instantiated.");
        }

        // ── PathSystem ──────────────────────────────────────────────────────

        private static PathSystem CreatePathSystem()
        {
            var go = new GameObject("PathSystem");
            var pathSystem = go.AddComponent<PathSystem>();

            // PathSystem doesn't need event channel wiring for the demo —
            // it gets called directly by PathSelectionUI.ConfirmSelection()
            Debug.Log("[Phase3Demo] PathSystem created.");
            return pathSystem;
        }

        // ── RitualSystem ────────────────────────────────────────────────────

        private static RitualSystem CreateRitualSystem()
        {
            var go = new GameObject("RitualSystem");
            var ritualSystem = go.AddComponent<RitualSystem>();

            // _combatEventsSource left null — ritual effects won't trigger in demo.
            // Full wiring requires a runtime bridge from player's ICombatEvents.
            Debug.Log("[Phase3Demo] RitualSystem created (effects inactive — needs runtime ICombatEvents wiring).");
            return ritualSystem;
        }

        // ── RewardSelectorUI ────────────────────────────────────────────────

        private static void CreateRewardSelectorUI(RitualSystem ritualSystem)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(REWARD_UI_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogWarning("[Phase3Demo] RewardSelectorUI prefab missing.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "RewardSelectorUI";
            var rewardUI = instance.GetComponent<RewardSelectorUI>();
            if (rewardUI == null) return;

            var so = new SerializedObject(rewardUI);

            // Wire event channels (may already be set on prefab, but ensure correct ones)
            WireSORef<VoidEventChannel>(so, "onShowRewardSelector", EVT_SHOW_REWARD);
            WireSORef<RewardSelectedEventChannel>(so, "onRewardSelected", EVT_REWARD_SELECTED);
            WireSORef<RewardConfig>(so, "rewardConfig", REWARD_CONFIG_PATH);

            // Wire ritual pool — all 8 ritual SOs
            var ritualPaths = new[]
            {
                $"{RITUAL_SO_ROOT}/Fire/BurnRitual.asset",
                $"{RITUAL_SO_ROOT}/Fire/BlazingDashRitual.asset",
                $"{RITUAL_SO_ROOT}/Fire/FlameStrikeRitual.asset",
                $"{RITUAL_SO_ROOT}/Fire/EmberShieldRitual.asset",
                $"{RITUAL_SO_ROOT}/Lightning/ChainLightningRitual.asset",
                $"{RITUAL_SO_ROOT}/Lightning/LightningStrikeRitual.asset",
                $"{RITUAL_SO_ROOT}/Lightning/ShockWaveRitual.asset",
                $"{RITUAL_SO_ROOT}/Lightning/StaticFieldRitual.asset",
            };

            var ritualPoolProp = so.FindProperty("ritualPool");
            ritualPoolProp.arraySize = ritualPaths.Length;
            for (int i = 0; i < ritualPaths.Length; i++)
            {
                ritualPoolProp.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<RitualData>(ritualPaths[i]);
            }

            // Wire RitualSystem reference
            so.FindProperty("ritualSystem").objectReferenceValue = ritualSystem;

            // Disable showOnStart — triggered by mediator
            so.FindProperty("showOnStart").boolValue = false;

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Phase3Demo] RewardSelectorUI wired with 8 ritual pool entries.");
        }

        // ── PathSelectionUI ─────────────────────────────────────────────────

        private static void CreatePathSelectionUI(PathSystem pathSystem)
        {
            var go = new GameObject("PathSelectionUI");
            var pathUI = go.AddComponent<PathSelectionUI>();
            var so = new SerializedObject(pathUI);

            // Wire PathSystem
            so.FindProperty("pathSystem").objectReferenceValue = pathSystem;

            // Wire event channel for mediator trigger
            EnsureEventChannel<VoidEventChannel>(EVT_SHOW_PATH);
            WireSORef<VoidEventChannel>(so, "onShowPathSelection", EVT_SHOW_PATH);

            // Wire Brutor's 3 paths (demo defaults to Brutor paths regardless of character)
            var brutorPaths = new[]
            {
                $"{PATH_SO_ROOT}/WardenPath.asset",
                $"{PATH_SO_ROOT}/BulwarkPath.asset",
                $"{PATH_SO_ROOT}/GuardianPath.asset",
            };

            var pathsProp = so.FindProperty("availablePaths");
            pathsProp.arraySize = brutorPaths.Length;
            for (int i = 0; i < brutorPaths.Length; i++)
            {
                pathsProp.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<PathData>(brutorPaths[i]);
            }

            // Disable showOnStart
            so.FindProperty("showOnStart").boolValue = false;

            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Phase3Demo] PathSelectionUI wired with Brutor's 3 paths (Warden/Bulwark/Guardian).");
        }

        // ── Phase 3 Demo Mediator ───────────────────────────────────────────

        private static void CreatePhase3DemoMediator()
        {
            var go = new GameObject("Phase3DemoMediator");
            var mediator = go.AddComponent<Phase3DemoMediator>();
            var so = new SerializedObject(mediator);

            WireSORef<VoidEventChannel>(so, "onWaveCleared", EVT_WAVE_CLEARED);
            WireSORef<VoidEventChannel>(so, "onShowRewardSelector", EVT_SHOW_REWARD);
            WireSORef<VoidEventChannel>(so, "onShowPathSelection", EVT_SHOW_PATH);
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[Phase3Demo] Mediator wired: Wave 1→Ritual, Wave 2→Path, Wave 3→Boss.");
        }

        // ── Health Bar Prefab Wiring ────────────────────────────────────────

        /// <summary>
        /// Bakes the EnemyHealthBar prefab reference into enemy prefab assets
        /// so WaveManager-spawned instances get health bars automatically.
        /// </summary>
        private static void WireHealthBarOnPrefab(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            var healthBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ENEMY_HEALTH_BAR_PATH);
            if (healthBarPrefab == null) return;

            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            var enemyBase = contents.GetComponent<EnemyBase>();
            if (enemyBase != null)
            {
                var so = new SerializedObject(enemyBase);
                var prop = so.FindProperty("healthBarPrefab");
                if (prop != null)
                {
                    prop.objectReferenceValue = healthBarPrefab;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // Also wire playerLayer on EnemyAI for WaveManager-spawned enemies
            var ai = contents.GetComponent<EnemyAI>();
            if (ai != null)
            {
                int playerHurtbox = LayerMask.NameToLayer("PlayerHurtbox");
                if (playerHurtbox >= 0)
                {
                    var aiSO = new SerializedObject(ai);
                    var layerProp = aiSO.FindProperty("playerLayer");
                    if (layerProp != null)
                    {
                        layerProp.intValue = 1 << playerHurtbox;
                        aiSO.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }

            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static void WireSORef<T>(SerializedObject so, string propName, string assetPath) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                var prop = so.FindProperty(propName);
                if (prop != null)
                    prop.objectReferenceValue = asset;
            }
        }

        /// <summary>Creates an SO event channel asset if it doesn't exist yet.</summary>
        private static T EnsureEventChannel<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            PlayerPrefabCreator.EnsureFolderExists(EVENTS_ROOT);
            var channel = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(channel, path);
            AssetDatabase.SaveAssets();
            return channel;
        }
    }
}
