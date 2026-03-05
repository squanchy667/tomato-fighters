using TomatoFighters.Editor.Prefabs;
using TomatoFighters.Paths;
using TomatoFighters.Roguelite;
using TomatoFighters.Shared.Data;
using TomatoFighters.Shared.Enums;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TomatoFighters.Editor.Scenes
{
    /// <summary>
    /// Creates a minimal test scene for verifying <see cref="PathSelectionUI"/>.
    /// Sets up a camera, <see cref="PathSystem"/>, and <see cref="PathSelectionUI"/>
    /// wired to Brutor's 3 path assets. Auto-shows the Main selection on play.
    /// Run via: <b>TomatoFighters > Scenes > Create Path Selection Test Scene</b>.
    /// </summary>
    public static class PathSelectionTestSceneCreator
    {
        private const string SCENE_FOLDER = "Assets/Scenes";
        private const string SCENE_PATH = SCENE_FOLDER + "/PathSelectionTest.unity";

        private static readonly string[] BRUTOR_PATH_ASSETS =
        {
            "Assets/ScriptableObjects/Paths/Brutor/WardenPath.asset",
            "Assets/ScriptableObjects/Paths/Brutor/BulwarkPath.asset",
            "Assets/ScriptableObjects/Paths/Brutor/GuardianPath.asset",
        };

        [MenuItem("TomatoFighters/Scenes/Create Path Selection Test Scene")]
        public static void CreateTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupCamera();
            CreatePathSelectionSystem();

            PlayerPrefabCreator.EnsureFolderExists(SCENE_FOLDER);
            EditorSceneManager.SaveScene(scene, SCENE_PATH);

            Debug.Log($"[PathSelectionTest] Scene created at {SCENE_PATH}");
            Debug.Log("[PathSelectionTest] Press Play to see the path selection UI. Press 1-3 to select, Enter or click CONFIRM.");
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

        private static void CreatePathSelectionSystem()
        {
            var go = new GameObject("PathSelectionSystem");

            // PathSystem
            var pathSystem = go.AddComponent<PathSystem>();
            var pathSystemSO = new SerializedObject(pathSystem);
            pathSystemSO.FindProperty("character").enumValueIndex = (int)CharacterType.Brutor;
            pathSystemSO.ApplyModifiedPropertiesWithoutUndo();

            // PathSelectionUI
            var ui = go.AddComponent<PathSelectionUI>();
            var uiSO = new SerializedObject(ui);
            uiSO.FindProperty("pathSystem").objectReferenceValue = pathSystem;
            uiSO.FindProperty("showOnStart").boolValue = true;

            // Wire available paths
            var pathsProp = uiSO.FindProperty("availablePaths");
            pathsProp.arraySize = BRUTOR_PATH_ASSETS.Length;
            for (int i = 0; i < BRUTOR_PATH_ASSETS.Length; i++)
            {
                var pathData = AssetDatabase.LoadAssetAtPath<PathData>(BRUTOR_PATH_ASSETS[i]);
                if (pathData == null)
                    Debug.LogWarning($"[PathSelectionTest] PathData not found at {BRUTOR_PATH_ASSETS[i]}. Run TomatoFighters > Create All Path Assets first.");
                pathsProp.GetArrayElementAtIndex(i).objectReferenceValue = pathData;
            }

            uiSO.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[PathSelectionTest] PathSystem + PathSelectionUI created with Brutor paths.");
        }
    }
}
