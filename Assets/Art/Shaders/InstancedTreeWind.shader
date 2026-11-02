Shader "NPC/InstancedTreeWind"
{
    Properties
    {
        _MainTex("Twelve-frame Tree Atlas", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+1" "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite On
        Blend Off

        Pass
        {
            Name "TreeUnlit"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex TreeVertex
            #pragma fragment TreeFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
                float _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                nointerpolation float frame : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float AnimationFrame(float seconds)
            {
                // Authored durations: 200,160,160,200,160,160,160,200,160,160,160,160 ms.
                return step(0.20, seconds) + step(0.36, seconds) + step(0.52, seconds)
                    + step(0.72, seconds) + step(0.88, seconds) + step(1.04, seconds)
                    + step(1.20, seconds) + step(1.40, seconds) + step(1.56, seconds)
                    + step(1.72, seconds) + step(1.88, seconds);
            }

            Varyings TreeVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                Varyings output;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                float3 originWS = TransformObjectToWorld(float3(0, 0, 0));
                float phase = frac(sin(dot(originWS.xy, float2(12.9898, 78.233))) * 43758.5453);
                float seconds = frac(_Time.y / 2.04 + phase) * 2.04;
                output.frame = AnimationFrame(seconds);
                output.uv = input.uv;
                return output;
            }

            half4 TreeFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // Pixel-center sampling preserves frame boundaries without atlas edge bleed.
                float2 pixel = clamp(floor(input.uv * float2(112, 144)), float2(0, 0), float2(111, 143));
                float2 atlasUV = (float2(input.frame * 112, 0) + pixel + 0.5) * _MainTex_TexelSize.xy;
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_PointClamp, atlasUV);
                clip(color.a - _Cutoff);
                return color;
            }
            ENDHLSL
        }
    }
}
