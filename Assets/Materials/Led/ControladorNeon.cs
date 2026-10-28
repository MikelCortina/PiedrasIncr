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
    public Color colorNeon = new Color(236.19f / 255f, 226.255f / 255f, 1f);

    [Range(0f, 10f)]
    public float intensidad = 1.5f;

    [Range(0f, 10f)]
    [Tooltip("Grosor del contorno (Solo FlatKit)")]
    public float grosorOutline = 1.0f;

    [Space(5)]
    [Range(0f, 1f)]
    [Tooltip("Cantidad base de zonas muertas (partes apagadas)")]
    public float cantidadZonasMuertas = 0f;

    [Tooltip("Velocidad base del parpadeo del neón")]
    public float velocidadParpadeo = 0f;

    [Header("Efecto de Fallo (Glitch)")]
    [Tooltip("Si es mayor que 0, las zonas muertas cambiarán aleatoriamente de posición y cantidad, simulando un tubo roto o fallando.")]
    [Range(0f, 1f)]
    public float inestabilidadFallo = 0f;

    // Variables ocultas para manejar el tiempo del glitch internamente
    [HideInInspector] public float temporizadorFallo = 0f;
    [HideInInspector] public float offsetZonasActual = 0f;
    [HideInInspector] public float cantidadZonasActual = 0f;
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

    public float velocidadParpadeo
    {
        get { return ajustesPorSlot.Length > 0 ? ajustesPorSlot[0].velocidadParpadeo : 0f; }
        set { for (int i = 0; i < ajustesPorSlot.Length; i++) ajustesPorSlot[i].velocidadParpadeo = value; }
    }
    // =================================================================================

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
                velocidadParpadeo = 0f,
                inestabilidadFallo = 0f
            }
        };
    }

    private Renderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;

    // IDs CLÁSICOS (Shader original)
    private static readonly int idMainColor = Shader.PropertyToID("_MainColor");
    private static readonly int idIntensity = Shader.PropertyToID("_Intensity");
    private static readonly int idDeadZoneAmount = Shader.PropertyToID("_DeadZoneAmount");
    private static readonly int idFlickerSpeed = Shader.PropertyToID("_FlickerSpeed");
    private static readonly int idDeadZoneOffset = Shader.PropertyToID("_DeadZoneOffset");

    // IDs NUEVOS FLATKIT OUTLINE
    private static readonly int idNeonColor = Shader.PropertyToID("_NeonColor");
    private static readonly int idNeonIntensity = Shader.PropertyToID("_NeonIntensity");
    private static readonly int idNeonDeadZoneAmount = Shader.PropertyToID("_NeonDeadZoneAmount");
    private static readonly int idNeonFlickerSpeed = Shader.PropertyToID("_NeonFlickerSpeed");
    private static readonly int idNeonDeadZoneOffset = Shader.PropertyToID("_NeonDeadZoneOffset");
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

            if (index < 0 || index >= meshRenderer.sharedMaterials.Length) continue;

            meshRenderer.GetPropertyBlock(propertyBlock, index);

            // =========================================================
            // LÓGICA DE INESTABILIDAD (GLITCH / FALLO DE NEÓN)
            // =========================================================
            float cantidadZonasFinal = ajuste.cantidadZonasMuertas;
            float offsetZonasFinal = 0f;

            if (ajuste.inestabilidadFallo > 0f && Application.isPlaying)
            {
                // El temporizador avanza más rápido cuanta más velocidad de parpadeo tenga
                float velocidadCambio = Mathf.Max(1f, ajuste.velocidadParpadeo);
                ajuste.temporizadorFallo -= Time.deltaTime * velocidadCambio;

                if (ajuste.temporizadorFallo <= 0f)
                {
                    // Cada vez que el temporizador llega a 0, cambiamos la posición y la cantidad de las zonas muertas

                    // 1. Variar la cantidad de zonas muertas basándonos en la inestabilidad
                    float variacionMaxima = ajuste.inestabilidadFallo;
                    ajuste.cantidadZonasActual = Mathf.Clamp01(ajuste.cantidadZonasMuertas + Random.Range(-variacionMaxima, variacionMaxima));

                    // 2. Cambiar la posición (offset) aleatoriamente por todo el tubo
                    ajuste.offsetZonasActual = Random.Range(0f, 100f);

                    // 3. Reiniciar el temporizador (saltos aleatorios rápidos)
                    ajuste.temporizadorFallo = Random.Range(0.05f, 0.3f);
                }

                cantidadZonasFinal = ajuste.cantidadZonasActual;
                offsetZonasFinal = ajuste.offsetZonasActual;
            }
            // =========================================================

            Color colorBrillante = ajuste.colorNeon * ajuste.intensidad;

            // 1. Shaders antiguos
            propertyBlock.SetColor(idMainColor, colorBrillante);
            propertyBlock.SetFloat(idIntensity, 1f);
            propertyBlock.SetFloat(idDeadZoneAmount, cantidadZonasFinal);
            propertyBlock.SetFloat(idFlickerSpeed, ajuste.velocidadParpadeo);
            propertyBlock.SetFloat(idDeadZoneOffset, offsetZonasFinal);

            // 2. Outline del nuevo FlatKit
            propertyBlock.SetColor(idNeonColor, colorBrillante);
            propertyBlock.SetFloat(idNeonIntensity, 1f);
            propertyBlock.SetFloat(idNeonDeadZoneAmount, cantidadZonasFinal);
            propertyBlock.SetFloat(idNeonFlickerSpeed, ajuste.velocidadParpadeo);
            propertyBlock.SetFloat(idNeonDeadZoneOffset, offsetZonasFinal);
            propertyBlock.SetFloat(idOutlineWidth, ajuste.grosorOutline);

            // 3. INTERIOR del FlatKit
            propertyBlock.SetColor(idBaseColor, colorBrillante);
            propertyBlock.SetColor(idEmissionColor, colorBrillante);

            meshRenderer.SetPropertyBlock(propertyBlock, index);
        }
    }
}