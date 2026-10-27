using UnityEngine;

/// <summary>
/// Rescate visible de una piedra que cae al vacío.
/// En lugar de volver a la última posición segura, busca el BORDE MÁS CERCANO
/// del terreno que pertenece a capasSuelo. La piedra sube, se aproxima y
/// permanece flotando ligeramente por dentro del borde hasta ser recogida.
/// Colocar en el mismo GameObject que el Rigidbody.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class RescateVacioPiedra : MonoBehaviour
{
    [Header("Detección del terreno")]
    [Tooltip("SOLO las capas de terreno/plataformas. Nunca Piedra, Bot o Player.")]
    public LayerMask capasSuelo;

    [Tooltip("Distancia vertical bajo la piedra para reconocer apoyo seguro.")]
    [Min(0.1f)] public float distanciaDeteccionSuelo = 1.8f;

    [Tooltip("Tiempo entre comprobaciones mientras la piedra está libre.")]
    [Min(0.02f)] public float intervaloComprobacion = 0.2f;

    [Tooltip("Metros por debajo de la última ALTURA de apoyo antes de rescatar.")]
    [Min(1f)] public float profundidadParaRescatar = 8f;

    [Tooltip("Opcional, ÚNICAMENTE si no se encuentra ningún borde cercano. No representa el último apoyo.")]
    public Transform puntoRescateAlternativo;

    [Header("Búsqueda del borde MÁS CERCANO")]
    [Tooltip("Radio máximo de búsqueda horizontal alrededor del lugar donde cayó la piedra.")]
    [Min(1f)] public float radioBusquedaBorde = 20f;

    [Tooltip("Separación entre anillos de búsqueda; menos = mayor precisión y coste.")]
    [Range(0.2f, 3f)] public float pasoBusqueda = 0.65f;

    [Tooltip("Número de direcciones alrededor de cada anillo.")]
    [Range(8, 72)] public int muestrasPorAnillo = 32;

    [Tooltip("Una vez localizado el límite, alejamos este margen hacia el interior para que sea recuperable.")]
    [Min(0.05f)] public float margenInteriorBorde = 0.4f;

    [Tooltip("Altura por encima de la última superficie donde inicia cada raycast de búsqueda.")]
    [Min(0.5f)] public float alturaInicioRaycast = 8f;

    [Tooltip("Distancia hacia abajo para localizar una superficie válida.")]
    [Min(1f)] public float longitudRaycastBusqueda = 30f;

    [Tooltip("Inclinación máxima del terreno admitida como punto de apoyo.")]
    [Range(0f, 85f)] public float pendienteMaxima = 55f;

    [Tooltip("Si la piedra nunca estuvo apoyada, usa esta altura global como referencia para buscar el suelo.")]
    public bool usarAlturaReferenciaManual = false;
    public float alturaReferenciaManual = 0f;

    [Header("Seguridad anti-caida infinita")]
    [Tooltip("Si no se encuentra un borde, subir igualmente y permanecer flotando en vez de caer sin fin.")]
    public bool rescatarSinBordeComoEmergencia = true;

    [Tooltip("Amplia automáticamente la búsqueda si el radio normal no alcanza ningún terreno.")]
    [Range(1f, 5f)] public float multiplicadorRadioEmergencia = 3f;

    [Header("Ascenso visible")]
    [Min(0.1f)] public float velocidadAscenso = 7f;
    [Min(0.1f)] public float velocidadVueltaAlBorde = 5f;
    [Tooltip("Arco extra sobre el borde antes de estabilizarse. La roca sigue una curva, no un trayecto en tres tramos.")]
    [Min(0f)] public float alturaExtraTrayecto = 1f;

    [Header("Campo gravitatorio - trayectoria curva")]
    [Tooltip("Desvio lateral del recorrido hacia el borde; evita que el ascenso parezca una linea recta.")]
    [Min(0f)] public float curvaturaLateral = 1.25f;

    [Tooltip("Balanceo lateral visible mientras el campo atrae la piedra. Se desvanece al llegar.")]
    [Range(0f, 1f)] public float amplitudOscilacionLateral = 0.30f;

    [Tooltip("Ciclos por segundo del balanceo lateral durante el vuelo.")]
    [Min(0f)] public float frecuenciaOscilacionLateral = 0.85f;

    [Header("Frenada natural al entrar en rescate")]
    [Tooltip("Tiempo que tarda en perder suavemente la velocidad de caída. No afecta a la física normal.")]
    [Min(0.05f)] public float duracionFrenado = 0.6f;
    [Tooltip("Limita la velocidad heredada para que el frenado no arrastre la piedra demasiado lejos.")]
    [Min(0.1f)] public float velocidadMaximaDuranteFrenado = 12f;

    [Header("Deslizamiento suave durante el rescate")]
    [Tooltip("Intensidad del arranque y frenada suaves en la trayectoria. 0.35 = easing completo.")]
    [Min(0.05f)] public float suavizadoMovimiento = 0.35f;
    [Tooltip("Aplica balanceo lateral y vertical mientras la piedra sigue la trayectoria curva.")]
    public bool oscilarDuranteRegreso = true;
    [Tooltip("Tiempo para introducir la oscilación sin un salto al terminar la frenada.")]
    [Min(0.05f)] public float entradaSuaveOscilacion = 0.45f;

    [Header("Levitación tras el rescate")]
    [Tooltip("Altura del CENTRO de la piedra sobre el suelo. Ajustar al radio real de la roca.")]
    [Min(0.1f)] public float alturaLevitacion = 1f;
    [Range(0f, 0.3f)] public float amplitudFlotacion = 0.08f;
    [Min(0f)] public float frecuenciaFlotacion = 1.3f;

    [Header("Depuración")]
    public bool mostrarGizmos = true;
    public bool mensajesDebug = true;

    public bool EstaLevitando => estado == Estado.Flotando;

    // Consultar desde SistemaPatada y desde cualquier herramienta que golpee rocas.
    // Incluye el ascenso y la vuelta al borde, no solo el momento final flotando.
    // No impide la recogida manual ni la de los bots.
    public bool ProtegidaContraGolpes => estado != Estado.Normal;
    public bool EstaRescatandose => estado == Estado.Frenando ||
                                   estado == Estado.TrayectoriaCurva;

    private enum Estado { Normal, Frenando, TrayectoriaCurva, Flotando }
    private Estado estado = Estado.Normal;

    private Rigidbody rb;

    // El estado real se guarda EN EL MOMENTO de iniciar el rescate,
    // no en Awake: el spawner o DeformacionPiedra pueden modificarlo.
    private bool hayEstadoFisicoRescate;
    private bool gravedadAntesRescate;
    private bool colisionesAntesRescate;
    private bool cinematicoAntesRescate;
    private RigidbodyInterpolation interpolacionAntesRescate;
    private bool hayAlturaReferencia;
    private float alturaReferenciaSuelo;
    private bool enManosOProcesamiento;
    private float tiempoComprobacion;
    private float proximoIntentoRescate;
    // Solo se usan mientras el rescate tiene el control del Rigidbody.
    private Vector3 posicionBaseVuelo;
    private Vector3 velocidadAlEmpezarFrenado;
    private Vector3 giroAlEmpezarFrenado;
    private float tiempoFrenando;
    private float tiempoOscilando;
    private float tiempoDesdeFrenada;
    // Padre mientras la roca esta bajo control del rescate. Si un sistema
    // de agarre la hace hija de la mano, cedemos el control de inmediato.
    private Transform padreAntesRescate;
    private Vector3 destino;
    // Control de la curva cubica Bezier del rescate, en coordenadas de mundo.
    private Vector3 inicioCurva;
    private Vector3 controlCurva1;
    private Vector3 controlCurva2;
    private Vector3 lateralCurva;
    private float tiempoCurva;
    private float duracionCurva;
    private Vector3 puntoBorde;
    private bool encontradoBorde;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        RegistrarAlturaSueloSiEstaApoyada();
        if (!hayAlturaReferencia && puntoRescateAlternativo != null)
        {
            alturaReferenciaSuelo = puntoRescateAlternativo.position.y;
            hayAlturaReferencia = true;
        }
        if (!hayAlturaReferencia && usarAlturaReferenciaManual)
        {
            alturaReferenciaSuelo = alturaReferenciaManual;
            hayAlturaReferencia = true;
        }

        // Puede haber piedras que nazcan por encima del suelo, sin apoyarse.
        // Intentamos identificar la altura del terreno directamente debajo.
        if (!hayAlturaReferencia && capasSuelo.value != 0)
        {
            Vector3 origen = rb.position + Vector3.up * alturaInicioRaycast;
            if (Physics.Raycast(origen, Vector3.down, out RaycastHit hit,
                    Mathf.Max(80f, alturaInicioRaycast + longitudRaycastBusqueda),
                    capasSuelo, QueryTriggerInteraction.Ignore) &&
                EsSueloAdmisible(hit))
            {
                alturaReferenciaSuelo = hit.point.y;
                hayAlturaReferencia = true;
            }
        }

        // Garantía de detección: si no hay ningún suelo debajo, usamos
        // la altura donde la piedra nació. Ya NO se cae infinitamente
        // porque nunca haya pisado una superficie.
        if (!hayAlturaReferencia)
        {
            alturaReferenciaSuelo = rb.position.y;
            hayAlturaReferencia = true;
            if (mensajesDebug)
                Debug.LogWarning(name +
                    ": sin suelo bajo el punto de aparición. " +
                    "Se usará la altura inicial para detectar el vacío.", this);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // Algunos sistemas de agarre hacen SetParent(puntoAgarre).
        // No podemos seguir imponiendo la levitacion si ya esta en la mano.
        if (estado != Estado.Normal && transform.parent != padreAntesRescate)
        {
            CederControlAQuienRecoge();
            return;
        }

        // Un bot o una máquina pueden desactivar todos los colliders para
        // recogerla. No les quitamos el control aunque estuviera levitando.
        if (estado != Estado.Normal && !HayColliderActivo())
        {
            CederControlAQuienRecoge();
            return;
        }

        if (estado == Estado.Normal)
        {
            if (rb.isKinematic || !rb.detectCollisions)
            {
                enManosOProcesamiento = true;
                return;
            }

            if (enManosOProcesamiento)
            {
                enManosOProcesamiento = false;
                tiempoComprobacion = 0f;
            }

            tiempoComprobacion -= Time.fixedDeltaTime;
            if (tiempoComprobacion <= 0f)
            {
                tiempoComprobacion = Mathf.Max(0.02f, intervaloComprobacion);
                RegistrarAlturaSueloSiEstaApoyada();
            }

            if (hayAlturaReferencia &&
                rb.position.y < alturaReferenciaSuelo - profundidadParaRescatar &&
                Time.time >= proximoIntentoRescate)
            {
                if (!IntentarIniciarRescate())
                {
                    // Evitamos repetir cientos de raycasts cada FixedUpdate.
                    proximoIntentoRescate = Time.time + 1f;
                }
            }
            return;
        }

        // Primero imitamos la pérdida progresiva de inercia: la piedra aún
        // cae un poco y frena, en vez de detenerse de golpe y salir hacia arriba.
        if (estado == Estado.Frenando)
        {
            tiempoFrenando += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(tiempoFrenando /
                Mathf.Max(0.05f, duracionFrenado));
            float factorInercia = 1f - Mathf.SmoothStep(0f, 1f, t);

            posicionBaseVuelo += velocidadAlEmpezarFrenado *
                                 factorInercia * Time.fixedDeltaTime;
            rb.MovePosition(posicionBaseVuelo);

            // Durante la frenada también se extingue progresivamente el giro
            // que traía del vuelo libre, sin aplicar torque al Rigidbody.
            float velocidadAngular = giroAlEmpezarFrenado.magnitude;
            if (velocidadAngular > 0.001f)
            {
                Quaternion giro = Quaternion.AngleAxis(
                    velocidadAngular * Mathf.Rad2Deg * factorInercia * Time.fixedDeltaTime,
                    giroAlEmpezarFrenado / velocidadAngular);
                rb.MoveRotation(giro * rb.rotation);
            }

            if (t >= 1f)
            {
                // Iniciamos una unica curva desde la posicion alcanzada
                // tras perder gradualmente la velocidad de la caida.
                PrepararTrayectoriaCurva();
                estado = Estado.TrayectoriaCurva;
            }
        }
        else if (estado == Estado.TrayectoriaCurva)
        {
            RecorrerTrayectoriaCurva();
        }
        else if (estado == Estado.Flotando)
        {
            tiempoOscilando += Time.fixedDeltaTime;
            rb.MovePosition(destino + Vector3.up * ObtenerOscilacion());
        }
    }

    // Un unico recorrido curvo: asciende y se acerca al borde a la vez.
    // No modifica ni la busqueda de terreno ni la fisica normal de las piedras.
    private void PrepararTrayectoriaCurva()
    {
        inicioCurva = posicionBaseVuelo;
        Vector3 desplazamiento = destino - inicioCurva;
        Vector3 horizontal = Vector3.ProjectOnPlane(desplazamiento, Vector3.up);

        // Perpendicular al camino: nos permite curvarlo y oscilar lateralmente.
        lateralCurva = horizontal.sqrMagnitude > 0.0001f
            ? Vector3.Cross(Vector3.up, horizontal.normalized).normalized
            : Vector3.right;

        float ascenso = Mathf.Max(0f, desplazamiento.y);
        float desvio = Mathf.Min(curvaturaLateral,
            Mathf.Max(0.4f, horizontal.magnitude * 0.4f));

        // Dos puntos de control: sube, se arquea y llega desde arriba.
        controlCurva1 = Vector3.Lerp(inicioCurva, destino, 0.25f)
            + Vector3.up * (ascenso * 0.18f)
            + lateralCurva * desvio;

        controlCurva2 = Vector3.Lerp(inicioCurva, destino, 0.80f)
            + Vector3.up * (ascenso * 0.20f + alturaExtraTrayecto)
            + lateralCurva * (desvio * 0.60f);

        // Estimamos la longitud para que las piedras lejanas no viajen
        // a la misma velocidad aparente que las cercanas.
        const int muestras = 24;
        float longitud = 0f;
        Vector3 anterior = inicioCurva;
        for (int i = 1; i <= muestras; i++)
        {
            Vector3 actual = EvaluarBezier((float)i / muestras);
            longitud += Vector3.Distance(anterior, actual);
            anterior = actual;
        }

        float velocidadMedia = Mathf.Max(0.1f,
            (velocidadAscenso + velocidadVueltaAlBorde) * 0.5f);
        duracionCurva = Mathf.Max(0.6f, longitud / velocidadMedia);
        tiempoCurva = 0f;
        tiempoOscilando = 0f;
        tiempoDesdeFrenada = 0f;
    }

    private Vector3 EvaluarBezier(float t)
    {
        float u = 1f - t;
        return (u * u * u) * inicioCurva
             + (3f * u * u * t) * controlCurva1
             + (3f * u * t * t) * controlCurva2
             + (t * t * t) * destino;
    }

    private void RecorrerTrayectoriaCurva()
    {
        float dt = Time.fixedDeltaTime;
        tiempoCurva += dt;
        tiempoOscilando += dt;
        tiempoDesdeFrenada += dt;

        float t = Mathf.Clamp01(tiempoCurva / duracionCurva);

        // La fuerza atrae suavemente al principio y se disipa al final.
        // Conservamos el parametro de suavizado existente en el Inspector.
        float intensidadSuavizado = Mathf.Clamp01(suavizadoMovimiento / 0.35f);
        float progreso = Mathf.Lerp(t, Mathf.SmoothStep(0f, 1f, t),
            intensidadSuavizado);

        Vector3 posicion = EvaluarBezier(progreso);

        if (oscilarDuranteRegreso)
        {
            // La oscilacion crece durante el viaje y desaparece en la llegada.
            // Es lateral (bien visible) y tambien ligeramente vertical.
            float envolvente = Mathf.Sin(progreso * Mathf.PI);
            float entrada = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(tiempoDesdeFrenada /
                    Mathf.Max(0.05f, entradaSuaveOscilacion)));

            float lateral = Mathf.Sin(tiempoCurva * frecuenciaOscilacionLateral
                * Mathf.PI * 2f) * amplitudOscilacionLateral;

            posicion += lateralCurva * (lateral * envolvente * entrada);
            posicion += Vector3.up * (ObtenerOscilacion() * envolvente * entrada);
        }

        if (t >= 1f)
        {
            // La curva llega exactamente al destino, sin saltos por oscilacion.
            rb.MovePosition(destino);
            tiempoOscilando = 0f;
            estado = Estado.Flotando;
            rb.detectCollisions = true;

            if (mensajesDebug)
                Debug.Log(name +
                    ": rescate curvo terminado; colisiones reactivadas para permitir agarrarla.", this);
        }
        else
        {
            rb.MovePosition(posicion);
        }
    }

    private float ObtenerOscilacion()
    {
        return Mathf.Sin(tiempoOscilando * frecuenciaFlotacion *
                         Mathf.PI * 2f) * amplitudFlotacion;
    }

    private void RegistrarAlturaSueloSiEstaApoyada()
    {
        if (capasSuelo.value == 0) return;

        Vector3 inicio = rb.position + Vector3.up * 0.2f;
        if (Physics.Raycast(inicio, Vector3.down, out RaycastHit hit,
                            distanciaDeteccionSuelo + 0.2f, capasSuelo,
                            QueryTriggerInteraction.Ignore) &&
            EsSueloAdmisible(hit))
        {
            // Guardamos SOLO la altura para detectar la caída;
            // NO guardamos este punto como destino de retorno.
            alturaReferenciaSuelo = hit.point.y;
            hayAlturaReferencia = true;
        }
    }

    private bool EsSueloAdmisible(RaycastHit hit)
    {
        if (hit.collider == null) return false;
        if (hit.collider.transform == transform ||
            hit.collider.transform.IsChildOf(transform)) return false;
        return Vector3.Angle(hit.normal, Vector3.up) <= pendienteMaxima;
    }

    // Consulta vertical a capasSuelo: la referencia Y permite buscar terreno
    // incluso aunque la piedra ya esté decenas de metros bajo la isla.
    private bool BuscarSueloEnXZ(Vector3 posicion, out RaycastHit hit)
    {
        hit = default(RaycastHit);
        if (capasSuelo.value == 0) return false;

        Vector3 origen = new Vector3(posicion.x,
                                     alturaReferenciaSuelo + alturaInicioRaycast,
                                     posicion.z);
        return Physics.Raycast(origen, Vector3.down, out hit,
                               longitudRaycastBusqueda, capasSuelo,
                               QueryTriggerInteraction.Ignore) &&
               EsSueloAdmisible(hit);
    }

    private bool IntentarIniciarRescate()
    {
        encontradoBorde = BuscarPuntoJuntoAlBorde(
            rb.position, out Vector3 sueloDestino, radioBusquedaBorde,
            pasoBusqueda, muestrasPorAnillo);

        // Si la roca sale lejos del borde, ensanchamos la búsqueda una vez
        // usando una malla menos densa para no multiplicar el coste.
        if (!encontradoBorde && capasSuelo.value != 0 &&
            multiplicadorRadioEmergencia > 1f)
        {
            encontradoBorde = BuscarPuntoJuntoAlBorde(
                rb.position, out sueloDestino,
                radioBusquedaBorde * multiplicadorRadioEmergencia,
                Mathf.Max(2f, pasoBusqueda),
                Mathf.Min(16, muestrasPorAnillo));
        }

        if (!encontradoBorde)
        {
            if (puntoRescateAlternativo != null)
            {
                sueloDestino = puntoRescateAlternativo.position;
                Debug.LogWarning(name + ": sin borde detectado; usando punto alternativo.", this);
            }
            else if (rescatarSinBordeComoEmergencia)
            {
                // Última garantía: levitar sobre las XZ de la caída,
                // sin volver al último punto seguro. Ajusta el LayerMask
                // y el radio para que la próxima vez encuentre un borde.
                sueloDestino = new Vector3(rb.position.x, alturaReferenciaSuelo, rb.position.z);
                Debug.LogWarning(name +
                    ": no se encontró un borde en Capas Suelo. " +
                    "Rescate de emergencia vertical; revisa Capas Suelo " +
                    "y aumenta Radio Busqueda Borde si hace falta.", this);
            }
            else
            {
                if (mensajesDebug)
                    Debug.LogWarning(name + ": búsqueda sin terreno; el rescate de emergencia está desactivado.", this);
                return false;
            }
        }

        puntoBorde = sueloDestino;
        padreAntesRescate = transform.parent;
        destino = sueloDestino + Vector3.up * alturaLevitacion;
        // El ascenso se preparará DESPUÉS del frenado, desde la
        // posición a la que realmente haya llegado la piedra.
        posicionBaseVuelo = rb.position;
        tiempoFrenando = 0f;
        tiempoOscilando = 0f;
        tiempoDesdeFrenada = 0f;

        // Desde este instante el rescate es el único dueño temporal
        // de la física. Guardamos los valores ACTUALES, no los de Awake.
        gravedadAntesRescate = rb.useGravity;
        colisionesAntesRescate = rb.detectCollisions;
        cinematicoAntesRescate = rb.isKinematic;
        interpolacionAntesRescate = rb.interpolation;
        hayEstadoFisicoRescate = true;

        // Conservar la inercia para animar un frenado VISIBLE antes de subir.
        // Se limita solo el recorrido simulado durante el rescate, no la
        // velocidad ni la masa de las piedras mientras están jugando.
        velocidadAlEmpezarFrenado = rb.isKinematic ? Vector3.zero :
            Vector3.ClampMagnitude(rb.linearVelocity,
                Mathf.Max(0.1f, velocidadMaximaDuranteFrenado));
        giroAlEmpezarFrenado = rb.isKinematic ? Vector3.zero : rb.angularVelocity;
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.detectCollisions = false;
        estado = Estado.Frenando;

        if (mensajesDebug)
            Debug.Log(name + ": frenada progresiva; despues comenzara la trayectoria gravitatoria curva.");
        return true;
    }

    /// <summary>
    /// Busca una transición entre XZ sin suelo y XZ con suelo en capasSuelo.
    /// Es una aproximación por raycasts (adecuada para islas y plataformas),
    /// no una lectura exacta de la frontera geométrica de toda la capa.
    /// </summary>
    private bool BuscarPuntoJuntoAlBorde(Vector3 centro,
                                         out Vector3 posicionSuelo,
                                         float radioMaximo,
                                         float distanciaEntreAnillos,
                                         int numeroMuestras)
    {
        posicionSuelo = Vector3.zero;
        bool centroValido = BuscarSueloEnXZ(centro, out RaycastHit golpeCentro);
        int anillos = Mathf.CeilToInt(radioMaximo / Mathf.Max(0.2f, distanciaEntreAnillos));
        int muestras = Mathf.Clamp(numeroMuestras, 8, 72);

        // Cada anillo se evalúa COMPLETO: no elegimos el primer ángulo que
        // dé resultado, sino la frontera realmente más próxima entre los
        // candidatos de ese anillo (dentro de la resolución de muestreo).
        for (int anillo = 1; anillo <= anillos; anillo++)
        {
            float radio = Mathf.Min(radioMaximo, anillo * distanciaEntreAnillos);
            bool hayCandidato = false;
            float menorDistanciaCuadrada = float.MaxValue;
            Vector3 mejorPosicion = Vector3.zero;

            for (int i = 0; i < muestras; i++)
            {
                float angulo = i * (Mathf.PI * 2f / muestras);
                Vector3 p = centro + new Vector3(Mathf.Cos(angulo) * radio, 0f,
                                                 Mathf.Sin(angulo) * radio);
                bool pValido = BuscarSueloEnXZ(p, out RaycastHit golpeP);
                if (centroValido == pValido) continue; // No cruza un borde.

                Vector3 dentro = centroValido ? centro : p;
                Vector3 fuera = centroValido ? p : centro;
                RaycastHit golpeDentro = centroValido ? golpeCentro : golpeP;

                Vector3 puntoCandidato = AfinarLimiteTerreno(dentro, fuera, golpeDentro);
                Vector3 delta = puntoCandidato - centro;
                delta.y = 0f;
                float distanciaCuadrada = delta.sqrMagnitude;
                if (distanciaCuadrada < menorDistanciaCuadrada)
                {
                    menorDistanciaCuadrada = distanciaCuadrada;
                    mejorPosicion = puntoCandidato;
                    hayCandidato = true;
                }
            }

            if (hayCandidato)
            {
                posicionSuelo = mejorPosicion;
                return true;
            }
        }

        // Si está sobre suelo, pero no hay borde en el radio configurado,
        // usamos el terreno bajo su misma posición (NO el último apoyo).
        if (centroValido)
        {
            posicionSuelo = golpeCentro.point;
            return true;
        }
        return false;
    }

    private Vector3 AfinarLimiteTerreno(Vector3 puntoInterior,
                                        Vector3 puntoExterior,
                                        RaycastHit golpeInterior)
    {
        Vector3 dentro = puntoInterior;
        Vector3 fuera = puntoExterior;
        RaycastHit ultimoGolpe = golpeInterior;

        // Búsqueda binaria de la transición suelo/vacío.
        for (int i = 0; i < 9; i++)
        {
            Vector3 medio = (dentro + fuera) * 0.5f;
            if (BuscarSueloEnXZ(medio, out RaycastHit hit))
            {
                dentro = medio;
                ultimoGolpe = hit;
            }
            else
            {
                fuera = medio;
            }
        }

        // La dejamos un poco DENTRO de la capa de suelo, no sobre el vacío.
        Vector3 haciaInterior = puntoInterior - puntoExterior;
        haciaInterior.y = 0f;
        if (haciaInterior.sqrMagnitude > 0.0001f)
        {
            Vector3 candidato = dentro + haciaInterior.normalized * margenInteriorBorde;
            if (BuscarSueloEnXZ(candidato, out RaycastHit hitInterior))
                return hitInterior.point;
        }
        return ultimoGolpe.point;
    }

    /// <summary>Llamar cuando el jugador recoja la piedra.</summary>
    public void NotificarRecogida()
    {
        // Cede la propiedad del Rigidbody a AgarreLanzamiento.
        // Este último establecerá isKinematic y detectCollisions.
        CederControlAQuienRecoge();
    }

    /// <summary>Llamar cuando AgarreLanzamiento suelte la piedra.</summary>
    public void NotificarLiberacion()
    {
        estado = Estado.Normal;
        enManosOProcesamiento = false;
        tiempoComprobacion = 0f;

        // Si hubo un rescate en curso, restaurar. En caso contrario,
        // no alterar la física que ya ha configurado quien la soltó.
        RestaurarPropiedadesNoCinematicas();
    }

    private void CederControlAQuienRecoge()
    {
        if (estado != Estado.Normal && mensajesDebug)
            Debug.Log(name + ": recogida; el rescate libera el Rigidbody.");

        estado = Estado.Normal;
        enManosOProcesamiento = true;
        tiempoComprobacion = 0f;
        RestaurarPropiedadesNoCinematicas();

        // NO tocamos isKinematic: el bot o AgarreLanzamiento
        // lo controlan durante el transporte.
    }

    private void RestaurarPropiedadesNoCinematicas()
    {
        if (rb == null || !hayEstadoFisicoRescate)
            return;

        rb.useGravity = gravedadAntesRescate;
        rb.detectCollisions = colisionesAntesRescate;
        rb.interpolation = interpolacionAntesRescate;
        hayEstadoFisicoRescate = false;
    }

    private void OnDisable()
    {
        // Si se desactiva el script cuando la roca esta flotando,
        // no debemos dejarla congelada, sin gravedad ni colisiones.
        if (!Application.isPlaying || rb == null || estado == Estado.Normal)
            return;

        RestaurarPropiedadesNoCinematicas();
        rb.isKinematic = cinematicoAntesRescate;
        if (!rb.isKinematic)
            rb.WakeUp();
        estado = Estado.Normal;
        enManosOProcesamiento = false;
    }

    private bool HayColliderActivo()
    {
        Collider[] col = GetComponentsInChildren<Collider>();
        for (int i = 0; i < col.Length; i++)
            if (col[i] != null && col[i].enabled) return true;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!mostrarGizmos || !Application.isPlaying) return;
        if (encontradoBorde)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(puntoBorde, 0.22f);
        }
        if (estado != Estado.Normal)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(destino, 0.24f);

            if (estado == Estado.TrayectoriaCurva || estado == Estado.Flotando)
            {
                // Se ve en Scene la curva real calculada para esta piedra.
                Vector3 anterior = inicioCurva;
                for (int i = 1; i <= 24; i++)
                {
                    Vector3 siguiente = EvaluarBezier(i / 24f);
                    Gizmos.DrawLine(anterior, siguiente);
                    anterior = siguiente;
                }
            }
            else
            {
                Gizmos.DrawLine(transform.position, destino);
            }
        }
    }
}
