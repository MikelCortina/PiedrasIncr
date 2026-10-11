using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CintaSplineDinamica : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private Mesh meshBanda;
    private Mesh meshRailIzquierdo;
    private Mesh meshRailDerecho;

    private GameObject railIzquierdoGO;
    private GameObject railDerechoGO;

    private MeshFilter railIzquierdoMF;
    private MeshFilter railDerechoMF;

    private MeshRenderer railIzquierdoMR;
    private MeshRenderer railDerechoMR;

    private Material materialBandaInstancia;
    private Material materialRailIzquierdoInstancia;
    private Material materialRailDerechoInstancia;

    private Vector3 puntoInicio;
    private Vector3 puntoFinal;
    private Vector3 direccionInicio;
    private Vector3 direccionFinal;

    // ancho = ancho TOTAL de cinta, incluyendo railes
    private float ancho;
    private float grosor;
    private int segmentos;
    private float fuerzaCurva;

    private float anchoRail = 0.16f;
    private float altoRail = 0.10f;
    private float separacionRailBanda = 0.035f;

    private bool esPreview;

    private MaterialPropertyBlock bloqueResaltadoTemporal;

    private GameObject contenedorVisualModular;

    private readonly List<Renderer> renderersVisualModular =
        new List<Renderer>();

    private readonly List<Renderer> renderersBandaVisualDeformada =
        new List<Renderer>();


    private readonly List<Vector3> centros =
        new List<Vector3>();

    private readonly List<Vector3> tangentes =
        new List<Vector3>();

    private readonly List<Vector3> laterales =
        new List<Vector3>();


    public Collider ConectorEntrada
    {
        get;
        private set;
    }


    public Collider ConectorSalida
    {
        get;
        private set;
    }


    private void Awake()
    {
        PrepararComponentes();
    }


    private void PrepararComponentes()
    {
        if (meshFilter == null)
        {
            meshFilter =
                GetComponent<MeshFilter>();
        }


        if (meshRenderer == null)
        {
            meshRenderer =
                GetComponent<MeshRenderer>();
        }


        if (meshBanda == null)
        {
            meshBanda =
                new Mesh();

            meshBanda.name =
                "CintaSpline_Banda";

            meshFilter.sharedMesh =
                meshBanda;
        }


        PrepararRailes();
    }


    private void PrepararRailes()
    {
        if (railIzquierdoGO == null)
        {
            railIzquierdoGO =
                new GameObject(
                    "RailSpline_Izquierdo"
                );

            railIzquierdoGO.transform.SetParent(
                transform,
                false
            );

            railIzquierdoGO.layer =
                gameObject.layer;

            railIzquierdoMF =
                railIzquierdoGO.AddComponent<MeshFilter>();

            railIzquierdoMR =
                railIzquierdoGO.AddComponent<MeshRenderer>();

            meshRailIzquierdo =
                new Mesh();

            meshRailIzquierdo.name =
                "CintaSpline_RailIzquierdo";

            railIzquierdoMF.sharedMesh =
                meshRailIzquierdo;
        }


        if (railDerechoGO == null)
        {
            railDerechoGO =
                new GameObject(
                    "RailSpline_Derecho"
                );

            railDerechoGO.transform.SetParent(
                transform,
                false
            );

            railDerechoGO.layer =
                gameObject.layer;

            railDerechoMF =
                railDerechoGO.AddComponent<MeshFilter>();

            railDerechoMR =
                railDerechoGO.AddComponent<MeshRenderer>();

            meshRailDerecho =
                new Mesh();

            meshRailDerecho.name =
                "CintaSpline_RailDerecho";

            railDerechoMF.sharedMesh =
                meshRailDerecho;
        }
    }


    public void ConfigurarMateriales(
        Material materialBanda,
        Material materialRail)
    {
        PrepararComponentes();


        if (materialBanda != null)
        {
            materialBandaInstancia =
                new Material(
                    materialBanda
                );

            meshRenderer.sharedMaterial =
                materialBandaInstancia;
        }


        if (materialRail == null)
        {
            materialRail =
                materialBanda;
        }


        if (materialRail != null)
        {
            materialRailIzquierdoInstancia =
                new Material(
                    materialRail
                );

            materialRailDerechoInstancia =
                new Material(
                    materialRail
                );


            railIzquierdoMR.sharedMaterial =
                materialRailIzquierdoInstancia;

            railDerechoMR.sharedMaterial =
                materialRailDerechoInstancia;
        }
    }


    public void ConfigurarVisual(
        float nuevoAnchoRail,
        float nuevoAltoRail,
        float nuevaSeparacion)
    {
        anchoRail =
            Mathf.Max(
                0.02f,
                nuevoAnchoRail
            );

        altoRail =
            Mathf.Max(
                0.01f,
                nuevoAltoRail
            );

        separacionRailBanda =
            Mathf.Max(
                0f,
                nuevaSeparacion
            );
    }


    public void ConfigurarComoPreview()
    {
        esPreview =
            true;


        Collider[] colliders =
            GetComponentsInChildren<Collider>(true);


        foreach (Collider col in colliders)
        {
            col.enabled =
                false;
        }
    }


    public void ConfigurarCurva(
        Vector3 inicio,
        Vector3 final,
        Vector3 dirInicio,
        Vector3 dirFinal,
        float nuevoAncho,
        float nuevoGrosor,
        int nuevosSegmentos,
        float nuevaFuerzaCurva)
    {
        PrepararComponentes();


        puntoInicio =
            inicio;

        puntoFinal =
            final;


        direccionInicio =
            dirInicio.sqrMagnitude >
            0.0001f
            ? dirInicio.normalized
            : Vector3.forward;


        direccionFinal =
            dirFinal.sqrMagnitude >
            0.0001f
            ? dirFinal.normalized
            : direccionInicio;


        ancho =
            Mathf.Max(
                0.20f,
                nuevoAncho
            );


        grosor =
            Mathf.Max(
                0.01f,
                nuevoGrosor
            );


        segmentos =
            Mathf.Max(
                2,
                nuevosSegmentos
            );


        fuerzaCurva =
            Mathf.Clamp(
                nuevaFuerzaCurva,
                0.01f,
                1f
            );


        transform.position =
            puntoInicio;

        transform.rotation =
            Quaternion.identity;


        // Asegura que los hijos hereden la layer de la cinta real.
        if (railIzquierdoGO != null)
            railIzquierdoGO.layer = gameObject.layer;

        if (railDerechoGO != null)
            railDerechoGO.layer = gameObject.layer;


        ReconstruirMallas();
    }


    public void EstablecerEstadoValido(
        bool valido,
        Color colorValido,
        Color colorInvalido)
    {
        Color color =
            valido
            ? colorValido
            : colorInvalido;


        color.a =
            0.50f;


        AplicarColorMaterial(
            materialBandaInstancia,
            color
        );


        AplicarColorMaterial(
            materialRailIzquierdoInstancia,
            color
        );


        AplicarColorMaterial(
            materialRailDerechoInstancia,
            color
        );
    }


    private static void AplicarColorMaterial(
        Material material,
        Color color)
    {
        if (material == null)
            return;


        if (material.HasProperty(
                "_BaseColor"))
        {
            material.SetColor(
                "_BaseColor",
                color
            );
        }


        if (material.HasProperty(
                "_Color"))
        {
            material.SetColor(
                "_Color",
                color
            );
        }
    }


    public void ForzarResaltadoTemporal(
        Color color)
    {
        PrepararComponentes();


        if (bloqueResaltadoTemporal == null)
        {
            bloqueResaltadoTemporal =
                new MaterialPropertyBlock();
        }


        bloqueResaltadoTemporal.Clear();


        color.a =
            1f;


        // URP Lit / Shader Graph suelen usar _BaseColor.
        bloqueResaltadoTemporal.SetColor(
            "_BaseColor",
            color
        );


        // Standard y otros shaders pueden usar _Color.
        bloqueResaltadoTemporal.SetColor(
            "_Color",
            color
        );


        AplicarBloqueResaltado(
            meshRenderer
        );


        AplicarBloqueResaltado(
            railIzquierdoMR
        );


        AplicarBloqueResaltado(
            railDerechoMR
        );


        foreach (Renderer renderer in renderersVisualModular)
        {
            AplicarBloqueResaltado(
                renderer
            );
        }
    }


    public void RestaurarResaltadoTemporal()
    {
        PrepararComponentes();


        MaterialPropertyBlock vacio =
            new MaterialPropertyBlock();


        if (meshRenderer != null)
        {
            meshRenderer.SetPropertyBlock(
                vacio
            );
        }


        if (railIzquierdoMR != null)
        {
            railIzquierdoMR.SetPropertyBlock(
                vacio
            );
        }


        if (railDerechoMR != null)
        {
            railDerechoMR.SetPropertyBlock(
                vacio
            );
        }


        foreach (Renderer renderer in renderersVisualModular)
        {
            if (renderer != null)
            {
                renderer.SetPropertyBlock(
                    vacio
                );
            }
        }
    }


    private void AplicarBloqueResaltado(
        Renderer renderer)
    {
        if (renderer == null ||
            bloqueResaltadoTemporal == null)
        {
            return;
        }


        renderer.SetPropertyBlock(
            bloqueResaltadoTemporal
        );
    }


    private void ReconstruirMallas()
    {
        float distancia =
            Vector3.Distance(
                puntoInicio,
                puntoFinal
            );


        if (distancia <
            0.02f)
        {
            meshBanda.Clear();
            meshRailIzquierdo.Clear();
            meshRailDerecho.Clear();

            return;
        }


        Vector3 p0 =
            puntoInicio;

        Vector3 p3 =
            puntoFinal;


        float longitudTangente =
            distancia *
            fuerzaCurva;


        Vector3 p1 =
            p0 +
            direccionInicio *
            longitudTangente;


        Vector3 p2 =
            p3 -
            direccionFinal *
            longitudTangente;


        int muestras =
            segmentos + 1;


        centros.Clear();
        tangentes.Clear();
        laterales.Clear();


        float longitudAcumulada =
            0f;

        Vector3 centroAnterior =
            p0;


        for (int i = 0;
             i < muestras;
             i++)
        {
            float t =
                i /
                (float)(muestras - 1);


            Vector3 centro =
                Bezier(
                    p0,
                    p1,
                    p2,
                    p3,
                    t
                );


            Vector3 tangente =
                BezierTangente(
                    p0,
                    p1,
                    p2,
                    p3,
                    t
                );


            if (tangente.sqrMagnitude <
                0.0001f)
            {
                tangente =
                    direccionInicio;
            }


            tangente.Normalize();


            Vector3 lateral =
                Vector3.Cross(
                    Vector3.up,
                    tangente
                );


            if (lateral.sqrMagnitude <
                0.0001f)
            {
                lateral =
                    Vector3.right;
            }


            lateral.Normalize();


            if (i > 0)
            {
                longitudAcumulada +=
                    Vector3.Distance(
                        centroAnterior,
                        centro
                    );
            }


            centroAnterior =
                centro;


            centros.Add(
                centro
            );

            tangentes.Add(
                tangente
            );

            laterales.Add(
                lateral
            );
        }


        float anchoBanda =
            ancho -
            2f *
            (
                anchoRail +
                separacionRailBanda
            );


        anchoBanda =
            Mathf.Max(
                0.10f,
                anchoBanda
            );


        ConstruirMallaBanda(
            anchoBanda
        );


        float offsetRail =
            anchoBanda * 0.5f +
            separacionRailBanda +
            anchoRail * 0.5f;


        ConstruirMallaRail(
            meshRailIzquierdo,
            -offsetRail
        );


        ConstruirMallaRail(
            meshRailDerecho,
            offsetRail
        );
    }


    private void ConstruirMallaBanda(
        float anchoBanda)
    {
        int muestras =
            centros.Count;


        List<Vector3> vertices =
            new List<Vector3>(
                muestras * 4
            );


        List<Vector2> uvs =
            new List<Vector2>(
                muestras * 4
            );


        List<int> triangulos =
            new List<int>(
                segmentos * 24
            );


        float longitudUV =
            0f;


        for (int i = 0;
             i < muestras;
             i++)
        {
            if (i > 0)
            {
                longitudUV +=
                    Vector3.Distance(
                        centros[i - 1],
                        centros[i]
                    );
            }


            Vector3 centro =
                centros[i];

            Vector3 lateral =
                laterales[i];


            Vector3 izquierda =
                centro -
                lateral *
                (anchoBanda * 0.5f);


            Vector3 derecha =
                centro +
                lateral *
                (anchoBanda * 0.5f);


            Vector3 abajo =
                Vector3.down *
                grosor;


            vertices.Add(
                izquierda -
                puntoInicio
            );

            vertices.Add(
                derecha -
                puntoInicio
            );

            vertices.Add(
                izquierda +
                abajo -
                puntoInicio
            );

            vertices.Add(
                derecha +
                abajo -
                puntoInicio
            );


            uvs.Add(
                new Vector2(
                    0f,
                    longitudUV
                )
            );

            uvs.Add(
                new Vector2(
                    1f,
                    longitudUV
                )
            );

            uvs.Add(
                new Vector2(
                    0f,
                    longitudUV
                )
            );

            uvs.Add(
                new Vector2(
                    1f,
                    longitudUV
                )
            );
        }


        ConstruirPrismaLongitudinal(
            meshBanda,
            vertices,
            uvs,
            triangulos,
            muestras
        );
    }


    private void ConstruirMallaRail(
        Mesh destino,
        float offsetLateral)
    {
        int muestras =
            centros.Count;


        List<Vector3> vertices =
            new List<Vector3>(
                muestras * 4
            );


        List<Vector2> uvs =
            new List<Vector2>(
                muestras * 4
            );


        List<int> triangulos =
            new List<int>(
                segmentos * 24
            );


        float longitudUV =
            0f;


        for (int i = 0;
             i < muestras;
             i++)
        {
            if (i > 0)
            {
                longitudUV +=
                    Vector3.Distance(
                        centros[i - 1],
                        centros[i]
                    );
            }


            Vector3 centro =
                centros[i];

            Vector3 lateral =
                laterales[i];


            Vector3 centroRail =
                centro +
                lateral *
                offsetLateral;


            Vector3 ladoA =
                centroRail -
                lateral *
                (anchoRail * 0.5f);


            Vector3 ladoB =
                centroRail +
                lateral *
                (anchoRail * 0.5f);


            Vector3 arriba =
                Vector3.up *
                altoRail;


            // El rail nace en la superficie de la banda y sube.
            vertices.Add(
                ladoA -
                puntoInicio
            );

            vertices.Add(
                ladoB -
                puntoInicio
            );

            vertices.Add(
                ladoA +
                arriba -
                puntoInicio
            );

            vertices.Add(
                ladoB +
                arriba -
                puntoInicio
            );


            uvs.Add(
                new Vector2(
                    0f,
                    longitudUV
                )
            );

            uvs.Add(
                new Vector2(
                    1f,
                    longitudUV
                )
            );

            uvs.Add(
                new Vector2(
                    0f,
                    longitudUV
                )
            );

            uvs.Add(
                new Vector2(
                    1f,
                    longitudUV
                )
            );
        }


        ConstruirPrismaRail(
            destino,
            vertices,
            uvs,
            triangulos,
            muestras
        );
    }


    private static void ConstruirPrismaLongitudinal(
        Mesh destino,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<int> triangulos,
        int muestras)
    {
        for (int i = 0;
             i < muestras - 1;
             i++)
        {
            int a =
                i * 4;

            int b =
                (i + 1) * 4;


            // Superior
            AddQuad(
                triangulos,
                a,
                b,
                b + 1,
                a + 1
            );

            // Inferior
            AddQuad(
                triangulos,
                a + 3,
                b + 3,
                b + 2,
                a + 2
            );

            // Lateral A
            AddQuad(
                triangulos,
                a + 2,
                b + 2,
                b,
                a
            );

            // Lateral B
            AddQuad(
                triangulos,
                a + 1,
                b + 1,
                b + 3,
                a + 3
            );
        }


        AddQuad(
            triangulos,
            2,
            0,
            1,
            3
        );


        int ultimo =
            (muestras - 1) *
            4;


        AddQuad(
            triangulos,
            ultimo,
            ultimo + 2,
            ultimo + 3,
            ultimo + 1
        );


        destino.Clear();

        destino.SetVertices(
            vertices
        );

        destino.SetUVs(
            0,
            uvs
        );

        destino.SetTriangles(
            triangulos,
            0
        );

        destino.RecalculateNormals();
        destino.RecalculateBounds();
    }


    private static void ConstruirPrismaRail(
        Mesh destino,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<int> triangulos,
        int muestras)
    {
        // Orden por muestra:
        // 0 = abajo A
        // 1 = abajo B
        // 2 = arriba A
        // 3 = arriba B
        for (int i = 0;
             i < muestras - 1;
             i++)
        {
            int a =
                i * 4;

            int b =
                (i + 1) * 4;


            // Cara superior
            AddQuad(
                triangulos,
                a + 2,
                a + 3,
                b + 3,
                b + 2
            );

            // Cara inferior
            AddQuad(
                triangulos,
                a + 1,
                a,
                b,
                b + 1
            );

            // Exterior A
            AddQuad(
                triangulos,
                a,
                a + 2,
                b + 2,
                b
            );

            // Exterior B
            AddQuad(
                triangulos,
                a + 3,
                a + 1,
                b + 1,
                b + 3
            );
        }


        // Tapas
        AddQuad(
            triangulos,
            0,
            1,
            3,
            2
        );


        int ultimo =
            (muestras - 1) *
            4;


        AddQuad(
            triangulos,
            ultimo + 2,
            ultimo + 3,
            ultimo + 1,
            ultimo
        );


        destino.Clear();

        destino.SetVertices(
            vertices
        );

        destino.SetUVs(
            0,
            uvs
        );

        destino.SetTriangles(
            triangulos,
            0
        );

        destino.RecalculateNormals();
        destino.RecalculateBounds();
    }


    public bool ConstruirVisualDeformado(
        GameObject prefabModulo,
        float longitudModulo,
        int subdivisiones)
    {
        if (prefabModulo == null ||
            longitudModulo <= 0.0001f ||
            centros.Count < 2)
        {
            return false;
        }


        Transform conectorEntrada =
            BuscarHijoPorNombre(
                prefabModulo,
                "PuntoConexion_Entrada"
            );


        Transform conectorSalida =
            BuscarHijoPorNombre(
                prefabModulo,
                "PuntoConexion_Salida"
            );


        if (conectorEntrada == null ||
            conectorSalida == null)
        {
            Debug.LogWarning(
                "[CINTA SPLINE V9.1] No puedo deformar '" +
                prefabModulo.name +
                "': faltan PuntoConexion_Entrada o PuntoConexion_Salida."
            );

            return false;
        }


        Vector3 entradaFuente =
            conectorEntrada.position;

        Vector3 salidaFuente =
            conectorSalida.position;


        Vector3 ejeFuente =
            salidaFuente -
            entradaFuente;


        float longitudFuente =
            ejeFuente.magnitude;


        if (longitudFuente <
            0.0001f)
        {
            return false;
        }


        ejeFuente.Normalize();


        Vector3 upFuente =
            Vector3.ProjectOnPlane(
                conectorEntrada.up,
                ejeFuente
            );


        if (upFuente.sqrMagnitude <
            0.0001f)
        {
            upFuente =
                Vector3.ProjectOnPlane(
                    Vector3.up,
                    ejeFuente
                );
        }


        if (upFuente.sqrMagnitude <
            0.0001f)
        {
            upFuente =
                Vector3.ProjectOnPlane(
                    Vector3.right,
                    ejeFuente
                );
        }


        upFuente.Normalize();


        Quaternion baseFuente =
            Quaternion.LookRotation(
                ejeFuente,
                upFuente
            );


        Quaternion inversaBaseFuente =
            Quaternion.Inverse(
                baseFuente
            );


        float longitudTotal =
            ObtenerLongitudAproximada();


        if (longitudTotal <=
            0.0001f)
        {
            return false;
        }


        int cantidadModulos =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    longitudTotal /
                    longitudModulo
                )
            );


        float longitudTramoObjetivo =
            longitudTotal /
            cantidadModulos;


        if (contenedorVisualModular != null)
        {
            Destroy(
                contenedorVisualModular
            );
        }


        renderersVisualModular.Clear();
        renderersBandaVisualDeformada.Clear();


        contenedorVisualModular =
            new GameObject(
                "VisualDeformado"
            );


        contenedorVisualModular.transform.SetParent(
            transform,
            false
        );


        contenedorVisualModular.layer =
            gameObject.layer;


        MeshFilter[] filtrosFuente =
            prefabModulo.GetComponentsInChildren<MeshFilter>(
                true
            );


        int piezasCreadas =
            0;

        bool encontroMeshNoLegible =
            false;


        for (int moduloIndice = 0;
             moduloIndice < cantidadModulos;
             moduloIndice++)
        {
            float distanciaInicioModulo =
                moduloIndice *
                longitudTramoObjetivo;


            // IMPORTANTE:
            // Instanciamos el prefab REAL en vez de reconstruir renderers vacíos.
            // Así conservamos exactamente sus materiales y dejamos activo
            // VariacionAlbedo, que en tu prefab puede modificar el material.
            GameObject modulo =
                Instantiate(
                    prefabModulo,
                    contenedorVisualModular.transform,
                    false
                );


            modulo.name =
                prefabModulo.name +
                "_Deformado_" +
                (moduloIndice + 1).ToString("00");


            EstablecerLayerRecursivo(
                modulo,
                gameObject.layer
            );


            // Desactivamos primero los colliders originales y la lógica
            // funcional del prefab. Los nuevos colliders seguirán el mesh curvo.
            Collider[] collidersOriginales =
                modulo.GetComponentsInChildren<Collider>(
                    true
                );


            foreach (Collider col in collidersOriginales)
            {
                if (col != null)
                {
                    col.enabled =
                        false;
                }
            }


            CintaTransportadora[] cintasOriginales =
                modulo.GetComponentsInChildren<CintaTransportadora>(
                    true
                );


            foreach (CintaTransportadora cintaOriginal in cintasOriginales)
            {
                if (cintaOriginal != null)
                {
                    cintaOriginal.enabled =
                        false;
                }
            }


            Rigidbody[] rigidbodies =
                modulo.GetComponentsInChildren<Rigidbody>(
                    true
                );


            foreach (Rigidbody rb in rigidbodies)
            {
                if (rb != null)
                {
                    rb.isKinematic =
                        true;

                    rb.detectCollisions =
                        false;
                }
            }


            foreach (MeshFilter filtroFuente in filtrosFuente)
            {
                if (filtroFuente == null ||
                    filtroFuente.sharedMesh == null)
                {
                    continue;
                }


                Mesh meshFuente =
                    filtroFuente.sharedMesh;


                if (!meshFuente.isReadable)
                {
                    encontroMeshNoLegible =
                        true;

                    continue;
                }


                string ruta =
                    ObtenerRutaRelativa(
                        prefabModulo.transform,
                        filtroFuente.transform
                    );


                Transform transformInstancia =
                    BuscarPorRutaRelativa(
                        modulo.transform,
                        ruta
                    );


                if (transformInstancia == null)
                {
                    continue;
                }


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


                Mesh meshTrabajo =
                    SubdividirMeshParaDeformacion(
                        meshFuente,
                        subdivisiones
                    );


                Vector3[] vertices =
                    meshTrabajo.vertices;


                Vector3[] verticesDeformados =
                    new Vector3[
                        vertices.Length
                    ];


                for (int v = 0;
                     v < vertices.Length;
                     v++)
                {
                    Vector3 posicionFuenteMundo =
                        filtroFuente.transform.TransformPoint(
                            vertices[v]
                        );


                    Vector3 offsetFuente =
                        inversaBaseFuente *
                        (
                            posicionFuenteMundo -
                            entradaFuente
                        );


                    float tLongitudinal =
                        offsetFuente.z /
                        longitudFuente;


                    tLongitudinal =
                        Mathf.Clamp01(
                            tLongitudinal
                        );


                    float distanciaSpline =
                        distanciaInicioModulo +
                        tLongitudinal *
                        longitudTramoObjetivo;


                    if (!ObtenerPuntoYTangentePorDistancia(
                            distanciaSpline,
                            out Vector3 puntoSpline,
                            out Vector3 tangenteSpline))
                    {
                        verticesDeformados[v] =
                            vertices[v];

                        continue;
                    }


                    Vector3 upSpline =
                        CalcularUpParaTangente(
                            tangenteSpline
                        );


                    Quaternion baseSpline =
                        Quaternion.LookRotation(
                            tangenteSpline,
                            upSpline
                        );


                    Vector3 posicionDeformadaMundo =
                        puntoSpline +
                        baseSpline *
                        new Vector3(
                            offsetFuente.x,
                            offsetFuente.y,
                            0f
                        );


                    // Guardamos cada vértice en el espacio local del MeshFilter
                    // DE LA INSTANCIA. Así mantenemos su renderer/materiales y
                    // toda la jerarquía original del prefab.
                    verticesDeformados[v] =
                        transformInstancia.InverseTransformPoint(
                            posicionDeformadaMundo
                        );
                }


                meshTrabajo.vertices =
                    verticesDeformados;


                meshTrabajo.RecalculateNormals();
                meshTrabajo.RecalculateBounds();


                try
                {
                    meshTrabajo.RecalculateTangents();
                }
                catch
                {
                }


                filtroInstancia.sharedMesh =
                    meshTrabajo;


                renderersVisualModular.Add(
                    rendererInstancia
                );


                bool esBanda =
                    filtroFuente.GetComponent<CintaTransportadora>() != null;


                if (esBanda)
                {
                    renderersBandaVisualDeformada.Add(
                        rendererInstancia
                    );
                }


                // V9.3:
                // El prefab deformado es SOLO VISUAL.
                //
                // No creamos colliders aquí. La física vuelve a usar
                // exclusivamente el MeshCollider procedural de la spline,
                // que es el sistema que ya estaba probado y movía las piedras
                // correctamente antes de introducir la deformación visual.
                piezasCreadas++;
            }
        }


        if (piezasCreadas == 0)
        {
            if (encontroMeshNoLegible)
            {
                Debug.LogError(
                    "[CINTA SPLINE V9.1] Los meshes de '" +
                    prefabModulo.name +
                    "' necesitan Read/Write Enabled."
                );
            }


            return false;
        }


        Debug.Log(
            "[CINTA SPLINE V9.3] Visual deformado creado | Prefab: " +
            prefabModulo.name +
            " | Módulos: " +
            cantidadModulos +
            " | Física procedural conservada"
        );


        return true;
    }


    private void EstablecerLayerRecursivo(
        GameObject raiz,
        int layer)
    {
        if (raiz == null)
        {
            return;
        }


        Transform[] transforms =
            raiz.GetComponentsInChildren<Transform>(
                true
            );


        foreach (Transform t in transforms)
        {
            t.gameObject.layer =
                layer;
        }
    }


    private string ObtenerRutaRelativa(
        Transform raiz,
        Transform objetivo)
    {
        if (raiz == null ||
            objetivo == null)
        {
            return string.Empty;
        }


        if (raiz ==
            objetivo)
        {
            return string.Empty;
        }


        List<string> partes =
            new List<string>();


        Transform actual =
            objetivo;


        while (actual != null &&
               actual != raiz)
        {
            partes.Add(
                actual.name
            );


            actual =
                actual.parent;
        }


        partes.Reverse();


        return string.Join(
            "/",
            partes
        );
    }


    private Transform BuscarPorRutaRelativa(
        Transform raiz,
        string ruta)
    {
        if (raiz == null)
        {
            return null;
        }


        if (string.IsNullOrEmpty(
                ruta))
        {
            return raiz;
        }


        return raiz.Find(
            ruta
        );
    }


    private Vector3 CalcularUpParaTangente(
        Vector3 tangente)
    {
        Vector3 up =
            Vector3.ProjectOnPlane(
                Vector3.up,
                tangente
            );


        if (up.sqrMagnitude <
            0.0001f)
        {
            up =
                Vector3.ProjectOnPlane(
                    transform.forward,
                    tangente
                );
        }


        if (up.sqrMagnitude <
            0.0001f)
        {
            up =
                Vector3.right;
        }


        return up.normalized;
    }


    private Mesh SubdividirMeshParaDeformacion(
        Mesh original,
        int iteraciones)
    {
        iteraciones =
            Mathf.Clamp(
                iteraciones,
                0,
                3
            );


        Vector3[] verticesOriginales =
            original.vertices;


        Vector2[] uvOriginales =
            original.uv;


        List<Vector3> vertices =
            new List<Vector3>(
                verticesOriginales
            );


        bool tieneUV =
            uvOriginales != null &&
            uvOriginales.Length ==
            verticesOriginales.Length;


        List<Vector2> uvs =
            tieneUV
            ? new List<Vector2>(
                uvOriginales
            )
            : new List<Vector2>();


        List<List<int>> triangulosPorSubmesh =
            new List<List<int>>();


        for (int s = 0;
             s < original.subMeshCount;
             s++)
        {
            triangulosPorSubmesh.Add(
                new List<int>(
                    original.GetTriangles(
                        s
                    )
                )
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


            for (int s = 0;
                 s < triangulosPorSubmesh.Count;
                 s++)
            {
                List<int> origen =
                    triangulosPorSubmesh[s];


                List<int> destino =
                    new List<int>(
                        origen.Count *
                        4
                    );


                for (int i = 0;
                     i + 2 < origen.Count;
                     i += 3)
                {
                    int a =
                        origen[i];

                    int b =
                        origen[i + 1];

                    int c =
                        origen[i + 2];


                    int ab =
                        ObtenerVerticeMedio(
                            a,
                            b,
                            vertices,
                            uvs,
                            tieneUV,
                            cacheAristas
                        );


                    int bc =
                        ObtenerVerticeMedio(
                            b,
                            c,
                            vertices,
                            uvs,
                            tieneUV,
                            cacheAristas
                        );


                    int ca =
                        ObtenerVerticeMedio(
                            c,
                            a,
                            vertices,
                            uvs,
                            tieneUV,
                            cacheAristas
                        );


                    AgregarTriangulo(
                        destino,
                        a,
                        ab,
                        ca
                    );


                    AgregarTriangulo(
                        destino,
                        ab,
                        b,
                        bc
                    );


                    AgregarTriangulo(
                        destino,
                        ca,
                        bc,
                        c
                    );


                    AgregarTriangulo(
                        destino,
                        ab,
                        bc,
                        ca
                    );
                }


                nuevosSubmeshes.Add(
                    destino
                );
            }


            triangulosPorSubmesh =
                nuevosSubmeshes;
        }


        Mesh resultado =
            new Mesh();


        resultado.name =
            original.name +
            "_SplineDeformado";


        if (vertices.Count >
            65535)
        {
            resultado.indexFormat =
                UnityEngine.Rendering.IndexFormat.UInt32;
        }


        resultado.SetVertices(
            vertices
        );


        if (tieneUV)
        {
            resultado.SetUVs(
                0,
                uvs
            );
        }


        resultado.subMeshCount =
            triangulosPorSubmesh.Count;


        for (int s = 0;
             s < triangulosPorSubmesh.Count;
             s++)
        {
            resultado.SetTriangles(
                triangulosPorSubmesh[s],
                s
            );
        }


        return resultado;
    }


    private int ObtenerVerticeMedio(
        int a,
        int b,
        List<Vector3> vertices,
        List<Vector2> uvs,
        bool tieneUV,
        Dictionary<ulong, int> cache)
    {
        uint menor =
            (uint)Mathf.Min(
                a,
                b
            );


        uint mayor =
            (uint)Mathf.Max(
                a,
                b
            );


        ulong clave =
            (
                (ulong)menor <<
                32
            ) |
            mayor;


        if (cache.TryGetValue(
                clave,
                out int existente))
        {
            return existente;
        }


        int nuevoIndice =
            vertices.Count;


        vertices.Add(
            (
                vertices[a] +
                vertices[b]
            ) *
            0.5f
        );


        if (tieneUV)
        {
            uvs.Add(
                (
                    uvs[a] +
                    uvs[b]
                ) *
                0.5f
            );
        }


        cache.Add(
            clave,
            nuevoIndice
        );


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


    public Renderer[] ObtenerRenderersBandaVisual()
    {
        return
            renderersBandaVisualDeformada.ToArray();
    }


    public void EstablecerVisualProceduralVisible(
        bool visible)
    {
        PrepararComponentes();


        if (meshRenderer != null)
        {
            meshRenderer.enabled =
                visible;
        }


        if (railIzquierdoMR != null)
        {
            railIzquierdoMR.enabled =
                visible;
        }


        if (railDerechoMR != null)
        {
            railDerechoMR.enabled =
                visible;
        }
    }


    private void PrepararModuloSoloVisual(
        GameObject modulo)
    {
        if (modulo == null)
        {
            return;
        }


        Collider[] colliders =
            modulo.GetComponentsInChildren<Collider>(
                true
            );


        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.enabled =
                    false;
            }
        }


        Rigidbody[] rigidbodies =
            modulo.GetComponentsInChildren<Rigidbody>(
                true
            );


        foreach (Rigidbody rb in rigidbodies)
        {
            if (rb != null)
            {
                rb.isKinematic =
                    true;

                rb.detectCollisions =
                    false;
            }
        }


        // El prefab repetido solo aporta su geometría/materiales.
        // Toda la lógica real sigue perteneciendo a la spline principal.
        MonoBehaviour[] comportamientos =
            modulo.GetComponentsInChildren<MonoBehaviour>(
                true
            );


        foreach (MonoBehaviour comportamiento in comportamientos)
        {
            if (comportamiento != null)
            {
                comportamiento.enabled =
                    false;
            }
        }
    }


    private Transform BuscarHijoPorNombre(
        GameObject raiz,
        string nombre)
    {
        if (raiz == null)
        {
            return null;
        }


        Transform[] hijos =
            raiz.GetComponentsInChildren<Transform>(
                true
            );


        foreach (Transform hijo in hijos)
        {
            if (hijo.name ==
                nombre)
            {
                return hijo;
            }
        }


        return null;
    }


    public bool ObtenerPuntoYTangentePorDistancia(
        float distanciaObjetivo,
        out Vector3 punto,
        out Vector3 tangente)
    {
        punto =
            puntoInicio;

        tangente =
            direccionInicio;


        if (centros.Count < 2)
        {
            return false;
        }


        float longitudTotal =
            ObtenerLongitudAproximada();


        distanciaObjetivo =
            Mathf.Clamp(
                distanciaObjetivo,
                0f,
                longitudTotal
            );


        float acumulada =
            0f;


        for (int i = 0;
             i < centros.Count - 1;
             i++)
        {
            Vector3 a =
                centros[i];

            Vector3 b =
                centros[i + 1];


            float longitudTramo =
                Vector3.Distance(
                    a,
                    b
                );


            if (longitudTramo <
                0.000001f)
            {
                continue;
            }


            if (acumulada +
                longitudTramo >=
                distanciaObjetivo)
            {
                float t =
                    (
                        distanciaObjetivo -
                        acumulada
                    ) /
                    longitudTramo;


                punto =
                    Vector3.Lerp(
                        a,
                        b,
                        t
                    );


                Vector3 tangenteA =
                    tangentes[
                        Mathf.Min(
                            i,
                            tangentes.Count - 1
                        )
                    ];


                Vector3 tangenteB =
                    tangentes[
                        Mathf.Min(
                            i + 1,
                            tangentes.Count - 1
                        )
                    ];


                tangente =
                    Vector3.Slerp(
                        tangenteA,
                        tangenteB,
                        t
                    );


                if (tangente.sqrMagnitude <
                    0.0001f)
                {
                    tangente =
                        (
                            b -
                            a
                        ).normalized;
                }
                else
                {
                    tangente.Normalize();
                }


                return true;
            }


            acumulada +=
                longitudTramo;
        }


        punto =
            centros[
                centros.Count - 1
            ];


        tangente =
            tangentes[
                tangentes.Count - 1
            ].normalized;


        return true;
    }


    public void ConstruirFisicaYConectores(
        float velocidad,
        bool animarTextura,
        Vector2 direccionTextura,
        float multiplicadorTextura,
        int layerConector,
        float tamanoConector)
    {
        PrepararComponentes();


        MeshCollider meshCollider =
            GetComponent<MeshCollider>();


        if (meshCollider == null)
        {
            meshCollider =
                gameObject.AddComponent<MeshCollider>();
        }


        // El collider principal corresponde a la banda.
        meshCollider.sharedMesh =
            meshBanda;


        // Los railes también bloquean físicamente las piedras.
        CrearOActualizarColliderRail(
            railIzquierdoGO,
            meshRailIzquierdo
        );


        CrearOActualizarColliderRail(
            railDerechoGO,
            meshRailDerecho
        );


        CintaSplineTransportador transportador =
            GetComponent<CintaSplineTransportador>();


        if (transportador == null)
        {
            transportador =
                gameObject.AddComponent<CintaSplineTransportador>();
        }


        transportador.spline =
            this;

        transportador.velocidad =
            velocidad;

        transportador.animarTextura =
            animarTextura;

        transportador.direccionTextura =
            direccionTextura;

        transportador.multiplicadorSincronizacion =
            multiplicadorTextura;


        ConectorEntrada =
            CrearConector(
                "PuntoConexion_Entrada",
                puntoInicio,
                direccionInicio,
                "ConectorEntrada",
                layerConector,
                tamanoConector
            );


        ConectorSalida =
            CrearConector(
                "PuntoConexion_Salida",
                puntoFinal,
                direccionFinal,
                "ConectorSalida",
                layerConector,
                tamanoConector
            );
    }


    private static void CrearOActualizarColliderRail(
        GameObject rail,
        Mesh meshRail)
    {
        if (rail == null ||
            meshRail == null)
        {
            return;
        }


        MeshCollider collider =
            rail.GetComponent<MeshCollider>();


        if (collider == null)
        {
            collider =
                rail.AddComponent<MeshCollider>();
        }


        collider.sharedMesh =
            meshRail;
    }


    private Collider CrearConector(
        string nombre,
        Vector3 posicion,
        Vector3 direccion,
        string tagConector,
        int layer,
        float tamano)
    {
        GameObject go =
            new GameObject(
                nombre
            );


        go.transform.SetParent(
            transform,
            true
        );


        go.transform.position =
            posicion;


        Vector3 forward =
            direccion;


        if (forward.sqrMagnitude <
            0.0001f)
        {
            forward =
                Vector3.forward;
        }


        go.transform.rotation =
            Quaternion.LookRotation(
                forward.normalized,
                Vector3.up
            );


        go.layer =
            layer;

        go.tag =
            tagConector;


        BoxCollider col =
            go.AddComponent<BoxCollider>();


        float lado =
            Mathf.Max(
                0.05f,
                tamano
            );


        col.size =
            Vector3.one *
            lado;

        col.isTrigger =
            true;


        return col;
    }


    public bool ValidarLimitesGeometricos(
        float radioMinimoPermitido,
        float pendienteMaximaGrados,
        bool mostrarDebug,
        out float radioMinimoDetectado,
        out float pendienteMaximaDetectada)
    {
        radioMinimoDetectado =
            float.PositiveInfinity;

        pendienteMaximaDetectada =
            0f;


        if (centros.Count < 2)
        {
            return true;
        }


        bool pendienteValida =
            true;

        bool curvaturaValida =
            true;


        // =================================================
        // PENDIENTE
        // =================================================
        // Medimos cada tramo pequeño de la spline.
        for (int i = 0;
             i < centros.Count - 1;
             i++)
        {
            Vector3 delta =
                centros[i + 1] -
                centros[i];


            float distanciaHorizontal =
                new Vector2(
                    delta.x,
                    delta.z
                ).magnitude;


            float pendienteGrados;


            if (distanciaHorizontal <
                0.0001f)
            {
                pendienteGrados =
                    Mathf.Abs(delta.y) >
                    0.0001f
                    ? 90f
                    : 0f;
            }
            else
            {
                pendienteGrados =
                    Mathf.Abs(
                        Mathf.Atan2(
                            delta.y,
                            distanciaHorizontal
                        ) *
                        Mathf.Rad2Deg
                    );
            }


            pendienteMaximaDetectada =
                Mathf.Max(
                    pendienteMaximaDetectada,
                    pendienteGrados
                );


            bool tramoPendienteValido =
                pendienteGrados <=
                pendienteMaximaGrados;


            if (!tramoPendienteValido)
            {
                pendienteValida =
                    false;
            }


            if (mostrarDebug)
            {
                Debug.DrawLine(
                    centros[i],
                    centros[i + 1],
                    tramoPendienteValido
                        ? Color.green
                        : Color.red
                );
            }
        }


        // =================================================
        // CURVATURA HORIZONTAL
        // =================================================
        // Tres puntos consecutivos proyectados en XZ definen
        // un círculo. Su radio nos da una medida estable del giro.
        if (centros.Count >= 3)
        {
            for (int i = 1;
                 i < centros.Count - 1;
                 i++)
            {
                Vector2 a =
                    new Vector2(
                        centros[i - 1].x,
                        centros[i - 1].z
                    );

                Vector2 b =
                    new Vector2(
                        centros[i].x,
                        centros[i].z
                    );

                Vector2 c =
                    new Vector2(
                        centros[i + 1].x,
                        centros[i + 1].z
                    );


                float ab =
                    Vector2.Distance(a, b);

                float bc =
                    Vector2.Distance(b, c);

                float ca =
                    Vector2.Distance(c, a);


                if (ab < 0.0001f ||
                    bc < 0.0001f ||
                    ca < 0.0001f)
                {
                    continue;
                }


                float dobleArea =
                    Mathf.Abs(
                        (b.x - a.x) *
                        (c.y - a.y) -
                        (b.y - a.y) *
                        (c.x - a.x)
                    );


                // Casi recto: radio prácticamente infinito.
                if (dobleArea <
                    0.00001f)
                {
                    continue;
                }


                // R = a*b*c / (4*A)
                // dobleArea = 2*A -> R = a*b*c / (2*dobleArea)
                float radio =
                    (ab * bc * ca) /
                    (2f * dobleArea);


                radioMinimoDetectado =
                    Mathf.Min(
                        radioMinimoDetectado,
                        radio
                    );


                bool puntoCurvaturaValido =
                    radio >=
                    radioMinimoPermitido;


                if (!puntoCurvaturaValido)
                {
                    curvaturaValida =
                        false;
                }


                if (mostrarDebug)
                {
                    Debug.DrawRay(
                        centros[i],
                        Vector3.up * 0.35f,
                        puntoCurvaturaValido
                            ? Color.cyan
                            : Color.magenta
                    );
                }
            }
        }


        return
            pendienteValida &&
            curvaturaValida;
    }


    public bool HayBloqueoClearance(
        LayerMask capaObstaculos,
        float margenLateral,
        float alturaMinima,
        float recorteExtremos,
        GameObject estructuraInicioAIgnorar,
        GameObject estructuraFinalAIgnorar,
        bool mostrarDebug)
    {
        if (centros.Count < 2)
        {
            return false;
        }


        float medioAncho =
            ancho * 0.5f +
            Mathf.Max(
                0f,
                margenLateral
            );


        float alturaCaja =
            Mathf.Max(
                alturaMinima,
                grosor + altoRail
            );


        float medioAlto =
            alturaCaja * 0.5f;


        float recorte =
            Mathf.Clamp(
                recorteExtremos,
                0f,
                0.45f
            );


        for (int i = 0;
             i < centros.Count - 1;
             i++)
        {
            Vector3 a =
                centros[i];

            Vector3 b =
                centros[i + 1];


            Vector3 delta =
                b - a;


            float longitud =
                delta.magnitude;


            if (longitud <
                0.001f)
            {
                continue;
            }


            Vector3 direccion =
                delta /
                longitud;


            float tInicio =
                0f;

            float tFin =
                1f;


            if (i == 0)
            {
                tInicio =
                    recorte;
            }


            if (i ==
                centros.Count - 2)
            {
                tFin =
                    1f -
                    recorte;
            }


            if (tFin <= tInicio)
            {
                continue;
            }


            Vector3 aRecortado =
                Vector3.Lerp(
                    a,
                    b,
                    tInicio
                );


            Vector3 bRecortado =
                Vector3.Lerp(
                    a,
                    b,
                    tFin
                );


            Vector3 centroCaja =
                (
                    aRecortado +
                    bRecortado
                ) *
                0.5f;


            centroCaja +=
                Vector3.up *
                (
                    altoRail * 0.5f -
                    grosor * 0.5f
                );


            float longitudCaja =
                Vector3.Distance(
                    aRecortado,
                    bRecortado
                );


            Quaternion rotacionCaja =
                Quaternion.LookRotation(
                    direccion,
                    Vector3.up
                );


            Vector3 halfExtents =
                new Vector3(
                    medioAncho,
                    medioAlto,
                    Mathf.Max(
                        0.01f,
                        longitudCaja *
                        0.5f
                    )
                );


            Collider[] impactos =
                Physics.OverlapBox(
                    centroCaja,
                    halfExtents,
                    rotacionCaja,
                    capaObstaculos,
                    QueryTriggerInteraction.Collide
                );


            bool bloqueado =
                false;


            foreach (Collider col in impactos)
            {
                if (col == null)
                {
                    continue;
                }


                GameObject raiz =
                    col.transform.root.gameObject;


                if (estructuraInicioAIgnorar != null &&
                    raiz ==
                    estructuraInicioAIgnorar.transform.root.gameObject)
                {
                    continue;
                }


                if (estructuraFinalAIgnorar != null &&
                    raiz ==
                    estructuraFinalAIgnorar.transform.root.gameObject)
                {
                    continue;
                }


                if (raiz ==
                    transform.root.gameObject)
                {
                    continue;
                }


                bloqueado =
                    true;

                break;
            }


            if (mostrarDebug)
            {
                Color color =
                    bloqueado
                    ? Color.red
                    : Color.green;


                Debug.DrawLine(
                    aRecortado,
                    bRecortado,
                    color
                );


                DibujarCajaDebug(
                    centroCaja,
                    halfExtents,
                    rotacionCaja,
                    color
                );
            }


            if (bloqueado)
            {
                return true;
            }
        }


        return false;
    }


    private static void DibujarCajaDebug(
        Vector3 centro,
        Vector3 halfExtents,
        Quaternion rotacion,
        Color color)
    {
        Vector3[] puntos =
            new Vector3[8];


        int indice =
            0;


        for (int x = -1;
             x <= 1;
             x += 2)
        {
            for (int y = -1;
                 y <= 1;
                 y += 2)
            {
                for (int z = -1;
                     z <= 1;
                     z += 2)
                {
                    Vector3 local =
                        new Vector3(
                            halfExtents.x * x,
                            halfExtents.y * y,
                            halfExtents.z * z
                        );


                    puntos[indice++] =
                        centro +
                        rotacion *
                        local;
                }
            }
        }


        int[,] aristas =
        {
            { 0, 1 },
            { 0, 2 },
            { 0, 4 },
            { 1, 3 },
            { 1, 5 },
            { 2, 3 },
            { 2, 6 },
            { 3, 7 },
            { 4, 5 },
            { 4, 6 },
            { 5, 7 },
            { 6, 7 }
        };


        for (int i = 0;
             i < aristas.GetLength(0);
             i++)
        {
            Debug.DrawLine(
                puntos[
                    aristas[i, 0]
                ],
                puntos[
                    aristas[i, 1]
                ],
                color
            );
        }
    }


    public float ObtenerAnchoActual()
    {
        return ancho;
    }


    public float ObtenerLongitudAproximada()
    {
        if (centros.Count < 2)
        {
            return 0f;
        }


        float longitud =
            0f;


        for (int i = 0;
             i < centros.Count - 1;
             i++)
        {
            longitud +=
                Vector3.Distance(
                    centros[i],
                    centros[i + 1]
                );
        }


        return longitud;
    }


    public Vector3 ObtenerDireccionEnPunto(
        Vector3 puntoMundo)
    {
        if (centros.Count == 0 ||
            tangentes.Count == 0)
        {
            return transform.forward;
        }


        int mejorIndice =
            0;


        float mejorDistancia =
            float.PositiveInfinity;


        for (int i = 0;
             i < centros.Count;
             i++)
        {
            float distancia =
                (
                    centros[i] -
                    puntoMundo
                ).sqrMagnitude;


            if (distancia <
                mejorDistancia)
            {
                mejorDistancia =
                    distancia;

                mejorIndice =
                    i;
            }
        }


        return
            tangentes[
                mejorIndice
            ].normalized;
    }


    private static void AddQuad(
        List<int> lista,
        int a,
        int b,
        int c,
        int d)
    {
        lista.Add(a);
        lista.Add(b);
        lista.Add(c);

        lista.Add(a);
        lista.Add(c);
        lista.Add(d);
    }


    private static Vector3 Bezier(
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        float t)
    {
        float u =
            1f - t;


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
        float u =
            1f - t;


        return
            3f * u * u *
            (p1 - p0) +
            6f * u * t *
            (p2 - p1) +
            3f * t * t *
            (p3 - p2);
    }
}
