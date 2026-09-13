using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class VisualTongueMesh : MonoBehaviour
{
    [Header("Referencia a la cadena")]
    [SerializeField] private VisualSphereChain cadena;

    [Header("Resolución de la malla")]
    [SerializeField] private int muestrasPorSegmento = 5;

    [SerializeField] private int ladosPorAnillo = 10;

    [Header("Grosor de la lengua")]
    [SerializeField] private float radioInicio = 0.075f;

    [SerializeField] private float radioFinal = 0.045f;

    [Header("Opciones")]
    [SerializeField] private bool cerrarExtremos = true;

    [SerializeField] private bool actualizarCadaFrame = true;

    private MeshFilter meshFilter;
    private Mesh mesh;

    private void Awake()
    {
        meshFilter =
            GetComponent<MeshFilter>();

        mesh =
            new Mesh();

        mesh.name =
            "ProceduralTongueMesh";

        mesh.MarkDynamic();

        meshFilter.sharedMesh =
            mesh;
    }

    private void LateUpdate()
    {
        if (!actualizarCadaFrame)
            return;

        ActualizarMalla();
    }

    [ContextMenu("Actualizar Malla")]
    public void ActualizarMalla()
    {
        if (cadena == null)
            return;

        Vector3[] puntos =
            cadena.ObtenerPosiciones();

        if (puntos == null ||
            puntos.Length < 2)
        {
            return;
        }

        List<Vector3> puntosSuavizados =
            CrearPuntosSuavizados(
                puntos
            );

        if (puntosSuavizados.Count < 2)
            return;

        ConstruirMalla(
            puntosSuavizados
        );
    }

    private List<Vector3> CrearPuntosSuavizados(
        Vector3[] puntos)
    {
        List<Vector3> puntosResultado =
            new List<Vector3>();

        int muestras =
            Mathf.Max(
                1,
                muestrasPorSegmento
            );

        for (int i = 0;
             i < puntos.Length - 1;
             i++)
        {
            for (int j = 0;
                 j < muestras;
                 j++)
            {
                float t =
                    j /
                    (float)muestras;

                Vector3 punto =
                    CalcularCatmullRom(
                        puntos,
                        i,
                        t
                    );

                puntosResultado.Add(
                    punto
                );
            }
        }

        // Añadir el último punto exactamente.
        puntosResultado.Add(
            puntos[puntos.Length - 1]
        );

        return puntosResultado;
    }

    private Vector3 CalcularCatmullRom(
        Vector3[] puntos,
        int indice,
        float t)
    {
        Vector3 p0 =
            puntos[
                Mathf.Max(
                    indice - 1,
                    0
                )
            ];

        Vector3 p1 =
            puntos[indice];

        Vector3 p2 =
            puntos[
                Mathf.Min(
                    indice + 1,
                    puntos.Length - 1
                )
            ];

        Vector3 p3 =
            puntos[
                Mathf.Min(
                    indice + 2,
                    puntos.Length - 1
                )
            ];

        float t2 =
            t * t;

        float t3 =
            t2 * t;

        return 0.5f *
               (
                   2f * p1 +
                   (-p0 + p2) * t +
                   (2f * p0 -
                    5f * p1 +
                    4f * p2 -
                    p3) * t2 +
                   (-p0 +
                    3f * p1 -
                    3f * p2 +
                    p3) * t3
               );
    }

    private void ConstruirMalla(
        List<Vector3> puntos)
    {
        int numeroPuntos =
            puntos.Count;

        int lados =
            Mathf.Max(
                3,
                ladosPorAnillo
            );

        Vector3[] vertices =
            new Vector3[
                numeroPuntos *
                lados
            ];

        Vector2[] uv =
            new Vector2[
                numeroPuntos *
                lados
            ];

        int numeroTriangulos =
            (numeroPuntos - 1) *
            lados *
            6;

        if (cerrarExtremos)
        {
            numeroTriangulos +=
                lados *
                6;
        }

        int[] triangulos =
            new int[
                numeroTriangulos
            ];

        Vector3 normalAnterior =
            Vector3.zero;

        for (int i = 0;
             i < numeroPuntos;
             i++)
        {
            Vector3 punto =
                puntos[i];

            Vector3 tangente =
                CalcularTangente(
                    puntos,
                    i
                );

            Vector3 normal =
                CalcularNormal(
                    tangente,
                    normalAnterior,
                    i
                );

            Vector3 binormal =
                Vector3.Cross(
                    tangente,
                    normal
                ).normalized;

            normalAnterior =
                normal;

            float porcentaje =
                i /
                (float)(numeroPuntos - 1);

            float radio =
                Mathf.Lerp(
                    radioInicio,
                    radioFinal,
                    porcentaje
                );

            for (int lado = 0;
                 lado < lados;
                 lado++)
            {
                float angulo =
                    lado /
                    (float)lados *
                    Mathf.PI *
                    2f;

                Vector3 direccionAnillo =
                    normal *
                    Mathf.Cos(angulo) +
                    binormal *
                    Mathf.Sin(angulo);

                Vector3 posicionVertice =
                    punto +
                    direccionAnillo *
                    radio;

                int indiceVertice =
                    i *
                    lados +
                    lado;

                vertices[indiceVertice] =
                    transform.InverseTransformPoint(
                        posicionVertice
                    );

                uv[indiceVertice] =
                    new Vector2(
                        lado /
                        (float)lados,
                        porcentaje
                    );
            }
        }

        int indiceTriangulo = 0;

        for (int i = 0;
             i < numeroPuntos - 1;
             i++)
        {
            for (int lado = 0;
                 lado < lados;
                 lado++)
            {
                int siguienteLado =
                    (lado + 1) %
                    lados;

                int actual =
                    i *
                    lados +
                    lado;

                int actualSiguiente =
                    i *
                    lados +
                    siguienteLado;

                int siguiente =
                    (i + 1) *
                    lados +
                    lado;

                int siguienteSiguiente =
                    (i + 1) *
                    lados +
                    siguienteLado;

                triangulos[indiceTriangulo++] =
                    actual;

                triangulos[indiceTriangulo++] =
                    siguiente;

                triangulos[indiceTriangulo++] =
                    siguienteSiguiente;

                triangulos[indiceTriangulo++] =
                    actual;

                triangulos[indiceTriangulo++] =
                    siguienteSiguiente;

                triangulos[indiceTriangulo++] =
                    actualSiguiente;
            }
        }

        if (cerrarExtremos)
        {
            indiceTriangulo =
                CrearTapa(
                    triangulos,
                    indiceTriangulo,
                    0,
                    lados,
                    true
                );

            CrearTapa(
                triangulos,
                indiceTriangulo,
                (numeroPuntos - 1) *
                lados,
                lados,
                false
            );
        }

        mesh.Clear();

        mesh.vertices =
            vertices;

        mesh.uv =
            uv;

        mesh.triangles =
            triangulos;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private Vector3 CalcularTangente(
        List<Vector3> puntos,
        int indice)
    {
        Vector3 tangente;

        if (indice == 0)
        {
            tangente =
                puntos[1] -
                puntos[0];
        }
        else if (indice == puntos.Count - 1)
        {
            tangente =
                puntos[indice] -
                puntos[indice - 1];
        }
        else
        {
            tangente =
                puntos[indice + 1] -
                puntos[indice - 1];
        }

        if (tangente.sqrMagnitude < 0.0001f)
        {
            return Vector3.forward;
        }

        return tangente.normalized;
    }

    private Vector3 CalcularNormal(
        Vector3 tangente,
        Vector3 normalAnterior,
        int indice)
    {
        Vector3 normal;

        if (indice == 0 ||
            normalAnterior.sqrMagnitude < 0.0001f)
        {
            normal =
                Vector3.Cross(
                    tangente,
                    Vector3.up
                );

            if (normal.sqrMagnitude < 0.0001f)
            {
                normal =
                    Vector3.Cross(
                        tangente,
                        Vector3.right
                    );
            }
        }
        else
        {
            // Transportar la normal anterior para
            // evitar giros bruscos de la malla.
            normal =
                normalAnterior -
                tangente *
                Vector3.Dot(
                    normalAnterior,
                    tangente
                );

            if (normal.sqrMagnitude < 0.0001f)
            {
                normal =
                    Vector3.Cross(
                        tangente,
                        Vector3.up
                    );
            }
        }

        return normal.normalized;
    }

    private int CrearTapa(
        int[] triangulos,
        int indiceTriangulo,
        int inicioAnillo,
        int lados,
        bool primeraTapa)
    {
        Vector3 centro =
            primeraTapa
                ? Vector3.zero
                : Vector3.zero;

        // Las tapas no necesitan un vértice central
        // adicional si se reutiliza el primer vértice
        // del anillo como referencia.
        for (int lado = 0;
             lado < lados;
             lado++)
        {
            int siguienteLado =
                (lado + 1) %
                lados;

            int actual =
                inicioAnillo +
                lado;

            int siguiente =
                inicioAnillo +
                siguienteLado;

            if (primeraTapa)
            {
                triangulos[indiceTriangulo++] =
                    actual;

                triangulos[indiceTriangulo++] =
                    siguiente;

                triangulos[indiceTriangulo++] =
                    actual;
            }
            else
            {
                triangulos[indiceTriangulo++] =
                    actual;

                triangulos[indiceTriangulo++] =
                    actual;

                triangulos[indiceTriangulo++] =
                    siguiente;
            }
        }

        return indiceTriangulo;
    }
}