using System.IO;
using UnityEditor;
using UnityEngine;

namespace TomatoFighters.Editor.VFX
{
    public static class ShaderUtils
    {
        private const string SHADER_FOLDER = "Assets/Shaders/Generated";

        private const string FALLBACK_SHADER_TEMPLATE = @"Shader ""TomatoFighters/{0}""
{{
    Properties
    {{
        _MainTex (""Texture"", 2D) = ""white"" {{}}
        _Color (""Tint"", Color) = (1,1,1,1)
    }}
    SubShader
    {{
        Tags {{ ""Queue""=""Transparent"" ""RenderType""=""Transparent"" ""RenderPipeline""=""UniversalPipeline"" }}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {{
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl""

            struct Attributes
            {{
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            }};

            struct Varyings
            {{
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            }};

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _Color;

            Varyings vert(Attributes IN)
            {{
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }}

            half4 frag(Varyings IN) : SV_Target
            {{
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                return tex * IN.color;
            }}
            ENDHLSL
        }}
    }}
}}";

        /// <summary>
        /// Load a shader from the generated shaders folder by asset path.
        /// Falls back to Shader.Find, then creates a basic URP shader if all else fails.
        /// </summary>
        public static Shader LoadOrCreateShader(string shaderAssetPath, string shaderName)
        {
            // Try loading by asset path first (most reliable after Refresh)
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderAssetPath);
            if (shader != null) return shader;

            // Try Shader.Find as fallback
            shader = Shader.Find($"TomatoFighters/{shaderName}");
            if (shader != null) return shader;

            // Create a basic fallback shader
            Debug.Log($"[VFX] Shader not found, creating fallback URP shader: {shaderName}");
            string code = string.Format(FALLBACK_SHADER_TEMPLATE, shaderName);

            string fullDir = Path.Combine(Application.dataPath,
                SHADER_FOLDER.Substring("Assets/".Length));
            if (!Directory.Exists(fullDir))
                Directory.CreateDirectory(fullDir);

            string diskPath = Path.Combine(Application.dataPath,
                shaderAssetPath.Substring("Assets/".Length));
            File.WriteAllText(diskPath, code);
            AssetDatabase.Refresh();

            shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderAssetPath);
            if (shader != null) return shader;

            // Last resort
            return Shader.Find("Sprites/Default");
        }

        /// <summary>
        /// Save shader code to disk, load it, and create a material.
        /// Pass null for code to use the built-in fallback URP shader.
        /// Returns the material.
        /// </summary>
        public static Material SaveShaderAndCreateMaterial(string code, string name)
        {
            // Use fallback template if no code provided
            if (string.IsNullOrEmpty(code))
                code = string.Format(FALLBACK_SHADER_TEMPLATE, name);

            string fullDir = Path.Combine(Application.dataPath,
                SHADER_FOLDER.Substring("Assets/".Length));
            if (!Directory.Exists(fullDir))
                Directory.CreateDirectory(fullDir);

            string shaderAssetPath = $"{SHADER_FOLDER}/{name}.shader";
            string diskPath = Path.Combine(Application.dataPath,
                shaderAssetPath.Substring("Assets/".Length));
            File.WriteAllText(diskPath, code);
            AssetDatabase.Refresh();

            var shader = LoadOrCreateShader(shaderAssetPath, name);
            var mat = new Material(shader);

            string matPath = $"{SHADER_FOLDER}/{name}_Mat.mat";
            // Overwrite if exists
            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(matPath);

            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            return mat;
        }

        /// <summary>
        /// List existing shaders that compile without errors.
        /// Only returns shaders that Unity can actually load and use.
        /// </summary>
        public static string ListValidShaders()
        {
            string[] guids = AssetDatabase.FindAssets("t:Shader", new[] { SHADER_FOLDER });
            if (guids.Length == 0)
                return "(none)";

            var sb = new System.Text.StringBuilder();
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
                if (shader == null) continue;

                // Skip shaders with compile errors
                if (ShaderUtil.ShaderHasError(shader)) continue;

                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                string shaderName = shader.name;
                int propertyCount = ShaderUtil.GetPropertyCount(shader);

                // Build a brief description from shader properties
                var props = new System.Text.StringBuilder();
                for (int i = 0; i < propertyCount && i < 5; i++)
                {
                    if (i > 0) props.Append(", ");
                    props.Append(ShaderUtil.GetPropertyName(shader, i));
                }

                sb.AppendLine($"- {fileName} (path: \"{shaderName}\", properties: {props})");
            }

            return sb.Length > 0 ? sb.ToString() : "(none — all shaders have errors)";
        }
    }
}
