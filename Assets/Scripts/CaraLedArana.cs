using System.Collections.Generic;
using UnityEngine;

// Cara LED de la pantalla de la araña: matriz de LEDs rojos con los rasgos en negro (LEDs apagados).
// Triste mientras no lleva nada; sonriente mientras lleva una piedra en su PuntoAgarre.
// Va en el Renderer de la pantalla: un slot concreto de su material (p. ej. el 2º material del cuerpo, que cubre la cara)
// o todo el Renderer (un Quad colocado sobre la pantalla).
// Las texturas y materiales se generan por código y se comparten entre todas las arañas con los mismos ajustes.
[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class CaraLedArana : MonoBehaviour
{
    [Header("Pantalla")]
    [Tooltip("Slot de material del Renderer que muestra la cara (0 = el primero). -1 = sustituye el material de todo el Renderer.")]
    public int indiceMaterial = 1;
    [Tooltip("Opcional: nombre del material del slot de la pantalla. Si existe en el Renderer, manda sobre Índice Material " +
             "(útil en modelos importados, donde el orden de los slots no es obvio).")]
    public string nombreMaterial = "";

    public enum Expresion { SegunPiedra, SiempreSonriente, SiempreTriste, SonrienteConFallos }
    [Tooltip("Según Piedra = triste sin carga y sonriente llevando una piedra (arañas). Sonriente Con Fallos = sonríe y " +
             "cada cierto tiempo la imagen falla (parpadea y se desplaza) y se queda triste unos segundos.")]
    public Expresion expresion = Expresion.SegunPiedra;

    [Tooltip("En vez de cambiar el material de la pantalla, pone delante un quad con la cara, ajustado a la forma de la " +
             "pantalla. No depende de las UV del modelo: úsalo en pantallas cuyas UV estén giradas o repartidas.")]
    public bool superponerQuad = false;

    [Tooltip("Solo editor: en la escena (fuera de Play) muestra la cara triste en vez de la sonriente, para ajustarla.")]
    public bool vistaPreviaTriste = false;

    [Header("Fallos (Sonriente Con Fallos)")]
    [Tooltip("Segundos entre un fallo y el siguiente (aleatorio entre mínimo y máximo).")]
    public Vector2 intervaloFallos = new Vector2(6f, 14f);
    [Tooltip("Segundos que se queda triste tras el fallo (aleatorio entre mínimo y máximo).")]
    public Vector2 duracionTriste = new Vector2(2f, 4f);
    [Tooltip("Duración del parpadeo al cambiar de cara.")]
    [Range(0.05f, 1.5f)] public float duracionParpadeo = 0.35f;
    [Tooltip("Cuánto se desplaza la imagen al fallar (fracción de la pantalla).")]
    [Range(0f, 0.2f)] public float desplazamientoFallo = 0.04f;
    [Tooltip("Quita de ControladorNeon el control de este slot (si no, lo reescribe cada frame y tiñe la cara).")]
    public bool apagarNeonDelSlot = true;

    [Tooltip("Encaja la cara en las UV de ese slot (que suelen ocupar solo un trocito del atlas), la pone derecha según el " +
             "'arriba' de la araña y ajusta las filas para que los LED salgan redondos. Desactivado = Rotación, Escala UV, " +
             "Desplazamiento UV y Filas a mano.")]
    public bool ajusteAutomatico = true;

    public enum Rotacion { Grados0, Grados90, Grados180, Grados270 }
    [Tooltip("Gira la cara si las UV de la pantalla no están derechas.")]
    public Rotacion rotacion = Rotacion.Grados0;
    public bool espejoHorizontal = false;
    public bool espejoVertical = false;
    [Tooltip("Escala y desplazamiento de la textura sobre las UV de la pantalla (Tiling / Offset).")]
    public Vector2 escalaUV = Vector2.one;
    public Vector2 desplazamientoUV = Vector2.zero;

    [Header("Detección de piedra")]
    [Tooltip("Punto donde la araña sujeta la piedra. Vacío = el PuntoAgarre del BotRecolector padre.")]
    public Transform puntoAgarre;
    public string tagPiedra = "Piedra";

    [Header("Matriz LED")]
    [Tooltip("LEDs a lo ancho. Más LEDs = cara y fondo con más resolución (las filas se ajustan solas con el ajuste automático).")]
    [Min(6)] public int columnas = 32;
    [Min(6)] public int filas = 24;
    [Tooltip("Píxeles de cada LED en la textura generada.")]
    [Range(4, 32)] public int pixelesPorLed = 12;
    [Tooltip("Tamaño del punto LED dentro de su celda (1 = se tocan).")]
    [Range(0.3f, 1f)] public float tamanoLed = 0.85f;

    [Header("Colores")]
    public Color colorFondo = new Color(1f, 0.08f, 0.06f);
    [Tooltip("Hueco entre LEDs.")]
    public Color colorHueco = new Color(0.25f, 0.01f, 0.01f);
    public Color colorLineas = Color.black;
    [Tooltip("Brillo del material (más de 1 hace bloom con HDR).")]
    [Range(0f, 8f)] public float intensidad = 1f;

    [Header("Rasgos (coordenadas 0..1 de la pantalla)")]
    public Vector2 ojoIzquierdo = new Vector2(0.32f, 0.66f);
    public Vector2 ojoDerecho = new Vector2(0.68f, 0.66f);
    [Range(0.02f, 0.2f)] public float radioOjo = 0.08f;
    [Tooltip("Altura del centro de la boca.")]
    [Range(0f, 1f)] public float alturaBoca = 0.3f;
    [Range(0.1f, 0.9f)] public float anchoBoca = 0.5f;
    [Tooltip("Cuánto se curva la boca (hacia arriba sonriendo, hacia abajo triste).")]
    [Range(0f, 0.4f)] public float curvaBoca = 0.14f;
    [Range(0.02f, 0.15f)] public float grosorBoca = 0.06f;

    [Header("Material")]
    [Tooltip("Opcional: material base (p. ej. URP Unlit). Vacío = se crea uno URP Unlit.")]
    public Material materialBase;

    private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    private static readonly int idBaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int idMainTex = Shader.PropertyToID("_MainTex");
    private static readonly int idBaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int idColor = Shader.PropertyToID("_Color");

    private Renderer pantalla;
    private static readonly int idBaseMapST = Shader.PropertyToID("_BaseMap_ST");
    private static readonly int idMainTexST = Shader.PropertyToID("_MainTex_ST");
    private MaterialPropertyBlock bloqueFallo;
    private enum EstadoFallos { Sonriendo, ParpadeoATriste, Triste, ParpadeoASonriente }
    private EstadoFallos estadoFallos;
    private float finEstado, siguienteCuadroFallo;

    // Pantallas cuyas UV forman un rectángulo girado dentro del atlas: la textura cubre el rectángulo
    // alineado que lo envuelve y la cara se dibuja dentro del paralelogramo.
    // textura(0..1) = mapaOrigen + s * mapaEjeS + t * mapaEjeT, con (s, t) = coordenadas 0..1 de la cara.
    private bool mapaGirado;
    private Vector2 mapaOrigen, mapaEjeS, mapaEjeT;
    private Material triste, sonriente;
    private bool? sonriendo;

    // ---------------------------------------------------------------- Vista previa en el editor

    private GameObject quadCreado;
    private static bool texturasLegibles; // solo para guardar la imagen del diagnóstico
    [System.NonSerialized] private Vector3[] esquinasQuad;      // en el mundo (no se guarda al recompilar)
    private Vector3 normalQuad;
    private string infoQuad = "sin quad";

    void OnEnable()
    {
        if (!Application.isPlaying) RefrescarVistaPrevia();
    }

    void OnDisable()
    {
        DestruirQuad();
    }

#if UNITY_EDITOR
    // Al tocar cualquier valor en el Inspector la cara de la escena se rehace (en el siguiente refresco del editor:
    // en OnValidate no se pueden crear objetos).
    void OnValidate()
    {
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && isActiveAndEnabled && !Application.isPlaying) RefrescarVistaPrevia();
        };
    }
#endif

    // Fuera de Play, las caras en quad se ven en la escena con un quad oculto que no se guarda. Los valores calculados
    // (slot, filas, giro...) se devuelven luego a como estaban para no ensuciar la escena.
    // Las caras en un slot de material (arañas) no se previsualizan: cambiarían el material guardado del modelo.
    void RefrescarVistaPrevia()
    {
        DestruirQuad();
        if (!superponerQuad) return;

        int indiceGuardado = indiceMaterial, filasGuardadas = filas;
        Rotacion rotacionGuardada = rotacion;
        Vector2 escalaGuardada = escalaUV, desplazamientoGuardado = desplazamientoUV;

        pantalla = GetComponent<Renderer>();
        ElegirSlotPorNombre();
        if (CrearQuad())
        {
            bool sonrisa = expresion == Expresion.SiempreSonriente ||
                           (expresion != Expresion.SiempreTriste && !vistaPreviaTriste);
            Aplicar(ObtenerMaterial(sonrisa));
#if UNITY_EDITOR
            EscribirDiagnostico(false);
#endif
        }

        indiceMaterial = indiceGuardado;
        filas = filasGuardadas;
        rotacion = rotacionGuardada;
        escalaUV = escalaGuardada;
        desplazamientoUV = desplazamientoGuardado;
        pantalla = GetComponent<Renderer>();
    }

    void DestruirQuad()
    {
        if (quadCreado == null) return;
        MeshFilter filtro = quadCreado.GetComponent<MeshFilter>();
        Mesh malla = filtro != null ? filtro.sharedMesh : null;

        if (Application.isPlaying)
        {
            Destroy(quadCreado);
            if (malla != null) Destroy(malla);
        }
        else
        {
            DestroyImmediate(quadCreado);
            if (malla != null) DestroyImmediate(malla);
        }
        quadCreado = null;
    }

    // Con el objeto seleccionado: contorno amarillo del quad de la cara y su normal (hacia donde mira).
    void OnDrawGizmosSelected()
    {
        if (!superponerQuad || esquinasQuad == null || esquinasQuad.Length < 4) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < 4; i++) Gizmos.DrawLine(esquinasQuad[i], esquinasQuad[(i + 1) % 4]);
        Vector3 centro = (esquinasQuad[0] + esquinasQuad[2]) * 0.5f;
        float largo = Vector3.Distance(esquinasQuad[0], esquinasQuad[2]) * 0.3f;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(centro, centro + normalQuad * largo);
    }

    [ContextMenu("Diagnóstico de la pantalla")]
    void Diagnostico() => EscribirDiagnostico(true);

    // Escribe el diagnóstico en Logs/CaraLed/<objeto>.txt (y la textura de la cara en .png) y, si se pide, en la consola.
    void EscribirDiagnostico(bool aConsola)
    {
        Renderer r = GetComponent<Renderer>();
        System.Text.StringBuilder sb = new System.Text.StringBuilder("[CaraLedArana] Diagnóstico de " + name + "\n");
        Material[] mats = r.sharedMaterials;
        for (int i = 0; i < mats.Length; i++)
            sb.Append("  slot ").Append(i).Append(": ").Append(mats[i] != null ? mats[i].name + " (" + mats[i].shader.name + ")" : "vacío").Append('\n');

        MeshFilter filtro = GetComponent<MeshFilter>();
        Mesh malla = filtro != null ? filtro.sharedMesh : null;
        if (malla == null && r is SkinnedMeshRenderer piel) malla = piel.sharedMesh;
        if (malla == null) { Debug.Log(sb.Append("  sin malla"), this); return; }

        sb.Append("  malla ").Append(malla.name).Append(" legible ").Append(malla.isReadable)
          .Append(" submallas ").Append(malla.subMeshCount).Append(" vértices ").Append(malla.vertexCount).Append('\n');
        if (malla.isReadable)
        {
            Vector2[] uvs = malla.uv;
            Vector3[] normales = malla.normals;
            for (int s = 0; s < malla.subMeshCount; s++)
            {
                int[] tri = malla.GetTriangles(s);
                Vector2 min = Vector2.one * float.MaxValue, max = Vector2.one * float.MinValue;
                Vector3 n = Vector3.zero;
                foreach (int i in tri)
                {
                    if (uvs.Length > i) { min = Vector2.Min(min, uvs[i]); max = Vector2.Max(max, uvs[i]); }
                    if (normales.Length > i) n += normales[i];
                }
                sb.Append("  submalla ").Append(s).Append(": triángulos ").Append(tri.Length / 3)
                  .Append(" uv ").Append(min.ToString("0.000")).Append("..").Append(max.ToString("0.000"))
                  .Append(" normal mundo ").Append(transform.TransformDirection(n.normalized).ToString("0.00")).Append('\n');
            }
        }

        int slot = indiceMaterial;
        if (!string.IsNullOrEmpty(nombreMaterial))
            for (int i = 0; i < mats.Length; i++) if (mats[i] != null && mats[i].name == nombreMaterial) slot = i;
        sb.Append("  slot usado ").Append(slot).Append(" | quad ").Append(superponerQuad).Append(" | ").Append(infoQuad)
          .Append(" | quad creado ").Append(quadCreado != null).Append('\n');
        if (quadCreado != null)
        {
            Renderer rq = quadCreado.GetComponent<Renderer>();
            Material mq = rq != null ? rq.sharedMaterial : null;
            Texture tex = mq != null && mq.HasProperty(idBaseMap) ? mq.GetTexture(idBaseMap) : null;
            sb.Append("  material quad ").Append(mq != null ? mq.name + " / " + mq.shader.name : "ninguno")
              .Append(" textura ").Append(tex != null ? tex.width + "x" + tex.height : "ninguna")
              .Append(" columnas ").Append(columnas).Append(" filas calculadas ").Append(filas);
        }
        string carpeta = System.IO.Path.Combine(Application.dataPath, "..", "Logs", "CaraLed");
        string archivo = System.IO.Path.Combine(carpeta, name + "_" + Mathf.Abs(GetHashCode()));
        try
        {
            System.IO.Directory.CreateDirectory(carpeta);
            sb.Append("\n  posición ").Append(transform.position.ToString("0.00")).Append(" rotación ")
              .Append(transform.eulerAngles.ToString("0.0")).Append(" escala global ").Append(transform.lossyScale.ToString("0.000"))
              .Append("\n  límites renderer ").Append(r.bounds.center.ToString("0.00")).Append(" tamaño ").Append(r.bounds.size.ToString("0.00"));
            System.IO.File.WriteAllText(archivo + ".txt", sb.ToString());

            texturasLegibles = true;
            Texture2D muestra = GenerarTextura(!vistaPreviaTriste);
            texturasLegibles = false;
            System.IO.File.WriteAllBytes(archivo + ".png", muestra.EncodeToPNG());
            DestroyImmediate(muestra);
        }
        catch (System.Exception e)
        {
            texturasLegibles = false;
            sb.Append("\n  (no se pudo guardar el diagnóstico: ").Append(e.Message).Append(')');
        }

        if (aConsola) Debug.Log(sb.ToString(), this);
    }

    // ---------------------------------------------------------------- Juego

    void Start()
    {
        if (!Application.isPlaying) return;
        DestruirQuad(); // por si quedaba la vista previa

        pantalla = GetComponent<Renderer>();
        ElegirSlotPorNombre();
        if (apagarNeonDelSlot) QuitarNeonDelSlot();

        BotRecolector bot = GetComponentInParent<BotRecolector>();
        if (puntoAgarre == null && bot != null) puntoAgarre = bot.puntoAgarre;

        if (superponerQuad)
        {
            // Sin quad no hay cara: nunca se cambia el material del slot (con UV giradas saldría descolocada).
            if (!CrearQuad())
            {
                Debug.LogWarning("[CaraLedArana] No se pudo crear la cara de " + name + ": " + infoQuad, this);
#if UNITY_EDITOR
                EscribirDiagnostico(false);
#endif
                enabled = false;
                return;
            }
        }
        else if (ajusteAutomatico) AjustarAlSubmesh(bot != null ? bot.transform.up : Vector3.up);

        triste = ObtenerMaterial(false);
        sonriente = ObtenerMaterial(true);
#if UNITY_EDITOR
        if (superponerQuad) EscribirDiagnostico(false);
#endif

        // Cada pantalla falla a su ritmo.
        estadoFallos = EstadoFallos.Sonriendo;
        finEstado = Time.time + Random.Range(intervaloFallos.x, intervaloFallos.y);
    }

    void Update()
    {
        if (!Application.isPlaying) return;

        if (expresion == Expresion.SonrienteConFallos)
        {
            ActualizarFallos();
            return;
        }

        bool lleva = expresion == Expresion.SiempreSonriente ||
                     (expresion == Expresion.SegunPiedra && LlevaPiedra());
        if (sonriendo == lleva) return;

        sonriendo = lleva;
        Aplicar(lleva ? sonriente : triste);
    }

    void Aplicar(Material material)
    {
        if (indiceMaterial < 0)
        {
            pantalla.sharedMaterial = material;
            return;
        }

        Material[] materiales = pantalla.sharedMaterials;
        if (indiceMaterial >= materiales.Length) return;
        materiales[indiceMaterial] = material;
        pantalla.sharedMaterials = materiales;
    }

    // ---------------------------------------------------------------- Fallos

    void ActualizarFallos()
    {
        if (sonriendo == null) { sonriendo = true; Aplicar(sonriente); }

        float ahora = Time.time;
        bool parpadeando = estadoFallos == EstadoFallos.ParpadeoATriste || estadoFallos == EstadoFallos.ParpadeoASonriente;

        if (ahora >= finEstado)
        {
            switch (estadoFallos)
            {
                case EstadoFallos.Sonriendo:
                    estadoFallos = EstadoFallos.ParpadeoATriste;
                    finEstado = ahora + duracionParpadeo;
                    break;
                case EstadoFallos.ParpadeoATriste:
                    QuitarFallo(triste, false);
                    estadoFallos = EstadoFallos.Triste;
                    finEstado = ahora + Random.Range(duracionTriste.x, duracionTriste.y);
                    break;
                case EstadoFallos.Triste:
                    estadoFallos = EstadoFallos.ParpadeoASonriente;
                    finEstado = ahora + duracionParpadeo;
                    break;
                case EstadoFallos.ParpadeoASonriente:
                    QuitarFallo(sonriente, true);
                    estadoFallos = EstadoFallos.Sonriendo;
                    finEstado = ahora + Random.Range(intervaloFallos.x, intervaloFallos.y);
                    break;
            }
            return;
        }

        if (!parpadeando || ahora < siguienteCuadroFallo) return;

        // Cuadro de fallo: cara al azar, imagen desplazada a saltos y brillo irregular.
        siguienteCuadroFallo = ahora + Random.Range(0.03f, 0.08f);
        bool caraSonriente = Random.value < 0.5f;
        Aplicar(caraSonriente ? sonriente : triste);

        if (bloqueFallo == null) bloqueFallo = new MaterialPropertyBlock();
        bloqueFallo.Clear();
        Vector2 salto = new Vector2(Random.Range(-1f, 1f), Random.Range(-0.3f, 0.3f)) * desplazamientoFallo;
        Vector4 st = new Vector4(escalaUV.x, escalaUV.y,
                                 desplazamientoUV.x + salto.x * escalaUV.x, desplazamientoUV.y + salto.y * escalaUV.y);
        bloqueFallo.SetVector(idBaseMapST, st);
        bloqueFallo.SetVector(idMainTexST, st);
        Color brillo = Color.white * intensidad * Random.Range(0.35f, 1.4f);
        brillo.a = 1f;
        bloqueFallo.SetColor(idBaseColor, brillo);
        bloqueFallo.SetColor(idColor, brillo);
        PonerBloque(bloqueFallo);
    }

    void QuitarFallo(Material final, bool esSonrisa)
    {
        if (bloqueFallo == null) bloqueFallo = new MaterialPropertyBlock();
        bloqueFallo.Clear();
        PonerBloque(bloqueFallo);
        Aplicar(final);
        sonriendo = esSonrisa;
    }

    void PonerBloque(MaterialPropertyBlock bloque)
    {
        if (indiceMaterial < 0) pantalla.SetPropertyBlock(bloque);
        else if (indiceMaterial < pantalla.sharedMaterials.Length) pantalla.SetPropertyBlock(bloque, indiceMaterial);
    }

    // ---------------------------------------------------------------- Quad superpuesto

    // Crea delante de la cara de la pantalla (el submesh del slot) un quad con sus mismas medidas, derecho según el
    // 'arriba' del mundo. La cara se pinta en ese quad, con UV 0..1, así que las UV del modelo dan igual.
    bool CrearQuad()
    {
        MeshFilter filtro = GetComponent<MeshFilter>();
        Mesh malla = filtro != null ? filtro.sharedMesh : null;
        if (malla == null && pantalla is SkinnedMeshRenderer piel) malla = piel.sharedMesh;
        if (malla == null || !malla.isReadable)
        {
            infoQuad = malla == null ? "sin malla" : "malla " + malla.name + " no legible (activa Read/Write en su modelo)";
            return false;
        }

        int[] triangulos = indiceMaterial >= 0 && indiceMaterial < malla.subMeshCount
            ? malla.GetTriangles(indiceMaterial) : malla.triangles;
        Vector3[] posiciones = malla.vertices;
        Vector3[] normales = malla.normals;
        if (triangulos.Length < 3) { infoQuad = "el slot " + indiceMaterial + " no tiene triángulos"; return false; }

        // Normal de la pantalla: la del triángulo más grande, hacia donde apuntan las normales del modelo.
        Vector3 normal = Vector3.zero;
        float mayor = 0f;
        for (int k = 0; k + 2 < triangulos.Length; k += 3)
        {
            Vector3 a = transform.TransformPoint(posiciones[triangulos[k]]);
            Vector3 b = transform.TransformPoint(posiciones[triangulos[k + 1]]);
            Vector3 c = transform.TransformPoint(posiciones[triangulos[k + 2]]);
            Vector3 cruz = Vector3.Cross(b - a, c - a);
            if (cruz.sqrMagnitude <= mayor) continue;
            mayor = cruz.sqrMagnitude;
            normal = cruz;
            if (normales.Length == posiciones.Length &&
                Vector3.Dot(normal, transform.TransformDirection(normales[triangulos[k]])) < 0f)
                normal = -normal;
        }
        if (mayor <= 0f) { infoQuad = "triángulos degenerados"; return false; }
        normal.Normalize();

        Vector3 arriba = Vector3.ProjectOnPlane(Vector3.up, normal);
        if (arriba.sqrMagnitude < 0.01f) arriba = Vector3.ProjectOnPlane(transform.up, normal);
        arriba.Normalize();
        Vector3 derecha = Vector3.Cross(normal, arriba);

        float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue, frente = float.MinValue;
        foreach (int i in triangulos)
        {
            Vector3 p = transform.TransformPoint(posiciones[i]);
            float x = Vector3.Dot(p, derecha), y = Vector3.Dot(p, arriba);
            xMin = Mathf.Min(xMin, x); xMax = Mathf.Max(xMax, x);
            yMin = Mathf.Min(yMin, y); yMax = Mathf.Max(yMax, y);
            frente = Mathf.Max(frente, Vector3.Dot(p, normal));
        }
        float ancho = xMax - xMin, alto = yMax - yMin;
        if (ancho < 1e-5f || alto < 1e-5f) { infoQuad = "pantalla sin tamaño (" + ancho + " x " + alto + ")"; return false; }

        esquinasQuad = new[]
        {
            derecha * xMin + arriba * yMin + normal * frente, derecha * xMin + arriba * yMax + normal * frente,
            derecha * xMax + arriba * yMax + normal * frente, derecha * xMax + arriba * yMin + normal * frente
        };
        normalQuad = normal;
        infoQuad = "triángulos " + triangulos.Length / 3 + " | ancho " + ancho.ToString("0.###") + " alto " + alto.ToString("0.###") +
                   " | normal " + normal.ToString("0.00") + " | centro " + ((esquinasQuad[0] + esquinasQuad[2]) * 0.5f).ToString("0.00");

        float separacion = 0.004f * Mathf.Max(ancho, alto);
        Vector3 Esquina(float x, float y) =>
            transform.InverseTransformPoint(derecha * x + arriba * y + normal * (frente + separacion));

        Mesh quad = new Mesh { name = "CaraLedQuad" };
        // Abajo-izq, arriba-izq, arriba-der, abajo-der: en sentido horario visto desde delante (cara visible).
        quad.vertices = new[] { Esquina(xMin, yMin), Esquina(xMin, yMax), Esquina(xMax, yMax), Esquina(xMax, yMin) };
        quad.uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
        Vector3 normalLocal = transform.InverseTransformDirection(normal).normalized;
        quad.normals = new[] { normalLocal, normalLocal, normalLocal, normalLocal };
        quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        quad.RecalculateBounds();

        GameObject objeto = new GameObject("CaraLedQuad");
        if (!Application.isPlaying)
        {
            objeto.hideFlags = HideFlags.HideAndDontSave;
            quad.hideFlags = HideFlags.HideAndDontSave;
        }
        quadCreado = objeto;
        objeto.layer = gameObject.layer;
        objeto.transform.SetParent(transform, false);
        objeto.AddComponent<MeshFilter>().sharedMesh = quad;
        MeshRenderer render = objeto.AddComponent<MeshRenderer>();
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;

        // A partir de aquí la cara vive en el quad.
        pantalla = render;
        indiceMaterial = -1;
        rotacion = Rotacion.Grados0;
        escalaUV = Vector2.one;
        desplazamientoUV = Vector2.zero;
        mapaGirado = false;
        filas = Mathf.Max(6, Mathf.RoundToInt(columnas * alto / ancho));
        return true;
    }

    void ElegirSlotPorNombre()
    {
        if (string.IsNullOrEmpty(nombreMaterial) || pantalla == null) return;

        Material[] materiales = pantalla.sharedMaterials;
        for (int i = 0; i < materiales.Length; i++)
        {
            if (materiales[i] != null && materiales[i].name == nombreMaterial)
            {
                indiceMaterial = i;
                return;
            }
        }
        Debug.LogWarning("[CaraLedArana] No hay ningún material '" + nombreMaterial + "' en " + name + ": se usa el slot " + indiceMaterial + ".", this);
    }

    // ControladorNeon pinta su slot con un MaterialPropertyBlock cada frame: se le quita ese slot
    // (los demás siguen funcionando) y se borra el bloque que ya hubiera dejado puesto.
    void QuitarNeonDelSlot()
    {
        foreach (ControladorNeon neon in GetComponents<ControladorNeon>())
        {
            if (neon.ajustesPorSlot == null) continue;

            List<AjustesNeonSlot> restantes = new List<AjustesNeonSlot>();
            foreach (AjustesNeonSlot slot in neon.ajustesPorSlot)
                if (indiceMaterial >= 0 && slot.indiceMaterial != indiceMaterial) restantes.Add(slot);

            if (restantes.Count == 0) neon.enabled = false;
            neon.ajustesPorSlot = restantes.ToArray();
        }

        MaterialPropertyBlock vacio = new MaterialPropertyBlock();
        if (indiceMaterial < 0) pantalla.SetPropertyBlock(vacio);
        else if (indiceMaterial < pantalla.sharedMaterials.Length) pantalla.SetPropertyBlock(vacio, indiceMaterial);
    }

    // El bot hace hija de PuntoAgarre la piedra que lleva.
    bool LlevaPiedra()
    {
        if (puntoAgarre == null) return false;
        foreach (Transform hijo in puntoAgarre)
            if (hijo.gameObject.activeInHierarchy && hijo.CompareTag(tagPiedra)) return true;
        return false;
    }

    // ---------------------------------------------------------------- Ajuste a la malla

    // Lee las UV del submesh de la pantalla: su rectángulo pasa a ser el 0..1 de la textura (Tiling/Offset),
    // se averigua qué eje UV apunta hacia arriba en el mundo para girar la cara, y se calculan las filas
    // para que cada LED mida lo mismo de ancho que de alto.
    void AjustarAlSubmesh(Vector3 arriba)
    {
        if (indiceMaterial < 0) return; // Quad completo: sus UV ya van de 0 a 1.

        MeshFilter filtro = GetComponent<MeshFilter>();
        Mesh malla = filtro != null ? filtro.sharedMesh : null;
        if (malla == null && pantalla is SkinnedMeshRenderer piel) malla = piel.sharedMesh;
        if (malla == null || !malla.isReadable || indiceMaterial >= malla.subMeshCount)
        {
            Debug.LogWarning("[CaraLedArana] No se pueden leer las UV de la pantalla: ajusta Rotación y Escala/Desplazamiento UV a mano.", this);
            return;
        }

        int[] triangulos = malla.GetTriangles(indiceMaterial);
        Vector2[] uvs = malla.uv;
        Vector3[] posiciones = malla.vertices;
        if (triangulos.Length < 3 || uvs.Length != posiciones.Length) return;

        Vector2 minimo = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 maximo = new Vector2(float.MinValue, float.MinValue);
        foreach (int i in triangulos)
        {
            minimo = Vector2.Min(minimo, uvs[i]);
            maximo = Vector2.Max(maximo, uvs[i]);
        }

        Vector2 tamano = maximo - minimo;
        if (tamano.x < 1e-6f || tamano.y < 1e-6f) return;

        escalaUV = new Vector2(1f / tamano.x, 1f / tamano.y);
        desplazamientoUV = -Vector2.Scale(minimo, escalaUV);

        // Direcciones (en el mundo) en las que crecen U y V, con el primer triángulo no degenerado.
        for (int k = 0; k + 2 < triangulos.Length; k += 3)
        {
            int a = triangulos[k], b = triangulos[k + 1], c = triangulos[k + 2];
            Vector3 e1 = posiciones[b] - posiciones[a], e2 = posiciones[c] - posiciones[a];
            Vector2 d1 = uvs[b] - uvs[a], d2 = uvs[c] - uvs[a];
            float determinante = d1.x * d2.y - d2.x * d1.y;
            if (Mathf.Abs(determinante) < 1e-12f) continue;

            Vector3 dirU = transform.TransformVector((e1 * d2.y - e2 * d1.y) / determinante);
            Vector3 dirV = transform.TransformVector((e2 * d1.x - e1 * d2.x) / determinante);

            float subeU = Vector3.Dot(dirU.normalized, arriba);
            float subeV = Vector3.Dot(dirV.normalized, arriba);
            if (Mathf.Abs(subeV) >= Mathf.Abs(subeU))
                rotacion = subeV >= 0f ? Rotacion.Grados0 : Rotacion.Grados180;
            else
                rotacion = subeU >= 0f ? Rotacion.Grados270 : Rotacion.Grados90;

            // LED cuadrados: el tamaño real de la pantalla a lo largo de U y de V.
            float largoU = dirU.magnitude * tamano.x;
            float largoV = dirV.magnitude * tamano.y;
            if (largoU > 1e-6f) filas = Mathf.Max(6, Mathf.RoundToInt(columnas * largoV / largoU));

            CalcularMapaGirado(malla, triangulos, uvs, posiciones, minimo, tamano, arriba, a, b, c);
            return;
        }
    }

    // ---------------------------------------------------------------- Guardar en el prefab (solo editor)

#if UNITY_EDITOR
    public const string CarpetaAssets = "Assets/Materials/Led/CaraArana";
    public const string RutaMaterialTriste = CarpetaAssets + "/CaraLed_Triste.mat";
    const string RutaTexturaTriste = CarpetaAssets + "/CaraLed_Triste_Textura.asset";

    // ¿El slot ya muestra la cara guardada? (para no volver a guardarla cada vez que se abre el editor)
    public bool TieneCaraGuardada()
    {
        Renderer r = GetComponent<Renderer>();
        if (r == null) return false;
        Material[] materiales = r.sharedMaterials;
        Material actual = indiceMaterial < 0 ? r.sharedMaterial
                        : indiceMaterial < materiales.Length ? materiales[indiceMaterial] : null;
        return actual != null && UnityEditor.AssetDatabase.GetAssetPath(actual) == RutaMaterialTriste;
    }

    // Genera la cara triste, la guarda como material y textura reales del proyecto y la pone en el slot.
    // Así el prefab ya la lleva puesta en el editor; en Play el script sigue cambiando entre triste y sonriente.
    [ContextMenu("Guardar cara en el material del slot")]
    public void GuardarCaraEnSlot()
    {
        pantalla = GetComponent<Renderer>();
        ElegirSlotPorNombre();
        BotRecolector bot = GetComponentInParent<BotRecolector>(true);
        if (ajusteAutomatico) AjustarAlSubmesh(bot != null ? bot.transform.up : Vector3.up);

        cache.Remove(Clave(false));          // material nuevo, no el que ya pudiera usar otra araña
        Material generado = ObtenerMaterial(false);
        cache.Remove(Clave(false));
        Texture2D textura = generado.HasProperty(idBaseMap) ? generado.GetTexture(idBaseMap) as Texture2D
                                                            : generado.GetTexture(idMainTex) as Texture2D;
        generado.hideFlags = HideFlags.None;
        if (textura != null) textura.hideFlags = HideFlags.None;

        if (!UnityEditor.AssetDatabase.IsValidFolder(CarpetaAssets))
            UnityEditor.AssetDatabase.CreateFolder("Assets/Materials/Led", "CaraArana");

        // Si ya existen se sobrescriben conservando su GUID (no se rompen otras referencias).
        Texture2D texturaAsset = Guardar(textura, RutaTexturaTriste);
        foreach (int id in new[] { idBaseMap, idMainTex })
            if (generado.HasProperty(id)) generado.SetTexture(id, texturaAsset);
        Material materialAsset = Guardar(generado, RutaMaterialTriste);
        UnityEditor.AssetDatabase.SaveAssets();

        UnityEditor.Undo.RecordObject(pantalla, "Cara LED en el slot");
        Aplicar(materialAsset);
        UnityEditor.EditorUtility.SetDirty(pantalla);
        UnityEditor.EditorUtility.SetDirty(this);
    }

    static T Guardar<T>(T objeto, string ruta) where T : Object
    {
        T existente = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(ruta);
        if (existente == null)
        {
            UnityEditor.AssetDatabase.CreateAsset(objeto, ruta);
            return objeto;
        }
        UnityEditor.EditorUtility.CopySerialized(objeto, existente);
        UnityEditor.EditorUtility.SetDirty(existente);
        return existente;
    }
#endif

    // Ejes de la cara en el mundo: "arriba" proyectado sobre la pantalla y "derecha" visto desde delante.
    // Con ellos se sabe dónde cae cada vértice en la cara (s, t) y se ajusta la relación UV -> cara.
    void CalcularMapaGirado(Mesh malla, int[] triangulos, Vector2[] uvs, Vector3[] posiciones,
                            Vector2 minimoUV, Vector2 tamanoUV, Vector3 arriba, int a, int b, int c)
    {
        mapaGirado = false;

        Vector3[] normales = malla.normals;
        Vector3 normal = normales.Length == posiciones.Length
            ? transform.TransformDirection(normales[a])
            : transform.TransformDirection(Vector3.Cross(posiciones[b] - posiciones[a], posiciones[c] - posiciones[a]));
        normal.Normalize();

        Vector3 ejeArriba = Vector3.ProjectOnPlane(arriba, normal);
        if (ejeArriba.sqrMagnitude < 1e-6f) return;
        ejeArriba.Normalize();
        Vector3 ejeDerecha = Vector3.Cross(normal, ejeArriba);

        // Rectángulo de la cara en el mundo.
        float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;
        foreach (int i in triangulos)
        {
            Vector3 p = transform.TransformPoint(posiciones[i]);
            float x = Vector3.Dot(p, ejeDerecha), y = Vector3.Dot(p, ejeArriba);
            xMin = Mathf.Min(xMin, x); xMax = Mathf.Max(xMax, x);
            yMin = Mathf.Min(yMin, y); yMax = Mathf.Max(yMax, y);
        }
        float ancho = xMax - xMin, alto = yMax - yMin;
        if (ancho < 1e-6f || alto < 1e-6f) return;

        Vector2 Cara(int i)
        {
            Vector3 p = transform.TransformPoint(posiciones[i]);
            return new Vector2((Vector3.Dot(p, ejeDerecha) - xMin) / ancho, (Vector3.Dot(p, ejeArriba) - yMin) / alto);
        }
        Vector2 Tex(int i) => new Vector2((uvs[i].x - minimoUV.x) / tamanoUV.x, (uvs[i].y - minimoUV.y) / tamanoUV.y);

        // Relación afín cara -> textura a partir del triángulo: [EjeS EjeT] * dCara = dTex.
        Vector2 c0 = Cara(a), c1 = Cara(b), c2 = Cara(c);
        Vector2 t0 = Tex(a), t1 = Tex(b), t2 = Tex(c);
        Vector2 dc1 = c1 - c0, dc2 = c2 - c0, dt1 = t1 - t0, dt2 = t2 - t0;
        float det = dc1.x * dc2.y - dc2.x * dc1.y;
        if (Mathf.Abs(det) < 1e-9f) return;

        Vector2 ejeS = (dt1 * dc2.y - dt2 * dc1.y) / det;
        Vector2 ejeT = (dt2 * dc1.x - dt1 * dc2.x) / det;

        // Si la cara ya está alineada con la textura, basta con el giro de 90° de siempre.
        bool alineada = Mathf.Min(Mathf.Abs(ejeS.x), Mathf.Abs(ejeS.y)) < 0.05f * ejeS.magnitude &&
                        Mathf.Min(Mathf.Abs(ejeT.x), Mathf.Abs(ejeT.y)) < 0.05f * ejeT.magnitude;
        if (alineada) return;

        mapaGirado = true;
        mapaEjeS = ejeS;
        mapaEjeT = ejeT;
        mapaOrigen = t0 - c0.x * ejeS - c0.y * ejeT;
        rotacion = Rotacion.Grados0; // el mapa ya pone la cara derecha
        filas = Mathf.Max(6, Mathf.RoundToInt(columnas * alto / ancho));
    }

    // ---------------------------------------------------------------- Generación

    Material ObtenerMaterial(bool sonrisa)
    {
        string clave = Clave(sonrisa);
        if (cache.TryGetValue(clave, out Material existente) && existente != null) return existente;

        Material material;
        if (materialBase != null)
        {
            material = new Material(materialBase);
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            material = new Material(shader);
        }

        material.name = sonrisa ? "CaraLed_Sonriente" : "CaraLed_Triste";
        Texture2D textura = GenerarTextura(sonrisa);
        if (!Application.isPlaying)
        {
            material.hideFlags = HideFlags.DontSave;
            textura.hideFlags = HideFlags.DontSave;
        }
        foreach (int id in new[] { idBaseMap, idMainTex })
        {
            if (!material.HasProperty(id)) continue;
            material.SetTexture(id, textura);
            material.SetTextureScale(id, escalaUV);
            material.SetTextureOffset(id, desplazamientoUV);
        }

        Color brillo = Color.white * intensidad;
        brillo.a = 1f;
        if (material.HasProperty(idBaseColor)) material.SetColor(idBaseColor, brillo);
        if (material.HasProperty(idColor)) material.SetColor(idColor, brillo);

        // En quad se ve por las dos caras: si la normal de la pantalla viniera al revés, la cara no desaparece.
        if (superponerQuad && material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);

        cache[clave] = material;
        return material;
    }

    Texture2D GenerarTextura(bool sonrisa)
    {
        if (mapaGirado) return GenerarTexturaGirada(sonrisa);

        int ancho = columnas * pixelesPorLed;
        int alto = filas * pixelesPorLed;
        Texture2D textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false)
        {
            name = sonrisa ? "CaraLed_Sonriente" : "CaraLed_Triste",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixeles = new Color[ancho * alto];
        float radioPunto = tamanoLed * 0.5f;

        for (int fila = 0; fila < filas; fila++)
        {
            for (int columna = 0; columna < columnas; columna++)
            {
                Vector2 centro = new Vector2((columna + 0.5f) / columnas, (fila + 0.5f) / filas);
                Color colorLed = EsRasgo(OrientarUV(centro), sonrisa) ? colorLineas : colorFondo;

                for (int py = 0; py < pixelesPorLed; py++)
                {
                    for (int px = 0; px < pixelesPorLed; px++)
                    {
                        // Punto redondo con borde suave dentro de la celda.
                        float dx = (px + 0.5f) / pixelesPorLed - 0.5f;
                        float dy = (py + 0.5f) / pixelesPorLed - 0.5f;
                        float distancia = Mathf.Sqrt(dx * dx + dy * dy);
                        float cobertura = Mathf.Clamp01((radioPunto - distancia) * pixelesPorLed + 0.5f);

                        int x = columna * pixelesPorLed + px;
                        int y = fila * pixelesPorLed + py;
                        pixeles[y * ancho + x] = Color.Lerp(colorHueco, colorLed, cobertura);
                    }
                }
            }
        }

        textura.SetPixels(pixeles);
        textura.Apply(false, !texturasLegibles);
        return textura;
    }

    // La textura envuelve el paralelogramo de la pantalla: cada píxel se pasa a coordenadas de la cara (s, t)
    // y se pinta el LED que le toca; fuera de la pantalla, color de hueco.
    Texture2D GenerarTexturaGirada(bool sonrisa)
    {
        // Resolución suficiente para que cada LED tenga unos 'pixelesPorLed' píxeles a lo largo de la cara.
        float necesario = Mathf.Max(columnas * pixelesPorLed / Mathf.Max(mapaEjeS.magnitude, 1e-4f),
                                    filas * pixelesPorLed / Mathf.Max(mapaEjeT.magnitude, 1e-4f));
        int lado = Mathf.Clamp(Mathf.CeilToInt(necesario), 64, 2048);

        Texture2D textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false)
        {
            name = sonrisa ? "CaraLed_Sonriente" : "CaraLed_Triste",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float det = mapaEjeS.x * mapaEjeT.y - mapaEjeT.x * mapaEjeS.y;
        float radioPunto = tamanoLed * 0.5f;
        float pixelesLed = Mathf.Max(2f, lado * mapaEjeS.magnitude / columnas); // para el borde suave

        // Qué LEDs son rasgo, una vez por LED.
        bool[,] rasgo = new bool[columnas, filas];
        for (int f = 0; f < filas; f++)
            for (int col = 0; col < columnas; col++)
                rasgo[col, f] = EsRasgo(OrientarUV(new Vector2((col + 0.5f) / columnas, (f + 0.5f) / filas)), sonrisa);

        Color[] pixeles = new Color[lado * lado];
        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                Vector2 d = new Vector2((x + 0.5f) / lado, (y + 0.5f) / lado) - mapaOrigen;
                float s = (d.x * mapaEjeT.y - mapaEjeT.x * d.y) / det;
                float tt = (mapaEjeS.x * d.y - d.x * mapaEjeS.y) / det;

                if (s < 0f || s >= 1f || tt < 0f || tt >= 1f)
                {
                    pixeles[y * lado + x] = colorHueco;
                    continue;
                }

                float cs = s * columnas, ct = tt * filas;
                int col = Mathf.Min((int)cs, columnas - 1), f = Mathf.Min((int)ct, filas - 1);
                float dx = cs - col - 0.5f, dy = ct - f - 0.5f;
                float cobertura = Mathf.Clamp01((radioPunto - Mathf.Sqrt(dx * dx + dy * dy)) * pixelesLed + 0.5f);
                pixeles[y * lado + x] = Color.Lerp(colorHueco, rasgo[col, f] ? colorLineas : colorFondo, cobertura);
            }
        }

        textura.SetPixels(pixeles);
        textura.Apply(false, !texturasLegibles);
        return textura;
    }

    // Pasa de coordenadas de la textura a coordenadas de la cara (giro y espejos alrededor del centro).
    Vector2 OrientarUV(Vector2 uv)
    {
        if (espejoHorizontal) uv.x = 1f - uv.x;
        if (espejoVertical) uv.y = 1f - uv.y;

        switch (rotacion)
        {
            case Rotacion.Grados90: return new Vector2(uv.y, 1f - uv.x);
            case Rotacion.Grados180: return new Vector2(1f - uv.x, 1f - uv.y);
            case Rotacion.Grados270: return new Vector2(1f - uv.y, uv.x);
            default: return uv;
        }
    }

    // Ojos redondos y boca en arco. Las distancias se miden en unidades de celda para que los trazos
    // tengan el mismo grosor en horizontal y en vertical aunque la matriz no sea cuadrada.
    bool EsRasgo(Vector2 uv, bool sonrisa)
    {
        // Ancho/alto de la cara: con la cara girada 90°, las columnas de la textura van en vertical.
        bool girada = rotacion == Rotacion.Grados90 || rotacion == Rotacion.Grados270;
        float aspecto = girada ? (float)filas / columnas : (float)columnas / filas;
        Vector2 escala = new Vector2(aspecto, 1f);

        if (Vector2.Scale(uv - ojoIzquierdo, escala).magnitude <= radioOjo * aspecto) return true;
        if (Vector2.Scale(uv - ojoDerecho, escala).magnitude <= radioOjo * aspecto) return true;

        float medio = anchoBoca * 0.5f;
        float relativo = (uv.x - 0.5f) / medio; // -1..1 a lo ancho de la boca
        if (Mathf.Abs(relativo) > 1f) return false;

        // Sonrisa: los extremos suben. Triste: los extremos bajan.
        float signo = sonrisa ? 1f : -1f;
        float yBoca = alturaBoca + signo * curvaBoca * (relativo * relativo - 0.5f);
        return Mathf.Abs(uv.y - yBoca) <= grosorBoca;
    }

    string Clave(bool sonrisa)
    {
        return string.Join("|", sonrisa, columnas, filas, pixelesPorLed, tamanoLed, colorFondo, colorHueco, colorLineas,
            intensidad, ojoIzquierdo, ojoDerecho, radioOjo, alturaBoca, anchoBoca, curvaBoca, grosorBoca,
            rotacion, espejoHorizontal, espejoVertical, escalaUV, desplazamientoUV,
            mapaGirado, mapaOrigen, mapaEjeS, mapaEjeT, superponerQuad,
            materialBase != null ? materialBase.GetHashCode() : 0);
    }
}
