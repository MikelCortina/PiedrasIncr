using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// Generador de arañas: se interactúa con él como con la mesa de trabajo (E) y abre el menú de compra de bots.
// Al comprar una araña (TiendaTrabajo.ComprarBot) se crea aquí dentro, se abre la puerta lateral girándola sobre
// su bisagra y la araña sale andando por ella; después se va a su parking.
// La pantalla muestra una cara LED sonriente (CaraLedArana, el mismo material que la cara de las arañas).
// Va en la raíz del modelo GeneradorArañas: la puerta y la pantalla se buscan solas por nombre.
public class GeneradorAranas : MonoBehaviour
{
    // =====================================================
    // INTERACCIÓN
    // =====================================================

    [Header("Interacción")]
    public KeyCode teclaInteractuar = KeyCode.E;

    [Tooltip("Distancia extra alrededor del modelo en la que el jugador puede interactuar.")]
    public float margenInteraccion = 2f;

    [Tooltip("Vacío = usa el texto de interacción de la MesaTrabajo de la escena.")]
    public TextMeshProUGUI textoInteraccion;

    public string mensajeInteraccion = "Pulsa E para comprar arañas";


    // =====================================================
    // MENÚ
    // =====================================================

    [Header("Menú de compra")]
    [Tooltip("Panel con los botones de comprar araña (PanelComprarBots).")]
    public GameObject panelCompraBots;

    [Tooltip("Vacío = el TiendaTrabajo de la escena.")]
    public TiendaTrabajo tiendaTrabajo;

    [Tooltip("Opcional: botón de cerrar del panel (ButtonCerrarBot). Se le añade cerrar este menú.")]
    public Button botonCerrar;

    [Tooltip("Opcional: botón de comprar del panel (ButtonComprarBot). Muestra el precio o por qué no se puede comprar.")]
    public Button botonComprar;

    [Tooltip("Vacío = se busca en la escena.")]
    public CamaraPrimeraPersona controladorCamara;

    [Tooltip("Vacío = se busca en la escena.")]
    public ArmaLanzadora armaLanzadora;

    [Tooltip("Pausa el juego con el menú abierto, como la mesa de trabajo. La araña sale al cerrarlo.")]
    public bool pausarConMenu = true;


    // =====================================================
    // PUERTA
    // =====================================================

    [Header("Puerta")]
    [Tooltip("Vacío = el hijo llamado 'Nombre Puerta'.")]
    public Transform puerta;
    public string nombrePuerta = "Cube.001";

    [Tooltip("Pone la bisagra en el otro borde vertical de la puerta.")]
    public bool bisagraEnElOtroLado = false;

    [Tooltip("Grados que gira la puerta hacia fuera (el sentido se calcula solo).")]
    [Range(10f, 170f)] public float anguloApertura = 90f;
    public float duracionApertura = 0.6f;
    public AnimationCurve curvaApertura = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("Segundos entre que aparece la araña dentro del generador y se empieza a abrir la puerta.")]
    public float esperaAntesDeAbrir = 0.3f;

    [Tooltip("Segundos que espera con la puerta abierta tras salir la última araña.")]
    public float esperaAntesDeCerrar = 0.8f;

    [Tooltip("Sistemas de partículas que se reproducen cada vez que la puerta empieza a abrirse.")]
    public List<ParticleSystem> particulasAlAbrir = new List<ParticleSystem>();


    // =====================================================
    // SALIDA DE LA ARAÑA
    // =====================================================

    [Header("Salida de la araña")]
    [Tooltip("Cuánto hacia dentro del generador aparece la araña antes de salir.")]
    public float profundidadInterior = 0.6f;

    [Tooltip("Hueco que deja entre la araña y la cara interior de la puerta al aparecer (para que no la atraviese).")]
    public float margenPuerta = 0.05f;

    [Tooltip("Hasta dónde anda por fuera de la puerta antes de ponerse a trabajar.")]
    public float distanciaSalida = 2.5f;

    [Tooltip("Velocidad al salir. 0 = la velocidad configurada del bot.")]
    public float velocidadSalida = 0f;

    [Tooltip("Al salir se va a su parking (si no, se queda en pausa junto a la puerta).")]
    public bool irAlParkingAlSalir = true;


    // =====================================================
    // PANTALLA
    // =====================================================

    [Header("Pantalla")]
    [Tooltip("Vacío = el Renderer hijo que tenga el material 'Material Pantalla'.")]
    public Renderer pantalla;
    public string materialPantalla = "Material.001";
    [Tooltip("Cara de la pantalla. Solo se usa si la pantalla aún no tiene CaraLedArana.")]
    public CaraLedArana.Expresion expresionPantalla = CaraLedArana.Expresion.SonrienteConFallos;


    // =====================================================
    // INTERNAS
    // =====================================================

    private TextMeshProUGUI textoBotonComprar;
    private bool jugadorCerca;
    private bool menuAbierto;
    private bool armaEstabaActiva;
    private string textoOriginal;

    private Transform bisagra;
    private bool puertaMedida;             // datos de la puerta tomados con ella cerrada
    private GeometriaPuerta geometriaPuerta;
    private float signoGiro = 1f;
    private Quaternion rotacionCerrada;
    private float aperturaActual;          // 0 cerrada, 1 abierta
    private Coroutine movimientoPuerta;

    private Transform contenedorEnEspera;  // inactivo: las arañas compradas esperan aquí su turno
    private readonly Queue<GameObject> colaSalida = new Queue<GameObject>();
    private bool sacandoAranas;


    // =====================================================
    // INICIO
    // =====================================================

    private void Awake()
    {
        // En la raíz de la escena (no hijo del generador) para que la escala del generador no se pase a las arañas.
        GameObject contenedor = new GameObject("ArañasEnEspera (" + name + ")");
        contenedor.SetActive(false);
        contenedorEnEspera = contenedor.transform;

        CrearZonaInteraccion();
        PrepararBisagra();
        PonerCara();
    }

    private void Start()
    {
        if (tiendaTrabajo == null) tiendaTrabajo = FindFirstObjectByType<TiendaTrabajo>();
        if (tiendaTrabajo != null && tiendaTrabajo.generadorAranas == null) tiendaTrabajo.generadorAranas = this;

        if (controladorCamara == null) controladorCamara = FindFirstObjectByType<CamaraPrimeraPersona>();
        if (armaLanzadora == null) armaLanzadora = FindFirstObjectByType<ArmaLanzadora>();

        if (textoInteraccion == null)
        {
            MesaTrabajo mesa = FindFirstObjectByType<MesaTrabajo>();
            if (mesa != null) textoInteraccion = mesa.textoInteraccion;
        }

        // Sin asignar: el mismo panel que usa el terminal de compra de bots, y su botón "Cerrar".
        if (panelCompraBots == null)
        {
            TerminalCompraBots terminal = FindFirstObjectByType<TerminalCompraBots>(FindObjectsInactive.Include);
            if (terminal != null) panelCompraBots = terminal.panelCompraBots;
        }
        if (botonCerrar == null && panelCompraBots != null)
            foreach (Button boton in panelCompraBots.GetComponentsInChildren<Button>(true))
                if (boton.name.Contains("Cerrar")) { botonCerrar = boton; break; }

        if (botonCerrar != null) botonCerrar.onClick.AddListener(CerrarMenu);

        if (botonComprar == null && panelCompraBots != null)
            foreach (Button boton in panelCompraBots.GetComponentsInChildren<Button>(true))
                if (boton.name.Contains("Comprar")) { botonComprar = boton; break; }

        if (botonComprar != null)
        {
            textoBotonComprar = botonComprar.GetComponentInChildren<TextMeshProUGUI>(true);
            // Se ejecuta después de TiendaTrabajo.ComprarBot (listener del Inspector): si no compró, dice por qué.
            botonComprar.onClick.AddListener(RefrescarBotonComprar);
        }
    }

    // Trigger que rodea todo el modelo con un margen: el jugador lo pisa para poder interactuar.
    private void CrearZonaInteraccion()
    {
        if (!CalcularLimitesLocales(GetComponentsInChildren<Renderer>(true), null, out Bounds limites))
            limites = new Bounds(Vector3.zero, Vector3.one * 2f);

        BoxCollider zona = gameObject.AddComponent<BoxCollider>();
        zona.isTrigger = true;
        zona.center = limites.center;

        Vector3 escala = transform.lossyScale;
        Vector3 margen = new Vector3(
            margenInteraccion / Mathf.Max(Mathf.Abs(escala.x), 0.0001f),
            margenInteraccion / Mathf.Max(Mathf.Abs(escala.y), 0.0001f),
            margenInteraccion / Mathf.Max(Mathf.Abs(escala.z), 0.0001f));
        zona.size = limites.size + margen * 2f;
    }

    private void PonerCara()
    {
        if (pantalla == null)
        {
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material m in r.sharedMaterials)
                    if (m != null && m.name == materialPantalla) { pantalla = r; break; }
                if (pantalla != null) break;
            }
        }

        if (pantalla == null)
        {
            Debug.LogWarning("[GeneradorAranas] No encuentro la pantalla (material '" + materialPantalla + "').", this);
            return;
        }

        // Si ya tiene cara (puesta a mano o con la herramienta de pantallas) se respeta su configuración.
        if (pantalla.GetComponent<CaraLedArana>() != null) return;

        CaraLedArana cara = pantalla.gameObject.AddComponent<CaraLedArana>();
        cara.apagarNeonDelSlot = false;
        cara.superponerQuad = true; // las UV de esta pantalla están giradas: la cara va en un quad delante
        foreach (Material m in pantalla.sharedMaterials)
            if (m != null && m.name == materialPantalla) cara.nombreMaterial = materialPantalla;
        cara.expresion = expresionPantalla;
    }


    // =====================================================
    // INTERACCIÓN
    // =====================================================

    private void Update()
    {
        if (menuAbierto)
        {
            if (Input.GetKeyDown(teclaInteractuar) || Input.GetKeyDown(KeyCode.Escape))
                CerrarMenu();
            return;
        }

        if (jugadorCerca && Input.GetKeyDown(teclaInteractuar))
            AbrirMenu();
    }

    private static bool EsJugador(Collider otro)
    {
        return otro.CompareTag("Player") || otro.GetComponentInParent<CharacterController>() != null;
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (!EsJugador(otro)) return;
        jugadorCerca = true;
        if (!menuAbierto) MostrarAviso(true);
    }

    private void OnTriggerExit(Collider otro)
    {
        if (!EsJugador(otro)) return;
        jugadorCerca = false;
        MostrarAviso(false);
    }

    // El texto puede ser el mismo que usa la mesa: se guarda el suyo y se devuelve al ocultarlo.
    private void MostrarAviso(bool mostrar)
    {
        if (textoInteraccion == null) return;

        if (mostrar)
        {
            textoOriginal = textoInteraccion.text;
            textoInteraccion.text = mensajeInteraccion;
            textoInteraccion.gameObject.SetActive(true);
        }
        else if (textoInteraccion.text == mensajeInteraccion)
        {
            textoInteraccion.gameObject.SetActive(false);
            if (textoOriginal != null) textoInteraccion.text = textoOriginal;
        }
    }

    public void AbrirMenu()
    {
        if (menuAbierto) return;
        if (panelCompraBots == null || tiendaTrabajo == null)
        {
            Debug.LogWarning("[GeneradorAranas] Falta asignar el Panel Compra Bots o no hay TiendaTrabajo.", this);
            return;
        }

        menuAbierto = true;
        MostrarAviso(false);

        panelCompraBots.SetActive(true);
        tiendaTrabajo.RefrescarTienda();
        RefrescarBotonComprar();

        if (controladorCamara != null) controladorCamara.SetBloqueadaPorUI(true);
        if (armaLanzadora != null)
        {
            armaEstabaActiva = armaLanzadora.enabled;
            armaLanzadora.enabled = false;
        }

        if (pausarConMenu) Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CerrarMenu()
    {
        if (!menuAbierto) return;
        menuAbierto = false;

        if (panelCompraBots != null) panelCompraBots.SetActive(false);
        RestaurarJuego();

        if (jugadorCerca) MostrarAviso(true);
    }

    private void RestaurarJuego()
    {
        if (pausarConMenu) Time.timeScale = 1f;
        if (controladorCamara != null) controladorCamara.SetBloqueadaPorUI(false);
        if (armaLanzadora != null) armaLanzadora.enabled = armaEstabaActiva;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public bool EstaMenuAbierto() => menuAbierto;

    private void RefrescarBotonComprar()
    {
        if (botonComprar == null || tiendaTrabajo == null || !menuAbierto) return;

        bool puede = tiendaTrabajo.PuedeComprarBot(out string texto);
        botonComprar.interactable = puede;
        if (textoBotonComprar != null) textoBotonComprar.text = texto;
    }

    private void OnDestroy()
    {
        if (contenedorEnEspera != null) Destroy(contenedorEnEspera.gameObject);
    }

    private void OnDisable()
    {
        if (!menuAbierto) return;
        menuAbierto = false;
        if (panelCompraBots != null) panelCompraBots.SetActive(false);
        RestaurarJuego();
    }


    // =====================================================
    // CREAR Y SACAR ARAÑAS (lo llama TiendaTrabajo)
    // =====================================================

    // Crea la araña dentro del generador, todavía inactiva (sin NavMesh ni IA): así TiendaTrabajo puede configurarla
    // y registrarla antes de que se despierte.
    public GameObject CrearBot(GameObject prefab)
    {
        CalcularSalida(out Vector3 interior, out _, out Quaternion mirando);
        return Instantiate(prefab, interior, mirando, contenedorEnEspera);
    }

    public void SacarBot(GameObject bot)
    {
        if (bot == null) return;
        colaSalida.Enqueue(bot);

        // Comprada: se cierra el menú (y se quita la pausa) para ver cómo se abre la puerta y sale.
        if (menuAbierto) CerrarMenu();
        if (!sacandoAranas) StartCoroutine(SacarAranas());
    }

    private IEnumerator SacarAranas()
    {
        sacandoAranas = true;

        while (colaSalida.Count > 0)
        {
            GameObject bot = colaSalida.Dequeue();
            if (bot == null) continue;

            yield return SacarUna(bot); // aparece dentro, se abre la puerta y sale

            if (colaSalida.Count == 0)
                yield return new WaitForSeconds(esperaAntesDeCerrar);
        }

        yield return MoverPuerta(0f);
        sacandoAranas = false;
    }

    private IEnumerator SacarUna(GameObject bot)
    {
        NavMeshAgent agente = bot.GetComponent<NavMeshAgent>();
        BotRecolector recolector = bot.GetComponent<BotRecolector>();
        ConfiguracionBot configuracion = bot.GetComponent<ConfiguracionBot>();

        // Sale andando "a mano": sin agente (no está sobre el NavMesh) ni IA hasta pisar fuera.
        if (agente != null) agente.enabled = false;
        if (recolector != null) recolector.enabled = false;

        CalcularSalida(out Vector3 interior, out Vector3 exterior, out Quaternion mirando);
        bot.transform.SetPositionAndRotation(interior, mirando);
        bot.transform.SetParent(null, true); // aquí aparece (dentro del generador)

        // Que nada de la araña (cabeza, patas) asome por la puerta cerrada: se mete hacia dentro lo que sobre.
        float sobra = SobresaleDeLaPuerta(bot);
        if (sobra > 0f && DatosPuerta(out GeometriaPuerta puertaDatos))
        {
            interior -= puertaDatos.normal * sobra;
            bot.transform.position = interior;
        }

        // Primero aparece la araña y después se abre la puerta.
        if (esperaAntesDeAbrir > 0f) yield return new WaitForSeconds(esperaAntesDeAbrir);
        if (aperturaActual < 0.99f) ReproducirParticulasApertura(); // solo si estaba cerrada (no entre arañas seguidas)
        yield return MoverPuerta(1f);

        float velocidad = velocidadSalida > 0f ? velocidadSalida
                        : configuracion != null ? configuracion.velocidadMovimiento : 2f;
        float distancia = Vector3.Distance(interior, exterior);
        float duracion = distancia / Mathf.Max(velocidad, 0.1f);

        for (float t = 0f; t < duracion && bot != null; t += Time.deltaTime)
        {
            bot.transform.position = Vector3.Lerp(interior, exterior, t / duracion);
            yield return null;
        }
        if (bot == null) yield break;
        bot.transform.position = exterior;

        // Ya fuera: vuelve a ser un bot normal.
        bool enNavMesh = NavMesh.SamplePosition(exterior, out NavMeshHit hit, Mathf.Max(3f, distanciaSalida), NavMesh.AllAreas);
        if (enNavMesh) bot.transform.position = hit.position;
        else Debug.LogWarning("[GeneradorAranas] No hay NavMesh junto a la puerta: la araña irá directa a su parking.", this);

        if (agente != null)
        {
            agente.enabled = true;
            if (enNavMesh) agente.Warp(hit.position);
        }

        if (recolector != null)
        {
            recolector.aparecerEnParkingAlIniciar = !enNavMesh;
            recolector.empezarEnPausa = true;
            recolector.enabled = true;
        }

        // Su Start (que busca parking, procesadora...) corre en el siguiente frame.
        yield return null;
        yield return null;

        if (irAlParkingAlSalir && enNavMesh && recolector != null && bot != null)
        {
            if (configuracion != null) configuracion.modoActual = ConfiguracionBot.ModoBot.Parking;
            recolector.ConfigurarModoParking();
        }
    }


    // =====================================================
    // GEOMETRÍA DE LA PUERTA
    // =====================================================

    // Datos de la puerta en el MUNDO, con la vertical del mundo como eje de la bisagra (así da igual cómo esté girado
    // el modelo): centro de la puerta, centro abajo, normal hacia fuera (horizontal), eje lateral y medio ancho/grosor.
    private struct GeometriaPuerta
    {
        public Vector3 centro, centroAbajo, normal, lateral;
        public float medioGrosor, medioAncho;
    }

    private bool DatosPuerta(out GeometriaPuerta datos)
    {
        if (puertaMedida) { datos = geometriaPuerta; return true; }

        datos = default;
        if (puerta == null) return false;

        // Ejes horizontales de referencia: los del generador proyectados sobre el suelo.
        Vector3 adelante = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (adelante.sqrMagnitude < 1e-4f) adelante = Vector3.ProjectOnPlane(transform.up, Vector3.up);
        adelante.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante);

        if (!Limites(puerta.GetComponentsInChildren<Renderer>(true), null, derecha, adelante, out Bounds caja)) return false;
        Limites(GetComponentsInChildren<Renderer>(true), puerta, derecha, adelante, out Bounds cuerpo);

        // La puerta es una placa: su eje horizontal más fino es el que apunta hacia fuera del cuerpo.
        bool finaEnX = caja.size.x <= caja.size.z;
        Vector3 eje = finaEnX ? derecha : adelante;
        float delta = finaEnX ? caja.center.x - cuerpo.center.x : caja.center.z - cuerpo.center.z;

        datos.normal = eje * (delta >= 0f ? 1f : -1f);
        datos.lateral = finaEnX ? adelante : derecha;
        datos.medioGrosor = (finaEnX ? caja.size.x : caja.size.z) * 0.5f;
        datos.medioAncho = (finaEnX ? caja.size.z : caja.size.x) * 0.5f;
        datos.centro = derecha * caja.center.x + Vector3.up * caja.center.y + adelante * caja.center.z;
        datos.centroAbajo = derecha * caja.center.x + Vector3.up * caja.min.y + adelante * caja.center.z;
        return true;
    }

    // Puntos (en el mundo) donde aparece la araña dentro y hasta donde sale fuera, y hacia dónde mira.
    private void CalcularSalida(out Vector3 interior, out Vector3 exterior, out Quaternion mirando)
    {
        Vector3 normalMundo, base_;
        if (DatosPuerta(out GeometriaPuerta datos))
        {
            base_ = datos.centroAbajo;
            normalMundo = datos.normal;
        }
        else
        {
            base_ = transform.position;
            normalMundo = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            if (normalMundo.sqrMagnitude < 0.01f) normalMundo = Vector3.forward;
        }

        exterior = base_ + normalMundo * distanciaSalida;
        if (NavMesh.SamplePosition(exterior, out NavMeshHit hit, Mathf.Max(3f, distanciaSalida), NavMesh.AllAreas))
            exterior = hit.position;

        interior = exterior - normalMundo * (distanciaSalida + profundidadInterior);
        mirando = Quaternion.LookRotation(normalMundo, Vector3.up);
    }

    // Mete la puerta dentro de un objeto "bisagra" colocado en su borde vertical exterior: girar la bisagra
    // alrededor de la vertical del mundo la abre hacia fuera.
    private void PrepararBisagra()
    {
        if (puerta == null && !string.IsNullOrEmpty(nombrePuerta))
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (t.name == nombrePuerta) { puerta = t; break; }

        if (!DatosPuerta(out GeometriaPuerta datos))
        {
            Debug.LogWarning("[GeneradorAranas] No encuentro la puerta ('" + nombrePuerta + "').", this);
            return;
        }

        geometriaPuerta = datos;
        puertaMedida = true;

        float signoLateral = bisagraEnElOtroLado ? 1f : -1f;
        Vector3 puntoBisagra = datos.centro
                             + datos.normal * datos.medioGrosor                   // cara de fuera
                             + datos.lateral * (datos.medioAncho * signoLateral); // borde vertical

        GameObject objetoBisagra = new GameObject("BisagraPuerta");
        bisagra = objetoBisagra.transform;
        bisagra.SetParent(transform, false);
        bisagra.SetPositionAndRotation(puntoBisagra, Quaternion.identity);
        puerta.SetParent(bisagra, true);
        rotacionCerrada = bisagra.rotation;

        // Sentido de giro: el que lleva el centro de la puerta hacia fuera. Se guarda aparte: el ángulo del
        // Inspector solo dice cuántos grados y nunca se modifica.
        Vector3 haciaCentro = datos.centro - puntoBisagra;
        Vector3 conMas = Quaternion.AngleAxis(Mathf.Abs(anguloApertura), Vector3.up) * haciaCentro;
        Vector3 conMenos = Quaternion.AngleAxis(-Mathf.Abs(anguloApertura), Vector3.up) * haciaCentro;
        signoGiro = Vector3.Dot(conMenos, datos.normal) > Vector3.Dot(conMas, datos.normal) ? -1f : 1f;
    }

    // Cuánto asoma la araña por delante de la cara interior de la puerta (0 si no la toca).
    private float SobresaleDeLaPuerta(GameObject bot)
    {
        if (!DatosPuerta(out GeometriaPuerta datos)) return 0f;

        float planoInterior = Vector3.Dot(datos.centro - datos.normal * datos.medioGrosor, datos.normal) - margenPuerta;
        float delante = float.MinValue;

        foreach (Renderer r in bot.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            Bounds b = r.bounds;
            // Punto de la caja más adelantado en la dirección de salida.
            Vector3 n = datos.normal;
            Vector3 extremo = b.center + new Vector3(Mathf.Sign(n.x) * b.extents.x, Mathf.Sign(n.y) * b.extents.y, Mathf.Sign(n.z) * b.extents.z);
            delante = Mathf.Max(delante, Vector3.Dot(extremo, n));
        }

        return delante == float.MinValue ? 0f : Mathf.Max(0f, delante - planoInterior);
    }

    private void ReproducirParticulasApertura()
    {
        foreach (ParticleSystem particulas in particulasAlAbrir)
        {
            if (particulas == null) continue;
            if (!particulas.gameObject.activeSelf) particulas.gameObject.SetActive(true);

            // Reinicia la emisión (las que ya estaban en el aire siguen) y vuelve a empezar, también los hijos.
            particulas.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            particulas.Play(true);
        }
    }

    private IEnumerator MoverPuerta(float objetivo)
    {
        if (bisagra == null) yield break;

        float inicio = aperturaActual;
        float duracion = Mathf.Max(duracionApertura * Mathf.Abs(objetivo - inicio), 0.0001f);
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            aperturaActual = Mathf.Lerp(inicio, objetivo, t / duracion);
            GirarPuerta();
            yield return null;
        }
        aperturaActual = objetivo;
        GirarPuerta();
    }

    // Giro alrededor de la vertical del mundo, desde la posición cerrada.
    private void GirarPuerta()
    {
        float grados = signoGiro * Mathf.Abs(anguloApertura) * curvaApertura.Evaluate(aperturaActual);
        bisagra.rotation = Quaternion.AngleAxis(grados, Vector3.up) * rotacionCerrada;
    }

    // Caja que envuelve esos renderers en un marco del mundo: x = derecha, y = vertical del mundo, z = adelante.
    private static bool Limites(Renderer[] renderers, Transform ignorar, Vector3 derecha, Vector3 adelante, out Bounds limites)
    {
        limites = default;
        bool hay = false;
        foreach (Renderer r in renderers)
        {
            if (r == null || (ignorar != null && r.transform.IsChildOf(ignorar))) continue;
            Bounds local = r.localBounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 esquina = local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = r.transform.TransformPoint(esquina);
                Vector3 punto = new Vector3(Vector3.Dot(p, derecha), p.y, Vector3.Dot(p, adelante));
                if (!hay) { limites = new Bounds(punto, Vector3.zero); hay = true; }
                else limites.Encapsulate(punto);
            }
        }
        return hay;
    }

    // Caja que envuelve esos renderers, en el espacio local del generador.
    private bool CalcularLimitesLocales(Renderer[] renderers, Transform ignorar, out Bounds limites)
    {
        limites = default;
        bool hay = false;
        foreach (Renderer r in renderers)
        {
            if (r == null || (ignorar != null && r.transform.IsChildOf(ignorar))) continue;
            Bounds local = r.localBounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 esquina = local.center + Vector3.Scale(local.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 punto = transform.InverseTransformPoint(r.transform.TransformPoint(esquina));
                if (!hay) { limites = new Bounds(punto, Vector3.zero); hay = true; }
                else limites.Encapsulate(punto);
            }
        }
        return hay;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying && puerta == null) return;
        CalcularSalida(out Vector3 interior, out Vector3 exterior, out _);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(interior, exterior);
        Gizmos.DrawWireSphere(exterior, 0.3f);
    }
}
