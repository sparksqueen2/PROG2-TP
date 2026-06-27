Shader "Game/GuardianGroundHalo"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (0.72, 0.42, 1, 0.85)
        _Falloff ("Falloff", Range(0.5, 6)) = 2.4
        _InnerBoost ("Inner Boost", Range(0, 2)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+10"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "GuardianGroundHalo"
            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                half _Falloff;
                half _InnerBoost;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv - 0.5;
                float dist = length(centered) * 2.0;
                float glow = saturate(1.0 - dist);
                glow = pow(glow, _Falloff);
                glow += pow(saturate(1.0 - dist * 1.35), _Falloff + 1.5) * _InnerBoost;

                half3 rgb = _GlowColor.rgb * glow;
                half alpha = glow * _GlowColor.a;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
