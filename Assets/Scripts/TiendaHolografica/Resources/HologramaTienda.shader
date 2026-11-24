// Holograma de la tienda: sin luces, aditivo, con borde brillante (fresnel), líneas de escaneo
// horizontales y una banda de luz que sube. Lo usan las previsualizaciones 3D de los objetos y el
// proyector del suelo. Está en Resources para que se incluya en la build.
// El tiempo lo manda TiendaHolografica (_HoloTiempo, sin escalar) porque la tienda pausa el juego.
Shader "PiedrasIncr/HologramaTienda"
{
    Properties
    {
        _Color ("Color", Color) = (0.30, 0.92, 1, 1)
        _Intensidad ("Intensidad", Float) = 1.4
        _Alpha ("Alpha", Range(0, 1)) = 0.85
        _Borde ("Potencia del borde", Float) = 2.2
        _Lineas ("Líneas por metro", Float) = 55
        _Banda ("Banda que sube", Range(0, 1)) = 0.6
        _Plano ("Solo color (sin borde ni líneas)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;
            float _Intensidad;
            float _Alpha;
            float _Borde;
            float _Lineas;
            float _Banda;
            float _Plano;
            float _HoloTiempo;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 vista : TEXCOORD1;
                float alto : TEXCOORD2;
                float4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                // Dirección de la cámara (vale también para cámaras ortográficas).
                o.vista = -UNITY_MATRIX_V[2].xyz;
                // Altura relativa al objeto: las líneas no dependen de dónde esté en el mundo.
                float3 mundo = mul(unity_ObjectToWorld, v.vertex).xyz;
                float3 origen = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                o.alto = mundo.y - origen.y;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.normal);
                float borde = pow(1.0 - saturate(abs(dot(n, normalize(i.vista)))), _Borde);

                float lineas = 0.6 + 0.4 * sin((i.alto * _Lineas - _HoloTiempo * 3.0) * 6.2831853);
                float banda = pow(frac(i.alto * 1.5 - _HoloTiempo * 0.45), 10.0) * _Banda;

                float a = saturate((0.22 + borde * 1.3) * lineas + banda);
                float3 c = _Color.rgb * _Intensidad * (0.55 + borde) + banda * 0.8;

                // Modo plano: para el proyector (anillos y rayos con degradado por color de vértice).
                a = lerp(a, 1.0, _Plano);
                c = lerp(c, _Color.rgb * _Intensidad, _Plano);

                return fixed4(c, a * _Alpha * i.color.a * _Color.a);
            }
            ENDCG
        }
    }
    Fallback Off
}
