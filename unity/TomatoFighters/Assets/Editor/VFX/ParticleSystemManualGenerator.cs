using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.VFX
{
    public class ParticleSystemManualGenerator : EditorWindow
    {
        private string prompt = "";
        private string effectName = "";
        private string generatedPrompt = "";
        private string pastedResponse = "";
        private string lastConfigJson = "";
        private string refinementPrompt = "";
        private bool hasGenerated;
        private Vector2 promptScroll;
        private Vector2 responseScroll;
        private Vector2 statusScroll;
        private string statusLog = "";
        private GameObject sceneInstance;

        private const string PREFAB_FOLDER = "Assets/Prefabs/VFX";

        private static string SYSTEM_PROMPT => VFXPrompts.PARTICLE_SYSTEM;

        [MenuItem("TomatoFighters/VFX Generator (Manual)")]
        public static void ShowWindow()
        {
            GetWindow<ParticleSystemManualGenerator>("VFX Generator (Manual)");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);

            // Step 1: Describe the effect
            EditorGUILayout.LabelField("1. Describe the effect:", EditorStyles.boldLabel);
            prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(40));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:", GUILayout.Width(40));
            effectName = EditorGUILayout.TextField(effectName);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("(auto-generated from prompt if blank)", EditorStyles.miniLabel);

            EditorGUILayout.Space(4);

            // Refine input (shown after first apply)
            if (hasGenerated)
            {
                EditorGUILayout.LabelField("Refinement (optional):", EditorStyles.boldLabel);
                refinementPrompt = EditorGUILayout.TextArea(refinementPrompt, GUILayout.Height(30));
            }

            EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(prompt));
            if (GUILayout.Button("Generate Prompt", GUILayout.Height(25)))
                GeneratePromptText();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);

            // Step 2: Copy the prompt
            EditorGUILayout.LabelField("2. Copy this prompt and paste into Claude:", EditorStyles.boldLabel);
            promptScroll = EditorGUILayout.BeginScrollView(promptScroll, GUILayout.Height(100));
            EditorGUILayout.TextArea(generatedPrompt, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Copy to Clipboard") && !string.IsNullOrEmpty(generatedPrompt))
            {
                GUIUtility.systemCopyBuffer = generatedPrompt;
                AppendStatus("Prompt copied to clipboard.");
            }

            EditorGUILayout.Space(8);

            // Step 3: Paste JSON response
            EditorGUILayout.LabelField("3. Paste Claude's JSON response:", EditorStyles.boldLabel);
            responseScroll = EditorGUILayout.BeginScrollView(responseScroll, GUILayout.Height(100));
            pastedResponse = EditorGUILayout.TextArea(pastedResponse, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(pastedResponse));
            if (GUILayout.Button("Apply", GUILayout.Height(30)))
                ApplyResponse();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(8);

            // Status
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            statusScroll = EditorGUILayout.BeginScrollView(statusScroll, GUILayout.Height(60));
            EditorGUILayout.TextArea(statusLog, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }

        private void GeneratePromptText()
        {
            if (!string.IsNullOrWhiteSpace(refinementPrompt) && hasGenerated)
            {
                generatedPrompt = SYSTEM_PROMPT +
                    "\n\n---\n\nPrevious configuration:\n" + lastConfigJson +
                    "\n\nRefinement request: " + refinementPrompt +
                    "\n\nReturn the complete updated ParticleConfig JSON.";
            }
            else
            {
                generatedPrompt = SYSTEM_PROMPT + "\n\n---\n\nEffect description: " + prompt;
            }
            AppendStatus("Prompt generated. Copy it and paste into Claude.");
        }

        private void ApplyResponse()
        {
            string text = pastedResponse.Trim();

            // Strip markdown fencing
            if (text.StartsWith("```"))
            {
                int nl = text.IndexOf('\n');
                if (nl >= 0) text = text.Substring(nl + 1);
            }
            if (text.EndsWith("```"))
                text = text.Substring(0, text.Length - 3);
            text = text.Trim();

            try
            {
                var config = JsonUtility.FromJson<ParticleConfig>(text);
                string name = GetEffectName();
                lastConfigJson = JsonUtility.ToJson(config, true);

                if (sceneInstance != null)
                    DestroyImmediate(sceneInstance);

                var go = ParticleSystemApplier.Apply(config, name);

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

                string prefabPath = $"{PREFAB_FOLDER}/{name}.prefab";
                PrefabUtility.SaveAsPrefabAssetAndConnect(go, prefabPath, InteractionMode.AutomatedAction);
                sceneInstance = go;

                var ps = go.GetComponent<ParticleSystem>();
                if (ps != null) ps.Play();

                hasGenerated = true;
                AppendStatus($"Applied: {prefabPath}");
            }
            catch (Exception e)
            {
                AppendStatus($"Error: {e.Message}");
            }
        }

        private string GetEffectName()
        {
            if (!string.IsNullOrWhiteSpace(effectName))
                return effectName;

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
