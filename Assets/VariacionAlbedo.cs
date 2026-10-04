using UnityEngine;

[System.Serializable]
public struct ConfiguracionMaterial
{
    [Tooltip("El índice del material en el Mesh Renderer (0, 1, 2...)")]
    public int indiceMaterial;

    [Header("Albedo")]
    public Color nuevoColor;
    public Texture nuevaTextura;

    [Header("Cel Shading (Single)")]
    public Color colorSombreado;
    public float selfShadingSize;
    public float edgeSize;
    public float localizedShading;

    [Header("Contorno")]
    public Color colorContorno;
    public float grosorContorno;

    [Header("Unity Built-in Shadows")]
    [Tooltip("Activa esto si quieres sobreescribir las sombras desde el script.")]
    public bool modificarSombras;

    [Range(0f, 1f)] public float shadowPower;
    public Color shadowColor;
    [Range(1f, 10f)] public float shadowSharpness;
}

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class VariacionAlbedo : MonoBehaviour
{
    [Header("Lista de Materiales a Modificar")]
    public ConfiguracionMaterial[] configuraciones;

    private Renderer render;
    private MaterialPropertyBlock propBlock;

    // Variables de Contorno / Albedo / Sombras originales
    private bool estadoGuardado = false;
    private Color[] coloresOriginales;
    private float[] grosoresOriginales;

    private bool estadoColorGuardado = false;
    private Color[] coloresAlbedoOriginales;

    private bool estadoSombreadoGuardado = false;
    private Color[] coloresSombreadoOriginales;

    private bool estadoSombrasGuardado = false;
    private float[] shadowPowersOriginales;
    private Color[] shadowColoresOriginales;
    private float[] shadowSharpnessOriginales;

    void OnEnable()
    {
        AplicarMaterial();
    }

    void OnValidate()
    {
        AplicarMaterial();
    }

    // ==========================================
    // FUNCIONES DE CONTROL (Se mantienen igual)
    // ==========================================
    public void ForzarColor(Color nuevoColor) { /* Mismo código */ }
    public void RestaurarColor() { /* Mismo código */ }
    public void ForzarColorSombreado(Color nuevoColor) { /* Mismo código */ }
    public void RestaurarColorSombreado() { /* Mismo código */ }
    public void ForzarContorno(Color nuevoColor, float nuevoGrosor) { /* Mismo código */ }
    public void RestaurarContorno() { /* Mismo código */ }

    // ==========================================
    // APLICACIÓN DEL MATERIAL (Corregido)
    // ==========================================
    private void AplicarMaterial()
    {
        if (render == null) render = GetComponent<Renderer>();
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        if (configuraciones == null || configuraciones.Length == 0) return;

        for (int i = 0; i < configuraciones.Length; i++)
        {
            var config = configuraciones[i];

            // Evitar errores si el array de materiales es menor
            if (render.sharedMaterials == null || render.sharedMaterials.Length <= config.indiceMaterial) continue;

            Material matBase = render.sharedMaterials[config.indiceMaterial];
            if (matBase == null) continue;

            render.GetPropertyBlock(propBlock, config.indiceMaterial);

            // Albedo
            propBlock.SetColor("_BaseColor", config.nuevoColor);
            if (config.nuevaTextura != null)
                propBlock.SetTexture("_BaseMap", config.nuevaTextura);

            // Cel Shading
            propBlock.SetColor("_ColorDim", config.colorSombreado);
            propBlock.SetFloat("_SelfShadingSize", config.selfShadingSize);
            propBlock.SetFloat("_ShadowEdgeSize", config.edgeSize);
            propBlock.SetFloat("_Flatness", config.localizedShading);

            // Contorno
            propBlock.SetColor("_OutlineColor", config.colorContorno);
            propBlock.SetFloat("_OutlineWidth", config.grosorContorno);

            // --- UNITY BUILT-IN SHADOWS (CORREGIDO) ---
            if (config.modificarSombras)
            {
                // SINCRONIZACIÓN OBLIGATORIA: Copiamos el modo de sombra del material
                // para que Flat Kit no asuma que vale 0 (None) al inyectar el bloque.
                if (matBase.HasProperty("_UnityShadowMode"))
                {
                    float currentMode = matBase.GetFloat("_UnityShadowMode");
                    propBlock.SetFloat("_UnityShadowMode", currentMode);

                    // SISTEMA DE DEBUGGING: Mira la pestaña 'Console' en Unity si las sombras fallan.
                    if (currentMode == 0f)
                        Debug.LogWarning($"[VariacionAlbedo] El material '{matBase.name}' tiene las sombras en modo 'None'. El script no podrá mostrarlas.");
                    else if (currentMode == 1f && config.shadowPower <= 0.05f)
                        Debug.LogWarning($"[VariacionAlbedo] Material '{matBase.name}' está en 'Multiply' pero el Shadow Power es casi 0. Serán invisibles.");
                    else if (currentMode == 2f && config.shadowColor.a <= 0.05f)
                        Debug.LogWarning($"[VariacionAlbedo] Material '{matBase.name}' está en 'Color' pero el Alpha del color es casi 0. Serán invisibles.");
                }

                // Inyección segura (Sharpness nunca bajará de 1)
                propBlock.SetFloat("_UnityShadowPower", config.shadowPower);
                propBlock.SetColor("_UnityShadowColor", config.shadowColor);
                propBlock.SetFloat("_UnityShadowSharpness", Mathf.Max(1f, config.shadowSharpness));
            }

            render.SetPropertyBlock(propBlock, config.indiceMaterial);
        }
    }
}