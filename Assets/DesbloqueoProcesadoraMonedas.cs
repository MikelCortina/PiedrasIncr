using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Collections;

/// <summary>
/// Se coloca en un objeto SIEMPRE ACTIVO con un Collider marcado Is Trigger.
/// Recibe monedas disparadas (ProyectilArma + Moneda) para construir la
/// desgastadora y para pagar sus mejoras de nivel. La tienda usa el MISMO
/// saldo de progreso a través de ComprarMejoraDesdeTienda().
/// </summary>
public class DesbloqueoProcesadoraMonedas : MonoBehaviour
{
    [System.Serializable]
    public class PiezasEvolucion
    {
        [Tooltip("Piezas que se encogerán y desaparecerán al alcanzar este nivel.")]
        public GameObject[] piezasViejas;
        [Tooltip("Piezas que brotarán y aparecerán al alcanzar este nivel.")]
        public GameObject[] piezasNuevas;
    }

    [Header("Evolución Visual (Nivel 1 - Construcción)")]
    [Tooltip("Objeto visual PADRE de las ruinas enteras.")]
    public GameObject ruinasVisual;
    [Tooltip("Lista de piezas individuales que forman las ruinas. Se encogerán en cascada al completarse la construcción.")]
    public GameObject[] partesRuinas;
    [Space(5)]
    [Tooltip("El contenedor padre de toda la máquina. Ayuda a organizar la jerarquía.")]
    public GameObject maquinaDesbloqueada;
    [Tooltip("Lista de piezas que componen la máquina inicial (Nivel 1). Brotarán en cascada al construir.")]
    public GameObject[] piezasMaquinaBase;

    [Header("Referencias de la Máquina")]
    public MaquinaErosion maquinaErosion;

    [Header("Evolución Visual (Niveles Superiores)")]
    public PiezasEvolucion piezasNivel2;
    public PiezasEvolucion piezasNivel3;

    [Header("Precio de construcción")]
    [Min(1)] public int monedasNecesarias = 20;
    [SerializeField, Min(0)] private int monedasRecibidas = 0;

    [Header("Precio de las mejoras")]
    [Min(1)] public int precioNivel2 = 10;
    [Min(1)] public int precioNivel3 = 20;
    [SerializeField, Min(0)] private int monedasMejoraRecibidas = 0;
    [SerializeField] private int nivelDeMejoraEnPago = 2;

    [Header("Tienda y texto")]
    public Cartera cartera;
    public TiendaTrabajo tiendaTrabajo;
    [Tooltip("Texto de mundo OPCIONAL, mejor situado en un Canvas junto a las ruinas.")]
    public TMP_Text textoProgreso;

    [Header("Efectos opcionales")]
    public ParticleSystem[] particulasDesbloqueo;
    public AudioSource audioSource;
    public AudioClip sonidoMoneda;
    public AudioClip sonidoDesbloqueo;
    public AudioClip sonidoMejora;

    [Header("Antena de Progreso (LED)")]
    [Tooltip("El objeto 3D de la antena. Ahora se mantiene activa en todos los niveles.")]
    public Transform antenaVisual;
    [Tooltip("El script ControladorNeon asignado a la bombilla de la antena.")]
    public ControladorNeon ledAntena;

    [Space(5)]
    [ColorUsage(true, true)] public Color colorInicio = Color.red;
    [ColorUsage(true, true)] public Color colorFinal = Color.green;

    [Range(0f, 10f)] public float intensidadInicio = 1f;
    [Range(0f, 10f)] public float intensidadFinal = 5f;

    private float progresoVisualLed = 0f;
    private Vector3 escalaOriginalAntena;
    private Quaternion rotacionOriginalAntena;

    // ==========================================
    // SISTEMA DE PANTALLA (RENDER TEXTURE)
    // ==========================================
    [Header("Pantalla LED Antena (Render Texture)")]
    [Tooltip("El texto TextMeshPro que muestra el contador.")]
    public TMP_Text textoPantallaAntena;
    [Tooltip("Velocidad a la que bajan los números visualmente.")]
    public float velocidadNumerosPantalla = 15f;
    [Tooltip("Formato del texto. Pon '0' para números normales, '00' si quieres que el 5 se vea como 05.")]
    public string formatoNumeros = "0";

    private float valorMostradoEnPantalla = 0f;
    // ==========================================

    [Header("Game Feel: Bloop de Antena")]
    [Tooltip("Cuánto crece la antena al recibir una moneda (1.15 = 15% más grande).")]
    public float multiplicadorBloopAntena = 1.15f;
    [Tooltip("Velocidad a la que la antena recupera su tamaño original tras el bloop.")]
    public float velocidadBloopAntena = 15f;

    [Header("Game Feel: Bloop de Estructura")]
    [Tooltip("Cuánto crece el cuerpo (ruinas/arma) al recibir una moneda. Usa valores bajos para simular peso (ej: 1.05).")]
    public float multiplicadorBloopEstructura = 1.05f;
    [Tooltip("Velocidad a la que el cuerpo recupera su tamaño original.")]
    public float velocidadBloopEstructura = 12f;

    [Header("Game Feel: Sway Físico de Antena")]
    [Tooltip("Fuerza del golpe. Se sumará como velocidad pura, permitiendo múltiples impactos solapados.")]
    public float fuerzaImpactoMoneda = 1500f;
    [Tooltip("Rigidez del muelle. Valores altos = intenta volver al centro con mucha fuerza.")]
    public float rigidezAntena = 250f;
    [Tooltip("Fricción. Valores bajos = la antena se quedará rebotando más tiempo (efecto muelle loco).")]
    public float amortiguacionAntena = 15f;

    // VARIABLES DEL SISTEMA FÍSICO DEL MUELLE
    private Vector2 velocidadSwayAntena;
    private Vector2 swayActualAntena;

    [Header("Animación de Construcción y Mejora")]
    public float velocidadDesaparicion = 10f;
    public float velocidadAparicion = 15f;
    [Tooltip("Tiempo (0.1 a 1) para solapar las animaciones. 0.1 = Ráfaga inmediata. 1.0 = Escalonado.")]
    [Range(0.1f, 1f)] public float porcentajeCascada = 0.5f;

    [Header("Rebote de la Maquina al Construir")]
    public float velocidadReboteArma = 12f;
    public float intensidadReboteArma = 0.15f;

    // Diccionario de escalas: Guarda el tamaño de CUALQUIER pieza automáticamente
    private Dictionary<GameObject, Vector3> escalasOriginales = new Dictionary<GameObject, Vector3>();

    private Coroutine corrutinaBloopAntena;
    private Coroutine corrutinaBloopEstructura;
    private bool enTransicion = false;
    private int piezasEnAnimacion = 0;

    // Compatibilidad con UIProcesadoraMundo.cs ya existente.
    public int MonedasRecibidas => monedasRecibidas;
    public int MonedasNecesarias => monedasNecesarias;
    public int MonedasMejoraRecibidas => monedasMejoraRecibidas;

    public int PrecioMejoraActual
    {
        get
        {
            if (maquinaErosion == null || !maquinaErosion.ProcesadoraDesbloqueada)
                return 0;
            if (maquinaErosion.NivelProcesado == 1) return precioNivel2;
            if (maquinaErosion.NivelProcesado == 2) return precioNivel3;
            return 0;
        }
    }

    public int PrecioMejoraRestante =>
        Mathf.Max(0, PrecioMejoraActual - monedasMejoraRecibidas);

    private readonly HashSet<ProyectilArma> proyectilesRecibidosFrame = new HashSet<ProyectilArma>();
    private int ultimoFrameImpacto = -1;

    private void OnValidate()
    {
        monedasNecesarias = Mathf.Max(1, monedasNecesarias);
        precioNivel2 = Mathf.Max(1, precioNivel2);
        precioNivel3 = Mathf.Max(1, precioNivel3);
        monedasRecibidas = Mathf.Clamp(monedasRecibidas, 0, monedasNecesarias);
        monedasMejoraRecibidas = Mathf.Max(0, monedasMejoraRecibidas);
    }

    private void Awake()
    {
        if (maquinaErosion == null)
            maquinaErosion = GetComponentInParent<MaquinaErosion>();
        if (maquinaDesbloqueada == null && maquinaErosion != null)
            maquinaDesbloqueada = maquinaErosion.gameObject;
    }

    private void RegistrarEscalas(GameObject[] piezas)
    {
        if (piezas == null) return;
        foreach (var pieza in piezas)
        {
            if (pieza != null && !escalasOriginales.ContainsKey(pieza))
            {
                escalasOriginales.Add(pieza, pieza.transform.localScale);
            }
        }
    }

    private void Start()
    {
        if (maquinaErosion == null || ruinasVisual == null || maquinaDesbloqueada == null || piezasMaquinaBase == null || piezasMaquinaBase.Length == 0)
        {
            Debug.LogError(name + ": Faltan asignar referencias en el inspector (MaquinaErosion, RuinasVisual, MaquinaDesbloqueada o PiezasMaquinaBase).", this);
            enabled = false;
            return;
        }

        // Al desactivar estos objetos el detector de monedas debe seguir activo.
        if (transform.IsChildOf(ruinasVisual.transform) ||
            transform.IsChildOf(maquinaDesbloqueada.transform))
        {
            Debug.LogError(name + ": mueve este componente/trigger fuera de Ruinas y de la máquina. Debe permanecer activo.", this);
            enabled = false;
            return;
        }

        // Registramos todos los tamaños iniciales
        RegistrarEscalas(new GameObject[] { ruinasVisual, maquinaDesbloqueada });
        RegistrarEscalas(partesRuinas);
        RegistrarEscalas(piezasMaquinaBase);
        RegistrarEscalas(piezasNivel2.piezasViejas); RegistrarEscalas(piezasNivel2.piezasNuevas);
        RegistrarEscalas(piezasNivel3.piezasViejas); RegistrarEscalas(piezasNivel3.piezasNuevas);

        if (antenaVisual != null)
        {
            escalaOriginalAntena = antenaVisual.localScale;
            rotacionOriginalAntena = antenaVisual.localRotation;
        }

        // Inicializamos el valor de la pantalla
        bool desbloqueada = maquinaErosion.ProcesadoraDesbloqueada;
        if (desbloqueada) monedasRecibidas = monedasNecesarias;

        SincronizarNivelPagado();

        bool maximo = maquinaErosion.ProcesadoAlMaximo();
        if (!desbloqueada) valorMostradoEnPantalla = monedasNecesarias - monedasRecibidas;
        else valorMostradoEnPantalla = maximo ? 0f : PrecioMejoraRestante;

        SincronizarAspecto(false);
        ActualizarTexto();
        RefrescarTienda();
    }

    private void ApagarPiezasFuturas()
    {
        if (maquinaErosion == null) return;
        int nivelActual = maquinaErosion.NivelProcesado;
        bool construida = maquinaErosion.ProcesadoraDesbloqueada;

        if (nivelActual < 2 || !construida)
        {
            if (piezasNivel2.piezasNuevas != null)
                foreach (var p in piezasNivel2.piezasNuevas)
                    if (p) p.SetActive(false);
        }

        if (nivelActual < 3 || !construida)
        {
            if (piezasNivel3.piezasNuevas != null)
                foreach (var p in piezasNivel3.piezasNuevas)
                    if (p) p.SetActive(false);
        }
    }

    private void Update()
    {
        bool maximo = maquinaErosion != null && maquinaErosion.ProcesadoAlMaximo();
        bool construida = maquinaErosion != null && maquinaErosion.ProcesadoraDesbloqueada;

        float progresoObjetivo = 0f;
        float monedasFaltantesReales = 0f;

        if (!construida)
        {
            progresoObjetivo = (float)monedasRecibidas / monedasNecesarias;
            monedasFaltantesReales = monedasNecesarias - monedasRecibidas;
        }
        else if (!maximo)
        {
            if (PrecioMejoraActual > 0) progresoObjetivo = (float)monedasMejoraRecibidas / PrecioMejoraActual;
            monedasFaltantesReales = PrecioMejoraRestante;
        }
        else
        {
            progresoObjetivo = 1f;
            monedasFaltantesReales = 0f;
        }

        // ==========================================
        // 1. ACTUALIZAR LED Y PANTALLA (RENDER TEXTURE)
        // ==========================================
        if (ledAntena != null)
        {
            progresoVisualLed = Mathf.Lerp(progresoVisualLed, progresoObjetivo, Time.deltaTime * 5f);
            ledAntena.colorNeon = Color.Lerp(colorInicio, colorFinal, progresoVisualLed);
            ledAntena.intensidad = Mathf.Lerp(intensidadInicio, intensidadFinal, progresoVisualLed);
        }

        if (textoPantallaAntena != null)
        {
            if (valorMostradoEnPantalla < monedasFaltantesReales)
            {
                valorMostradoEnPantalla = monedasFaltantesReales;
            }

            if (valorMostradoEnPantalla != monedasFaltantesReales)
            {
                valorMostradoEnPantalla = Mathf.MoveTowards(valorMostradoEnPantalla, monedasFaltantesReales, velocidadNumerosPantalla * Time.deltaTime);
            }

            if (maximo && valorMostradoEnPantalla <= 0.01f)
            {
                textoPantallaAntena.text = "MAX";
            }
            else
            {
                textoPantallaAntena.text = Mathf.CeilToInt(valorMostradoEnPantalla).ToString(formatoNumeros);
            }
        }

        // ==========================================
        // 2. FÍSICAS ADITIVAS DE MUELLE (SWAY)
        // ==========================================
        if (antenaVisual != null && antenaVisual.gameObject.activeSelf && !enTransicion)
        {
            Vector2 fuerzaResorte = -swayActualAntena * rigidezAntena;
            Vector2 fuerzaAmortiguacion = -velocidadSwayAntena * amortiguacionAntena;

            velocidadSwayAntena += (fuerzaResorte + fuerzaAmortiguacion) * Time.deltaTime;
            swayActualAntena += velocidadSwayAntena * Time.deltaTime;

            antenaVisual.localRotation = rotacionOriginalAntena * Quaternion.Euler(swayActualAntena.x, 0f, swayActualAntena.y);
        }
    }

    /// <summary>Se dispara desde la zona con Collider IsTrigger.</summary>
    private void OnTriggerEnter(Collider otro)
    {
        if (otro == null || maquinaErosion == null || maquinaErosion.ProcesadoAlMaximo() && maquinaErosion.ProcesadoraDesbloqueada)
            return;

        Rigidbody rigidbodyProyectil = otro.attachedRigidbody;
        GameObject raiz = rigidbodyProyectil != null
            ? rigidbodyProyectil.gameObject : otro.gameObject;

        // Sólo los proyectiles del arma; las monedas del suelo no cuentan.
        ProyectilArma proyectil = raiz.GetComponent<ProyectilArma>();
        if (proyectil == null)
            proyectil = otro.GetComponentInParent<ProyectilArma>();
        if (proyectil == null)
            return;

        Moneda moneda = raiz.GetComponent<Moneda>();
        if (moneda == null)
            moneda = raiz.GetComponentInChildren<Moneda>();
        if (moneda == null)
            moneda = otro.GetComponentInParent<Moneda>();
        if (moneda == null)
            return;

        // Un proyectil con varios colliders sólo puede sumar una vez.
        if (ultimoFrameImpacto != Time.frameCount)
        {
            proyectilesRecibidosFrame.Clear();
            ultimoFrameImpacto = Time.frameCount;
        }
        if (!proyectilesRecibidosFrame.Add(proyectil))
            return;

        int valor = Mathf.Max(1, moneda.valor);
        if (!AceptarPago(valor))
            return;

        if (!enTransicion)
        {
            // 1. Bloop de Antena
            if (corrutinaBloopAntena != null) StopCoroutine(corrutinaBloopAntena);
            corrutinaBloopAntena = StartCoroutine(HacerBloopAntena());

            // 2. Bloop de Estructura Independiente
            if (corrutinaBloopEstructura != null) StopCoroutine(corrutinaBloopEstructura);
            corrutinaBloopEstructura = StartCoroutine(HacerBloopEstructura());

            // 3. IMPACTO FÍSICO Sway
            AplicarGolpeFisicoAntena();
        }

        if (audioSource != null && sonidoMoneda != null)
            audioSource.PlayOneShot(sonidoMoneda);

        // El arma ya descontó el coste al disparar: aquí NO cobramos a Cartera.
        Destroy(proyectil.gameObject);
    }

    private void AplicarGolpeFisicoAntena()
    {
        if (antenaVisual == null || !antenaVisual.gameObject.activeSelf) return;

        Vector2 direccionImpacto = UnityEngine.Random.insideUnitCircle.normalized;
        float fuerzaReal = UnityEngine.Random.Range(fuerzaImpactoMoneda * 0.7f, fuerzaImpactoMoneda);

        velocidadSwayAntena += direccionImpacto * fuerzaReal;
    }

    /// <summary>Recibe una cantidad pagada y avanza sólo la fase actual.</summary>
    private bool AceptarPago(int cantidad)
    {
        if (cantidad <= 0 || maquinaErosion == null) return false;

        if (!maquinaErosion.ProcesadoraDesbloqueada)
        {
            monedasRecibidas = Mathf.Min(monedasNecesarias, monedasRecibidas + cantidad);
            if (monedasRecibidas >= monedasNecesarias)
                CompletarConstruccion();
        }
        else
        {
            if (maquinaErosion.ProcesadoAlMaximo()) return false;
            SincronizarNivelPagado();
            monedasMejoraRecibidas = Mathf.Min(PrecioMejoraActual,
                monedasMejoraRecibidas + cantidad);
            if (monedasMejoraRecibidas >= PrecioMejoraActual)
                CompletarMejora();
        }

        ActualizarTexto();
        RefrescarTienda();
        return true;
    }

    private void CompletarConstruccion()
    {
        maquinaErosion.DesbloquearProcesadora();
        SincronizarAspecto(true);

        if (particulasDesbloqueo != null)
        {
            foreach (ParticleSystem ps in particulasDesbloqueo)
                if (ps != null) ps.Play();
        }
        if (audioSource != null && sonidoDesbloqueo != null)
            audioSource.PlayOneShot(sonidoDesbloqueo);

        nivelDeMejoraEnPago = maquinaErosion.NivelProcesado + 1;
        monedasMejoraRecibidas = 0;
        Debug.Log("¡Desgastadora construida! Ahora acepta mejoras por disparos o tienda.", this);
    }

    private void CompletarMejora()
    {
        // Sólo avanzamos si realmente se pudo aplicar la mejora.
        if (!maquinaErosion.MejorarProcesado()) return;

        if (audioSource != null && sonidoMejora != null)
            audioSource.PlayOneShot(sonidoMejora);

        ApagarPiezasFuturas();
        StartCoroutine(AnimarMejora(maquinaErosion.NivelProcesado == 2 ? piezasNivel2 : piezasNivel3));

        nivelDeMejoraEnPago = maquinaErosion.NivelProcesado + 1;
        monedasMejoraRecibidas = 0;
        Debug.Log("Desgastadora mejorada al nivel " + maquinaErosion.NivelProcesado, this);
    }

    private void SincronizarNivelPagado()
    {
        if (maquinaErosion == null || !maquinaErosion.ProcesadoraDesbloqueada) return;
        int nivelEsperado = maquinaErosion.NivelProcesado + 1;
        if (nivelEsperado != nivelDeMejoraEnPago)
        {
            // Una mejora externa ha cambiado el nivel: evitamos trasladar pagos
            // hechos para el nivel anterior a otro nivel accidentalmente.
            nivelDeMejoraEnPago = nivelEsperado;
            monedasMejoraRecibidas = 0;
        }
        monedasMejoraRecibidas = Mathf.Clamp(
            monedasMejoraRecibidas, 0, PrecioMejoraActual);
    }

    /// <summary>
    /// Enlazar a OnClick del botón "Mejorar procesadora" de la tienda,
    /// sustituyendo el callback antiguo TiendaTrabajo.ComprarMejoraProcesadora.
    /// Sólo cobra las monedas QUE FALTAN tras los disparos.
    /// </summary>
    public void ComprarMejoraDesdeTienda()
    {
        if (maquinaErosion == null || cartera == null ||
            !maquinaErosion.ProcesadoraDesbloqueada ||
            maquinaErosion.ProcesadoAlMaximo())
            return;

        SincronizarNivelPagado();
        int restante = PrecioMejoraRestante;
        if (restante <= 0) return;

        if (!cartera.GastarMonedas(restante))
        {
            Debug.Log("Faltan monedas para completar la mejora: " + restante, this);
            return;
        }
        AceptarPago(restante);

        if (!enTransicion)
        {
            if (corrutinaBloopAntena != null) StopCoroutine(corrutinaBloopAntena);
            corrutinaBloopAntena = StartCoroutine(HacerBloopAntena());

            if (corrutinaBloopEstructura != null) StopCoroutine(corrutinaBloopEstructura);
            corrutinaBloopEstructura = StartCoroutine(HacerBloopEstructura());

            AplicarGolpeFisicoAntena();
        }
    }

    public void ActualizarTexto()
    {
        if (textoProgreso == null || maquinaErosion == null) return;

        if (!maquinaErosion.ProcesadoraDesbloqueada)
            textoProgreso.text = "CONSTRUIR DESGASTADORA\n" +
                                 monedasRecibidas + " / " + monedasNecesarias;
        else if (!maquinaErosion.ProcesadoAlMaximo())
            textoProgreso.text = "DESGASTADORA NIVEL " + maquinaErosion.NivelProcesado +
                " → " + (maquinaErosion.NivelProcesado + 1) + "\n" +
                monedasMejoraRecibidas + " / " + PrecioMejoraActual;
        else
            textoProgreso.text = "DESGASTADORA NIVEL MÁXIMO";
    }

    public void RefrescarTienda()
    {
        if (tiendaTrabajo != null) tiendaTrabajo.RefrescarTienda();
    }

    private void SincronizarAspecto(bool animado)
    {
        ApagarPiezasFuturas();

        bool desbloqueada = maquinaErosion != null && maquinaErosion.ProcesadoraDesbloqueada;

        if (!animado)
        {
            if (ruinasVisual != null)
            {
                ruinasVisual.SetActive(!desbloqueada);
                if (escalasOriginales.ContainsKey(ruinasVisual) && (partesRuinas == null || partesRuinas.Length == 0))
                    ruinasVisual.transform.localScale = !desbloqueada ? escalasOriginales[ruinasVisual] : Vector3.zero;
            }

            if (partesRuinas != null)
            {
                foreach (var p in partesRuinas)
                {
                    if (p)
                    {
                        p.SetActive(!desbloqueada);
                        if (escalasOriginales.ContainsKey(p)) p.transform.localScale = !desbloqueada ? escalasOriginales[p] : Vector3.zero;
                    }
                }
            }

            if (maquinaDesbloqueada != null)
            {
                maquinaDesbloqueada.SetActive(desbloqueada);
            }

            // Forzamos visibilidad correcta de piezas base
            if (piezasMaquinaBase != null)
            {
                foreach (var p in piezasMaquinaBase)
                {
                    if (p)
                    {
                        p.SetActive(desbloqueada);
                        if (escalasOriginales.ContainsKey(p)) p.transform.localScale = desbloqueada ? escalasOriginales[p] : Vector3.zero;
                    }
                }
            }

            if (antenaVisual)
            {
                antenaVisual.gameObject.SetActive(true);
                antenaVisual.localScale = escalaOriginalAntena;
                antenaVisual.localRotation = rotacionOriginalAntena;
                swayActualAntena = Vector2.zero;
                velocidadSwayAntena = Vector2.zero;
            }

            if (desbloqueada)
            {
                int nivelActual = maquinaErosion.NivelProcesado;
                if (piezasNivel2.piezasViejas != null) foreach (var p in piezasNivel2.piezasViejas) if (p) p.SetActive(nivelActual < 2);
                if (piezasNivel3.piezasViejas != null) foreach (var p in piezasNivel3.piezasViejas) if (p) p.SetActive(nivelActual < 3);

                if (nivelActual >= 2 && piezasNivel2.piezasNuevas != null) foreach (var p in piezasNivel2.piezasNuevas) if (p) { p.SetActive(true); p.transform.localScale = escalasOriginales[p]; }
                if (nivelActual >= 3 && piezasNivel3.piezasNuevas != null) foreach (var p in piezasNivel3.piezasNuevas) if (p) { p.SetActive(true); p.transform.localScale = escalasOriginales[p]; }
            }
        }
        else
        {
            StartCoroutine(AnimarTransicionConstruccion());
        }
    }

    // =========================================================================
    // CORRUTINAS DE ANIMACIÓN (GAME FEEL)
    // =========================================================================

    private IEnumerator HacerBloopAntena()
    {
        if (antenaVisual == null || !antenaVisual.gameObject.activeSelf) yield break;

        Vector3 escalaBaseAntena = escalaOriginalAntena;
        antenaVisual.localScale = escalaBaseAntena * multiplicadorBloopAntena;

        while (true)
        {
            if (enTransicion) yield break;

            if (Vector3.Distance(antenaVisual.localScale, escalaBaseAntena) > (escalaBaseAntena.magnitude * 0.02f + 0.01f))
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaBaseAntena, Time.deltaTime * velocidadBloopAntena);
            }
            else
            {
                antenaVisual.localScale = escalaBaseAntena;
                break;
            }
            yield return null;
        }
    }

    private IEnumerator HacerBloopEstructura()
    {
        List<GameObject> piezasActivas = new List<GameObject>();
        bool desbloqueada = maquinaErosion != null && maquinaErosion.ProcesadoraDesbloqueada;
        int nivelActual = maquinaErosion != null ? maquinaErosion.NivelProcesado : 1;

        if (!desbloqueada)
        {
            if (partesRuinas != null && partesRuinas.Length > 0)
            {
                foreach (var p in partesRuinas) if (p != null && p.activeSelf) piezasActivas.Add(p);
            }
            else if (ruinasVisual != null && ruinasVisual.activeSelf)
            {
                piezasActivas.Add(ruinasVisual);
            }
        }
        else
        {
            // Las piezas base en lugar del objeto padre entero
            if (piezasMaquinaBase != null)
            {
                foreach (var p in piezasMaquinaBase) if (p != null && p.activeSelf) piezasActivas.Add(p);
            }

            if (nivelActual >= 2 && piezasNivel2.piezasNuevas != null) foreach (var p in piezasNivel2.piezasNuevas) if (p != null && p.activeSelf) piezasActivas.Add(p);
            if (nivelActual >= 3 && piezasNivel3.piezasNuevas != null) foreach (var p in piezasNivel3.piezasNuevas) if (p != null && p.activeSelf) piezasActivas.Add(p);
        }

        foreach (var p in piezasActivas)
        {
            if (escalasOriginales.ContainsKey(p))
                p.transform.localScale = escalasOriginales[p] * multiplicadorBloopEstructura;
        }

        while (true)
        {
            if (enTransicion) yield break;

            bool todoTerminado = true;

            foreach (var p in piezasActivas)
            {
                if (p != null && p.activeSelf && escalasOriginales.ContainsKey(p))
                {
                    Vector3 escBase = escalasOriginales[p];
                    if (Vector3.Distance(p.transform.localScale, escBase) > (escBase.magnitude * 0.02f + 0.01f))
                    {
                        p.transform.localScale = Vector3.Lerp(p.transform.localScale, escBase, Time.deltaTime * velocidadBloopEstructura);
                        todoTerminado = false;
                    }
                    else
                    {
                        p.transform.localScale = escBase;
                    }
                }
            }

            if (todoTerminado) break;
            yield return null;
        }
    }

    private IEnumerator AnimarDesaparicionPiezaRuina(GameObject pieza, Vector3 escalaOriginal)
    {
        piezasEnAnimacion++;
        if (pieza != null)
        {
            float umbralCero = escalaOriginal.magnitude * 0.05f + 0.01f;
            while (pieza != null && pieza.transform.localScale.magnitude > umbralCero)
            {
                pieza.transform.localScale = Vector3.Lerp(pieza.transform.localScale, Vector3.zero, Time.deltaTime * velocidadDesaparicion);
                yield return null;
            }
            if (pieza != null) { pieza.transform.localScale = Vector3.zero; pieza.SetActive(false); }
        }
        piezasEnAnimacion--;
    }

    private IEnumerator AnimarPiezaIndividualArma(GameObject pieza, Vector3 escalaFinal)
    {
        piezasEnAnimacion++;
        if (pieza != null)
        {
            Vector3 escalaRebote = escalaFinal * (1f + intensidadReboteArma);

            float umbralRebote = escalaRebote.magnitude * 0.02f + 0.01f;
            while (pieza != null && Vector3.Distance(pieza.transform.localScale, escalaRebote) > umbralRebote)
            {
                pieza.transform.localScale = Vector3.Lerp(pieza.transform.localScale, escalaRebote, Time.deltaTime * velocidadAparicion);
                yield return null;
            }
            if (pieza != null) pieza.transform.localScale = escalaRebote;

            float umbralFinalPieza = escalaFinal.magnitude * 0.02f + 0.01f;
            while (pieza != null && Vector3.Distance(pieza.transform.localScale, escalaFinal) > umbralFinalPieza)
            {
                pieza.transform.localScale = Vector3.Lerp(pieza.transform.localScale, escalaFinal, Time.deltaTime * velocidadReboteArma);
                yield return null;
            }
            if (pieza != null) pieza.transform.localScale = escalaFinal;
        }
        piezasEnAnimacion--;
    }

    private IEnumerator AnimarTransicionConstruccion()
    {
        enTransicion = true;
        piezasEnAnimacion = 0;

        if (maquinaDesbloqueada != null) { maquinaDesbloqueada.SetActive(true); }

        if (piezasMaquinaBase != null)
        {
            for (int i = 0; i < piezasMaquinaBase.Length; i++)
            {
                if (piezasMaquinaBase[i] != null) { piezasMaquinaBase[i].SetActive(false); piezasMaquinaBase[i].transform.localScale = Vector3.zero; }
            }
        }

        // FASE 1: Antena hace bloop y se esconde
        if (antenaVisual != null && antenaVisual.gameObject.activeSelf)
        {
            swayActualAntena = Vector2.zero;
            velocidadSwayAntena = Vector2.zero;
            antenaVisual.localRotation = rotacionOriginalAntena;

            Vector3 escalaBloopAntena = escalaOriginalAntena * multiplicadorBloopAntena;
            float umbralBloopAntena = escalaBloopAntena.magnitude * 0.02f + 0.01f;

            while (Vector3.Distance(antenaVisual.localScale, escalaBloopAntena) > umbralBloopAntena)
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaBloopAntena, Time.deltaTime * velocidadBloopAntena * 2f);
                yield return null;
            }
            antenaVisual.localScale = escalaBloopAntena;

            float umbralAntena = escalaOriginalAntena.magnitude * 0.05f + 0.01f;
            while (antenaVisual.localScale.magnitude > umbralAntena)
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, Vector3.zero, Time.deltaTime * velocidadDesaparicion);
                yield return null;
            }
            antenaVisual.localScale = Vector3.zero;
        }

        // FASE 2: Ruinas desaparecen en cascada
        if (partesRuinas != null && partesRuinas.Length > 0)
        {
            for (int i = 0; i < partesRuinas.Length; i++)
            {
                GameObject pieza = partesRuinas[i];
                if (pieza == null || !pieza.activeSelf || !escalasOriginales.ContainsKey(pieza)) continue;

                StartCoroutine(AnimarDesaparicionPiezaRuina(pieza, escalasOriginales[pieza]));

                float tiempoEspera = Mathf.Lerp(0.01f, 0.2f, porcentajeCascada);
                yield return new WaitForSeconds(tiempoEspera);
            }
            while (piezasEnAnimacion > 0) yield return null;
        }
        else if (ruinasVisual != null && ruinasVisual.activeSelf && escalasOriginales.ContainsKey(ruinasVisual))
        {
            // Fallback si no hay lista de piezas separadas
            StartCoroutine(AnimarDesaparicionPiezaRuina(ruinasVisual, escalasOriginales[ruinasVisual]));
            float tiempoEspera = Mathf.Lerp(0.01f, 0.2f, porcentajeCascada);
            yield return new WaitForSeconds(tiempoEspera);
            while (piezasEnAnimacion > 0) yield return null;
        }

        // FASE 2.5: Antena renace
        if (antenaVisual != null)
        {
            progresoVisualLed = 0f;
            swayActualAntena = Vector2.zero;
            velocidadSwayAntena = Vector2.zero;
            antenaVisual.localRotation = rotacionOriginalAntena;

            Vector3 escalaBloopAntena = escalaOriginalAntena * multiplicadorBloopAntena;
            float umbralBloopAntena = escalaBloopAntena.magnitude * 0.02f + 0.01f;

            while (Vector3.Distance(antenaVisual.localScale, escalaBloopAntena) > umbralBloopAntena)
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaBloopAntena, Time.deltaTime * velocidadAparicion);
                yield return null;
            }
            antenaVisual.localScale = escalaBloopAntena;

            float umbralFinalAntena = escalaOriginalAntena.magnitude * 0.02f + 0.01f;
            while (Vector3.Distance(antenaVisual.localScale, escalaOriginalAntena) > umbralFinalAntena)
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaOriginalAntena, Time.deltaTime * velocidadBloopAntena);
                yield return null;
            }
            antenaVisual.localScale = escalaOriginalAntena;
        }

        // FASE 3: Maquina aparece (en cascada)
        if (piezasMaquinaBase != null && piezasMaquinaBase.Length > 0)
        {
            // EL BUCLE EMPIEZA AHORA DESDE CERO PARA LA PRIMERA PIEZA TAMBIÉN
            for (int i = 0; i < piezasMaquinaBase.Length; i++)
            {
                GameObject pieza = piezasMaquinaBase[i];
                if (pieza == null || !escalasOriginales.ContainsKey(pieza)) continue;

                pieza.SetActive(true);
                pieza.transform.localScale = Vector3.zero;

                Vector3 escalaFinal = escalasOriginales[pieza];
                StartCoroutine(AnimarPiezaIndividualArma(pieza, escalaFinal));

                float tiempoEspera = Mathf.Lerp(0.01f, 0.2f, porcentajeCascada);
                yield return new WaitForSeconds(tiempoEspera);
            }
            while (piezasEnAnimacion > 0) yield return null;
        }

        enTransicion = false;
    }

    private IEnumerator AnimarMejora(PiezasEvolucion nivelNuevo)
    {
        enTransicion = true;
        piezasEnAnimacion = 0;

        // FASE 1: Antena hace bloop y se esconde
        if (antenaVisual != null && antenaVisual.gameObject.activeSelf)
        {
            swayActualAntena = Vector2.zero;
            velocidadSwayAntena = Vector2.zero;
            antenaVisual.localRotation = rotacionOriginalAntena;

            Vector3 escalaBloopAntena = escalaOriginalAntena * multiplicadorBloopAntena;
            float umbralBloopAntena = escalaBloopAntena.magnitude * 0.02f + 0.01f;

            while (Vector3.Distance(antenaVisual.localScale, escalaBloopAntena) > umbralBloopAntena)
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaBloopAntena, Time.deltaTime * velocidadBloopAntena * 2f);
                yield return null;
            }
            antenaVisual.localScale = escalaBloopAntena;

            float umbralAntena = escalaOriginalAntena.magnitude * 0.05f + 0.01f;
            while (antenaVisual.localScale.magnitude > umbralAntena)
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, Vector3.zero, Time.deltaTime * velocidadDesaparicion);
                yield return null;
            }
            antenaVisual.localScale = Vector3.zero;
        }

        // FASE 2: Las "Piezas Viejas" desaparecen
        if (nivelNuevo.piezasViejas != null && nivelNuevo.piezasViejas.Length > 0)
        {
            for (int i = 0; i < nivelNuevo.piezasViejas.Length; i++)
            {
                GameObject pieza = nivelNuevo.piezasViejas[i];
                if (pieza == null || !pieza.activeSelf || !escalasOriginales.ContainsKey(pieza)) continue;

                StartCoroutine(AnimarDesaparicionPiezaRuina(pieza, escalasOriginales[pieza]));

                float tiempoEspera = Mathf.Lerp(0.01f, 0.2f, porcentajeCascada);
                yield return new WaitForSeconds(tiempoEspera);
            }
            while (piezasEnAnimacion > 0) yield return null;
        }

        // FASE 3: Antena renace
        if (antenaVisual != null)
        {
            progresoVisualLed = 0f;
            swayActualAntena = Vector2.zero;
            velocidadSwayAntena = Vector2.zero;
            antenaVisual.localRotation = rotacionOriginalAntena;

            Vector3 escalaBloopAntena = escalaOriginalAntena * multiplicadorBloopAntena;
            float umbralBloopAntena = escalaBloopAntena.magnitude * 0.02f + 0.01f;

            while (Vector3.Distance(antenaVisual.localScale, escalaBloopAntena) > umbralBloopAntena)
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaBloopAntena, Time.deltaTime * velocidadAparicion);
                yield return null;
            }
            antenaVisual.localScale = escalaBloopAntena;

            float umbralFinalAntena = escalaOriginalAntena.magnitude * 0.02f + 0.01f;
            while (Vector3.Distance(antenaVisual.localScale, escalaOriginalAntena) > umbralFinalAntena)
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaOriginalAntena, Time.deltaTime * velocidadBloopAntena);
                yield return null;
            }
            antenaVisual.localScale = escalaOriginalAntena;
        }

        // FASE 4: Las "Piezas Nuevas" brotan
        if (nivelNuevo.piezasNuevas != null && nivelNuevo.piezasNuevas.Length > 0)
        {
            for (int i = 0; i < nivelNuevo.piezasNuevas.Length; i++)
            {
                GameObject pieza = nivelNuevo.piezasNuevas[i];
                if (pieza == null || !escalasOriginales.ContainsKey(pieza)) continue;

                pieza.SetActive(true);
                pieza.transform.localScale = Vector3.zero;

                Vector3 escalaFinal = escalasOriginales[pieza];
                StartCoroutine(AnimarPiezaIndividualArma(pieza, escalaFinal));

                float tiempoEspera = Mathf.Lerp(0.01f, 0.2f, porcentajeCascada);
                yield return new WaitForSeconds(tiempoEspera);
            }
            while (piezasEnAnimacion > 0) yield return null;
        }

        enTransicion = false;
    }
}