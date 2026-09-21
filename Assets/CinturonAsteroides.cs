using UnityEngine;

public class CinturonAsteroides : MonoBehaviour
{
    // --- NUEVO: Estructura pública para pasarle los datos a la Lluvia ---
    public struct DatosSpawnMeteorito
    {
        public float anguloInicial;
        public float distanciaAlCentro;
        public float alturaY;
        public float velocidadOrbita;
    }

    private struct AsteroideOrbital
    {
        public Transform transformAsteroide;
        public float anguloActual;
        public float distanciaAlCentro;
        public float velocidadOrbita;
        public float offsetAlturaY;
        public Vector3 ejeRotacionLocal;
        public float velocidadRotacionLocal;
    }

    [Header("Referencias")]
    public Transform objetivoOrbita;
    public GameObject[] prefabsAsteroides;

    [Header("Rotación del Objetivo Central")]
    public Vector3 ejeRotacionObjetivo = Vector3.up;
    public float velocidadRotacionObjetivo = 15f;

    [Header("Orientación Dinámica (Hacia el Jugador)")]
    public Transform jugador;
    public string tagJugador = "Player";
    public float inclinacionX = 15f;

    [Header("Forma del Anillo")]
    public int cantidadAsteroides = 200;
    public float radioInterior = 40f;
    public float radioExterior = 60f;
    public float elevacionCinturon = 10f;
    public float grosorAltura = 5f;

    // --- NUEVO: Zona de Caída ---
    [Header("Fracción de Caída (Drop Zone)")]
    [Tooltip("El ángulo central desde donde se permite que caigan los meteoritos (0 a 360)")]
    [Range(0f, 360f)] public float anguloCentroCaida = 180f;
    [Tooltip("La amplitud en grados de la zona de caída. 90 significa 45 grados a cada lado del centro.")]
    [Range(10f, 360f)] public float aperturaCaida = 90f;

    [Header("Movimiento y Variación")]
    public Vector2 rangoVelocidadOrbita = new Vector2(20f, 40f);
    public Vector2 rangoVelocidadRotacionLocal = new Vector2(10f, 50f);
    public Vector2 rangoEscala = new Vector2(500f, 500f);

    private AsteroideOrbital[] arrayAsteroides;

    void Start()
    {
        if (objetivoOrbita == null || prefabsAsteroides == null || prefabsAsteroides.Length == 0) return;

        if (jugador == null)
        {
            GameObject objJugador = GameObject.FindGameObjectWithTag(tagJugador);
            if (objJugador != null) jugador = objJugador.transform;
        }

        GenerarCinturon();
    }

    void GenerarCinturon()
    {
        arrayAsteroides = new AsteroideOrbital[cantidadAsteroides];

        for (int i = 0; i < cantidadAsteroides; i++)
        {
            GameObject prefabAleatorio = prefabsAsteroides[Random.Range(0, prefabsAsteroides.Length)];
            GameObject nuevoAsteroide = Instantiate(prefabAleatorio, transform);
            Destroy(nuevoAsteroide.GetComponent<Rigidbody>());
            float escalaRandom = Random.Range(rangoEscala.x, rangoEscala.y);
            nuevoAsteroide.transform.localScale = new Vector3(escalaRandom, escalaRandom, escalaRandom);

            arrayAsteroides[i] = new AsteroideOrbital
            {
                transformAsteroide = nuevoAsteroide.transform,
                anguloActual = Random.Range(0f, 360f),
                distanciaAlCentro = Random.Range(radioInterior, radioExterior),
                velocidadOrbita = Random.Range(rangoVelocidadOrbita.x, rangoVelocidadOrbita.y),
                offsetAlturaY = Random.Range(-grosorAltura, grosorAltura),
                ejeRotacionLocal = Random.onUnitSphere,
                velocidadRotacionLocal = Random.Range(rangoVelocidadRotacionLocal.x, rangoVelocidadRotacionLocal.y)
            };

            if (Random.value > 0.5f) arrayAsteroides[i].velocidadOrbita *= -1f;
        }
    }

    void Update()
    {
        if (objetivoOrbita == null || arrayAsteroides == null) return;

        if (velocidadRotacionObjetivo != 0f)
        {
            objetivoOrbita.Rotate(ejeRotacionObjetivo, velocidadRotacionObjetivo * Time.deltaTime, Space.Self);
        }

        for (int i = 0; i < arrayAsteroides.Length; i++)
        {
            arrayAsteroides[i].anguloActual += arrayAsteroides[i].velocidadOrbita * Time.deltaTime;

            if (arrayAsteroides[i].anguloActual > 360f) arrayAsteroides[i].anguloActual -= 360f;
            else if (arrayAsteroides[i].anguloActual < 0f) arrayAsteroides[i].anguloActual += 360f;

            Vector3 posicionFinal = ObtenerPosicionEnCinturon(
                arrayAsteroides[i].anguloActual,
                arrayAsteroides[i].distanciaAlCentro,
                arrayAsteroides[i].offsetAlturaY
            );

            arrayAsteroides[i].transformAsteroide.position = posicionFinal;

            arrayAsteroides[i].transformAsteroide.Rotate(
                arrayAsteroides[i].ejeRotacionLocal,
                arrayAsteroides[i].velocidadRotacionLocal * Time.deltaTime,
                Space.Self
            );
        }
    }

    // =======================================================
    // --- LÓGICA DE COORDENADAS Y ZONA DE CAÍDA ---
    // =======================================================

    public void ObtenerMatrizCinturon(out Vector3 centro, out Quaternion rotacion)
    {
        centro = objetivoOrbita != null ? objetivoOrbita.position + new Vector3(0f, elevacionCinturon, 0f) : Vector3.zero;
        rotacion = Quaternion.identity;

        Transform targetJugador = jugador;
        if (targetJugador == null && !string.IsNullOrEmpty(tagJugador))
        {
            GameObject j = GameObject.FindGameObjectWithTag(tagJugador);
            if (j != null) targetJugador = j.transform;
        }

        if (targetJugador != null)
        {
            Vector3 direccionHaciaJugador = targetJugador.position - centro;
            direccionHaciaJugador.y = 0f;
            if (direccionHaciaJugador.sqrMagnitude > 0.001f)
            {
                rotacion = Quaternion.LookRotation(direccionHaciaJugador) * Quaternion.Euler(inclinacionX, 0f, 0f);
            }
        }
    }

    public Vector3 ObtenerPosicionEnCinturon(float anguloGrados, float distancia, float alturaY)
    {
        ObtenerMatrizCinturon(out Vector3 centro, out Quaternion rotacion);
        float radianes = anguloGrados * Mathf.Deg2Rad;
        Vector3 posicionLocal = new Vector3(Mathf.Cos(radianes) * distancia, alturaY, Mathf.Sin(radianes) * distancia);
        return centro + (rotacion * posicionLocal);
    }

    public DatosSpawnMeteorito ObtenerDatosSpawnMeteorito()
    {
        float velocidadO = Random.Range(rangoVelocidadOrbita.x, rangoVelocidadOrbita.y);
        if (Random.value > 0.5f) velocidadO *= -1f;

        return new DatosSpawnMeteorito
        {
            anguloInicial = Random.Range(0f, 360f),
            distanciaAlCentro = Random.Range(radioInterior, radioExterior),
            alturaY = Random.Range(-grosorAltura, grosorAltura),
            velocidadOrbita = velocidadO
        };
    }

    public bool EstaEnZonaDeCaida(float angulo)
    {
        // DeltaAngle nos da la distancia más corta entre dos ángulos, evitando bugs al pasar de 360 a 0
        float diferencia = Mathf.Abs(Mathf.DeltaAngle(anguloCentroCaida, angulo));
        return diferencia <= (aperturaCaida / 2f);
    }

    // =======================================================
    // --- GIZMOS ---
    // =======================================================

    void OnDrawGizmosSelected()
    {
        if (objetivoOrbita == null) return;

        ObtenerMatrizCinturon(out Vector3 centroElevado, out Quaternion rotacionAnillo);

        // 1. Dibujar anillo completo tenue
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.2f);
        DibujarArcoGizmo(centroElevado, radioInterior, rotacionAnillo, 0f, 360f);
        DibujarArcoGizmo(centroElevado, radioExterior, rotacionAnillo, 0f, 360f);

        // 2. Dibujar zona de caída destacada (Roja)
        Gizmos.color = Color.red;
        float anguloInicio = anguloCentroCaida - (aperturaCaida / 2f);
        float anguloFin = anguloCentroCaida + (aperturaCaida / 2f);

        DibujarArcoGizmo(centroElevado, radioInterior, rotacionAnillo, anguloInicio, anguloFin);
        DibujarArcoGizmo(centroElevado, radioExterior, rotacionAnillo, anguloInicio, anguloFin);

        // Líneas laterales para cerrar la porción
        Vector3 pInteriorInicio = ObtenerPosicionEnCinturon(anguloInicio, radioInterior, 0f);
        Vector3 pExteriorInicio = ObtenerPosicionEnCinturon(anguloInicio, radioExterior, 0f);
        Vector3 pInteriorFin = ObtenerPosicionEnCinturon(anguloFin, radioInterior, 0f);
        Vector3 pExteriorFin = ObtenerPosicionEnCinturon(anguloFin, radioExterior, 0f);

        Gizmos.DrawLine(pInteriorInicio, pExteriorInicio);
        Gizmos.DrawLine(pInteriorFin, pExteriorFin);
    }

    void DibujarArcoGizmo(Vector3 centro, float radio, Quaternion rotacionAnillo, float anguloInicio, float anguloFin)
    {
        int segmentos = 40;
        float rango = Mathf.Abs(Mathf.DeltaAngle(anguloInicio, anguloFin));
        if (rango < 0.1f && anguloFin - anguloInicio >= 360f) rango = 360f; // Caso de círculo completo

        float paso = rango / segmentos;

        float radInicial = anguloInicio * Mathf.Deg2Rad;
        Vector3 posInicialLocal = new Vector3(Mathf.Cos(radInicial) * radio, 0, Mathf.Sin(radInicial) * radio);
        Vector3 puntoAnterior = centro + (rotacionAnillo * posInicialLocal);

        for (int i = 1; i <= segmentos; i++)
        {
            float radianes = (anguloInicio + paso * i) * Mathf.Deg2Rad;
            Vector3 posLocal = new Vector3(Mathf.Cos(radianes) * radio, 0, Mathf.Sin(radianes) * radio);
            Vector3 puntoNuevo = centro + (rotacionAnillo * posLocal);
            Gizmos.DrawLine(puntoAnterior, puntoNuevo);
            puntoAnterior = puntoNuevo;
        }
    }
}