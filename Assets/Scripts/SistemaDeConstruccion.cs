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

    [Header("Cintas automáticas")]
    [Tooltip("Zona central para elegir cinta recta. Cuanto mayor sea, más tendrás que colocarte a un lado para elegir una curva.")]
    [Range(0f, 1f)]
    public float umbralLateralCinta = 0.35f;

    [Tooltip("Muestra en consola cuándo la cinta automática cambia entre recta, izquierda y derecha.")]
    public bool debugCintaAutomatica = false;

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

    void Update()
    {
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
            // Si hay un cooldown activo de antes, no creamos el holograma todavía
            if (cooldownHolograma <= 0f)
            {
                CrearHolograma();
            }
            Debug.Log("Modo construcción ACTIVADO");
        }
        else
        {
            DestruirHolograma();
            Debug.Log("Modo construcción DESACTIVADO");
        }
    }

    void CambiarEdificioActual()
    {
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
        HologramaColision detector = hologramaActual.GetComponent<HologramaColision>();

        edificioApuntado = null;

        bool chocaConector = Physics.Raycast(rayo, out RaycastHit hitConector, distanciaMaximaConstruccion, capaConectores, QueryTriggerInteraction.Collide);
        bool conectorValido = false;

        if (chocaConector)
        {
            if (actual.tipo == TipoEdificio.Rampa && (hitConector.collider.CompareTag("ConectorSalida") || hitConector.collider.CompareTag("ConectorEntrada")))
                conectorValido = true;
            else if (actual.tipo == TipoEdificio.Cinta && (hitConector.collider.CompareTag("ConectorSalida") || hitConector.collider.CompareTag("ConectorEntrada")))
                conectorValido = true;
            else if (actual.tipo == TipoEdificio.Pared && (hitConector.collider.CompareTag("RailRampa") || hitConector.collider.CompareTag("ConectorParedSalida") || hitConector.collider.CompareTag("ConectorParedEntrada")))
                conectorValido = true;
        }

        if (conectorValido)
        {
            posicionSueloBloqueada = null;

            if (hitConector.collider == slotBloqueado)
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
                estaImantado = true;
                apuntandoValido = true;
                imanApuntado = hitConector.collider;

                if (detector != null) detector.rampaAIgnorar = hitConector.collider.transform.root.gameObject;

                if (actual.tipo == TipoEdificio.Rampa)
                {
                    hologramaActual.transform.rotation = hitConector.transform.rotation;
                    if (hitConector.collider.CompareTag("ConectorSalida")) AlinearPiezas("PuntoConexion_Entrada", hitConector.transform.position);
                    else if (hitConector.collider.CompareTag("ConectorEntrada")) AlinearPiezas("PuntoConexion_Salida", hitConector.transform.position);
                }
                else if (actual.tipo == TipoEdificio.Cinta)
                {
                    bool conectandoDesdeSalida = hitConector.collider.CompareTag("ConectorSalida");

                    int nuevaVariante = DeterminarVarianteCinta(
                        hitConector.transform,
                        conectandoDesdeSalida
                    );

                    if (nuevaVariante != indiceVarianteActual)
                    {
                        CambiarVarianteHolograma(nuevaVariante);
                        detector = hologramaActual.GetComponent<HologramaColision>();
                    }

                    // Si conectamos al final de otra cinta usamos nuestra entrada.
                    // Si conectamos por detrás usamos nuestra salida.
                    string nombreConectorMio = conectandoDesdeSalida
                        ? "PuntoConexion_Entrada"
                        : "PuntoConexion_Salida";

                    AlinearConectorCinta(nombreConectorMio, hitConector.transform);
                }
                else if (actual.tipo == TipoEdificio.Pared)
                {
                    if (hitConector.collider.CompareTag("RailRampa"))
                    {
                        hologramaActual.transform.position = hitConector.transform.position;
                        hologramaActual.transform.rotation = hitConector.transform.rotation * Quaternion.Euler(-90f, 0f, 0f);
                    }
                    else
                    {
                        hologramaActual.transform.rotation = hitConector.transform.root.rotation;

                        if (hitConector.collider.CompareTag("ConectorParedSalida")) AlinearPiezas("PuntoConexionPared_Entrada", hitConector.transform.position);
                        else if (hitConector.collider.CompareTag("ConectorParedEntrada")) AlinearPiezas("PuntoConexionPared_Salida", hitConector.transform.position);
                    }
                }
            }
        }
        else if (Physics.Raycast(rayo, out hit, distanciaMaximaConstruccion, capaEdificios))
        {
            EdificioConstruido infoEdificio = hit.transform.root.GetComponent<EdificioConstruido>();

            slotBloqueado = null;
            posicionSueloBloqueada = null;
            timerBloqueoSlot = 0f;

            if (infoEdificio != null && infoEdificio.tipo == actual.tipo)
            {
                edificioApuntado = hit.transform.root.gameObject;
                estaImantado = false;
                apuntandoValido = false;
                imanApuntado = null;
                if (detector != null) detector.rampaAIgnorar = null;
            }
            else
            {
                apuntandoValido = false;
                imanApuntado = null;
                if (detector != null) detector.rampaAIgnorar = null;
            }
        }
        else if (Physics.Raycast(rayo, out hit, distanciaMaximaConstruccion, capaSuelo, QueryTriggerInteraction.Collide))
        {
            slotBloqueado = null;

            if (posicionSueloBloqueada.HasValue && Vector3.Distance(hit.point, posicionSueloBloqueada.Value) < 2.0f)
            {
                apuntandoValido = false;
                estaImantado = false;
                imanApuntado = null;
                if (detector != null) detector.rampaAIgnorar = null;
            }
            else
            {
                posicionSueloBloqueada = null;
                timerBloqueoSlot = 0f;

                // Si una cinta no está conectada a otra, vuelve a la variante recta.
                if (actual.tipo == TipoEdificio.Cinta && indiceVarianteActual != 0)
                {
                    CambiarVarianteHolograma(0);
                    detector = hologramaActual.GetComponent<HologramaColision>();
                }

                hologramaActual.transform.position = hit.point;
                estaImantado = false;
                apuntandoValido = true;
                imanApuntado = null;
                if (detector != null) detector.rampaAIgnorar = null;
            }
        }
        else
        {
            slotBloqueado = null;
            posicionSueloBloqueada = null;
            timerBloqueoSlot = 0f;

            apuntandoValido = false;
            imanApuntado = null;
            if (detector != null) detector.rampaAIgnorar = null;
        }

        hologramaActual.SetActive(apuntandoValido);
    }

    // =========================================================
    // CINTAS AUTOMÁTICAS
    // =========================================================

    int DeterminarVarianteCinta(Transform conector, bool conectandoDesdeSalida)
    {
        // Orden obligatorio de variantes:
        // 0 = Recta
        // 1 = Curva Izquierda
        // 2 = Curva Derecha
        InfoEdificio actual = edificios[indiceEdificioActual];

        if (actual.prefabsHologramas == null || actual.prefabsHologramas.Length < 3)
            return 0;

        if (camaraPrincipal == null)
            return 0;

        Vector3 haciaJugador = camaraPrincipal.transform.position - conector.position;
        haciaJugador.y = 0f;

        if (haciaJugador.sqrMagnitude < 0.001f)
            return 0;

        haciaJugador.Normalize();

        // Si estamos construyendo desde una salida, el eje Right del conector
        // marca directamente derecha/izquierda.
        //
        // Si conectamos contra una entrada estamos construyendo en sentido
        // contrario, por eso invertimos el eje lateral.
        Vector3 derechaConstruccion = conectandoDesdeSalida
            ? conector.right
            : -conector.right;

        derechaConstruccion.y = 0f;

        if (derechaConstruccion.sqrMagnitude < 0.001f)
            return 0;

        derechaConstruccion.Normalize();

        float lateral = Vector3.Dot(haciaJugador, derechaConstruccion);

        int varianteDeseada;

        if (Mathf.Abs(lateral) <= umbralLateralCinta)
        {
            varianteDeseada = 0;
        }
        else if (lateral < 0f)
        {
            // Construyendo al revés, la geometría del prefab se recorre al revés,
            // así que izquierda y derecha se intercambian.
            varianteDeseada = conectandoDesdeSalida ? 1 : 2;
        }
        else
        {
            varianteDeseada = conectandoDesdeSalida ? 2 : 1;
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
        if (!estaImantado && edificioApuntado == null)
        {
            if (Input.GetMouseButton(1)) rotacionManualOffset += Input.GetAxis("Mouse X") * velocidadRotacion;

            Vector3 direccionCamara = camaraPrincipal.transform.forward;
            direccionCamara.y = 0f;

            if (direccionCamara.sqrMagnitude > 0.001f)
            {
                Quaternion rotacionBase = Quaternion.LookRotation(direccionCamara.normalized);

                if (edificios[indiceEdificioActual].tipo == TipoEdificio.Pared)
                {
                    hologramaActual.transform.rotation = rotacionBase * Quaternion.Euler(-90f, rotacionManualOffset, 0f);
                }
                else
                {
                    hologramaActual.transform.rotation = rotacionBase * Quaternion.Euler(0f, rotacionManualOffset, 0f);
                }
            }
        }
    }

    bool PuedeColocarActual()
    {
        HologramaColision detector = hologramaActual.GetComponent<HologramaColision>();
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
                EdificioConstruido infoEdificio = edificioApuntado.GetComponent<EdificioConstruido>();

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
                        posicionSueloBloqueada = edificioApuntado.transform.position;
                    }

                    foreach (GameObject dep in infoEdificio.edificiosDependientes)
                    {
                        if (dep != null) dep.AddComponent<EfectoBloopDestruccion>();
                    }

                    foreach (Collider colHijo in infoEdificio.conectoresHijosBloqueados)
                    {
                        if (colHijo != null) colHijo.enabled = true;
                    }
                }

                edificioApuntado.AddComponent<EfectoBloopDestruccion>();

                edificioApuntadoAnterior = null;
                edificioApuntado = null;

                // Aplicar cooldown modificado al destruir
                cooldownHolograma = tiempoEsperaConstruccion;
                timerBloqueoSlot = 1.0f;

                DestruirHolograma();
                return;
            }

            if (PuedeColocarActual() && hologramaActual.activeSelf)
            {
                InfoEdificio actual = edificios[indiceEdificioActual];

                GameObject nuevaEstructura = Instantiate(actual.prefabsReales[indiceVarianteActual], hologramaActual.transform.position, hologramaActual.transform.rotation);

                nuevaEstructura.AddComponent<EfectoBloop>();

                EdificioConstruido id = nuevaEstructura.AddComponent<EdificioConstruido>();
                id.tipo = actual.tipo;

                if (estaImantado && imanApuntado != null)
                {
                    id.conectorUsado = imanApuntado;
                    imanApuntado.enabled = false;

                    if (actual.tipo == TipoEdificio.Rampa || actual.tipo == TipoEdificio.Cinta)
                    {
                        string nombreConectorAQuemar = imanApuntado.CompareTag("ConectorSalida") ? "PuntoConexion_Entrada" : "PuntoConexion_Salida";

                        Collider miConectorApagado = null;
                        Transform[] hijosNuevaRampa = nuevaEstructura.GetComponentsInChildren<Transform>();
                        foreach (Transform hijo in hijosNuevaRampa)
                        {
                            if (hijo.name == nombreConectorAQuemar)
                            {
                                miConectorApagado = hijo.GetComponent<Collider>();
                                if (miConectorApagado != null) miConectorApagado.enabled = false;
                                break;
                            }
                        }

                        EdificioConstruido rampaPadre = imanApuntado.transform.root.GetComponent<EdificioConstruido>();
                        if (rampaPadre != null && miConectorApagado != null)
                        {
                            rampaPadre.conectoresHijosBloqueados.Add(miConectorApagado);
                        }
                    }
                    else if (actual.tipo == TipoEdificio.Pared)
                    {
                        if (imanApuntado.CompareTag("RailRampa"))
                        {
                            EdificioConstruido rampaPadre = imanApuntado.transform.root.GetComponent<EdificioConstruido>();
                            if (rampaPadre != null) rampaPadre.edificiosDependientes.Add(nuevaEstructura);

                            Collider[] collidersPared = nuevaEstructura.GetComponentsInChildren<Collider>();
                            foreach (Collider col in collidersPared)
                            {
                                if (col.CompareTag("ConectorParedSalida") || col.CompareTag("ConectorParedEntrada"))
                                {
                                    col.enabled = false;
                                }
                            }
                        }
                        else
                        {
                            string nombreConectorAQuemar = imanApuntado.CompareTag("ConectorParedSalida") ? "PuntoConexionPared_Entrada" : "PuntoConexionPared_Salida";

                            Collider miConectorApagado = null;
                            Transform[] hijosNuevaPared = nuevaEstructura.GetComponentsInChildren<Transform>();
                            foreach (Transform hijo in hijosNuevaPared)
                            {
                                if (hijo.name == nombreConectorAQuemar)
                                {
                                    miConectorApagado = hijo.GetComponent<Collider>();
                                    if (miConectorApagado != null) miConectorApagado.enabled = false;
                                    break;
                                }
                            }

                            EdificioConstruido paredPadre = imanApuntado.transform.root.GetComponent<EdificioConstruido>();
                            if (paredPadre != null && miConectorApagado != null)
                            {
                                paredPadre.conectoresHijosBloqueados.Add(miConectorApagado);
                            }
                        }
                    }
                }

                // AÑADIDO: Aplicar el cooldown desde el inspector al construir
                cooldownHolograma = tiempoEsperaConstruccion;

                // Destruimos el holograma actual. El Update se encargará de crear el nuevo
                // cuando termine el tiempo de cooldown.
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