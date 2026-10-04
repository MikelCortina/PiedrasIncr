using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIProcesadoraMundo : MonoBehaviour
{
    // =====================================================
    // REFERENCIAS
    // =====================================================

    [Header("Referencias")]

    public MaquinaErosion maquinaErosion;

    public DesbloqueoProcesadoraMonedas desbloqueoMonedas;

    public TMP_Text textoEstado;

    public TMP_Text textoProgreso;

    public Slider barraProgreso;


    // =====================================================
    // POSICIÓN
    // =====================================================

    [Header("Posición del cartel")]

    [Tooltip(
        "Empty colocado encima de la procesadora."
    )]
    public Transform puntoUI;


    [Tooltip(
        "Mantiene el cartel en PuntoUI."
    )]
    public bool seguirPuntoUI = true;


    // =====================================================
    // ORIENTACIÓN
    // =====================================================

    [Header("Mirar al jugador")]

    public bool mirarCamara = true;

    public bool mantenerVertical = true;


    // =====================================================
    // VISIBILIDAD
    // =====================================================

    [Header("Visibilidad")]

    [Tooltip(
        "Distancia desde la que aparece automáticamente."
    )]
    public float distanciaMostrar = 5f;


    [Tooltip(
        "Tiempo que permanece visible después de una moneda, " +
        "desbloqueo o mejora."
    )]
    public float tiempoVisibleTrasEvento = 3f;


    [Tooltip(
        "Velocidad de aparición y desaparición."
    )]
    public float velocidadFade = 5f;


    [Tooltip(
        "CanvasGroup del cartel. Si se deja vacío, " +
        "se busca o crea automáticamente."
    )]
    public CanvasGroup canvasGroup;


    // =====================================================
    // INTERNAS
    // =====================================================

    private Camera camaraPrincipal;

    private float tiempoForzadoVisible = 0f;

    private int nivelAnterior = -1;

    private bool desbloqueadaAnterior = false;

    private bool estadoInicializado = false;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        BuscarReferencias();

        PrepararCanvasGroup();

        ColocarEnPunto();

        ActualizarUI();

        InicializarEstado();

        ActualizarVisibilidad(
            true
        );
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        // =================================================
        // POSICIÓN
        // =================================================

        if (seguirPuntoUI)
        {
            ColocarEnPunto();
        }


        // =================================================
        // CONTENIDO
        // =================================================

        ActualizarUI();


        // =================================================
        // DETECTAR CAMBIOS
        // =================================================

        DetectarCambiosEstado();


        // =================================================
        // VISIBILIDAD
        // =================================================

        ActualizarVisibilidad(
            false
        );


        // =================================================
        // MIRAR JUGADOR
        // =================================================

        if (mirarCamara)
        {
            MirarAJugador();
        }
    }


    // =====================================================
    // BUSCAR REFERENCIAS
    // =====================================================

    private void BuscarReferencias()
    {
        if (maquinaErosion == null)
        {
            maquinaErosion =
                GetComponentInParent<MaquinaErosion>();
        }


        if (desbloqueoMonedas == null &&
            maquinaErosion != null)
        {
            desbloqueoMonedas =
                maquinaErosion
                    .GetComponentInChildren<
                        DesbloqueoProcesadoraMonedas
                    >(
                        true
                    );
        }


        camaraPrincipal =
            Camera.main;
    }


    // =====================================================
    // CANVAS GROUP
    // =====================================================

    private void PrepararCanvasGroup()
    {
        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }


        if (canvasGroup == null)
        {
            canvasGroup =
                gameObject.AddComponent<CanvasGroup>();
        }


        // Esta UI es informativa.
        canvasGroup.interactable =
            false;


        canvasGroup.blocksRaycasts =
            false;
    }


    // =====================================================
    // POSICIONAR
    // =====================================================

    private void ColocarEnPunto()
    {
        if (puntoUI == null)
            return;


        transform.position =
            puntoUI.position;
    }


    // =====================================================
    // INICIALIZAR ESTADO
    // =====================================================

    private void InicializarEstado()
    {
        if (maquinaErosion == null)
            return;


        desbloqueadaAnterior =
            maquinaErosion
                .ProcesadoraDesbloqueada;


        nivelAnterior =
            maquinaErosion
                .NivelProcesado;


        estadoInicializado =
            true;
    }


    // =====================================================
    // DETECTAR DESBLOQUEO / MEJORAS
    // =====================================================

    private void DetectarCambiosEstado()
    {
        if (maquinaErosion == null)
            return;


        if (!estadoInicializado)
        {
            InicializarEstado();

            return;
        }


        bool desbloqueadaActual =
            maquinaErosion
                .ProcesadoraDesbloqueada;


        int nivelActual =
            maquinaErosion
                .NivelProcesado;


        // =================================================
        // ACABA DE DESBLOQUEARSE
        // =================================================

        if (desbloqueadaActual &&
            !desbloqueadaAnterior)
        {
            MostrarTemporalmente();
        }


        // =================================================
        // HA SUBIDO DE NIVEL
        // =================================================

        if (desbloqueadaActual &&
            nivelActual != nivelAnterior)
        {
            MostrarTemporalmente();
        }


        desbloqueadaAnterior =
            desbloqueadaActual;


        nivelAnterior =
            nivelActual;
    }


    // =====================================================
    // MOSTRAR TEMPORALMENTE
    // =====================================================

    public void MostrarTemporalmente()
    {
        tiempoForzadoVisible =
            tiempoVisibleTrasEvento;


        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                1f;
        }
    }


    // =====================================================
    // VISIBILIDAD
    // =====================================================

    private void ActualizarVisibilidad(
        bool instantaneo)
    {
        if (canvasGroup == null)
            return;


        if (camaraPrincipal == null)
        {
            camaraPrincipal =
                Camera.main;
        }


        // =================================================
        // CERCA DEL JUGADOR
        // =================================================

        bool jugadorCerca =
            false;


        if (camaraPrincipal != null)
        {
            Vector3 puntoReferencia =
                transform.position;


            if (maquinaErosion != null)
            {
                puntoReferencia =
                    maquinaErosion
                        .transform
                        .position;
            }


            float distancia =
                Vector3.Distance(
                    camaraPrincipal
                        .transform
                        .position,

                    puntoReferencia
                );


            jugadorCerca =
                distancia <=
                distanciaMostrar;
        }


        // =================================================
        // TEMPORIZADOR
        // =================================================

        if (tiempoForzadoVisible > 0f)
        {
            tiempoForzadoVisible -=
                Time.deltaTime;


            if (tiempoForzadoVisible < 0f)
            {
                tiempoForzadoVisible =
                    0f;
            }
        }


        // =================================================
        // DECIDIR VISIBILIDAD
        // =================================================

        bool mostrar =
            jugadorCerca ||
            tiempoForzadoVisible > 0f;


        float alphaObjetivo =
            mostrar
            ? 1f
            : 0f;


        // =================================================
        // INSTANTÁNEO AL INICIAR
        // =================================================

        if (instantaneo)
        {
            canvasGroup.alpha =
                alphaObjetivo;

            return;
        }


        // =================================================
        // FADE
        // =================================================

        canvasGroup.alpha =
            Mathf.MoveTowards(
                canvasGroup.alpha,
                alphaObjetivo,
                velocidadFade *
                Time.deltaTime
            );
    }


    // =====================================================
    // ACTUALIZAR UI
    // =====================================================

    private void ActualizarUI()
    {
        if (maquinaErosion == null)
            return;


        // =================================================
        // BLOQUEADA
        // =================================================

        if (!maquinaErosion
            .ProcesadoraDesbloqueada)
        {
            if (textoEstado != null)
            {
                textoEstado.text =
                    "PROCESADORA BLOQUEADA";
            }


            int recibidas =
                0;


            int necesarias =
                1;


            if (desbloqueoMonedas != null)
            {
                recibidas =
                    desbloqueoMonedas
                        .MonedasRecibidas;


                necesarias =
                    Mathf.Max(
                        1,
                        desbloqueoMonedas
                            .MonedasNecesarias
                    );
            }


            if (textoProgreso != null)
            {
                textoProgreso.text =
                    recibidas +
                    " / " +
                    necesarias +
                    " MONEDAS";
            }


            if (barraProgreso != null)
            {
                barraProgreso.minValue =
                    0f;


                barraProgreso.maxValue =
                    necesarias;


                barraProgreso.value =
                    recibidas;
            }


            return;
        }


        // =================================================
        // DESBLOQUEADA
        // =================================================

        int nivel =
            maquinaErosion
                .NivelProcesado;


        if (textoEstado != null)
        {
            textoEstado.text =
                "PROCESADORA\nNIVEL " +
                nivel;
        }


        if (barraProgreso != null)
        {
            barraProgreso.minValue =
                0f;


            barraProgreso.maxValue =
                3f;


            barraProgreso.value =
                nivel;
        }


        if (textoProgreso != null)
        {
            if (nivel >= 3)
            {
                textoProgreso.text =
                    "NIVEL MÁXIMO";
            }
            else
            {
                textoProgreso.text =
                    "NIVEL " +
                    nivel +
                    " / 3";
            }
        }
    }


    // =====================================================
    // MIRAR AL JUGADOR
    // =====================================================

    private void MirarAJugador()
    {
        if (camaraPrincipal == null)
        {
            camaraPrincipal =
                Camera.main;
        }


        if (camaraPrincipal == null)
            return;


        Vector3 direccion =
            transform.position -
            camaraPrincipal
                .transform
                .position;


        if (mantenerVertical)
        {
            direccion.y =
                0f;
        }


        if (direccion.sqrMagnitude <
            0.001f)
        {
            return;
        }


        transform.rotation =
            Quaternion.LookRotation(
                direccion,
                Vector3.up
            );
    }


    // =====================================================
    // INSPECTOR
    // =====================================================

    [ContextMenu("Colocar UI en Punto")]
    private void ColocarUIDesdeInspector()
    {
        ColocarEnPunto();
    }
}