Shader "TomatoFighters/Light"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _LightColor("Light Color", Color) = (1, 1, 1, 1)
        _Intensity("Intensity", Range(0, 10)) = 1
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        Pass
        {
            Blend One One
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata_t
            {
                float4 vertex : POSITION;
                half4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                half2 texcoord : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            half4 _LightColor;
            float _Intensity;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.position = TransformObjectToHClip(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color;
                return o;
            }
            
            half4 frag(v2f i) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
                half3 lightEffect = _LightColor.rgb * _Intensity;
                half4 outputColor = half4(texColor.rgb * lightEffect, texColor.a);
                outputColor *= i.color; // Apply vertex color for tinting
                return outputColor;
            }

            ENDHLSL
        }
    }
}