Shader "Custom/Additive_Base_URP"
{
    Properties
    {
        _MainTexture ("MainTexture", 2D) = "white" {}
        _Glow_Intensity ("Glow_Intensity", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTexture);
            SAMPLER(sampler_MainTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTexture_ST;
                float _Glow_Intensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv0        : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv0         : TEXCOORD0;
                float4 vertexColor : COLOR;
                float  fogCoord    : TEXCOORD1;
            };

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTexture);
                o.vertexColor = v.color;
                o.fogCoord = ComputeFogFactor(o.positionHCS.z);
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTexture, sampler_MainTexture, i.uv0);
                float3 emissive = tex.rgb * i.vertexColor.rgb * i.vertexColor.a * _Glow_Intensity;
                float3 finalColor = MixFog(emissive, i.fogCoord);
                return float4(finalColor, 1);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}