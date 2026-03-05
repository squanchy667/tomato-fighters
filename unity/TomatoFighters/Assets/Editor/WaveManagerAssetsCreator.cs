using TomatoFighters.Shared.Events;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// Creates the 6 SO event channel assets used by WaveManager and LevelBound.
    /// Run via <b>TomatoFighters → Create Wave Event Assets</b>.
    /// Re-running is safe — existing assets are preserved.
    /// </summary>
    public static class WaveManagerAssetsCreator
    {
        private const string ROOT = "Assets/ScriptableObjects/Events";

        [MenuItem("TomatoFighters/Create Wave Event Assets")]
        public static void CreateAllWaveEventAssets()
        {
            EnsureFolder("Assets/ScriptableObjects");
            EnsureFolder(ROOT);

            // Int event channels
            CreateAsset<IntEventChannel>("OnWaveStart");

            // Void event channels
            CreateAsset<VoidEventChannel>("OnWaveCleared");
            CreateAsset<VoidEventChannel>("OnAreaComplete");
            CreateAsset<VoidEventChannel>("OnCameraLock");
            CreateAsset<VoidEventChannel>("OnCameraUnlock");
            CreateAsset<VoidEventChannel>("OnBoundReached");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[T010] All 6 wave event channel assets created at Assets/ScriptableObjects/Events/");
        }

        private static void CreateAsset<T>(string fileName) where T : ScriptableObject
        {
            string path = $"{ROOT}/{fileName}.asset";

            // Don't overwrite existing — these are config-free SO assets
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                Debug.Log($"[T010] Already exists: {path}");
                return;
            }

            var instance = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(instance, path);
            Debug.Log($"[T010] Created: {path}");
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                int lastSlash = path.LastIndexOf('/');
                string parent = path[..lastSlash];
                string folderName = path[(lastSlash + 1)..];
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
