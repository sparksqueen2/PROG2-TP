Shader "Game/MenuVerticalFog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.45, 0.48, 0.54, 0.35)
        _FogDensity ("Fog Density", Range(0, 2)) = 0.85
        _NoiseTex ("Noise", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Float) = 0.004
        _NoiseSpeed ("Noise Speed", Vector) = (0.04, 0.02, 0, 0)
        _VerticalPower ("Vertical Power", Range(0.2, 4)) = 1.4
        _EdgeFade ("Edge Fade", Range(0, 1)) = 0.55
        _DepthBlend ("Depth Blend", Float) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "MenuVerticalFog"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _FogDensity;
                float4 _NoiseTex_ST;
                float _NoiseScale;
                float4 _NoiseSpeed;
                float _VerticalPower;
                float _EdgeFade;
                float _DepthBlend;
            CBUFFER_END

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPosition : TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.worldPosition = positionInputs.positionWS;
                output.uv = input.uv;
                output.screenPosition = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 noiseUv = input.worldPosition.xz * _NoiseScale + _Time.y * _NoiseSpeed.xy;
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUv).r;
                noise = lerp(0.65, 1.0, noise);

                float verticalFade = pow(saturate(1.0 - input.uv.y), _VerticalPower);

                float2 centeredUv = input.uv * 2.0 - 1.0;
                float radialFade = 1.0 - saturate(length(centeredUv) - (1.0 - _EdgeFade));
                radialFade = smoothstep(0.0, 1.0, radialFade);

                float2 screenUv = input.screenPosition.xy / input.screenPosition.w;
                float sceneRawDepth = SampleSceneDepth(screenUv);
                float sceneEyeDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                float fogEyeDepth = LinearEyeDepth(input.positionCS.z, _ZBufferParams);
                float depthFade = saturate((sceneEyeDepth - fogEyeDepth) * _DepthBlend);

                float alpha = _FogColor.a * _FogDensity * verticalFade * radialFade * noise * depthFade;
                return half4(_FogColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
