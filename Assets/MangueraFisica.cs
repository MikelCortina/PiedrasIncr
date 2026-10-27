using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MangueraFisica : MonoBehaviour
{
    [Header("Extremos")]
    public Transform puntoFijo;
    public Transform boquilla;

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

        float distancia = Vector3.Distance(
            puntoFijo.position,
            boquilla.position
        );

        // Longitud total guardada en el surtidor.
        longitudMaxima = Mathf.Max(0.01f, distancia * holgura);

        // Al principio solo sale lo necesario para llegar a la boquilla.
        longitudDesplegada = Mathf.Min(
            longitudMaxima,
            distancia + combaVisible
        );

        longitudSegmento = longitudDesplegada / segmentos;

        for (int i = 0; i <= segmentos; i++)
        {
            float t = (float)i / segmentos;

            Vector3 posicion = Vector3.Lerp(
                puntoFijo.position,
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
            longitudDesplegada = Vector3.Distance(
                puntoFijo.position,
                boquilla.position
            );

            longitudSegmento = longitudDesplegada / segmentos;

            float suavizado = 1f - Mathf.Exp(-velocidadTensado * dt);

            posiciones[0] = puntoFijo.position;
            posiciones[segmentos] = boquilla.position;

            for (int i = 1; i < segmentos; i++)
            {
                float t = (float)i / segmentos;

                // Línea entre el surtidor y la nave.
                Vector3 objetivo = Vector3.Lerp(
                    puntoFijo.position,
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
        float distanciaActual = Vector3.Distance(
            puntoFijo.position,
            boquilla.position
        );

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

        // Simulacion Verlet: gravedad e inercia.
        for (int i = 1; i < segmentos; i++)
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
            posiciones[0] = puntoFijo.position;
            posiciones[segmentos] = boquilla.position;

            // Restricciones de longitud.
            for (int i = 0; i < segmentos; i++)
            {
                Vector3 diferencia =
                    posiciones[i + 1] - posiciones[i];

                float distancia = diferencia.magnitude;

                if (distancia < 0.0001f)
                    continue;

                Vector3 correccion =
                    diferencia * ((distancia - longitudSegmento) / distancia);

                if (i == 0)
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
                for (int i = 1; i < segmentos; i++)
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
        posiciones[0] = puntoFijo.position;
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