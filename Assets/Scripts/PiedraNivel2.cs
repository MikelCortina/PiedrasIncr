using System;
using UnityEngine;

/// <summary>
/// Piedra de nivel 2 (hoja FINAL): cae tras agrietarse la luna, brilla distinto y el jugador
/// todavía NO puede interactuar con ella (ni patada, ni torbellino, ni agujero, ni agarre, ni bots).
///
/// Cómo queda fuera de los sistemas de juego:
/// - Tag distinto de "Piedra": la ignoran el agarre, el agujero, el torbellino, el Fisionador y el escáner.
/// - Capa fuera de la máscara de la patada (capaPiedra).
/// - Sin DeformacionPiedra / DesgastePiedra / EmisorFragmentos / RegistroPiedraGlobal: no se desgasta,
///   no da fragmentos y los bots no la ven. El prefab ya viene sin ellos; aquí se quitan por si acaso.
/// </summary>
[DisallowMultipleComponent]
public class PiedraNivel2 : MonoBehaviour
{
    /// <summary>Se lanza cada vez que aparece una piedra de nivel 2 (lo escucha ControladorFinDemo).</summary>
    public static event Action<PiedraNivel2> OnPiedraNivel2Creada;

    [Header("No interactuable")]
    [Tooltip("Tag que se le pone al nacer. Cualquiera distinto de 'Piedra'.")]
    public string tagNivel2 = "Untagged";
    [Tooltip("Capa que se le pone al nacer (0 = Default). Debe quedar fuera de la 'Capa Piedra' de SistemaPatada.")]
    [Range(0, 31)] public int capaNivel2 = 0;

    [Header("Brillo")]
    [Tooltip("Color del cuerpo de la piedra.")]
    public Color colorBase = new Color(0.25f, 0.95f, 0.85f, 1f);
    [Tooltip("Color del contorno de neón (HDR para que el bloom lo haga brillar).")]
    [ColorUsage(true, true)] public Color colorBrillo = new Color(0.3f, 2.4f, 2.1f, 1f);
    [Tooltip("Grosor del contorno de neón.")]
    public float grosorContorno = 3f;
    [Tooltip("Latidos por segundo del brillo.")]
    public float velocidadPulso = 1.6f;
    [Range(0f, 1f)] public float pulsoMinimo = 0.45f;

    [Header("Luz")]
    public bool crearLuz = true;
    public Color colorLuz = new Color(0.35f, 1f, 0.9f, 1f);
    public float intensidadLuz = 4f;
    public float rangoLuz = 8f;

    private Renderer[] renderers;
    private MaterialPropertyBlock bloque;
    private Light luz;
    private float desfase;

    private static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int IdColor = Shader.PropertyToID("_Color");
    private static readonly int IdOutlineColor = Shader.PropertyToID("_OutlineColor");
    private static readonly int IdOutlineWidth = Shader.PropertyToID("_OutlineWidth");
    private static readonly int IdEmission = Shader.PropertyToID("_EmissionColor");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEventos()
    {
        OnPiedraNivel2Creada = null;
    }

    private void Awake()
    {
        gameObject.tag = tagNivel2;
        gameObject.layer = capaNivel2;

        // Seguridad: si el prefab trae componentes de juego, se quitan antes de su primer Start.
        QuitarSiExiste<RegistroPiedraGlobal>();
        QuitarSiExiste<EmisorFragmentos>();
        QuitarSiExiste<DeformacionPiedra>();
        QuitarSiExiste<DesgastePiedra>();
        QuitarSiExiste<RescateVacioPiedra>();

        // Solo el modelo de la piedra: las estelas y partículas hijas se quedan con su aspecto.
        MeshRenderer propio = GetComponent<MeshRenderer>();
        renderers = propio != null ? new Renderer[] { propio } : GetComponentsInChildren<MeshRenderer>();
        bloque = new MaterialPropertyBlock();
        desfase = UnityEngine.Random.value * 10f;

        if (crearLuz)
        {
            GameObject objetoLuz = new GameObject("BrilloNivel2");
            objetoLuz.transform.SetParent(transform, false);
            luz = objetoLuz.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = colorLuz;
            luz.range = rangoLuz;
            luz.intensity = intensidadLuz;
            luz.shadows = LightShadows.None;
        }
    }

    private void Start()
    {
        OnPiedraNivel2Creada?.Invoke(this);
    }

    private void QuitarSiExiste<T>() where T : Component
    {
        T componente = GetComponent<T>();
        if (componente != null) Destroy(componente);
    }

    // LateUpdate: se aplica después de VariacionAlbedo, que también usa bloques de propiedades.
    private void LateUpdate()
    {
        float t = 0.5f + 0.5f * Mathf.Sin((Time.time + desfase) * velocidadPulso * Mathf.PI * 2f);
        float pulso = Mathf.Lerp(pulsoMinimo, 1f, t);
        Color brillo = colorBrillo * pulso;
        brillo.a = 1f;

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            int materiales = r.sharedMaterials.Length;
            for (int i = 0; i < materiales; i++)
            {
                r.GetPropertyBlock(bloque, i);
                bloque.SetColor(IdBaseColor, colorBase);
                bloque.SetColor(IdColor, colorBase);
                bloque.SetColor(IdOutlineColor, brillo);
                bloque.SetFloat(IdOutlineWidth, grosorContorno);
                bloque.SetColor(IdEmission, brillo * 0.5f);
                r.SetPropertyBlock(bloque, i);
            }
        }

        if (luz != null) luz.intensity = intensidadLuz * pulso;
    }
}
