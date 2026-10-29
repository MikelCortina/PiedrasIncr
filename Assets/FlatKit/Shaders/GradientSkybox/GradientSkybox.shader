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
        _StarRadius ("Star Radius (in cells)", Range(0.05, 0.5)) = 0.45

        [Header(Star Breathing)]
        _StarBreathSpeed ("Breath Speed", Range(0, 5)) = 0
        _StarBreathIntensity ("Breath Brightness", Range(0, 1)) = 0
        _StarBreathSize ("Breath Size", Range(0, 1)) = 0

        // --- NUBES (mismo cálculo que el TornadoShader01_Unlit) ---
        [Space(20)]
        [Header(Tornado Clouds)]
        _CloudOpacity ("Cloud Opacity", Range(0, 1)) = 0
        [HDR] _CloudColor ("Cloud Color", Color) = (1.2, 1.24, 1.3, 1)
        _CloudScale ("Cloud Scale (cells around the sky)", Range(1, 64)) = 10
        _CloudPower ("Cloud Power", Range(0.2, 4)) = 1.5
        _CloudThreshold ("Cloud Threshold", Range(0, 1)) = 0.12
        _CloudSoftness ("Cloud Softness", Range(0.001, 1)) = 0.12
        _CloudTwirl ("Twirl Amount", Range(0, 50)) = 6
        _CloudTwirlCenter ("Twirl Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _CloudTwirlSpeed ("Twirl Speed (UV/s)", Vector) = (-0.005, -0.005, 0, 0)
        _CloudShear ("Radial Shear Amount", Range(0, 30)) = 10
        _CloudNoiseSpeed ("Noise Speed (UV/s)", Vector) = (-0.01, 0.005, 0, 0)
        _CloudPoleFade ("Pole Fade", Range(0, 1)) = 0.25
        _CloudStarOcclusion ("Clouds Hide Stars", Range(0, 1)) = 1
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
    half _StarRadius;
    half _StarBreathSpeed;
    half _StarBreathIntensity;
    half _StarBreathSize;

    // Variables de las nubes
    half _CloudOpacity;
    half4 _CloudColor;
    float _CloudScale;
    half _CloudPower;
    half _CloudThreshold;
    half _CloudSoftness;
    float _CloudTwirl;
    float4 _CloudTwirlCenter;
    float4 _CloudTwirlSpeed;
    float _CloudShear;
    float4 _CloudNoiseSpeed;
    half _CloudPoleFade;
    half _CloudStarOcclusion;

    // Función matemática para generar ruido aleatorio en 3D
    float rand3d(float3 p) {
        return frac(sin(dot(p, float3(12.9898, 78.233, 45.164))) * 43758.5453);
    }

    float rand2d(float2 p) {
        return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
    }

    // Ruido de valor que se repite cada 'periodo' celdas en X: así no hay costura al dar la vuelta al cielo.
    float valorPeriodico(float2 p, float periodo) {
        float2 i = floor(p);
        float2 f = frac(p);
        f = f * f * (3.0 - 2.0 * f);
        // Módulo siempre positivo (el remolino puede llevar la U a valores negativos).
        float x0 = i.x - periodo * floor(i.x / periodo);
        float x1 = x0 + 1.0 - periodo * floor((x0 + 1.0) / periodo);
        float a = rand2d(float2(x0, i.y));
        float b = rand2d(float2(x1, i.y));
        float c = rand2d(float2(x0, i.y + 1.0));
        float d = rand2d(float2(x1, i.y + 1.0));
        return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
    }

    // Equivalente al nodo Simple Noise de Shader Graph (3 octavas), periódico en U.
    float ruidoSimplePeriodico(float2 uv, float escala) {
        float t = 0.0;
        for (int k = 0; k < 3; k++) {
            float frecuencia = exp2(k);
            float amplitud = exp2(-(3 - k));
            float celdas = max(1.0, round(escala * frecuencia));
            // La V cubre medio círculo: la mitad de celdas para que sean cuadradas.
            t += valorPeriodico(float2(uv.x * celdas, uv.y * celdas * 0.5), celdas) * amplitud;
        }
        return t;
    }

    // Diferencia con el centro teniendo en cuenta que U da la vuelta (u = 0 y u = 1 son el mismo punto).
    float2 deltaEnvuelto(float2 uv, float2 centro) {
        float2 delta = uv - centro;
        delta.x -= round(delta.x);
        return delta;
    }

    float2 twirl(float2 uv, float2 centro, float fuerza, float2 desfase) {
        float2 delta = deltaEnvuelto(uv, centro);
        float angulo = fuerza * length(delta);
        float s = sin(angulo), c = cos(angulo);
        return float2(c * delta.x - s * delta.y, s * delta.x + c * delta.y) + centro + desfase;
    }

    float2 radialShear(float2 uv, float2 centro, float fuerza, float2 desfase) {
        float2 delta = deltaEnvuelto(uv, centro);
        float delta2 = dot(delta, delta) * fuerza;
        return uv + float2(delta.y, -delta.x) * delta2 + desfase;
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

        // Respiración: cada estrella late con su propia fase y velocidad, cambiando brillo y tamaño.
        float fase = rand3d(grid + 17.31) * 6.2831853;
        float velocidad = lerp(0.6, 1.4, rand3d(grid + 41.7));
        float pulso = 0.5 + 0.5 * sin(_Time.y * _StarBreathSpeed * velocidad + fase);
        float brillo = lerp(1.0 - _StarBreathIntensity, 1.0 + _StarBreathIntensity, pulso);
        float radio = min(0.5, _StarRadius * lerp(1.0 - _StarBreathSize * 0.5, 1.0 + _StarBreathSize * 0.5, pulso));

        // Estrella redonda centrada en su celda (proyectada sobre la esfera del cielo).
        float3 centroEstrella = normalize(grid + 0.5) * _StarSize;
        float distancia = length(viewDir * _StarSize - centroEstrella);
        float suavizadoBorde = max(fwidth(distancia), 0.0001);
        float formaEstrella = 1.0 - smoothstep(radio - suavizadoBorde, radio + suavizadoBorde, distancia);
        float estrellas = starBrightness * brillo * formaEstrella;

        // 3. Nubes tornado sobre la esfera del cielo
        float nube = 0.0;
        if (_CloudOpacity > 0.0) {
            float2 uv = float2(0.5 + atan2(viewDir.z, viewDir.x) / 6.2831853, 0.5 + asin(clamp(viewDir.y, -1.0, 1.0)) / 3.1415927);
            float2 uvTwirl = twirl(uv, _CloudTwirlCenter.xy, _CloudTwirl, _CloudTwirlSpeed.xy * _Time.y);
            float2 uvShear = radialShear(uv, float2(0.5, 0.5), _CloudShear, _CloudNoiseSpeed.xy * _Time.y);
            float ruido = ruidoSimplePeriodico(uvTwirl, _CloudScale) * ruidoSimplePeriodico(uvShear, _CloudScale);
            ruido = pow(saturate(ruido), _CloudPower);
            nube = smoothstep(_CloudThreshold, _CloudThreshold + _CloudSoftness, ruido);
            // Los polos de la proyección esférica se estiran: se difuminan.
            nube *= 1.0 - smoothstep(1.0 - _CloudPoleFade, 1.0, abs(viewDir.y));
            nube *= _CloudOpacity;
        }

        // 4. Composición: estrellas detrás de las nubes
        fixed4 finalColor = skyColor + estrellas * (1.0 - nube * _CloudStarOcclusion) * _StarColor;
        finalColor = lerp(finalColor, _CloudColor, nube);

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
