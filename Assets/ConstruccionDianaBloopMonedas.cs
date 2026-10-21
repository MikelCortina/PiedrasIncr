using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Construye y mejora una DianaBloop disparando monedas a una zona de pago.
/// Compatible con el pago por Cartera desde un boton de tienda.
/// La moneda disparada ya se cobro al salir del arma: no se cobra otra vez.
/// </summary>
public class ConstruccionDianaBloopMonedas : MonoBehaviour
{
    [Serializable]
    public class EstadisticasNivel
    {
        [Min(0.05f)] public float tiempoCooldown = 2.5f;
        [Range(1, 3)] public int piedrasPorActivacion = 1;
    }

    [Header("Construccion: referencias")]
    public GameObject ruinasVisual;
    [Tooltip("GameObject completo de la DianaBloop, desactivado hasta construir.")]
    public GameObject dianaDesbloqueada;
    public DianaBloop dianaBloop;

    [Header("Costes (monedas)")]
    [Min(1)] public int precioConstruccion = 20;
    [Min(1)] public int precioNivel2 = 10;
    [Min(1)] public int precioNivel3 = 20;

    [Header("Progreso")]
    [SerializeField] private bool construida = false;
    [SerializeField, Range(1, 3)] private int nivelActual = 1;
    [SerializeField, Min(0)] private int monedasAportadas = 0;

    [Header("Estadisticas por nivel")]
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

    public int MonedasRestantes => NivelMaximo ? 0 :
        Mathf.Max(0, PrecioActual - monedasAportadas);

    // Un mismo proyectil con varios colliders solo puede aportar una vez.
    private readonly HashSet<int> proyectilesDelFrame = new HashSet<int>();
    private int frameRegistro = -1;

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
        if (dianaBloop == null && dianaDesbloqueada != null)
            dianaBloop = dianaDesbloqueada.GetComponentInChildren<DianaBloop>(true);
        if (dianaDesbloqueada == null && dianaBloop != null)
            dianaDesbloqueada = dianaBloop.gameObject;
    }

    private void Start()
    {
        if (ruinasVisual == null || dianaDesbloqueada == null || dianaBloop == null)
        {
            Debug.LogError(name + ": asigna Ruinas Visual, Diana Desbloqueada y Diana Bloop.", this);
            enabled = false;
            return;
        }

        if (transform.IsChildOf(ruinasVisual.transform) ||
            transform.IsChildOf(dianaDesbloqueada.transform) ||
            dianaDesbloqueada == ruinasVisual)
        {
            Debug.LogError(name + ": ZonaPago debe ser hermana de Ruinas y DianaBloop, no hija.", this);
            enabled = false;
            return;
        }

        Collider trigger = GetComponent<Collider>();
        if (trigger == null || !trigger.isTrigger)
            Debug.LogWarning(name + ": usa un BoxCollider con Is Trigger activado en ZonaPago.", this);

        SincronizarAspecto();
        AplicarEstadisticas();
        ActualizarPresentacion();
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

        // SOLO el valor del proyectil. El arma ya hizo Cartera.GastarMonedas.
        AportarPago(Mathf.Max(1, moneda.valor));
        ReproducirSonido(sonidoMoneda);
        Destroy(proyectil.gameObject);
    }

    private void AportarPago(int cantidad)
    {
        int pendiente = cantidad;
        while (pendiente > 0 && !NivelMaximo)
        {
            int faltan = MonedasRestantes;
            if (faltan <= 0)
            {
                CompletarEtapa();
                continue;
            }

            int aportado = Mathf.Min(pendiente, faltan);
            monedasAportadas += aportado;
            pendiente -= aportado;
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
            SincronizarAspecto();
            AplicarEstadisticas();
            ReproducirParticulas(particulasConstruccion);
            ReproducirSonido(sonidoConstruccion);
            Debug.Log("DianaBloop construida: nivel 1, una piedra.", this);
        }
        else if (nivelActual < 3)
        {
            nivelActual++;
            AplicarEstadisticas();
            ReproducirParticulas(particulasMejora);
            ReproducirSonido(sonidoMejora);
            Debug.Log("DianaBloop mejorada al nivel " + nivelActual, this);
        }
    }

    private void SincronizarAspecto()
    {
        if (ruinasVisual != null)
            ruinasVisual.SetActive(!construida);

        if (dianaDesbloqueada != null)
        {
            if (construida)
            {
                // Al construir, activar tanto el meteorito como todos sus hijos.
                ActivarMeteoritoCompleto(dianaDesbloqueada);
            }
            else
            {
                // Antes de construir, mantener oculto todo el meteorito.
                dianaDesbloqueada.SetActive(false);
            }
        }
    }

    private void AplicarEstadisticas()
    {
        if (!construida || dianaBloop == null) return;

        EstadisticasNivel datos = nivelActual == 1 ? nivel1 :
            nivelActual == 2 ? nivel2 : nivel3;
        if (datos == null) return;

        dianaBloop.tiempoCooldown = Mathf.Max(0.05f, datos.tiempoCooldown);
        dianaBloop.piedrasPorActivacion = Mathf.Clamp(datos.piedrasPorActivacion, 1, 3);
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
            Debug.LogWarning(name + ": asigna la Cartera del jugador para pagar desde tienda.", this);
            return;
        }

        int faltan = MonedasRestantes;
        if (faltan <= 0) return;

        if (!cartera.GastarMonedas(faltan))
        {
            Debug.Log(name + ": monedas insuficientes. Faltan " + faltan + ".", this);
            return;
        }

        AportarPago(faltan);
    }

    private void ActualizarPresentacion()
    {
        if (textoProgreso != null)
        {
            if (!construida)
                textoProgreso.text = "CONSTRUIR DIANA BLOOP\n" +
                    monedasAportadas + " / " + PrecioActual + " MONEDAS";
            else if (NivelMaximo)
                textoProgreso.text = "DIANA BLOOP\nNIVEL MAXIMO (3)";
            else
                textoProgreso.text = "DIANA BLOOP NIVEL " + nivelActual +
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


    private void ActivarMeteoritoCompleto(GameObject meteorito)
    {
        if (meteorito == null) return;

        meteorito.SetActive(true);

        foreach (Transform hijo in meteorito.GetComponentsInChildren<Transform>(true))
        {
            hijo.gameObject.SetActive(true);
        }
    }

}
