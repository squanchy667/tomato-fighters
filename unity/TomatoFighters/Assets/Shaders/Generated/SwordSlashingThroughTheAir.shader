Shader "TomatoFighters/SwordSlashingExtended"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _GlowPower ("Glow Power", Range(0, 10)) = 2
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 1
        _Spread ("Spread", Range(0, 5)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend One One 

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 texcoord     : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 texcoord     : TEXCOORD0;
                half4 color         : COLOR;
                float3 worldPos     : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _GlowColor;
            float _GlowPower;
            half _GlowIntensity;
            half _Spread;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.texcoord = IN.texcoord * _Spread;
                OUT.color = IN.color;
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.texcoord) * IN.color;
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);
                half fresnel = pow(1.0 - dot(viewDir, normalize(IN.worldPos)), _GlowPower);
                half4 glow = _GlowColor * fresnel * _GlowIntensity;
                
                return texColor + glow;
            }

            ENDHLSL
        }
    }
}