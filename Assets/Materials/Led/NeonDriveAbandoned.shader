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
        // Se renderiza como opaco para que el Bloom lo detecte sin problemas de transparencia
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _MainColor;
            float _Intensity;
            float _FlickerSpeed;
            float _FlickerAmount;
            float _Segments;
            float _DeadZoneAmount;
            float _BarDirection;

            // Función de ruido pseudoaleatorio para generar imperfecciones
            float random(float2 st) 
            {
                return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. LÓGICA DE PARPADEO (Tubo de gas inestable)
                float t = _Time.y * _FlickerSpeed;
                float noiseFlicker = random(float2(floor(t), 0.0));
                // Interpola entre encendido (1.0) y apagado temporal usando el ruido
                float flicker = lerp(1.0, step(0.5, noiseFlicker), _FlickerAmount);

                // 2. LÓGICA DE ZONAS MUERTAS (LEDs fundidos o tubos quemados)
                // Selecciona si la barra lee las coordenadas X o Y
                float axis = lerp(i.uv.x, i.uv.y, _BarDirection);
                
                // Divide la geometría en segmentos virtuales
                float currentSegment = floor(axis * _Segments);
                
                // Usa la coordenada del segmento para generar un valor fijo y decidir si está "roto"
                float isDead = step(1.0 - _DeadZoneAmount, random(float2(currentSegment, 1.0)));
                
                // Si el segmento está roto, baja su brillo al 5% para que no sea un negro plano y retenga algo del cristal
                float damageMultiplier = lerp(1.0, 0.05, isDead);

                // 3. COLOR FINAL
                float4 finalColor = _MainColor * _Intensity * flicker * damageMultiplier;
                
                return finalColor;
            }
            ENDCG
        }
    }
}