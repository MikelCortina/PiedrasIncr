using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Zona de pago SIEMPRE ACTIVA para construir y mejorar la torreta de asteroides.
/// Las monedas disparadas (ProyectilArma + Moneda) y la tienda comparten el
/// mismo progreso. No modifica el disparo ni el apuntado de la torreta.
/// </summary>
public class ConstruccionAmetralladoraMonedas : MonoBehaviour
{
    [Serializable]
    public class EstadisticasNivel
    {
        [Min(0.01f)] public float cadenciaDisparo = 1f;
        [Min(1f)] public float alcance = 300f;
        [Min(0.01f)] public float velocidadGiro = 10f;
        [Min(1f)] public float velocidadMaximaProyectil = 150f;

        [Header("Evolución Visual (Mejora)")]
        [Tooltip("Piezas que se encogerán y desaparecerán al alcanzar este nivel.")]
        public GameObject[] piezasViejas;
        [Tooltip("Piezas que brotarán y aparecerán al alcanzar este nivel.")]
        public GameObject[] piezasNuevas;
    }

    [Header("Referencias de la construcción base")]
    [Tooltip("Lista de piezas que forman las ruinas. Se encogerán en cascada al completarse la construcción.")]
    public GameObject[] partesRuinas;

    [Tooltip("Lista de piezas que forman el arma inicial (Nivel 1).")]
    public GameObject[] partesArmaDesbloqueada;

    public AmetralladoraAsteroides ametralladora;

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

    [Header("Costes en monedas")]
    [Min(1)] public int precioConstruccion = 20;
    [Min(1)] public int precioNivel2 = 10;
    [Min(1)] public int precioNivel3 = 20;

    [Header("Progreso")]
    [SerializeField] private bool construida = false;
    [SerializeField, Range(1, 3)] private int nivelActual = 1;
    [Tooltip("Monedas acumuladas para la etapa actual, ya sea con disparos o desde la tienda.")]
    [SerializeField, Min(0)] private int monedasAportadas = 0;

    [Header("Estadísticas por nivel")]
    public EstadisticasNivel nivel1 = new EstadisticasNivel
    {
        cadenciaDisparo = 1f,
        alcance = 300f,
        velocidadGiro = 10f,
        velocidadMaximaProyectil = 150f
    };
    public EstadisticasNivel nivel2 = new EstadisticasNivel
    {
        cadenciaDisparo = 0.75f,
        alcance = 350f,
        velocidadGiro = 12f,
        velocidadMaximaProyectil = 185f
    };
    public EstadisticasNivel nivel3 = new EstadisticasNivel
    {
        cadenciaDisparo = 0.5f,
        alcance = 400f,
        velocidadGiro = 15f,
        velocidadMaximaProyectil = 220f
    };

    [Header("Tienda y UI (opcionales)")]
    public Cartera cartera;
    public TiendaTrabajo tiendaTrabajo;
    [Tooltip("TMP_Text activo incluso mientras la torreta está oculta.")]
    public TMP_Text textoProgreso;

    [Header("Efectos (opcionales)")]
    public ParticleSystem[] particulasConstruccion;
    public ParticleSystem[] particulasMejora;
    public AudioSource audioSource;
    public AudioClip sonidoMoneda;
    public AudioClip sonidoConstruccion;
    public AudioClip sonidoMejora;

    [Header("Game Feel (Animaciones)")]
    [Tooltip("Cuánto crecen los objetos al recibir una moneda o al construirse (1.15 = 15% más grandes).")]
    public float multiplicadorBloop = 1.15f;
    [Tooltip("Velocidad a la que los objetos recuperan su tamaño original tras el bloop.")]
    public float velocidadBloop = 15f;
    [Tooltip("Tiempo en segundos que las piezas se mantienen infladas al máximo antes de encogerse.")]
    public float tiempoEsperaBloopRuinas = 0.2f;

    [Space(10)]
    [Tooltip("Velocidad a la que la antena se encoge hasta desaparecer al completarse.")]
    public float velocidadOcultarAntena = 15f;
    [Tooltip("Velocidad a la que las piezas de las ruinas se encogen hasta desaparecer.")]
    public float velocidadOcultarRuinas = 10f;
    [Tooltip("Velocidad a la que las piezas del arma brotan hacia arriba (antes de rebotar).")]
    public float velocidadMostrarArma = 15f;

    [Space(10)]
    [Tooltip("Tiempo (0.1 a 1) para solapar la desaparición de las ruinas. 0.1 = Ola rapidísima. 1.0 = Escalonado.")]
    [Range(0.1f, 1f)] public float porcentajeCascadaRuinas = 0.5f;

    [Tooltip("Tiempo (0.1 a 1) para solapar la construcción de piezas y mejoras. 0.1 = Ráfaga inmediata. 1.0 = Escalonado.")]
    [Range(0.1f, 1f)] public float porcentajeCascadaArma = 0.5f;

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

    private Coroutine corrutinaBloop;
    private bool enTransicion = false;
    private int piezasEnAnimacion = 0;

    private void OnValidate()
    {
        precioConstruccion = Mathf.Max(1, precioConstruccion);
        precioNivel2 = Mathf.Max(1, precioNivel2);
        precioNivel3 = Mathf.Max(1, precioNivel3);
        nivelActual = Mathf.Clamp(nivelActual, 1, 3);
        monedasAportadas = Mathf.Clamp(monedasAportadas, 0, PrecioActual);
    }

    private void Awake()
    {
        if (ametralladora == null && partesArmaDesbloqueada != null)
        {
            foreach (var parte in partesArmaDesbloqueada)
            {
                if (parte != null)
                {
                    ametralladora = parte.GetComponentInChildren<AmetralladoraAsteroides>(true);
                    if (ametralladora != null) break;
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

        // Registramos todos los tamaños iniciales de cualquier pieza que pueda aparecer en el juego
        RegistrarEscalas(partesRuinas);
        RegistrarEscalas(partesArmaDesbloqueada);
        RegistrarEscalas(nivel1.piezasViejas); RegistrarEscalas(nivel1.piezasNuevas);
        RegistrarEscalas(nivel2.piezasViejas); RegistrarEscalas(nivel2.piezasNuevas);
        RegistrarEscalas(nivel3.piezasViejas); RegistrarEscalas(nivel3.piezasNuevas);

        if (antenaVisual != null) escalaOriginalAntena = antenaVisual.localScale;

        if (construida) monedasAportadas = Mathf.Clamp(monedasAportadas, 0, PrecioActual);

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
        if (ledAntena != null)
        {
            float progresoObjetivo = 0f;
            if (!NivelMaximo)
            {
                if (PrecioActual > 0) progresoObjetivo = (float)monedasAportadas / PrecioActual;
            }
            else progresoObjetivo = 1f;

            // Suavizamos el progreso
            progresoVisualLed = Mathf.Lerp(progresoVisualLed, progresoObjetivo, Time.deltaTime * 5f);

            // Interpolamos estrictamente entre nuestro COLOR DE INICIO y FINAL, e INTENSIDAD DE INICIO y FINAL
            ledAntena.colorNeon = Color.Lerp(colorInicio, colorFinal, progresoVisualLed);
            ledAntena.intensidad = Mathf.Lerp(intensidadInicio, intensidadFinal, progresoVisualLed);
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
            if (corrutinaBloop != null) StopCoroutine(corrutinaBloop);
            corrutinaBloop = StartCoroutine(HacerBloop());
        }

        if (audioSource != null && sonidoMoneda != null) audioSource.PlayOneShot(sonidoMoneda);
        Destroy(proyectil.gameObject);
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
        }
        else if (nivelActual < 3)
        {
            nivelActual++;
            AplicarEstadisticas();
            ReproducirParticulas(particulasMejora);
            ReproducirSonido(sonidoMejora);

            ApagarPiezasFuturas();
            StartCoroutine(AnimarMejora(nivelActual == 2 ? nivel2 : nivel3));
        }
    }

    private void SincronizarAspecto(bool animado)
    {
        ApagarPiezasFuturas();

        if (!animado)
        {
            foreach (var p in partesRuinas) { if (p) { p.SetActive(!construida); p.transform.localScale = construida ? Vector3.zero : escalasOriginales[p]; } }
            if (antenaVisual) { antenaVisual.gameObject.SetActive(true); antenaVisual.localScale = escalaOriginalAntena; }
            foreach (var p in partesArmaDesbloqueada) { if (p) { p.SetActive(construida); p.transform.localScale = construida ? escalasOriginales[p] : Vector3.zero; } }

            // Aplicar visibilidad de las piezas extra si cargamos la partida
            if (construida)
            {
                if (nivel2.piezasViejas != null) foreach (var p in nivel2.piezasViejas) if (p) p.SetActive(nivelActual < 2);
                if (nivel3.piezasViejas != null) foreach (var p in nivel3.piezasViejas) if (p) p.SetActive(nivelActual < 3);

                if (nivelActual >= 2 && nivel2.piezasNuevas != null) foreach (var p in nivel2.piezasNuevas) if (p) { p.SetActive(true); p.transform.localScale = escalasOriginales[p]; }
                if (nivelActual >= 3 && nivel3.piezasNuevas != null) foreach (var p in nivel3.piezasNuevas) if (p) { p.SetActive(true); p.transform.localScale = escalasOriginales[p]; }
            }
        }
        else
        {
            StartCoroutine(AnimarTransicionConstruccion());
        }
    }

    private void AplicarEstadisticas()
    {
        if (!construida || ametralladora == null) return;
        EstadisticasNivel stats = nivelActual == 1 ? nivel1 : nivelActual == 2 ? nivel2 : nivel3;
        if (stats == null) return;

        ametralladora.cadenciaDisparo = Mathf.Max(0.01f, stats.cadenciaDisparo);
        ametralladora.alcance = Mathf.Max(1f, stats.alcance);
        ametralladora.velocidadGiro = Mathf.Max(0.01f, stats.velocidadGiro);
        ametralladora.velocidadMaximaProyectil = Mathf.Max(1f, stats.velocidadMaximaProyectil);
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
            if (corrutinaBloop != null) StopCoroutine(corrutinaBloop);
            corrutinaBloop = StartCoroutine(HacerBloop());
        }
    }

    private void ActualizarPresentacion()
    {
        if (textoProgreso != null)
        {
            if (!construida) textoProgreso.text = "CONSTRUIR AMETRALLADORA\n" + monedasAportadas + " / " + PrecioActual + " MONEDAS";
            else if (NivelMaximo) textoProgreso.text = "AMETRALLADORA\nNIVEL MÁXIMO (3)";
            else textoProgreso.text = "AMETRALLADORA NIVEL " + nivelActual + "\nMEJORA: " + monedasAportadas + " / " + PrecioActual + " MONEDAS";
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

    private IEnumerator HacerBloop()
    {
        bool hayAntena = antenaVisual != null && antenaVisual.gameObject.activeSelf;
        Vector3 escalaBaseAntena = hayAntena ? escalaOriginalAntena : Vector3.zero;
        if (hayAntena) antenaVisual.localScale = escalaBaseAntena * multiplicadorBloop;

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

        foreach (var p in piezasActivas)
        {
            if (escalasOriginales.ContainsKey(p)) p.transform.localScale = escalasOriginales[p] * multiplicadorBloop;
        }

        while (true)
        {
            if (enTransicion) yield break;

            bool todoTerminado = true;

            if (hayAntena)
            {
                if (Vector3.Distance(antenaVisual.localScale, escalaBaseAntena) > (escalaBaseAntena.magnitude * 0.02f + 0.01f))
                {
                    antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaBaseAntena, Time.deltaTime * velocidadBloop);
                    todoTerminado = false;
                }
                else antenaVisual.localScale = escalaBaseAntena;
            }

            foreach (var p in piezasActivas)
            {
                if (p != null && p.activeSelf && escalasOriginales.ContainsKey(p))
                {
                    Vector3 escBase = escalasOriginales[p];
                    if (Vector3.Distance(p.transform.localScale, escBase) > (escBase.magnitude * 0.02f + 0.01f))
                    {
                        p.transform.localScale = Vector3.Lerp(p.transform.localScale, escBase, Time.deltaTime * velocidadBloop);
                        todoTerminado = false;
                    }
                    else p.transform.localScale = escBase;
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
            Vector3 escalaBloopObjetivo = escalaOriginal * multiplicadorBloop;
            float umbralBloop = escalaBloopObjetivo.magnitude * 0.02f + 0.01f;

            while (pieza != null && Vector3.Distance(pieza.transform.localScale, escalaBloopObjetivo) > umbralBloop)
            {
                pieza.transform.localScale = Vector3.Lerp(pieza.transform.localScale, escalaBloopObjetivo, Time.deltaTime * velocidadBloop * 2f);
                yield return null;
            }
            if (pieza != null) pieza.transform.localScale = escalaBloopObjetivo;

            if (tiempoEsperaBloopRuinas > 0f) yield return new WaitForSeconds(tiempoEsperaBloopRuinas);

            float umbralCero = escalaOriginal.magnitude * 0.05f + 0.01f;
            while (pieza != null && pieza.transform.localScale.magnitude > umbralCero)
            {
                pieza.transform.localScale = Vector3.Lerp(pieza.transform.localScale, Vector3.zero, Time.deltaTime * velocidadOcultarRuinas);
                yield return null;
            }
            if (pieza != null) { pieza.transform.localScale = Vector3.zero; pieza.SetActive(false); }
        }
        piezasEnAnimacion--;
    }

    private IEnumerator AnimarPiezaIndividualArma(GameObject pieza, Vector3 escalaFinal, Vector3 escalaBloop)
    {
        piezasEnAnimacion++;
        if (pieza != null)
        {
            float umbralBloopPieza = escalaBloop.magnitude * 0.02f + 0.01f;
            while (pieza != null && Vector3.Distance(pieza.transform.localScale, escalaBloop) > umbralBloopPieza)
            {
                pieza.transform.localScale = Vector3.Lerp(pieza.transform.localScale, escalaBloop, Time.deltaTime * velocidadMostrarArma);
                yield return null;
            }
            if (pieza != null) pieza.transform.localScale = escalaBloop;

            float umbralFinalPieza = escalaFinal.magnitude * 0.02f + 0.01f;
            while (pieza != null && Vector3.Distance(pieza.transform.localScale, escalaFinal) > umbralFinalPieza)
            {
                pieza.transform.localScale = Vector3.Lerp(pieza.transform.localScale, escalaFinal, Time.deltaTime * velocidadBloop);
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

        for (int i = 0; i < partesArmaDesbloqueada.Length; i++)
        {
            if (partesArmaDesbloqueada[i] != null) { partesArmaDesbloqueada[i].SetActive(false); partesArmaDesbloqueada[i].transform.localScale = Vector3.zero; }
        }

        // FASE 1: Antena se esconde
        if (antenaVisual != null && antenaVisual.gameObject.activeSelf)
        {
            Vector3 escalaBloopAntena = escalaOriginalAntena * multiplicadorBloop;
            float umbralBloopAntena = escalaBloopAntena.magnitude * 0.02f + 0.01f;

            while (Vector3.Distance(antenaVisual.localScale, escalaBloopAntena) > umbralBloopAntena)
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaBloopAntena, Time.deltaTime * velocidadBloop * 2f);
                yield return null;
            }
            antenaVisual.localScale = escalaBloopAntena;

            if (tiempoEsperaBloopRuinas > 0f) yield return new WaitForSeconds(tiempoEsperaBloopRuinas);

            float umbralAntena = escalaOriginalAntena.magnitude * 0.05f + 0.01f;
            while (antenaVisual.localScale.magnitude > umbralAntena)
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, Vector3.zero, Time.deltaTime * velocidadOcultarAntena);
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

                float tiempoTotalPieza = 0.15f + tiempoEsperaBloopRuinas + 0.25f;
                float tiempoEspera = Mathf.Lerp(0.01f, tiempoTotalPieza, porcentajeCascadaRuinas);
                yield return new WaitForSeconds(tiempoEspera);
            }
            while (piezasEnAnimacion > 0) yield return null;
        }

        // FASE 2.5: Antena renace y resetea su LED
        if (antenaVisual != null)
        {
            progresoVisualLed = 0f;
            Vector3 escalaBloopAntena = escalaOriginalAntena * multiplicadorBloop;
            float umbralBloopAntena = escalaBloopAntena.magnitude * 0.02f + 0.01f;

            while (Vector3.Distance(antenaVisual.localScale, escalaBloopAntena) > umbralBloopAntena)
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaBloopAntena, Time.deltaTime * velocidadMostrarArma);
                yield return null;
            }
            antenaVisual.localScale = escalaBloopAntena;

            float umbralFinalAntena = escalaOriginalAntena.magnitude * 0.02f + 0.01f;
            while (Vector3.Distance(antenaVisual.localScale, escalaOriginalAntena) > umbralFinalAntena)
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaOriginalAntena, Time.deltaTime * velocidadBloop);
                yield return null;
            }
            antenaVisual.localScale = escalaOriginalAntena;
        }

        // FASE 3: Arma aparece en cascada
        if (partesArmaDesbloqueada != null && partesArmaDesbloqueada.Length > 0)
        {
            if (partesArmaDesbloqueada[0] != null)
            {
                partesArmaDesbloqueada[0].SetActive(true);
                partesArmaDesbloqueada[0].transform.localScale = escalasOriginales[partesArmaDesbloqueada[0]];
            }

            for (int i = 1; i < partesArmaDesbloqueada.Length; i++)
            {
                GameObject pieza = partesArmaDesbloqueada[i];
                if (pieza == null || !escalasOriginales.ContainsKey(pieza)) continue;

                pieza.SetActive(true);
                pieza.transform.localScale = Vector3.zero;

                Vector3 escalaFinal = escalasOriginales[pieza];
                Vector3 escalaBloop = escalaFinal * multiplicadorBloop;

                StartCoroutine(AnimarPiezaIndividualArma(pieza, escalaFinal, escalaBloop));

                float tiempoTotalArma = 0.35f;
                float tiempoEspera = Mathf.Lerp(0.01f, tiempoTotalArma, porcentajeCascadaArma);
                yield return new WaitForSeconds(tiempoEspera);
            }
            while (piezasEnAnimacion > 0) yield return null;
        }

        enTransicion = false;
    }

    private IEnumerator AnimarMejora(EstadisticasNivel nivelNuevo)
    {
        enTransicion = true;
        piezasEnAnimacion = 0;

        // FASE 1: Antena se esconde
        if (antenaVisual != null && antenaVisual.gameObject.activeSelf)
        {
            Vector3 escalaBloopAntena = escalaOriginalAntena * multiplicadorBloop;
            float umbralBloopAntena = escalaBloopAntena.magnitude * 0.02f + 0.01f;

            while (Vector3.Distance(antenaVisual.localScale, escalaBloopAntena) > umbralBloopAntena)
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaBloopAntena, Time.deltaTime * velocidadBloop * 2f);
                yield return null;
            }
            antenaVisual.localScale = escalaBloopAntena;

            if (tiempoEsperaBloopRuinas > 0f) yield return new WaitForSeconds(tiempoEsperaBloopRuinas);

            float umbralAntena = escalaOriginalAntena.magnitude * 0.05f + 0.01f;
            while (antenaVisual.localScale.magnitude > umbralAntena)
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, Vector3.zero, Time.deltaTime * velocidadOcultarAntena);
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

                float tiempoTotalPieza = 0.15f + tiempoEsperaBloopRuinas + 0.25f;
                float tiempoEspera = Mathf.Lerp(0.01f, tiempoTotalPieza, porcentajeCascadaArma);
                yield return new WaitForSeconds(tiempoEspera);
            }
            while (piezasEnAnimacion > 0) yield return null;
        }

        // FASE 3: Antena renace
        if (antenaVisual != null)
        {
            progresoVisualLed = 0f;
            Vector3 escalaBloopAntena = escalaOriginalAntena * multiplicadorBloop;
            float umbralBloopAntena = escalaBloopAntena.magnitude * 0.02f + 0.01f;

            while (Vector3.Distance(antenaVisual.localScale, escalaBloopAntena) > umbralBloopAntena)
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaBloopAntena, Time.deltaTime * velocidadMostrarArma);
                yield return null;
            }
            antenaVisual.localScale = escalaBloopAntena;

            float umbralFinalAntena = escalaOriginalAntena.magnitude * 0.02f + 0.01f;
            while (Vector3.Distance(antenaVisual.localScale, escalaOriginalAntena) > umbralFinalAntena)
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaOriginalAntena, Time.deltaTime * velocidadBloop);
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
                Vector3 escalaBloop = escalaFinal * multiplicadorBloop;

                StartCoroutine(AnimarPiezaIndividualArma(pieza, escalaFinal, escalaBloop));

                float tiempoTotalArma = 0.35f;
                float tiempoEspera = Mathf.Lerp(0.01f, tiempoTotalArma, porcentajeCascadaArma);
                yield return new WaitForSeconds(tiempoEspera);
            }
            while (piezasEnAnimacion > 0) yield return null;
        }

        enTransicion = false;
    }
}