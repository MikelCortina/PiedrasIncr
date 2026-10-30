using UnityEngine;
using UnityEngine.Rendering;

// Partículas ambientales globales: polvo flotando y pequeños destellos.
// Se emiten en una caja que acompaña a la cámara y se simulan en el mundo,
// así con pocas partículas siempre hay ambiente alrededor del jugador.
// Crea y configura sus dos ParticleSystem hijos ("Polvo" y "Destellos").
[ExecuteAlways]
public class ParticulasAmbientales : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Cámara a la que siguen las partículas (vacío = la cámara principal).")]
    public Transform camara;
    public Material materialPolvo;
    public Material materialDestellos;

    [Header("Zona de emisión (alrededor de la cámara)")]
    public Vector3 tamanoZona = new Vector3(30f, 6f, 30f);
    [Tooltip("Desplazamiento de la zona respecto a la cámara (negativo en Y = más cerca del suelo).")]
    public Vector3 desplazamientoZona = new Vector3(0f, -0.5f, 0f);

    [Header("Polvo")]
    [Min(0)] public int cantidadPolvo = 70;
    public Vector2 tamanoPolvo = new Vector2(0.04f, 0.12f);
    public Vector2 vidaPolvo = new Vector2(8f, 14f);
    public Color colorPolvo = new Color(1f, 0.92f, 0.78f, 0.35f);
    [Tooltip("Fuerza del movimiento ondulante del polvo.")]
    public float derivaPolvo = 0.25f;

    [Header("Destellos")]
    [Min(0)] public int cantidadDestellos = 20;
    public Vector2 tamanoDestellos = new Vector2(0.03f, 0.08f);
    public Vector2 vidaDestellos = new Vector2(1.5f, 3.5f);
    [ColorUsage(true, true)] public Color colorDestellos = new Color(1f, 0.95f, 0.75f, 1f);
    public float derivaDestellos = 0.15f;

    private ParticleSystem polvo;
    private ParticleSystem destellos;

    void OnEnable()
    {
        Configurar();
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Configurar(); };
#endif
    }

    void LateUpdate()
    {
        // En el editor no se mueve, para no marcar la escena como modificada sin parar.
        if (!Application.isPlaying) return;

        Transform objetivo = camara;
        if (objetivo == null && Camera.main != null) objetivo = Camera.main.transform;
        if (objetivo == null) return;

        // Solo se mueve el emisor: las partículas ya emitidas se quedan en el mundo.
        transform.position = objetivo.position + desplazamientoZona;
    }

    [ContextMenu("Aplicar configuración")]
    public void Configurar()
    {
        polvo = ObtenerSistema("Polvo");
        destellos = ObtenerSistema("Destellos");

        ConfigurarSistema(polvo, cantidadPolvo, vidaPolvo, tamanoPolvo, colorPolvo, derivaPolvo, materialPolvo, false);
        ConfigurarSistema(destellos, cantidadDestellos, vidaDestellos, tamanoDestellos, colorDestellos, derivaDestellos, materialDestellos, true);
    }

    ParticleSystem ObtenerSistema(string nombre)
    {
        Transform hijo = transform.Find(nombre);
        if (hijo == null)
        {
            hijo = new GameObject(nombre).transform;
            hijo.SetParent(transform, false);
        }

        ParticleSystem sistema = hijo.GetComponent<ParticleSystem>();
        if (sistema == null) sistema = hijo.gameObject.AddComponent<ParticleSystem>();
        return sistema;
    }

    void ConfigurarSistema(ParticleSystem sistema, int cantidad, Vector2 vida, Vector2 tamano, Color color, float deriva, Material material, bool parpadeo)
    {
        var principal = sistema.main;
        principal.loop = true;
        principal.prewarm = true;
        principal.playOnAwake = true;
        principal.simulationSpace = ParticleSystemSimulationSpace.World;
        principal.maxParticles = Mathf.Max(1, cantidad);
        principal.startLifetime = new ParticleSystem.MinMaxCurve(vida.x, vida.y);
        principal.startSize = new ParticleSystem.MinMaxCurve(tamano.x, tamano.y);
        principal.startSpeed = 0f;
        principal.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        principal.startColor = color;
        principal.gravityModifier = 0f;

        // Ritmo justo para mantener "cantidad" partículas vivas.
        var emision = sistema.emission;
        emision.enabled = cantidad > 0;
        emision.rateOverTime = cantidad / Mathf.Max(0.1f, (vida.x + vida.y) * 0.5f);

        var forma = sistema.shape;
        forma.enabled = true;
        forma.shapeType = ParticleSystemShapeType.Box;
        forma.scale = tamanoZona;
        forma.position = Vector3.zero;

        // Movimiento suave y orgánico.
        var ruido = sistema.noise;
        ruido.enabled = deriva > 0f;
        ruido.strength = deriva;
        ruido.frequency = 0.25f;
        ruido.scrollSpeed = 0.1f;
        ruido.quality = ParticleSystemNoiseQuality.Low;

        // Entrada y salida con fundido para que nunca aparezcan de golpe.
        var colorVida = sistema.colorOverLifetime;
        colorVida.enabled = true;
        Gradient degradado = new Gradient();
        degradado.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, parpadeo ? 0.5f : 0.2f), new GradientAlphaKey(1f, parpadeo ? 0.5f : 0.8f), new GradientAlphaKey(0f, 1f) });
        colorVida.color = degradado;

        // Los destellos crecen y se apagan (parpadeo).
        var tamanoVida = sistema.sizeOverLifetime;
        tamanoVida.enabled = parpadeo;
        if (parpadeo)
        {
            AnimationCurve curva = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));
            tamanoVida.size = new ParticleSystem.MinMaxCurve(1f, curva);
        }

        var render = sistema.GetComponent<ParticleSystemRenderer>();
        render.renderMode = ParticleSystemRenderMode.Billboard;
        render.shadowCastingMode = ShadowCastingMode.Off;
        render.receiveShadows = false;
        render.lightProbeUsage = LightProbeUsage.Off;
        render.reflectionProbeUsage = ReflectionProbeUsage.Off;
        if (material != null) render.sharedMaterial = material;

        if (Application.isPlaying && !sistema.isPlaying) sistema.Play();
    }
}
