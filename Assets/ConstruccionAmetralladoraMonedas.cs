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
    }

    [Header("Referencias de la construcción")]
    public GameObject ruinasVisual;
    [Tooltip("Objeto completo de la torreta, que se ocultará hasta construirla.")]
    public GameObject armaDesbloqueada;
    public AmetralladoraAsteroides ametralladora;

    [Header("Antena de Progreso")]
    [Tooltip("El objeto 3D de la antena que se encogerá primero.")]
    public Transform antenaVisual;
    [Tooltip("El script ControladorNeon asignado a la bombilla de la antena.")]
    public ControladorNeon ledAntena;

    [ColorUsage(true, true)] public Color colorLedVacio = Color.red;
    [ColorUsage(true, true)] public Color colorLedLleno = Color.green;

    public float intensidadLedVacio = 1f;
    public float intensidadLedLleno = 5f;

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
    [Tooltip("Cuánto crecen los objetos al recibir una moneda (1.15 = 15% más grandes).")]
    public float multiplicadorBloop = 1.15f;
    [Tooltip("Velocidad a la que los objetos recuperan su tamaño original tras el bloop.")]
    public float velocidadBloop = 15f;
    [Tooltip("Tiempo en segundos que las ruinas se mantienen infladas al máximo antes de encogerse.")]
    public float tiempoEsperaBloopRuinas = 0.2f;

    [Space(10)]
    [Tooltip("Velocidad a la que la antena se encoge hasta desaparecer al completarse.")]
    public float velocidadOcultarAntena = 15f;
    [Tooltip("Velocidad a la que las ruinas se encogen hasta desaparecer.")]
    public float velocidadOcultarRuinas = 10f;
    [Tooltip("Velocidad a la que el arma nueva crece desde cero hasta su tamaño original.")]
    public float velocidadMostrarArma = 10f;

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

    public int MonedasRestantes => NivelMaximo
        ? 0
        : Mathf.Max(0, PrecioActual - monedasAportadas);

    private readonly HashSet<int> proyectilesDelFrame = new HashSet<int>();
    private int frameRegistro = -1;

    // Variables internas para animaciones
    private Vector3 escalaOriginalRuinas;
    private Vector3 escalaOriginalArma;
    private Coroutine corrutinaBloop;
    private bool enTransicion = false;

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
        if (ametralladora == null && armaDesbloqueada != null)
            ametralladora = armaDesbloqueada.GetComponentInChildren<AmetralladoraAsteroides>(true);

        if (armaDesbloqueada == null && ametralladora != null)
            armaDesbloqueada = ametralladora.gameObject;
    }

    private void Start()
    {
        if (ruinasVisual == null || armaDesbloqueada == null || ametralladora == null)
        {
            Debug.LogError(name + ": asigna Ruinas Visual, Arma Desbloqueada y Ametralladora.", this);
            enabled = false;
            return;
        }

        if (transform.IsChildOf(ruinasVisual.transform) ||
            transform.IsChildOf(armaDesbloqueada.transform))
        {
            Debug.LogError(name + ": ZonaPago debe estar FUERA de Ruinas y Arma.", this);
            enabled = false;
            return;
        }

        Collider zona = GetComponent<Collider>();
        if (zona == null || !zona.isTrigger)
            Debug.LogWarning(name + ": añade un BoxCollider con Is Trigger activado a ZonaPago.", this);

        // Guardamos las escalas originales antes de modificar nada
        escalaOriginalRuinas = ruinasVisual.transform.localScale;
        escalaOriginalArma = armaDesbloqueada.transform.localScale;
        if (antenaVisual != null) escalaOriginalAntena = antenaVisual.localScale;

        if (construida) monedasAportadas = Mathf.Clamp(monedasAportadas, 0, PrecioActual);

        SincronizarAspecto(false);
        AplicarEstadisticas();
        ActualizarPresentacion();
    }

    private void Update()
    {
        if (!construida && ledAntena != null && PrecioActual > 0)
        {
            float progresoObjetivo = (float)monedasAportadas / PrecioActual;
            progresoVisualLed = Mathf.Lerp(progresoVisualLed, progresoObjetivo, Time.deltaTime * 5f);

            ledAntena.colorNeon = Color.Lerp(colorLedVacio, colorLedLleno, progresoVisualLed);
            ledAntena.intensidad = Mathf.Lerp(intensidadLedVacio, intensidadLedLleno, progresoVisualLed);
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

        // Bloop animado si no está justo construyéndose
        if (!enTransicion)
        {
            if (corrutinaBloop != null) StopCoroutine(corrutinaBloop);
            corrutinaBloop = StartCoroutine(HacerBloop());
        }

        if (audioSource != null && sonidoMoneda != null)
            audioSource.PlayOneShot(sonidoMoneda);

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

            if (monedasAportadas >= PrecioActual)
                CompletarEtapa();
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
            SincronizarAspecto(true); // Animación secuencial de construcción
            AplicarEstadisticas();
            ReproducirParticulas(particulasConstruccion);
            ReproducirSonido(sonidoConstruccion);
            Debug.Log("¡Ametralladora construida! Nivel 1 activado.", this);
        }
        else if (nivelActual < 3)
        {
            nivelActual++;
            AplicarEstadisticas();
            ReproducirParticulas(particulasMejora);
            ReproducirSonido(sonidoMejora);
            Debug.Log("Ametralladora mejorada a nivel " + nivelActual, this);
        }
    }

    private void SincronizarAspecto(bool animado)
    {
        if (!animado)
        {
            if (ruinasVisual != null)
            {
                ruinasVisual.SetActive(!construida);
                ruinasVisual.transform.localScale = construida ? Vector3.zero : escalaOriginalRuinas;
            }
            if (armaDesbloqueada != null)
            {
                armaDesbloqueada.SetActive(construida);
                armaDesbloqueada.transform.localScale = construida ? escalaOriginalArma : Vector3.zero;
            }
            if (antenaVisual != null)
            {
                antenaVisual.gameObject.SetActive(!construida);
                antenaVisual.localScale = construida ? Vector3.zero : escalaOriginalAntena;
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

        EstadisticasNivel stats = nivelActual == 1 ? nivel1 :
            nivelActual == 2 ? nivel2 : nivel3;
        if (stats == null) return;

        ametralladora.cadenciaDisparo = Mathf.Max(0.01f, stats.cadenciaDisparo);
        ametralladora.alcance = Mathf.Max(1f, stats.alcance);
        ametralladora.velocidadGiro = Mathf.Max(0.01f, stats.velocidadGiro);
        ametralladora.velocidadMaximaProyectil =
            Mathf.Max(1f, stats.velocidadMaximaProyectil);
    }

    public void ComprarConstruccionDesdeTienda()
    {
        if (!construida) PagarRestanteDesdeTienda();
    }

    public void ComprarMejoraDesdeTienda()
    {
        if (construida && !NivelMaximo) PagarRestanteDesdeTienda();
    }

    private void PagarRestanteDesdeTienda()
    {
        if (cartera == null)
        {
            Debug.LogWarning("ConstruccionAmetralladoraMonedas: asigna Cartera.", this);
            return;
        }

        int faltan = MonedasRestantes;
        if (faltan <= 0) return;

        if (!cartera.GastarMonedas(faltan))
        {
            Debug.Log("No hay monedas suficientes. Faltan " + faltan + ".", this);
            return;
        }

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
            if (!construida)
                textoProgreso.text = "CONSTRUIR AMETRALLADORA\n" +
                    monedasAportadas + " / " + PrecioActual + " MONEDAS";
            else if (NivelMaximo)
                textoProgreso.text = "AMETRALLADORA\nNIVEL MÁXIMO (3)";
            else
                textoProgreso.text = "AMETRALLADORA NIVEL " + nivelActual +
                    "\nMEJORA: " + monedasAportadas + " / " + PrecioActual + " MONEDAS";
        }

        ProgresoActualizado?.Invoke();
        if (tiendaTrabajo != null) tiendaTrabajo.RefrescarTienda();
    }

    private void ReproducirParticulas(ParticleSystem[] sistemas)
    {
        if (sistemas == null) return;
        foreach (ParticleSystem ps in sistemas)
            if (ps != null) ps.Play();
    }

    private void ReproducirSonido(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    // =========================================================================
    // CORRUTINAS DE ANIMACIÓN (GAME FEEL)
    // =========================================================================

    private IEnumerator HacerBloop()
    {
        Transform objPrincipal = construida ? armaDesbloqueada.transform : ruinasVisual.transform;
        Vector3 escalaBasePrincipal = construida ? escalaOriginalArma : escalaOriginalRuinas;

        bool hayAntena = !construida && antenaVisual != null && antenaVisual.gameObject.activeSelf;
        Vector3 escalaBaseAntena = hayAntena ? escalaOriginalAntena : Vector3.zero;

        // Crecimiento instantáneo al impactar la moneda para ambos objetos
        if (objPrincipal != null) objPrincipal.localScale = escalaBasePrincipal * multiplicadorBloop;
        if (hayAntena) antenaVisual.localScale = escalaBaseAntena * multiplicadorBloop;

        // Bucle paralelo para recuperar el tamaño original suavemente
        while (true)
        {
            if (enTransicion) yield break; // Abortar si salta la cinemática de construcción

            // SOLUCIÓN MATEMÁTICA: Tolerancia subida a 0.02f para evitar la "cola" exponencial y pausas fantasmas
            bool principalListo = objPrincipal == null || Vector3.Distance(objPrincipal.localScale, escalaBasePrincipal) < 0.02f;
            bool antenaLista = !hayAntena || Vector3.Distance(antenaVisual.localScale, escalaBaseAntena) < 0.02f;

            if (principalListo && antenaLista) break;

            if (objPrincipal != null)
                objPrincipal.localScale = Vector3.Lerp(objPrincipal.localScale, escalaBasePrincipal, Time.deltaTime * velocidadBloop);

            if (hayAntena)
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, escalaBaseAntena, Time.deltaTime * velocidadBloop);

            yield return null;
        }

        // Forzamos la escala exacta al terminar para que quede perfecto
        if (objPrincipal != null) objPrincipal.localScale = escalaBasePrincipal;
        if (hayAntena) antenaVisual.localScale = escalaBaseAntena;
    }

    private IEnumerator AnimarTransicionConstruccion()
    {
        enTransicion = true;

        // FASE 1: Reducimos la Antena primero (si existe y está activa)
        if (antenaVisual != null && antenaVisual.gameObject.activeSelf)
        {
            while (antenaVisual.localScale.sqrMagnitude > 0.01f) // Tolerancia corregida
            {
                antenaVisual.localScale = Vector3.Lerp(antenaVisual.localScale, Vector3.zero, Time.deltaTime * velocidadOcultarAntena);
                yield return null;
            }
            antenaVisual.localScale = Vector3.zero;
            antenaVisual.gameObject.SetActive(false);
        }

        // FASE 1.5: Anticipación de las Ruinas (Bloop hacia arriba antes de desaparecer)
        if (ruinasVisual != null && ruinasVisual.activeSelf)
        {
            Vector3 escalaBloopObjetivo = escalaOriginalRuinas * multiplicadorBloop;

            // Hinchamos las ruinas rápidamente usando una aceleración extra (x1.5) para que el "snap" sea inmediato
            while (Vector3.Distance(ruinasVisual.transform.localScale, escalaBloopObjetivo) > 0.02f)
            {
                ruinasVisual.transform.localScale = Vector3.Lerp(ruinasVisual.transform.localScale, escalaBloopObjetivo, Time.deltaTime * velocidadBloop * 1.5f);
                yield return null;
            }
            ruinasVisual.transform.localScale = escalaBloopObjetivo;

            // ¡Ahora este tiempo de espera responderá exactamente a los segundos que le marques!
            if (tiempoEsperaBloopRuinas > 0f)
            {
                yield return new WaitForSeconds(tiempoEsperaBloopRuinas);
            }
        }

        // FASE 2: Reducimos las Ruinas y desplegamos el Arma
        if (armaDesbloqueada != null)
        {
            armaDesbloqueada.SetActive(true);
            armaDesbloqueada.transform.localScale = Vector3.zero;
        }

        bool ruinasOcultas = ruinasVisual == null || !ruinasVisual.activeSelf;
        bool armaLista = armaDesbloqueada == null;

        while (!ruinasOcultas || !armaLista)
        {
            if (!ruinasOcultas)
            {
                ruinasVisual.transform.localScale = Vector3.Lerp(ruinasVisual.transform.localScale, Vector3.zero, Time.deltaTime * velocidadOcultarRuinas);

                if (ruinasVisual.transform.localScale.sqrMagnitude < 0.01f)
                {
                    ruinasVisual.transform.localScale = Vector3.zero;
                    ruinasVisual.SetActive(false);
                    ruinasOcultas = true;
                }
            }

            if (!armaLista)
            {
                armaDesbloqueada.transform.localScale = Vector3.Lerp(armaDesbloqueada.transform.localScale, escalaOriginalArma, Time.deltaTime * velocidadMostrarArma);

                if (Vector3.Distance(armaDesbloqueada.transform.localScale, escalaOriginalArma) < 0.02f)
                {
                    armaDesbloqueada.transform.localScale = escalaOriginalArma;
                    armaLista = true;
                }
            }

            yield return null;
        }

        enTransicion = false;
    }
}