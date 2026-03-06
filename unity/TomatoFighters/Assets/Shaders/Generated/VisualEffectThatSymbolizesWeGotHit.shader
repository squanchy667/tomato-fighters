Shader "TomatoFighters/VisualEffectThatSymbolizesWeGotHit"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _HitColor("Hit Color", Color) = (1, 0, 0, 1)
        _HitIntensity("Hit Intensity", Range(0, 1)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 200

        Pass
        {
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _HitColor;
            float _HitIntensity;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.color = IN.color;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half3 hitEffect = _HitIntensity * _HitColor.rgb;
                half4 finalColor = texColor * IN.color + half4(hitEffect, 0);
                return finalColor;
            }
            ENDHLSL
        }
    }
}