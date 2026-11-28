using System.Collections;
using UnityEngine;

public class MeteoritoInicial : MonoBehaviour
{
    [Header("Referencias")]
    public LluviaMeteoritos lluviaMeteoritos;
    public GameObject prefabMeteorito;
    public Transform puntoSalidaLuna;
    [Tooltip("Solo se usa si 'Impactar Delante De Nave' está desactivado (o no se encuentra la nave).")]
    public Transform puntoImpacto;

    [Header("Impacto delante de la nave")]
    [Tooltip("Activado: cae delante de donde se para la nave (se calcula con su spline). Desactivado: cae en 'Punto Impacto'.")]
    public bool impactarDelanteDeNave = true;
    [Tooltip("La intro de la nave. Vacío = se busca sola.")]
    public CambioCamaraIntro intro;
    [Tooltip("Metros por delante del morro de la nave parada.")]
    [Min(0f)] public float distanciaDelanteNave = 12f;
    [Tooltip("Metros hacia la derecha de la nave (negativo = izquierda).")]
    public float desplazamientoLateral = 0f;

    [Header("Viaje desde la Luna")]
    [Min(0.1f)] public float duracionViaje = 2f;
    [Min(0.1f)] public float duracionDescenso = 2.5f;
    [Min(10f)] public float alturaEntrada = 90f;
    [Min(0f)] public float distanciaEntradaHorizontal = 40f;
    [Min(0f)] public float alturaArco = 12f;
    [Min(1f)] public float aceleracionDescenso = 1.7f;
    [Min(0f)] public float fuerzaGiro = 40f;

    [Header("Suelo")]
    public string tagSuelo = "Floor";
    [Min(1f)] public float alcanceBusquedaSuelo = 2000f;

    [Header("Depuracion")]
    public bool mostrarTrayectoria = true;
    [Range(8, 80)] public int segmentosTrayectoria = 40;
    [SerializeField] private bool meteoritoLanzado;

    [Header("Pruebas repetibles")]
    public bool repetirConTecla = true;
    public KeyCode teclaRepetir = KeyCode.F8;
    public bool repetirAutomaticamente = false;
    [Min(0.5f)] public float intervaloRepeticion = 6f;

    private Coroutine rutinaRepeticion;

    /// <summary>Última piedra lanzada por el meteorito inicial (la que cae delante de la nave).</summary>
    public static Rigidbody PiedraLanzada { get; private set; }

    /// <summary>Segundos desde que se lanza hasta que llega al suelo (CambioCamaraIntro lo usa para que caiga al aparcar).</summary>
    public float DuracionCaida => duracionViaje + duracionDescenso;
    public bool Lanzado => meteoritoLanzado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstatico()
    {
        PiedraLanzada = null;
    }

    private void Update()
    {
        if (repetirConTecla && Input.GetKeyDown(teclaRepetir))
            RepetirMeteorito();

        if (repetirAutomaticamente && meteoritoLanzado && rutinaRepeticion == null)
            rutinaRepeticion = StartCoroutine(RutinaRepeticion());
        else if (!repetirAutomaticamente && rutinaRepeticion != null)
        {
            StopCoroutine(rutinaRepeticion);
            rutinaRepeticion = null;
        }
    }

    private IEnumerator RutinaRepeticion()
    {
        while (repetirAutomaticamente)
        {
            yield return new WaitForSeconds(Mathf.Max(0.5f, intervaloRepeticion));

            if (repetirAutomaticamente)
                RepetirMeteorito();
        }

        rutinaRepeticion = null;
    }

    private void OnDisable()
    {
        if (rutinaRepeticion != null)
        {
            StopCoroutine(rutinaRepeticion);
            rutinaRepeticion = null;
        }
    }

    [ContextMenu("Lanzar meteorito inicial")]
    public void LanzarMeteorito()
    {
        // (Lo llama CambioCamaraIntro antes de aparcar, para que caiga justo al parar; SecuenciaIntro también si la intro se ha saltado.)
        if (!Application.isPlaying || meteoritoLanzado)
            return;

        CrearMeteorito();
    }

    [ContextMenu("Repetir meteorito de prueba")]
    public void RepetirMeteorito()
    {
        if (!Application.isPlaying)
            return;

        CrearMeteorito();
    }

    private void CrearMeteorito()
    {
        GameObject prefab = lluviaMeteoritos != null && lluviaMeteoritos.prefabPiedra != null
            ? lluviaMeteoritos.prefabPiedra
            : prefabMeteorito;

        Vector3 impactoDeseado;
        if (prefab == null || puntoSalidaLuna == null || !ObtenerPuntoImpacto(out impactoDeseado))
        {
            Debug.LogError("[METEORITO INICIAL] Faltan el prefab, la salida de la Luna o el punto de impacto.");
            return;
        }

        Vector3 origen = puntoSalidaLuna.position;
        Vector3 suelo = ObtenerPosicionSuelo(impactoDeseado);
        GameObject meteorito = Instantiate(prefab, origen, Random.rotation);
        Rigidbody rb = meteorito.GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("[METEORITO INICIAL] El prefab asignado no tiene Rigidbody en la raiz.", meteorito);
            Destroy(meteorito);
            return;
        }

        GeneradorPiedra generador = meteorito.GetComponent<GeneradorPiedra>();
        if (generador != null)
            generador.Generar();

        Collider colision = meteorito.GetComponent<Collider>();
        float radio = colision != null ? Mathf.Max(0.05f, colision.bounds.extents.y) : 0.5f;
        Vector3 destino = suelo + Vector3.up * radio;
        Vector3 entrada = ObtenerPuntoEntrada(destino);

        MeteoritoVisual visual = meteorito.GetComponent<MeteoritoVisual>();
        if (visual != null)
            visual.enabled = false;

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.isKinematic = true;

        PrepararEfectosCaida(meteorito, visual);

        meteoritoLanzado = true;
        PiedraLanzada = rb;
        StartCoroutine(SecuenciaCaida(rb, meteorito, visual, origen, entrada, destino, radio));
        Debug.Log("[METEORITO INICIAL] Salida Luna: " + origen + " | Entrada atmosfera: " + entrada + " | Impacto: " + destino);
    }

    private IEnumerator SecuenciaCaida(Rigidbody rb, GameObject meteorito, MeteoritoVisual visual, Vector3 origen, Vector3 entrada, Vector3 destino, float radio)
    {
        float tiempo = 0f;
        Quaternion rotacion = rb.rotation;
        Vector3 velocidadAngular = Random.onUnitSphere * fuerzaGiro;

        while (rb != null && tiempo < duracionViaje)
        {
            tiempo += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(tiempo / duracionViaje);
            float avance = t * t * (3f - 2f * t);
            rb.MovePosition(Vector3.LerpUnclamped(origen, entrada, avance));
            rotacion = Quaternion.Euler(velocidadAngular * Time.fixedDeltaTime) * rotacion;
            rb.MoveRotation(rotacion);
            yield return new WaitForFixedUpdate();
        }

        if (rb == null)
            yield break;

        rb.position = entrada;
        if (visual != null)
        {
            visual.ConfigurarAlturas(entrada.y, destino.y);
            visual.enabled = true;
        }

        tiempo = 0f;
        Vector3 control = ObtenerPuntoControl(entrada, destino);

        while (rb != null && tiempo < duracionDescenso)
        {
            tiempo += Time.fixedDeltaTime;
            float progreso = Mathf.Clamp01(tiempo / duracionDescenso);
            float t = Mathf.Pow(progreso, aceleracionDescenso);
            Vector3 siguiente = CalcularBezier(entrada, control, destino, t);
            Vector3 desplazamiento = siguiente - rb.position;

            if (desplazamiento.sqrMagnitude > 0.000001f)
            {
                Vector3 direccion = desplazamiento.normalized;
                float distancia = desplazamiento.magnitude;
                RaycastHit[] impactos = Physics.SphereCastAll(rb.position, radio * 0.85f, direccion, distancia + 0.05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

                foreach (RaycastHit impacto in impactos)
                {
                    if (impacto.collider != null && impacto.collider.CompareTag(tagSuelo))
                    {
                        Vector3 centroImpacto = impacto.point + impacto.normal * radio;
                        FinalizarImpacto(rb, visual, centroImpacto, entrada.y);
                        yield break;
                    }
                }

                rb.MovePosition(siguiente);
            }

            rotacion = Quaternion.Euler(velocidadAngular * Time.fixedDeltaTime) * rotacion;
            rb.MoveRotation(rotacion);
            yield return new WaitForFixedUpdate();
        }

        if (rb != null)
            FinalizarImpacto(rb, visual, destino, entrada.y);
    }

    private void FinalizarImpacto(Rigidbody rb, MeteoritoVisual visual, Vector3 destino, float alturaInicial)
    {
        rb.position = destino;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.down * 4f;

        if (visual != null)
        {
            visual.ConfigurarAlturas(alturaInicial, destino.y + 0.05f);
            visual.enabled = true;
        }

        Debug.Log("[METEORITO INICIAL] Llegada al suelo: " + destino);
    }

    // Delante de donde se para la nave; si no se puede calcular, el 'Punto Impacto' de la escena.
    private bool ObtenerPuntoImpacto(out Vector3 punto)
    {
        if (impactarDelanteDeNave)
        {
            if (intro == null && Application.isPlaying)
                intro = FindFirstObjectByType<CambioCamaraIntro>();

            Vector3 posicionNave, frente;
            if (intro != null && intro.ObtenerPoseParadaNave(out posicionNave, out frente))
            {
                Vector3 derecha = Vector3.Cross(Vector3.up, frente);
                punto = posicionNave + frente * distanciaDelanteNave + derecha * desplazamientoLateral;
                return true;
            }
        }

        punto = puntoImpacto != null ? puntoImpacto.position : Vector3.zero;
        return puntoImpacto != null;
    }

    private Vector3 ObtenerPosicionSuelo(Vector3 posicion)
    {
        Vector3 origenRaycast = posicion + Vector3.up * (alcanceBusquedaSuelo * 0.5f);
        RaycastHit[] impactos = Physics.RaycastAll(origenRaycast, Vector3.down, alcanceBusquedaSuelo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        bool encontrado = false;
        float menorDistancia = float.MaxValue;
        Vector3 suelo = posicion;

        foreach (RaycastHit impacto in impactos)
        {
            if (impacto.collider == null || !impacto.collider.CompareTag(tagSuelo))
                continue;

            float diferencia = Mathf.Abs(impacto.point.y - posicion.y);
            if (diferencia < menorDistancia)
            {
                menorDistancia = diferencia;
                suelo = impacto.point;
                encontrado = true;
            }
        }

        if (!encontrado)
            Debug.LogWarning("[METEORITO INICIAL] No encuentro un Collider con tag " + tagSuelo + " bajo el punto de impacto. Utilizare su posicion actual.");

        return suelo;
    }

    private Vector3 ObtenerPuntoEntrada(Vector3 destino)
    {
        Vector3 direccion = destino - puntoSalidaLuna.position;
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.001f)
            direccion = Vector3.forward;

        return destino - direccion.normalized * distanciaEntradaHorizontal + Vector3.up * alturaEntrada;
    }

    private Vector3 ObtenerPuntoControl(Vector3 entrada, Vector3 destino)
    {
        float arcoSeguro = Mathf.Min(alturaArco, Mathf.Max(0f, entrada.y - destino.y) * 0.4f);
        return Vector3.Lerp(entrada, destino, 0.5f) + Vector3.up * arcoSeguro;
    }

    private static Vector3 CalcularBezier(Vector3 origen, Vector3 control, Vector3 destino, float t)
    {
        float u = 1f - t;
        return u * u * origen + 2f * u * t * control + t * t * destino;
    }

    private static void PrepararEfectosCaida(GameObject meteorito, MeteoritoVisual visual)
    {
        if (visual != null && visual.estelasMeteorito != null)
        {
            foreach (TrailRenderer estela in visual.estelasMeteorito)
            {
                if (estela == null)
                    continue;

                ActivarPadres(estela.transform, meteorito.transform);
                estela.enabled = true;
                estela.Clear();
                estela.emitting = true;
            }
        }

        ParticleSystem[] particulas = meteorito.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particula in particulas)
        {
            if (particula == null || !particula.main.loop)
                continue;

            bool esParticulaDeImpacto = false;
            if (visual != null && visual.particulasImpacto != null)
            {
                foreach (ParticleSystem efectoImpacto in visual.particulasImpacto)
                {
                    if (efectoImpacto != null && particula.transform.IsChildOf(efectoImpacto.transform))
                    {
                        esParticulaDeImpacto = true;
                        break;
                    }
                }
            }

            if (esParticulaDeImpacto)
                continue;

            ActivarPadres(particula.transform, meteorito.transform);
            if (!particula.isPlaying)
                particula.Play(false);
        }
    }

    private static void ActivarPadres(Transform efecto, Transform raiz)
    {
        Transform actual = efecto;
        while (actual != null && actual != raiz)
        {
            if (!actual.gameObject.activeSelf)
                actual.gameObject.SetActive(true);
            actual = actual.parent;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 destino;
        if (!mostrarTrayectoria || puntoSalidaLuna == null || !ObtenerPuntoImpacto(out destino))
            return;

        Vector3 origen = puntoSalidaLuna.position;
        Vector3 entrada = ObtenerPuntoEntrada(destino);
        Vector3 control = ObtenerPuntoControl(entrada, destino);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origen, entrada);
        Gizmos.DrawSphere(origen, 1f);

        Gizmos.color = Color.yellow;
        Vector3 anterior = entrada;
        for (int i = 1; i <= segmentosTrayectoria; i++)
        {
            float t = i / (float)segmentosTrayectoria;
            Vector3 siguiente = CalcularBezier(entrada, control, destino, t);
            Gizmos.DrawLine(anterior, siguiente);
            anterior = siguiente;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(destino, 1.5f);
    }
}
