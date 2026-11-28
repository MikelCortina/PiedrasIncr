using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Edificio que empieza en ruinas (un cubo) y se construye y mejora LANZÁNDOLE MONEDAS,
/// como la diana, la ametralladora y la desgastadora:
///  - Cualquier moneda lanzada (fusil o mano) que toque las ruinas, el edificio o su zona de pago suma su valor.
///  - Al completar el precio: las ruinas desaparecen y aparece el edificio (nivel 1). Luego se mejora hasta el máximo.
///  - Un cartel flotante dice cuántas monedas faltan; las ruinas o el edificio hacen 'bloop' con cada moneda.
///  - Cada nivel puede cambiar piezas (viejas/nuevas) y lanzar un evento (Inspector).
///
/// Se monta solo (con valores por defecto) en el Generador de Arañas, el Fisionador y la tienda de edificios.
/// Para cambiar precios, niveles o efectos, añadid el componente a mano en esos objetos: se configura solo
/// al añadirlo y lo que cambiéis se guarda.
/// </summary>
[DisallowMultipleComponent]
public class ConstruccionConMonedas : MonoBehaviour
{
    [Serializable]
    public class Nivel
    {
        [Tooltip("Monedas para llegar a este nivel (el 1 es construirlo). Si hay Id Economia en el Excel, manda el Excel.")]
        [Min(1)] public int precio = 20;
        [Tooltip("Piezas que desaparecen al llegar a este nivel.")]
        public GameObject[] piezasViejas;
        [Tooltip("Piezas que aparecen al llegar a este nivel.")]
        public GameObject[] piezasNuevas;
        [Tooltip("Qué más pasa al llegar a este nivel (p. ej. activar algo, cambiar un valor).")]
        public UnityEvent alLlegar = new UnityEvent();
    }

    public enum Tipo { Personalizado, GeneradorAranas, Fisionador, TiendaEdificios }

    [Header("Qué es")]
    [Tooltip("Cambia cómo se monta solo (qué se desbloquea al construirlo). Personalizado: solo lo de abajo.")]
    public Tipo tipo = Tipo.Personalizado;
    public string nombre = "EDIFICIO";
    [Tooltip("Id del Excel (EconomiaDemo) para los precios. Vacío o sin esa fila = los precios de cada nivel.")]
    public string idEconomia = "";

    [Header("Niveles (el 1 es construirlo)")]
    public List<Nivel> niveles = new List<Nivel>();

    [Header("Ruinas y edificio")]
    [Tooltip("Lo que se ve antes de construirlo (el cubo). Vacío = se crea un cubo del tamaño del edificio.")]
    public GameObject[] partesRuinas;
    [Tooltip("Lo que aparece al construirlo. Vacío = lo maneja su propio script (Generador, Fisionador) o los hijos del objeto.")]
    public GameObject[] partesConstruidas;
    [Tooltip("Qué más pasa al construirlo (nivel 1).")]
    public UnityEvent alConstruir = new UnityEvent();

    [Header("Efectos por defecto de los niveles")]
    [Tooltip("Generador de Arañas: arañas extra que se pueden tener por cada nivel por encima del 1.")]
    [Min(0)] public int aranasExtraPorNivel = 2;

    [Header("Cartel")]
    public bool mostrarCartel = true;
    [Tooltip("Metros por encima del edificio.")]
    public float alturaCartel = 1.2f;
    public float tamanoCartel = 2.2f;
    [Tooltip("Distancia a partir de la que el cartel no se ve.")]
    public float distanciaCartel = 35f;
    [Tooltip("{0} = nombre, {1} = monedas aportadas, {2} = precio. Construir.")]
    public string formatoConstruir = "{0}\nCONSTRUIR  {1} / {2}";
    [Tooltip("{0} = nombre, {1} = aportadas, {2} = precio, {3} = nivel siguiente.")]
    public string formatoMejorar = "{0}\nNIVEL {3}  {1} / {2}";
    public string formatoMaximo = "{0}\nNIVEL MAX";

    [Header("Respuesta a las monedas")]
    [Tooltip("Cuánto crece con cada moneda (0.08 = un 8 %).")]
    public float fuerzaBloop = 0.08f;
    public float velocidadBloop = 14f;
    public AudioClip sonidoMoneda;
    public AudioClip sonidoConstruir;
    [Range(0f, 1f)] public float volumen = 0.8f;

    // --- Estado ---
    [Header("Estado (solo lectura)")]
    [SerializeField] private int nivelActual;      // 0 = en ruinas
    [SerializeField] private int monedasAportadas; // para el siguiente nivel

    public event Action ProgresoActualizado;

    public bool Construida => nivelActual > 0;
    public int NivelActual => nivelActual;
    public int NivelMaximo => Mathf.Max(1, niveles.Count);
    public bool EnMaximo => nivelActual >= NivelMaximo;
    public int PrecioSiguiente => EnMaximo ? 0 : Precio(nivelActual + 1);
    public int MonedasRestantes => EnMaximo ? 0 : Mathf.Max(0, PrecioSiguiente - monedasAportadas);

    private readonly HashSet<ProyectilArma> cobradosEsteFrame = new HashSet<ProyectilArma>();
    private int frameCobro = -1;
    private readonly Dictionary<Transform, Vector3> escalas = new Dictionary<Transform, Vector3>();
    private float bloop;
    private TextMeshPro cartel;
    private AudioSource audioSource;
    private Bounds limitesEdificio;

    // =====================================================
    // MONTAJE AUTOMÁTICO
    // =====================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Instalar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        AnadirATodos();
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo) => AnadirATodos();

    private static void AnadirATodos()
    {
        foreach (GeneradorAranas g in FindObjectsByType<GeneradorAranas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (g != null && !g.Desbloqueado && g.GetComponent<ConstruccionConMonedas>() == null)
                g.gameObject.AddComponent<ConstruccionConMonedas>();

        foreach (Fisionador f in FindObjectsByType<Fisionador>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (f != null && !f.desbloqueado && f.GetComponent<ConstruccionConMonedas>() == null)
                f.gameObject.AddComponent<ConstruccionConMonedas>();

        foreach (TiendaEdificiosTrigger t in FindObjectsByType<TiendaEdificiosTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t != null && t.GetComponent<ConstruccionConMonedas>() == null)
                t.gameObject.AddComponent<ConstruccionConMonedas>();
    }

    // Al añadirlo (a mano en el editor o solo al empezar) se configura según el objeto.
    private void Reset() => ConfigurarPorDefecto();

    private void ConfigurarPorDefecto()
    {
        GeneradorAranas generador = GetComponent<GeneradorAranas>();
        Fisionador fisionador = GetComponent<Fisionador>();
        TiendaEdificiosTrigger tiendaEdificios = GetComponent<TiendaEdificiosTrigger>();

        if (generador != null)
        {
            tipo = Tipo.GeneradorAranas;
            nombre = "GENERADOR DE ARAÑAS";
            idEconomia = "";
            CrearNiveles(30, 60, 120);
            if (partesRuinas == null || partesRuinas.Length == 0) partesRuinas = generador.objetosBloqueado;
        }
        else if (fisionador != null)
        {
            tipo = Tipo.Fisionador;
            nombre = "FISIONADOR";
            idEconomia = EconomiaDemo.Ids.Fisionador; // nivel 1 del Excel (300); 2 y 3 los de aquí
            CrearNiveles(300, 450, 600);
            if (partesRuinas == null || partesRuinas.Length == 0) partesRuinas = fisionador.objetosBloqueado;
        }
        else if (tiendaEdificios != null)
        {
            tipo = Tipo.TiendaEdificios;
            nombre = "TIENDA DE EDIFICIOS";
            idEconomia = "";
            CrearNiveles(15, 30, 60);
        }
    }

    private void CrearNiveles(params int[] precios)
    {
        if (niveles != null && niveles.Count > 0) return;
        niveles = new List<Nivel>();
        foreach (int p in precios) niveles.Add(new Nivel { precio = p });
    }

    // =====================================================
    // CICLO DE VIDA
    // =====================================================

    private void Start()
    {
        if (niveles == null || niveles.Count == 0) ConfigurarPorDefecto();
        if (niveles == null || niveles.Count == 0) CrearNiveles(20, 40, 80);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        // El edificio no se compra de otra forma mientras esté este componente.
        Fisionador fisionador = GetComponent<Fisionador>();
        if (fisionador != null) fisionador.compraConTecla = false;

        limitesEdificio = CalcularLimites();
        if (tipo == Tipo.TiendaEdificios && (partesRuinas == null || partesRuinas.Length == 0))
            CrearCuboRuinas();

        // Ya construido por otro lado (guardado, Inspector): empieza en nivel 1.
        if (YaDesbloqueadoPorSuScript()) nivelActual = Mathf.Max(nivelActual, 1);

        AplicarEstado(false);
        PrepararCobro();
        CrearCartel();
        ActualizarCartel();
    }

    private void Update()
    {
        if (bloop > 0f)
        {
            bloop = Mathf.MoveTowards(bloop, 0f, Time.deltaTime * velocidadBloop * fuerzaBloop);
            AplicarBloop();
        }
        OrientarCartel();
    }

    private bool YaDesbloqueadoPorSuScript()
    {
        GeneradorAranas generador = GetComponent<GeneradorAranas>();
        if (generador != null && generador.Desbloqueado) return true;
        Fisionador fisionador = GetComponent<Fisionador>();
        if (fisionador != null && fisionador.desbloqueado) return true;
        return false;
    }

    // =====================================================
    // PRECIOS
    // =====================================================

    private int Precio(int nivel)
    {
        if (nivel < 1 || nivel > niveles.Count) return 0;
        int respaldo = Mathf.Max(1, niveles[nivel - 1].precio);
        if (string.IsNullOrEmpty(idEconomia)) return respaldo;
        int delExcel = ProveedorEconomia.Actual.CosteOPorDefecto(idEconomia, nivel, respaldo);
        return delExcel > 0 ? delExcel : respaldo;
    }

    // =====================================================
    // COBRO
    // =====================================================

    // Las monedas pagan al tocar las ruinas, el edificio o cualquier collider de este objeto.
    private void PrepararCobro()
    {
        foreach (Collider c in GetComponentsInChildren<Collider>(true))
            AnadirReceptor(c);
        if (partesRuinas != null)
            foreach (GameObject pieza in partesRuinas)
                if (pieza != null)
                    foreach (Collider c in pieza.GetComponentsInChildren<Collider>(true))
                        AnadirReceptor(c);
    }

    private void AnadirReceptor(Collider c)
    {
        if (c == null) return;
        ReceptorMonedasConstruccion receptor = c.GetComponent<ReceptorMonedasConstruccion>();
        if (receptor == null) receptor = c.gameObject.AddComponent<ReceptorMonedasConstruccion>();
        receptor.construccion = this;
    }

    /// <summary>Si 'otro' es una moneda lanzada (fusil o mano), paga y la destruye.</summary>
    public void IntentarCobrar(Collider otro)
    {
        if (otro == null || EnMaximo) return;

        Rigidbody rb = otro.attachedRigidbody;
        ProyectilArma proyectil = rb != null ? rb.GetComponent<ProyectilArma>() : null;
        if (proyectil == null) proyectil = otro.GetComponentInParent<ProyectilArma>();
        if (proyectil == null) return;

        Moneda moneda = proyectil.GetComponent<Moneda>();
        if (moneda == null) moneda = proyectil.GetComponentInChildren<Moneda>();
        if (moneda == null) return;

        if (frameCobro != Time.frameCount)
        {
            cobradosEsteFrame.Clear();
            frameCobro = Time.frameCount;
        }
        if (!cobradosEsteFrame.Add(proyectil)) return;

        Aportar(Mathf.Max(1, moneda.valor));
        bloop = fuerzaBloop;
        if (sonidoMoneda != null) audioSource.PlayOneShot(sonidoMoneda, volumen);
        Destroy(proyectil.gameObject);
    }

    private void Aportar(int cantidad)
    {
        monedasAportadas += cantidad;
        while (!EnMaximo && monedasAportadas >= PrecioSiguiente)
        {
            int precio = PrecioSiguiente;
            monedasAportadas -= precio;
            SubirNivel(precio);
        }
        if (EnMaximo) monedasAportadas = 0;

        ActualizarCartel();
        ProgresoActualizado?.Invoke();
    }

    // =====================================================
    // NIVELES
    // =====================================================

    private void SubirNivel(int precioPagado)
    {
        nivelActual++;
        string id = string.IsNullOrEmpty(idEconomia) ? "Construccion:" + nombre : idEconomia;
        EventosEconomia.NotificarCompra(id, nivelActual, precioPagado);
        if (sonidoConstruir != null) audioSource.PlayOneShot(sonidoConstruir, volumen);

        if (nivelActual == 1) StartCoroutine(Construir());
        else AplicarNivel(nivelActual, true);

        AplicarEfectoPorDefecto();
    }

    private IEnumerator Construir()
    {
        // Las ruinas se encogen y desaparecen.
        float t = 0f;
        List<Transform> ruinas = new List<Transform>();
        if (partesRuinas != null)
            foreach (GameObject p in partesRuinas)
                if (p != null && p.activeSelf) { ruinas.Add(p.transform); GuardarEscala(p.transform); }

        while (t < 1f)
        {
            t += Time.deltaTime / 0.35f;
            foreach (Transform r in ruinas)
                if (r != null) r.localScale = escalas[r] * Mathf.Lerp(1f, 0f, t * t);
            yield return null;
        }
        foreach (Transform r in ruinas)
            if (r != null) { r.gameObject.SetActive(false); r.localScale = escalas[r]; }

        AplicarEstado(true);
        AplicarNivel(1, true);
        alConstruir?.Invoke();
        ActualizarCartel();
    }

    // Lo que hay que activar o desactivar según esté construido o no.
    private void AplicarEstado(bool animar)
    {
        bool construida = Construida;

        if (partesRuinas != null)
            foreach (GameObject p in partesRuinas)
                if (p != null) p.SetActive(!construida);

        switch (tipo)
        {
            case Tipo.GeneradorAranas:
                GeneradorAranas generador = GetComponent<GeneradorAranas>();
                if (construida && generador != null) generador.Desbloquear();
                break;
            case Tipo.Fisionador:
                Fisionador fisionador = GetComponent<Fisionador>();
                if (construida && fisionador != null) fisionador.Desbloquear();
                break;
            case Tipo.TiendaEdificios:
                TiendaEdificiosTrigger tienda = GetComponent<TiendaEdificiosTrigger>();
                if (tienda != null) tienda.enabled = construida;
                MostrarHijos(construida, animar);
                break;
        }

        if (partesConstruidas != null)
            foreach (GameObject p in partesConstruidas)
                if (p != null)
                {
                    p.SetActive(construida);
                    if (construida && animar) StartCoroutine(Aparecer(p.transform));
                }

        for (int n = 2; n <= NivelMaximo; n++)
            AplicarPiezasNivel(n, nivelActual >= n, false);
    }

    private void AplicarNivel(int nivel, bool animar)
    {
        AplicarPiezasNivel(nivel, true, animar);
        if (nivel >= 1 && nivel <= niveles.Count) niveles[nivel - 1].alLlegar?.Invoke();
    }

    private void AplicarPiezasNivel(int nivel, bool alcanzado, bool animar)
    {
        if (nivel < 1 || nivel > niveles.Count) return;
        Nivel n = niveles[nivel - 1];
        if (n.piezasViejas != null)
            foreach (GameObject p in n.piezasViejas)
                if (p != null && alcanzado) p.SetActive(false);
        if (n.piezasNuevas != null)
            foreach (GameObject p in n.piezasNuevas)
                if (p != null)
                {
                    p.SetActive(alcanzado);
                    if (alcanzado && animar) StartCoroutine(Aparecer(p.transform));
                }
    }

    // Efectos que ya traen los niveles sin configurar nada (luego se pueden cambiar o ampliar con 'Al Llegar').
    private void AplicarEfectoPorDefecto()
    {
        if (tipo == Tipo.GeneradorAranas && nivelActual >= 2 && aranasExtraPorNivel > 0)
        {
            TiendaTrabajo tienda = FindAnyObjectByType<TiendaTrabajo>(FindObjectsInactive.Include);
            if (tienda != null) tienda.maximoBots += aranasExtraPorNivel;
        }
    }

    // =====================================================
    // TIENDA DE EDIFICIOS: CUBO Y MODELO
    // =====================================================

    // Hijos visibles del objeto (el modelo), sin contar el cubo ni el cartel.
    private readonly List<GameObject> hijosModelo = new List<GameObject>();

    private void MostrarHijos(bool mostrar, bool animar)
    {
        if (hijosModelo.Count == 0)
            foreach (Transform hijo in transform)
                if (hijo.gameObject.activeSelf && !EsRuina(hijo.gameObject) && (cartel == null || hijo != cartel.transform))
                    hijosModelo.Add(hijo.gameObject);

        foreach (GameObject hijo in hijosModelo)
        {
            if (hijo == null) continue;
            hijo.SetActive(mostrar);
            if (mostrar && animar) StartCoroutine(Aparecer(hijo.transform));
        }

        // Los renderers del propio objeto raíz (si los tiene).
        foreach (Renderer r in GetComponents<Renderer>()) r.enabled = mostrar;
    }

    private bool EsRuina(GameObject go)
    {
        if (partesRuinas == null) return false;
        foreach (GameObject p in partesRuinas) if (p == go) return true;
        return false;
    }

    // Un cubo gris del tamaño del edificio, como los de las otras ruinas.
    private void CrearCuboRuinas()
    {
        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubo.name = "Cubo " + nombre;
        cubo.transform.SetParent(transform, true);
        cubo.transform.position = limitesEdificio.center;
        cubo.transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        Vector3 tamano = limitesEdificio.size;
        cubo.transform.localScale = Vector3.one;
        Vector3 escalaPadre = cubo.transform.lossyScale;
        cubo.transform.localScale = new Vector3(tamano.x / Mathf.Max(0.0001f, escalaPadre.x),
                                                tamano.y / Mathf.Max(0.0001f, escalaPadre.y),
                                                tamano.z / Mathf.Max(0.0001f, escalaPadre.z));

        Material material = MaterialDeOtroCubo();
        if (material != null) cubo.GetComponent<Renderer>().sharedMaterial = material;

        partesRuinas = new[] { cubo };
    }

    // El material de los cubos de las otras ruinas, para que se vean igual.
    private static Material MaterialDeOtroCubo()
    {
        foreach (GeneradorAranas g in FindObjectsByType<GeneradorAranas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (g.objetosBloqueado != null)
                foreach (GameObject o in g.objetosBloqueado)
                    if (o != null && o.GetComponentInChildren<Renderer>(true) != null)
                        return o.GetComponentInChildren<Renderer>(true).sharedMaterial;
        foreach (Fisionador f in FindObjectsByType<Fisionador>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (f.objetosBloqueado != null)
                foreach (GameObject o in f.objetosBloqueado)
                    if (o != null && o.GetComponentInChildren<Renderer>(true) != null)
                        return o.GetComponentInChildren<Renderer>(true).sharedMaterial;
        return null;
    }

    private Bounds CalcularLimites()
    {
        Renderer[] renders = GetComponentsInChildren<Renderer>(false);
        bool hay = false;
        Bounds b = new Bounds(transform.position, Vector3.one * 2f);
        foreach (Renderer r in renders)
        {
            if (r is ParticleSystemRenderer || r is TrailRenderer) continue;
            if (!hay) { b = r.bounds; hay = true; }
            else b.Encapsulate(r.bounds);
        }
        if (partesRuinas != null)
            foreach (GameObject p in partesRuinas)
                if (p != null)
                    foreach (Renderer r in p.GetComponentsInChildren<Renderer>(true))
                    {
                        if (!hay) { b = r.bounds; hay = true; }
                        else b.Encapsulate(r.bounds);
                    }
        return b;
    }

    // =====================================================
    // ANIMACIONES
    // =====================================================

    private void GuardarEscala(Transform t)
    {
        if (t != null && !escalas.ContainsKey(t)) escalas[t] = t.localScale;
    }

    private IEnumerator Aparecer(Transform t)
    {
        GuardarEscala(t);
        Vector3 final = escalas[t];
        float k = 0f;
        while (k < 1f && t != null)
        {
            k += Time.deltaTime / 0.4f;
            float s = 1f + 2.2f * Mathf.Pow(Mathf.Clamp01(k) - 1f, 3f) + 1.2f * Mathf.Pow(Mathf.Clamp01(k) - 1f, 2f); // sale con rebote
            t.localScale = final * Mathf.Max(0f, s);
            yield return null;
        }
        if (t != null) t.localScale = final;
    }

    // Crece un poco con cada moneda: las ruinas si está sin construir, el edificio si ya está.
    private void AplicarBloop()
    {
        float factor = 1f + bloop;
        if (!Construida)
        {
            if (partesRuinas == null) return;
            foreach (GameObject p in partesRuinas)
                if (p != null) { GuardarEscala(p.transform); p.transform.localScale = escalas[p.transform] * factor; }
        }
        else
        {
            foreach (Transform hijo in transform)
            {
                if (cartel != null && hijo == cartel.transform) continue;
                if (!hijo.gameObject.activeSelf || EsRuina(hijo.gameObject)) continue;
                GuardarEscala(hijo);
                hijo.localScale = escalas[hijo] * factor;
            }
        }
    }

    // =====================================================
    // CARTEL
    // =====================================================

    private void CrearCartel()
    {
        if (!mostrarCartel) return;

        GameObject go = new GameObject("Cartel " + nombre);
        go.transform.SetParent(transform, true);
        go.transform.position = new Vector3(limitesEdificio.center.x, limitesEdificio.max.y + alturaCartel, limitesEdificio.center.z);
        go.transform.localScale = Vector3.one;
        Vector3 escalaPadre = go.transform.lossyScale;
        go.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, escalaPadre.x), 1f / Mathf.Max(0.0001f, escalaPadre.y),
                                              1f / Mathf.Max(0.0001f, escalaPadre.z)) * tamanoCartel * 0.1f;

        cartel = go.AddComponent<TextMeshPro>();
        cartel.alignment = TextAlignmentOptions.Center;
        cartel.fontSize = 10f;
        cartel.textWrappingMode = TextWrappingModes.NoWrap;

        TemaUINeon tema = TemaUINeon.Actual;
        if (tema != null)
        {
            if (tema.fuenteTMP != null) cartel.font = tema.fuenteTMP;
            if (tema.MaterialTexto != null) cartel.fontSharedMaterial = tema.MaterialTexto;
            cartel.color = tema.colorNeon;
        }
    }

    private void ActualizarCartel()
    {
        if (cartel == null) return;
        string texto;
        try
        {
            if (EnMaximo) texto = string.Format(formatoMaximo, nombre);
            else if (!Construida) texto = string.Format(formatoConstruir, nombre, monedasAportadas, PrecioSiguiente);
            else texto = string.Format(formatoMejorar, nombre, monedasAportadas, PrecioSiguiente, nivelActual + 1);
        }
        catch (FormatException)
        {
            texto = nombre;
        }
        cartel.text = texto;
    }

    private void OrientarCartel()
    {
        if (cartel == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        bool visible = (cam.transform.position - cartel.transform.position).sqrMagnitude <= distanciaCartel * distanciaCartel;
        if (cartel.enabled != visible) cartel.enabled = visible;
        if (!visible) return;

        Vector3 hacia = cartel.transform.position - cam.transform.position;
        hacia.y = 0f;
        if (hacia.sqrMagnitude > 0.0001f) cartel.transform.rotation = Quaternion.LookRotation(hacia, Vector3.up);
    }

    // =====================================================
    // GUARDADO
    // =====================================================

    /// <summary>Restaura nivel y monedas aportadas sin cobrar ni animar (cargar partida).</summary>
    public void RestaurarEstado(int nivel, int aportadas)
    {
        nivelActual = Mathf.Clamp(nivel, 0, NivelMaximo);
        monedasAportadas = EnMaximo ? 0 : Mathf.Clamp(aportadas, 0, Mathf.Max(0, PrecioSiguiente - 1));
        AplicarEstado(false);
        ActualizarCartel();
    }
}

