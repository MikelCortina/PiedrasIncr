using UnityEngine;

public class SistemaConstruccion : MonoBehaviour
{
    public enum TipoEdificio { Rampa, Pared }

    [System.Serializable]
    public struct InfoEdificio
    {
        public string nombre;
        public TipoEdificio tipo;
        public GameObject prefabReal;
        public GameObject prefabHolograma;
    }

    [Header("Catálogo de Edificios")]
    public InfoEdificio[] edificios;
    private int indiceEdificioActual = 0;

    [Header("Referencias")]
    public Camera camaraPrincipal;

    [Header("Capas (Layers)")]
    public LayerMask capaSuelo;
    public LayerMask capaConectores;

    [Header("Ajustes de Construcción")]
    public KeyCode teclaConstruccion = KeyCode.B;
    public KeyCode teclaCambiarTipo = KeyCode.Tab; // NUEVO: Alternar edificio
    public float velocidadRotacion = 10f;
    public float distanciaMaximaConstruccion = 15f;

    private GameObject hologramaActual;
    public bool modoConstruccion = false;
    private bool estaImantado = false;

    private Collider imanApuntado = null;
    private float rotacionManualOffset = 0f;

    void Update()
    {
        if (Input.GetKeyDown(teclaConstruccion))
        {
            AlternarModoConstruccion();
        }

        // --- NUEVO: Cambiar de edificio con TAB ---
        if (modoConstruccion && Input.GetKeyDown(teclaCambiarTipo))
        {
            CambiarEdificioActual();
        }

        if (modoConstruccion && hologramaActual != null)
        {
            ManejarPosicionamientoYMagnetismo();

            if (hologramaActual.activeSelf)
            {
                ManejarRotacionLibre();
                ActualizarColorYValidacion();
                ManejarColocacion();
            }
        }
    }

    void AlternarModoConstruccion()
    {
        modoConstruccion = !modoConstruccion;

        if (modoConstruccion)
        {
            CrearHolograma();
        }
        else
        {
            DestruirHolograma();
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
        hologramaActual = Instantiate(edificios[indiceEdificioActual].prefabHolograma);
    }

    void DestruirHolograma()
    {
        if (hologramaActual != null) Destroy(hologramaActual);
    }

    void ManejarPosicionamientoYMagnetismo()
    {
        Ray rayo = camaraPrincipal.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        bool apuntandoValido = false;

        InfoEdificio actual = edificios[indiceEdificioActual];
        HologramaColision detector = hologramaActual.GetComponent<HologramaColision>();

        // 1. SI ES UNA RAMPA: Busca Conectores de Rampa
        if (actual.tipo == TipoEdificio.Rampa && Physics.Raycast(rayo, out hit, distanciaMaximaConstruccion, capaConectores, QueryTriggerInteraction.Collide)
            && (hit.collider.CompareTag("ConectorSalida") || hit.collider.CompareTag("ConectorEntrada")))
        {
            estaImantado = true;
            apuntandoValido = true;
            imanApuntado = hit.collider;
            hologramaActual.transform.rotation = hit.transform.rotation;

            if (detector != null) detector.rampaAIgnorar = hit.collider.transform.root.gameObject;

            if (hit.collider.CompareTag("ConectorSalida")) AlinearPiezas("PuntoConexion_Entrada", hit.transform.position);
            else if (hit.collider.CompareTag("ConectorEntrada")) AlinearPiezas("PuntoConexion_Salida", hit.transform.position);
        }
        // 2. SI ES UNA PARED: Busca Conectores de Rail
        else if (actual.tipo == TipoEdificio.Pared && Physics.Raycast(rayo, out hit, distanciaMaximaConstruccion, capaConectores, QueryTriggerInteraction.Collide)
                 && hit.collider.CompareTag("RailRampa"))
        {
            estaImantado = true;
            apuntandoValido = true;
            imanApuntado = hit.collider;

            // La pared se pega exactamente en la posición y rotación del raíl
            hologramaActual.transform.position = hit.transform.position;
            hologramaActual.transform.rotation = hit.transform.rotation;

            if (detector != null) detector.rampaAIgnorar = hit.collider.transform.root.gameObject;
        }
        // 3. SUELO LIBRE (Ambos edificios pueden ir en el suelo)
        else if (Physics.Raycast(rayo, out hit, distanciaMaximaConstruccion, capaSuelo, QueryTriggerInteraction.Collide))
        {
            hologramaActual.transform.position = hit.point;
            estaImantado = false;
            apuntandoValido = true;
            imanApuntado = null;

            if (detector != null) detector.rampaAIgnorar = null;
        }
        else
        {
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
        if (!estaImantado)
        {
            if (Input.GetMouseButton(1)) rotacionManualOffset += Input.GetAxis("Mouse X") * velocidadRotacion;

            Vector3 direccionCamara = camaraPrincipal.transform.forward;
            direccionCamara.y = 0f;

            if (direccionCamara.sqrMagnitude > 0.001f)
            {
                Quaternion rotacionBase = Quaternion.LookRotation(direccionCamara.normalized);
                hologramaActual.transform.rotation = rotacionBase * Quaternion.Euler(0f, rotacionManualOffset, 0f);
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
        Color colorEstado = PuedeColocarActual() ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
        Renderer[] renderizadores = hologramaActual.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderizadores)
        {
            // NUEVO: Repasamos TODOS los materiales del objeto por si tiene varias texturas
            foreach (Material mat in r.materials)
            {
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colorEstado);
                if (mat.HasProperty("_Color")) mat.color = colorEstado;
            }
        }
    }

    void ManejarColocacion()
    {
        if (PuedeColocarActual() && Input.GetMouseButtonDown(0))
        {
            InfoEdificio actual = edificios[indiceEdificioActual];
            GameObject nuevaEstructura = Instantiate(actual.prefabReal, hologramaActual.transform.position, hologramaActual.transform.rotation);

            // Sistema de quemado de imanes usados
            if (estaImantado && imanApuntado != null)
            {
                if (actual.tipo == TipoEdificio.Rampa)
                {
                    imanApuntado.enabled = false;
                    string nombreConectorAQuemar = imanApuntado.CompareTag("ConectorSalida") ? "PuntoConexion_Entrada" : "PuntoConexion_Salida";

                    Transform[] hijosNuevaRampa = nuevaEstructura.GetComponentsInChildren<Transform>();
                    foreach (Transform hijo in hijosNuevaRampa)
                    {
                        if (hijo.name == nombreConectorAQuemar)
                        {
                            Collider col = hijo.GetComponent<Collider>();
                            if (col != null) col.enabled = false;
                            break;
                        }
                    }
                }
                else if (actual.tipo == TipoEdificio.Pared)
                {
                    // Al poner una pared en un raíl, desactivamos el raíl para que no se pongan 2 paredes encima
                    imanApuntado.enabled = false;
                }
            }
        }
    }
}