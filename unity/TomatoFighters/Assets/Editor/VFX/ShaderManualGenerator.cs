using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.VFX
{
    public class ShaderManualGenerator : EditorWindow
    {
        private string prompt = "";
        private string shaderName = "";
        private string generatedPrompt = "";
        private string pastedCode = "";
        private string lastShaderCode = "";
        private string refinementPrompt = "";
        private bool hasGenerated;
        private Vector2 promptScroll;
        private Vector2 codeScroll;
        private Vector2 statusScroll;
        private string statusLog = "";

        private const string SHADER_FOLDER = "Assets/Shaders/Generated";

        [MenuItem("TomatoFighters/Shader Generator (Manual)")]
        public static void ShowWindow()
        {
            GetWindow<ShaderManualGenerator>("Shader Generator (Manual)");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);

            // Step 1: Describe
            EditorGUILayout.LabelField("1. Describe the shader:", EditorStyles.boldLabel);
            prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(40));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:", GUILayout.Width(40));
            shaderName = EditorGUILayout.TextField(shaderName);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("(auto-generated from prompt if blank)", EditorStyles.miniLabel);

            EditorGUILayout.Space(4);

            // Refine input
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

            // Step 2: Copy prompt
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

            // Step 3: Paste shader code
            EditorGUILayout.LabelField("3. Paste Claude's shader code:", EditorStyles.boldLabel);
            codeScroll = EditorGUILayout.BeginScrollView(codeScroll, GUILayout.Height(120));
            pastedCode = EditorGUILayout.TextArea(pastedCode, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(pastedCode));
            if (GUILayout.Button("Apply", GUILayout.Height(30)))
                ApplyShader();
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
            string name = GetShaderName();

            if (!string.IsNullOrWhiteSpace(refinementPrompt) && hasGenerated)
            {
                generatedPrompt = ShaderGenerator.SYSTEM_PROMPT +
                    "\n\n---\n\nPrevious shader code:\n" + lastShaderCode +
                    "\n\nRefinement request: " + refinementPrompt +
                    "\n\nReturn the complete updated shader code.";
            }
            else
            {
                generatedPrompt = ShaderGenerator.SYSTEM_PROMPT +
                    "\n\n---\n\nShader name: " + name +
                    "\n\nEffect description: " + prompt;
            }
            AppendStatus("Prompt generated. Copy it and paste into Claude.");
        }

        private void ApplyShader()
        {
            string code = ClaudeApiClient.StripMarkdownFencing(pastedCode);

            try
            {
                string name = GetShaderName();
                lastShaderCode = code;

                ShaderUtils.SaveShaderAndCreateMaterial(code, name);
                AppendStatus($"Applied: {SHADER_FOLDER}/{name}.shader + material");

                hasGenerated = true;
            }
            catch (Exception e)
            {
                AppendStatus($"Error: {e.Message}");
            }
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
