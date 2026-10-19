using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class ControladorNeon : MonoBehaviour
{
    [Header("Ajustes del Neón Único")]
    [Tooltip("Índice del material en el MeshRenderer (0 es el primero)")]
    public int indiceMaterial = 0;

    [Space(10)]
    [ColorUsage(true, true)]
    public Color colorNeon = Color.cyan;

    [Range(0f, 10f)]
    public float intensidad = 3.0f;

    // =====================================
    // NUEVA VARIABLE PARA EL OUTLINE
    // =====================================
    [Range(0f, 10f)]
    [Tooltip("Grosor del contorno (Outline). Solo funciona con el shader de FlatKit.")]
    public float grosorOutline = 1.0f;

    [Space(10)]
    [Header("Efectos")]
    [Range(0f, 1f)]
    public float cantidadZonasMuertas = 0.15f;
    public float velocidadParpadeo = 15f;

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

    // ID PARA MODIFICAR EL GROSOR DEL OUTLINE
    private static readonly int idOutlineWidth = Shader.PropertyToID("_OutlineWidth");

    // IDs NUEVOS FLATKIT INTERIOR
    private static readonly int idBaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int idEmissionColor = Shader.PropertyToID("_EmissionColor");

    void Update()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<Renderer>();
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        if (meshRenderer.sharedMaterials == null || meshRenderer.sharedMaterials.Length == 0) return;
        if (indiceMaterial < 0 || indiceMaterial >= meshRenderer.sharedMaterials.Length) return;

        // Forzar activación de la luz de Emisión
        Material mat = meshRenderer.sharedMaterials[indiceMaterial];
        if (mat != null && !mat.IsKeywordEnabled("_EMISSION"))
        {
            mat.EnableKeyword("_EMISSION");
        }

        meshRenderer.GetPropertyBlock(propertyBlock, indiceMaterial);

        // Inyectar la intensidad directamente en el color
        Color colorBrillante = colorNeon * intensidad;

        // 1. Shaders antiguos
        propertyBlock.SetColor(idMainColor, colorBrillante);
        propertyBlock.SetFloat(idIntensity, 1f);
        propertyBlock.SetFloat(idDeadZoneAmount, cantidadZonasMuertas);
        propertyBlock.SetFloat(idFlickerSpeed, velocidadParpadeo);

        // 2. Outline del nuevo FlatKit
        propertyBlock.SetColor(idNeonColor, colorBrillante);
        propertyBlock.SetFloat(idNeonIntensity, 1f);
        propertyBlock.SetFloat(idNeonDeadZoneAmount, cantidadZonasMuertas);
        propertyBlock.SetFloat(idNeonFlickerSpeed, velocidadParpadeo);

        // APLICAMOS EL GROSOR DEL OUTLINE
        propertyBlock.SetFloat(idOutlineWidth, grosorOutline);

        // 3. INTERIOR del FlatKit
        propertyBlock.SetColor(idBaseColor, colorBrillante);
        propertyBlock.SetColor(idEmissionColor, colorBrillante);

        meshRenderer.SetPropertyBlock(propertyBlock, indiceMaterial);
    }
}