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

        if (modoConstruccion)
        {
            // AÑADIDO: Lógica del temporizador
            if (cooldownHolograma > 0f)
            {
                cooldownHolograma -= Time.deltaTime;

                // Mientras estamos en cooldown, si existe un holograma, lo ocultamos
                if (hologramaActual != null && hologramaActual.activeSelf)
                {
                    hologramaActual.SetActive(false);
                }

                // Si el cooldown acaba de terminar y no tenemos holograma, lo creamos
                if (cooldownHolograma <= 0f && hologramaActual == null)
                {
                    CrearHolograma();
                }

                // Evitamos que ejecute lógica de construcción mientras espera
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
                    InfoEdificio actual = edificios[indiceEdificioActual];
                    hologramaActual.transform.Rotate(actual.offsetRotacion, Space.Self);
                    hologramaActual.transform.Translate(actual.offsetPosicion, Space.Self);
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
            if (cooldownHolograma <= 0f)
            {
                CrearHolograma();
            }

            Debug.Log("Modo construcción ACTIVADO");
        }
        else
        {
            LiberarSnapCinta(false);
            DestruirHolograma();
            Debug.Log("Modo construcción DESACTIVADO");
        }
    }

    void CambiarEdificioActual()
    {
        LiberarSnapCinta(false);

        indiceEdificioActual = (indiceEdificioActual + 1) % edificios.Length;
        DestruirHolograma();

        // Al cambiar de tipo, ignoramos el cooldown para que la respuesta sea inmediata
        cooldownHolograma = 0f;
        CrearHolograma();
    }

    void CrearHolograma()
    {
        rotacionManualOffset = 0f;

        InfoEdificio edificioActual = edificios[indiceEdificioActual];
        if (edificioActual.prefabsHologramas.Length > 0)
        {
            // Las cintas siempre empiezan como recta.
            // Orden esperado:
            // [0] Recta
            // [1] Curva Izquierda
            // [2] Curva Derecha
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

        // ---------------------------------------------------------
        // CINTA CON SNAP BLOQUEADO
        // ---------------------------------------------------------
        // Una vez que hemos encontrado un conector de cinta, lo conservamos.
        // Así el jugador puede dejar de apuntar al collider y mover el ratón
        // libremente para escoger recta / izquierda / derecha.
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
                    // No hacemos return: en este mismo frame el holograma vuelve
                    // a comportarse como una construcción libre.
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
                    hologramaActual.transform.rotation = conectorDetectado.transform.rotation;

                    if (conectorDetectado.CompareTag("ConectorSalida"))
                        AlinearPiezas("PuntoConexion_Entrada", conectorDetectado.transform.position);
                    else if (conectorDetectado.CompareTag("ConectorEntrada"))
                        AlinearPiezas("PuntoConexion_Salida", conectorDetectado.transform.position);
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
                        hologramaActual.transform.rotation = conectorDetectado.transform.root.rotation;

                        if (conectorDetectado.CompareTag("ConectorParedSalida"))
                            AlinearPiezas("PuntoConexionPared_Entrada", conectorDetectado.transform.position);
                        else if (conectorDetectado.CompareTag("ConectorParedEntrada"))
                            AlinearPiezas("PuntoConexionPared_Salida", conectorDetectado.transform.position);
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

                // Una cinta colocada libremente empieza como recta.
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

    // =========================================================
    // CINTAS AUTOMÁTICAS - SNAP PERSISTENTE + RATÓN
    // =========================================================

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

        // Si el conector ha quedado detrás de la cámara, la intención de
        // continuar esa cadena ya no es clara: soltamos tras el tiempo de gracia.
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

        // Si se desactiva la zona magnética, mantenemos un raycast normal
        // como comportamiento de respaldo.
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

            // La posición en pantalla manda. La distancia física solo desempata
            // si hay dos conectores prácticamente en el mismo lugar de la mirilla.
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

        // Nunca permitimos que la zona magnética capture los conectores
        // del propio holograma que estamos moviendo.
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

        int nuevaVariante = DeterminarVarianteCintaDesdeRaton(
            rayo,
            conector,
            snapCintaDesdeSalida
        );

        if (nuevaVariante != indiceVarianteActual)
        {
            CambiarVarianteHolograma(nuevaVariante);
            detector = hologramaActual.GetComponentInChildren<HologramaColision>(true);

            if (detector != null)
                detector.rampaAIgnorar = conector.root.gameObject;
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
        // Orden obligatorio:
        // 0 = Recta
        // 1 = Curva Izquierda
        // 2 = Curva Derecha
        InfoEdificio actual = edificios[indiceEdificioActual];

        if (actual.prefabsHologramas == null ||
            actual.prefabsHologramas.Length < 3 ||
            conector == null)
        {
            return 0;
        }

        Vector3 puntoRaton;

        // Preferimos un punto real del suelo. Si no lo encontramos,
        // usamos un plano horizontal a la altura del conector.
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

        // Histéresis:
        // si ya estamos en una curva, no volvemos a recta hasta entrar
        // claramente en la zona central.
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
            Debug.LogWarning(
                "No se encontró '" + nombreConectorMio +
                "' dentro del holograma de cinta."
            );
            return;
        }

        // Primero igualamos la orientación del conector propio con el destino.
        Quaternion diferenciaRotacion =
            conectorDestino.rotation * Quaternion.Inverse(miConector.rotation);

        hologramaActual.transform.rotation =
            diferenciaRotacion * hologramaActual.transform.rotation;

        // Después igualamos exactamente las posiciones.
        Vector3 diferenciaPosicion =
            miConector.position - hologramaActual.transform.position;

        hologramaActual.transform.position =
            conectorDestino.position - diferenciaPosicion;
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

    void AlinearPiezas(string nombreConectorMio, Vector3 posicionDestino)
    {
        Transform miConector = null;
        Transform[] todosLosHijos = hologramaActual.GetComponentsInChildren<Transform>();
        foreach (Transform hijo in todosLosHijos)
        {
            if (hijo.name == nombreConectorMio) { miConector = hijo; break; }
        }

        if (miConector != null)
        {
            Vector3 diferencia = miConector.position - hologramaActual.transform.position;
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
        HologramaColision detector = hologramaActual.GetComponentInChildren<HologramaColision>(true);
        return detector == null || !detector.HayColision;
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

                // -------------------------------------------------
                // ENCADENADO AUTOMÁTICO DE CINTAS
                // -------------------------------------------------
                // Tras colocar una cinta, dejamos bloqueado su extremo libre.
                // Así el siguiente holograma aparece listo para continuar.
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