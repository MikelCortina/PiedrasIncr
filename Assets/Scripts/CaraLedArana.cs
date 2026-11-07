using System.Collections.Generic;
using UnityEngine;

// Cara LED de la pantalla de la araña: matriz de LEDs rojos con los rasgos en negro (LEDs apagados).
// Triste mientras no lleva nada; sonriente mientras lleva una piedra en su PuntoAgarre.
// Va en el Renderer de la pantalla: un slot concreto de su material (p. ej. el 2º material del cuerpo, que cubre la cara)
// o todo el Renderer (un Quad colocado sobre la pantalla).
// Las texturas y materiales se generan por código y se comparten entre todas las arañas con los mismos ajustes.
[RequireComponent(typeof(Renderer))]
public class CaraLedArana : MonoBehaviour
{
    [Header("Pantalla")]
    [Tooltip("Slot de material del Renderer que muestra la cara (0 = el primero). -1 = sustituye el material de todo el Renderer.")]
    public int indiceMaterial = 1;
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
    [Min(6)] public int columnas = 16;
    [Min(6)] public int filas = 12;
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
    private Material triste, sonriente;
    private bool? sonriendo;

    void Start()
    {
        pantalla = GetComponent<Renderer>();
        if (apagarNeonDelSlot) QuitarNeonDelSlot();

        BotRecolector bot = GetComponentInParent<BotRecolector>();
        if (puntoAgarre == null && bot != null) puntoAgarre = bot.puntoAgarre;

        if (ajusteAutomatico) AjustarAlSubmesh(bot != null ? bot.transform.up : Vector3.up);

        triste = ObtenerMaterial(false);
        sonriente = ObtenerMaterial(true);
    }

    void Update()
    {
        bool lleva = LlevaPiedra();
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
            return;
        }
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

        cache[clave] = material;
        return material;
    }

    Texture2D GenerarTextura(bool sonrisa)
    {
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
        textura.Apply(false, true);
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
            materialBase != null ? materialBase.GetHashCode() : 0);
    }
}
