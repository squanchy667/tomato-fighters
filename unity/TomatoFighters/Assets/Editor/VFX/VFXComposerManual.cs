using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.VFX
{
    public class VFXComposerManual : EditorWindow
    {
        private enum Step { Describe, ShaderPrompt, ShaderPaste, ParticlePrompt, ParticlePaste }

        private Step currentStep = Step.Describe;
        private string prompt = "";
        private string effectName = "";
        private string generatedPrompt = "";
        private string pastedResponse = "";
        private string lastShaderCode = "";
        private string lastParticleJson = "";
        private Material composedMaterial;
        private bool allowReuseShader = true;
        private Vector2 promptScroll;
        private Vector2 pasteScroll;
        private Vector2 statusScroll;
        private string statusLog = "";
        private GameObject sceneInstance;

        private const string SHADER_FOLDER = "Assets/Shaders/Generated";
        private const string PREFAB_FOLDER = "Assets/Prefabs/VFX";

        private static string SHADER_STEP_SYSTEM => VFXPrompts.SHADER_PICK_OR_CREATE;
        private static string PARTICLE_STEP_SYSTEM => VFXPrompts.PARTICLE_SYSTEM;

        [MenuItem("TomatoFighters/VFX Composer (Manual)")]
        public static void ShowWindow()
        {
            GetWindow<VFXComposerManual>("VFX Composer (Manual)");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);

            // Progress bar
            string[] stepNames = { "Describe", "Shader Prompt", "Paste Shader", "Particle Prompt", "Paste Particles" };
            int stepIndex = (int)currentStep;
            EditorGUI.ProgressBar(
                EditorGUILayout.GetControlRect(GUILayout.Height(20)),
                (stepIndex + 1) / 5f,
                $"Step {stepIndex + 1}/5: {stepNames[stepIndex]}");

            EditorGUILayout.Space(8);

            switch (currentStep)
            {
                case Step.Describe: DrawDescribeStep(); break;
                case Step.ShaderPrompt: DrawPromptStep("shader"); break;
                case Step.ShaderPaste: DrawPasteStep("shader response (USE_EXISTING: ... or full shader code)"); break;
                case Step.ParticlePrompt: DrawPromptStep("particle config"); break;
                case Step.ParticlePaste: DrawPasteStep("particle config JSON"); break;
            }

            EditorGUILayout.Space(8);

            // Status
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            statusScroll = EditorGUILayout.BeginScrollView(statusScroll, GUILayout.Height(60));
            EditorGUILayout.TextArea(statusLog, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();

            // Reset button
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Start Over"))
            {
                currentStep = Step.Describe;
                generatedPrompt = "";
                pastedResponse = "";
                AppendStatus("Reset.");
            }
        }

        private void DrawDescribeStep()
        {
            EditorGUILayout.LabelField("Describe the full VFX effect:", EditorStyles.boldLabel);
            prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(60));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:", GUILayout.Width(40));
            effectName = EditorGUILayout.TextField(effectName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            allowReuseShader = EditorGUILayout.Toggle("Allow reuse existing shader", allowReuseShader);

            EditorGUILayout.Space(8);

            EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(prompt));
            if (GUILayout.Button("Next: Generate Shader Prompt", GUILayout.Height(30)))
            {
                string name = GetEffectName();
                if (allowReuseShader)
                {
                    string existingShaders = ShaderUtils.ListValidShaders();
                    generatedPrompt = SHADER_STEP_SYSTEM +
                        "\n\n---\n\nEffect description: " + prompt +
                        "\nShader name (if new): " + name +
                        "\n\nExisting shaders in project:\n" + existingShaders;
                }
                else
                {
                    generatedPrompt = ShaderGenerator.SYSTEM_PROMPT +
                        "\n\n---\n\nShader name: " + name +
                        "\n\nEffect description: " + prompt;
                }
                pastedResponse = "";
                currentStep = Step.ShaderPrompt;
                AppendStatus("Shader prompt generated.");
            }
            EditorGUI.EndDisabledGroup();
        }

        private void DrawPromptStep(string label)
        {
            EditorGUILayout.LabelField($"Copy this {label} prompt to Claude:", EditorStyles.boldLabel);
            promptScroll = EditorGUILayout.BeginScrollView(promptScroll, GUILayout.Height(180));
            EditorGUILayout.TextArea(generatedPrompt, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy to Clipboard", GUILayout.Height(25)))
            {
                GUIUtility.systemCopyBuffer = generatedPrompt;
                AppendStatus("Copied to clipboard.");
            }
            if (GUILayout.Button("Next: Paste Response", GUILayout.Height(25)))
            {
                pastedResponse = "";
                currentStep = currentStep == Step.ShaderPrompt ? Step.ShaderPaste : Step.ParticlePaste;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPasteStep(string label)
        {
            EditorGUILayout.LabelField($"Paste Claude's {label}:", EditorStyles.boldLabel);
            pasteScroll = EditorGUILayout.BeginScrollView(pasteScroll, GUILayout.Height(180));
            pastedResponse = EditorGUILayout.TextArea(pastedResponse, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUI.BeginDisabledGroup(string.IsNullOrWhiteSpace(pastedResponse));
            if (currentStep == Step.ShaderPaste)
            {
                if (GUILayout.Button("Apply Shader & Next", GUILayout.Height(30)))
                    ApplyShaderAndAdvance();
            }
            else
            {
                if (GUILayout.Button("Apply & Create Prefab", GUILayout.Height(30)))
                    ApplyParticleAndFinish();
            }
            EditorGUI.EndDisabledGroup();
        }

        private void ApplyShaderAndAdvance()
        {
            try
            {
                string name = GetEffectName();
                string response = ClaudeApiClient.StripMarkdownFencing(pastedResponse.Trim());

                if (response.StartsWith("USE_EXISTING:"))
                {
                    string existingName = response.Substring("USE_EXISTING:".Length).Trim();
                    if (existingName.StartsWith("TomatoFighters/"))
                        existingName = existingName.Substring("TomatoFighters/".Length);

                    string matPath = $"{SHADER_FOLDER}/{existingName}_Mat.mat";
                    composedMaterial = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if (composedMaterial != null)
                    {
                        lastShaderCode = "";
                        AppendStatus($"Using existing shader: {existingName}");
                    }
                    else
                    {
                        AppendStatus($"Shader \"{existingName}\" not found, creating fallback.");
                        composedMaterial = ShaderUtils.SaveShaderAndCreateMaterial(null, name);
                    }
                }
                else
                {
                    SaveNewShader(response, name);
                }

                // Generate particle prompt
                string shaderUsed = composedMaterial != null ? composedMaterial.shader.name : "Default-Particle";
                generatedPrompt = PARTICLE_STEP_SYSTEM +
                    "\n\n---\n\nEffect description: " + prompt +
                    "\nShader being used: " + shaderUsed +
                    "\n\nGenerate a ParticleConfig that works well with this shader.";
                pastedResponse = "";
                currentStep = Step.ParticlePrompt;
                AppendStatus("Shader done. Now generate particle config prompt.");
            }
            catch (Exception e)
            {
                AppendStatus($"Error: {e.Message}");
            }
        }

        private void SaveNewShader(string code, string name)
        {
            lastShaderCode = code;
            composedMaterial = ShaderUtils.SaveShaderAndCreateMaterial(code, name);
            AppendStatus($"Shader created: {SHADER_FOLDER}/{name}.shader");
        }

        private void ApplyParticleAndFinish()
        {
            try
            {
                string name = GetEffectName();
                string json = ClaudeApiClient.StripMarkdownFencing(pastedResponse.Trim());
                var config = JsonUtility.FromJson<ParticleConfig>(json);
                lastParticleJson = JsonUtility.ToJson(config, true);

                if (sceneInstance != null)
                    DestroyImmediate(sceneInstance);

                var go = ParticleSystemApplier.Apply(config, name);

                // Override material
                if (composedMaterial != null)
                {
                    var renderer = go.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null)
                        renderer.material = composedMaterial;
                }

                // Save prefab
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

                AppendStatus($"Done! Prefab: {prefabPath}");
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
            return sb.Length > 0 ? sb.ToString() : "ComposedEffect";
        }

        private void AppendStatus(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            statusLog = $"[{timestamp}] {message}\n{statusLog}";
        }
    }
}
