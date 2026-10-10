Shader "Custom/GlowingParticles"
{
    Properties
    {
        [MainTexture] _BaseMap("Particle Texture", 2D) = "white" {}
        [HDR] _BaseColor("Glow Color", Color) = (1,1,1,1)
        _Intensity("Glow Intensity", Range(1, 20)) = 1.0
        
        [Space(10)]
        [Header(Blending Settings)]
        // Por defecto: Alpha Blending (SrcAlpha / OneMinusSrcAlpha)
        // Para Aditivo (fuego/magia): pon DstBlend en "One"
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", Float) = 5 
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dest Blend", Float) = 10 
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
            "IgnoreProjector" = "True"
        }

        // Configuración típica para partículas (transparentes, sin escribir en el ZBuffer, se ven por ambos lados)
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off 

        Pass
        {
            Name "Unlit"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR; // Coge el color del módulo "Color over Lifetime" del Particle System
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Intensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                // Transforma la posición del modelo a la pantalla
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                // Pasa el color de los vértices (el que configuras en el sistema de partículas)
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. Leemos la textura de la partícula
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                
                // 2. Mezclamos la Textura * El color del Particle System * Nuestro Color HDR del material
                half4 finalColor = texColor * input.color * _BaseColor;
                
                // 3. Multiplicamos SOLO los canales RGB (color) por la intensidad.
                // El canal Alpha (transparencia) lo dejamos intacto para que no se vuelva un cuadrado sólido.
                finalColor.rgb *= _Intensity; 
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}