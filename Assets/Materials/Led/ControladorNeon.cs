using UnityEngine;

[ExecuteAlways] // <--- ESTO ES LA CLAVE PARA VERLO EN EL EDITOR SIN DARLE A PLAY
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
    [Range(0f, 1f)]
    public float cantidadZonasMuertas = 0.15f;
    public float velocidadParpadeo = 15f;

    private Renderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;

    void Update()
    {
        // Inicializamos aquí por si se ejecuta en modo edición antes del Awake
        if (meshRenderer == null) meshRenderer = GetComponent<Renderer>();
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        // Evitamos errores si el índice no existe
        if (meshRenderer.sharedMaterials == null || meshRenderer.sharedMaterials.Length == 0) return;
        if (indiceMaterial < 0 || indiceMaterial >= meshRenderer.sharedMaterials.Length) return;

        // 1. Leemos el bloque actual
        meshRenderer.GetPropertyBlock(propertyBlock, indiceMaterial);

        // 2. Modificamos los valores (Los nombres de los strings deben coincidir con tu Shader)
        propertyBlock.SetColor("_MainColor", colorNeon);
        propertyBlock.SetFloat("_Intensity", intensidad);
        propertyBlock.SetFloat("_DeadZoneAmount", cantidadZonasMuertas);
        propertyBlock.SetFloat("_FlickerSpeed", velocidadParpadeo);

        // 3. Aplicamos los cambios al objeto
        meshRenderer.SetPropertyBlock(propertyBlock, indiceMaterial);
    }
}