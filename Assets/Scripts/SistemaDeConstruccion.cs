using UnityEngine;
using System.Collections.Generic;

public class SistemaConstruccion : MonoBehaviour
{
    public enum TipoEdificio { Rampa, Pared, Libre }

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
    public float tiempoEsperaConstruccion = 0.5f;

    [Header("Ajustes de Curvatura Dinámica (Eje X)")]
    [Tooltip("Mantén esta tecla para acceder a la geometría y curvar la rampa lateralmente (Puedes usar rueda del ratón)")]
    public KeyCode teclaCurvar = KeyCode.Space;
    public float velocidadCurvatura = 3f;
    public float curvaturaMaxima = 3f;

    // --- Estructura para guardar el estado original de TODOS los conectores ---
    private struct ConectorCache
    {
        public Transform transform;
        public Vector3 originalRootPos;
        public Quaternion originalRootRot;
    }

    // --- Variables Internas de Curvatura ---
    private float curvaturaActual = 0f;
    private float tiempoCurvado = 0f;
    private MeshFilter[] hologramMeshFilters;
    private Vector3[][] hologramOriginalVerts;
    private List<ConectorCache> conectoresHologramaCache = new List<ConectorCache>();
    private float hologramaMinZ, hologramaMaxZ;

    // --- Variables Internas del Sistema ---
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
            if (cooldownHolograma > 0f)
            {
                if (hologramaActual != null && hologramaActual.activeSelf)
                {
                    hologramaActual.SetActive(false);
                }

                cooldownHolograma -= Time.deltaTime;

                if (cooldownHolograma <= 0f && hologramaActual == null)
                {
                    CrearHolograma();
                }

                return;
            }
            else if (hologramaActual != null)
            {
                // ==========================================
                // LÓGICA DE CURVATURA DINÁMICA (SOLO RAMPAS)
                // ==========================================
                if (edificios[indiceEdificioActual].tipo == TipoEdificio.Rampa)
                {
                    ManejarCurvaturaRampa();
                }

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
        if (modoConstruccion == activar) return;

        modoConstruccion = activar;

        if (modeloMartillo != null)
        {
            modeloMartillo.SetActive(modoConstruccion);
        }

        if (modoConstruccion)
        {
            if (cooldownHolograma <= 0f) CrearHolograma();
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

        cooldownHolograma = 0f;
        CrearHolograma();
    }

    void CrearHolograma()
    {
        rotacionManualOffset = 0f;
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

        slotBloqueado = null;
        posicionSueloBloqueada = null;
        timerBloqueoSlot = 0f;

        // Si es una rampa, preparamos las cachés de los vértices para curvarla
        if (edificioActual.tipo == TipoEdificio.Rampa)
        {
            curvaturaActual = 0f;
            tiempoCurvado = 0f;
            InicializarCurvaturaHolograma();
        }
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

    // ========================================================
    // DEFORMADOR MATEMÁTICO DE RAMPAS (EJE X)
    // ========================================================
    void InicializarCurvaturaHolograma()
    {
        hologramMeshFilters = hologramaActual.GetComponentsInChildren<MeshFilter>();
        hologramOriginalVerts = new Vector3[hologramMeshFilters.Length][];
        Transform rootTransform = hologramaActual.transform;

        for (int i = 0; i < hologramMeshFilters.Length; i++)
        {
            Mesh clon = Instantiate(hologramMeshFilters[i].sharedMesh);
            hologramMeshFilters[i].mesh = clon;
            hologramOriginalVerts[i] = clon.vertices;
        }

        conectoresHologramaCache.Clear();
        Transform conectorEntrada = null;
        Transform conectorSalida = null;

        Transform[] hijos = hologramaActual.GetComponentsInChildren<Transform>();
        foreach (Transform t in hijos)
        {
            if (t.CompareTag("ConectorEntrada")) conectorEntrada = t;
            if (t.CompareTag("ConectorSalida")) conectorSalida = t;

            if (t.CompareTag("ConectorSalida") || t.CompareTag("ConectorEntrada") ||
                t.CompareTag("RailRampa") || t.CompareTag("ConectorParedSalida") ||
                t.CompareTag("ConectorParedEntrada") || t.name.Contains("PuntoConexion"))
            {
                ConectorCache cache = new ConectorCache();
                cache.transform = t;
                cache.originalRootPos = rootTransform.InverseTransformPoint(t.position);
                cache.originalRootRot = Quaternion.Inverse(rootTransform.rotation) * t.rotation;
                conectoresHologramaCache.Add(cache);
            }
        }

        if (conectorEntrada != null && conectorSalida != null)
        {
            hologramaMinZ = rootTransform.InverseTransformPoint(conectorEntrada.position).z;
            hologramaMaxZ = rootTransform.InverseTransformPoint(conectorSalida.position).z;
        }
        else
        {
            hologramaMinZ = float.MaxValue;
            hologramaMaxZ = float.MinValue;
            foreach (var cache in conectoresHologramaCache)
            {
                if (cache.originalRootPos.z < hologramaMinZ) hologramaMinZ = cache.originalRootPos.z;
                if (cache.originalRootPos.z > hologramaMaxZ) hologramaMaxZ = cache.originalRootPos.z;
            }
        }
    }

    void ManejarCurvaturaRampa()
    {
        if (Input.GetKey(teclaCurvar))
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel") * 10f;
            float teclado = 0f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.UpArrow)) teclado = 1f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.DownArrow)) teclado = -1f;

            float inputTotal = scroll + (teclado * Time.deltaTime * velocidadCurvatura);

            if (inputTotal == 0f)
            {
                tiempoCurvado += Time.deltaTime * velocidadCurvatura * 0.5f;
                curvaturaActual = Mathf.Sin(tiempoCurvado) * curvaturaMaxima;
            }
            else
            {
                curvaturaActual += inputTotal;
                curvaturaActual = Mathf.Clamp(curvaturaActual, -curvaturaMaxima, curvaturaMaxima);
                tiempoCurvado = Mathf.Asin(curvaturaActual / curvaturaMaxima);
            }

            AplicarCurvaturaHolograma();
        }
    }

    void AplicarCurvaturaHolograma()
    {
        if (hologramMeshFilters == null) return;
        float lengthZ = hologramaMaxZ - hologramaMinZ;
        if (Mathf.Abs(lengthZ) <= 0.001f) return;

        Transform rootTransform = hologramaActual.transform;

        for (int i = 0; i < hologramMeshFilters.Length; i++)
        {
            Mesh m = hologramMeshFilters[i].mesh;
            Vector3[] verts = m.vertices;
            Vector3[] orig = hologramOriginalVerts[i];
            Transform mfTransform = hologramMeshFilters[i].transform;

            for (int j = 0; j < verts.Length; j++)
            {
                Vector3 vMundo = mfTransform.TransformPoint(orig[j]);
                Vector3 vRoot = rootTransform.InverseTransformPoint(vMundo);

                // CAMBIO CLAVE: Cambiado de vRoot.z - hologramaMinZ (Entrada) a hologramaMaxZ - vRoot.z (Salida)
                // de modo que en el extremo de Salida (MaxZ) "t" sea 0 (fijo) y en la Entrada (MinZ) "t" sea 1 (máximo movimiento).
                float t = (hologramaMaxZ - vRoot.z) / lengthZ;
                vRoot.x += curvaturaActual * (t * t);

                vMundo = rootTransform.TransformPoint(vRoot);
                verts[j] = mfTransform.InverseTransformPoint(vMundo);
            }
            m.vertices = verts;
            m.RecalculateNormals();
            m.RecalculateBounds();
        }

        foreach (ConectorCache cache in conectoresHologramaCache)
        {
            if (cache.transform == null) continue;

            // Mismo cálculo inverso para que el conector de salida sea el estático (T=0)
            float tVal = (hologramaMaxZ - cache.originalRootPos.z) / lengthZ;
            Vector3 nuevaPosRoot = cache.originalRootPos;
            nuevaPosRoot.x += curvaturaActual * (tVal * tVal);

            cache.transform.position = rootTransform.TransformPoint(nuevaPosRoot);

            // Derivada de la parábola invertida para que la rotación también se anule en el extremo estático (Salida, t=0).
            float slopeExtra = (2f * curvaturaActual * tVal) / lengthZ;
            float anguloExtra = Mathf.Atan(slopeExtra) * Mathf.Rad2Deg;

            // Se calcula y aplica la rotación dinamica desde la Salida fija hacia la Entrada curvada.
            Quaternion rotacionDinamicaRoot = Quaternion.Euler(0, anguloExtra, 0) * cache.originalRootRot;
            cache.transform.rotation = rootTransform.rotation * rotacionDinamicaRoot;
        }
    }

    void AplicarCurvaturaAObjetoReal(GameObject estructuraReal)
    {
        MeshFilter[] mfs = estructuraReal.GetComponentsInChildren<MeshFilter>();
        float lengthZ = hologramaMaxZ - hologramaMinZ;
        Transform rootTransform = estructuraReal.transform;

        foreach (MeshFilter mf in mfs)
        {
            Mesh clon = Instantiate(mf.sharedMesh);
            Vector3[] verts = clon.vertices;
            Transform mfTransform = mf.transform;

            for (int j = 0; j < verts.Length; j++)
            {
                Vector3 vMundo = mfTransform.TransformPoint(verts[j]);
                Vector3 vRoot = rootTransform.InverseTransformPoint(vMundo);

                // Aplicar el mismo cálculo inverso para la malla del objeto real.
                float t = (hologramaMaxZ - vRoot.z) / lengthZ;
                vRoot.x += curvaturaActual * (t * t);

                vMundo = rootTransform.TransformPoint(vRoot);
                verts[j] = mfTransform.InverseTransformPoint(vMundo);
            }
            clon.vertices = verts;
            clon.RecalculateNormals();
            clon.RecalculateBounds();
            mf.mesh = clon;

            MeshCollider mc = mf.GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.sharedMesh = clon;
            }
        }

        Transform[] hijos = estructuraReal.GetComponentsInChildren<Transform>();
        foreach (Transform t in hijos)
        {
            if (t.CompareTag("ConectorSalida") || t.CompareTag("ConectorEntrada") ||
                t.CompareTag("RailRampa") || t.CompareTag("ConectorParedSalida") ||
                t.CompareTag("ConectorParedEntrada") || t.name.Contains("PuntoConexion"))
            {
                Vector3 origPosRoot = rootTransform.InverseTransformPoint(t.position);
                // Mismo cálculo inverso para los conectores reales.
                float tVal = (hologramaMaxZ - origPosRoot.z) / lengthZ;

                origPosRoot.x += curvaturaActual * (tVal * tVal);
                t.position = rootTransform.TransformPoint(origPosRoot);

                float slopeExtra = (2f * curvaturaActual * tVal) / lengthZ;
                float anguloExtra = Mathf.Atan(slopeExtra) * Mathf.Rad2Deg;

                Quaternion originalRootRot = Quaternion.Inverse(rootTransform.rotation) * t.rotation;
                Quaternion rotacionDinamicaRoot = Quaternion.Euler(0, anguloExtra, 0) * originalRootRot;
                t.rotation = rootTransform.rotation * rotacionDinamicaRoot;
            }
        }
    }
    // ========================================================

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

                cooldownHolograma = tiempoEsperaConstruccion;
                timerBloqueoSlot = 1.0f;

                DestruirHolograma();
                return;
            }

            if (PuedeColocarActual() && hologramaActual.activeSelf)
            {
                InfoEdificio actual = edificios[indiceEdificioActual];

                GameObject nuevaEstructura = Instantiate(actual.prefabsReales[indiceVarianteActual], hologramaActual.transform.position, hologramaActual.transform.rotation);

                // APLICAR LA CURVA A LA ESTRUCTURA REAL Y AL MESH COLLIDER
                if (actual.tipo == TipoEdificio.Rampa && curvaturaActual != 0f)
                {
                    AplicarCurvaturaAObjetoReal(nuevaEstructura);
                }

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