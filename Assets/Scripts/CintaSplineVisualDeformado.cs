using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Capa VISUAL para una CintaSpline ya construida.
///
/// IMPORTANTE:
/// - No crea colliders.
/// - No toca CintaSplineTransportador.
/// - No toca Rigidbody de las piedras.
/// - No toca conectores, snap, clearance ni destrucción.
///
/// Solo instancia el prefab visual de la cinta y deforma sus meshes
/// para que sigan la misma curva Bézier que CintaSplineDinamica.
/// </summary>
public class CintaSplineVisualDeformado : MonoBehaviour
{
    private CintaSplineDinamica spline;
    private GameObject prefabModulo;
    private float fuerzaCurva;
    private int subdivisiones;
    private bool ocultarProcedural;

    private GameObject contenedorVisual;

    private readonly List<Renderer> renderersVisuales =
        new List<Renderer>();

    private readonly List<float> distanciasArco =
        new List<float>();

    private readonly List<float> parametrosArco =
        new List<float>();

    private Vector3 p0;
    private Vector3 p1;
    private Vector3 p2;
    private Vector3 p3;

    private float longitudTotal;


    public void Construir(
        CintaSplineDinamica splineObjetivo,
        GameObject prefabVisual,
        float fuerzaCurvaSpline,
        int subdivisionesMesh,
        bool ocultarVisualProcedural)
    {
        spline = splineObjetivo;
        prefabModulo = prefabVisual;
        fuerzaCurva = Mathf.Clamp(fuerzaCurvaSpline, 0.01f, 1f);
        subdivisiones = Mathf.Clamp(subdivisionesMesh, 0, 3);
        ocultarProcedural = ocultarVisualProcedural;

        if (spline == null || prefabModulo == null)
        {
            Debug.LogWarning("[CINTA VISUAL] Falta spline o prefab visual.");
            return;
        }

        if (spline.ConectorEntrada == null ||
            spline.ConectorSalida == null)
        {
            Debug.LogWarning("[CINTA VISUAL] La spline todavía no tiene conectores.");
            return;
        }

        Transform entradaSpline = spline.ConectorEntrada.transform;
        Transform salidaSpline = spline.ConectorSalida.transform;

        p0 = entradaSpline.position;
        p3 = salidaSpline.position;

        Vector3 dirInicio = entradaSpline.forward;
        Vector3 dirFinal = salidaSpline.forward;

        if (dirInicio.sqrMagnitude < 0.0001f)
            dirInicio = Vector3.forward;

        if (dirFinal.sqrMagnitude < 0.0001f)
            dirFinal = dirInicio;

        dirInicio.Normalize();
        dirFinal.Normalize();

        float distanciaRecta = Vector3.Distance(p0, p3);
        float longitudTangente = distanciaRecta * fuerzaCurva;

        p1 = p0 + dirInicio * longitudTangente;
        p2 = p3 - dirFinal * longitudTangente;

        PrepararTablaArco();

        if (longitudTotal <= 0.0001f)
        {
            Debug.LogWarning("[CINTA VISUAL] La longitud de la spline es cero.");
            return;
        }

        Transform conectorEntradaFuente =
            BuscarHijoPorNombre(prefabModulo, "PuntoConexion_Entrada");

        Transform conectorSalidaFuente =
            BuscarHijoPorNombre(prefabModulo, "PuntoConexion_Salida");

        if (conectorEntradaFuente == null ||
            conectorSalidaFuente == null)
        {
            Debug.LogWarning(
                "[CINTA VISUAL] El prefab '" + prefabModulo.name +
                "' necesita PuntoConexion_Entrada y PuntoConexion_Salida."
            );
            return;
        }

        Vector3 entradaFuente = conectorEntradaFuente.position;
        Vector3 salidaFuente = conectorSalidaFuente.position;

        Vector3 ejeFuente = salidaFuente - entradaFuente;
        float longitudModulo = ejeFuente.magnitude;

        if (longitudModulo <= 0.0001f)
        {
            Debug.LogWarning("[CINTA VISUAL] Longitud del módulo visual inválida.");
            return;
        }

        ejeFuente.Normalize();

        Vector3 upFuente = Vector3.ProjectOnPlane(
            conectorEntradaFuente.up,
            ejeFuente
        );

        if (upFuente.sqrMagnitude < 0.0001f)
            upFuente = Vector3.ProjectOnPlane(Vector3.up, ejeFuente);

        if (upFuente.sqrMagnitude < 0.0001f)
            upFuente = Vector3.ProjectOnPlane(Vector3.right, ejeFuente);

        upFuente.Normalize();

        Quaternion baseFuente = Quaternion.LookRotation(ejeFuente, upFuente);
        Quaternion inversaBaseFuente = Quaternion.Inverse(baseFuente);
        float correccionAlturaBanda =
    CalcularCorreccionAlturaBanda(
        prefabModulo,
        entradaFuente,
        inversaBaseFuente
    );

        Debug.Log(
            "[CINTA VISUAL] Corrección vertical automática: " +
            correccionAlturaBanda.ToString("0.###") +
            " m"
        );

        int cantidadModulos = Mathf.Max(
            1,
            Mathf.RoundToInt(longitudTotal / longitudModulo)
        );

        float longitudTramo = longitudTotal / cantidadModulos;

        if (contenedorVisual != null)
            Destroy(contenedorVisual);

        renderersVisuales.Clear();

        contenedorVisual = new GameObject("VisualDeformado");
        contenedorVisual.transform.SetParent(transform, false);
        contenedorVisual.layer = gameObject.layer;

        MeshFilter[] filtrosFuente =
            prefabModulo.GetComponentsInChildren<MeshFilter>(true);

        int piezasCreadas = 0;
        bool meshNoLegible = false;

        for (int moduloIndice = 0;
             moduloIndice < cantidadModulos;
             moduloIndice++)
        {
            float distanciaInicioModulo = moduloIndice * longitudTramo;

            // Instanciamos el prefab REAL para conservar sus materiales,
            // VariacionAlbedo y el aspecto original.
            GameObject modulo = Instantiate(
                prefabModulo,
                contenedorVisual.transform,
                false
            );

            modulo.name =
                prefabModulo.name +
                "_Visual_" +
                (moduloIndice + 1).ToString("00");

            EstablecerLayerRecursivo(modulo, gameObject.layer);
            DesactivarFisicaDelVisual(modulo);

            foreach (MeshFilter filtroFuente in filtrosFuente)
            {
                if (filtroFuente == null ||
                    filtroFuente.sharedMesh == null)
                {
                    continue;
                }

                Mesh meshFuente = filtroFuente.sharedMesh;

                if (!meshFuente.isReadable)
                {
                    meshNoLegible = true;
                    continue;
                }

                string ruta = ObtenerRutaRelativa(
                    prefabModulo.transform,
                    filtroFuente.transform
                );

                Transform transformInstancia = BuscarPorRutaRelativa(
                    modulo.transform,
                    ruta
                );

                if (transformInstancia == null)
                    continue;

                MeshFilter filtroInstancia =
                    transformInstancia.GetComponent<MeshFilter>();

                MeshRenderer rendererInstancia =
                    transformInstancia.GetComponent<MeshRenderer>();

                if (filtroInstancia == null ||
                    rendererInstancia == null ||
                    !rendererInstancia.enabled)
                {
                    continue;
                }

                Mesh meshTrabajo = SubdividirMeshParaDeformacion(
                    meshFuente,
                    subdivisiones
                );

                Vector3[] vertices = meshTrabajo.vertices;
                Vector3[] deformados = new Vector3[vertices.Length];

                for (int v = 0; v < vertices.Length; v++)
                {
                    Vector3 posicionFuenteMundo =
                        filtroFuente.transform.TransformPoint(vertices[v]);

                    Vector3 offsetFuente =
                        inversaBaseFuente *
                        (posicionFuenteMundo - entradaFuente);

                    float tLongitudinal = Mathf.Clamp01(
                        offsetFuente.z / longitudModulo
                    );

                    float distanciaSpline =
                        distanciaInicioModulo +
                        tLongitudinal * longitudTramo;

                    if (!ObtenerPuntoYTangentePorDistancia(
                            distanciaSpline,
                            out Vector3 puntoSpline,
                            out Vector3 tangenteSpline))
                    {
                        deformados[v] = vertices[v];
                        continue;
                    }

                    Vector3 upSpline = CalcularUpParaTangente(tangenteSpline);

                    Quaternion baseSpline = Quaternion.LookRotation(
                        tangenteSpline,
                        upSpline
                    );

                    Vector3 posicionDeformadaMundo =
                        puntoSpline +
                        baseSpline * new Vector3(
                            offsetFuente.x,
                            offsetFuente.y - correccionAlturaBanda,
                            0f
                        );

                    deformados[v] =
                        transformInstancia.InverseTransformPoint(
                            posicionDeformadaMundo
                        );
                }

                meshTrabajo.vertices = deformados;
                meshTrabajo.RecalculateNormals();
                meshTrabajo.RecalculateBounds();

                try
                {
                    meshTrabajo.RecalculateTangents();
                }
                catch
                {
                }

                filtroInstancia.sharedMesh = meshTrabajo;
                renderersVisuales.Add(rendererInstancia);
                piezasCreadas++;
            }
        }

        if (piezasCreadas <= 0)
        {
            if (contenedorVisual != null)
            {
                Destroy(contenedorVisual);
                contenedorVisual = null;
            }

            if (meshNoLegible)
            {
                Debug.LogError(
                    "[CINTA VISUAL] Los meshes de '" + prefabModulo.name +
                    "' no tienen Read/Write activado. Actívalo en el importador del modelo."
                );
            }

            return;
        }

        if (ocultarProcedural)
            OcultarSoloRenderersProcedurales();

        Debug.Log(
            "[CINTA VISUAL] Creado visual puro | Prefab: " +
            prefabModulo.name +
            " | Módulos: " +
            cantidadModulos +
            " | Física original intacta"
        );
    }


    private void PrepararTablaArco()
    {
        distanciasArco.Clear();
        parametrosArco.Clear();

        const int muestras = 160;

        Vector3 anterior = Bezier(0f);
        float acumulada = 0f;

        distanciasArco.Add(0f);
        parametrosArco.Add(0f);

        for (int i = 1; i <= muestras; i++)
        {
            float t = i / (float)muestras;
            Vector3 actual = Bezier(t);

            acumulada += Vector3.Distance(anterior, actual);

            distanciasArco.Add(acumulada);
            parametrosArco.Add(t);

            anterior = actual;
        }

        longitudTotal = acumulada;
    }

    private static float CalcularCorreccionAlturaBanda(
    GameObject prefab,
    Vector3 entradaFuente,
    Quaternion inversaBaseFuente)
    {
        if (prefab == null)
            return 0f;

        CintaTransportadora cinta =
            prefab.GetComponentInChildren<CintaTransportadora>(true);

        if (cinta == null)
            return 0f;

        MeshFilter filtro =
            cinta.GetComponent<MeshFilter>();

        if (filtro == null)
        {
            filtro =
                cinta.GetComponentInChildren<MeshFilter>(true);
        }

        if (filtro == null ||
            filtro.sharedMesh == null ||
            !filtro.sharedMesh.isReadable)
        {
            return 0f;
        }

        Vector3[] vertices =
            filtro.sharedMesh.vertices;

        float alturaSuperior =
            float.NegativeInfinity;

        foreach (Vector3 vertice in vertices)
        {
            Vector3 posicionMundo =
                filtro.transform.TransformPoint(
                    vertice
                );

            Vector3 posicionBase =
                inversaBaseFuente *
                (
                    posicionMundo -
                    entradaFuente
                );

            alturaSuperior =
                Mathf.Max(
                    alturaSuperior,
                    posicionBase.y
                );
        }

        if (float.IsNegativeInfinity(
                alturaSuperior))
        {
            return 0f;
        }

        return alturaSuperior;
    }


    private bool ObtenerPuntoYTangentePorDistancia(
        float distancia,
        out Vector3 punto,
        out Vector3 tangente)
    {
        punto = p0;
        tangente = (p3 - p0).normalized;

        if (distanciasArco.Count < 2 || longitudTotal <= 0.0001f)
            return false;

        distancia = Mathf.Clamp(distancia, 0f, longitudTotal);

        int indice = 1;

        while (indice < distanciasArco.Count &&
               distanciasArco[indice] < distancia)
        {
            indice++;
        }

        indice = Mathf.Clamp(indice, 1, distanciasArco.Count - 1);

        float d0 = distanciasArco[indice - 1];
        float d1 = distanciasArco[indice];

        float factor = Mathf.InverseLerp(d0, d1, distancia);

        float t = Mathf.Lerp(
            parametrosArco[indice - 1],
            parametrosArco[indice],
            factor
        );

        punto = Bezier(t);
        tangente = BezierTangente(t);

        if (tangente.sqrMagnitude < 0.0001f)
            tangente = Vector3.forward;

        tangente.Normalize();
        return true;
    }


    private Vector3 Bezier(float t)
    {
        float u = 1f - t;

        return
            u * u * u * p0 +
            3f * u * u * t * p1 +
            3f * u * t * t * p2 +
            t * t * t * p3;
    }


    private Vector3 BezierTangente(float t)
    {
        float u = 1f - t;

        return
            3f * u * u * (p1 - p0) +
            6f * u * t * (p2 - p1) +
            3f * t * t * (p3 - p2);
    }


    private static Vector3 CalcularUpParaTangente(Vector3 tangente)
    {
        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, tangente);

        if (up.sqrMagnitude < 0.0001f)
            up = Vector3.ProjectOnPlane(Vector3.forward, tangente);

        if (up.sqrMagnitude < 0.0001f)
            up = Vector3.right;

        return up.normalized;
    }


    private void DesactivarFisicaDelVisual(GameObject modulo)
    {
        // OJO: esto se aplica ÚNICAMENTE a la copia visual del prefab.
        // La física real de CintaSpline no se toca.
        Collider[] colliders = modulo.GetComponentsInChildren<Collider>(true);

        foreach (Collider col in colliders)
        {
            if (col != null)
                col.enabled = false;
        }

        CintaTransportadora[] cintas =
            modulo.GetComponentsInChildren<CintaTransportadora>(true);

        foreach (CintaTransportadora cinta in cintas)
        {
            if (cinta != null)
                cinta.enabled = false;
        }

        CintaSplineTransportador[] transportadoresSpline =
            modulo.GetComponentsInChildren<CintaSplineTransportador>(true);

        foreach (CintaSplineTransportador transportador in transportadoresSpline)
        {
            if (transportador != null)
                transportador.enabled = false;
        }

        Rigidbody[] rigidbodies =
            modulo.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
        }
    }


    private void OcultarSoloRenderersProcedurales()
    {
        MeshRenderer rendererBanda = spline.GetComponent<MeshRenderer>();

        if (rendererBanda != null)
            rendererBanda.enabled = false;

        Transform railIzquierdo = spline.transform.Find("RailSpline_Izquierdo");
        Transform railDerecho = spline.transform.Find("RailSpline_Derecho");

        if (railIzquierdo != null)
        {
            MeshRenderer mr = railIzquierdo.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        if (railDerecho != null)
        {
            MeshRenderer mr = railDerecho.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }
    }


    private static Transform BuscarHijoPorNombre(
        GameObject raiz,
        string nombre)
    {
        if (raiz == null)
            return null;

        Transform[] transforms = raiz.GetComponentsInChildren<Transform>(true);

        foreach (Transform t in transforms)
        {
            if (t.name == nombre)
                return t;
        }

        return null;
    }


    private static string ObtenerRutaRelativa(
        Transform raiz,
        Transform objetivo)
    {
        if (raiz == objetivo)
            return string.Empty;

        List<string> partes = new List<string>();
        Transform actual = objetivo;

        while (actual != null && actual != raiz)
        {
            partes.Add(actual.name);
            actual = actual.parent;
        }

        if (actual != raiz)
            return string.Empty;

        partes.Reverse();
        return string.Join("/", partes.ToArray());
    }


    private static Transform BuscarPorRutaRelativa(
        Transform raiz,
        string ruta)
    {
        if (string.IsNullOrEmpty(ruta))
            return raiz;

        return raiz.Find(ruta);
    }


    private static void EstablecerLayerRecursivo(
        GameObject objeto,
        int layer)
    {
        if (objeto == null)
            return;

        objeto.layer = layer;

        foreach (Transform hijo in objeto.transform)
        {
            if (hijo != null)
                EstablecerLayerRecursivo(hijo.gameObject, layer);
        }
    }


    private Mesh SubdividirMeshParaDeformacion(
        Mesh original,
        int iteraciones)
    {
        iteraciones = Mathf.Clamp(iteraciones, 0, 3);

        Vector3[] verticesOriginales = original.vertices;
        Vector2[] uvOriginales = original.uv;

        List<Vector3> vertices =
            new List<Vector3>(verticesOriginales);

        bool tieneUV =
            uvOriginales != null &&
            uvOriginales.Length == verticesOriginales.Length;

        List<Vector2> uvs = tieneUV
            ? new List<Vector2>(uvOriginales)
            : new List<Vector2>();

        List<List<int>> triangulosPorSubmesh =
            new List<List<int>>();

        for (int s = 0; s < original.subMeshCount; s++)
        {
            triangulosPorSubmesh.Add(
                new List<int>(original.GetTriangles(s))
            );
        }

        for (int iteracion = 0;
             iteracion < iteraciones;
             iteracion++)
        {
            Dictionary<ulong, int> cacheAristas =
                new Dictionary<ulong, int>();

            List<List<int>> nuevosSubmeshes =
                new List<List<int>>();

            for (int s = 0; s < triangulosPorSubmesh.Count; s++)
            {
                List<int> origen = triangulosPorSubmesh[s];
                List<int> destino = new List<int>(origen.Count * 4);

                for (int i = 0; i + 2 < origen.Count; i += 3)
                {
                    int a = origen[i];
                    int b = origen[i + 1];
                    int c = origen[i + 2];

                    int ab = ObtenerVerticeMedio(
                        a, b, vertices, uvs, tieneUV, cacheAristas
                    );

                    int bc = ObtenerVerticeMedio(
                        b, c, vertices, uvs, tieneUV, cacheAristas
                    );

                    int ca = ObtenerVerticeMedio(
                        c, a, vertices, uvs, tieneUV, cacheAristas
                    );

                    AgregarTriangulo(destino, a, ab, ca);
                    AgregarTriangulo(destino, ab, b, bc);
                    AgregarTriangulo(destino, ca, bc, c);
                    AgregarTriangulo(destino, ab, bc, ca);
                }

                nuevosSubmeshes.Add(destino);
            }

            triangulosPorSubmesh = nuevosSubmeshes;
        }

        Mesh resultado = new Mesh();
        resultado.name = original.name + "_VisualSpline";

        if (vertices.Count > 65535)
        {
            resultado.indexFormat =
                UnityEngine.Rendering.IndexFormat.UInt32;
        }

        resultado.SetVertices(vertices);

        if (tieneUV)
            resultado.SetUVs(0, uvs);

        resultado.subMeshCount = triangulosPorSubmesh.Count;

        for (int s = 0; s < triangulosPorSubmesh.Count; s++)
        {
            resultado.SetTriangles(triangulosPorSubmesh[s], s);
        }

        return resultado;
    }


    private static int ObtenerVerticeMedio(
        int a,
        int b,
        List<Vector3> vertices,
        List<Vector2> uvs,
        bool tieneUV,
        Dictionary<ulong, int> cache)
    {
        uint menor = (uint)Mathf.Min(a, b);
        uint mayor = (uint)Mathf.Max(a, b);
        ulong clave = ((ulong)menor << 32) | mayor;

        if (cache.TryGetValue(clave, out int existente))
            return existente;

        int nuevoIndice = vertices.Count;

        vertices.Add((vertices[a] + vertices[b]) * 0.5f);

        if (tieneUV)
            uvs.Add((uvs[a] + uvs[b]) * 0.5f);

        cache.Add(clave, nuevoIndice);
        return nuevoIndice;
    }


    private static void AgregarTriangulo(
        List<int> triangulos,
        int a,
        int b,
        int c)
    {
        triangulos.Add(a);
        triangulos.Add(b);
        triangulos.Add(c);
    }
}
