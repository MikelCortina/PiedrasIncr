using UnityEngine;
using System.Collections.Generic;

public class SistemaConstruccion : MonoBehaviour
{
    public enum TipoEdificio { Rampa, Pared, Libre, Cinta }

    [System.Serializable]
    public struct InfoEdificio
    {
        public string nombre;
        public TipoEdificio tipo;
        public GameObject[] prefabsReales;
        public GameObject[] prefabsHologramas;

        [Header("Ajustes de Offset")]
        [Tooltip("Desplazamiento extra (Ej: subirlo un poco en Y)")]
        public Vector3 offsetPosicion;
        [Tooltip("Rotación extra (Ej: girarlo 90 grados por defecto)")]
        public Vector3 offsetRotacion;
    }

    [Header("Martillo")]
    public GameObject modeloMartillo;

    [Header("Catálogo de Edificios")]
    public InfoEdificio[] edificios;
    private int indiceEdificioActual = 0;

    private int indiceVarianteActual = 0;

    [Header("Referencias")]
    public Camera camaraPrincipal;

    [Header("Capas (Layers)")]
    public LayerMask capaSuelo;
    public LayerMask capaConectores;
    public LayerMask capaEdificios;

    [Header("Estilo de Contornos (Holograma y Destrucción)")]
    public Color colorOutlineValido = Color.green;
    public float grosorOutlineValido = 2f;
    [Space(5)]
    public Color colorOutlineInvalido = Color.red;
    public float grosorOutlineInvalido = 2f;
    [Space(5)]
    public Color colorOutlineDestruccion = Color.red;
    public float grosorOutlineDestruccion = 4f;

    [Header("Ajustes de Construcción")]
    public KeyCode teclaConstruccion = KeyCode.B;
    public KeyCode teclaCambiarTipo = KeyCode.Tab;
    public float velocidadRotacion = 10f;
    public float distanciaMaximaConstruccion = 15f;

    [Header("Cintas automáticas - Snap")]
    [Tooltip("Ángulo respecto al frente a partir del cual la cinta pasa de recta a curva.")]
    [Range(5f, 80f)]
    public float anguloActivarCurvaCinta = 30f;

    [Tooltip("Evita que el holograma parpadee entre recta y curva cerca del límite.")]
    [Range(0f, 20f)]
    public float histeresisCinta = 8f;

    [Tooltip("Distancia mínima desde el conector que debe tener el punto del ratón para decidir la dirección.")]
    [Min(0.1f)]
    public float distanciaMinimaDireccionCinta = 0.75f;

    [Tooltip("Tras colocar una cinta, deja preparado automáticamente el extremo libre para continuar construyendo.")]
    public bool encadenarCintasAutomaticamente = true;

    [Tooltip("Muestra en consola y Scene la selección automática de recta/izquierda/derecha.")]
    public bool debugCintaAutomatica = false;

    [Header("Cintas automáticas - Zona magnética")]
    [Tooltip("Activa el snap por zona alrededor de la mirilla. No hace falta acertar exactamente al collider del conector.")]
    public bool usarZonaMagneticaCintas = true;

    [Tooltip("Radio de captura alrededor del centro de pantalla. Es una fracción del lado menor de la pantalla. 0.16 equivale a una zona bastante cómoda.")]
    [Range(0.05f, 0.45f)]
    public float radioCapturaMirillaCinta = 0.16f;

    [Tooltip("Radio para mantener un snap ya adquirido. Debe ser mayor que el radio de captura para evitar que el snap parpadee.")]
    [Range(0.08f, 0.60f)]
    public float radioLiberarMirillaCinta = 0.30f;

    [Tooltip("Tiempo que el conector debe permanecer fuera de la zona grande de la mirilla antes de liberar el snap.")]
    [Min(0f)]
    public float tiempoGraciaSoltarSnapCinta = 0.25f;

    [Tooltip("Pequeño tiempo tras liberar un snap antes de poder capturar otro. Evita reenganches instantáneos accidentales.")]
    [Min(0f)]
    public float tiempoBloqueoReengancheCinta = 0.20f;

    // AÑADIDO: Tiempo personalizable de espera
    [Tooltip("Tiempo en segundos que tarda en aparecer el siguiente holograma tras construir")]
    public float tiempoEsperaConstruccion = 0.5f;


    // =====================================================
    // CINTA SPLINE - CONSTRUCCIÓN FLEXIBLE
    // =====================================================

    [Header("Cinta Spline - Experimental")]

    [Tooltip(
        "Sustituye el sistema recta/izquierda/derecha por una cinta flexible " +
        "de dos clics cuando el edificio seleccionado es de tipo Cinta."
    )]
    public bool usarCintaSpline = true;


    [Tooltip("Material usado mientras la cinta se está previsualizando.")]
    public Material materialCintaSplineHolograma;


    [Tooltip(
        "Material de la cinta construida. Si queda vacío se intenta copiar " +
        "el material del primer prefab real de Cinta."
    )]
    public Material materialCintaSplineReal;


    [Header("Cinta Spline - Geometría")]

    [Min(0.20f)]
    public float anchoCintaSpline = 1.60f;

    [Min(0.02f)]
    public float grosorCintaSpline = 0.14f;

    [Range(8, 64)]
    public int segmentosCintaSpline = 28;

    [Range(0.05f, 0.80f)]
    public float fuerzaCurvaCintaSpline = 0.35f;


    [Header("Cinta Spline - Visual estilo prefab")]

    [Tooltip("Ancho de cada rail lateral.")]
    [Min(0.03f)]
    public float anchoRailSpline = 0.16f;

    [Tooltip("Altura de cada rail lateral sobre la banda.")]
    [Min(0.02f)]
    public float altoRailSpline = 0.10f;

    [Tooltip("Separación entre la banda central y cada rail.")]
    [Min(0f)]
    public float separacionRailBandaSpline = 0.035f;

    [Tooltip(
        "Material opcional para los railes. Si queda vacío se intenta copiar " +
        "automáticamente el material de 'Cube (1)' del prefab real."
    )]
    public Material materialRailSplineReal;

    [Tooltip(
        "Material opcional de railes para el holograma. Si queda vacío se usa " +
        "el material real de rail y se tiñe verde/rojo."
    )]
    public Material materialRailSplineHolograma;


    [Header("Cinta Spline - Límites")]

    [Min(0.25f)]
    public float longitudMinimaCintaSpline = 1.00f;

    [Min(1.00f)]
    public float longitudMaximaCintaSpline = 18.00f;

    [Tooltip(
        "Radio mínimo permitido en una curva horizontal. " +
        "Cuanto mayor sea, más suaves tendrán que ser los giros."
    )]
    [Min(0.10f)]
    public float radioMinimoCurvaturaSpline = 1.25f;

    [Tooltip(
        "Pendiente máxima permitida en grados. " +
        "30° es una subida fuerte pero todavía razonable para una cinta."
    )]
    [Range(1f, 80f)]
    public float pendienteMaximaGradosSpline = 30f;

    [Tooltip(
        "La rueda del ratón sube o baja el extremo libre mientras se diseña la cinta."
    )]
    public float velocidadAlturaCintaSpline = 0.35f;


    [Header("Cinta Spline - Transporte")]

    [Min(0f)]
    public float velocidadTransporteSpline = 5f;

    public bool animarTexturaSpline = true;

    public Vector2 direccionTexturaSpline =
        new Vector2(0f, -1f);

    public float multiplicadorTexturaSpline =
        0.20f;


    [Header("Cinta Spline - Conectores")]

    [Min(0.05f)]
    public float tamanoConectorSpline = 0.35f;


    [Header("Cinta Spline - Clearance")]

    [Tooltip(
        "Activa la comprobación de obstáculos a lo largo de toda la spline."
    )]
    public bool usarClearanceCintaSpline = true;

    [Tooltip(
        "Margen extra a cada lado de la cinta para detectar obstáculos."
    )]
    [Min(0f)]
    public float margenLateralClearanceSpline = 0.08f;

    [Tooltip(
        "Altura mínima de la caja de comprobación. Útil para detectar paredes y máquinas."
    )]
    [Min(0.02f)]
    public float alturaClearanceSpline = 0.30f;

    [Tooltip(
        "Separa un poco las cajas del inicio y del final para evitar falsos positivos justo en los conectores."
    )]
    [Range(0f, 0.45f)]
    public float recorteExtremosClearanceSpline = 0.08f;


    [Header("Cinta Spline - Debug")]

    public bool debugCintaSpline = true;


    private bool splineInicioFijado =
        false;

    private Vector3 splinePuntoInicio;

    private Vector3 splineDireccionInicio =
        Vector3.forward;

    private float splineAlturaManual =
        0f;

    private Collider splineConectorInicio =
        null;

    private CintaSplineDinamica splinePreview =
        null;


    // Cinta spline construida actualmente apuntada con el martillo.
    private GameObject cintaSplineApuntadaActual =
        null;

    private CintaSplineDinamica visualSplineApuntadaAnterior =
        null;


    private GameObject hologramaActual;
    public bool modoConstruccion = false;
    private bool estaImantado = false;

    private Collider imanApuntado = null;
    private float rotacionManualOffset = 0f;

    private GameObject edificioApuntado = null;
    private GameObject edificioApuntadoAnterior = null;

    private float cooldownHolograma = 0f;

    private Collider slotBloqueado = null;
    private Vector3? posicionSueloBloqueada = null;
    private float timerBloqueoSlot = 0f;

    // Snap persistente de cintas: una vez encontrado un conector no hace falta
    // seguir apuntándolo mientras elegimos recta / izquierda / derecha.
    private Collider conectorCintaBloqueado = null;
    private bool snapCintaDesdeSalida = true;
    private float tiempoHastaReengancheCinta = 0f;
    private float tiempoFueraRangoSnapCinta = 0f;
    private bool cancelarRotacionCintaEsteFrame = false;

    void Update()
    {
        cancelarRotacionCintaEsteFrame = false;

        if (modoConstruccion && Input.GetKeyDown(teclaCambiarTipo))
        {
            CambiarEdificioActual();
        }

        if (timerBloqueoSlot > 0f)
        {
            timerBloqueoSlot -= Time.deltaTime;

            if (timerBloqueoSlot <= 0f)
            {
                slotBloqueado = null;
                posicionSueloBloqueada = null;
            }
        }

        if (modoConstruccion &&
            EsCintaSplineSeleccionada())
        {
            ActualizarConstruccionCintaSpline();

            return;
        }


        if (modoConstruccion)
        {
            if (cooldownHolograma > 0f)
            {
                cooldownHolograma -= Time.deltaTime;

                if (hologramaActual != null && hologramaActual.activeSelf)
                {
                    hologramaActual.SetActive(false);
                }

                if (cooldownHolograma <= 0f && hologramaActual == null)
                {
                    CrearHolograma();
                }

                return;
            }
            else if (hologramaActual != null)
            {
                // 1. Calculamos la posición y rotación base (Imán o Suelo)
                ManejarPosicionamientoYMagnetismo();

                // 2. Calculamos la rotación manual (Clic derecho)
                ManejarRotacionLibre();

                // 3. APLICAMOS EL OFFSET (Posición y Rotación extra desde el Inspector)
                if (hologramaActual.activeSelf)
                {
                    // ¡SOLUCIONADO! Aplicamos el offset solo cuando NO estamos enganchados
                    if (!estaImantado)
                    {
                        InfoEdificio actual = edificios[indiceEdificioActual];
                        hologramaActual.transform.Rotate(actual.offsetRotacion, Space.Self);
                        hologramaActual.transform.Translate(actual.offsetPosicion, Space.Self);
                    }
                }

                // 4. Validamos colores y colocamos
                ActualizarColorYValidacion();
                ManejarColocacionODestruccion();
            }
        }
    }

    public void SetModoConstruccion(bool activar)
    {
        if (modoConstruccion == activar)
            return;

        modoConstruccion = activar;

        if (modeloMartillo != null)
        {
            modeloMartillo.SetActive(modoConstruccion);
        }

        if (modoConstruccion)
        {
            if (cooldownHolograma <= 0f &&
                !EsCintaSplineSeleccionada())
            {
                CrearHolograma();
            }

            Debug.Log("Modo construcción ACTIVADO");
        }
        else
        {
            LimpiarResaltadoDestruccionSpline();
            CancelarCintaSpline();
            LiberarSnapCinta(false);
            DestruirHolograma();
            Debug.Log("Modo construcción DESACTIVADO");
        }
    }

    void CambiarEdificioActual()
    {
        LimpiarResaltadoDestruccionSpline();
        CancelarCintaSpline();
        LiberarSnapCinta(false);

        indiceEdificioActual =
            (indiceEdificioActual + 1) %
            edificios.Length;

        DestruirHolograma();

        cooldownHolograma = 0f;


        if (!EsCintaSplineSeleccionada())
        {
            CrearHolograma();
        }
    }

    void CrearHolograma()
    {
        if (EsCintaSplineSeleccionada())
        {
            return;
        }


        rotacionManualOffset = 0f;

        InfoEdificio edificioActual = edificios[indiceEdificioActual];
        if (edificioActual.prefabsHologramas.Length > 0)
        {
            if (edificioActual.tipo == TipoEdificio.Cinta)
                indiceVarianteActual = 0;
            else
                indiceVarianteActual = Random.Range(0, edificioActual.prefabsHologramas.Length);

            hologramaActual = Instantiate(edificioActual.prefabsHologramas[indiceVarianteActual]);
        }
        else
        {
            Debug.LogError("No hay hologramas asignados para el edificio: " + edificioActual.nombre);
        }

        slotBloqueado = null;
        posicionSueloBloqueada = null;
        timerBloqueoSlot = 0f;
    }

    void DestruirHolograma()
    {
        if (hologramaActual != null) Destroy(hologramaActual);

        if (edificioApuntadoAnterior != null)
        {
            VariacionAlbedo[] variacionesAnt = edificioApuntadoAnterior.GetComponentsInChildren<VariacionAlbedo>();
            foreach (VariacionAlbedo va in variacionesAnt) va.RestaurarContorno();

            edificioApuntadoAnterior = null;
            edificioApuntado = null;
        }
    }

    void ManejarPosicionamientoYMagnetismo()
    {
        Ray rayo = camaraPrincipal.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        bool apuntandoValido = false;

        InfoEdificio actual = edificios[indiceEdificioActual];
        HologramaColision detector = hologramaActual.GetComponentInChildren<HologramaColision>(true);

        edificioApuntado = null;

        if (actual.tipo == TipoEdificio.Cinta && conectorCintaBloqueado != null)
        {
            if (Input.GetMouseButtonDown(1))
            {
                cancelarRotacionCintaEsteFrame = true;
                LiberarSnapCinta(true);
                hologramaActual.SetActive(false);
                return;
            }

            if (!conectorCintaBloqueado.enabled ||
                !conectorCintaBloqueado.gameObject.activeInHierarchy)
            {
                LiberarSnapCinta(false);
            }
            else if (DebeLiberarSnapCintaPorMirilla())
            {
                tiempoFueraRangoSnapCinta += Time.deltaTime;

                if (tiempoFueraRangoSnapCinta >= tiempoGraciaSoltarSnapCinta)
                {
                    if (debugCintaAutomatica)
                        Debug.Log("Cinta automática -> snap liberado al salir de la zona de mirilla.");

                    LiberarSnapCinta(true);
                }
                else
                {
                    apuntandoValido = ManejarCintaBloqueada(rayo, ref detector);
                    hologramaActual.SetActive(apuntandoValido);
                    return;
                }
            }
            else
            {
                tiempoFueraRangoSnapCinta = 0f;
                apuntandoValido = ManejarCintaBloqueada(rayo, ref detector);
                hologramaActual.SetActive(apuntandoValido);
                return;
            }
        }

        RaycastHit hitConector = default;
        Collider conectorDetectado = null;
        bool chocaConector = false;

        if (actual.tipo == TipoEdificio.Cinta)
        {
            conectorDetectado = BuscarConectorCintaZonaMagnetica();
            chocaConector = conectorDetectado != null;
        }
        else
        {
            chocaConector = Physics.Raycast(
                rayo,
                out hitConector,
                distanciaMaximaConstruccion,
                capaConectores,
                QueryTriggerInteraction.Collide
            );

            if (chocaConector)
                conectorDetectado = hitConector.collider;
        }

        bool conectorValido = false;

        if (chocaConector && conectorDetectado != null)
        {
            if (actual.tipo == TipoEdificio.Rampa &&
                (conectorDetectado.CompareTag("ConectorSalida") ||
                 conectorDetectado.CompareTag("ConectorEntrada")))
            {
                conectorValido = true;
            }
            else if (actual.tipo == TipoEdificio.Cinta &&
                     Time.time >= tiempoHastaReengancheCinta &&
                     (conectorDetectado.CompareTag("ConectorSalida") ||
                      conectorDetectado.CompareTag("ConectorEntrada")))
            {
                conectorValido = true;
            }
            else if (actual.tipo == TipoEdificio.Pared &&
                     (conectorDetectado.CompareTag("RailRampa") ||
                      conectorDetectado.CompareTag("ConectorParedSalida") ||
                      conectorDetectado.CompareTag("ConectorParedEntrada")))
            {
                conectorValido = true;
            }
        }

        if (conectorValido)
        {
            posicionSueloBloqueada = null;

            if (conectorDetectado == slotBloqueado)
            {
                apuntandoValido = false;
                estaImantado = false;
                imanApuntado = null;
                if (detector != null) detector.rampaAIgnorar = null;
            }
            else
            {
                slotBloqueado = null;
                timerBloqueoSlot = 0f;

                if (actual.tipo == TipoEdificio.Cinta)
                {
                    BloquearSnapCinta(conectorDetectado);
                    apuntandoValido = ManejarCintaBloqueada(rayo, ref detector);
                    hologramaActual.SetActive(apuntandoValido);
                    return;
                }

                estaImantado = true;
                apuntandoValido = true;
                imanApuntado = conectorDetectado;

                if (detector != null)
                    detector.rampaAIgnorar = conectorDetectado.transform.root.gameObject;

                if (actual.tipo == TipoEdificio.Rampa)
                {
                    // ¡SOLUCIONADO! Llamamos a AlinearPiezas pasando tanto la posición como la rotación 
                    // para que los encajes de rampa ocurran en un solo paso matemático
                    if (conectorDetectado.CompareTag("ConectorSalida"))
                        AlinearPiezas("PuntoConexion_Entrada", conectorDetectado.transform.position, conectorDetectado.transform.rotation);
                    else if (conectorDetectado.CompareTag("ConectorEntrada"))
                        AlinearPiezas("PuntoConexion_Salida", conectorDetectado.transform.position, conectorDetectado.transform.rotation);
                }
                else if (actual.tipo == TipoEdificio.Pared)
                {
                    if (conectorDetectado.CompareTag("RailRampa"))
                    {
                        hologramaActual.transform.position = conectorDetectado.transform.position;
                        hologramaActual.transform.rotation =
                            conectorDetectado.transform.rotation * Quaternion.Euler(-90f, 0f, 0f);
                    }
                    else
                    {
                        // Mantenemos la lógica de la pared intacta, pasando su propia rotación base
                        if (conectorDetectado.CompareTag("ConectorParedSalida"))
                            AlinearPiezas("PuntoConexionPared_Entrada", conectorDetectado.transform.position, conectorDetectado.transform.root.rotation);
                        else if (conectorDetectado.CompareTag("ConectorParedEntrada"))
                            AlinearPiezas("PuntoConexionPared_Salida", conectorDetectado.transform.position, conectorDetectado.transform.root.rotation);
                    }
                }
            }
        }
        else if (Physics.Raycast(rayo, out hit, distanciaMaximaConstruccion, capaEdificios))
        {
            EdificioConstruido infoEdificio =
                hit.transform.root.GetComponent<EdificioConstruido>();

            slotBloqueado = null;
            posicionSueloBloqueada = null;
            timerBloqueoSlot = 0f;

            if (infoEdificio != null && infoEdificio.tipo == actual.tipo)
            {
                edificioApuntado = hit.transform.root.gameObject;
                estaImantado = false;
                apuntandoValido = false;
                imanApuntado = null;

                if (detector != null)
                    detector.rampaAIgnorar = null;
            }
            else
            {
                apuntandoValido = false;
                imanApuntado = null;

                if (detector != null)
                    detector.rampaAIgnorar = null;
            }
        }
        else if (Physics.Raycast(
                     rayo,
                     out hit,
                     distanciaMaximaConstruccion,
                     capaSuelo,
                     QueryTriggerInteraction.Collide))
        {
            slotBloqueado = null;

            if (posicionSueloBloqueada.HasValue &&
                Vector3.Distance(hit.point, posicionSueloBloqueada.Value) < 2.0f)
            {
                apuntandoValido = false;
                estaImantado = false;
                imanApuntado = null;

                if (detector != null)
                    detector.rampaAIgnorar = null;
            }
            else
            {
                posicionSueloBloqueada = null;
                timerBloqueoSlot = 0f;

                if (actual.tipo == TipoEdificio.Cinta && indiceVarianteActual != 0)
                {
                    CambiarVarianteHolograma(0);
                    detector = hologramaActual.GetComponentInChildren<HologramaColision>(true);
                }

                hologramaActual.transform.position = hit.point;
                estaImantado = false;
                apuntandoValido = true;
                imanApuntado = null;

                if (detector != null)
                    detector.rampaAIgnorar = null;
            }
        }
        else
        {
            slotBloqueado = null;
            posicionSueloBloqueada = null;
            timerBloqueoSlot = 0f;

            apuntandoValido = false;
            imanApuntado = null;

            if (detector != null)
                detector.rampaAIgnorar = null;
        }

        hologramaActual.SetActive(apuntandoValido);
    }

    // =====================================================
    // CINTA SPLINE - INTEGRACIÓN CON MARTILLO
    // =====================================================

    bool EsCintaSplineSeleccionada()
    {
        if (!usarCintaSpline ||
            edificios == null ||
            edificios.Length == 0 ||
            indiceEdificioActual < 0 ||
            indiceEdificioActual >= edificios.Length)
        {
            return false;
        }


        return
            edificios[indiceEdificioActual].tipo ==
            TipoEdificio.Cinta;
    }


    void ActualizarConstruccionCintaSpline()
    {
        // El sistema spline sustituye únicamente el posicionamiento de Cinta.
        // Rampas, paredes y edificios libres siguen usando el sistema anterior.
        if (hologramaActual != null)
        {
            DestruirHolograma();
        }


        // Clic derecho cancela la cinta que estamos dibujando.
        if (Input.GetMouseButtonDown(1))
        {
            CancelarCintaSpline();

            return;
        }


        Ray rayo =
            camaraPrincipal.ScreenPointToRay(
                Input.mousePosition
            );


        // =================================================
        // DESTRUCCIÓN CON MARTILLO
        // =================================================
        //
        // Si la mirilla está sobre una cinta spline YA construida,
        // el clic izquierdo destruye esa cinta en vez de iniciar/terminar
        // una nueva spline. Esto mantiene el comportamiento antiguo:
        // "apuntar a una construcción del mismo tipo -> destruir".
        //
        // También funciona mientras el encadenado automático tiene una
        // nueva spline preparada: al destruir cancelamos primero ese preview.
        ActualizarCintaSplineApuntadaParaDestruccion(
            rayo
        );


        if (cintaSplineApuntadaActual != null &&
            Input.GetMouseButtonDown(0))
        {
            DestruirCintaSplineConMartillo(
                cintaSplineApuntadaActual
            );

            return;
        }


        Collider conectorCercano =
            BuscarConectorCintaZonaMagnetica();


        // =================================================
        // PRIMER CLIC -> FIJAR INICIO
        // =================================================

        if (!splineInicioFijado)
        {
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }


            if (!IntentarObtenerInicioCintaSpline(
                    rayo,
                    conectorCercano,
                    out Vector3 puntoInicio,
                    out Vector3 direccionInicio,
                    out Collider conectorInicio))
            {
                return;
            }


            splinePuntoInicio =
                puntoInicio;

            splineDireccionInicio =
                direccionInicio;

            splineConectorInicio =
                conectorInicio;

            splineAlturaManual =
                0f;

            splineInicioFijado =
                true;


            CrearPreviewCintaSpline();


            if (debugCintaSpline)
            {
                Debug.Log(
                    "[CINTA SPLINE] Inicio fijado" +
                    (
                        splineConectorInicio != null
                        ? " en conector " +
                          splineConectorInicio.name
                        : " en suelo"
                    )
                );
            }


            return;
        }


        // =================================================
        // EXTREMO LIBRE -> SIGUE LA MIRILLA
        // =================================================

        bool finalImantado =
            conectorCercano != null &&
            conectorCercano != splineConectorInicio;


        Vector3 puntoFinal;

        Vector3 direccionFinal;


        if (finalImantado)
        {
            puntoFinal =
                conectorCercano.transform.position;


            direccionFinal =
                ObtenerDireccionSalidaConectorSpline(
                    conectorCercano,
                    false
                );
        }
        else
        {
            float rueda =
                Input.mouseScrollDelta.y;


            if (Mathf.Abs(rueda) >
                0.001f)
            {
                splineAlturaManual +=
                    rueda *
                    velocidadAlturaCintaSpline;
            }


            if (!IntentarObtenerPuntoFinalCintaSpline(
                    rayo,
                    out puntoFinal))
            {
                return;
            }


            puntoFinal +=
                Vector3.up *
                splineAlturaManual;


            Vector3 delta =
                puntoFinal -
                splinePuntoInicio;


            direccionFinal =
                Vector3.ProjectOnPlane(
                    delta,
                    Vector3.up
                );


            if (direccionFinal.sqrMagnitude <
                0.001f)
            {
                direccionFinal =
                    splineDireccionInicio;
            }
            else
            {
                direccionFinal.Normalize();
            }
        }


        float distancia =
            Vector3.Distance(
                splinePuntoInicio,
                puntoFinal
            );


        bool longitudValida =
            distancia >=
                longitudMinimaCintaSpline &&
            distancia <=
                longitudMaximaCintaSpline;


        if (splinePreview == null)
        {
            CrearPreviewCintaSpline();
        }


        bool clearanceValido = true;


        if (splinePreview != null)
        {
            splinePreview.ConfigurarCurva(
                splinePuntoInicio,
                puntoFinal,
                splineDireccionInicio,
                direccionFinal,
                anchoCintaSpline,
                grosorCintaSpline,
                segmentosCintaSpline,
                fuerzaCurvaCintaSpline
            );


            if (usarClearanceCintaSpline &&
                longitudValida)
            {
                GameObject estructuraInicioAIgnorar =
                    splineConectorInicio != null
                    ? splineConectorInicio.transform.root.gameObject
                    : null;


                GameObject estructuraFinalAIgnorar =
                    conectorCercano != null
                    ? conectorCercano.transform.root.gameObject
                    : null;


                clearanceValido =
                    !splinePreview.HayBloqueoClearance(
                        capaEdificios,
                        margenLateralClearanceSpline,
                        alturaClearanceSpline,
                        recorteExtremosClearanceSpline,
                        estructuraInicioAIgnorar,
                        estructuraFinalAIgnorar,
                        debugCintaSpline
                    );
            }


            bool geometriaValida =
                true;


            float radioMinimoDetectado =
                float.PositiveInfinity;

            float pendienteMaximaDetectada =
                0f;


            if (longitudValida)
            {
                geometriaValida =
                    splinePreview.ValidarLimitesGeometricos(
                        radioMinimoCurvaturaSpline,
                        pendienteMaximaGradosSpline,
                        debugCintaSpline,
                        out radioMinimoDetectado,
                        out pendienteMaximaDetectada
                    );
            }


            bool splineValida =
                longitudValida &&
                clearanceValido &&
                geometriaValida;


            splinePreview.EstablecerEstadoValido(
                splineValida,
                colorOutlineValido,
                colorOutlineInvalido
            );


            // =================================================
            // SEGUNDO CLIC -> CONSTRUIR
            // =================================================

            if (Input.GetMouseButtonDown(0) &&
                splineValida)
            {
                ConstruirCintaSpline(
                    puntoFinal,
                    direccionFinal,
                    conectorCercano
                );
            }
        }
    }


    void ActualizarCintaSplineApuntadaParaDestruccion(
        Ray rayo)
    {
        GameObject nuevaCintaApuntada =
            null;

        CintaSplineDinamica nuevoVisual =
            null;


        if (Physics.Raycast(
                rayo,
                out RaycastHit hit,
                distanciaMaximaConstruccion,
                capaEdificios,
                QueryTriggerInteraction.Ignore))
        {
            GameObject raiz =
                hit.transform.root.gameObject;


            EdificioConstruido info =
                raiz.GetComponent<
                    EdificioConstruido
                >();


            CintaSplineDinamica spline =
                raiz.GetComponent<
                    CintaSplineDinamica
                >();


            if (info != null &&
                info.tipo == TipoEdificio.Cinta &&
                spline != null)
            {
                nuevaCintaApuntada =
                    raiz;

                nuevoVisual =
                    spline;
            }
        }


        if (visualSplineApuntadaAnterior != null &&
            visualSplineApuntadaAnterior != nuevoVisual)
        {
            visualSplineApuntadaAnterior
                .RestaurarResaltadoTemporal();
        }


        cintaSplineApuntadaActual =
            nuevaCintaApuntada;


        visualSplineApuntadaAnterior =
            nuevoVisual;


        if (visualSplineApuntadaAnterior != null)
        {
            visualSplineApuntadaAnterior
                .ForzarResaltadoTemporal(
                    colorOutlineDestruccion
                );
        }
    }


    void LimpiarResaltadoDestruccionSpline()
    {
        if (visualSplineApuntadaAnterior != null)
        {
            visualSplineApuntadaAnterior
                .RestaurarResaltadoTemporal();
        }


        visualSplineApuntadaAnterior =
            null;

        cintaSplineApuntadaActual =
            null;
    }


    void DestruirCintaSplineConMartillo(
        GameObject cinta)
    {
        if (cinta == null)
        {
            return;
        }


        // Si el encadenado automático estaba preparando otra cinta,
        // lo cancelamos para no conservar referencias a conectores
        // que podrían desaparecer con esta destrucción.
        CancelarCintaSpline();


        EdificioConstruido info =
            cinta.GetComponent<
                EdificioConstruido
            >();


        if (info != null)
        {
            // -------------------------------------------------
            // 1) LIBERAR EL CONECTOR DEL LADO DE ENTRADA
            // -------------------------------------------------
            //
            // Al crear una spline enganchada, conectorUsado apunta
            // al conector de la estructura anterior que apagamos.
            if (info.conectorUsado != null)
            {
                info.conectorUsado.enabled =
                    true;


                // Mantenemos el pequeño bloqueo temporal que ya utilizaba
                // tu sistema antiguo para evitar reenganchar en el mismo frame.
                slotBloqueado =
                    info.conectorUsado;

                posicionSueloBloqueada =
                    null;
            }
            else
            {
                slotBloqueado =
                    null;

                posicionSueloBloqueada =
                    cinta.transform.position;
            }


            // -------------------------------------------------
            // 2) LIBERAR CONSTRUCCIONES DEPENDIENTES
            // -------------------------------------------------
            foreach (GameObject dependiente
                     in info.edificiosDependientes)
            {
                if (dependiente != null &&
                    dependiente.GetComponent<
                        EfectoBloopDestruccion
                    >() == null)
                {
                    dependiente.AddComponent<
                        EfectoBloopDestruccion
                    >();
                }
            }


            // -------------------------------------------------
            // 3) LIBERAR EL OTRO LADO / SIGUIENTE CINTA
            // -------------------------------------------------
            //
            // Aquí están guardados:
            // - el input de una spline construida después de ésta;
            // - un conector externo al que terminamos enganchados.
            //
            // Por eso, al borrar una cinta intermedia A-B-C,
            // vuelve a quedar disponible la salida de A y la entrada de C.
            foreach (Collider conector
                     in info.conectoresHijosBloqueados)
            {
                if (conector != null)
                {
                    conector.enabled =
                        true;
                }
            }
        }


        // Si por cualquier motivo el snap magnético antiguo tenía
        // bloqueado un conector de esta cinta, lo soltamos.
        if (conectorCintaBloqueado != null &&
            conectorCintaBloqueado.transform.root.gameObject ==
            cinta)
        {
            LiberarSnapCinta(
                false
            );
        }


        // Quitamos el rojo antes de lanzar el efecto de destrucción.
        LimpiarResaltadoDestruccionSpline();


        if (cinta.GetComponent<
                EfectoBloopDestruccion
            >() == null)
        {
            cinta.AddComponent<
                EfectoBloopDestruccion
            >();
        }


        timerBloqueoSlot =
            1.0f;


        if (debugCintaSpline)
        {
            Debug.Log(
                "[CINTA SPLINE] Destruida con martillo. " +
                "Conectores liberados."
            );
        }
    }


    bool IntentarObtenerInicioCintaSpline(
        Ray rayo,
        Collider conector,
        out Vector3 punto,
        out Vector3 direccion,
        out Collider conectorUsado)
    {
        punto =
            Vector3.zero;

        direccion =
            Vector3.forward;

        conectorUsado =
            null;


        if (conector != null)
        {
            punto =
                conector.transform.position;

            direccion =
                ObtenerDireccionSalidaConectorSpline(
                    conector,
                    true
                );

            conectorUsado =
                conector;

            return true;
        }


        if (Physics.Raycast(
                rayo,
                out RaycastHit hit,
                distanciaMaximaConstruccion,
                capaSuelo,
                QueryTriggerInteraction.Ignore))
        {
            punto =
                hit.point;


            direccion =
                Vector3.ProjectOnPlane(
                    camaraPrincipal.transform.forward,
                    Vector3.up
                );


            if (direccion.sqrMagnitude <
                0.001f)
            {
                direccion =
                    Vector3.forward;
            }


            direccion.Normalize();


            return true;
        }


        return false;
    }


    bool IntentarObtenerPuntoFinalCintaSpline(
        Ray rayo,
        out Vector3 punto)
    {
        if (Physics.Raycast(
                rayo,
                out RaycastHit hit,
                distanciaMaximaConstruccion,
                capaSuelo,
                QueryTriggerInteraction.Ignore))
        {
            punto =
                hit.point;

            return true;
        }


        Plane plano =
            new Plane(
                Vector3.up,
                splinePuntoInicio
            );


        if (plano.Raycast(
                rayo,
                out float distanciaPlano))
        {
            punto =
                rayo.GetPoint(
                    distanciaPlano
                );


            return
                distanciaPlano <=
                distanciaMaximaConstruccion *
                2f;
        }


        punto =
            Vector3.zero;

        return false;
    }


    Vector3 ObtenerDireccionSalidaConectorSpline(
        Collider conector,
        bool esInicio)
    {
        if (conector == null)
        {
            return Vector3.forward;
        }


        Vector3 direccion;


        if (esInicio)
        {
            direccion =
                conector.CompareTag(
                    "ConectorEntrada"
                )
                ? -conector.transform.forward
                : conector.transform.forward;
        }
        else
        {
            // Para llegar al conector hacemos la operación inversa:
            // la tangente debe entrar en él, no salir de él.
            direccion =
                conector.CompareTag(
                    "ConectorEntrada"
                )
                ? conector.transform.forward
                : -conector.transform.forward;
        }


        direccion =
            Vector3.ProjectOnPlane(
                direccion,
                Vector3.up
            );


        if (direccion.sqrMagnitude <
            0.001f)
        {
            direccion =
                Vector3.forward;
        }


        return direccion.normalized;
    }


    void CrearPreviewCintaSpline()
    {
        if (splinePreview != null)
        {
            return;
        }


        GameObject preview =
            new GameObject(
                "CintaSpline_Holograma"
            );


        splinePreview =
            preview.AddComponent<
                CintaSplineDinamica
            >();


        splinePreview.ConfigurarMateriales(
            ObtenerMaterialBandaCintaSpline(true),
            ObtenerMaterialRailCintaSpline(true)
        );


        splinePreview.ConfigurarVisual(
            anchoRailSpline,
            altoRailSpline,
            separacionRailBandaSpline
        );


        splinePreview.ConfigurarComoPreview();
    }


    Material ObtenerMaterialBandaCintaSpline(
        bool holograma)
    {
        Material materialAsignado =
            holograma
            ? materialCintaSplineHolograma
            : materialCintaSplineReal;


        if (materialAsignado != null)
        {
            return materialAsignado;
        }


        InfoEdificio actual =
            edificios[indiceEdificioActual];


        // Para la banda real buscamos primero el hijo "Cube",
        // que es la pieza que en tu prefab lleva CintaTransportadora.
        if (actual.prefabsReales != null &&
            actual.prefabsReales.Length > 0 &&
            actual.prefabsReales[0] != null)
        {
            Transform[] hijos =
                actual.prefabsReales[0]
                    .GetComponentsInChildren<Transform>(true);


            foreach (Transform hijo in hijos)
            {
                if (hijo.name == "Cube")
                {
                    Renderer renderer =
                        hijo.GetComponent<Renderer>();


                    if (renderer != null &&
                        renderer.sharedMaterial != null)
                    {
                        return renderer.sharedMaterial;
                    }
                }
            }
        }


        // Fallback al primer renderer disponible.
        GameObject[] prefabs =
            holograma
            ? actual.prefabsHologramas
            : actual.prefabsReales;


        if (prefabs != null &&
            prefabs.Length > 0 &&
            prefabs[0] != null)
        {
            Renderer renderer =
                prefabs[0]
                    .GetComponentInChildren<Renderer>(true);


            if (renderer != null)
            {
                return renderer.sharedMaterial;
            }
        }


        return null;
    }


    Material ObtenerMaterialRailCintaSpline(
        bool holograma)
    {
        Material materialAsignado =
            holograma
            ? materialRailSplineHolograma
            : materialRailSplineReal;


        if (materialAsignado != null)
        {
            return materialAsignado;
        }


        InfoEdificio actual =
            edificios[indiceEdificioActual];


        // En tu prefab Cube (1) es la estructura/laterales.
        if (actual.prefabsReales != null &&
            actual.prefabsReales.Length > 0 &&
            actual.prefabsReales[0] != null)
        {
            Transform[] hijos =
                actual.prefabsReales[0]
                    .GetComponentsInChildren<Transform>(true);


            foreach (Transform hijo in hijos)
            {
                if (hijo.name == "Cube (1)")
                {
                    Renderer renderer =
                        hijo.GetComponent<Renderer>();


                    if (renderer != null &&
                        renderer.sharedMaterial != null)
                    {
                        return renderer.sharedMaterial;
                    }
                }
            }
        }


        // Si no encontramos Cube (1), usamos el material de banda.
        return ObtenerMaterialBandaCintaSpline(
            holograma
        );
    }


    void ConstruirCintaSpline(
        Vector3 puntoFinal,
        Vector3 direccionFinal,
        Collider conectorFinal)
    {
        GameObject nuevaCinta =
            new GameObject(
                "CintaSpline"
            );


        // Copiamos la layer principal de la cinta real antigua.
        InfoEdificio actual =
            edificios[indiceEdificioActual];


        if (actual.prefabsReales != null &&
            actual.prefabsReales.Length > 0 &&
            actual.prefabsReales[0] != null)
        {
            nuevaCinta.layer =
                actual.prefabsReales[0].layer;
        }


        CintaSplineDinamica spline =
            nuevaCinta.AddComponent<
                CintaSplineDinamica
            >();


        spline.ConfigurarMateriales(
            ObtenerMaterialBandaCintaSpline(false),
            ObtenerMaterialRailCintaSpline(false)
        );


        spline.ConfigurarVisual(
            anchoRailSpline,
            altoRailSpline,
            separacionRailBandaSpline
        );


        spline.ConfigurarCurva(
            splinePuntoInicio,
            puntoFinal,
            splineDireccionInicio,
            direccionFinal,
            anchoCintaSpline,
            grosorCintaSpline,
            segmentosCintaSpline,
            fuerzaCurvaCintaSpline
        );


        int layerConector =
            ObtenerPrimerLayerDeMask(
                capaConectores
            );


        spline.ConstruirFisicaYConectores(
            velocidadTransporteSpline,
            animarTexturaSpline,
            direccionTexturaSpline,
            multiplicadorTexturaSpline,
            layerConector,
            tamanoConectorSpline
        );


        nuevaCinta.AddComponent<
            EfectoBloop
        >();


        EdificioConstruido id =
            nuevaCinta.AddComponent<
                EdificioConstruido
            >();


        id.tipo =
            TipoEdificio.Cinta;


        // =================================================
        // QUEMAR CONECTOR DE INICIO SI ESTABA ENGANCHADO
        // =================================================

        Collider conectorEntradaNuevo =
            spline.ConectorEntrada;


        Collider conectorSalidaNuevo =
            spline.ConectorSalida;


        if (splineConectorInicio != null)
        {
            id.conectorUsado =
                splineConectorInicio;


            splineConectorInicio.enabled =
                false;


            if (conectorEntradaNuevo != null)
            {
                conectorEntradaNuevo.enabled =
                    false;
            }


            EdificioConstruido estructuraPadre =
                splineConectorInicio
                    .transform
                    .root
                    .GetComponent<
                        EdificioConstruido
                    >();


            if (estructuraPadre != null &&
                conectorEntradaNuevo != null)
            {
                estructuraPadre
                    .conectoresHijosBloqueados
                    .Add(
                        conectorEntradaNuevo
                    );
            }
        }


        // De momento permitimos snap visual al conector final y lo quemamos
        // también si existe. Esto deja la cinta físicamente cerrada por ambos lados.
        if (conectorFinal != null &&
            conectorFinal != splineConectorInicio)
        {
            conectorFinal.enabled =
                false;


            // Guardamos también el conector externo en ESTA cinta.
            // Así, si destruimos la spline, el sistema existente lo
            // vuelve a habilitar automáticamente.
            id.conectoresHijosBloqueados.Add(
                conectorFinal
            );


            if (conectorSalidaNuevo != null)
            {
                conectorSalidaNuevo.enabled =
                    false;
            }


            EdificioConstruido estructuraFinal =
                conectorFinal
                    .transform
                    .root
                    .GetComponent<
                        EdificioConstruido
                    >();


            if (estructuraFinal != null &&
                conectorSalidaNuevo != null)
            {
                estructuraFinal
                    .conectoresHijosBloqueados
                    .Add(
                        conectorSalidaNuevo
                    );
            }
        }


        if (debugCintaSpline)
        {
            Debug.Log(
                "[CINTA SPLINE] Construida | Longitud: " +
                Vector3.Distance(
                    splinePuntoInicio,
                    puntoFinal
                ).ToString("0.00") +
                " m"
            );
        }


        // =================================================
        // ENCADENAR AUTOMÁTICAMENTE
        // =================================================

        DestruirPreviewCintaSpline();


        if (encadenarCintasAutomaticamente &&
            conectorSalidaNuevo != null &&
            conectorFinal == null)
        {
            splineInicioFijado =
                true;


            splinePuntoInicio =
                conectorSalidaNuevo
                    .transform
                    .position;


            splineDireccionInicio =
                conectorSalidaNuevo
                    .transform
                    .forward;


            splineConectorInicio =
                conectorSalidaNuevo;


            splineAlturaManual =
                0f;


            CrearPreviewCintaSpline();
        }
        else
        {
            ResetearEstadoCintaSpline();
        }
    }


    void CancelarCintaSpline()
    {
        DestruirPreviewCintaSpline();
        ResetearEstadoCintaSpline();
    }


    void DestruirPreviewCintaSpline()
    {
        if (splinePreview != null)
        {
            Destroy(
                splinePreview.gameObject
            );


            splinePreview =
                null;
        }
    }


    void ResetearEstadoCintaSpline()
    {
        splineInicioFijado =
            false;

        splineConectorInicio =
            null;

        splineAlturaManual =
            0f;
    }


    int ObtenerPrimerLayerDeMask(
        LayerMask mascara)
    {
        int valor =
            mascara.value;


        for (int i = 0;
             i < 32;
             i++)
        {
            if ((valor & (1 << i)) != 0)
            {
                return i;
            }
        }


        return 0;
    }


    void BloquearSnapCinta(Collider conector)
    {
        if (conector == null)
            return;

        conectorCintaBloqueado = conector;
        snapCintaDesdeSalida = conector.CompareTag("ConectorSalida");
        tiempoFueraRangoSnapCinta = 0f;

        estaImantado = true;
        imanApuntado = conector;

        if (debugCintaAutomatica)
        {
            Debug.Log(
                "Snap de cinta bloqueado en: " +
                conector.name +
                " | " +
                (snapCintaDesdeSalida ? "SALIDA" : "ENTRADA")
            );
        }
    }

    void LiberarSnapCinta(bool bloquearReenganche)
    {
        conectorCintaBloqueado = null;
        tiempoFueraRangoSnapCinta = 0f;

        if (imanApuntado != null &&
            (imanApuntado.CompareTag("ConectorSalida") ||
             imanApuntado.CompareTag("ConectorEntrada")))
        {
            imanApuntado = null;
        }

        estaImantado = false;

        if (hologramaActual != null)
        {
            ValidadorClearanceConstruccion validadorClearance =
                hologramaActual.GetComponentInChildren<ValidadorClearanceConstruccion>(true);

            if (validadorClearance != null)
                validadorClearance.estructuraAIgnorar = null;
        }

        if (bloquearReenganche)
            tiempoHastaReengancheCinta = Time.time + tiempoBloqueoReengancheCinta;
    }

    bool DebeLiberarSnapCintaPorMirilla()
    {
        if (!usarZonaMagneticaCintas ||
            conectorCintaBloqueado == null ||
            camaraPrincipal == null)
        {
            return false;
        }

        Vector3 pantalla = camaraPrincipal.WorldToScreenPoint(
            conectorCintaBloqueado.bounds.center
        );

        if (pantalla.z <= 0f)
            return true;

        Vector2 centroPantalla = new Vector2(
            Screen.width * 0.5f,
            Screen.height * 0.5f
        );

        float radioPixels =
            Mathf.Min(Screen.width, Screen.height) * radioLiberarMirillaCinta;

        float distanciaPantalla = Vector2.Distance(
            new Vector2(pantalla.x, pantalla.y),
            centroPantalla
        );

        if (debugCintaAutomatica)
        {
            Debug.DrawRay(
                conectorCintaBloqueado.bounds.center,
                camaraPrincipal.transform.up * 0.35f,
                distanciaPantalla <= radioPixels ? Color.green : Color.red
            );
        }

        return distanciaPantalla > radioPixels;
    }

    Collider BuscarConectorCintaZonaMagnetica()
    {
        if (camaraPrincipal == null || Time.time < tiempoHastaReengancheCinta)
            return null;

        if (!usarZonaMagneticaCintas)
        {
            Ray rayoCentro = camaraPrincipal.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );

            if (Physics.Raycast(
                    rayoCentro,
                    out RaycastHit hit,
                    distanciaMaximaConstruccion,
                    capaConectores,
                    QueryTriggerInteraction.Collide) &&
                EsConectorCintaReenganchable(hit.collider))
            {
                return hit.collider;
            }

            return null;
        }

        Collider[] candidatos = Physics.OverlapSphere(
            camaraPrincipal.transform.position,
            distanciaMaximaConstruccion,
            capaConectores,
            QueryTriggerInteraction.Collide
        );

        Vector2 centroPantalla = new Vector2(
            Screen.width * 0.5f,
            Screen.height * 0.5f
        );

        float radioPixels =
            Mathf.Min(Screen.width, Screen.height) * radioCapturaMirillaCinta;

        Collider mejor = null;
        float mejorPuntuacion = float.MaxValue;

        foreach (Collider candidato in candidatos)
        {
            if (!EsConectorCintaReenganchable(candidato))
                continue;

            Vector3 pantalla = camaraPrincipal.WorldToScreenPoint(
                candidato.bounds.center
            );

            if (pantalla.z <= 0f)
                continue;

            Vector2 puntoPantalla = new Vector2(pantalla.x, pantalla.y);
            float distanciaPantalla = Vector2.Distance(
                puntoPantalla,
                centroPantalla
            );

            if (distanciaPantalla > radioPixels)
                continue;

            float distanciaMundo = Vector3.Distance(
                camaraPrincipal.transform.position,
                candidato.bounds.center
            );

            float puntuacion =
                distanciaPantalla +
                (distanciaMundo / Mathf.Max(0.01f, distanciaMaximaConstruccion)) * 20f;

            if (puntuacion < mejorPuntuacion)
            {
                mejorPuntuacion = puntuacion;
                mejor = candidato;
            }
        }

        if (debugCintaAutomatica && mejor != null)
        {
            Debug.DrawLine(
                camaraPrincipal.transform.position,
                mejor.bounds.center,
                Color.cyan
            );
        }

        return mejor;
    }

    bool EsConectorCintaReenganchable(Collider conector)
    {
        if (conector == null ||
            !conector.enabled ||
            !conector.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (!conector.CompareTag("ConectorSalida") &&
            !conector.CompareTag("ConectorEntrada"))
        {
            return false;
        }

        if (Time.time < tiempoHastaReengancheCinta)
            return false;

        if (conector == slotBloqueado)
            return false;

        if (hologramaActual != null &&
            conector.transform.root == hologramaActual.transform.root)
        {
            return false;
        }

        return true;
    }

    bool ManejarCintaBloqueada(Ray rayo, ref HologramaColision detector)
    {
        if (conectorCintaBloqueado == null)
            return false;

        Transform conector = conectorCintaBloqueado.transform;

        estaImantado = true;
        imanApuntado = conectorCintaBloqueado;

        if (detector != null)
            detector.rampaAIgnorar = conector.root.gameObject;

        ValidadorClearanceConstruccion validadorClearance =
            hologramaActual.GetComponentInChildren<ValidadorClearanceConstruccion>(true);

        if (validadorClearance != null)
            validadorClearance.estructuraAIgnorar = conector.root.gameObject;

        int nuevaVariante = DeterminarVarianteCintaDesdeRaton(
            rayo,
            conector,
            snapCintaDesdeSalida
        );

        if (nuevaVariante != indiceVarianteActual)
        {
            CambiarVarianteHolograma(nuevaVariante);

            detector =
                hologramaActual.GetComponentInChildren<HologramaColision>(true);

            if (detector != null)
                detector.rampaAIgnorar = conector.root.gameObject;

            validadorClearance =
                hologramaActual.GetComponentInChildren<ValidadorClearanceConstruccion>(true);

            if (validadorClearance != null)
                validadorClearance.estructuraAIgnorar = conector.root.gameObject;
        }

        string nombreConectorMio = snapCintaDesdeSalida
            ? "PuntoConexion_Entrada"
            : "PuntoConexion_Salida";

        AlinearConectorCinta(nombreConectorMio, conector);

        return true;
    }

    int DeterminarVarianteCintaDesdeRaton(
        Ray rayo,
        Transform conector,
        bool conectandoDesdeSalida)
    {
        InfoEdificio actual = edificios[indiceEdificioActual];

        if (actual.prefabsHologramas == null ||
            actual.prefabsHologramas.Length < 3 ||
            conector == null)
        {
            return 0;
        }

        Vector3 puntoRaton;

        if (Physics.Raycast(
                rayo,
                out RaycastHit hitSuelo,
                distanciaMaximaConstruccion * 2f,
                capaSuelo,
                QueryTriggerInteraction.Ignore))
        {
            puntoRaton = hitSuelo.point;
        }
        else
        {
            Plane planoSeleccion = new Plane(Vector3.up, conector.position);

            if (!planoSeleccion.Raycast(rayo, out float distanciaPlano))
                return indiceVarianteActual;

            puntoRaton = rayo.GetPoint(distanciaPlano);
        }

        Vector3 direccionDeseada = puntoRaton - conector.position;
        direccionDeseada.y = 0f;

        if (direccionDeseada.magnitude < distanciaMinimaDireccionCinta)
            return indiceVarianteActual;

        direccionDeseada.Normalize();

        Vector3 frenteConstruccion = conectandoDesdeSalida
            ? conector.forward
            : -conector.forward;

        frenteConstruccion.y = 0f;

        if (frenteConstruccion.sqrMagnitude < 0.001f)
            return 0;

        frenteConstruccion.Normalize();

        float angulo = Vector3.SignedAngle(
            frenteConstruccion,
            direccionDeseada,
            Vector3.up
        );

        int indiceIzquierda = conectandoDesdeSalida ? 1 : 2;
        int indiceDerecha = conectandoDesdeSalida ? 2 : 1;

        float umbralEntrada = anguloActivarCurvaCinta;
        float umbralSalida = Mathf.Max(
            0f,
            anguloActivarCurvaCinta - histeresisCinta
        );

        int varianteDeseada = indiceVarianteActual;

        if (indiceVarianteActual == indiceIzquierda)
        {
            if (angulo > -umbralSalida)
                varianteDeseada = 0;
            else
                varianteDeseada = indiceIzquierda;
        }
        else if (indiceVarianteActual == indiceDerecha)
        {
            if (angulo < umbralSalida)
                varianteDeseada = 0;
            else
                varianteDeseada = indiceDerecha;
        }
        else
        {
            if (angulo <= -umbralEntrada)
                varianteDeseada = indiceIzquierda;
            else if (angulo >= umbralEntrada)
                varianteDeseada = indiceDerecha;
            else
                varianteDeseada = 0;
        }

        if (debugCintaAutomatica)
        {
            Debug.DrawLine(conector.position, puntoRaton, Color.yellow);
            Debug.DrawRay(
                conector.position,
                frenteConstruccion * 2f,
                Color.green
            );
        }

        return varianteDeseada;
    }

    void CambiarVarianteHolograma(int nuevoIndice)
    {
        InfoEdificio actual = edificios[indiceEdificioActual];

        if (actual.prefabsHologramas == null ||
            nuevoIndice < 0 ||
            nuevoIndice >= actual.prefabsHologramas.Length)
        {
            return;
        }

        if (nuevoIndice == indiceVarianteActual && hologramaActual != null)
            return;

        Vector3 posicion = Vector3.zero;
        Quaternion rotacion = Quaternion.identity;
        bool estabaActivo = true;

        if (hologramaActual != null)
        {
            posicion = hologramaActual.transform.position;
            rotacion = hologramaActual.transform.rotation;
            estabaActivo = hologramaActual.activeSelf;
            Destroy(hologramaActual);
        }

        indiceVarianteActual = nuevoIndice;

        hologramaActual = Instantiate(
            actual.prefabsHologramas[indiceVarianteActual],
            posicion,
            rotacion
        );

        hologramaActual.SetActive(estabaActivo);

        if (debugCintaAutomatica)
        {
            string nombreVariante = indiceVarianteActual switch
            {
                0 => "RECTA",
                1 => "IZQUIERDA",
                2 => "DERECHA",
                _ => "DESCONOCIDA"
            };

            Debug.Log("Cinta automática -> " + nombreVariante);
        }
    }

    // ¡SOLUCIONADO! Método optimizado con matrices matemáticas para cintas
    void AlinearConectorCinta(string nombreConectorMio, Transform conectorDestino)
    {
        if (hologramaActual == null || conectorDestino == null)
            return;

        Transform miConector = null;
        Transform[] todosLosHijos = hologramaActual.GetComponentsInChildren<Transform>(true);

        foreach (Transform hijo in todosLosHijos)
        {
            if (hijo.name == nombreConectorMio)
            {
                miConector = hijo;
                break;
            }
        }

        if (miConector == null)
        {
            Debug.LogWarning("No se encontró '" + nombreConectorMio + "' dentro del holograma de cinta.");
            return;
        }

        Vector3 posicionLocalConector = hologramaActual.transform.InverseTransformPoint(miConector.position);

        Quaternion diferenciaRotacion = conectorDestino.rotation * Quaternion.Inverse(miConector.rotation);
        hologramaActual.transform.rotation = diferenciaRotacion * hologramaActual.transform.rotation;

        Vector3 nuevaPosicionConector = hologramaActual.transform.TransformPoint(posicionLocalConector);

        Vector3 diferenciaPosicion = nuevaPosicionConector - hologramaActual.transform.position;
        hologramaActual.transform.position = conectorDestino.position - diferenciaPosicion;
    }

    Collider BuscarColliderConector(GameObject estructura, string nombreConector)
    {
        if (estructura == null)
            return null;

        Transform[] hijos = estructura.GetComponentsInChildren<Transform>(true);

        foreach (Transform hijo in hijos)
        {
            if (hijo.name == nombreConector)
                return hijo.GetComponent<Collider>();
        }

        return null;
    }

    // ¡SOLUCIONADO! Método optimizado con matrices para Rampas y Paredes
    void AlinearPiezas(string nombreConectorMio, Vector3 posicionDestino, Quaternion rotacionDestino)
    {
        Transform miConector = null;
        Transform[] todosLosHijos = hologramaActual.GetComponentsInChildren<Transform>(true);

        foreach (Transform hijo in todosLosHijos)
        {
            if (hijo.name == nombreConectorMio)
            {
                miConector = hijo;
                break;
            }
        }

        if (miConector != null)
        {
            Vector3 posicionLocalConector = hologramaActual.transform.InverseTransformPoint(miConector.position);

            // Establecemos primero la rotación deseada
            hologramaActual.transform.rotation = rotacionDestino;

            // Calculamos exactamente la nueva ubicación del hijo basándonos en la rotación que acabamos de aplicar
            Vector3 posicionGlobalActualizada = hologramaActual.transform.TransformPoint(posicionLocalConector);

            Vector3 diferencia = posicionGlobalActualizada - hologramaActual.transform.position;
            hologramaActual.transform.position = posicionDestino - diferencia;
        }
    }

    void ManejarRotacionLibre()
    {
        if (cancelarRotacionCintaEsteFrame)
            return;

        if (!estaImantado && edificioApuntado == null)
        {
            if (Input.GetMouseButton(1))
                rotacionManualOffset += Input.GetAxis("Mouse X") * velocidadRotacion;

            Vector3 direccionCamara = camaraPrincipal.transform.forward;
            direccionCamara.y = 0f;

            if (direccionCamara.sqrMagnitude > 0.001f)
            {
                Quaternion rotacionBase =
                    Quaternion.LookRotation(direccionCamara.normalized);

                if (edificios[indiceEdificioActual].tipo == TipoEdificio.Pared)
                {
                    hologramaActual.transform.rotation =
                        rotacionBase *
                        Quaternion.Euler(-90f, rotacionManualOffset, 0f);
                }
                else
                {
                    hologramaActual.transform.rotation =
                        rotacionBase *
                        Quaternion.Euler(0f, rotacionManualOffset, 0f);
                }
            }
        }
    }

    bool PuedeColocarActual()
    {
        if (hologramaActual == null)
            return false;

        HologramaColision detector =
            hologramaActual.GetComponentInChildren<HologramaColision>(true);

        if (detector != null && detector.HayColision)
            return false;

        ValidadorClearanceConstruccion validadorClearance =
            hologramaActual.GetComponentInChildren<ValidadorClearanceConstruccion>(true);

        if (validadorClearance != null)
        {
            validadorClearance.ComprobarClearance();

            if (validadorClearance.HayBloqueo)
                return false;
        }

        return true;
    }

    void ActualizarColorYValidacion()
    {
        if (edificioApuntadoAnterior != null && edificioApuntadoAnterior != edificioApuntado)
        {
            VariacionAlbedo[] variacionesAnt = edificioApuntadoAnterior.GetComponentsInChildren<VariacionAlbedo>();
            foreach (VariacionAlbedo va in variacionesAnt) va.RestaurarContorno();
        }

        if (edificioApuntado != null)
        {
            if (edificioApuntado != edificioApuntadoAnterior)
            {
                VariacionAlbedo[] variacionesAct = edificioApuntado.GetComponentsInChildren<VariacionAlbedo>();
                foreach (VariacionAlbedo va in variacionesAct) va.ForzarContorno(colorOutlineDestruccion, grosorOutlineDestruccion);
            }
        }
        else if (hologramaActual.activeSelf)
        {
            Color colorBaseEstado = PuedeColocarActual() ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
            Color colorOutlineActual = PuedeColocarActual() ? colorOutlineValido : colorOutlineInvalido;
            float grosorOutlineActual = PuedeColocarActual() ? grosorOutlineValido : grosorOutlineInvalido;

            Renderer[] renderizadores = hologramaActual.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderizadores)
            {
                foreach (Material mat in r.materials)
                {
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colorBaseEstado);
                    if (mat.HasProperty("_Color")) mat.color = colorBaseEstado;
                }
            }

            VariacionAlbedo[] variaciones = hologramaActual.GetComponentsInChildren<VariacionAlbedo>();
            foreach (VariacionAlbedo va in variaciones) va.ForzarContorno(colorOutlineActual, grosorOutlineActual);
        }

        edificioApuntadoAnterior = edificioApuntado;
    }

    void ManejarColocacionODestruccion()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (edificioApuntado != null)
            {
                EdificioConstruido infoEdificio =
                    edificioApuntado.GetComponent<EdificioConstruido>();

                if (infoEdificio != null)
                {
                    if (infoEdificio.conectorUsado != null)
                    {
                        infoEdificio.conectorUsado.enabled = true;
                        slotBloqueado = infoEdificio.conectorUsado;
                        posicionSueloBloqueada = null;
                    }
                    else
                    {
                        slotBloqueado = null;
                        posicionSueloBloqueada =
                            edificioApuntado.transform.position;
                    }

                    foreach (GameObject dep in infoEdificio.edificiosDependientes)
                    {
                        if (dep != null)
                            dep.AddComponent<EfectoBloopDestruccion>();
                    }

                    foreach (Collider colHijo in infoEdificio.conectoresHijosBloqueados)
                    {
                        if (colHijo != null)
                            colHijo.enabled = true;
                    }
                }

                if (conectorCintaBloqueado != null &&
                    conectorCintaBloqueado.transform.root.gameObject == edificioApuntado)
                {
                    LiberarSnapCinta(false);
                }

                edificioApuntado.AddComponent<EfectoBloopDestruccion>();

                edificioApuntadoAnterior = null;
                edificioApuntado = null;

                cooldownHolograma = tiempoEsperaConstruccion;
                timerBloqueoSlot = 1.0f;

                DestruirHolograma();
                return;
            }

            if (PuedeColocarActual() && hologramaActual.activeSelf)
            {
                InfoEdificio actual = edificios[indiceEdificioActual];

                bool estabaConectada =
                    estaImantado && imanApuntado != null;

                bool construyendoDesdeSalida =
                    snapCintaDesdeSalida;

                Collider conectorAnterior =
                    imanApuntado;

                GameObject nuevaEstructura = Instantiate(
                    actual.prefabsReales[indiceVarianteActual],
                    hologramaActual.transform.position,
                    hologramaActual.transform.rotation
                );

                nuevaEstructura.AddComponent<EfectoBloop>();

                EdificioConstruido id =
                    nuevaEstructura.AddComponent<EdificioConstruido>();

                id.tipo = actual.tipo;

                if (estabaConectada && conectorAnterior != null)
                {
                    id.conectorUsado = conectorAnterior;
                    conectorAnterior.enabled = false;

                    if (actual.tipo == TipoEdificio.Rampa ||
                        actual.tipo == TipoEdificio.Cinta)
                    {
                        string nombreConectorAQuemar =
                            conectorAnterior.CompareTag("ConectorSalida")
                                ? "PuntoConexion_Entrada"
                                : "PuntoConexion_Salida";

                        Collider miConectorApagado =
                            BuscarColliderConector(
                                nuevaEstructura,
                                nombreConectorAQuemar
                            );

                        if (miConectorApagado != null)
                            miConectorApagado.enabled = false;

                        EdificioConstruido estructuraPadre =
                            conectorAnterior.transform.root
                                .GetComponent<EdificioConstruido>();

                        if (estructuraPadre != null &&
                            miConectorApagado != null)
                        {
                            estructuraPadre.conectoresHijosBloqueados
                                .Add(miConectorApagado);
                        }
                    }
                    else if (actual.tipo == TipoEdificio.Pared)
                    {
                        if (conectorAnterior.CompareTag("RailRampa"))
                        {
                            EdificioConstruido rampaPadre =
                                conectorAnterior.transform.root
                                    .GetComponent<EdificioConstruido>();

                            if (rampaPadre != null)
                                rampaPadre.edificiosDependientes
                                    .Add(nuevaEstructura);

                            Collider[] collidersPared =
                                nuevaEstructura.GetComponentsInChildren<Collider>();

                            foreach (Collider col in collidersPared)
                            {
                                if (col.CompareTag("ConectorParedSalida") ||
                                    col.CompareTag("ConectorParedEntrada"))
                                {
                                    col.enabled = false;
                                }
                            }
                        }
                        else
                        {
                            string nombreConectorAQuemar =
                                conectorAnterior.CompareTag("ConectorParedSalida")
                                    ? "PuntoConexionPared_Entrada"
                                    : "PuntoConexionPared_Salida";

                            Collider miConectorApagado =
                                BuscarColliderConector(
                                    nuevaEstructura,
                                    nombreConectorAQuemar
                                );

                            if (miConectorApagado != null)
                                miConectorApagado.enabled = false;

                            EdificioConstruido paredPadre =
                                conectorAnterior.transform.root
                                    .GetComponent<EdificioConstruido>();

                            if (paredPadre != null &&
                                miConectorApagado != null)
                            {
                                paredPadre.conectoresHijosBloqueados
                                    .Add(miConectorApagado);
                            }
                        }
                    }
                }

                if (actual.tipo == TipoEdificio.Cinta &&
                    encadenarCintasAutomaticamente)
                {
                    string siguienteConector;

                    if (estabaConectada)
                    {
                        siguienteConector = construyendoDesdeSalida
                            ? "PuntoConexion_Salida"
                            : "PuntoConexion_Entrada";
                    }
                    else
                    {
                        siguienteConector = "PuntoConexion_Salida";
                    }

                    Collider conectorSiguiente =
                        BuscarColliderConector(
                            nuevaEstructura,
                            siguienteConector
                        );

                    if (conectorSiguiente != null &&
                        conectorSiguiente.enabled)
                    {
                        conectorCintaBloqueado = conectorSiguiente;
                        snapCintaDesdeSalida =
                            conectorSiguiente.CompareTag("ConectorSalida");
                        tiempoFueraRangoSnapCinta = 0f;

                        estaImantado = true;
                        imanApuntado = conectorSiguiente;
                    }
                    else
                    {
                        LiberarSnapCinta(false);
                    }
                }
                else if (actual.tipo == TipoEdificio.Cinta)
                {
                    LiberarSnapCinta(false);
                }

                cooldownHolograma = tiempoEsperaConstruccion;

                DestruirHolograma();
            }
        }
    }
}

public class EdificioConstruido : MonoBehaviour
{
    public SistemaConstruccion.TipoEdificio tipo;
    public Collider conectorUsado;
    public List<GameObject> edificiosDependientes = new List<GameObject>();

    [Tooltip("Conectores de otras rampas/paredes acopladas a nosotros, que debemos re-encender si morimos")]
    public List<Collider> conectoresHijosBloqueados = new List<Collider>();
}