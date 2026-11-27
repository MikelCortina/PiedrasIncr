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

    [Header("Moneda en la mano")]
    [Tooltip("Solo si no hay fusil en la escena: posición respecto a la cámara.")]
    public Vector3 posicionEnMano = new Vector3(0.28f, -0.24f, 0.55f);
    [Tooltip("Tamaño de la moneda en la mano, en metros (lo más ancho). No depende de la escala del prefab ni del padre.")]
    public float tamanoEnMano = 0.12f;

    private bool equipada;
    private GameObject visual;
    private Vector3 posicionVisual;
    private float proximoLanzamiento;

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
        if (visual != null)
        {
            // Sin monedas (o recargando) la mano se queda vacía.
            visual.SetActive(lista);
            visual.transform.localPosition = posicionVisual + Vector3.up * Mathf.Sin(Time.time * 2f) * 0.01f;
            visual.transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.Self);
        }

        if (Input.GetKeyDown(teclaLanzar) && Time.time >= proximoLanzamiento) Lanzar();
    }

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
    }

    // Copia solo las mallas del prefab de la moneda (sin física ni scripts) y la cuelga de la cámara.
    private void CrearVisual()
    {
        BuscarReferencias();
        if (visual != null || prefabMoneda == null || camara == null) return;

        visual = new GameObject("MonedaEnMano");
        // Donde va el modelo del fusil (mismo padre y misma posición); si no hay fusil, delante de la cámara.
        Transform modelo = fusil != null && fusil.modeloArma != null ? fusil.modeloArma.transform : null;
        if (modelo != null && modelo.parent != null)
        {
            visual.transform.SetParent(modelo.parent, false);
            posicionVisual = modelo.localPosition;
            visual.transform.localRotation = modelo.localRotation;
        }
        else
        {
            visual.transform.SetParent(camara.transform, false);
            posicionVisual = posicionEnMano;
        }
        visual.transform.localPosition = posicionVisual;
        visual.transform.localScale = Vector3.one;

        Transform raiz = prefabMoneda.transform;
        foreach (MeshFilter mf in prefabMoneda.GetComponentsInChildren<MeshFilter>(true))
        {
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mf.sharedMesh == null || mr == null) continue;

            GameObject pieza = new GameObject(mf.name);
            pieza.transform.SetParent(visual.transform, false);
            pieza.transform.localPosition = raiz.InverseTransformPoint(mf.transform.position);
            pieza.transform.localRotation = Quaternion.Inverse(raiz.rotation) * mf.transform.rotation;
            pieza.transform.localScale = mf.transform.lossyScale;
            pieza.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            MeshRenderer copia = pieza.AddComponent<MeshRenderer>();
            copia.sharedMaterials = mr.sharedMaterials;
            copia.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        AjustarTamano();
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
