void LightGrid_float
    (UnityTexture2D source,
     float2 uv,
     float2 grid,
     float dotSize,
     out float3 outColor)
{
    // --- CONFIGURACIÓN INTERNA ---
    float3 ledColor = float3(1.0, 0.0, 0.0); // Color del LED encendido (Rojo puro)
    float3 backgroundColor = float3(0.0, 0.0, 0.0); // FONDO NEGRO ABSOLUTO
    float ledIntensity = 2.0; // Brillo controlado (evita que se queme en blanco)
    // -----------------------------------------------------------------

    // Prevenir errores si grid o dotSize son cero o no están asignados
    float2 s_grid = max(grid, float2(1.0, 1.0));
    float s_dotSize = clamp(dotSize, 0.05, 0.95);

    // Coordenadas de la cuadrícula
    float2 gc = uv * s_grid;
    float2 idx = floor(gc);

    // Muestreo de la textura (con UVs pixelados)
    float2 q_uv = idx / s_grid;
    float4 src = SAMPLE_TEXTURE2D(source.tex, source.samplerstate, q_uv);

    // Detección robusta: se enciende si tiene color rojo O si tiene transparencia/alpha
    // (Por si la Render Texture del texto de Unity usa canal alpha en vez de color)
    float isLit = saturate(max(src.r, src.a));

    // Distancia desde el centro de la bombilla
    float dist = length(frac(gc) - 0.5);

    // Borde de la bombilla más nítido y definido
    float ledShape = 1.0 - smoothstep(s_dotSize * 0.4, s_dotSize * 0.5, dist);

    // Pequeño relieve interior para dar volumen 3D a la bombilla
    float highlight = saturate((frac(gc).y - 0.3) / (s_dotSize + 0.1));
    float finalShape = ledShape * (0.4 + 0.6 * highlight);

    // Mezcla de colores final:
    // Si la textura de entrada no tiene nada (0), se pinta negro absoluto.
    // Si tiene texto, se pinta la bombilla roja con la forma (ledShape) y el brillo.
    outColor = lerp(backgroundColor, ledColor * ledIntensity, finalShape * isLit);
}