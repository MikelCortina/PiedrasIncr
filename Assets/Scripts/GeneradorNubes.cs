using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Genera nubes procedurales con el TornadoShader (Modelos/Nuke/Shaders/TornadoShader01_Unlit).
// Cada nube son varios bultos irregulares fundidos en una sola malla. Se reconstruyen a partir
// de la semilla al activarse, así que no se guardan en la escena ni crean assets.
[ExecuteAlways]
public class GeneradorNubes : MonoBehaviour
{
    [Header("Material")]
    [Tooltip("Material con el TornadoShader01_Unlit (por defecto Modelos/Nuke/Materials/Nubes_Tornado)")]
    public Material materialNubes;

    [Header("Reparto")]
    [Min(1)] public int cantidad = 12;
    [Tooltip("Caja, en coordenadas locales de este objeto, donde aparecen las nubes")]
    public Vector3 tamanoZona = new Vector3(400f, 40f, 400f);
    [Tooltip("Distancia mínima entre centros de nubes")]
    [Min(0f)] public float separacionMinima = 40f;
    public int semilla = 1234;

    [Header("Forma de cada nube")]
    [Tooltip("Radio aproximado de cada nube (mínimo, máximo)")]
    public Vector2 radio = new Vector2(12f, 30f);
    [Tooltip("Número de bultos que forman cada nube (mínimo, máximo)")]
    public Vector2Int bultos = new Vector2Int(4, 9);
    [Tooltip("Altura relativa de la nube (1 = redonda)")]
    [Range(0.2f, 1f)] public float aplastado = 0.55f;
    [Tooltip("Cuánto se deforma la superficie de cada bulto")]
    [Range(0f, 0.5f)] public float irregularidad = 0.18f;
    [Range(1, 4)] public int subdivisiones = 2;

    [Header("Variación por nube")]
    [Tooltip("Variación aleatoria del brillo del color del material")]
    [Range(0f, 0.3f)] public float variacionTono = 0.08f;
    [Tooltip("Desplaza el centro del remolino del shader para que no giren todas igual")]
    public bool centroRemolinoAleatorio = true;

    [Header("Movimiento (solo en juego)")]
    [Tooltip("Desplazamiento de las nubes en unidades por segundo, en local")]
    public Vector3 deriva = new Vector3(0.6f, 0f, 0.2f);
    [Tooltip("Al salir de la zona, la nube reaparece por el lado contrario")]
    public bool envolverEnZona = true;

    private const string PrefijoNube = "Nube_";
    private static readonly int idColor = Shader.PropertyToID("Color_98BAF229");
    private static readonly int idCentroRemolino = Shader.PropertyToID("Vector2_DA9B1676");

    private readonly List<Transform> nubes = new List<Transform>();
    private bool regenerarPendiente;

    private void OnEnable()
    {
        Generar();
    }

    private void OnDisable()
    {
        Limpiar();
    }

    private void OnValidate()
    {
        // En OnValidate no se pueden crear objetos: se regenera en el siguiente tick.
        regenerarPendiente = true;
#if UNITY_EDITOR
        EditorApplication.delayCall -= RegenerarSiPendiente;
        EditorApplication.delayCall += RegenerarSiPendiente;
#endif
    }

    private void RegenerarSiPendiente()
    {
        if (this == null || !isActiveAndEnabled || !regenerarPendiente)
            return;

        Generar();
    }

    private void Update()
    {
        if (regenerarPendiente)
            Generar();

        if (!Application.isPlaying || deriva == Vector3.zero)
            return;

        Vector3 mitad = tamanoZona * 0.5f;
        foreach (Transform nube in nubes)
        {
            if (nube == null)
                continue;

            Vector3 p = nube.localPosition + deriva * Time.deltaTime;

            if (envolverEnZona)
            {
                p.x = Envolver(p.x, mitad.x);
                p.y = Envolver(p.y, mitad.y);
                p.z = Envolver(p.z, mitad.z);
            }

            nube.localPosition = p;
        }
    }

    private static float Envolver(float valor, float mitad)
    {
        if (mitad <= 0f)
            return valor;

        if (valor > mitad) return valor - 2f * mitad;
        if (valor < -mitad) return valor + 2f * mitad;
        return valor;
    }

    [ContextMenu("Regenerar nubes")]
    public void Generar()
    {
        regenerarPendiente = false;
        Limpiar();

        if (materialNubes == null)
            return;

        System.Random rng = new System.Random(semilla);
        List<Vector3> centros = new List<Vector3>();
        Color colorBase = materialNubes.HasProperty(idColor) ? materialNubes.GetColor(idColor) : Color.white;
        MaterialPropertyBlock bloque = new MaterialPropertyBlock();

        for (int i = 0; i < cantidad; i++)
        {
            Vector3 centro = Vector3.zero;
            bool colocada = false;

            for (int intento = 0; intento < 30 && !colocada; intento++)
            {
                centro = new Vector3(
                    Aleatorio(rng, -0.5f, 0.5f) * tamanoZona.x,
                    Aleatorio(rng, -0.5f, 0.5f) * tamanoZona.y,
                    Aleatorio(rng, -0.5f, 0.5f) * tamanoZona.z);

                colocada = true;
                foreach (Vector3 otro in centros)
                {
                    if (Vector3.Distance(otro, centro) < separacionMinima)
                    {
                        colocada = false;
                        break;
                    }
                }
            }

            if (!colocada)
                continue;

            centros.Add(centro);

            GameObject nube = new GameObject(PrefijoNube + i);
            nube.hideFlags = HideFlags.DontSave;
            nube.transform.SetParent(transform, false);
            nube.transform.localPosition = centro;
            nube.transform.localRotation = Quaternion.Euler(0f, Aleatorio(rng, 0f, 360f), 0f);

            MeshFilter filtro = nube.AddComponent<MeshFilter>();
            filtro.sharedMesh = ConstruirMallaNube(rng, Aleatorio(rng, radio.x, radio.y));

            MeshRenderer render = nube.AddComponent<MeshRenderer>();
            render.sharedMaterial = materialNubes;
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;

            // Variación por nube sin crear materiales nuevos.
            float brillo = 1f + Aleatorio(rng, -variacionTono, variacionTono);
            bloque.Clear();
            bloque.SetColor(idColor, new Color(colorBase.r * brillo, colorBase.g * brillo, colorBase.b * brillo, colorBase.a));
            if (centroRemolinoAleatorio)
                bloque.SetVector(idCentroRemolino, new Vector4(Aleatorio(rng, 0.3f, 0.7f), Aleatorio(rng, 0.3f, 0.7f), 0f, 0f));
            render.SetPropertyBlock(bloque);

            nubes.Add(nube.transform);
        }
    }

    private void Limpiar()
    {
        nubes.Clear();

        // Tras recargar scripts la lista se pierde: se buscan las nubes por nombre y flags.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform hijo = transform.GetChild(i);
            if ((hijo.hideFlags & HideFlags.DontSave) == 0 || !hijo.name.StartsWith(PrefijoNube))
                continue;

            MeshFilter filtro = hijo.GetComponent<MeshFilter>();
            if (filtro != null && filtro.sharedMesh != null)
                Destruir(filtro.sharedMesh);

            Destruir(hijo.gameObject);
        }
    }

    private static void Destruir(Object objeto)
    {
        if (Application.isPlaying)
            Destroy(objeto);
        else
            DestroyImmediate(objeto);
    }

    private static float Aleatorio(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    // =====================================================
    // MALLA DE LA NUBE
    // =====================================================

    private Mesh ConstruirMallaNube(System.Random rng, float radioNube)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangulos = new List<int>();

        // Tolera mínimo y máximo invertidos mientras se editan en el Inspector.
        int minBultos = Mathf.Max(1, Mathf.Min(bultos.x, bultos.y));
        int numBultos = rng.Next(minBultos, Mathf.Max(minBultos, Mathf.Max(bultos.x, bultos.y)) + 1);
        for (int b = 0; b < numBultos; b++)
        {
            // El primer bulto va en el centro y es el más grande; el resto se reparte alrededor.
            Vector3 centro = Vector3.zero;
            float radioBulto = radioNube * 0.6f;

            if (b > 0)
            {
                float angulo = Aleatorio(rng, 0f, Mathf.PI * 2f);
                float distancia = Aleatorio(rng, 0.25f, 0.75f) * radioNube;
                centro = new Vector3(Mathf.Cos(angulo) * distancia, Aleatorio(rng, -0.1f, 0.25f) * radioNube * aplastado, Mathf.Sin(angulo) * distancia);
                radioBulto = radioNube * Aleatorio(rng, 0.3f, 0.5f) * (1f - 0.4f * distancia / radioNube);
            }

            Vector3 desfaseRuido = new Vector3(Aleatorio(rng, 0f, 100f), Aleatorio(rng, 0f, 100f), Aleatorio(rng, 0f, 100f));
            AnadirBulto(vertices, uvs, triangulos, centro, radioBulto, desfaseRuido);
        }

        Mesh malla = new Mesh { name = "NubeProcedural" };
        malla.hideFlags = HideFlags.DontSave;
        if (vertices.Count > 65000)
            malla.indexFormat = IndexFormat.UInt32;
        malla.SetVertices(vertices);
        malla.SetUVs(0, uvs);
        malla.SetTriangles(triangulos, 0);
        malla.RecalculateNormals();
        malla.RecalculateBounds();
        return malla;
    }

    private void AnadirBulto(List<Vector3> vertices, List<Vector2> uvs, List<int> triangulos, Vector3 centro, float radioBulto, Vector3 desfaseRuido)
    {
        Esfera(subdivisiones, out List<Vector3> dirs, out List<int> caras);

        // Triángulos sueltos: así cada uno puede corregir la costura de la UV esférica
        // (el shader es unlit, las normales no importan).
        for (int t = 0; t < caras.Count; t += 3)
        {
            Vector3 d0 = dirs[caras[t]], d1 = dirs[caras[t + 1]], d2 = dirs[caras[t + 2]];
            Vector2 uv0 = UvEsferica(d0), uv1 = UvEsferica(d1), uv2 = UvEsferica(d2);
            CorregirCostura(ref uv0, ref uv1, ref uv2);

            int baseIndice = vertices.Count;
            vertices.Add(PuntoBulto(d0, centro, radioBulto, desfaseRuido));
            vertices.Add(PuntoBulto(d1, centro, radioBulto, desfaseRuido));
            vertices.Add(PuntoBulto(d2, centro, radioBulto, desfaseRuido));
            uvs.Add(uv0);
            uvs.Add(uv1);
            uvs.Add(uv2);
            triangulos.Add(baseIndice);
            triangulos.Add(baseIndice + 1);
            triangulos.Add(baseIndice + 2);
        }
    }

    private Vector3 PuntoBulto(Vector3 direccion, Vector3 centro, float radioBulto, Vector3 desfaseRuido)
    {
        Vector3 q = direccion * 1.7f + desfaseRuido;
        float ruido = (Mathf.PerlinNoise(q.x, q.y) + Mathf.PerlinNoise(q.y + 31.7f, q.z) + Mathf.PerlinNoise(q.z + 17.3f, q.x)) / 3f;
        float r = radioBulto * (1f + irregularidad * (ruido * 2f - 1f) * 2f);

        Vector3 p = direccion * r;
        p.y *= aplastado;
        // Base más plana, como las nubes de verdad.
        if (p.y < 0f)
            p.y *= 0.45f;

        return centro + p;
    }

    private static Vector2 UvEsferica(Vector3 d)
    {
        return new Vector2(0.5f + Mathf.Atan2(d.z, d.x) / (2f * Mathf.PI), 0.5f + Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) / Mathf.PI);
    }

    private static void CorregirCostura(ref Vector2 a, ref Vector2 b, ref Vector2 c)
    {
        float max = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
        if (max - Mathf.Min(a.x, Mathf.Min(b.x, c.x)) <= 0.5f)
            return;

        if (a.x < 0.5f) a.x += 1f;
        if (b.x < 0.5f) b.x += 1f;
        if (c.x < 0.5f) c.x += 1f;
    }

    // Icosfera unitaria (direcciones normalizadas).
    private static void Esfera(int nivel, out List<Vector3> dirs, out List<int> caras)
    {
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        dirs = new List<Vector3>
        {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
        };
        for (int i = 0; i < dirs.Count; i++)
            dirs[i] = dirs[i].normalized;

        caras = new List<int>
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };

        for (int n = 0; n < nivel; n++)
        {
            Dictionary<long, int> medios = new Dictionary<long, int>();
            List<int> nuevas = new List<int>(caras.Count * 4);
            for (int i = 0; i < caras.Count; i += 3)
            {
                int a = caras[i], b = caras[i + 1], c = caras[i + 2];
                int ab = PuntoMedio(a, b, dirs, medios), bc = PuntoMedio(b, c, dirs, medios), ca = PuntoMedio(c, a, dirs, medios);
                nuevas.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            caras = nuevas;
        }
    }

    private static int PuntoMedio(int a, int b, List<Vector3> dirs, Dictionary<long, int> medios)
    {
        long clave = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        if (medios.TryGetValue(clave, out int indice))
            return indice;

        dirs.Add(((dirs[a] + dirs[b]) * 0.5f).normalized);
        medios[clave] = dirs.Count - 1;
        return dirs.Count - 1;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        materialNubes = AssetDatabase.LoadAssetAtPath<Material>("Assets/Modelos/Nuke/Materials/Nubes_Tornado.mat");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.8f, 1f, 0.5f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, tamanoZona);
    }

    [MenuItem("GameObject/Efectos/Generador de nubes (Tornado)", false, 10)]
    private static void CrearGenerador(MenuCommand comando)
    {
        GameObject objeto = new GameObject("GeneradorNubes");
        GameObjectUtility.SetParentAndAlign(objeto, comando.context as GameObject);
        if (comando.context == null && SceneView.lastActiveSceneView != null)
            objeto.transform.position = SceneView.lastActiveSceneView.pivot;
        objeto.AddComponent<GeneradorNubes>();
        Undo.RegisterCreatedObjectUndo(objeto, "Crear generador de nubes");
        Selection.activeObject = objeto;
    }
#endif
}
