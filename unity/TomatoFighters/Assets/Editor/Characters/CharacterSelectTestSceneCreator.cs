using TomatoFighters.Characters;
using TomatoFighters.Editor.Prefabs;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TomatoFighters.Editor.Characters
{
    /// <summary>
    /// Creates a test scene with a <see cref="CharacterSpawner"/> for quick character switching.
    /// Populates a <see cref="CharacterRegistry"/> SO with all available character prefabs.
    /// Run via menu: <b>TomatoFighters > Characters > Create Character Select Test Scene</b>.
    /// </summary>
    public static class CharacterSelectTestSceneCreator
    {
        private const string SCENE_FOLDER = "Assets/Scenes";
        private const string SCENE_PATH = SCENE_FOLDER + "/CharacterSelectTest.unity";
        private const string REGISTRY_FOLDER = "Assets/ScriptableObjects/Characters";
        private const string REGISTRY_PATH = REGISTRY_FOLDER + "/CharacterRegistry.asset";
        private const string DUMMY_PREFAB_PATH = "Assets/Prefabs/Enemies/TestDummy.prefab";
        private const string INPUT_ACTIONS_PATH = "Assets/InputSystem_Actions.inputactions";

        private const float ARENA_WIDTH = 20f;
        private const float ARENA_HEIGHT = 10f;
        private const float WALL_THICKNESS = 1f;

        private static readonly (CharacterType type, string prefabPath, string statsPath)[] CHARACTER_DEFS =
        {
            (CharacterType.Brutor,  "Assets/Prefabs/Player/Player.prefab",  "Assets/ScriptableObjects/Characters/BrutorStats.asset"),
            (CharacterType.Slasher, "Assets/Prefabs/Player/Slasher.prefab", "Assets/ScriptableObjects/Characters/SlasherStats.asset"),
            (CharacterType.Mystica, "Assets/Prefabs/Player/Mystica.prefab", "Assets/ScriptableObjects/Characters/MysticaStats.asset"),
            (CharacterType.Viper,   "Assets/Prefabs/Player/Player.prefab",  "Assets/ScriptableObjects/Characters/ViperStats.asset"),
        };

        [MenuItem("TomatoFighters/Characters/Create Character Select Test Scene")]
        public static void CreateTestScene()
        {
            var registry = CreateOrUpdateRegistry();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupCamera();
            CreateArenaBackground();
            CreateArenaWalls();
            CreateSpawner(registry);
            CreateTestDummy();
            CreateControlsHint();

            PlayerPrefabCreator.EnsureFolderExists(SCENE_FOLDER);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);

            Debug.Log($"[CharacterSelectTest] Scene created at {SCENE_PATH}");
            Debug.Log("[CharacterSelectTest] Press 1-4 to switch characters: 1=Brutor, 2=Slasher, 3=Mystica, 4=Viper");
        }

        /// <summary>
        /// Creates or updates the CharacterRegistry SO with all known character prefabs/stats.
        /// </summary>
        [MenuItem("TomatoFighters/Characters/Create Character Registry")]
        public static CharacterRegistry CreateOrUpdateRegistry()
        {
            PlayerPrefabCreator.EnsureFolderExists(REGISTRY_FOLDER);

            var existing = AssetDatabase.LoadAssetAtPath<CharacterRegistry>(REGISTRY_PATH);
            if (existing == null)
            {
                existing = ScriptableObject.CreateInstance<CharacterRegistry>();
                AssetDatabase.CreateAsset(existing, REGISTRY_PATH);
            }

            var entries = new CharacterEntry[CHARACTER_DEFS.Length];
            for (int i = 0; i < CHARACTER_DEFS.Length; i++)
            {
                var (type, prefabPath, statsPath) = CHARACTER_DEFS[i];
                entries[i] = new CharacterEntry
                {
                    characterType = type,
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath),
                    baseStats = AssetDatabase.LoadAssetAtPath<CharacterBaseStats>(statsPath)
                };

                if (entries[i].prefab == null)
                    Debug.LogWarning($"[CharacterSelectTest] Prefab not found for {type} at {prefabPath}.");
                if (entries[i].baseStats == null)
                    Debug.LogWarning($"[CharacterSelectTest] Stats not found for {type} at {statsPath}.");
            }

            existing.characters = entries;
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();

            Debug.Log("[CharacterSelectTest] CharacterRegistry updated with all character entries.");
            return existing;
        }

        private static void CreateSpawner(CharacterRegistry registry)
        {
            var spawnerGO = new GameObject("CharacterSpawner");
            spawnerGO.transform.position = Vector3.zero;

            var spawner = spawnerGO.AddComponent<CharacterSpawner>();
            var so = new SerializedObject(spawner);
            so.FindProperty("registry").objectReferenceValue = registry;
            so.FindProperty("selectedCharacter").enumValueIndex = (int)CharacterType.Slasher;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Add spawn point child
            var spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.SetParent(spawnerGO.transform);
            spawnPoint.transform.localPosition = new Vector3(-3f, 0f, 0f);

            var spawnPointSO = new SerializedObject(spawner);
            spawnPointSO.FindProperty("spawnPoint").objectReferenceValue = spawnPoint.transform;
            spawnPointSO.ApplyModifiedPropertiesWithoutUndo();

            // Add debug character switcher for runtime
            spawnerGO.AddComponent<CharacterSwitchDebugUI>();

            Debug.Log("[CharacterSelectTest] CharacterSpawner created with SpawnPoint at (-3, 0).");
        }

        private static void CreateTestDummy()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DUMMY_PREFAB_PATH);
            if (prefab == null)
            {
                Debug.LogWarning("[CharacterSelectTest] TestDummy prefab not found. Skipping.");
                return;
            }

            var dummy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            dummy.transform.position = new Vector3(3f, 0f, 0f);
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
            sr.sprite = CreateWhiteSprite();
            sr.color = new Color(0.25f, 0.3f, 0.2f);
            ground.transform.localScale = new Vector3(ARENA_WIDTH, ARENA_HEIGHT, 1f);
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

        private static void CreateWall(string name, Transform parent, Vector3 pos, Vector2 size)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            var col = wall.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        private static void CreateControlsHint()
        {
            var textGO = new GameObject("ControlsHint");
            var tm = textGO.AddComponent<TextMesh>();
            tm.text = "1=Brutor 2=Slasher 3=Mystica 4=Viper | WASD:Move Space:Jump Shift:Dash LMB:Light C:Heavy";
            tm.fontSize = 24;
            tm.characterSize = 0.12f;
            tm.anchor = TextAnchor.UpperCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 1f, 1f, 0.5f);
            textGO.transform.position = new Vector3(0f, ARENA_HEIGHT / 2f - 0.5f, 0f);
        }

        private static Sprite CreateWhiteSprite()
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
