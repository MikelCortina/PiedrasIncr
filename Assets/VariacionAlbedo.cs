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

    // Variables originales
    private bool estadoContornoGuardado = false;
    private Color[] contornoColoresOriginales;
    private float[] contornoGrosoresOriginales;

    private bool estadoColorGuardado = false;
    private Color[] coloresAlbedoOriginales;

    void OnEnable()
    {
        AplicarMaterial();
    }

    void OnValidate()
    {
        AplicarMaterial();
    }

    // ==========================================
    // FUNCIONES DE CONTROL: ALBEDO (COLOR BASE)
    // ==========================================
    public void ForzarColor(Color nuevoColor)
    {
        if (render == null) render = GetComponent<Renderer>();
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        if (!estadoColorGuardado && configuraciones != null)
        {
            estadoColorGuardado = true;
            coloresAlbedoOriginales = new Color[configuraciones.Length];

            for (int i = 0; i < configuraciones.Length; i++)
            {
                coloresAlbedoOriginales[i] = configuraciones[i].nuevoColor;
            }
        }

        if (configuraciones == null) return;

        for (int i = 0; i < configuraciones.Length; i++)
        {
            if (render.sharedMaterials == null || render.sharedMaterials.Length <= configuraciones[i].indiceMaterial) continue;

            render.GetPropertyBlock(propBlock, configuraciones[i].indiceMaterial);
            propBlock.SetColor("_BaseColor", nuevoColor);
            propBlock.SetColor("_Color", nuevoColor); // Por si usa el shader viejo estándar
            render.SetPropertyBlock(propBlock, configuraciones[i].indiceMaterial);
        }
    }

    public void RestaurarColor()
    {
        if (!estadoColorGuardado || render == null || configuraciones == null) return;

        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        for (int i = 0; i < configuraciones.Length; i++)
        {
            if (render.sharedMaterials == null || render.sharedMaterials.Length <= configuraciones[i].indiceMaterial) continue;

            render.GetPropertyBlock(propBlock, configuraciones[i].indiceMaterial);
            propBlock.SetColor("_BaseColor", coloresAlbedoOriginales[i]);
            propBlock.SetColor("_Color", coloresAlbedoOriginales[i]);
            render.SetPropertyBlock(propBlock, configuraciones[i].indiceMaterial);
        }
    }

    // ==========================================
    // FUNCIONES DE CONTROL: CONTORNOS
    // ==========================================
    public void ForzarContorno(Color nuevoColor, float nuevoGrosor)
    {
        if (render == null) render = GetComponent<Renderer>();
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        if (!estadoContornoGuardado && configuraciones != null)
        {
            estadoContornoGuardado = true;
            contornoColoresOriginales = new Color[configuraciones.Length];
            contornoGrosoresOriginales = new float[configuraciones.Length];

            for (int i = 0; i < configuraciones.Length; i++)
            {
                contornoColoresOriginales[i] = configuraciones[i].colorContorno;
                contornoGrosoresOriginales[i] = configuraciones[i].grosorContorno;
            }
        }

        if (configuraciones == null) return;

        for (int i = 0; i < configuraciones.Length; i++)
        {
            if (render.sharedMaterials == null || render.sharedMaterials.Length <= configuraciones[i].indiceMaterial) continue;

            render.GetPropertyBlock(propBlock, configuraciones[i].indiceMaterial);
            propBlock.SetColor("_OutlineColor", nuevoColor);
            propBlock.SetFloat("_OutlineWidth", nuevoGrosor);
            render.SetPropertyBlock(propBlock, configuraciones[i].indiceMaterial);
        }
    }

    public void RestaurarContorno()
    {
        if (!estadoContornoGuardado || render == null || configuraciones == null) return;

        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        for (int i = 0; i < configuraciones.Length; i++)
        {
            if (render.sharedMaterials == null || render.sharedMaterials.Length <= configuraciones[i].indiceMaterial) continue;

            render.GetPropertyBlock(propBlock, configuraciones[i].indiceMaterial);
            propBlock.SetColor("_OutlineColor", contornoColoresOriginales[i]);
            propBlock.SetFloat("_OutlineWidth", contornoGrosoresOriginales[i]);
            render.SetPropertyBlock(propBlock, configuraciones[i].indiceMaterial);
        }
    }

    // ==========================================
    // APLICACIÓN INICIAL DEL MATERIAL
    // ==========================================
    private void AplicarMaterial()
    {
        if (render == null) render = GetComponent<Renderer>();
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        if (configuraciones == null || configuraciones.Length == 0) return;

        for (int i = 0; i < configuraciones.Length; i++)
        {
            var config = configuraciones[i];

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

            // Unity Shadows
            if (config.modificarSombras)
            {
                if (matBase.HasProperty("_UnityShadowMode"))
                {
                    float currentMode = matBase.GetFloat("_UnityShadowMode");
                    propBlock.SetFloat("_UnityShadowMode", currentMode);
                }

                propBlock.SetFloat("_UnityShadowPower", config.shadowPower);
                propBlock.SetColor("_UnityShadowColor", config.shadowColor);
                propBlock.SetFloat("_UnityShadowSharpness", Mathf.Max(1f, config.shadowSharpness));
            }

            render.SetPropertyBlock(propBlock, config.indiceMaterial);
        }
    }
}