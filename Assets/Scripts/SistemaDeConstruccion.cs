using UnityEngine;
using System.Collections.Generic;

public class SistemaConstruccion : MonoBehaviour
{
    public enum TipoEdificio { Rampa, Pared }

    [System.Serializable]
    public struct InfoEdificio
    {
        public string nombre;
        public TipoEdificio tipo;
        // NUEVO: Ahora son listas para permitir variaciones
        public GameObject[] prefabsReales;
        public GameObject[] prefabsHologramas;
    }

    [Header("Martillo")]
    public GameObject modeloMartillo;

    [Header("Catálogo de Edificios")]
    public InfoEdificio[] edificios;
    private int indiceEdificioActual = 0;

    // NUEVO: Guarda qué variante aleatoria está usando el holograma actual
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

        if (modoConstruccion && hologramaActual != null)
        {
            if (cooldownHolograma > 0f)
            {
                cooldownHolograma -= Time.deltaTime;

                if (hologramaActual.activeSelf)
                {
                    hologramaActual.SetActive(false);
                }
            }
            else
            {
                ManejarPosicionamientoYMagnetismo();

                ManejarRotacionLibre();

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
            modeloMartillo.SetActive(
                modoConstruccion
            );
        }


        if (modoConstruccion)
        {
            CrearHolograma();

            Debug.Log(
                "Modo construcción ACTIVADO"
            );
        }
        else
        {
            DestruirHolograma();

            Debug.Log(
                "Modo construcción DESACTIVADO"
            );
        }
    }

    void CambiarEdificioActual()
    {
        indiceEdificioActual = (indiceEdificioActual + 1) % edificios.Length;
        DestruirHolograma();
        CrearHolograma();
    }

    void CrearHolograma()
    {
        rotacionManualOffset = 0f;

        // NUEVO: Elegir una variante aleatoria de la lista
        InfoEdificio edificioActual = edificios[indiceEdificioActual];
        if (edificioActual.prefabsHologramas.Length > 0)
        {
            indiceVarianteActual = Random.Range(0, edificioActual.prefabsHologramas.Length);
            hologramaActual = Instantiate(edificioActual.prefabsHologramas[indiceVarianteActual]);
        }
        else
        {
            Debug.LogError("No hay hologramas asignados para el edificio: " + edificioActual.nombre);
        }

        cooldownHolograma = 0f;

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
            slotBloqueado = null;
            posicionSueloBloqueada = null;
            timerBloqueoSlot = 0f;

            EdificioConstruido infoEdificio = hit.transform.root.GetComponent<EdificioConstruido>();

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

                cooldownHolograma = 0.15f;
                timerBloqueoSlot = 1.0f;

                // NUEVO: Al destruir, regeneramos el holograma para que cambie de modelo si es necesario
                DestruirHolograma();
                CrearHolograma();
                return;
            }

            if (PuedeColocarActual() && hologramaActual.activeSelf)
            {
                InfoEdificio actual = edificios[indiceEdificioActual];

                // NUEVO: Instanciamos el prefab real basándonos en el índice de la variante que generó el holograma
                GameObject nuevaEstructura = Instantiate(actual.prefabsReales[indiceVarianteActual], hologramaActual.transform.position, hologramaActual.transform.rotation);

                nuevaEstructura.AddComponent<EfectoBloop>();

                EdificioConstruido id = nuevaEstructura.AddComponent<EdificioConstruido>();
                id.tipo = actual.tipo;

                if (estaImantado && imanApuntado != null)
                {
                    id.conectorUsado = imanApuntado;
                    imanApuntado.enabled = false;

                    if (actual.tipo == TipoEdificio.Rampa)
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

                cooldownHolograma = 0.15f;

                // NUEVO: Regeneramos el holograma para que el próximo que vayas a colocar sea aleatorio también
                DestruirHolograma();
                CrearHolograma();
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