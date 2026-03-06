using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.VFX
{
    public class ShaderGenerator : EditorWindow
    {
        private string prompt = "";
        private string shaderName = "";
        private string refinementPrompt = "";
        private string lastShaderCode = "";
        private bool hasGenerated;
        private bool isProcessing;
        private bool showApiSettings;
        private string apiKeyInput = "";
        private Vector2 statusScroll;
        private Vector2 previewScroll;
        private string statusLog = "";
        private string previewCode = "";

        private const string SHADER_FOLDER = "Assets/Shaders/Generated";

        internal static string SYSTEM_PROMPT => VFXPrompts.SHADER_NEW;

        [MenuItem("TomatoFighters/Shader Generator")]
        public static void ShowWindow()
        {
            GetWindow<ShaderGenerator>("Shader Generator");
        }

        private void OnEnable()
        {
            apiKeyInput = ClaudeApiClient.GetApiKey();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);

            // Prompt
            EditorGUILayout.LabelField("Describe the shader:", EditorStyles.boldLabel);
            prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(50));

            EditorGUILayout.Space(4);

            // Name
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:", GUILayout.Width(40));
            shaderName = EditorGUILayout.TextField(shaderName);
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

            // Refinement prompt
            if (hasGenerated)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Refine:", EditorStyles.boldLabel);
                refinementPrompt = EditorGUILayout.TextArea(refinementPrompt, GUILayout.Height(40));
            }

            EditorGUILayout.Space(8);

            // Shader preview
            if (!string.IsNullOrEmpty(previewCode))
            {
                EditorGUILayout.LabelField("Generated Shader", EditorStyles.boldLabel);
                previewScroll = EditorGUILayout.BeginScrollView(previewScroll, GUILayout.Height(150));
                EditorGUILayout.TextArea(previewCode, EditorStyles.textArea, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(4);

            // Status log
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            statusScroll = EditorGUILayout.BeginScrollView(statusScroll, GUILayout.Height(80));
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

                if (!string.IsNullOrEmpty(apiKeyInput))
                {
                    var platform = ClaudeApiClient.DetectPlatform(apiKeyInput);
                    EditorGUILayout.HelpBox(
                        $"Detected platform: {ClaudeApiClient.PlatformLabel(platform)}",
                        MessageType.Info);
                }
                EditorGUI.indentLevel--;
            }
        }

        private async void Generate()
        {
            string name = GetShaderName();
            isProcessing = true;
            AppendStatus($"Generating shader \"{name}\" ...");
            Repaint();

            try
            {
                string userPrompt = $"Shader name: {name}\n\nEffect description: {prompt}";
                string code = await ClaudeApiClient.SendRawPrompt(SYSTEM_PROMPT, userPrompt);
                SaveShader(code, name);
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
            string name = GetShaderName();
            isProcessing = true;
            AppendStatus($"Refining shader \"{name}\" ...");
            Repaint();

            try
            {
                string userPrompt =
                    $"Previous shader code:\n{lastShaderCode}\n\nRefinement request: {refinementPrompt}\n\nReturn the complete updated shader code.";
                string code = await ClaudeApiClient.SendRawPrompt(SYSTEM_PROMPT, userPrompt);
                SaveShader(code, name);
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

        private void SaveShader(string code, string name)
        {
            lastShaderCode = code;
            previewCode = code;
            hasGenerated = true;

            ShaderUtils.SaveShaderAndCreateMaterial(code, name);
            AppendStatus($"Saved: {SHADER_FOLDER}/{name}.shader + material");
        }

        private string GetShaderName()
        {
            if (!string.IsNullOrWhiteSpace(shaderName))
                return shaderName;

            string cleaned = Regex.Replace(prompt.Trim(), @"[^a-zA-Z0-9\s]", "");
            string[] words = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new System.Text.StringBuilder();
            foreach (string word in words)
            {
                if (word.Length > 0)
                    sb.Append(char.ToUpper(word[0])).Append(word.Substring(1).ToLower());
            }
            return sb.Length > 0 ? sb.ToString() : "GeneratedShader";
        }

        private void AppendStatus(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            statusLog = $"[{timestamp}] {message}\n{statusLog}";
        }
    }
}
