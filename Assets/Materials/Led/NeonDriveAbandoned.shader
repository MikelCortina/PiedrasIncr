Shader "Custom/NeonDriveAbandoned"
{
    Properties
    {
        [HDR] _MainColor ("Color del Neon", Color) = (0.0, 1.0, 1.0, 1.0)
        _Intensity ("Intensidad General", Range(0, 10)) = 3.0
        
        [Header(Efecto Parpadeo)]
        _FlickerSpeed ("Velocidad de Parpadeo", Float) = 15.0
        _FlickerAmount ("Cantidad de Parpadeo", Range(0, 1)) = 0.3
        
        [Header(Efecto Zonas Muertas)]
        _Segments ("Numero de Segmentos", Float) = 50.0
        _DeadZoneAmount ("Cantidad de Zonas Muertas", Range(0, 1)) = 0.15
        [Toggle] _BarDirection ("Direccion Vertical", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // Habilita el soporte para instanciamiento en la GPU
            #pragma multi_compile_instancing 
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // Necesario para instanciamiento
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID // Para pasar el ID al fragment shader
            };

            // Declaración de variables preparadas para Material Property Blocks
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MainColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _Intensity)
                UNITY_DEFINE_INSTANCED_PROP(float, _FlickerSpeed)
                UNITY_DEFINE_INSTANCED_PROP(float, _FlickerAmount)
                UNITY_DEFINE_INSTANCED_PROP(float, _Segments)
                UNITY_DEFINE_INSTANCED_PROP(float, _DeadZoneAmount)
                UNITY_DEFINE_INSTANCED_PROP(float, _BarDirection)
            UNITY_INSTANCING_BUFFER_END(Props)

            // Función de ruido pseudoaleatorio para generar imperfecciones
            float random(float2 st) 
            {
                return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123);
            }

            v2f vert (appdata v)
            {
                v2f o;
                
                // Configura y transfiere el ID de la instancia
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Configura el ID en el fragment shader
                UNITY_SETUP_INSTANCE_ID(i);

                // Leemos las propiedades desde el búfer de instanciamiento
                float4 mainColor = UNITY_ACCESS_INSTANCED_PROP(Props, _MainColor);
                float intensity = UNITY_ACCESS_INSTANCED_PROP(Props, _Intensity);
                float flickerSpeed = UNITY_ACCESS_INSTANCED_PROP(Props, _FlickerSpeed);
                float flickerAmount = UNITY_ACCESS_INSTANCED_PROP(Props, _FlickerAmount);
                float segments = UNITY_ACCESS_INSTANCED_PROP(Props, _Segments);
                float deadZoneAmount = UNITY_ACCESS_INSTANCED_PROP(Props, _DeadZoneAmount);
                float barDirection = UNITY_ACCESS_INSTANCED_PROP(Props, _BarDirection);

                // 1. LÓGICA DE PARPADEO (Tubo de gas inestable)
                float t = _Time.y * flickerSpeed;
                float noiseFlicker = random(float2(floor(t), 0.0));
                float flicker = lerp(1.0, step(0.5, noiseFlicker), flickerAmount);

                // 2. LÓGICA DE ZONAS MUERTAS (LEDs fundidos o tubos quemados)
                float axis = lerp(i.uv.x, i.uv.y, barDirection);
                float currentSegment = floor(axis * segments);
                float isDead = step(1.0 - deadZoneAmount, random(float2(currentSegment, 1.0)));
                float damageMultiplier = lerp(1.0, 0.05, isDead);

                // 3. COLOR FINAL
                float4 finalColor = mainColor * intensity * flicker * damageMultiplier;
                
                return finalColor;
            }
            ENDCG
        }
    }
}