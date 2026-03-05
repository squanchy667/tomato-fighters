using TomatoFighters.Roguelite;
using TomatoFighters.Shared.Events;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.Prefabs
{
    /// <summary>
    /// Creates the RewardSelectorUI prefab with all required SO event channels wired.
    /// Run via menu: <b>TomatoFighters > Create RewardSelectorUI Prefab</b>.
    /// Idempotent — safe to re-run.
    /// </summary>
    public static class RewardSelectorUICreator
    {
        private const string PREFAB_FOLDER = "Assets/Prefabs/UI";
        private const string PREFAB_PATH = PREFAB_FOLDER + "/RewardSelectorUI.prefab";
        private const string SO_FOLDER = "Assets/ScriptableObjects/Events";
        private const string SHOW_EVENT_PATH = SO_FOLDER + "/OnShowRewardSelector.asset";
        private const string SELECTED_EVENT_PATH = SO_FOLDER + "/OnRewardSelected.asset";
        private const string CONFIG_FOLDER = "Assets/ScriptableObjects/Roguelite";
        private const string CONFIG_PATH = CONFIG_FOLDER + "/RewardConfig.asset";

        [MenuItem("TomatoFighters/Create RewardSelectorUI Prefab")]
        public static void Create()
        {
            PlayerPrefabCreator.EnsureFolderExists(PREFAB_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(SO_FOLDER);
            PlayerPrefabCreator.EnsureFolderExists(CONFIG_FOLDER);

            // Create or load SO event channels
            var showEvent = CreateOrLoadAsset<VoidEventChannel>(SHOW_EVENT_PATH);
            var selectedEvent = CreateOrLoadAsset<RewardSelectedEventChannel>(SELECTED_EVENT_PATH);
            var rewardConfig = CreateOrLoadRewardConfig();

            // Create or update prefab
            bool isNew = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) == null;
            var prefab = SetupPrefab(showEvent, selectedEvent, rewardConfig);

            string verb = isNew ? "Created" : "Updated";
            Debug.Log($"[RewardSelectorUICreator] {verb} RewardSelectorUI prefab at {PREFAB_PATH}");
            Debug.Log($"[RewardSelectorUICreator] SO events: {SHOW_EVENT_PATH}, {SELECTED_EVENT_PATH}");
            Debug.Log($"[RewardSelectorUICreator] Config: {CONFIG_PATH}");

            Selection.activeObject = prefab;
        }

        private static GameObject SetupPrefab(
            VoidEventChannel showEvent,
            RewardSelectedEventChannel selectedEvent,
            RewardConfig rewardConfig)
        {
            bool isExisting = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null;
            GameObject root;

            if (isExisting)
                root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            else
                root = new GameObject("RewardSelectorUI");

            // RewardSelectorUI component
            var ui = EnsureComponent<RewardSelectorUI>(root);
            var so = new SerializedObject(ui);

            var showProp = so.FindProperty("onShowRewardSelector");
            if (showProp != null)
                showProp.objectReferenceValue = showEvent;

            var selectedProp = so.FindProperty("onRewardSelected");
            if (selectedProp != null)
                selectedProp.objectReferenceValue = selectedEvent;

            var configProp = so.FindProperty("rewardConfig");
            if (configProp != null)
                configProp.objectReferenceValue = rewardConfig;

            so.ApplyModifiedPropertiesWithoutUndo();

            // Save prefab
            GameObject savedPrefab;
            if (isExisting)
            {
                savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
                Object.DestroyImmediate(root);
            }

            return savedPrefab;
        }

        private static RewardConfig CreateOrLoadRewardConfig()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RewardConfig>(CONFIG_PATH);
            if (existing != null) return existing;

            var config = ScriptableObject.CreateInstance<RewardConfig>();
            AssetDatabase.CreateAsset(config, CONFIG_PATH);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RewardSelectorUICreator] Created RewardConfig at {CONFIG_PATH}");
            return config;
        }

        private static T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[RewardSelectorUICreator] Created {typeof(T).Name} at {path}");
            return asset;
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }
    }
}
