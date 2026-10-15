using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Se coloca en un objeto SIEMPRE ACTIVO con un Collider marcado Is Trigger.
/// Recibe monedas disparadas (ProyectilArma + Moneda) para construir la
/// desgastadora y para pagar sus mejoras de nivel. La tienda usa el MISMO
/// saldo de progreso a través de ComprarMejoraDesdeTienda().
/// </summary>
public class DesbloqueoProcesadoraMonedas : MonoBehaviour
{
    [Header("Construcción: objetos de la escena")]
    public MaquinaErosion maquinaErosion;
    [Tooltip("Objeto visual de las ruinas. NO debe contener este componente ni su trigger.")]
    public GameObject ruinasVisual;
    [Tooltip("Objeto de la máquina completa. Debe estar fuera de la jerarquía del trigger.")]
    public GameObject maquinaDesbloqueada;

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

    // Conservados como campos compatibles con el montaje anterior.
    // Al usar ruinas + máquina como objetos separados ya no es necesario
    // cambiar materiales de renderers para simular el bloqueo.
    [Header("Compatibilidad con montaje anterior (opcional)")]
    public GameObject[] objetosSoloDesbloqueada;
    public Renderer[] renderersProcesadora;
    public Material materialBloqueado;

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

    private readonly HashSet<int> proyectilesRecibidosFrame = new HashSet<int>();
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

    private void Start()
    {
        if (maquinaErosion == null || ruinasVisual == null || maquinaDesbloqueada == null)
        {
            Debug.LogError(name + ": asigna MaquinaErosion, Ruinas Visual y Maquina Desbloqueada.", this);
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

        bool desbloqueada = maquinaErosion.ProcesadoraDesbloqueada;
        ruinasVisual.SetActive(!desbloqueada);
        maquinaDesbloqueada.SetActive(desbloqueada);

        if (desbloqueada)
            monedasRecibidas = monedasNecesarias;

        SincronizarNivelPagado();
        ActualizarTexto();
        RefrescarTienda();
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
        int id = proyectil.GetInstanceID();
        if (!proyectilesRecibidosFrame.Add(id))
            return;

        int valor = Mathf.Max(1, moneda.valor);
        if (!AceptarPago(valor))
            return;

        if (audioSource != null && sonidoMoneda != null)
            audioSource.PlayOneShot(sonidoMoneda);

        // El arma ya descontó el coste al disparar: aquí NO cobramos a Cartera.
        Destroy(proyectil.gameObject);
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
        // Primero aparece la máquina; después queda habilitada para procesar.
        maquinaDesbloqueada.SetActive(true);
        maquinaErosion.DesbloquearProcesadora();
        ruinasVisual.SetActive(false);

        if (objetosSoloDesbloqueada != null)
        {
            foreach (GameObject go in objetosSoloDesbloqueada)
                if (go != null) go.SetActive(true);
        }

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
}
