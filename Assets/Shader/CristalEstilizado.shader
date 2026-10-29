// Cristal con el estilo del proyecto (FlatKit): luz en escalones, borde Fresnel de corte duro con
// color HDR tipo neón, brillo especular "toon" y dos franjas diagonales de reflejo estilizado.
// Opcionalmente refracta lo que hay detrás (necesita "Opaque Texture" activado en el URP Asset).
Shader "Custom/CristalEstilizado"
{
    Properties
    {
        [Header(Cristal)]
        _ColorCristal ("Color del Cristal (alpha = opacidad)", Color) = (0.55, 0.85, 1.0, 0.25)
        _ColorSombra ("Color en Sombra", Color) = (0.35, 0.45, 0.7, 1.0)
        [IntRange] _NivelesCel ("Escalones de Luz", Range(1, 6)) = 2
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Caras Visibles (Off = ambas)", Float) = 0

        [Header(Borde Fresnel)]
        [HDR] _ColorBorde ("Color del Borde", Color) = (0.6, 1.4, 2.0, 1.0)
        _UmbralBorde ("Grosor del Borde", Range(0, 1)) = 0.55
        _SuavidadBorde ("Suavidad del Borde", Range(0.001, 0.5)) = 0.03

        [Header(Brillo Especular)]
        [HDR] _ColorBrillo ("Color del Brillo", Color) = (2.0, 2.0, 2.0, 1.0)
        _TamanoBrillo ("Tamano del Brillo", Range(0, 1)) = 0.25
        _SuavidadBrillo ("Suavidad del Brillo", Range(0.001, 0.5)) = 0.02

        [Header(Franjas de Reflejo)]
        [HDR] _ColorFranjas ("Color de las Franjas", Color) = (1.3, 1.6, 1.8, 0.6)
        _EjeFranjas ("Direccion de las Franjas (espacio local)", Vector) = (1, 1, 0, 0)
        _EscalaFranjas ("Repeticion de las Franjas", Float) = 1.5
        _AnchoFranja1 ("Ancho Franja 1", Range(0, 0.5)) = 0.12
        _SeparacionFranjas ("Separacion entre Franjas", Range(0, 0.5)) = 0.05
        _AnchoFranja2 ("Ancho Franja 2", Range(0, 0.5)) = 0.04
        _DesplazamientoFranjas ("Posicion de las Franjas", Range(0, 1)) = 0.3
        _VelocidadFranjas ("Velocidad de Barrido (0 = quietas)", Float) = 0

        [Header(Refraccion)]
        [Toggle(_REFRACCION_ON)] _Refraccion ("Refractar lo de detras (requiere Opaque Texture)", Float) = 1
        _IntensidadRefraccion ("Grosor de Refraccion (metros)", Range(0, 0.1)) = 0.02
        _TinteFondo ("Tinte del Fondo", Range(0, 1)) = 0.35
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

        Pass
        {
            Name "CristalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local _REFRACCION_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorCristal;
                half4 _ColorSombra;
                half _NivelesCel;
                half4 _ColorBorde;
                half _UmbralBorde;
                half _SuavidadBorde;
                half4 _ColorBrillo;
                half _TamanoBrillo;
                half _SuavidadBrillo;
                half4 _ColorFranjas;
                float4 _EjeFranjas;
                float _EscalaFranjas;
                half _AnchoFranja1;
                half _SeparacionFranjas;
                half _AnchoFranja2;
                half _DesplazamientoFranjas;
                float _VelocidadFranjas;
                half _IntensidadRefraccion;
                half _TinteFondo;
            CBUFFER_END

            struct Atributos
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Interpolados
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Interpolados vert(Atributos entrada)
            {
                Interpolados salida = (Interpolados)0;
                UNITY_SETUP_INSTANCE_ID(entrada);
                UNITY_TRANSFER_INSTANCE_ID(entrada, salida);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(salida);

                VertexPositionInputs posiciones = GetVertexPositionInputs(entrada.positionOS.xyz);

                salida.positionCS = posiciones.positionCS;
                salida.positionWS = posiciones.positionWS;
                salida.normalWS = TransformObjectToWorldNormal(entrada.normalOS);
                salida.positionOS = entrada.positionOS.xyz;
                salida.fogFactor = ComputeFogFactor(posiciones.positionCS.z);
                return salida;
            }

            // Escalón con antialiasing: corte duro estilo cel pero sin dientes de sierra.
            half Escalon(half borde, half valor, half suavidad)
            {
                return smoothstep(borde - suavidad, borde + suavidad, valor);
            }

            // 1 dentro de [inicio, inicio + ancho] dentro de un periodo 0..1, con bordes suavizados por pixel.
            half Banda(half f, half inicio, half ancho, half aa)
            {
                return smoothstep(inicio - aa, inicio + aa, f) * (1.0h - smoothstep(inicio + ancho - aa, inicio + ancho + aa, f));
            }

            half4 frag(Interpolados entrada, FRONT_FACE_TYPE cara : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(entrada);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(entrada);

                // Con las dos caras visibles, la cara trasera usa la normal invertida (se ve bien desde dentro de la cabina).
                float3 normal = normalize(entrada.normalWS);
                normal = IS_FRONT_VFACE(cara, normal, -normal);

                float3 vista = GetWorldSpaceNormalizeViewDir(entrada.positionWS);
                Light luz = GetMainLight();

                // --- Luz en escalones (como el cel shading de FlatKit) ---
                half nDotL = saturate(dot(normal, luz.direction));
                half niveles = max(1.0h, _NivelesCel);
                half cel = ceil(nDotL * niveles) / niveles;
                half3 colorLuz = lerp(_ColorSombra.rgb, half3(1, 1, 1), cel) * luz.color;
                half3 colorCristal = _ColorCristal.rgb * colorLuz;

                // --- Borde Fresnel de corte duro ---
                half fresnel = 1.0h - saturate(dot(normal, vista));
                half borde = Escalon(1.0h - _UmbralBorde, fresnel, _SuavidadBorde);

                // --- Brillo especular toon ---
                float3 medio = normalize(luz.direction + vista);
                half especular = saturate(dot(normal, medio));
                half brillo = Escalon(1.0h - _TamanoBrillo * 0.2h, especular, _SuavidadBrillo) * step(0.001h, nDotL);

                // --- Franjas diagonales de reflejo (en espacio local: se mueven con la pieza) ---
                float3 eje = normalize(_EjeFranjas.xyz + float3(0.0001, 0, 0));
                float coordenada = dot(entrada.positionOS, eje) * _EscalaFranjas + _Time.y * _VelocidadFranjas;
                half f = frac(coordenada);
                half aa = max(fwidth(coordenada), 0.0005);
                half franja1 = Banda(f, _DesplazamientoFranjas, _AnchoFranja1, aa);
                half franja2 = Banda(f, _DesplazamientoFranjas + _AnchoFranja1 + _SeparacionFranjas, _AnchoFranja2, aa);
                half franjas = saturate(franja1 + franja2) * (1.0h - borde);

                // Brillos que se suman encima del cristal.
                half3 brillos = borde * _ColorBorde.rgb + brillo * _ColorBrillo.rgb + franjas * _ColorFranjas.rgb;
                half opacidadBrillos = saturate(borde * _ColorBorde.a + brillo * _ColorBrillo.a + franjas * _ColorFranjas.a);

                half4 resultado;

            #if defined(_REFRACCION_ON)
                // Desplazamos el punto en el mundo (el grosor del cristal, en metros) y lo proyectamos a pantalla.
                // Un desplazamiento fijo en pantalla hacía de lupa al alejarse: la cabina ocupaba poco y el
                // desplazamiento seguía siendo el mismo, así que el interior se veía ampliado y movido.
                float3 puntoDetras = entrada.positionWS - normal * _IntensidadRefraccion;
                float4 pantallaDetras = ComputeScreenPos(TransformWorldToHClip(puntoDetras));
                float2 uvRefraccion = pantallaDetras.xy / pantallaDetras.w;
                half3 fondo = SampleSceneColor(uvRefraccion);
                fondo = lerp(fondo, fondo * _ColorCristal.rgb, _TinteFondo);

                half3 color = lerp(fondo, colorCristal, _ColorCristal.a);
                color = lerp(color, color + brillos, opacidadBrillos);
                resultado = half4(color, 1.0h);
            #else
                half3 color = colorCristal + brillos;
                resultado = half4(color, saturate(_ColorCristal.a + opacidadBrillos));
            #endif

                resultado.rgb = MixFog(resultado.rgb, entrada.fogFactor);
                return resultado;
            }
            ENDHLSL
        }
    }

    FallBack Off
}