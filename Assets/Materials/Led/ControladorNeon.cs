using UnityEngine;

// Creamos una estructura para guardar los valores independientes de cada slot
[System.Serializable]
public class AjustesNeonSlot
{
    [Tooltip("Nombre para organizarte en el inspector")]
    public string nombre = "Pieza Neón";

    [Tooltip("Índice del material en el MeshRenderer (0 es el primero)")]
    public int indiceMaterial = 0;

    [Space(5)]
    [ColorUsage(true, true)]
    public Color colorNeon = new Color(236.19f/255f, 226.255f/255f, 1f);

    [Range(0f, 10f)]
    public float intensidad = 1.5f;

    [Range(0f, 10f)]
    [Tooltip("Grosor del contorno (Solo FlatKit)")]
    public float grosorOutline = 1.0f;

    [Space(5)]
    [Range(0f, 1f)]
    public float cantidadZonasMuertas = 0f;
    public float velocidadParpadeo = 0f;
}

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class ControladorNeon : MonoBehaviour
{
    [Header("Ajustes por Material")]
    [Tooltip("Añade aquí tantos slots como materiales quieras controlar individualmente")]
    public AjustesNeonSlot[] ajustesPorSlot = new AjustesNeonSlot[] { new AjustesNeonSlot() };

    // =================================================================================
    // PROPIEDADES DE COMPATIBILIDAD
    // Para que tus scripts (Ruleta, ArmaLanzadora) puedan seguir controlándolo globalmente
    // sin que den errores de código.
    // =================================================================================
    public Color colorNeon
    {
        get { return ajustesPorSlot.Length > 0 ? ajustesPorSlot[0].colorNeon : Color.black; }
        set { for (int i = 0; i < ajustesPorSlot.Length; i++) ajustesPorSlot[i].colorNeon = value; }
    }

    public float intensidad
    {
        get { return ajustesPorSlot.Length > 0 ? ajustesPorSlot[0].intensidad : 0f; }
        set { for (int i = 0; i < ajustesPorSlot.Length; i++) ajustesPorSlot[i].intensidad = value; }
    }

    private void Reset()
    {
        ajustesPorSlot = new AjustesNeonSlot[]
        {
            new AjustesNeonSlot()
            {
                nombre = "Pieza Neón",
                indiceMaterial = 0,
                colorNeon = new Color(236.19f/255f, 226.255f/255f, 1f),
                intensidad = 1.5f,
                grosorOutline = 1.0f,
                cantidadZonasMuertas = 0f,
                velocidadParpadeo = 0f
            }
        };
    }

    public float velocidadParpadeo
    {
        get { return ajustesPorSlot.Length > 0 ? ajustesPorSlot[0].velocidadParpadeo : 0f; }
        set { for (int i = 0; i < ajustesPorSlot.Length; i++) ajustesPorSlot[i].velocidadParpadeo = value; }
    }
    // =================================================================================

    private Renderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;

    // IDs CLÁSICOS (Shader original)
    private static readonly int idMainColor = Shader.PropertyToID("_MainColor");
    private static readonly int idIntensity = Shader.PropertyToID("_Intensity");
    private static readonly int idDeadZoneAmount = Shader.PropertyToID("_DeadZoneAmount");
    private static readonly int idFlickerSpeed = Shader.PropertyToID("_FlickerSpeed");

    // IDs NUEVOS FLATKIT OUTLINE
    private static readonly int idNeonColor = Shader.PropertyToID("_NeonColor");
    private static readonly int idNeonIntensity = Shader.PropertyToID("_NeonIntensity");
    private static readonly int idNeonDeadZoneAmount = Shader.PropertyToID("_NeonDeadZoneAmount");
    private static readonly int idNeonFlickerSpeed = Shader.PropertyToID("_NeonFlickerSpeed");
    private static readonly int idOutlineWidth = Shader.PropertyToID("_OutlineWidth");

    // IDs NUEVOS FLATKIT INTERIOR
    private static readonly int idBaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int idEmissionColor = Shader.PropertyToID("_EmissionColor");

    void Update()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<Renderer>();
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        if (meshRenderer.sharedMaterials == null || meshRenderer.sharedMaterials.Length == 0) return;
        if (ajustesPorSlot == null || ajustesPorSlot.Length == 0) return;

        // Recorremos la lista para aplicar las propiedades específicas de cada índice
        for (int i = 0; i < ajustesPorSlot.Length; i++)
        {
            AjustesNeonSlot ajuste = ajustesPorSlot[i];
            int index = ajuste.indiceMaterial;

            // Protegemos por si pones un número mayor a los materiales del objeto
            if (index < 0 || index >= meshRenderer.sharedMaterials.Length) continue;

            // Recogemos los datos base de este slot
            meshRenderer.GetPropertyBlock(propertyBlock, index);

            // Inyectar la intensidad en el color para este slot específico
            Color colorBrillante = ajuste.colorNeon * ajuste.intensidad;

            // 1. Shaders antiguos
            propertyBlock.SetColor(idMainColor, colorBrillante);
            propertyBlock.SetFloat(idIntensity, 1f);
            propertyBlock.SetFloat(idDeadZoneAmount, ajuste.cantidadZonasMuertas);
            propertyBlock.SetFloat(idFlickerSpeed, ajuste.velocidadParpadeo);

            // 2. Outline del nuevo FlatKit
            propertyBlock.SetColor(idNeonColor, colorBrillante);
            propertyBlock.SetFloat(idNeonIntensity, 1f);
            propertyBlock.SetFloat(idNeonDeadZoneAmount, ajuste.cantidadZonasMuertas);
            propertyBlock.SetFloat(idNeonFlickerSpeed, ajuste.velocidadParpadeo);
            propertyBlock.SetFloat(idOutlineWidth, ajuste.grosorOutline);

            // 3. INTERIOR del FlatKit
            propertyBlock.SetColor(idBaseColor, colorBrillante);
            propertyBlock.SetColor(idEmissionColor, colorBrillante);

            // Se lo asignamos SOLAMENTE a esta instancia y a este índice
            meshRenderer.SetPropertyBlock(propertyBlock, index);
        }
    }
}