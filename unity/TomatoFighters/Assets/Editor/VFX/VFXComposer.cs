using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.VFX
{
    public class VFXComposer : EditorWindow
    {
        private string prompt = "";
        private string effectName = "";
        private string refinementPrompt = "";
        private string lastShaderCode = "";
        private string lastParticleJson = "";
        private bool hasGenerated;
        private bool isProcessing;
        private bool allowReuseShader = true;
        private bool showApiSettings;
        private string apiKeyInput = "";
        private Vector2 statusScroll;
        private string statusLog = "";
        private GameObject sceneInstance;

        private const string SHADER_FOLDER = "Assets/Shaders/Generated";
        private const string PREFAB_FOLDER = "Assets/Prefabs/VFX";

        private static string SHADER_STEP_SYSTEM => VFXPrompts.SHADER_PICK_OR_CREATE;
        private static string SHADER_NEW_ONLY_SYSTEM => VFXPrompts.SHADER_NEW;
        private static string PARTICLE_STEP_SYSTEM => VFXPrompts.PARTICLE_SYSTEM;

        [MenuItem("TomatoFighters/VFX Composer")]
        public static void ShowWindow()
        {
            GetWindow<VFXComposer>("VFX Composer");
        }

        private void OnEnable()
        {
            apiKeyInput = ClaudeApiClient.GetApiKey();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Describe the full VFX effect:", EditorStyles.boldLabel);
            prompt = EditorGUILayout.TextArea(prompt, GUILayout.Height(60));

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:", GUILayout.Width(40));
            effectName = EditorGUILayout.TextField(effectName);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("(auto-generated from prompt if blank)", EditorStyles.miniLabel);

            EditorGUILayout.Space(4);
            allowReuseShader = EditorGUILayout.Toggle("Allow reuse existing shader", allowReuseShader);

            EditorGUILayout.Space(8);

            // Buttons
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(isProcessing || string.IsNullOrWhiteSpace(prompt));
            if (GUILayout.Button("Compose", GUILayout.Height(30)))
                Compose();
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(isProcessing || !hasGenerated || string.IsNullOrWhiteSpace(refinementPrompt));
            if (GUILayout.Button("Refine", GUILayout.Height(30)))
                Refine();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (hasGenerated)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Refine:", EditorStyles.boldLabel);
                refinementPrompt = EditorGUILayout.TextArea(refinementPrompt, GUILayout.Height(40));
            }

            EditorGUILayout.Space(8);

            // Status
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
            statusScroll = EditorGUILayout.BeginScrollView(statusScroll, GUILayout.Height(120));
            EditorGUILayout.TextArea(statusLog, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);

            // API Settings
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
                EditorGUI.indentLevel--;
            }
        }

        private async void Compose()
        {
            string name = GetEffectName();
            isProcessing = true;
            Repaint();

            try
            {
                // Step 1: Shader selection/creation
                AppendStatus($"[1/3] {(allowReuseShader ? "Choosing" : "Creating")} shader for \"{name}\" ...");
                Repaint();

                string shaderPrompt;
                string systemPrompt;
                if (allowReuseShader)
                {
                    string existingShaders = ShaderUtils.ListValidShaders();
                    shaderPrompt = $"Effect description: {prompt}\n\nExisting shaders in project:\n{existingShaders}";
                    systemPrompt = SHADER_STEP_SYSTEM;
                }
                else
                {
                    shaderPrompt = $"Shader name: {name}\n\nEffect description: {prompt}";
                    systemPrompt = SHADER_NEW_ONLY_SYSTEM;
                }
                string shaderResponse = await ClaudeApiClient.SendRawPrompt(systemPrompt, shaderPrompt);

                Material material = ProcessShaderResponse(shaderResponse, name);

                // Step 2: Particle system
                AppendStatus("[2/3] Generating particle system ...");
                Repaint();

                string particlePrompt = $"Effect description: {prompt}\nShader being used: {material.shader.name}\n\nGenerate a ParticleConfig that works well with this shader.";
                string particleResponse = await ClaudeApiClient.SendRawPrompt(PARTICLE_STEP_SYSTEM, particlePrompt);
                var config = JsonUtility.FromJson<ParticleConfig>(particleResponse);
                lastParticleJson = JsonUtility.ToJson(config, true);

                // Step 3: Assemble
                AppendStatus("[3/3] Assembling prefab ...");
                Repaint();

                AssemblePrefab(config, material, name);
                hasGenerated = true;
                AppendStatus($"Done! {PREFAB_FOLDER}/{name}.prefab");
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
            Repaint();

            try
            {
                AppendStatus($"[1/3] Refining shader ...");
                Repaint();

                string shaderPrompt;
                string systemPrompt;
                if (allowReuseShader)
                {
                    string existingShaders = ShaderUtils.ListValidShaders();
                    shaderPrompt =
                        $"Previous shader:\n{lastShaderCode}\n\nEffect description: {prompt}\nRefinement: {refinementPrompt}\n\nExisting shaders:\n{existingShaders}";
                    systemPrompt = SHADER_STEP_SYSTEM;
                }
                else
                {
                    shaderPrompt =
                        $"Previous shader:\n{lastShaderCode}\n\nShader name: {name}\n\nEffect description: {prompt}\nRefinement: {refinementPrompt}";
                    systemPrompt = SHADER_NEW_ONLY_SYSTEM;
                }
                string shaderResponse = await ClaudeApiClient.SendRawPrompt(systemPrompt, shaderPrompt);
                Material material = ProcessShaderResponse(shaderResponse, name);

                AppendStatus("[2/3] Refining particle system ...");
                Repaint();

                string particlePrompt =
                    $"Previous config:\n{lastParticleJson}\n\nEffect: {prompt}\nShader: {material.shader.name}\nRefinement: {refinementPrompt}\n\nReturn the complete updated ParticleConfig JSON.";
                string particleResponse = await ClaudeApiClient.SendRawPrompt(PARTICLE_STEP_SYSTEM, particlePrompt);
                var config = JsonUtility.FromJson<ParticleConfig>(particleResponse);
                lastParticleJson = JsonUtility.ToJson(config, true);

                AppendStatus("[3/3] Assembling prefab ...");
                Repaint();

                AssemblePrefab(config, material, name);
                AppendStatus($"Refined! {PREFAB_FOLDER}/{name}.prefab");
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

        private Material ProcessShaderResponse(string response, string name)
        {
            response = response.Trim();

            // Check if AI chose an existing shader
            if (response.StartsWith("USE_EXISTING:"))
            {
                string existingName = response.Substring("USE_EXISTING:".Length).Trim();
                // Strip "TomatoFighters/" prefix if AI included it
                if (existingName.StartsWith("TomatoFighters/"))
                    existingName = existingName.Substring("TomatoFighters/".Length);

                // Try loading existing material
                string matPath = $"{SHADER_FOLDER}/{existingName}_Mat.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat != null)
                {
                    AppendStatus($"Using existing shader: {existingName}");
                    lastShaderCode = "";
                    return mat;
                }

                // Existing not found — create a fresh shader with fallback
                AppendStatus($"Existing shader \"{existingName}\" not found, creating fallback");
                var fallbackMat = ShaderUtils.SaveShaderAndCreateMaterial(null, name);
                AppendStatus($"Fallback shader created: {SHADER_FOLDER}/{name}.shader");
                return fallbackMat;
            }

            // New shader code from AI
            lastShaderCode = response;
            var material = ShaderUtils.SaveShaderAndCreateMaterial(response, name);
            AppendStatus($"Shader created: {SHADER_FOLDER}/{name}.shader");
            return material;
        }

        private void AssemblePrefab(ParticleConfig config, Material material, string name)
        {
            if (sceneInstance != null)
                DestroyImmediate(sceneInstance);

            var go = ParticleSystemApplier.Apply(config, name);

            // Override material with the composed one
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
                renderer.material = material;

            // Ensure prefab folder
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
