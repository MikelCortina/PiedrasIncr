using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Marcas de quemadura en el suelo donde impactan los meteoritos.
// No son proyectores (decals): son una malla plana pegada al suelo, así que nunca manchan
// piedras, edificios ni al jugador, y lo que esté encima las tapa con normalidad.
// Hay un máximo de marcas: al superarlo se reutiliza la más antigua. Opcionalmente se desvanecen con el tiempo.
public class GestorQuemaduras : MonoBehaviour
{
    public static GestorQuemaduras Instancia { get; private set; }

    [Header("Aspecto")]
    public Material materialQuemadura;
    [Tooltip("Diámetro mínimo y máximo de la marca (metros).")]
    public Vector2 tamano = new Vector2(3.5f, 5.5f);
    [Tooltip("Separación sobre el suelo para que no parpadee con él.")]
    public float alturaSobreSuelo = 0.02f;

    [Header("Suelo")]
    [Tooltip("Solo se marcan los objetos con este tag.")]
    public string tagSuelo = "Floor";
    [Tooltip("Distancia hacia abajo a la que se busca el suelo desde el meteorito.")]
    public float distanciaBusqueda = 15f;

    [Header("Cantidad y duración")]
    [Min(1)] public int maximoQuemaduras = 30;
    [Tooltip("Segundos que dura cada marca antes de empezar a desvanecerse (0 = no desaparecen).")]
    [Min(0f)] public float duracion = 90f;
    [Min(0.01f)] public float duracionFundido = 6f;
    [Tooltip("Segundos que tarda la marca en extenderse al aparecer.")]
    [Min(0.01f)] public float duracionAparicion = 0.2f;

    private class Quemadura
    {
        public Transform transform;
        public MeshRenderer render;
        public float nacimiento;
        public float escala;
    }

    private static readonly int IdColorBase = Shader.PropertyToID("_BaseColor");

    // Cola: la primera es siempre la más antigua.
    private readonly Queue<Quemadura> activas = new Queue<Quemadura>();
    private readonly Stack<Quemadura> libres = new Stack<Quemadura>();
    private readonly List<Quemadura> temporal = new List<Quemadura>();
    private MaterialPropertyBlock bloque;
    private Mesh quad;
    private Color colorBase = Color.white;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        bloque = new MaterialPropertyBlock();
        quad = CrearQuad();
        if (materialQuemadura != null && materialQuemadura.HasProperty(IdColorBase))
            colorBase = materialQuemadura.GetColor(IdColorBase);
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
        if (quad != null) Destroy(quad);
    }

    // Busca el suelo bajo el punto y deja una quemadura. Devuelve false si debajo no hay suelo.
    public static bool CrearBajo(Vector3 punto)
    {
        return Instancia != null && Instancia.CrearEnSuelo(punto);
    }

    public bool CrearEnSuelo(Vector3 punto)
    {
        if (materialQuemadura == null) return false;

        RaycastHit[] impactos = Physics.RaycastAll(punto + Vector3.up * 2f, Vector3.down, distanciaBusqueda + 2f, ~0, QueryTriggerInteraction.Ignore);

        bool encontrado = false;
        RaycastHit suelo = default;
        foreach (RaycastHit impacto in impactos)
        {
            if (!impacto.collider.CompareTag(tagSuelo)) continue;
            if (!encontrado || impacto.distance < suelo.distance)
            {
                suelo = impacto;
                encontrado = true;
            }
        }

        if (!encontrado) return false;

        Crear(suelo.point, suelo.normal);
        return true;
    }

    public void Crear(Vector3 puntoSuelo, Vector3 normal)
    {
        Quemadura quemadura = activas.Count >= maximoQuemaduras ? activas.Dequeue() : ObtenerLibre();

        Quaternion rotacion = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        quemadura.transform.SetPositionAndRotation(puntoSuelo + normal * alturaSobreSuelo, rotacion);
        quemadura.escala = Random.Range(tamano.x, tamano.y);
        quemadura.nacimiento = Time.time;
        quemadura.transform.localScale = Vector3.zero;
        quemadura.transform.gameObject.SetActive(true);
        AplicarAlfa(quemadura, 1f);

        activas.Enqueue(quemadura);
    }

    void Update()
    {
        if (activas.Count == 0) return;

        temporal.Clear();
        temporal.AddRange(activas);
        bool hayCaducadas = false;

        foreach (Quemadura quemadura in temporal)
        {
            float edad = Time.time - quemadura.nacimiento;

            // Aparición: se extiende rápido desde el centro.
            if (edad < duracionAparicion)
            {
                float t = 1f - Mathf.Pow(1f - edad / duracionAparicion, 3f);
                quemadura.transform.localScale = Vector3.one * (quemadura.escala * Mathf.Lerp(0.4f, 1f, t));
            }
            else if (quemadura.transform.localScale.x != quemadura.escala)
            {
                quemadura.transform.localScale = Vector3.one * quemadura.escala;
            }

            if (duracion <= 0f || edad < duracion) continue;

            float alfa = 1f - (edad - duracion) / duracionFundido;
            if (alfa > 0f)
                AplicarAlfa(quemadura, alfa);
            else
                hayCaducadas = true;
        }

        // Las caducadas son siempre las más antiguas, así que están al principio de la cola.
        while (hayCaducadas && activas.Count > 0 && Time.time - activas.Peek().nacimiento >= duracion + duracionFundido)
        {
            Quemadura caducada = activas.Dequeue();
            caducada.transform.gameObject.SetActive(false);
            libres.Push(caducada);
        }
    }

    Quemadura ObtenerLibre()
    {
        if (libres.Count > 0) return libres.Pop();

        GameObject objeto = new GameObject("Quemadura");
        objeto.transform.SetParent(transform, false);
        objeto.AddComponent<MeshFilter>().sharedMesh = quad;

        MeshRenderer render = objeto.AddComponent<MeshRenderer>();
        render.sharedMaterial = materialQuemadura;
        render.shadowCastingMode = ShadowCastingMode.Off;
        render.receiveShadows = false;
        render.lightProbeUsage = LightProbeUsage.Off;
        render.reflectionProbeUsage = ReflectionProbeUsage.Off;

        return new Quemadura { transform = objeto.transform, render = render };
    }

    void AplicarAlfa(Quemadura quemadura, float alfa)
    {
        Color color = colorBase;
        color.a *= alfa;
        bloque.SetColor(IdColorBase, color);
        quemadura.render.SetPropertyBlock(bloque);
    }

    // Plano de 1 x 1 m tumbado (mirando hacia +Y).
    static Mesh CrearQuad()
    {
        Mesh mesh = new Mesh { name = "QuadQuemadura" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f)
        };
        mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
        mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        mesh.RecalculateBounds();
        return mesh;
    }
}
