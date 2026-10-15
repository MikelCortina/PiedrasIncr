using System;
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

    // Sólo bloquea impactos repetidos de un mismo proyectil en el mismo frame
    // (por ejemplo, monedas con varios colliders). Destroy es diferido en Unity.
    private readonly HashSet<int> proyectilesDelFrame = new HashSet<int>();
    private int frameRegistro = -1;

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

        // El receptor de monedas no puede desactivarse junto con las ruinas
        // o la torreta: debe ser un hermano bajo un padre que siga activo.
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

        if (construida) monedasAportadas = Mathf.Clamp(monedasAportadas, 0, PrecioActual);
        SincronizarAspecto();
        AplicarEstadisticas();
        ActualizarPresentacion();
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (otro == null || NivelMaximo) return;

        // Buscar el proyectil del arma, no una moneda normal del suelo.
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

        // El arma YA ha pagado Cartera.GastarMonedas al disparar.
        // No descontar monedas dos veces.
        int valor = Mathf.Max(1, moneda.valor);
        AportarPago(valor);

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
            SincronizarAspecto();
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

    private void SincronizarAspecto()
    {
        if (ruinasVisual != null) ruinasVisual.SetActive(!construida);
        if (armaDesbloqueada != null) armaDesbloqueada.SetActive(construida);
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

    /// <summary>Paga lo que falta para construir, desde la tienda.</summary>
    public void ComprarConstruccionDesdeTienda()
    {
        if (!construida) PagarRestanteDesdeTienda();
    }

    /// <summary>Paga lo que falta de la mejora actual, desde la tienda.</summary>
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
}
