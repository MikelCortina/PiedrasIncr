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
            
            // Marca el stencil (valor 1) donde se ve la boca del agujero, sin color ni profundidad.
            // El suelo (StylizedFloorSurface 1, cola Geometry+2) no se dibuja donde está marcado,
            // así que el agujero se ve hueco y lo que cae dentro sigue viéndose entero
            // (antes la profundidad de la máscara tapaba los contornos y lo dibujado después).
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
            ColorMask 0
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Librería central de URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Datos de entrada de la malla del cilindro
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            // Datos que pasan a la pantalla
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            // El Vertex Shader calcula dónde están los vértices del cilindro en la cámara
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            // El Fragment Shader no devuelve color porque ColorMask 0 lo bloquea
            half4 frag(Varyings input) : SV_Target
            {
                // Solo marca la tapa superior del cilindro (la boca del agujero). Las paredes quedan bajo
                // el suelo: si marcasen, al mirar en diagonal se borraría el suelo de delante del agujero.
                clip(normalize(input.normalWS).y - 0.5);
                return 0;
            }
            ENDHLSL
        }
    }
}