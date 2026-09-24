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
}

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class VariacionAlbedo : MonoBehaviour
{
    [Header("Lista de Materiales a Modificar")]
    public ConfiguracionMaterial[] configuraciones;

    private Renderer render;
    private MaterialPropertyBlock propBlock;

    private bool estadoGuardado = false;
    private Color[] coloresOriginales;
    private float[] grosoresOriginales;

    void OnEnable()
    {
        AplicarMaterial();
    }

    void OnValidate()
    {
        AplicarMaterial();
    }
    public void ForzarContorno(Color nuevoColor, float nuevoGrosor)
    {
        if (configuraciones == null || configuraciones.Length == 0) return;

        // Si es la primera vez que lo modificamos, guardamos los valores base
        if (!estadoGuardado)
        {
            coloresOriginales = new Color[configuraciones.Length];
            grosoresOriginales = new float[configuraciones.Length];
            for (int i = 0; i < configuraciones.Length; i++)
            {
                coloresOriginales[i] = configuraciones[i].colorContorno;
                grosoresOriginales[i] = configuraciones[i].grosorContorno;
            }
            estadoGuardado = true;
        }

        // Aplicamos el nuevo color a todas las configuraciones
        for (int i = 0; i < configuraciones.Length; i++)
        {
            configuraciones[i].colorContorno = nuevoColor;
            configuraciones[i].grosorContorno = nuevoGrosor;
        }

        AplicarMaterial();
    }

    public void RestaurarContorno()
    {
        if (!estadoGuardado || configuraciones == null) return;

        // Devolvemos los colores a como estaban en el Inspector originalmente
        for (int i = 0; i < configuraciones.Length; i++)
        {
            configuraciones[i].colorContorno = coloresOriginales[i];
            configuraciones[i].grosorContorno = grosoresOriginales[i];
        }

        AplicarMaterial();
    }
    private void AplicarMaterial()
    {
        if (render == null) render = GetComponent<Renderer>();
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        if (configuraciones == null || configuraciones.Length == 0) return;

        for (int i = 0; i < configuraciones.Length; i++)
        {
            var config = configuraciones[i];

            if (render.sharedMaterials.Length <= config.indiceMaterial) continue;

            render.GetPropertyBlock(propBlock, config.indiceMaterial);

            // Albedo
            propBlock.SetColor("_BaseColor", config.nuevoColor);
            if (config.nuevaTextura != null)
            {
                propBlock.SetTexture("_BaseMap", config.nuevaTextura);
            }

            // Cel Shading
            propBlock.SetColor("_ColorDim", config.colorSombreado);
            propBlock.SetFloat("_SelfShadingSize", config.selfShadingSize);
            propBlock.SetFloat("_ShadowEdgeSize", config.edgeSize);
            propBlock.SetFloat("_Flatness", config.localizedShading);

            // Contorno
            propBlock.SetColor("_OutlineColor", config.colorContorno);
            propBlock.SetFloat("_OutlineWidth", config.grosorContorno);

            render.SetPropertyBlock(propBlock, config.indiceMaterial);
        }
    }
}