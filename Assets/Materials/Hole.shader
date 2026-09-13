Shader "HoleShader/HoleURP"
{
    SubShader
    {
        // Añadimos el tag de URP y mantenemos tu orden de renderizado (Geometry+1)
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry+1" 
        }

        Pass
        {
            Name "DepthMask"
            
            // Esto es lo que hace la magia: escribe la profundidad, pero es invisible en color
            ColorMask 0
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Librería central de URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Datos de entrada de la malla del cilindro
            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            // Datos que pasan a la pantalla
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            // El Vertex Shader calcula dónde están los vértices del cilindro en la cámara
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            // El Fragment Shader no devuelve color porque ColorMask 0 lo bloquea
            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}