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

        int finSalida = IndiceFinSalida();
        FijarExtremos(finSalida);

        Vector3 inicioCaida = posiciones[finSalida];

        for (int i = finSalida + 1; i < segmentos; i++)
        {
            float t = (float)(i - finSalida) / (segmentos - finSalida);

            Vector3 posicion = Vector3.Lerp(
                inicioCaida,
                boquilla.position,
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

            for (int i = finSalidaConectada + 1; i < segmentos; i++)
            {
                float t = (float)(i - finSalidaConectada) / (segmentos - finSalidaConectada);

                // Línea entre el final de la salida recta y la nave.
                Vector3 objetivo = Vector3.Lerp(
                    inicioTramo,
                    boquilla.position,
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

        // Simulacion Verlet: gravedad e inercia.
        for (int i = finSalida + 1; i < segmentos; i++)
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
            for (int i = finSalida; i < segmentos; i++)
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
                else if (i + 1 == segmentos)
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
                for (int i = finSalida + 1; i < segmentos; i++)
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

        return longitudSalidaRecta + Vector3.Distance(finSalida, boquilla.position);
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

        posiciones[segmentos] = boquilla.position;
    }


    void LateUpdate()
    {
        if (posiciones != null)
            ActualizarVisual();
    }

    void ActualizarVisual()
    {
        for (int i = 0; i < posiciones.Length; i++)
        {
            linea.SetPosition(i, posiciones[i]);
        }
    }
}
