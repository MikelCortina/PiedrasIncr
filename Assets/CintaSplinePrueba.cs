using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CintaSplinePrueba : MonoBehaviour
{
    [Header("Referencias")]
    public Camera camaraPrincipal;
    public LayerMask capaSuelo;
    public Material materialHolograma;

    [Header("Forma")]
    [Min(0.2f)] public float ancho = 1.6f;
    [Min(0.02f)] public float grosor = 0.14f;
    [Range(6, 64)] public int segmentos = 24;

    [Header("Curva")]
    [Tooltip("Cuánto se estiran las tangentes de la curva. 0.35 suele dar una curva suave.")]
    [Range(0.05f, 0.8f)]
    public float fuerzaCurva = 0.35f;

    [Header("Colocación")]
    [Min(1f)] public float distanciaMaxima = 25f;
    public float velocidadAlturaRueda = 0.35f;
    public bool usarCentroPantalla = true;

    [Header("Debug")]
    public bool mostrarDebug = true;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh meshPreview;

    private bool colocando;
    private Vector3 puntoInicio;
    private Vector3 puntoFinal;
    private Vector3 direccionInicio;
    private float alturaManual;

    private readonly List<Vector3> vertices = new();
    private readonly List<int> triangulos = new();
    private readonly List<Vector2> uvs = new();

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        meshPreview = new Mesh
        {
            name = "CintaSplinePreview"
        };

        meshFilter.sharedMesh = meshPreview;

        if (materialHolograma != null)
        {
            meshRenderer.sharedMaterial = materialHolograma;
        }

        meshRenderer.enabled = false;
    }

    private void Update()
    {
        if (camaraPrincipal == null)
        {
            camaraPrincipal = Camera.main;
        }

        if (camaraPrincipal == null)
            return;

        Ray rayo = usarCentroPantalla
            ? camaraPrincipal.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))
            : camaraPrincipal.ScreenPointToRay(Input.mousePosition);

        if (!colocando)
        {
            if (Input.GetMouseButtonDown(0) &&
                IntentarObtenerPunto(rayo, out Vector3 punto))
            {
                puntoInicio = punto;
                puntoFinal = punto;

                direccionInicio = Vector3.ProjectOnPlane(
                    camaraPrincipal.transform.forward,
                    Vector3.up
                );

                if (direccionInicio.sqrMagnitude < 0.001f)
                    direccionInicio = Vector3.forward;

                direccionInicio.Normalize();

                alturaManual = 0f;
                colocando = true;
                meshRenderer.enabled = true;

                if (mostrarDebug)
                    Debug.Log("[CINTA SPLINE] Inicio fijado. Mueve la mirada y haz otro clic para colocar.");
            }

            return;
        }

        // Rueda del ratón = subir/bajar el extremo libre.
        float rueda = Input.mouseScrollDelta.y;

        if (Mathf.Abs(rueda) > 0.001f)
        {
            alturaManual += rueda * velocidadAlturaRueda;
        }

        if (IntentarObtenerPunto(rayo, out Vector3 puntoObjetivo))
        {
            puntoFinal = puntoObjetivo + Vector3.up * alturaManual;
            ReconstruirMalla();
        }

        // Segundo clic: colocar una copia definitiva del mesh actual.
        if (Input.GetMouseButtonDown(0))
        {
            ConfirmarCinta();
        }

        // Clic derecho: cancelar.
        if (Input.GetMouseButtonDown(1))
        {
            Cancelar();
        }
    }

    private bool IntentarObtenerPunto(Ray rayo, out Vector3 punto)
    {
        if (Physics.Raycast(
                rayo,
                out RaycastHit hit,
                distanciaMaxima,
                capaSuelo,
                QueryTriggerInteraction.Ignore))
        {
            punto = hit.point;
            return true;
        }

        // Si no hay suelo justo bajo la mirilla, usamos un plano horizontal
        // a la altura del punto inicial para que el extremo siga respondiendo.
        float alturaPlano = colocando ? puntoInicio.y : transform.position.y;
        Plane plano = new Plane(Vector3.up, new Vector3(0f, alturaPlano, 0f));

        if (plano.Raycast(rayo, out float distancia))
        {
            punto = rayo.GetPoint(distancia);
            return distancia <= distanciaMaxima * 2f;
        }

        punto = Vector3.zero;
        return false;
    }

    private void ReconstruirMalla()
    {
        if (Vector3.Distance(puntoInicio, puntoFinal) < 0.15f)
            return;

        int muestras = Mathf.Max(2, segmentos + 1);

        Vector3 delta = puntoFinal - puntoInicio;
        float distancia = delta.magnitude;
        float longitudTangente = distancia * fuerzaCurva;

        Vector3 direccionFinal = Vector3.ProjectOnPlane(delta, Vector3.up);

        if (direccionFinal.sqrMagnitude < 0.001f)
            direccionFinal = direccionInicio;

        direccionFinal.Normalize();

        Vector3 p0 = puntoInicio;
        Vector3 p1 = p0 + direccionInicio * longitudTangente;
        Vector3 p3 = puntoFinal;
        Vector3 p2 = p3 - direccionFinal * longitudTangente;

        vertices.Clear();
        triangulos.Clear();
        uvs.Clear();

        // Creamos dos vértices superiores y dos inferiores por muestra.
        for (int i = 0; i < muestras; i++)
        {
            float t = i / (float)(muestras - 1);

            Vector3 centro = Bezier(p0, p1, p2, p3, t);
            Vector3 tangente = BezierTangente(p0, p1, p2, p3, t).normalized;

            Vector3 lateral = Vector3.Cross(Vector3.up, tangente);

            if (lateral.sqrMagnitude < 0.001f)
                lateral = Vector3.right;

            lateral.Normalize();

            Vector3 izquierda = centro - lateral * (ancho * 0.5f);
            Vector3 derecha = centro + lateral * (ancho * 0.5f);

            Vector3 abajo = Vector3.down * grosor;

            vertices.Add(transform.InverseTransformPoint(izquierda));          // 0
            vertices.Add(transform.InverseTransformPoint(derecha));            // 1
            vertices.Add(transform.InverseTransformPoint(izquierda + abajo));  // 2
            vertices.Add(transform.InverseTransformPoint(derecha + abajo));    // 3

            float v = t * distancia;
            uvs.Add(new Vector2(0f, v));
            uvs.Add(new Vector2(1f, v));
            uvs.Add(new Vector2(0f, v));
            uvs.Add(new Vector2(1f, v));
        }

        for (int i = 0; i < muestras - 1; i++)
        {
            int a = i * 4;
            int b = (i + 1) * 4;

            // Cara superior.
            AddQuad(a, b, b + 1, a + 1);

            // Cara inferior.
            AddQuad(a + 3, b + 3, b + 2, a + 2);

            // Lateral izquierdo.
            AddQuad(a + 2, b + 2, b, a);

            // Lateral derecho.
            AddQuad(a + 1, b + 1, b + 3, a + 3);
        }

        // Tapas.
        AddQuad(2, 0, 1, 3);

        int ultimo = (muestras - 1) * 4;
        AddQuad(ultimo, ultimo + 2, ultimo + 3, ultimo + 1);

        meshPreview.Clear();
        meshPreview.SetVertices(vertices);
        meshPreview.SetTriangles(triangulos, 0);
        meshPreview.SetUVs(0, uvs);
        meshPreview.RecalculateNormals();
        meshPreview.RecalculateBounds();

        if (mostrarDebug)
        {
            Debug.DrawLine(p0, p1, Color.green);
            Debug.DrawLine(p2, p3, Color.cyan);
        }
    }

    private void ConfirmarCinta()
    {
        if (meshPreview == null || meshPreview.vertexCount < 4)
            return;

        GameObject nueva = new GameObject("CintaSpline_Colocada");

        MeshFilter mf = nueva.AddComponent<MeshFilter>();
        MeshRenderer mr = nueva.AddComponent<MeshRenderer>();
        MeshCollider mc = nueva.AddComponent<MeshCollider>();

        Mesh copia = Instantiate(meshPreview);
        copia.name = "CintaSplineMesh";

        mf.sharedMesh = copia;
        mc.sharedMesh = copia;

        if (materialHolograma != null)
            mr.sharedMaterial = materialHolograma;

        if (mostrarDebug)
        {
            Debug.Log(
                "[CINTA SPLINE] Cinta colocada. Longitud aproximada: " +
                Vector3.Distance(puntoInicio, puntoFinal).ToString("0.00") +
                " m"
            );
        }

        Cancelar();
    }

    private void Cancelar()
    {
        colocando = false;
        alturaManual = 0f;

        meshPreview.Clear();
        meshRenderer.enabled = false;
    }

    private void AddQuad(int a, int b, int c, int d)
    {
        triangulos.Add(a);
        triangulos.Add(b);
        triangulos.Add(c);

        triangulos.Add(a);
        triangulos.Add(c);
        triangulos.Add(d);
    }

    private static Vector3 Bezier(
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        float t)
    {
        float u = 1f - t;

        return
            u * u * u * p0 +
            3f * u * u * t * p1 +
            3f * u * t * t * p2 +
            t * t * t * p3;
    }

    private static Vector3 BezierTangente(
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        float t)
    {
        float u = 1f - t;

        return
            3f * u * u * (p1 - p0) +
            6f * u * t * (p2 - p1) +
            3f * t * t * (p3 - p2);
    }
}
