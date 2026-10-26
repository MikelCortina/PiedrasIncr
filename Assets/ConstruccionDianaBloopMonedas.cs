using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Construye y mejora una DianaBloop disparando monedas a una zona de pago.
/// Compatible con el pago por Cartera desde un boton de tienda.
/// La moneda disparada ya se cobro al salir del arma: no se cobra otra vez.
/// Incluye todos los elementos visuales de mejora, LEDs, render textures y Sway Físico.
/// </summary>
public class ConstruccionDianaBloopMonedas : MonoBehaviour
{
    [Serializable]
    public class EstadisticasNivel
    {
        [Min(0.05f)] public float tiempoCooldown = 2.5f;
        [Range(1, 3)] public int piedrasPorActivacion = 1;

        [Header("Evolución Visual (Mejora)")]
        [Tooltip("Piezas que se encogerán y desaparecerán al alcanzar este nivel.")]
        public GameObject[] piezasViejas;
        [Tooltip("Piezas que brotarán y aparecerán al alcanzar este nivel.")]
        public GameObject[] piezasNuevas;
    }

    [Header("Referencias de la construcción base")]
    [Tooltip("Lista de piezas que forman las ruinas. Se encogerán en cascada al completarse la construcción.")]
    public GameObject[] partesRuinas;

    [Tooltip("Lista de piezas que forman la diana inicial (Nivel 1).")]
    public GameObject[] partesArmaDesbloqueada;

    public DianaBloop dianaBloop;

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

    [Header("Costes (monedas)")]
    [Min(1)] public int precioConstruccion = 20;
    [Min(1)] public int precioNivel2 = 10;
    [Min(1)] public int precioNivel3 = 20;

    [Header("Progreso")]
    [SerializeField] private bool construida = false;
    [SerializeField, Range(1, 3)] private int nivelActual = 1;
    [SerializeField, Min(0)] private int monedasAportadas = 0;

    [Header("Estadísticas por nivel")]
    public EstadisticasNivel nivel1 = new EstadisticasNivel
    {
        tiempoCooldown = 2.5f,
        piedrasPorActivacion = 1
    };
    public EstadisticasNivel nivel2 = new EstadisticasNivel
    {
        tiempoCooldown = 2.0f,
        piedrasPorActivacion = 2
    };
    public EstadisticasNivel nivel3 = new EstadisticasNivel
    {
        tiempoCooldown = 1.5f,
        piedrasPorActivacion = 3
    };

    [Header("Tienda y UI (opcionales)")]
    public Cartera cartera;
    public TiendaTrabajo tiendaTrabajo;
    [Tooltip("Texto de progreso fuera de Ruinas y de Diana, para que no se oculte.")]
    public TMP_Text textoProgreso;

    [Header("Efectos (opcionales)")]
    public ParticleSystem[] particulasConstruccion;
    public ParticleSystem[] particulasMejora;
    public AudioSource audioSource;
    public AudioClip sonidoMoneda;
    public AudioClip sonidoConstruccion;
    public AudioClip sonidoMejora;

    [Header("Game Feel: Bloop de Antena")]
    [Tooltip("Cuánto crece la antena al recibir una moneda (1.15 = 15% más grande).")]
    public float multiplicadorBloopAntena = 1.15f;
    [Tooltip("Velocidad a la que la antena recupera su tamaño original tras el bloop.")]
    public float velocidadBloopAntena = 15f;

    [Header("Game Feel: Bloop de Estructura")]
    [Tooltip("Cuánto crece el cuerpo al recibir una moneda. Usa valores bajos para simular peso (ej: 1.05).")]
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

    [Header("Rebote del Arma al Construir")]
    public float velocidadReboteArma = 12f;
    public float intensidadReboteArma = 0.15f;

    public event Action ProgresoActualizado;

    public bool EstaConstruida => construida;
    public int NivelActual => construida ? nivelActual : 0;
    public bool NivelMaximo => construida && nivelActual >= 3;
    public int MonedasAportadas => monedasAportadas;

    public int PrecioActual
    {
        get
        {
            if (!construida) return Mathf.Max(1, precioConstruccion);
            if (nivelActual == 1) return Mathf.Max(1, precioNivel2);
            if (nivelActual == 2) return Mathf.Max(1, precioNivel3);
            return 0;
        }
    }

    public int MonedasRestantes => NivelMaximo ? 0 : Mathf.Max(0, PrecioActual - monedasAportadas);

    private readonly HashSet<int> proyectilesDelFrame = new HashSet<int>();
    private int frameRegistro = -1;

    // Diccionario de escalas: Guarda el tamaño de CUALQUIER pieza automáticamente
    private Dictionary<GameObject, Vector3> escalasOriginales = new Dictionary<GameObject, Vector3>();

    private Coroutine corrutinaBloopAntena;
    private Coroutine corrutinaBloopEstructura;
    private bool enTransicion = false;
    private int piezasEnAnimacion = 0;

    private void OnValidate()
    {
        precioConstruccion = Mathf.Max(1, precioConstruccion);
        precioNivel2 = Mathf.Max(1, precioNivel2);
        precioNivel3 = Mathf.Max(1, precioNivel3);
        nivelActual = Mathf.Clamp(nivelActual, 1, 3);
        monedasAportadas = Mathf.Clamp(monedasAportadas, 0, PrecioActual);
        NormalizarNivel(nivel1);
        NormalizarNivel(nivel2);
        NormalizarNivel(nivel3);
    }

    private static void NormalizarNivel(EstadisticasNivel nivel)
    {
        if (nivel == null) return;
        nivel.tiempoCooldown = Mathf.Max(0.05f, nivel.tiempoCooldown);
        nivel.piedrasPorActivacion = Mathf.Clamp(nivel.piedrasPorActivacion, 1, 3);
    }

    private void Awake()
    {
        if (dianaBloop == null && partesArmaDesbloqueada != null)
        {
            foreach (var parte in partesArmaDesbloqueada)
            {
                if (parte != null)
                {
                    dianaBloop = parte.GetComponentInChildren<DianaBloop>(true);
                    if (dianaBloop != null) break;
                }
            }
        }
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
        if (partesRuinas == null || partesRuinas.Length == 0 || partesArmaDesbloqueada == null || partesArmaDesbloqueada.Length == 0)
        {
            Debug.LogError(name + ": asigna al menos una pieza en Partes Ruinas y una en Partes Arma Desbloqueada.", this);
            enabled = false;
            return;
        }

        Collider trigger = GetComponent<Collider>();
        if (trigger == null || !trigger.isTrigger)
            Debug.LogWarning(name + ": usa un BoxCollider con Is Trigger activado en ZonaPago.", this);

        RegistrarEscalas(partesRuinas);
        RegistrarEscalas(partesArmaDesbloqueada);
        RegistrarEscalas(nivel1.piezasViejas); RegistrarEscalas(nivel1.piezasNuevas);
        RegistrarEscalas(nivel2.piezasViejas); RegistrarEscalas(nivel2.piezasNuevas);
        RegistrarEscalas(nivel3.piezasViejas); RegistrarEscalas(nivel3.piezasNuevas);

        if (antenaVisual != null)
        {
            escalaOriginalAntena = antenaVisual.localScale;
            rotacionOriginalAntena = antenaVisual.localRotation;
        }

        if (construida) monedasAportadas = Mathf.Clamp(monedasAportadas, 0, PrecioActual);

        // Inicializamos el valor de la pantalla
        valorMostradoEnPantalla = NivelMaximo ? 0f : MonedasRestantes;

        SincronizarAspecto(false);
        AplicarEstadisticas();
        ActualizarPresentacion();
    }

    private void ApagarPiezasFuturas()
    {
        if (nivelActual < 2 || !construida)
        {
            if (nivel2.piezasNuevas != null)
                foreach (var p in nivel2.piezasNuevas)
                    if (p) p.SetActive(false);
        }

        if (nivelActual < 3 || !construida)
        {
            if (nivel3.piezasNuevas != null)
                foreach (var p in nivel3.piezasNuevas)
                    if (p) p.SetActive(false);
        }
    }

    private void Update()
    {
        // ==========================================
        // 1. ACTUALIZAR LED Y PANTALLA (RENDER TEXTURE)
        // ==========================================
        float progresoObjetivo = 0f;
        float monedasFaltantesReales = NivelMaximo ? 0f : MonedasRestantes;

        if (!NivelMaximo)
        {
            if (PrecioActual > 0) progresoObjetivo = (float)monedasAportadas / PrecioActual;
        }
        else progresoObjetivo = 1f;

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

            if (NivelMaximo && valorMostradoEnPantalla <= 0.01f)
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

    private void OnTriggerEnter(Collider otro)
    {
        if (otro == null || NivelMaximo) return;

        Rigidbody rb = otro.attachedRigidbody;
        ProyectilArma proyectil = rb != null ? rb.GetComponent<ProyectilArma>() : null;
        if (proyectil == null) proyectil = otro.GetComponentInParent<ProyectilArma>();
        if (proyectil == null) proyectil = otro.GetComponent<ProyectilArma>();
        if (proyectil == null) return;

        Moneda moneda = proyectil.GetComponent<Moneda>();
        if (moneda == null) moneda = proyectil.GetComponentInChildren<Moneda>();
        if (moneda == null) moneda = otro.GetComponentInParent<Moneda>();
        if (moneda == null) return;

        if (frameRegistro != Time.frameCount)
        {
            proyectilesDelFrame.Clear();
            frameRegistro = Time.frameCount;
        }

        if (!proyectilesDelFrame.Add(proyectil.GetInstanceID())) return;

        int valor = Mathf.Max(1, moneda.valor);
        AportarPago(valor);

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

        if (audioSource != null && sonidoMoneda != null) audioSource.PlayOneShot(sonidoMoneda);
        Destroy(proyectil.gameObject);
    }

    private void AplicarGolpeFisicoAntena()
    {
        if (antenaVisual == null || !antenaVisual.gameObject.activeSelf) return;

        Vector2 direccionImpacto = UnityEngine.Random.insideUnitCircle.normalized;
        float fuerzaReal = UnityEngine.Random.Range(fuerzaImpactoMoneda * 0.7f, fuerzaImpactoMoneda);

        velocidadSwayAntena += direccionImpacto * fuerzaReal;
    }

    private void AportarPago(int cantidad)
    {
        int pendienteDeAplicar = cantidad;
        while (pendienteDeAplicar > 0 && !NivelMaximo)
        {
            int faltan = MonedasRestantes;
            if (faltan == 0)
            {
                CompletarEtapa();
                continue;
            }

            int aporte = Mathf.Min(pendienteDeAplicar, faltan);
            monedasAportadas += aporte;
            pendienteDeAplicar -= aporte;

            if (monedasAportadas >= PrecioActual) CompletarEtapa();
        }

        ActualizarPresentacion();
    }

    private void CompletarEtapa()
    {
        monedasAportadas = 0;
        if (!construida)
        {
            construida = true;
            nivelActual = 1;
            SincronizarAspecto(true);
            AplicarEstadisticas();
            ReproducirParticulas(particulasConstruccion);
            ReproducirSonido(sonidoConstruccion);
            Debug.Log("DianaBloop construida: nivel 1", this);
        }
        else if (nivelActual < 3)
        {
            nivelActual++;
            AplicarEstadisticas();
            ReproducirParticulas(particulasMejora);
            ReproducirSonido(sonidoMejora);

            ApagarPiezasFuturas();
            StartCoroutine(AnimarMejora(nivelActual == 2 ? nivel2 : nivel3));
            Debug.Log("DianaBloop mejorada al nivel " + nivelActual, this);
        }
    }

    private void SincronizarAspecto(bool animado)
    {
        ApagarPiezasFuturas();

        if (!animado)
        {
            foreach (var p in partesRuinas) { if (p) { p.SetActive(!construida); p.transform.localScale = construida ? Vector3.zero : escalasOriginales[p]; } }
            if (antenaVisual)
            {
                antenaVisual.gameObject.SetActive(true);
                antenaVisual.localScale = escalaOriginalAntena;
                antenaVisual.localRotation = rotacionOriginalAntena;
                swayActualAntena = Vector2.zero;
                velocidadSwayAntena = Vector2.zero;
            }

            // Si está construida, activamos todas las partes base del arma
            foreach (var p in partesArmaDesbloqueada)
            {
                if (p)
                {
                    p.SetActive(construida);
                    p.transform.localScale = construida ? escalasOriginales[p] : Vector3.zero;
                    if (construida) ActivarHijos(p);
                }
            }

            if (construida)
            {
                if (nivel2.piezasViejas != null) foreach (var p in nivel2.piezasViejas) if (p) p.SetActive(nivelActual < 2);
                if (nivel3.piezasViejas != null) foreach (var p in nivel3.piezasViejas) if (p) p.SetActive(nivelActual < 3);

                if (nivelActual >= 2 && nivel2.piezasNuevas != null) foreach (var p in nivel2.piezasNuevas) if (p) { p.SetActive(true); p.transform.localScale = escalasOriginales[p]; ActivarHijos(p); }
                if (nivelActual >= 3 && nivel3.piezasNuevas != null) foreach (var p in nivel3.piezasNuevas) if (p) { p.SetActive(true); p.transform.localScale = escalasOriginales[p]; ActivarHijos(p); }
            }
        }
        else
        {
            StartCoroutine(AnimarTransicionConstruccion());
        }
    }

    private void ActivarHijos(GameObject obj)
    {
        if (obj == null) return;
        foreach (Transform hijo in obj.GetComponentsInChildren<Transform>(true))
        {
            hijo.gameObject.SetActive(true);
        }
    }

    private void AplicarEstadisticas()
    {
        if (!construida || dianaBloop == null) return;
        EstadisticasNivel stats = nivelActual == 1 ? nivel1 : nivelActual == 2 ? nivel2 : nivel3;
        if (stats == null) return;

        dianaBloop.tiempoCooldown = Mathf.Max(0.05f, stats.tiempoCooldown);
        dianaBloop.piedrasPorActivacion = Mathf.Clamp(stats.piedrasPorActivacion, 1, 3);
    }

    public void ComprarConstruccionDesdeTienda() { if (!construida) PagarRestanteDesdeTienda(); }
    public void ComprarMejoraDesdeTienda() { if (construida && !NivelMaximo) PagarRestanteDesdeTienda(); }

    private void PagarRestanteDesdeTienda()
    {
        if (cartera == null) return;
        int faltan = MonedasRestantes;
        if (faltan <= 0 || !cartera.GastarMonedas(faltan)) return;

        AportarPago(faltan);

        if (!enTransicion)
        {
            if (corrutinaBloopAntena != null) StopCoroutine(corrutinaBloopAntena);
            corrutinaBloopAntena = StartCoroutine(HacerBloopAntena());

            if (corrutinaBloopEstructura != null) StopCoroutine(corrutinaBloopEstructura);
            corrutinaBloopEstructura = StartCoroutine(HacerBloopEstructura());

            AplicarGolpeFisicoAntena();
        }
    }

    private void ActualizarPresentacion()
    {
        if (textoProgreso != null)
        {
            if (!construida) textoProgreso.text = "CONSTRUIR DIANA BLOOP\n" + monedasAportadas + " / " + PrecioActual + " MONEDAS";
            else if (NivelMaximo) textoProgreso.text = "DIANA BLOOP\nNIVEL MÁXIMO (3)";
            else textoProgreso.text = "DIANA BLOOP NIVEL " + nivelActual + "\nMEJORA: " + monedasAportadas + " / " + PrecioActual + " MONEDAS";
        }
        ProgresoActualizado?.Invoke();
        if (tiendaTrabajo != null) tiendaTrabajo.RefrescarTienda();
    }

    private void ReproducirParticulas(ParticleSystem[] sistemas)
    {
        if (sistemas == null) return;
        foreach (ParticleSystem ps in sistemas) if (ps != null) ps.Play();
    }

    private void ReproducirSonido(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
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
        if (!construida)
        {
            foreach (var p in partesRuinas) if (p != null && p.activeSelf) piezasActivas.Add(p);
        }
        else
        {
            foreach (var p in partesArmaDesbloqueada) if (p != null && p.activeSelf) piezasActivas.Add(p);
            if (nivelActual >= 2 && nivel2.piezasNuevas != null) foreach (var p in nivel2.piezasNuevas) if (p != null && p.activeSelf) piezasActivas.Add(p);
            if (nivelActual >= 3 && nivel3.piezasNuevas != null) foreach (var p in nivel3.piezasNuevas) if (p != null && p.activeSelf) piezasActivas.Add(p);
        }

        // Aplicamos el hinchado inicial
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
            // Añado un contador de seguridad para forzar la salida si se atasca
            float tiempoSeguridad = 0f;
            while (pieza != null && pieza.transform.localScale.magnitude > umbralCero && tiempoSeguridad < 2f)
            {
                tiempoSeguridad += Time.deltaTime;
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

            float tiempoSeguridad = 0f;
            float umbralRebote = escalaRebote.magnitude * 0.02f + 0.01f;
            while (pieza != null && Vector3.Distance(pieza.transform.localScale, escalaRebote) > umbralRebote && tiempoSeguridad < 2f)
            {
                tiempoSeguridad += Time.deltaTime;
                pieza.transform.localScale = Vector3.Lerp(pieza.transform.localScale, escalaRebote, Time.deltaTime * velocidadAparicion);
                yield return null;
            }
            if (pieza != null) pieza.transform.localScale = escalaRebote;

            tiempoSeguridad = 0f;
            float umbralFinalPieza = escalaFinal.magnitude * 0.02f + 0.01f;
            while (pieza != null && Vector3.Distance(pieza.transform.localScale, escalaFinal) > umbralFinalPieza && tiempoSeguridad < 2f)
            {
                tiempoSeguridad += Time.deltaTime;
                pieza.transform.localScale = Vector3.Lerp(pieza.transform.localScale, escalaFinal, Time.deltaTime * velocidadReboteArma);
                yield return null;
            }
            if (pieza != null)
            {
                pieza.transform.localScale = escalaFinal;
                ActivarHijos(pieza);
            }
        }
        piezasEnAnimacion--;
    }

    private IEnumerator AnimarTransicionConstruccion()
    {
        enTransicion = true;
        piezasEnAnimacion = 0;

        for (int i = 0; i < partesArmaDesbloqueada.Length; i++)
        {
            if (partesArmaDesbloqueada[i] != null) { partesArmaDesbloqueada[i].SetActive(false); partesArmaDesbloqueada[i].transform.localScale = Vector3.zero; }
        }

        // FASE 1: Antena hace bloop y se esconde
        if (antenaVisual != null && antenaVisual.gameObject.activeSelf)
        {
            swayActualAntena = Vector2.zero;
            velocidadSwayAntena = Vector2.zero;
            antenaVisual.localRotation = rotacionOriginalAntena;

            Vector3 escalaBloopAntena = escalaOriginalAntena * multiplicadorBloopAntena;
            float umbralBloopAntena = escalaBloopAntena.magnitude * 0.02f + 0.01f;
            float tiempoSeguridad = 0f;

            while (Vector3.Distance(antenaVisual.localScale, escalaBloopAntena) > umbralBloopAntena && tiempoSeguridad < 2f)
            {
                tiempoSeguridad += Time.deltaTime;
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaBloopAntena, Time.deltaTime * velocidadBloopAntena * 2f);
                yield return null;
            }
            antenaVisual.localScale = escalaBloopAntena;

            tiempoSeguridad = 0f;
            float umbralAntena = escalaOriginalAntena.magnitude * 0.05f + 0.01f;
            while (antenaVisual.localScale.magnitude > umbralAntena && tiempoSeguridad < 2f)
            {
                tiempoSeguridad += Time.deltaTime;
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

            float timeoutTotal = 0f;
            while (piezasEnAnimacion > 0 && timeoutTotal < 3f)
            {
                timeoutTotal += Time.deltaTime;
                yield return null;
            }
            // Forzamos el reset por si alguna corrutina falló
            piezasEnAnimacion = 0;
        }

        // FASE 2.5: Antena renace y resetea su LED
        if (antenaVisual != null)
        {
            progresoVisualLed = 0f;
            swayActualAntena = Vector2.zero;
            velocidadSwayAntena = Vector2.zero;
            antenaVisual.localRotation = rotacionOriginalAntena;

            Vector3 escalaBloopAntena = escalaOriginalAntena * multiplicadorBloopAntena;
            float umbralBloopAntena = escalaBloopAntena.magnitude * 0.02f + 0.01f;
            float tiempoSeguridad = 0f;

            while (Vector3.Distance(antenaVisual.localScale, escalaBloopAntena) > umbralBloopAntena && tiempoSeguridad < 2f)
            {
                tiempoSeguridad += Time.deltaTime;
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaBloopAntena, Time.deltaTime * velocidadAparicion);
                yield return null;
            }
            antenaVisual.localScale = escalaBloopAntena;

            tiempoSeguridad = 0f;
            float floatUmbralFinal = escalaOriginalAntena.magnitude * 0.02f + 0.01f;
            while (Vector3.Distance(antenaVisual.localScale, escalaOriginalAntena) > floatUmbralFinal && tiempoSeguridad < 2f)
            {
                tiempoSeguridad += Time.deltaTime;
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaOriginalAntena, Time.deltaTime * velocidadBloopAntena);
                yield return null;
            }
            antenaVisual.localScale = escalaOriginalAntena;
        }

        // FASE 3: Arma aparece en cascada
        if (partesArmaDesbloqueada != null && partesArmaDesbloqueada.Length > 0)
        {
            if (partesArmaDesbloqueada[0] != null && escalasOriginales.ContainsKey(partesArmaDesbloqueada[0]))
            {
                partesArmaDesbloqueada[0].SetActive(true);
                partesArmaDesbloqueada[0].transform.localScale = escalasOriginales[partesArmaDesbloqueada[0]];
                ActivarHijos(partesArmaDesbloqueada[0]);
            }

            for (int i = 1; i < partesArmaDesbloqueada.Length; i++)
            {
                GameObject pieza = partesArmaDesbloqueada[i];
                if (pieza == null || !escalasOriginales.ContainsKey(pieza)) continue;

                pieza.SetActive(true);
                pieza.transform.localScale = Vector3.zero;

                Vector3 escalaFinal = escalasOriginales[pieza];
                StartCoroutine(AnimarPiezaIndividualArma(pieza, escalaFinal));

                float tiempoEspera = Mathf.Lerp(0.01f, 0.2f, porcentajeCascada);
                yield return new WaitForSeconds(tiempoEspera);
            }

            float timeoutTotal = 0f;
            while (piezasEnAnimacion > 0 && timeoutTotal < 3f)
            {
                timeoutTotal += Time.deltaTime;
                yield return null;
            }
            piezasEnAnimacion = 0;
        }

        enTransicion = false;

        // Cierre de seguridad: si algo falló, forzamos que el aspecto sea el correcto.
        SincronizarAspecto(false);
    }

    private IEnumerator AnimarMejora(EstadisticasNivel nivelNuevo)
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
            float tiempoSeguridad = 0f;

            while (Vector3.Distance(antenaVisual.localScale, escalaBloopAntena) > umbralBloopAntena && tiempoSeguridad < 2f)
            {
                tiempoSeguridad += Time.deltaTime;
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaBloopAntena, Time.deltaTime * velocidadBloopAntena * 2f);
                yield return null;
            }
            antenaVisual.localScale = escalaBloopAntena;

            tiempoSeguridad = 0f;
            float umbralAntena = escalaOriginalAntena.magnitude * 0.05f + 0.01f;
            while (antenaVisual.localScale.magnitude > umbralAntena && tiempoSeguridad < 2f)
            {
                tiempoSeguridad += Time.deltaTime;
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

            float timeoutTotal = 0f;
            while (piezasEnAnimacion > 0 && timeoutTotal < 3f)
            {
                timeoutTotal += Time.deltaTime;
                yield return null;
            }
            piezasEnAnimacion = 0;
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
            float tiempoSeguridad = 0f;

            while (Vector3.Distance(antenaVisual.localScale, escalaBloopAntena) > umbralBloopAntena && tiempoSeguridad < 2f)
            {
                tiempoSeguridad += Time.deltaTime;
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaBloopAntena, Time.deltaTime * velocidadAparicion);
                yield return null;
            }
            antenaVisual.localScale = escalaBloopAntena;

            tiempoSeguridad = 0f;
            float floatUmbralFinal = escalaOriginalAntena.magnitude * 0.02f + 0.01f;
            while (Vector3.Distance(antenaVisual.localScale, escalaOriginalAntena) > floatUmbralFinal && tiempoSeguridad < 2f)
            {
                tiempoSeguridad += Time.deltaTime;
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

            float timeoutTotal = 0f;
            while (piezasEnAnimacion > 0 && timeoutTotal < 3f)
            {
                timeoutTotal += Time.deltaTime;
                yield return null;
            }
            piezasEnAnimacion = 0;
        }

        enTransicion = false;

        // Cierre de seguridad.
        SincronizarAspecto(false);
    }
}
