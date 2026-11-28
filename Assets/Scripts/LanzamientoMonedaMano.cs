using UnityEngine;

/// <summary>
/// Sin el Fusil de monedas: con la tecla del fusil (3) sacas una moneda en la mano y la lanzas despacio (clic izquierdo).
/// La moneda lanzada es un proyectil igual que el del fusil (Moneda + ProyectilArma), así que paga construcciones
/// como el generador de meteoritos. La activa GestorEquipamiento (la crea solo si no está en la escena).
/// </summary>
public class LanzamientoMonedaMano : MonoBehaviour
{
    [Header("Referencias (vacío = se buscan solas)")]
    public Cartera cartera;
    public Camera camara;
    [Tooltip("Moneda que se lanza. Vacío = el proyectil del Fusil de monedas.")]
    public GameObject prefabMoneda;
    [Tooltip("El Fusil de monedas: la moneda aparece donde va su modelo y sale por su punto de disparo. Vacío = se busca.")]
    public ArmaLanzadora fusil;
    [Tooltip("Usa el crecimiento, la vida y la inmunidad del proyectil del fusil (en vez de los de abajo).")]
    public bool usarAjustesDelFusil = true;

    [Header("Lanzamiento")]
    public KeyCode teclaLanzar = KeyCode.Mouse0;
    [Tooltip("Velocidad de la moneda (el fusil dispara mucho más fuerte).")]
    public float fuerzaLanzamiento = 12f;
    [Tooltip("Cuánto sube la moneda al lanzarla (0 = recta).")]
    [Range(0f, 1f)] public float elevacion = 0.2f;
    [Tooltip("Segundos entre una moneda y la siguiente.")]
    public float esperaEntreLanzamientos = 0.8f;
    [Min(1)] public int valorMoneda = 1;
    public float escalaProyectil = 1.5f;
    public float velocidadCrecimiento = 8f;
    public float vidaProyectil = 15f;
    [Tooltip("Segundos en los que la moneda lanzada no se puede volver a recoger.")]
    public float tiempoInmunidad = 1.5f;

    [Header("Moneda en la mano (como un arma)")]
    [Tooltip("Posición respecto a la cámara (X derecha, Y arriba, Z delante).")]
    public Vector3 posicionEnMano = new Vector3(0.3f, -0.26f, 0.6f);
    [Tooltip("Tamaño de la moneda en la mano, en metros (lo más ancho). No depende de la escala del prefab ni del padre.")]
    public float tamanoEnMano = 0.2f;
    [Tooltip("Inclinación de la moneda (grados). Con 0,0,0 se ve de frente, con la cara hacia la cámara.")]
    public Vector3 inclinacionEnMano = new Vector3(12f, -28f, 10f);
    [Tooltip("Cuánto se balancea al mover la vista (se queda un poco atrás y vuelve).")]
    public float balanceoMirada = 0.02f;
    [Tooltip("Respiración: subida y bajada suave en reposo (metros).")]
    public float respiracion = 0.008f;
    [Tooltip("Segundos que tarda en subir la siguiente moneda a la mano después de lanzar.")]
    public float duracionAparicion = 0.25f;
    [Tooltip("Lo que se retira la mano hacia abajo cuando no hay moneda (metros).")]
    public float bajadaSinMoneda = 0.18f;

    private bool equipada;
    private GameObject visual;
    private Transform modeloVisual;
    private float proximoLanzamiento;
    private float presencia;          // 0 = mano bajada (sin moneda), 1 = moneda en la mano
    private Vector2 balanceo;
    private Quaternion rotacionCamaraAnterior;
    private float empujon;            // pequeño golpe hacia delante al lanzar

    public bool Equipada => equipada;

    public void SetEquipada(bool valor)
    {
        equipada = valor;
        if (equipada) CrearVisual();
        if (visual != null) visual.SetActive(equipada);
    }

    private void BuscarReferencias()
    {
        if (camara == null) camara = Camera.main;
        if (cartera == null) cartera = FindFirstObjectByType<Cartera>();
        if (fusil == null) fusil = FindFirstObjectByType<ArmaLanzadora>(FindObjectsInactive.Include);
        if (prefabMoneda == null && fusil != null) prefabMoneda = fusil.prefabProyectil;
    }

    private void Update()
    {
        if (!equipada) return;
        BuscarReferencias();
        if (camara == null) return;

        bool lista = Time.time >= proximoLanzamiento && cartera != null && cartera.Monedas >= valorMoneda;
        if (visual != null) AnimarVisual(lista);

        if (Input.GetKeyDown(teclaLanzar) && Time.time >= proximoLanzamiento) Lanzar();
    }

    // La moneda va como un arma: abajo a la derecha, con balanceo al mirar y respiración.
    // Sin monedas (o recién lanzada) baja fuera de la vista y vuelve a subir cuando hay otra.
    private void AnimarVisual(bool lista)
    {
        float dt = Time.deltaTime;
        presencia = Mathf.MoveTowards(presencia, lista ? 1f : 0f, dt / Mathf.Max(0.01f, duracionAparicion));
        float subida = 1f - (1f - presencia) * (1f - presencia); // sale rápido y frena al llegar
        visual.SetActive(presencia > 0.01f);

        // Balanceo: la moneda se queda un poco atrás cuando giras la vista y vuelve a su sitio.
        Quaternion rotacionCamara = camara.transform.rotation;
        Quaternion giro = Quaternion.Inverse(rotacionCamaraAnterior) * rotacionCamara;
        rotacionCamaraAnterior = rotacionCamara;
        Vector3 euler = giro.eulerAngles;
        Vector2 delta = new Vector2(Mathf.DeltaAngle(0f, euler.y), Mathf.DeltaAngle(0f, euler.x));
        balanceo = Vector2.Lerp(balanceo, Vector2.ClampMagnitude(-delta * balanceoMirada, 0.06f), 1f - Mathf.Exp(-12f * dt));
        balanceo = Vector2.Lerp(balanceo, Vector2.zero, 1f - Mathf.Exp(-6f * dt));

        empujon = Mathf.MoveTowards(empujon, 0f, dt * 6f);

        Vector3 posicion = posicionEnMano
                           + new Vector3(balanceo.x, -balanceo.y, 0f)
                           + Vector3.up * (Mathf.Sin(Time.time * 2f) * respiracion - (1f - subida) * bajadaSinMoneda)
                           + Vector3.forward * empujon * 0.12f;
        visual.transform.localPosition = posicion;
        visual.transform.localRotation = Quaternion.Euler(inclinacionEnMano + new Vector3(-balanceo.y * 300f, balanceo.x * 300f, 0f));

        // Un leve vaivén de la moneda sobre sí misma para que brille y se note que es 3D.
        if (modeloVisual != null)
            modeloVisual.localRotation = Quaternion.AngleAxis(Mathf.Sin(Time.time * 1.3f) * 12f, Vector3.up) * rotacionBaseModelo;
    }

    private Quaternion rotacionBaseModelo = Quaternion.identity;

    private void Lanzar()
    {
        if (prefabMoneda == null)
        {
            Debug.LogWarning("LanzamientoMonedaMano: no hay prefab de moneda (asígnalo o pon el Fusil de monedas en la escena).", this);
            return;
        }
        if (cartera == null || !cartera.GastarMonedas(valorMoneda))
        {
            Debug.Log("No tienes monedas para lanzar.");
            return;
        }

        Transform cam = camara.transform;
        // Sale por el punto de disparo del fusil (o desde la mano si no hay fusil).
        Vector3 origen = fusil != null && fusil.puntoDisparo != null ? fusil.puntoDisparo.position
                       : visual != null ? visual.transform.position : cam.position + cam.rotation * posicionEnMano;
        Vector3 direccion = (cam.forward + Vector3.up * elevacion).normalized;

        GameObject proyectil = Instantiate(prefabMoneda, origen, Quaternion.LookRotation(direccion, cam.up));
        Moneda moneda = proyectil.GetComponent<Moneda>();
        if (moneda == null) moneda = proyectil.AddComponent<Moneda>();
        moneda.valor = valorMoneda;
        if (proyectil.GetComponent<ProyectilArma>() == null) proyectil.AddComponent<ProyectilArma>();

        Rigidbody rb = proyectil.GetComponent<Rigidbody>();
        if (rb == null) rb = proyectil.AddComponent<Rigidbody>();
        rb.linearVelocity = direccion * fuerzaLanzamiento;
        rb.AddRelativeTorque(new Vector3(0f, Random.Range(-8f, 8f), 0f), ForceMode.Impulse);

        bool delFusil = usarAjustesDelFusil && fusil != null;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        proyectil.AddComponent<CrecimientoProyectil>().Configurar(delFusil ? fusil.escalaMaxima : escalaProyectil,
                                                                  delFusil ? fusil.velocidadCrecimiento : velocidadCrecimiento);
        proyectil.AddComponent<ProteccionMagnetica>().Configurar(delFusil ? fusil.tiempoInmunidad : tiempoInmunidad);
        Destroy(proyectil, delFusil ? Mathf.Max(fusil.vidaProyectil, vidaProyectil) : vidaProyectil);

        proximoLanzamiento = Time.time + esperaEntreLanzamientos;

        // La moneda ha salido de la mano: desaparece de golpe y la siguiente subirá desde abajo.
        presencia = 0f;
        empujon = 1f;
    }

    // Copia solo las mallas del prefab de la moneda (sin física ni scripts) y la cuelga de la cámara,
    // girada para que se vea de cara (no de canto).
    private void CrearVisual()
    {
        BuscarReferencias();
        if (visual != null || prefabMoneda == null || camara == null) return;

        visual = new GameObject("MonedaEnMano");
        visual.transform.SetParent(camara.transform, false);
        visual.transform.localPosition = posicionEnMano;
        visual.transform.localScale = Vector3.one;
        rotacionCamaraAnterior = camara.transform.rotation;

        modeloVisual = new GameObject("Modelo").transform;
        modeloVisual.SetParent(visual.transform, false);

        Transform raiz = prefabMoneda.transform;
        Bounds limitesLocales = new Bounds();
        bool hayLimites = false;
        foreach (MeshFilter mf in prefabMoneda.GetComponentsInChildren<MeshFilter>(true))
        {
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mf.sharedMesh == null || mr == null) continue;

            // Límites de la moneda en el espacio del prefab, para saber cuál es su eje "fino" (la cara).
            Bounds b = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 esquina = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 local = raiz.InverseTransformPoint(mf.transform.TransformPoint(esquina));
                if (!hayLimites) { limitesLocales = new Bounds(local, Vector3.zero); hayLimites = true; }
                else limitesLocales.Encapsulate(local);
            }

            GameObject pieza = new GameObject(mf.name);
            pieza.transform.SetParent(modeloVisual, false);
            pieza.transform.localPosition = raiz.InverseTransformPoint(mf.transform.position);
            pieza.transform.localRotation = Quaternion.Inverse(raiz.rotation) * mf.transform.rotation;
            pieza.transform.localScale = mf.transform.lossyScale;
            pieza.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            MeshRenderer copia = pieza.AddComponent<MeshRenderer>();
            copia.sharedMaterials = mr.sharedMaterials;
            copia.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Centrada en la mano y con la cara (su eje más fino) mirando a la cámara.
        if (hayLimites)
        {
            Vector3 s = limitesLocales.size;
            Vector3 ejeFino = s.x <= s.y && s.x <= s.z ? Vector3.right : (s.y <= s.z ? Vector3.up : Vector3.forward);
            rotacionBaseModelo = Quaternion.FromToRotation(ejeFino, Vector3.back);
            modeloVisual.localRotation = rotacionBaseModelo;
            foreach (Transform pieza in modeloVisual)
                pieza.localPosition -= limitesLocales.center;
        }

        AjustarTamano();
        presencia = 0f;
        visual.SetActive(equipada);
    }

    // Escala la moneda de la mano para que mida 'tamanoEnMano' metros (el proyectil nace pequeño y crece al lanzarse).
    private void AjustarTamano()
    {
        Renderer[] piezas = visual.GetComponentsInChildren<Renderer>();
        if (piezas.Length == 0) return;
        Bounds limites = piezas[0].bounds;
        foreach (Renderer r in piezas) limites.Encapsulate(r.bounds);
        float mayor = Mathf.Max(limites.size.x, limites.size.y, limites.size.z);
        if (mayor > 0.0001f) visual.transform.localScale *= tamanoEnMano / mayor;
    }

    private void OnDisable()
    {
        if (visual != null) visual.SetActive(false);
    }
}
