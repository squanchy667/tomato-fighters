using TomatoFighters.Characters;
using TomatoFighters.Combat;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.Prefabs
{
    /// <summary>
    /// Editor utility that creates a fully-wired Player prefab and default MovementConfig.
    /// Run via menu: TomatoFighters > Create Player Prefab.
    /// </summary>
    public static class PlayerPrefabCreator
    {
        private const string PREFAB_FOLDER = "Assets/Prefabs/Player";
        private const string CONFIG_FOLDER = "Assets/ScriptableObjects/MovementConfigs";
        private const string PREFAB_PATH = PREFAB_FOLDER + "/Player.prefab";
        private const string CONFIG_PATH = CONFIG_FOLDER + "/Brutor_MovementConfig.asset";

        [MenuItem("TomatoFighters/Create Player Prefab")]
        public static void CreatePlayerPrefab()
        {
            EnsureFolderExists(PREFAB_FOLDER);
            EnsureFolderExists(CONFIG_FOLDER);

            var config = CreateOrLoadMovementConfig();
            var prefab = BuildPrefab(config);

            Debug.Log($"[PlayerPrefabCreator] Player prefab created at {PREFAB_PATH}");
            Debug.Log($"[PlayerPrefabCreator] MovementConfig created at {CONFIG_PATH}");

            Selection.activeObject = prefab;
        }

        private static MovementConfig CreateOrLoadMovementConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MovementConfig>(CONFIG_PATH);
            if (existing != null)
            {
                Debug.Log("[PlayerPrefabCreator] Using existing Brutor_MovementConfig.");
                return existing;
            }

            var config = ScriptableObject.CreateInstance<MovementConfig>();

            // Brutor defaults: slowest character (SPD 0.7), tank archetype
            config.moveSpeed = 5.6f;          // 8 base * 0.7 SPD
            config.depthSpeed = 3.5f;          // slower depth movement for heavy feel
            config.groundAcceleration = 60f;
            config.airAcceleration = 30f;
            config.jumpForce = 12f;            // lower jump for heavy feel
            config.jumpGravity = 35f;          // slower fall for weighty arc
            config.coyoteTime = 0.1f;
            config.jumpBufferTime = 0.12f;
            config.dashSpeed = 16f;            // 20 base * ~0.8 (tanky feel)
            config.dashDuration = 0.18f;       // slightly longer dash
            config.dashCooldown = 0.7f;
            config.dashHasIFrames = true;

            AssetDatabase.CreateAsset(config, CONFIG_PATH);
            AssetDatabase.SaveAssets();

            return config;
        }

        private static GameObject BuildPrefab(MovementConfig config)
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

            // Build the hierarchy
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
            sr.color = new Color(0.85f, 0.2f, 0.2f); // tomato red

            // -- Shadow child (stays at feet) --
            var shadowChild = new GameObject("Shadow");
            shadowChild.transform.SetParent(root.transform);
            shadowChild.transform.localPosition = Vector3.zero;
            var shadowSR = shadowChild.AddComponent<SpriteRenderer>();
            shadowSR.color = new Color(0f, 0f, 0f, 0.3f);
            shadowSR.sortingOrder = -1;

            // -- CharacterMotor --
            var motor = root.AddComponent<CharacterMotor>();

            // Wire serialized fields via SerializedObject
            var motorSO = new SerializedObject(motor);
            motorSO.FindProperty("config").objectReferenceValue = config;
            motorSO.FindProperty("characterType").enumValueIndex = (int)CharacterType.Brutor;
            motorSO.FindProperty("spriteTransform").objectReferenceValue = spriteChild.transform;
            motorSO.ApplyModifiedPropertiesWithoutUndo();

            // -- CharacterInputHandler --
            var inputHandler = root.AddComponent<CharacterInputHandler>();
            var inputSO = new SerializedObject(inputHandler);
            inputSO.FindProperty("motor").objectReferenceValue = motor;
            inputSO.ApplyModifiedPropertiesWithoutUndo();

            // Save as prefab
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);

            return prefab;
        }

        private static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parts = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
