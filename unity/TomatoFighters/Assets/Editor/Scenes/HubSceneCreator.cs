using TomatoFighters.Roguelite;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// Programmatically creates the Hub scene with all required components wired.
    ///
    /// <para>Run via <b>TomatoFighters/Scenes/Create Hub Scene</b> from the Unity menu bar.
    /// The scene is written to <c>Assets/Scenes/Hub.unity</c> and opened for immediate use.</para>
    ///
    /// <para>Re-running overwrites the existing Hub scene — safe to re-run after changes
    /// to the creator script.</para>
    ///
    /// <para><b>Components created:</b>
    /// <list type="bullet">
    ///   <item><c>Hub Manager</c> — <see cref="HubManager"/> with all SerializeField references wired.</item>
    ///   <item><c>Save System</c> — <see cref="SaveSystem"/>.</item>
    ///   <item><c>Meta Progression</c> — <see cref="MetaProgression"/> with <see cref="CurrencyManager"/> wired.</item>
    ///   <item><c>Currency Manager</c> — <see cref="CurrencyManager"/>.</item>
    ///   <item><c>Inspiration System</c> — <see cref="InspirationSystem"/>.</item>
    /// </list></para>
    /// </summary>
    public static class HubSceneCreator
    {
        private const string SCENE_PATH = "Assets/Scenes/Hub.unity";

        [MenuItem("TomatoFighters/Scenes/Create Hub Scene")]
        public static void CreateHubScene()
        {
            // Create a new empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── Instantiate all GameObjects ───────────────────────────────────

            var saveSystemGo       = new GameObject("Save System");
            var currencyManagerGo  = new GameObject("Currency Manager");
            var metaProgressionGo  = new GameObject("Meta Progression");
            var inspirationSystemGo = new GameObject("Inspiration System");
            var hubManagerGo       = new GameObject("Hub Manager");

            // ── Add components ────────────────────────────────────────────────

            var saveSystem        = saveSystemGo.AddComponent<SaveSystem>();
            var currencyManager   = currencyManagerGo.AddComponent<CurrencyManager>();
            var metaProgression   = metaProgressionGo.AddComponent<MetaProgression>();
            var inspirationSystem = inspirationSystemGo.AddComponent<InspirationSystem>();
            var hubManager        = hubManagerGo.AddComponent<HubManager>();

            // ── Wire SerializeField references on MetaProgression ─────────────
            // MetaProgression needs CurrencyManager
            var metaSo = new SerializedObject(metaProgression);
            var currencyProp = metaSo.FindProperty("_currencyManager");
            if (currencyProp != null)
            {
                currencyProp.objectReferenceValue = currencyManager;
                metaSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // ── Wire SerializeField references on InspirationSystem ───────────
            var inspirationSo = new SerializedObject(inspirationSystem);
            var inspirationCurrencyProp = inspirationSo.FindProperty("_currencyManager");
            if (inspirationCurrencyProp != null)
            {
                inspirationCurrencyProp.objectReferenceValue = currencyManager;
                inspirationSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // ── Wire SerializeField references on HubManager ──────────────────
            var hubSo = new SerializedObject(hubManager);

            SetRef(hubSo, "_saveSystem",        saveSystem);
            SetRef(hubSo, "_metaProgression",   metaProgression);
            SetRef(hubSo, "_currencyManager",   currencyManager);
            SetRef(hubSo, "_inspirationSystem", inspirationSystem);

            hubSo.ApplyModifiedPropertiesWithoutUndo();

            // ── Save the scene ────────────────────────────────────────────────

            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            AssetDatabase.Refresh();

            Debug.Log($"[HubSceneCreator] Hub scene created at: {SCENE_PATH}");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void SetRef(SerializedObject so, string propertyName, Object value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null)
                prop.objectReferenceValue = value;
            else
                Debug.LogWarning($"[HubSceneCreator] Property '{propertyName}' not found on {so.targetObject.GetType().Name}.");
        }
    }
}
