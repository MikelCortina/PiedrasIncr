Shader "FlatKit/GradientSkybox_Stars" {

    Properties {
        _Color2 ("Top Color", Color) = (0.97, 0.67, 0.51, 0)
        _Color1 ("Bottom Color", Color) = (0, 0.7, 0.74, 0)

        [Space]
        _Intensity ("Intensity", Range (0, 2)) = 1.0
        _Exponent ("Exponent", Range (0, 3)) = 1.0

        [Space]
        _DirectionYaw ("Direction X angle", Range (0, 180)) = 0
        _DirectionPitch ("Direction Y angle", Range (0, 180)) = 0

        [Space]
        _NoiseIntensity ("Noise Intensity", Range (0, 0.25)) = 0.0

        [HideInInspector]
        _Direction ("Direction", Vector) = (0, 1, 0, 0)

        // --- NUEVAS PROPIEDADES PARA LAS ESTRELLAS ---
        [Space(20)]
        [Header(Stars)]
        [HDR] _StarColor ("Star Color (HDR for Bloom)", Color) = (1, 1, 1, 1)
        _StarDensity ("Star Density", Range(0, 1)) = 0.1
        _StarSize ("Star Size", Range(50, 1500)) = 500
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    struct appdata {
        float4 position : POSITION;
        float3 texcoord : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct v2f {
        float4 position : SV_POSITION;
        float3 texcoord : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    half4 _Color1;
    half4 _Color2;
    half3 _Direction;
    half _Intensity;
    half _Exponent;
    half _NoiseIntensity;

    // Variables de las estrellas
    half4 _StarColor;
    half _StarDensity;
    half _StarSize;

    // Función matemática para generar ruido aleatorio en 3D
    float rand3d(float3 p) {
        return frac(sin(dot(p, float3(12.9898, 78.233, 45.164))) * 43758.5453);
    }

    v2f vert (appdata v) {
        v2f o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        o.position = UnityObjectToClipPos(v.position);
        o.texcoord = v.texcoord;
        return o;
    }

    fixed4 frag (v2f i) : COLOR {
        float3 viewDir = normalize(i.texcoord);

        // 1. Lógica original del gradiente del cielo
        const half d = dot(viewDir, _Direction) * 0.5f + 0.5f;
        float t = pow(d, _Exponent);
        t += frac(sin(dot(t, float4(12.9898, 78.233, 45.164, 94.673))) * 43758.5453) * _NoiseIntensity;
        fixed4 skyColor = lerp(_Color1, _Color2, t) * _Intensity;

        // 2. Lógica de las estrellas procedurales
        // Dividimos el cielo en una cuadrícula virtual basada en _StarSize
        float3 grid = floor(viewDir * _StarSize);
        float randomVal = rand3d(grid);

        // Ajustamos la densidad para que un valor de 0 a 1 sea intuitivo (máximo 10% del cielo cubierto para no cegar)
        float threshold = 1.0 - (_StarDensity * 0.1); 

        // smoothstep hace que algunas estrellas brillen mucho y otras queden tenues (más realista)
        float starBrightness = smoothstep(threshold, 1.0, randomVal);

        // 3. Sumamos las estrellas al color del cielo
        fixed4 finalColor = skyColor + (starBrightness * _StarColor);

        return finalColor;
    }

    ENDCG

    SubShader {
        Tags { "RenderType"="Background" "Queue"="Background" }

        Pass {
            ZWrite Off
            Cull Off
            Fog { Mode Off }
            CGPROGRAM
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    }

    // ADVERTENCIA SOBRE EL INSPECTOR:
    // Si FlatKit oculta las nuevas variables de las estrellas en el inspector,
    // ponle dos barras delante a la línea de abajo (// CustomEditor) para usar el inspector normal de Unity.
    CustomEditor "GradientSkyboxEditor"
}