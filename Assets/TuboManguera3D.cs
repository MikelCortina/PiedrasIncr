
using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TuboManguera3D : MonoBehaviour
{
    [Header("Referencia")]
    public MangueraFisica manguera;

    [Header("Aspecto del tubo")]
    [Range(6, 24)] public int caras = 12;
    public float radio = 0.125f;

    private Mesh malla;
    private Vector3[] vertices;
    private Vector3[] normales;
    private Vector2[] uvs;

    private int anillosCreados;
    private int carasCreadas;

    void Awake()
    {
        if (manguera == null)
            manguera = GetComponentInParent<MangueraFisica>();
    }

    void LateUpdate()
    {
        if (manguera == null || manguera.CantidadPuntos < 2)
            return;

        int anillos = manguera.CantidadPuntos;

        if (malla == null ||
            anillos != anillosCreados ||
            caras != carasCreadas)
        {
            CrearMalla(anillos);
        }

        float longitud = 0f;
        Vector3 tangenteAnterior = Vector3.forward;
        Vector3 lateral = Vector3.right;

        for (int i = 0; i < anillos; i++)
        {
            Vector3 centro = manguera.ObtenerPunto(i);

            if (i > 0)
            {
                longitud += Vector3.Distance(
                    centro,
                    manguera.ObtenerPunto(i - 1)
                );
            }

            Vector3 tangente;

            if (i == 0)
                tangente = manguera.ObtenerPunto(1) - centro;
            else if (i == anillos - 1)
                tangente = centro - manguera.ObtenerPunto(i - 1);
            else
                tangente = manguera.ObtenerPunto(i + 1)
                         - manguera.ObtenerPunto(i - 1);

            if (tangente.sqrMagnitude < 0.000001f)
                tangente = tangenteAnterior;

            tangente.Normalize();

            if (i == 0)
            {
                lateral = Vector3.Cross(tangente, Vector3.up);

                if (lateral.sqrMagnitude < 0.000001f)
                    lateral = Vector3.Cross(tangente, Vector3.forward);

                lateral.Normalize();
            }
            else
            {
                lateral = Quaternion.FromToRotation(
                    tangenteAnterior, tangente
                ) * lateral;

                lateral = Vector3.ProjectOnPlane(
                    lateral, tangente
                ).normalized;
            }

            Vector3 vertical = Vector3.Cross(tangente, lateral);
            tangenteAnterior = tangente;

            for (int j = 0; j <= caras; j++)
            {
                float angulo = j * Mathf.PI * 2f / caras;

                Vector3 direccion =
                    lateral * Mathf.Cos(angulo) +
                    vertical * Mathf.Sin(angulo);

                int indice = i * (caras + 1) + j;

                vertices[indice] = transform.InverseTransformPoint(
                    centro + direccion * radio
                );

                normales[indice] = transform.InverseTransformDirection(
                    direccion
                ).normalized;

                uvs[indice] = new Vector2(
                    (float)j / caras,
                    longitud / (2f * Mathf.PI * Mathf.Max(radio, 0.001f))
                );
            }
        }

        malla.vertices = vertices;
        malla.normals = normales;
        malla.uv = uvs;
        malla.RecalculateBounds();
    }

    void CrearMalla(int anillos)
    {
        if (malla != null)
            Destroy(malla);

        anillosCreados = anillos;
        carasCreadas = caras;

        int cantidadVertices = anillos * (caras + 1);

        vertices = new Vector3[cantidadVertices];
        normales = new Vector3[cantidadVertices];
        uvs = new Vector2[cantidadVertices];

        int[] triangulos = new int[(anillos - 1) * caras * 6];
        int t = 0;

        for (int i = 0; i < anillos - 1; i++)
        {
            for (int j = 0; j < caras; j++)
            {
                int a = i * (caras + 1) + j;
                int b = a + 1;
                int c = a + caras + 1;
                int d = c + 1;

                triangulos[t++] = a;
                triangulos[t++] = b;
                triangulos[t++] = c;

                triangulos[t++] = b;
                triangulos[t++] = d;
                triangulos[t++] = c;
            }
        }

        malla = new Mesh();
        malla.name = "Manguera Roja Dinamica";
        malla.MarkDynamic();

        malla.vertices = vertices;
        malla.triangles = triangulos;

        GetComponent<MeshFilter>().sharedMesh = malla;
    }

    void OnDestroy()
    {
        if (malla != null)
            Destroy(malla);
    }
}
