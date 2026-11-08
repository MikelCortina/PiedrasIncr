
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(BotRecolector))]
public class AranaPateable : MonoBehaviour
{
    [Header("Patadas")]
    public bool permitirPatadas = true;
    public Camera camaraJugador;
    public float alcancePatada = 4f;
    [Range(0.5f, 0.99f)] public float precisionMirada = 0.75f;
    public float tiempoEntrePatadas = 1f;
    public bool comprobarObstaculos = false;

    [Header("Tipos de caida")]
    public bool permitirCaidasLaterales = true;
    [Range(0.4f, 1.5f)] public float predominioLateral = 0.85f;
    public float anguloCaidaLateral = 93f;
    public float torsionImpacto = 12f;

    [Header("Reaccion inicial")]
    public float duracionReaccion = 0.13f;
    public float inclinacionGolpe = 18f;
    public float aplastamientoGolpe = 0.12f;

    [Header("Voltereta")]
    public float duracionVoltereta = 0.7f;
    public float alturaVoltereta = 1.1f;
    public float desplazamientoPatada = 0.4f;

    [Header("En el suelo")]
    public float tiempoBocaArriba = 1.8f;
    public float balanceoBocaArriba = 10f;
    public float amplitudPatas = 38f;
    public float frecuenciaPatas = 5.5f;
    public float pequenosSaltos = 0.045f;

    [Header("Salto de recuperacion")]
    public float duracionPreparacionSalto = 0.18f;
    public float duracionRecuperacion = 0.65f;
    public float alturaSalto = 0.95f;

    [Header("Aterrizaje")]
    public float duracionAterrizaje = 0.4f;
    public float alturaRebote = 0.13f;
    public float aplastamientoAterrizaje = 0.16f;
    public float tambaleoFinal = 12f;

    [Header("Personalidad")]
    [Range(0f, 1f)] public float probabilidadEnfado = 0.65f;
    public float duracionEnfado = 1.4f;
    public float amplitudAmenaza = 45f;
    public float giroMaximoMirada = 35f;
    public float duracionMareo = 1.1f;

    [Header("Efectos opcionales")]
    public AudioClip sonidoGolpe;
    public AudioClip sonidoCaida;
    public AudioClip sonidoRecuperacion;
    public AudioClip sonidoEnfado;
    public ParticleSystem particulasGolpe;
    public ParticleSystem particulasAterrizaje;
    [Range(0f, 1f)] public float volumen = 0.8f;

    [Header("Diagnostico")]
    [SerializeField] private string ultimaCaida = "Ninguna";
    [SerializeField] private string ultimaReaccion = "Ninguna";

    private static readonly FieldInfo campoEstado =
        typeof(BotRecolector).GetField(
            "estadoActual",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

    private static readonly FieldInfo campoInteraccion =
        typeof(BotRecolector).GetField(
            "enInteraccion",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

    private BotRecolector recolector;
    private NavMeshAgent agente;
    private GestorPatasArana gestor;
    private PataProcedural[] patas;
    private Transform visual;
    private MonoBehaviour balanceoCuerpo;

    private bool animando;
    private bool recolectorActivo;
    private bool gestorActivo;
    private bool balanceoActivo;
    private bool agenteDetenido;
    private bool[] patasActivas;

    private Vector3 posicionOriginal;
    private Quaternion rotacionOriginal;
    private Vector3 escalaOriginal;
    private Vector3 centroLocal;
    private Vector3 centroPadre;
    private Vector3 direccionPatada;
    private Vector3 ejeCaidaLocal;

    private float escalaBot;
    private float relojAnimacion;
    private float ultimaPatada = -999f;
    private float anguloFinalCaida;
    private float anguloFinalRecuperacion;
    private float sentidoBalanceo;
    private bool caidaLateral;

    private readonly List<EstadoTransform> estados =
        new List<EstadoTransform>();

    private DatosPata[] datosPatas;

    private struct EstadoTransform
    {
        public Transform objeto;
        public Vector3 posicion;
        public Quaternion rotacion;
        public Vector3 escala;
    }

    private struct DatosPata
    {
        public Transform cadera;
        public Transform rodilla;
        public Transform pie;
        public Transform superior;
        public Transform inferior;
        public Vector3 caderaLocal;
        public Vector3 rodillaLocal;
        public Vector3 pieLocal;
    }

    private void Awake()
    {
        recolector = GetComponent<BotRecolector>();
        agente = GetComponent<NavMeshAgent>();

        gestor = GetComponentInChildren<GestorPatasArana>(true);
        patas = GetComponentsInChildren<PataProcedural>(true);

        visual = transform.Find("SpiderVisual");

        if (visual == null && recolector != null)
            visual = recolector.spiderVisual;

        if (visual == null && gestor != null)
            visual = gestor.transform;

        foreach (MonoBehaviour componente in
            GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (componente != null &&
                componente.GetType().Name == "BalanceoCuerpoArana")
            {
                balanceoCuerpo = componente;
                break;
            }
        }
    }

    private void Update()
    {
        if (!permitirPatadas || animando || !TeclaPulsada())
            return;

        Camera camara = camaraJugador != null
            ? camaraJugador
            : Camera.main;

        if (camara == null)
            return;

        AranaPateable objetivo = BuscarObjetivo(camara);

        if (objetivo == this)
            Patear(camara.transform.forward);
    }

    private bool TeclaPulsada()
    {
        bool pulsada = false;

#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            pulsada |= UnityEngine.InputSystem.Keyboard.current
                .kKey.wasPressedThisFrame;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        pulsada |= Input.GetKeyDown(KeyCode.K);
#endif

        return pulsada;
    }

    private AranaPateable BuscarObjetivo(Camera camara)
    {
        AranaPateable mejor = null;
        float mejorDistancia = float.MaxValue;

        AranaPateable[] candidatas =
            FindObjectsByType<AranaPateable>(
                FindObjectsSortMode.None
            );

        foreach (AranaPateable candidata in candidatas)
        {
            if (candidata == null ||
                !candidata.permitirPatadas ||
                candidata.animando ||
                !candidata.isActiveAndEnabled)
                continue;

            Vector3 punto = candidata.visual != null
                ? candidata.visual.position
                : candidata.transform.position;

            punto += Vector3.up *
                Mathf.Max(
                    0.15f,
                    Mathf.Abs(candidata.transform.lossyScale.y) * 0.4f
                );

            Vector3 direccion = punto - camara.transform.position;
            float distancia = direccion.magnitude;

            if (distancia < 0.01f || distancia > alcancePatada)
                continue;

            float alineacion = Vector3.Dot(
                camara.transform.forward,
                direccion.normalized
            );

            if (alineacion < precisionMirada)
                continue;

            if (comprobarObstaculos &&
                Physics.Raycast(
                    camara.transform.position,
                    direccion.normalized,
                    out RaycastHit impacto,
                    distancia,
                    ~0,
                    QueryTriggerInteraction.Ignore))
            {
                AranaPateable encontrada =
                    impacto.collider.GetComponentInParent<AranaPateable>();

                if (encontrada != candidata)
                    continue;
            }

            if (distancia < mejorDistancia)
            {
                mejor = candidata;
                mejorDistancia = distancia;
            }
        }

        return mejor;
    }

    [ContextMenu("PROBAR PATADA FRONTAL")]
    public void ProbarPatadaFrontal()
    {
        if (Application.isPlaying)
            Patear(-transform.forward);
    }

    [ContextMenu("PROBAR PATADA TRASERA")]
    public void ProbarPatadaTrasera()
    {
        if (Application.isPlaying)
            Patear(transform.forward);
    }

    [ContextMenu("PROBAR PATADA IZQUIERDA")]
    public void ProbarPatadaIzquierda()
    {
        if (Application.isPlaying)
            Patear(transform.right);
    }

    [ContextMenu("PROBAR PATADA DERECHA")]
    public void ProbarPatadaDerecha()
    {
        if (Application.isPlaying)
            Patear(-transform.right);
    }

    [ContextMenu("PROBAR PATADA")]
    public void ProbarPatada()
    {
        if (Application.isPlaying)
            Patear(transform.forward);
    }

    public void Patear(Vector3 direccion)
    {
        IntentarPatear(direccion);
    }

    public bool IntentarPatear(Vector3 direccion)
    {
        if (!isActiveAndEnabled ||
            !permitirPatadas || animando)
            return false;

        if (Time.time - ultimaPatada < tiempoEntrePatadas)
            return false;

        if (!PuedeRecibirPatada())
            return false;

        direccionPatada = Vector3.ProjectOnPlane(
            direccion,
            Vector3.up
        ).normalized;

        if (direccionPatada.sqrMagnitude < 0.001f)
            direccionPatada = transform.forward;

        CalcularCaida();

        ultimaPatada = Time.time;
        animando = true;

        StartCoroutine(SecuenciaCompleta());
        return true;
    }

    private bool PuedeRecibirPatada()
    {
        if (recolector == null ||
            agente == null ||
            visual == null ||
            visual.parent == null ||
            !recolector.enabled ||
            !agente.enabled ||
            !agente.isOnNavMesh)
        {
            Debug.LogWarning(
                "[PATADA] Araña no preparada: " + name
            );
            return false;
        }

        if (patas == null || patas.Length != 6)
        {
            Debug.LogWarning(
                "[PATADA] Se necesitan seis patas: " + name
            );
            return false;
        }

        foreach (PataProcedural pata in patas)
        {
            if (pata == null ||
                pata.cadera == null ||
                pata.rodilla == null ||
                pata.pie == null)
            {
                Debug.LogWarning(
                    "[PATADA] Referencias incompletas: " + name
                );
                return false;
            }
        }

        if (campoEstado == null)
        {
            Debug.LogWarning(
                "[PATADA] No se puede consultar el estado del bot."
            );
            return false;
        }

        BotRecolector.EstadoBot estado =
            (BotRecolector.EstadoBot)campoEstado.GetValue(recolector);

        bool estadoSeguro =
            estado == BotRecolector.EstadoBot.Buscando ||
            estado == BotRecolector.EstadoBot.Esperando ||
            estado == BotRecolector.EstadoBot.YendoAPiedra;

        if (!estadoSeguro)
        {
            Debug.Log(
                "[PATADA] Bloqueada durante: " + estado
            );
            return false;
        }

        if (campoInteraccion != null &&
            (bool)campoInteraccion.GetValue(recolector))
        {
            return false;
        }

        if (recolector.puntoAgarre != null &&
            recolector.puntoAgarre
                .GetComponentInChildren<Rigidbody>(true) != null)
        {
            Debug.Log(
                "[PATADA] Bloqueada mientras transporta una piedra."
            );
            return false;
        }

        return true;
    }


    private void CalcularCaida()
    {
        Vector3 local =
            transform.InverseTransformDirection(direccionPatada);

        local.y = 0f;
        local.Normalize();

        caidaLateral =
            permitirCaidasLaterales &&
            Mathf.Abs(local.x) >
            Mathf.Abs(local.z) * predominioLateral;

        ejeCaidaLocal = Vector3.Cross(
            Vector3.up,
            local
        ).normalized;

        if (ejeCaidaLocal.sqrMagnitude < 0.001f)
            ejeCaidaLocal = Vector3.right;

        sentidoBalanceo = local.x >= 0f ? 1f : -1f;

        anguloFinalCaida = 180f;
        anguloFinalRecuperacion = 360f;

        if (caidaLateral)
        {
            ultimaCaida = local.x >= 0f
                ? "Lateral derecho"
                : "Lateral izquierdo";
        }
        else
        {
            ultimaCaida = local.z >= 0f
                ? "Voltereta hacia delante"
                : "Voltereta hacia atras";
        }

        Debug.Log("[PATADA] " + name + " | " + ultimaCaida);
    }


    private IEnumerator SecuenciaCompleta()
    {
        PrepararAnimacion();

        ReproducirSonido(sonidoGolpe);
        EmitirParticulas(particulasGolpe);

        float tiempo = 0f;

        while (tiempo < duracionReaccion)
        {
            tiempo += Time.deltaTime;
            relojAnimacion += Time.deltaTime;

            float t = Mathf.Clamp01(
                tiempo / Mathf.Max(0.01f, duracionReaccion)
            );

            float s = Suavizar(t);

            MoverVisual(
                inclinacionGolpe * s,
                0f,
                0f,
                -0.04f * Mathf.Sin(t * Mathf.PI),
                -0.1f * s,
                aplastamientoGolpe * Mathf.Sin(t * Mathf.PI)
            );

            AgitarPatas(relojAnimacion, 0.3f * s, false);
            yield return null;
        }

        tiempo = 0f;

        while (tiempo < duracionVoltereta)
        {
            tiempo += Time.deltaTime;
            relojAnimacion += Time.deltaTime;

            float t = Mathf.Clamp01(
                tiempo / Mathf.Max(0.01f, duracionVoltereta)
            );

            float s = Suavizar(t);

            float angulo = Mathf.Lerp(
                inclinacionGolpe,
                anguloFinalCaida,
                s
            );

            float altura =
                Mathf.Sin(t * Mathf.PI) * alturaVoltereta +
                0.08f * s;

            float torsion =
                Mathf.Sin(t * Mathf.PI) * torsionImpacto;

            MoverVisual(
                angulo,
                torsion * sentidoBalanceo,
                torsion * 0.35f,
                altura,
                Mathf.Lerp(-0.1f, desplazamientoPatada, s),
                0f
            );

            AgitarPatas(
                relojAnimacion,
                Mathf.Lerp(0.4f, 1.2f, s),
                false
            );

            yield return null;
        }

        ReproducirSonido(sonidoCaida);

        tiempo = 0f;

        while (tiempo < tiempoBocaArriba)
        {
            tiempo += Time.deltaTime;
            relojAnimacion += Time.deltaTime;

            float oscilacion =
                Mathf.Sin(tiempo * 9f) *
                balanceoBocaArriba;

            float altura =
                0.08f +
                Mathf.Abs(Mathf.Sin(tiempo * 12f)) *
                pequenosSaltos;

            MoverVisual(
                anguloFinalCaida +
                    oscilacion * (caidaLateral ? 0.5f : 1f),
                oscilacion * 0.5f,
                Mathf.Sin(tiempo * 5f) * 4f,
                altura,
                desplazamientoPatada,
                0f
            );

            AgitarPatas(
                relojAnimacion,
                0.9f + 0.3f * Mathf.Sin(tiempo * 3f),
                false
            );

            yield return null;
        }

        tiempo = 0f;

        while (tiempo < duracionPreparacionSalto)
        {
            tiempo += Time.deltaTime;
            relojAnimacion += Time.deltaTime;

            float t = Mathf.Clamp01(
                tiempo / Mathf.Max(0.01f, duracionPreparacionSalto)
            );

            MoverVisual(
                anguloFinalCaida,
                0f,
                0f,
                Mathf.Lerp(0.08f, 0.02f, Suavizar(t)),
                desplazamientoPatada,
                0.12f * Suavizar(t)
            );

            AgitarPatas(relojAnimacion, 1.4f, false);
            yield return null;
        }

        ReproducirSonido(sonidoRecuperacion);

        tiempo = 0f;

        while (tiempo < duracionRecuperacion)
        {
            tiempo += Time.deltaTime;
            relojAnimacion += Time.deltaTime;

            float t = Mathf.Clamp01(
                tiempo / Mathf.Max(0.01f, duracionRecuperacion)
            );

            float s = Suavizar(t);

            MoverVisual(
                Mathf.Lerp(
                    anguloFinalCaida,
                    anguloFinalRecuperacion,
                    s
                ),
                Mathf.Sin(t * Mathf.PI) * -8f,
                0f,
                Mathf.Sin(t * Mathf.PI) * alturaSalto,
                Mathf.Lerp(desplazamientoPatada, 0f, s),
                0.12f * (1f - s)
            );

            AgitarPatas(
                relojAnimacion,
                1.3f * (1f - s),
                false
            );

            yield return null;
        }

        EmitirParticulas(particulasAterrizaje);

        tiempo = 0f;

        while (tiempo < duracionAterrizaje)
        {
            tiempo += Time.deltaTime;
            relojAnimacion += Time.deltaTime;

            float t = Mathf.Clamp01(
                tiempo / Mathf.Max(0.01f, duracionAterrizaje)
            );

            float cabeceo =
                Mathf.Sin(t * Mathf.PI * 4f) *
                tambaleoFinal *
                (1f - t) * (1f - t);

            MoverVisual(
                anguloFinalRecuperacion + cabeceo,
                -cabeceo * 0.35f,
                0f,
                Mathf.Sin(t * Mathf.PI) *
                    alturaRebote * (1f - t),
                0f,
                Mathf.Sin(t * Mathf.PI) *
                    aplastamientoAterrizaje
            );

            AgitarPatas(
                relojAnimacion,
                0.25f * (1f - t),
                false
            );

            yield return null;
        }

        if (UnityEngine.Random.value < probabilidadEnfado)
            yield return ReaccionEnfadada();
        else
            yield return ReaccionMareada();

        RestaurarAnimacion();
    }

    private IEnumerator ReaccionEnfadada()
    {
        ultimaReaccion = "Enfadada";

        ReproducirSonido(sonidoEnfado);

        Camera camara = camaraJugador != null
            ? camaraJugador
            : Camera.main;

        float giroJugador = 0f;

        if (camara != null)
        {
            Vector3 haciaJugador = Vector3.ProjectOnPlane(
                camara.transform.position - transform.position,
                Vector3.up
            );

            if (haciaJugador.sqrMagnitude > 0.001f)
            {
                giroJugador = Mathf.Clamp(
                    Vector3.SignedAngle(
                        transform.forward,
                        haciaJugador,
                        Vector3.up
                    ),
                    -giroMaximoMirada,
                    giroMaximoMirada
                );
            }
        }

        float tiempo = 0f;

        while (tiempo < duracionEnfado)
        {
            tiempo += Time.deltaTime;
            relojAnimacion += Time.deltaTime;

            float t = Mathf.Clamp01(
                tiempo / Mathf.Max(0.01f, duracionEnfado)
            );

            float intensidad = Mathf.Sin(t * Mathf.PI);

            float inclinacion =
                Mathf.Sin(tiempo * 13f) *
                7f * intensidad;

            MoverVisual(
                inclinacion,
                Mathf.Sin(tiempo * 10f) * 4f * intensidad,
                giroJugador * intensidad,
                0.025f * Mathf.Abs(Mathf.Sin(tiempo * 13f)),
                0f,
                0f
            );

            float amplitudAnterior = amplitudPatas;

            amplitudPatas = amplitudAmenaza;

            AgitarPatas(
                relojAnimacion,
                1.25f * intensidad,
                true
            );

            amplitudPatas = amplitudAnterior;

            yield return null;
        }
    }

    private IEnumerator ReaccionMareada()
    {
        ultimaReaccion = "Mareada";

        float tiempo = 0f;

        while (tiempo < duracionMareo)
        {
            tiempo += Time.deltaTime;
            relojAnimacion += Time.deltaTime;

            float t = Mathf.Clamp01(
                tiempo / Mathf.Max(0.01f, duracionMareo)
            );

            float intensidad = 1f - Suavizar(t);

            float inclinacion =
                Mathf.Sin(tiempo * 8f) *
                14f * intensidad;

            MoverVisual(
                inclinacion,
                Mathf.Cos(tiempo * 6f) * 11f * intensidad,
                Mathf.Sin(tiempo * 4f) * 9f * intensidad,
                0f,
                0f,
                0f
            );

            AgitarPatas(
                relojAnimacion,
                0.2f * intensidad,
                false
            );

            yield return null;
        }
    }

    private void PrepararAnimacion()
    {
        recolectorActivo = recolector.enabled;
        gestorActivo = gestor != null && gestor.enabled;
        agenteDetenido = agente.isStopped;

        balanceoActivo =
            balanceoCuerpo != null &&
            balanceoCuerpo.enabled;

        posicionOriginal = visual.localPosition;
        rotacionOriginal = visual.localRotation;
        escalaOriginal = visual.localScale;

        escalaBot = Mathf.Max(
            0.01f,
            Mathf.Abs(transform.lossyScale.x)
        );

        relojAnimacion = 0f;
        centroLocal = Vector3.zero;

        estados.Clear();

        patasActivas = new bool[patas.Length];
        datosPatas = new DatosPata[patas.Length];

        for (int i = 0; i < patas.Length; i++)
        {
            PataProcedural pata = patas[i];

            patasActivas[i] = pata.enabled;

            DatosPata datos = new DatosPata
            {
                cadera = pata.cadera,
                rodilla = pata.rodilla,
                pie = pata.pie,
                superior = pata.upperLeg,
                inferior = pata.lowerLeg
            };

            datos.caderaLocal =
                visual.InverseTransformPoint(datos.cadera.position);

            datos.rodillaLocal =
                visual.InverseTransformPoint(datos.rodilla.position);

            datos.pieLocal =
                visual.InverseTransformPoint(datos.pie.position);

            centroLocal += datos.caderaLocal;
            datosPatas[i] = datos;

            GuardarTransform(datos.rodilla);
            GuardarTransform(datos.pie);
            GuardarTransform(datos.superior);
            GuardarTransform(datos.inferior);
        }

        centroLocal /= patas.Length;

        centroPadre =
            visual.parent.InverseTransformPoint(
                visual.TransformPoint(centroLocal)
            );

        agente.isStopped = true;
        recolector.enabled = false;

        if (gestor != null)
            gestor.enabled = false;

        if (balanceoCuerpo != null)
            balanceoCuerpo.enabled = false;

        foreach (PataProcedural pata in patas)
            pata.enabled = false;
    }

    private void GuardarTransform(Transform objeto)
    {
        if (objeto == null)
            return;

        foreach (EstadoTransform estado in estados)
        {
            if (estado.objeto == objeto)
                return;
        }

        estados.Add(new EstadoTransform
        {
            objeto = objeto,
            posicion = objeto.localPosition,
            rotacion = objeto.localRotation,
            escala = objeto.localScale
        });
    }

    private void MoverVisual(
        float angulo,
        float oscilacionZ,
        float oscilacionY,
        float altura,
        float avance,
        float compresion)
    {
        compresion = Mathf.Clamp(compresion, 0f, 0.3f);

        visual.localScale = Vector3.Scale(
            escalaOriginal,
            new Vector3(
                1f + compresion * 0.45f,
                1f - compresion,
                1f + compresion * 0.45f
            )
        );

        Quaternion giroPrincipal =
            Quaternion.AngleAxis(angulo, ejeCaidaLocal);

        Quaternion giroSecundario =
            Quaternion.Euler(
                0f,
                oscilacionY,
                oscilacionZ
            );

        Quaternion rotacion =
            rotacionOriginal *
            giroPrincipal *
            giroSecundario;

        Vector3 desplazamientoMundo =
            Vector3.up * altura * escalaBot +
            direccionPatada * avance * escalaBot;

        Vector3 desplazamientoLocal =
            visual.parent.InverseTransformVector(
                desplazamientoMundo
            );

        Vector3 centroEscalado =
            Vector3.Scale(
                centroLocal,
                visual.localScale
            );

        visual.localRotation = rotacion;

        visual.localPosition =
            centroPadre +
            desplazamientoLocal -
            rotacion * centroEscalado;
    }

    private void AgitarPatas(
        float tiempo,
        float intensidad,
        bool soloDelanteras)
    {
        for (int i = 0; i < datosPatas.Length; i++)
        {
            DatosPata pata = datosPatas[i];

            bool esDelantera = gestor != null
                ? patas[i] == gestor.pataFL ||
                  patas[i] == gestor.pataFR
                : i < 2;

            float fuerza =
                soloDelanteras && !esDelantera
                ? 0f
                : intensidad;

            float fase =
                tiempo * frecuenciaPatas * Mathf.PI * 2f +
                i * 1.73f;

            float ondaPrincipal = Mathf.Sin(fase);
            float ondaSecundaria = Mathf.Sin(fase * 1.47f + i);
            float ondaRodilla = Mathf.Cos(fase * 1.28f);

            float giroSuperior =
                (ondaPrincipal * 0.7f +
                 ondaSecundaria * 0.3f) *
                amplitudPatas * fuerza;

            float giroInferior =
                ondaRodilla *
                amplitudPatas * 0.72f * fuerza;

            Quaternion giroCadera =
                Quaternion.AngleAxis(
                    giroSuperior,
                    Vector3.forward
                );

            Quaternion giroRodilla =
                Quaternion.AngleAxis(
                    giroInferior,
                    Vector3.right
                );

            Vector3 rodillaLocal =
                pata.caderaLocal +
                giroCadera *
                (pata.rodillaLocal - pata.caderaLocal);

            Vector3 pieLocal =
                rodillaLocal +
                giroCadera *
                giroRodilla *
                (pata.pieLocal - pata.rodillaLocal);

            Vector3 caderaMundo =
                visual.TransformPoint(pata.caderaLocal);

            Vector3 rodillaMundo =
                visual.TransformPoint(rodillaLocal);

            Vector3 pieMundo =
                visual.TransformPoint(pieLocal);

            pata.rodilla.position = rodillaMundo;
            pata.pie.position = pieMundo;

            ColocarSegmento(
                pata.superior,
                caderaMundo,
                rodillaMundo
            );

            ColocarSegmento(
                pata.inferior,
                rodillaMundo,
                pieMundo
            );
        }
    }

    private void ColocarSegmento(
        Transform segmento,
        Vector3 inicio,
        Vector3 final)
    {
        if (segmento == null)
            return;

        Vector3 direccion = final - inicio;

        if (direccion.sqrMagnitude < 0.0001f)
            return;

        Vector3 ejeY = direccion.normalized;

        Vector3 ejeZ = Vector3.ProjectOnPlane(
            visual.forward,
            ejeY
        );

        if (ejeZ.sqrMagnitude < 0.0001f)
            ejeZ = Vector3.ProjectOnPlane(visual.right, ejeY);

        if (ejeZ.sqrMagnitude < 0.0001f)
            return;

        segmento.position = (inicio + final) * 0.5f;

        segmento.rotation =
            Quaternion.LookRotation(
                ejeZ.normalized,
                ejeY
            );
    }

    private static float Suavizar(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private void ReproducirSonido(AudioClip clip)
    {
        if (clip == null)
            return;

        AudioSource fuente = GetComponent<AudioSource>();

        if (fuente != null)
            fuente.PlayOneShot(clip, volumen);
        else
            AudioSource.PlayClipAtPoint(
                clip,
                transform.position,
                volumen
            );
    }

    private void EmitirParticulas(ParticleSystem efecto)
    {
        if (efecto != null)
            efecto.Play();
    }

    private void RestaurarAnimacion()
    {
        if (!animando)
            return;

        ConfiguracionBot configuracion =
            GetComponent<ConfiguracionBot>();

        bool pausado =
            configuracion != null &&
            configuracion.modoActual ==
                ConfiguracionBot.ModoBot.Pausa;

        bool trabajando =
            configuracion != null &&
            configuracion.modoActual ==
                ConfiguracionBot.ModoBot.Trabajar;

        bool enParking =
            configuracion != null &&
            configuracion.modoActual ==
                ConfiguracionBot.ModoBot.Parking;

        bool enInteraccionActual =
            recolector != null &&
            campoInteraccion != null &&
            (bool)campoInteraccion.GetValue(recolector);

        if (visual != null)
        {
            visual.localPosition = posicionOriginal;
            visual.localRotation = rotacionOriginal;
            visual.localScale = escalaOriginal;
        }

        foreach (EstadoTransform estado in estados)
        {
            if (estado.objeto == null)
                continue;

            estado.objeto.localPosition = estado.posicion;
            estado.objeto.localRotation = estado.rotacion;
            estado.objeto.localScale = estado.escala;
        }

        if (patas != null && patasActivas != null)
        {
            for (int i = 0; i < patas.Length; i++)
            {
                if (patas[i] != null)
                    patas[i].enabled = patasActivas[i];
            }
        }

        if (balanceoCuerpo != null)
            balanceoCuerpo.enabled = balanceoActivo;

        if (gestor != null)
            gestor.enabled = gestorActivo;

        if (agente != null &&
            agente.enabled &&
            agente.isOnNavMesh)
        {
            if (pausado)
            {
                if (agente.hasPath)
                    agente.ResetPath();

                agente.isStopped = false;
            }
            else if (enInteraccionActual)
            {
                agente.isStopped = true;
            }
            else if (trabajando || enParking)
            {
                agente.isStopped = false;
            }
            else
            {
                agente.isStopped = agenteDetenido;
            }
        }

        if (recolector != null)
            recolector.enabled = recolectorActivo;

        animando = false;

        Debug.Log(
            "[PATADA] " + name +
            " | Recuperada | Reaccion: " +
            ultimaReaccion
        );
    }

    private void OnDisable()
    {
        if (!animando)
            return;

        StopAllCoroutines();
        RestaurarAnimacion();
    }
}
