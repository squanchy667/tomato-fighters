using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.VFX
{
    public class ParticleSystemGenerator : EditorWindow
    {
        private string prompt = "";
        private string effectName = "";
        private string refinementPrompt = "";
        private string lastConfigJson = "";
        private bool hasGenerated;
        private bool isProcessing;
        private bool showApiSettings;
        private string apiKeyInput = "";
        private Vector2 statusScroll;
        private string statusLog = "";
        private GameObject sceneInstance;

        private const string PREFAB_FOLDER = "Assets/Prefabs/VFX";

        [MenuItem("TomatoFighters/VFX Generator")]
        public static void ShowWindow()
        {
            GetWindow<ParticleSystemGenerator>("VFX Generator");
        }

        private void OnEnable()
        {
            apiKeyInput = ClaudeApiClient.GetApiKey();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);

            // Prompt
            EditorGUILayout.LabelField("Prompt:", EditorStyles.boldLabel);
            prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(50));

            EditorGUILayout.Space(4);

            // Name
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:", GUILayout.Width(40));
            effectName = EditorGUILayout.TextField(effectName);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("(auto-generated from prompt if blank)", EditorStyles.miniLabel);

            EditorGUILayout.Space(8);

            // Buttons
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(isProcessing || string.IsNullOrWhiteSpace(prompt));
            if (GUILayout.Button("Generate", GUILayout.Height(30)))
                Generate();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(isProcessing || !hasGenerated || string.IsNullOrWhiteSpace(refinementPrompt));
            if (GUILayout.Button("Refine", GUILayout.Height(30)))
                Refine();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            // Refinement prompt (shown after first generate)
            if (hasGenerated)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Refine:", EditorStyles.boldLabel);
                refinementPrompt = EditorGUILayout.TextArea(refinementPrompt, GUILayout.Height(40));
            }

            EditorGUILayout.Space(8);

            // Status log
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            statusScroll = EditorGUILayout.BeginScrollView(statusScroll, GUILayout.Height(100));
            EditorGUILayout.TextArea(statusLog, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);

            // API Settings foldout
            showApiSettings = EditorGUILayout.Foldout(showApiSettings, "API Settings");
            if (showApiSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                apiKeyInput = EditorGUILayout.PasswordField("API Key:", apiKeyInput);
                if (GUILayout.Button("Save", GUILayout.Width(50)))
                {
                    ClaudeApiClient.SetApiKey(apiKeyInput);
                    var detected = ClaudeApiClient.DetectPlatform(apiKeyInput);
                    AppendStatus($"API key saved. Platform: {ClaudeApiClient.PlatformLabel(detected)}");
                }
                EditorGUILayout.EndHorizontal();

                // Show detected platform
                if (!string.IsNullOrEmpty(apiKeyInput))
                {
                    var platform = ClaudeApiClient.DetectPlatform(apiKeyInput);
                    EditorGUILayout.HelpBox(
                        $"Detected platform: {ClaudeApiClient.PlatformLabel(platform)}\n" +
                        "Key prefix: sk-ant- = Anthropic, sk-or- = OpenRouter, other = OpenAI",
                        MessageType.Info);
                }
                EditorGUI.indentLevel--;
            }
        }

        private async void Generate()
        {
            string name = GetEffectName();
            isProcessing = true;
            AppendStatus($"Generating \"{name}\" ...");
            Repaint();

            try
            {
                var config = await ClaudeApiClient.SendPrompt(prompt);
                lastConfigJson = JsonUtility.ToJson(config, true);
                ApplyAndSave(config, name);
                hasGenerated = true;
                AppendStatus($"Generated: {PREFAB_FOLDER}/{name}.prefab");
            }
            catch (Exception e)
            {
                AppendStatus($"Error: {e.Message}");
            }
            finally
            {
                isProcessing = false;
                Repaint();
            }
        }

        private async void Refine()
        {
            string name = GetEffectName();
            isProcessing = true;
            AppendStatus($"Refining \"{name}\" ...");
            Repaint();

            try
            {
                var config = await ClaudeApiClient.SendRefinement(lastConfigJson, refinementPrompt);
                lastConfigJson = JsonUtility.ToJson(config, true);
                ApplyAndSave(config, name);
                AppendStatus($"Refined: {PREFAB_FOLDER}/{name}.prefab");
            }
            catch (Exception e)
            {
                AppendStatus($"Error: {e.Message}");
            }
            finally
            {
                isProcessing = false;
                Repaint();
            }
        }

        private void ApplyAndSave(ParticleConfig config, string name)
        {
            // Destroy previous scene instance
            if (sceneInstance != null)
                DestroyImmediate(sceneInstance);

            // Create configured particle system
            var go = ParticleSystemApplier.Apply(config, name);

            // Ensure output folder exists
            if (!AssetDatabase.IsValidFolder(PREFAB_FOLDER))
            {
                string[] parts = PREFAB_FOLDER.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }

            // Save as prefab
            string prefabPath = $"{PREFAB_FOLDER}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.AutomatedAction);

            // Keep the scene instance for preview
            sceneInstance = go;

            // Play the particle system for immediate feedback
            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null)
                ps.Play();
        }

        private string GetEffectName()
        {
            if (!string.IsNullOrWhiteSpace(effectName))
                return effectName;

            // Auto-generate PascalCase name from prompt
            string cleaned = Regex.Replace(prompt.Trim(), @"[^a-zA-Z0-9\s]", "");
            string[] words = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new System.Text.StringBuilder();
            foreach (string word in words)
            {
                if (word.Length > 0)
                    sb.Append(char.ToUpper(word[0])).Append(word.Substring(1).ToLower());
            }
            return sb.Length > 0 ? sb.ToString() : "ParticleEffect";
        }

        private void AppendStatus(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            statusLog = $"[{timestamp}] {message}\n{statusLog}";
        }
    }
}
