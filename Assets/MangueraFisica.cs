using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MangueraFisica : MonoBehaviour
{
    [Header("Extremos")]
    public Transform puntoFijo;
    public Transform boquilla;

    [Header("Salida desde la pared")]
    [Tooltip("Metros que la manguera sale recta y en horizontal desde el punto fijo antes de caer por gravedad (0 = sin tramo recto)")]
    [Min(0f)] public float longitudSalidaRecta = 0.3f;
    [Tooltip("Eje local del punto fijo hacia el que sale la manguera. Se aplana a horizontal.")]
    public Vector3 direccionSalidaLocal = Vector3.forward;

    [Header("Entrada en la boquilla")]
    [Tooltip("Punto de la boquilla donde entra la manguera (vacío = el centro de la boquilla)")]
    public Transform puntoEntradaBoquilla;
    [Tooltip("Eje local (de la boquilla, o del punto de entrada si se asigna) por el que la manguera sale de la boquilla")]
    public Vector3 direccionEntradaLocal = Vector3.back;
    [Tooltip("Metros de manguera que salen rectos de la boquilla y giran con ella (0 = sin tramo recto)")]
    [Min(0f)] public float longitudEntradaRecta = 0.2f;
    [Tooltip("Gira la boquilla para que apunte en la dirección en la que va la manguera")]
    public bool alinearBoquilla = true;
    public float fuerzaAlineacion = 30f;
    public float amortiguacionAlineacion = 4f;
    private Rigidbody rbBoquilla;

    [Header("Fisica")]
    [Range(8, 60)] public int segmentos = 30;
    [Range(1f, 22f)] public float holgura = 1.3f;
    public float gravedad = 9.81f;
    [Range(0.9f, 1f)] public float amortiguacion = 0.99f;
    [Range(2, 30)] public int iteraciones = 15;

    [Header("Visual")]
    public float grosor = 0.25f;

    [Header("Colision con el suelo")]
    public LayerMask capasSuelo;
    public float radioManguera = 0.125f;

    [Header("Sistema de recogida")]
    public float combaVisible = 0.4f;
    public float velocidadSalida = 8f;
    public float velocidadRecogida = 3f;

    [Header("Tensado al conectar")]
    public float velocidadTensado = 8f;
    public float curvaturaTensada = 0.08f;

    [Header("Carrete (gira al estirar)")]
    [Tooltip("Objeto que gira sobre su eje Z local mientras la manguera sale (y al revés cuando se recoge)")]
    public Transform objetoGiratorio;
    [Tooltip("Grados que gira por cada metro de manguera que sale. Negativo = gira al otro lado.")]
    public float gradosPorMetro = 360f;
    [Tooltip("Si se desactiva, solo gira al estirar y se queda quieto al recoger")]
    public bool girarAlRecoger = true;
    private float longitudAnteriorGiro;

    [Header("LED del carrete (brilla al girar)")]
    [Tooltip("LED que se ilumina mientras gira el carrete. Vacío = se busca en el objeto giratorio y sus hijos.")]
    public ControladorNeon ledCarrete;
    [Tooltip("Intensidad del LED girando a velocidad máxima. En reposo vuelve a la intensidad que tenía al empezar.")]
    public float intensidadLedGirando = 4f;
    [Tooltip("Metros por segundo de manguera a partir de los cuales el LED brilla al máximo")]
    [Min(0.01f)] public float velocidadLedMaxima = 3f;
    [Tooltip("Rapidez con la que el LED sube y baja de intensidad (más alto = más inmediato)")]
    [Min(0f)] public float suavizadoLed = 8f;
    private float intensidadLedReposo;
    private float intensidadLedActual;

    private bool conectadaANave = false;

    public void TensarAlConectar()
    {
        conectadaANave = true;
    }

    private float longitudMaxima;
    private float longitudDesplegada;

    private Vector3[] posiciones;
    private Vector3[] posicionesAnteriores;
    private float longitudSegmento;
    private LineRenderer linea;
    public int CantidadPuntos => posiciones == null ? 0 : posiciones.Length;
    public Vector3 ObtenerPunto(int indice) => posiciones[indice];

    void Start()
    {
        if (puntoFijo == null || boquilla == null)
        {
            Debug.LogError("Faltan los extremos de la manguera.", this);
            enabled = false;
            return;
        }

        linea = GetComponent<LineRenderer>();
        rbBoquilla = boquilla.GetComponent<Rigidbody>();

        linea.useWorldSpace = true;
        linea.positionCount = segmentos + 1;
        linea.startWidth = grosor;
        linea.endWidth = grosor;
        linea.numCornerVertices = 8;
        linea.numCapVertices = 8;

        posiciones = new Vector3[segmentos + 1];
        posicionesAnteriores = new Vector3[segmentos + 1];

        float distancia = DistanciaHastaBoquilla();

        // Longitud total guardada en el surtidor.
        longitudMaxima = Mathf.Max(0.01f, distancia * holgura);

        // Al principio solo sale lo necesario para llegar a la boquilla.
        longitudDesplegada = Mathf.Min(
            longitudMaxima,
            distancia + combaVisible
        );

        longitudSegmento = longitudDesplegada / segmentos;
        longitudAnteriorGiro = longitudDesplegada;

        if (ledCarrete == null && objetoGiratorio != null)
            ledCarrete = objetoGiratorio.GetComponentInChildren<ControladorNeon>();
        if (ledCarrete != null)
        {
            intensidadLedReposo = ledCarrete.intensidad;
            intensidadLedActual = intensidadLedReposo;
        }

        int finSalida = IndiceFinSalida();
        FijarExtremos(finSalida);

        Vector3 inicioCaida = posiciones[finSalida];
        int inicioEntrada = IndiceInicioEntrada(finSalida);

        for (int i = finSalida + 1; i < inicioEntrada; i++)
        {
            float t = (float)(i - finSalida) / (inicioEntrada - finSalida);

            Vector3 posicion = Vector3.Lerp(
                inicioCaida,
                posiciones[inicioEntrada],
                t
            );

            posicion += Vector3.down *
                        (distancia * 0.15f * Mathf.Sin(t * Mathf.PI));

            posiciones[i] = posicion;
            posicionesAnteriores[i] = posicion;
        }

        ActualizarVisual();
    }


    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (conectadaANave)
        {
            // Recoger toda la longitud sobrante.
            longitudDesplegada = DistanciaHastaBoquilla();

            longitudSegmento = longitudDesplegada / segmentos;

            float suavizado = 1f - Mathf.Exp(-velocidadTensado * dt);

            int finSalidaConectada = IndiceFinSalida();
            FijarExtremos(finSalidaConectada);

            Vector3 inicioTramo = posiciones[finSalidaConectada];
            int inicioEntradaConectada = IndiceInicioEntrada(finSalidaConectada);

            for (int i = finSalidaConectada + 1; i < inicioEntradaConectada; i++)
            {
                float t = (float)(i - finSalidaConectada) / (inicioEntradaConectada - finSalidaConectada);

                // Línea entre el final de la salida recta y la entrada de la boquilla.
                Vector3 objetivo = Vector3.Lerp(
                    inicioTramo,
                    posiciones[inicioEntradaConectada],
                    t
                );

                // Una ligera curvatura natural.
                objetivo += Vector3.down *
                            Mathf.Sin(t * Mathf.PI) *
                            curvaturaTensada;

                // Recogida progresiva, sin cambios bruscos.
                posiciones[i] = Vector3.Lerp(
                    posiciones[i],
                    objetivo,
                    suavizado
                );

                posicionesAnteriores[i] = posiciones[i];
            }

            return;
        }

        // Distancia actual entre el surtidor y la boquilla.
        float distanciaActual = DistanciaHastaBoquilla();

        // Solo desplegar la longitud necesaria, con un poco de comba.
        float longitudObjetivo = Mathf.Min(
            longitudMaxima,
            distanciaActual + combaVisible
        );

        // Sale rápido al tirar; se recoge más despacio.
        float velocidadCarrete = longitudObjetivo > longitudDesplegada
            ? velocidadSalida
            : velocidadRecogida;

        longitudDesplegada = Mathf.MoveTowards(
            longitudDesplegada,
            longitudObjetivo,
            velocidadCarrete * dt
        );

        // Evitar que la longitud visible sea menor que la distancia
        // que separa los extremos.
        longitudDesplegada = Mathf.Min(
            longitudMaxima,
            Mathf.Max(longitudDesplegada, distanciaActual)
        );

        // Actualizamos el tamaño de los segmentos.
        longitudSegmento = longitudDesplegada / segmentos;

        // Los puntos hasta finSalida forman el tramo recto: no caen ni se corrigen.
        int finSalida = IndiceFinSalida();
        // Desde aquí hasta el final, el tramo recto que sale de la boquilla (también fijo).
        int inicioEntrada = IndiceInicioEntrada(finSalida);

        // Simulacion Verlet: gravedad e inercia.
        for (int i = finSalida + 1; i < inicioEntrada; i++)
        {
            Vector3 actual = posiciones[i];

            Vector3 velocidad =
                (posiciones[i] - posicionesAnteriores[i])
                * amortiguacion;

            posiciones[i] += velocidad
                + Vector3.down * gravedad * dt * dt;

            posicionesAnteriores[i] = actual;
        }

        // Mantener la longitud y evitar penetrar el suelo.
        for (int j = 0; j < iteraciones; j++)
        {
            FijarExtremos(finSalida);

            // Restricciones de longitud.
            for (int i = finSalida; i < inicioEntrada; i++)
            {
                Vector3 diferencia =
                    posiciones[i + 1] - posiciones[i];

                float distancia = diferencia.magnitude;

                if (distancia < 0.0001f)
                    continue;

                Vector3 correccion =
                    diferencia * ((distancia - longitudSegmento) / distancia);

                if (i == finSalida)
                {
                    posiciones[i + 1] -= correccion;
                }
                else if (i + 1 == inicioEntrada)
                {
                    posiciones[i] += correccion;
                }
                else
                {
                    posiciones[i] += correccion * 0.5f;
                    posiciones[i + 1] -= correccion * 0.5f;
                }
            }

            // Colision de los segmentos con el suelo.
            if (capasSuelo.value != 0)
            {
                for (int i = finSalida + 1; i < inicioEntrada; i++)
                {
                    Vector3 origen = posiciones[i] + Vector3.up * 3f;

                    if (Physics.Raycast(
                        origen,
                        Vector3.down,
                        out RaycastHit impacto,
                        6f,
                        capasSuelo,
                        QueryTriggerInteraction.Ignore))
                    {
                        float alturaMinima =
                            impacto.point.y + radioManguera;

                        if (posiciones[i].y < alturaMinima)
                        {
                            posiciones[i].y = alturaMinima;

                            posicionesAnteriores[i].y =
                                Mathf.Max(
                                    posicionesAnteriores[i].y,
                                    alturaMinima
                                );
                        }
                    }
                }
            }
        }

        // Los extremos permanecen sujetos.
        FijarExtremos(finSalida);

        AlinearBoquilla(finSalida, inicioEntrada);
    }

    Transform ReferenciaEntrada() => puntoEntradaBoquilla != null ? puntoEntradaBoquilla : boquilla;

    // Dirección en la que la manguera sale de la boquilla (gira con ella).
    Vector3 DireccionEntrada()
    {
        Vector3 direccion = ReferenciaEntrada().TransformDirection(direccionEntradaLocal);
        return direccion.sqrMagnitude < 0.000001f ? Vector3.zero : direccion.normalized;
    }

    // Primer punto del tramo recto de la boquilla. Deja al menos un punto libre tras la salida de la pared.
    int IndiceInicioEntrada(int finSalida)
    {
        if (longitudEntradaRecta <= 0f || longitudSegmento <= 0.0001f)
            return segmentos;

        int puntos = Mathf.RoundToInt(longitudEntradaRecta / longitudSegmento);
        return Mathf.Clamp(segmentos - puntos, finSalida + 2, segmentos);
    }

    // Sujeta el tramo recto que sale de la boquilla siguiendo su orientación,
    // para que la manguera no parezca salirse del pitorro al girar.
    void FijarEntrada(int inicioEntrada)
    {
        Vector3 origen = ReferenciaEntrada().position;
        Vector3 direccion = DireccionEntrada();

        for (int i = inicioEntrada; i <= segmentos; i++)
        {
            Vector3 punto = origen + direccion * ((segmentos - i) * longitudSegmento);
            posiciones[i] = punto;
            posicionesAnteriores[i] = punto;
        }
    }

    // Par de giro que lleva la entrada de la boquilla hacia donde va la manguera, para que cuelgue con naturalidad.
    void AlinearBoquilla(int finSalida, int inicioEntrada)
    {
        if (!alinearBoquilla || rbBoquilla == null || rbBoquilla.isKinematic)
            return;

        // Se mira unos puntos atrás: la dirección del tramo libre es más estable que la del punto vecino.
        int referencia = Mathf.Max(finSalida, inicioEntrada - 3);
        Vector3 actual = DireccionEntrada();
        Vector3 objetivo = posiciones[referencia] - posiciones[inicioEntrada];

        if (actual == Vector3.zero || objetivo.sqrMagnitude < 0.000001f)
            return;

        Vector3 eje = Vector3.Cross(actual, objetivo.normalized);
        if (eje.sqrMagnitude < 0.000001f)
            return;

        float angulo = Vector3.Angle(actual, objetivo) * Mathf.Deg2Rad;
        Vector3 aceleracion = eje.normalized * (angulo * fuerzaAlineacion)
                              - rbBoquilla.angularVelocity * amortiguacionAlineacion;

        rbBoquilla.AddTorque(aceleracion, ForceMode.Acceleration);
    }

    // Dirección horizontal en la que la manguera sale de la pared.
    Vector3 DireccionSalida()
    {
        Vector3 direccion = Vector3.ProjectOnPlane(
            puntoFijo.TransformDirection(direccionSalidaLocal),
            Vector3.up
        );

        // Si el eje elegido apunta en vertical, sale hacia la boquilla.
        if (direccion.sqrMagnitude < 0.000001f)
            direccion = Vector3.ProjectOnPlane(boquilla.position - puntoFijo.position, Vector3.up);

        if (direccion.sqrMagnitude < 0.000001f)
            direccion = Vector3.forward;

        return direccion.normalized;
    }

    // Longitud mínima para llegar a la boquilla pasando por la salida recta.
    float DistanciaHastaBoquilla()
    {
        Vector3 finSalida = puntoFijo.position + DireccionSalida() * longitudSalidaRecta;

        Vector3 inicioEntrada = ReferenciaEntrada().position + DireccionEntrada() * longitudEntradaRecta;

        return longitudSalidaRecta + Vector3.Distance(finSalida, inicioEntrada) + longitudEntradaRecta;
    }

    // Último punto del tramo recto. Siempre deja al menos dos puntos libres antes de la boquilla.
    int IndiceFinSalida()
    {
        if (longitudSalidaRecta <= 0f || longitudSegmento <= 0.0001f)
            return 0;

        int indice = Mathf.RoundToInt(longitudSalidaRecta / longitudSegmento);

        return Mathf.Clamp(indice, 0, segmentos - 2);
    }

    // Sujeta el tramo recto de salida (sin velocidad) y la boquilla.
    void FijarExtremos(int finSalida)
    {
        Vector3 origen = puntoFijo.position;
        Vector3 direccion = DireccionSalida();

        for (int i = 0; i <= finSalida; i++)
        {
            Vector3 punto = origen + direccion * (i * longitudSegmento);

            posiciones[i] = punto;
            posicionesAnteriores[i] = punto;
        }

        FijarEntrada(IndiceInicioEntrada(finSalida));
    }


    void LateUpdate()
    {
        if (posiciones != null)
        {
            // La boquilla se interpola entre pasos físicos: se vuelve a sujetar su tramo cada fotograma.
            FijarEntrada(IndiceInicioEntrada(IndiceFinSalida()));
            ActualizarVisual();
            GirarCarrete();
        }
    }

    // El giro sigue a la longitud desplegada: cuanto más rápido sale la manguera, más rápido gira.
    void GirarCarrete()
    {
        float cambio = longitudDesplegada - longitudAnteriorGiro;
        longitudAnteriorGiro = longitudDesplegada;

        bool gira = objetoGiratorio != null && cambio != 0f && (cambio > 0f || girarAlRecoger);
        if (gira)
            objetoGiratorio.Rotate(0f, 0f, cambio * gradosPorMetro, Space.Self);

        ActualizarLedCarrete(gira ? Mathf.Abs(cambio) : 0f);
    }

    // La longitud cambia en FixedUpdate, así que hay fotogramas sin cambio: el suavizado evita que parpadee.
    void ActualizarLedCarrete(float cambioAbsoluto)
    {
        if (ledCarrete == null || Time.deltaTime <= 0f)
            return;

        float velocidad = cambioAbsoluto / Time.deltaTime;
        float objetivo = Mathf.Lerp(intensidadLedReposo, intensidadLedGirando, Mathf.Clamp01(velocidad / velocidadLedMaxima));

        float suavizado = suavizadoLed <= 0f ? 1f : 1f - Mathf.Exp(-suavizadoLed * Time.deltaTime);
        intensidadLedActual = Mathf.Lerp(intensidadLedActual, objetivo, suavizado);
        ledCarrete.intensidad = intensidadLedActual;
    }

    void ActualizarVisual()
    {
        for (int i = 0; i < posiciones.Length; i++)
        {
            linea.SetPosition(i, posiciones[i]);
        }
    }
}
