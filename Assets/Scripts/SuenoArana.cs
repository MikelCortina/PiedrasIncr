using UnityEngine;
using UnityEngine.AI;

// Una araña "duerme" cuando está en Pausa o en el Parking y lleva un rato quieta:
// pone la cara dormida (CaraLedArana) y suelta zetas. Si la patean, al levantarse se pone a trabajar.
// AranaPateable lo añade solo si el prefab no lo tiene; añádelo tú al prefab para ajustar los valores.
[DisallowMultipleComponent]
public class SuenoArana : MonoBehaviour
{
    [Header("Cuándo duerme")]
    public bool duermeEnPausa = true;
    public bool duermeEnParking = true;
    [Tooltip("Segundos quieta antes de quedarse dormida.")]
    public float tiempoParaDormirse = 2f;
    [Tooltip("Por debajo de esta velocidad se considera que está quieta.")]
    public float velocidadQuieta = 0.05f;

    [Header("Al despertar (patada)")]
    [Tooltip("Al levantarse de la patada pasa a modo Trabajar.")]
    public bool trabajarAlDespertar = true;

    [Header("Zetas")]
    [Tooltip("Opcional: tu propio sistema de partículas. Vacío = se crea uno con zetas.")]
    public ParticleSystem particulasZetas;
    [Tooltip("Posición de las zetas respecto a la araña (se escala con ella).")]
    public Vector3 desplazamientoZetas = new Vector3(0f, 1.4f, 0.5f);
    public Color colorZetas = new Color(0.8f, 0.92f, 1f, 1f);
    public float zetasPorSegundo = 0.9f;
    public float tamanoZetas = 0.4f;
    [Tooltip("Opcional: material para las zetas. Vacío = partículas sin luz con una Z dibujada.")]
    public Material materialZetas;

    public bool Dormida { get; private set; }

    private ConfiguracionBot configuracion;
    private BotRecolector recolector;
    private NavMeshAgent agente;
    private AranaPateable pateable;
    private float tiempoQuieta;

    private static Texture2D texturaZ;
    private static Material materialPorDefecto;

    private void Awake()
    {
        configuracion = GetComponent<ConfiguracionBot>();
        recolector = GetComponent<BotRecolector>();
        agente = GetComponent<NavMeshAgent>();
        pateable = GetComponent<AranaPateable>();
    }

    private void Start()
    {
        if (particulasZetas == null)
            particulasZetas = CrearZetas();

        if (particulasZetas != null)
            particulasZetas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void Update()
    {
        tiempoQuieta = PuedeDormir() ? tiempoQuieta + Time.deltaTime : 0f;
        CambiarDormida(tiempoQuieta >= tiempoParaDormirse);
    }

    private void OnDisable()
    {
        tiempoQuieta = 0f;
        CambiarDormida(false);
    }

    private bool PuedeDormir()
    {
        if (configuracion == null)
            return false;

        bool modoDeDormir =
            (duermeEnPausa && configuracion.modoActual == ConfiguracionBot.ModoBot.Pausa) ||
            (duermeEnParking && configuracion.modoActual == ConfiguracionBot.ModoBot.Parking);
        if (!modoDeDormir)
            return false;

        // Pateada, o dentro del generador (sin agente): despierta.
        if (pateable != null && pateable.EstaAnimando)
            return false;
        if (agente == null || !agente.enabled || agente.velocity.magnitude > velocidadQuieta)
            return false;

        // Cargando una piedra no se duerme.
        if (recolector != null && recolector.puntoAgarre != null &&
            recolector.puntoAgarre.GetComponentInChildren<Rigidbody>(true) != null)
            return false;

        return true;
    }

    private void CambiarDormida(bool dormida)
    {
        if (Dormida == dormida)
            return;

        Dormida = dormida;

        if (particulasZetas == null)
            return;

        if (dormida)
            particulasZetas.Play(true);
        else
            particulasZetas.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    // La llama AranaPateable cuando la araña se levanta de una patada recibida mientras dormía.
    public void Despertar()
    {
        tiempoQuieta = 0f;
        CambiarDormida(false);

        if (!trabajarAlDespertar || configuracion == null)
            return;

        configuracion.SetModoTrabajar();

        if (agente != null && agente.enabled && agente.isOnNavMesh)
            agente.isStopped = false;

        Debug.Log("[SUEÑO] " + name + " se ha despertado y se pone a trabajar.");
    }

    // ---------------------------------------------------------------- Zetas por defecto

    private ParticleSystem CrearZetas()
    {
        float escala = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));

        GameObject objeto = new GameObject("ZetasSueno");
        objeto.transform.SetParent(transform, false);
        objeto.transform.localPosition = desplazamientoZetas;

        ParticleSystem ps = objeto.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 2f;
        main.startLifetime = 2.4f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(tamanoZetas * escala * 0.8f, tamanoZetas * escala * 1.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
        main.startColor = colorZetas;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.maxParticles = 12;

        ParticleSystem.EmissionModule emision = ps.emission;
        emision.rateOverTime = zetasPorSegundo;

        ParticleSystem.ShapeModule forma = ps.shape;
        forma.enabled = false;

        // Suben despacio y se van un poco hacia un lado (las tres curvas en el mismo modo).
        ParticleSystem.VelocityOverLifetimeModule velocidad = ps.velocityOverLifetime;
        velocidad.enabled = true;
        velocidad.space = ParticleSystemSimulationSpace.World;
        velocidad.x = new ParticleSystem.MinMaxCurve(0.05f * escala, 0.2f * escala);
        velocidad.y = new ParticleSystem.MinMaxCurve(0.35f * escala, 0.5f * escala);
        velocidad.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.NoiseModule ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.15f * escala;
        ruido.frequency = 0.6f;

        ParticleSystem.SizeOverLifetimeModule tamano = ps.sizeOverLifetime;
        tamano.enabled = true;
        tamano.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(0.3f, 1f), new Keyframe(1f, 1.25f)));

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient degradado = new Gradient();
        degradado.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        color.color = degradado;

        ParticleSystemRenderer render = objeto.GetComponent<ParticleSystemRenderer>();
        render.renderMode = ParticleSystemRenderMode.Billboard;
        render.sharedMaterial = materialZetas != null ? materialZetas : MaterialPorDefecto();

        return ps;
    }

    private static Material MaterialPorDefecto()
    {
        if (materialPorDefecto != null)
            return materialPorDefecto;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        bool urp = shader != null;
        if (!urp) shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("[SUEÑO] No encuentro un shader para las zetas: asigna 'materialZetas'.");
            return null;
        }

        materialPorDefecto = new Material(shader) { name = "Zetas_Sueno" };
        materialPorDefecto.mainTexture = TexturaZ();

        if (urp)
        {
            // Transparente con mezcla alfa normal.
            materialPorDefecto.SetTexture("_BaseMap", TexturaZ());
            materialPorDefecto.SetColor("_BaseColor", Color.white);
            materialPorDefecto.SetFloat("_Surface", 1f);
            materialPorDefecto.SetFloat("_Blend", 0f);
            materialPorDefecto.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            materialPorDefecto.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            materialPorDefecto.SetFloat("_ZWrite", 0f);
            materialPorDefecto.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            materialPorDefecto.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        return materialPorDefecto;
    }

    // Una "Z" gruesa con bordes suaves sobre fondo transparente.
    private static Texture2D TexturaZ()
    {
        if (texturaZ != null)
            return texturaZ;

        const int lado = 64;
        texturaZ = new Texture2D(lado, lado, TextureFormat.RGBA32, false)
        {
            name = "Zeta",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 arribaIzq = new Vector2(12, 52), arribaDer = new Vector2(52, 52);
        Vector2 abajoIzq = new Vector2(12, 12), abajoDer = new Vector2(52, 12);
        const float grosor = 5f;

        Color[] pixeles = new Color[lado * lado];
        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Mathf.Min(DistanciaSegmento(p, arribaIzq, arribaDer),
                          Mathf.Min(DistanciaSegmento(p, arribaDer, abajoIzq),
                                    DistanciaSegmento(p, abajoIzq, abajoDer)));
                float alfa = Mathf.Clamp01(grosor + 0.5f - d);
                pixeles[y * lado + x] = new Color(1f, 1f, 1f, alfa);
            }
        }

        texturaZ.SetPixels(pixeles);
        texturaZ.Apply(false, true);
        return texturaZ;
    }

    private static float DistanciaSegmento(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * t)).magnitude;
    }
}
