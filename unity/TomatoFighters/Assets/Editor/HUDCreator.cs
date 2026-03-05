using TomatoFighters.Shared.Events;
using TomatoFighters.World.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TomatoFighters.Editor
{
    /// <summary>
    /// Creator Script for all HUD-related prefabs and ScriptableObject assets.
    /// Generates: PlayerHUD Canvas prefab, EnemyHealthBar prefab, and SO event channel assets.
    /// Safe to re-run — loads existing assets and updates in place.
    /// </summary>
    public static class HUDCreator
    {
        private const string SO_PATH = "Assets/ScriptableObjects/Events";
        private const string PREFAB_PATH = "Assets/Prefabs/UI";

        // ── Menu Entry ──────────────────────────────────────────────────────

        [MenuItem("TomatoFighters/Create HUD Assets")]
        public static void CreateAll()
        {
            EnsureDirectories();

            var healthChanged = CreateOrLoadSO<FloatEventChannel>(SO_PATH, "OnPlayerHealthChanged");
            var manaChanged = CreateOrLoadSO<FloatEventChannel>(SO_PATH, "OnPlayerManaChanged");
            var comboHit = CreateOrLoadSO<IntEventChannel>(SO_PATH, "OnComboHitConfirmed");

            CreatePlayerHUDPrefab(healthChanged, manaChanged, comboHit);
            CreateEnemyHealthBarPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[HUDCreator] All HUD assets created/updated.");
        }

        // ── Player HUD Prefab ───────────────────────────────────────────────

        private static void CreatePlayerHUDPrefab(
            FloatEventChannel healthChanged,
            FloatEventChannel manaChanged,
            IntEventChannel comboHit)
        {
            string prefabPath = $"{PREFAB_PATH}/PlayerHUD.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            var root = existing != null
                ? PrefabUtility.InstantiatePrefab(existing) as GameObject
                : new GameObject("PlayerHUD");

            // Canvas setup
            var canvas = GetOrAdd<Canvas>(root);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = GetOrAdd<CanvasScaler>(root);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            GetOrAdd<GraphicRaycaster>(root);

            // HUDManager
            var hudManager = GetOrAdd<HUDManager>(root);

            // ── Health Bar (top-left) ───────────────────────────────────────
            var healthBarGO = FindOrCreateChild(root, "HealthBar");
            var healthBarUI = GetOrAdd<HealthBarUI>(healthBarGO);

            var healthRect = GetOrAdd<RectTransform>(healthBarGO);
            SetAnchor(healthRect, new Vector2(0, 1), new Vector2(0, 1));
            healthRect.anchoredPosition = new Vector2(20, -20);
            healthRect.sizeDelta = new Vector2(300, 30);

            // Health background
            var healthBG = FindOrCreateChild(healthBarGO, "Background");
            var healthBGImg = GetOrAdd<Image>(healthBG);
            healthBGImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            StretchFill(healthBG);

            // Health fill
            var healthFillGO = FindOrCreateChild(healthBarGO, "Fill");
            var healthFillImg = GetOrAdd<Image>(healthFillGO);
            healthFillImg.color = Color.green;
            healthFillImg.type = Image.Type.Filled;
            healthFillImg.fillMethod = Image.FillMethod.Horizontal;
            healthFillImg.fillAmount = 1f;
            StretchFill(healthFillGO);

            // Health damage flash
            var healthFlashGO = FindOrCreateChild(healthBarGO, "DamageFlash");
            var healthFlashImg = GetOrAdd<Image>(healthFlashGO);
            healthFlashImg.color = new Color(1f, 1f, 1f, 0f);
            StretchFill(healthFlashGO);

            // Wire HealthBarUI fields via SerializedObject
            WireHealthBarUI(healthBarUI, healthFillImg, healthFlashImg, healthChanged);

            // ── Mana Bar (below health) ─────────────────────────────────────
            var manaBarGO = FindOrCreateChild(root, "ManaBar");
            var manaBarUI = GetOrAdd<ManaBarUI>(manaBarGO);

            var manaRect = GetOrAdd<RectTransform>(manaBarGO);
            SetAnchor(manaRect, new Vector2(0, 1), new Vector2(0, 1));
            manaRect.anchoredPosition = new Vector2(20, -55);
            manaRect.sizeDelta = new Vector2(250, 20);

            // Mana background
            var manaBG = FindOrCreateChild(manaBarGO, "Background");
            var manaBGImg = GetOrAdd<Image>(manaBG);
            manaBGImg.color = new Color(0.1f, 0.1f, 0.2f, 0.9f);
            StretchFill(manaBG);

            // Mana fill
            var manaFillGO = FindOrCreateChild(manaBarGO, "Fill");
            var manaFillImg = GetOrAdd<Image>(manaFillGO);
            manaFillImg.color = new Color(0.2f, 0.5f, 1f);
            manaFillImg.type = Image.Type.Filled;
            manaFillImg.fillMethod = Image.FillMethod.Horizontal;
            manaFillImg.fillAmount = 1f;
            StretchFill(manaFillGO);

            WireManaBarUI(manaBarUI, manaFillImg, manaChanged);

            // ── Combo Counter (right side) ──────────────────────────────────
            var comboGO = FindOrCreateChild(root, "ComboCounter");
            var comboUI = GetOrAdd<ComboCounterUI>(comboGO);

            var comboRect = GetOrAdd<RectTransform>(comboGO);
            SetAnchor(comboRect, new Vector2(1, 0.5f), new Vector2(1, 0.5f));
            comboRect.anchoredPosition = new Vector2(-80, 0);
            comboRect.sizeDelta = new Vector2(150, 80);

            // Combo number text
            var comboTextGO = FindOrCreateChild(comboGO, "CountText");
            var comboText = GetOrAdd<Text>(comboTextGO);
            comboText.text = "0";
            comboText.fontSize = 48;
            comboText.fontStyle = FontStyle.Bold;
            comboText.alignment = TextAnchor.MiddleCenter;
            comboText.color = new Color(1f, 0.9f, 0.2f);
            StretchFill(comboTextGO);

            // Combo label
            var comboLabelGO = FindOrCreateChild(comboGO, "Label");
            var comboLabelRT = GetOrAdd<RectTransform>(comboLabelGO);
            SetAnchor(comboLabelRT, new Vector2(0, 0), new Vector2(1, 0));
            comboLabelRT.anchoredPosition = new Vector2(0, -15);
            comboLabelRT.sizeDelta = new Vector2(0, 20);
            var comboLabel = GetOrAdd<Text>(comboLabelGO);
            comboLabel.text = "HITS";
            comboLabel.fontSize = 16;
            comboLabel.alignment = TextAnchor.MiddleCenter;
            comboLabel.color = new Color(1f, 1f, 1f, 0.7f);

            WireComboCounterUI(comboUI, comboText, comboLabel, comboHit);

            // ── Path Indicator (top-right) ──────────────────────────────────
            var pathGO = FindOrCreateChild(root, "PathIndicator");
            var pathUI = GetOrAdd<PathIndicatorUI>(pathGO);

            var pathRect = GetOrAdd<RectTransform>(pathGO);
            SetAnchor(pathRect, new Vector2(1, 1), new Vector2(1, 1));
            pathRect.anchoredPosition = new Vector2(-20, -20);
            pathRect.sizeDelta = new Vector2(200, 60);

            // Main path text
            var mainPathGO = FindOrCreateChild(pathGO, "MainPath");
            var mainPathRT = GetOrAdd<RectTransform>(mainPathGO);
            SetAnchor(mainPathRT, new Vector2(0, 0.5f), new Vector2(1, 1));
            mainPathRT.sizeDelta = Vector2.zero;
            mainPathRT.anchoredPosition = Vector2.zero;
            var mainPathText = GetOrAdd<Text>(mainPathGO);
            mainPathText.text = "---";
            mainPathText.fontSize = 18;
            mainPathText.alignment = TextAnchor.MiddleRight;
            mainPathText.color = Color.white;

            // Main tier text
            var mainTierGO = FindOrCreateChild(pathGO, "MainTier");
            var mainTierRT = GetOrAdd<RectTransform>(mainTierGO);
            SetAnchor(mainTierRT, new Vector2(0, 0.5f), new Vector2(0, 1));
            mainTierRT.anchoredPosition = new Vector2(5, 0);
            mainTierRT.sizeDelta = new Vector2(30, 0);
            var mainTierText = GetOrAdd<Text>(mainTierGO);
            mainTierText.text = "";
            mainTierText.fontSize = 14;
            mainTierText.alignment = TextAnchor.MiddleLeft;
            mainTierText.color = new Color(1f, 0.8f, 0.2f);

            // Secondary path text
            var secPathGO = FindOrCreateChild(pathGO, "SecondaryPath");
            var secPathRT = GetOrAdd<RectTransform>(secPathGO);
            SetAnchor(secPathRT, new Vector2(0, 0), new Vector2(1, 0.5f));
            secPathRT.sizeDelta = Vector2.zero;
            secPathRT.anchoredPosition = Vector2.zero;
            var secPathText = GetOrAdd<Text>(secPathGO);
            secPathText.text = "---";
            secPathText.fontSize = 14;
            secPathText.alignment = TextAnchor.MiddleRight;
            secPathText.color = new Color(0.7f, 0.7f, 0.7f);

            // Secondary tier text
            var secTierGO = FindOrCreateChild(pathGO, "SecondaryTier");
            var secTierRT = GetOrAdd<RectTransform>(secTierGO);
            SetAnchor(secTierRT, new Vector2(0, 0), new Vector2(0, 0.5f));
            secTierRT.anchoredPosition = new Vector2(5, 0);
            secTierRT.sizeDelta = new Vector2(30, 0);
            var secTierText = GetOrAdd<Text>(secTierGO);
            secTierText.text = "";
            secTierText.fontSize = 12;
            secTierText.alignment = TextAnchor.MiddleLeft;
            secTierText.color = new Color(0.7f, 0.7f, 0.7f);

            WirePathIndicatorUI(pathUI, mainPathText, secPathText, mainTierText, secTierText);

            // Wire HUDManager
            WireHUDManager(hudManager, healthBarUI, manaBarUI, comboUI, pathUI);

            // Save prefab
            if (existing != null)
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
                Object.DestroyImmediate(root);
            }

            Debug.Log($"[HUDCreator] PlayerHUD prefab saved to {prefabPath}");
        }

        // ── Enemy Health Bar Prefab ─────────────────────────────────────────

        private static void CreateEnemyHealthBarPrefab()
        {
            string prefabPath = $"{PREFAB_PATH}/EnemyHealthBar.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            var root = existing != null
                ? PrefabUtility.InstantiatePrefab(existing) as GameObject
                : new GameObject("EnemyHealthBar");

            // World-space Canvas
            var canvas = GetOrAdd<Canvas>(root);
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 90;

            var rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1f, 0.25f);
            rt.localScale = Vector3.one;

            var barUI = GetOrAdd<EnemyHealthBarUI>(root);

            // Health background
            var healthBG = FindOrCreateChild(root, "HealthBG");
            var healthBGImg = GetOrAdd<Image>(healthBG);
            healthBGImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            var healthBGRT = healthBG.GetComponent<RectTransform>();
            healthBGRT.anchorMin = new Vector2(0, 0.4f);
            healthBGRT.anchorMax = Vector2.one;
            healthBGRT.sizeDelta = Vector2.zero;
            healthBGRT.anchoredPosition = Vector2.zero;

            // Health fill
            var healthFillGO = FindOrCreateChild(root, "HealthFill");
            var healthFillImg = GetOrAdd<Image>(healthFillGO);
            healthFillImg.color = Color.green;
            healthFillImg.type = Image.Type.Filled;
            healthFillImg.fillMethod = Image.FillMethod.Horizontal;
            healthFillImg.fillAmount = 1f;
            var healthFillRT = healthFillGO.GetComponent<RectTransform>();
            healthFillRT.anchorMin = new Vector2(0, 0.4f);
            healthFillRT.anchorMax = Vector2.one;
            healthFillRT.sizeDelta = Vector2.zero;
            healthFillRT.anchoredPosition = Vector2.zero;

            // Pressure background
            var pressureBG = FindOrCreateChild(root, "PressureBG");
            var pressureBGImg = GetOrAdd<Image>(pressureBG);
            pressureBGImg.color = new Color(0.1f, 0.1f, 0.2f, 0.6f);
            var pressureBGRT = pressureBG.GetComponent<RectTransform>();
            pressureBGRT.anchorMin = Vector2.zero;
            pressureBGRT.anchorMax = new Vector2(1, 0.35f);
            pressureBGRT.sizeDelta = Vector2.zero;
            pressureBGRT.anchoredPosition = Vector2.zero;

            // Pressure fill
            var pressureFillGO = FindOrCreateChild(root, "PressureFill");
            var pressureFillImg = GetOrAdd<Image>(pressureFillGO);
            pressureFillImg.color = new Color(0.3f, 0.5f, 1f);
            pressureFillImg.type = Image.Type.Filled;
            pressureFillImg.fillMethod = Image.FillMethod.Horizontal;
            pressureFillImg.fillAmount = 0f;
            var pressureFillRT = pressureFillGO.GetComponent<RectTransform>();
            pressureFillRT.anchorMin = Vector2.zero;
            pressureFillRT.anchorMax = new Vector2(1, 0.35f);
            pressureFillRT.sizeDelta = Vector2.zero;
            pressureFillRT.anchoredPosition = Vector2.zero;

            // Wire EnemyHealthBarUI
            WireEnemyHealthBarUI(barUI, healthFillImg, pressureFillImg);

            // Save prefab
            if (existing != null)
            {
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.AutomatedAction);
                Object.DestroyImmediate(root);
            }

            Debug.Log($"[HUDCreator] EnemyHealthBar prefab saved to {prefabPath}");
        }

        // ── SO Wiring Helpers ───────────────────────────────────────────────

        private static void WireHealthBarUI(HealthBarUI target, Image fill, Image flash, FloatEventChannel channel)
        {
            var so = new SerializedObject(target);
            so.FindProperty("fillImage").objectReferenceValue = fill;
            so.FindProperty("damageFlashImage").objectReferenceValue = flash;
            so.FindProperty("onHealthChanged").objectReferenceValue = channel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireManaBarUI(ManaBarUI target, Image fill, FloatEventChannel channel)
        {
            var so = new SerializedObject(target);
            so.FindProperty("fillImage").objectReferenceValue = fill;
            so.FindProperty("onManaChanged").objectReferenceValue = channel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireComboCounterUI(ComboCounterUI target, Text countText, Text label, IntEventChannel channel)
        {
            var so = new SerializedObject(target);
            so.FindProperty("comboText").objectReferenceValue = countText;
            so.FindProperty("labelText").objectReferenceValue = label;
            so.FindProperty("onComboHitConfirmed").objectReferenceValue = channel;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WirePathIndicatorUI(PathIndicatorUI target, Text main, Text secondary, Text mainTier, Text secTier)
        {
            var so = new SerializedObject(target);
            so.FindProperty("mainPathText").objectReferenceValue = main;
            so.FindProperty("secondaryPathText").objectReferenceValue = secondary;
            so.FindProperty("mainTierText").objectReferenceValue = mainTier;
            so.FindProperty("secondaryTierText").objectReferenceValue = secTier;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireHUDManager(HUDManager target, HealthBarUI health, ManaBarUI mana, ComboCounterUI combo, PathIndicatorUI path)
        {
            var so = new SerializedObject(target);
            so.FindProperty("healthBar").objectReferenceValue = health;
            so.FindProperty("manaBar").objectReferenceValue = mana;
            so.FindProperty("comboCounter").objectReferenceValue = combo;
            so.FindProperty("pathIndicator").objectReferenceValue = path;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireEnemyHealthBarUI(EnemyHealthBarUI target, Image healthFill, Image pressureFill)
        {
            var so = new SerializedObject(target);
            so.FindProperty("healthFill").objectReferenceValue = healthFill;
            so.FindProperty("pressureFill").objectReferenceValue = pressureFill;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Generic Helpers ─────────────────────────────────────────────────

        private static T CreateOrLoadSO<T>(string folder, string name) where T : ScriptableObject
        {
            string path = $"{folder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var so = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[HUDCreator] Created {typeof(T).Name}: {path}");
            return so;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            return comp != null ? comp : go.AddComponent<T>();
        }

        private static GameObject FindOrCreateChild(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            if (t != null) return t.gameObject;

            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static void SetAnchor(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(
                (anchorMin.x + anchorMax.x) * 0.5f,
                (anchorMin.y + anchorMax.y) * 0.5f);
        }

        private static void StretchFill(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static void EnsureDirectories()
        {
            EnsureDir("Assets/ScriptableObjects");
            EnsureDir(SO_PATH);
            EnsureDir("Assets/Prefabs");
            EnsureDir(PREFAB_PATH);
        }

        private static void EnsureDir(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                int lastSlash = path.LastIndexOf('/');
                string parent = path.Substring(0, lastSlash);
                string folder = path.Substring(lastSlash + 1);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
