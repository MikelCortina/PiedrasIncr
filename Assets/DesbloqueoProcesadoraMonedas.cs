using UnityEngine;

public class DesbloqueoProcesadoraMonedas : MonoBehaviour
{
    // =====================================================
    // REFERENCIAS
    // =====================================================

    [Header("Referencias")]

    public MaquinaErosion maquinaErosion;


    [Tooltip(
        "UI flotante de la procesadora."
    )]
    public UIProcesadoraMundo uiProcesadora;


    // =====================================================
    // VISUALES BLOQUEADA / DESBLOQUEADA
    // =====================================================

    [Header("Aspecto de la máquina")]

    [Tooltip(
        "Objetos que estarán ocultos mientras la procesadora " +
        "esté bloqueada. Ejemplo: VFX, luces, humo."
    )]
    public GameObject[] objetosSoloDesbloqueada;


    [Tooltip(
        "Renderers que estarán oscuros mientras " +
        "la máquina esté bloqueada."
    )]
    public Renderer[] renderersProcesadora;


    [Tooltip(
        "Material oscuro utilizado mientras la " +
        "procesadora está bloqueada."
    )]
    public Material materialBloqueado;


    // =====================================================
    // TIENDA
    // =====================================================

    [Header("Tienda")]

    public TiendaTrabajo tiendaTrabajo;


    // =====================================================
    // DESBLOQUEO
    // =====================================================

    [Header("Desbloqueo")]

    [Min(1)]
    public int monedasNecesarias = 20;


    [SerializeField]
    private int monedasRecibidas = 0;


    // =====================================================
    // EFECTOS
    // =====================================================

    [Header("Efectos de desbloqueo")]

    public ParticleSystem[] particulasDesbloqueo;

    public AudioSource audioSource;

    public AudioClip sonidoMoneda;

    public AudioClip sonidoDesbloqueo;


    // =====================================================
    // MATERIALES ORIGINALES
    // =====================================================

    private Material[][] materialesOriginales;


    // =====================================================
    // PROPIEDADES
    // =====================================================

    public int MonedasRecibidas
        => monedasRecibidas;


    public int MonedasNecesarias
        => monedasNecesarias;


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        // =================================================
        // MÁQUINA
        // =================================================

        if (maquinaErosion == null)
        {
            maquinaErosion =
                GetComponentInParent<
                    MaquinaErosion
                >();
        }


        // =================================================
        // UI
        // =================================================

        if (uiProcesadora == null &&
            maquinaErosion != null)
        {
            uiProcesadora =
                maquinaErosion
                    .GetComponentInChildren<
                        UIProcesadoraMundo
                    >(
                        true
                    );
        }


        // =================================================
        // GUARDAR MATERIALES
        // =================================================

        GuardarMaterialesOriginales();
    }


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        if (maquinaErosion == null)
        {
            Debug.LogError(
                name +
                ": no se ha encontrado MaquinaErosion."
            );

            return;
        }


        // =================================================
        // BUSCAR TIENDA SI NO ESTÁ ASIGNADA
        // =================================================

        if (tiendaTrabajo == null)
        {
            tiendaTrabajo =
                FindFirstObjectByType<
                    TiendaTrabajo
                >();
        }


        // =================================================
        // ESTADO INICIAL
        // =================================================

        if (maquinaErosion
            .ProcesadoraDesbloqueada)
        {
            monedasRecibidas =
                monedasNecesarias;


            MostrarDesbloqueada();
        }
        else
        {
            monedasRecibidas =
                Mathf.Clamp(
                    monedasRecibidas,
                    0,
                    monedasNecesarias
                );


            MostrarBloqueada();
        }
    }


    // =====================================================
    // GUARDAR MATERIALES ORIGINALES
    // =====================================================

    private void GuardarMaterialesOriginales()
    {
        if (renderersProcesadora == null)
        {
            materialesOriginales =
                new Material[0][];

            return;
        }


        materialesOriginales =
            new Material[
                renderersProcesadora.Length
            ][];


        for (int i = 0;
             i < renderersProcesadora.Length;
             i++)
        {
            Renderer renderer =
                renderersProcesadora[i];


            if (renderer == null)
                continue;


            materialesOriginales[i] =
                renderer.sharedMaterials;
        }
    }


    // =====================================================
    // DETECTAR IMPACTO
    // =====================================================

    private void OnTriggerEnter(
        Collider otro)
    {
        if (otro == null ||
            maquinaErosion == null)
        {
            return;
        }


        // =================================================
        // OBTENER OBJETO DEL PROYECTIL
        // =================================================

        GameObject objetoProyectil =
            otro.gameObject;


        if (otro.attachedRigidbody != null)
        {
            objetoProyectil =
                otro.attachedRigidbody
                    .gameObject;
        }


        // =================================================
        // ¿ES PROYECTIL DEL ARMA?
        // =================================================

        ProyectilArma proyectil =
            objetoProyectil
                .GetComponent<ProyectilArma>();


        if (proyectil == null)
        {
            proyectil =
                otro.GetComponentInParent<
                    ProyectilArma
                >();
        }


        // Si no viene del arma,
        // no nos interesa.
        if (proyectil == null)
            return;


        // =================================================
        // MOSTRAR UI POR EL IMPACTO
        // =================================================

        if (uiProcesadora != null)
        {
            uiProcesadora
                .MostrarTemporalmente();
        }


        // =================================================
        // SI YA ESTÁ DESBLOQUEADA
        // =================================================

        if (maquinaErosion
            .ProcesadoraDesbloqueada)
        {
            // Mostramos información,
            // pero NO consumimos la moneda.
            return;
        }


        // =================================================
        // BUSCAR MONEDA
        // =================================================

        Moneda moneda =
            objetoProyectil
                .GetComponent<Moneda>();


        if (moneda == null)
        {
            moneda =
                otro.GetComponentInParent<
                    Moneda
                >();
        }


        if (moneda == null)
            return;


        // =================================================
        // VALOR DE LA MONEDA
        // =================================================

        int valor =
            Mathf.Max(
                1,
                moneda.valor
            );


        // =================================================
        // SUMAR
        // =================================================

        monedasRecibidas +=
            valor;


        monedasRecibidas =
            Mathf.Clamp(
                monedasRecibidas,
                0,
                monedasNecesarias
            );


        // =================================================
        // SONIDO DE MONEDA
        // =================================================

        if (audioSource != null &&
            sonidoMoneda != null)
        {
            audioSource.PlayOneShot(
                sonidoMoneda
            );
        }


        Debug.Log(
            "Procesadora: " +
            monedasRecibidas +
            "/" +
            monedasNecesarias +
            " monedas."
        );


        // =================================================
        // CONSUMIR PROYECTIL
        // =================================================

        Destroy(
            objetoProyectil
        );


        // =================================================
        // ¿HEMOS LLEGADO AL MÁXIMO?
        // =================================================

        if (monedasRecibidas >=
            monedasNecesarias)
        {
            Desbloquear();
        }
    }


    // =====================================================
    // DESBLOQUEAR
    // =====================================================

    private void Desbloquear()
    {
        if (maquinaErosion == null)
            return;


        if (maquinaErosion
            .ProcesadoraDesbloqueada)
        {
            return;
        }


        monedasRecibidas =
            monedasNecesarias;


        // =================================================
        // DESBLOQUEAR MÁQUINA
        // =================================================

        maquinaErosion
            .DesbloquearProcesadora();


        // =================================================
        // ASPECTO NORMAL
        // =================================================

        MostrarDesbloqueada();


        // =================================================
        // MOSTRAR UI
        // =================================================

        if (uiProcesadora != null)
        {
            uiProcesadora
                .MostrarTemporalmente();
        }


        // =================================================
        // ACTUALIZAR TIENDA
        // =================================================

        if (tiendaTrabajo != null)
        {
            tiendaTrabajo
                .RefrescarTienda();
        }


        // =================================================
        // PARTÍCULAS
        // =================================================

        if (particulasDesbloqueo != null)
        {
            foreach (
                ParticleSystem ps
                in particulasDesbloqueo)
            {
                if (ps != null)
                {
                    ps.Play();
                }
            }
        }


        // =================================================
        // SONIDO
        // =================================================

        if (audioSource != null &&
            sonidoDesbloqueo != null)
        {
            audioSource.PlayOneShot(
                sonidoDesbloqueo
            );
        }


        Debug.Log(
            "¡PROCESADORA ACTIVADA!"
        );
    }


    // =====================================================
    // BLOQUEADA
    // =====================================================

    private void MostrarBloqueada()
    {
        // =================================================
        // APAGAR VFX / LUCES
        // =================================================

        ActualizarObjetosDesbloqueo(
            false
        );


        // =================================================
        // MATERIAL OSCURO
        // =================================================

        if (materialBloqueado == null ||
            renderersProcesadora == null)
        {
            return;
        }


        foreach (
            Renderer renderer
            in renderersProcesadora)
        {
            if (renderer == null)
                continue;


            Material[] materiales =
                renderer.sharedMaterials;


            for (int i = 0;
                 i < materiales.Length;
                 i++)
            {
                materiales[i] =
                    materialBloqueado;
            }


            renderer.sharedMaterials =
                materiales;
        }
    }


    // =====================================================
    // DESBLOQUEADA
    // =====================================================

    private void MostrarDesbloqueada()
    {
        // =================================================
        // ENCENDER VFX / LUCES
        // =================================================

        ActualizarObjetosDesbloqueo(
            true
        );


        // =================================================
        // RECUPERAR MATERIALES
        // =================================================

        if (renderersProcesadora == null ||
            materialesOriginales == null)
        {
            return;
        }


        for (int i = 0;
             i < renderersProcesadora.Length;
             i++)
        {
            Renderer renderer =
                renderersProcesadora[i];


            if (renderer == null)
                continue;


            if (i >=
                materialesOriginales.Length)
            {
                continue;
            }


            if (materialesOriginales[i] == null)
                continue;


            renderer.sharedMaterials =
                materialesOriginales[i];
        }
    }


    // =====================================================
    // OBJETOS SOLO DESBLOQUEADA
    // =====================================================

    private void ActualizarObjetosDesbloqueo(
        bool activos)
    {
        if (objetosSoloDesbloqueada == null)
            return;


        foreach (
            GameObject objeto
            in objetosSoloDesbloqueada)
        {
            if (objeto != null)
            {
                objeto.SetActive(
                    activos
                );
            }
        }
    }
}