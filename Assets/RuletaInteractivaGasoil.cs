using UnityEngine;

/// <summary>
/// Ruleta que se gira con la mano (GestorRuletasFPS) para abrir una tapa o válvula.
/// La ruleta gira exactamente lo mismo que la mano y en el mismo sentido en pantalla:
/// las vueltas que das son las vueltas que da ella.
/// </summary>
public class RuletaInteractivaGasoil : MonoBehaviour
{
    // Ángulo con el que se ajustaron las velocidades de retorno (2 vueltas).
    private const float AnguloReferenciaRetorno = 720f;

    [Header("Ajustes de Giro de la Ruleta")]
    [Tooltip("Vueltas que hay que dar (con la mano y con la ruleta, es lo mismo) para abrir.")]
    [Min(0.25f)] public float vueltasParaAbrir = 6f;
    [Tooltip("Eje local sobre el cual girará la ruleta (Normalmente Vector3.forward, Vector3.up o Vector3.right)")]
    public Vector3 ejeGiroLocal = Vector3.forward;
    [Tooltip("El sentido visual se calcula solo para que la ruleta acompañe a la mano. Márcalo si en esta ruleta sale al revés.")]
    public bool invertirGiroVisual = false;

    [Min(1f)]
    [Tooltip("Velocidad a la que la ruleta se desenrosca, en grados por segundo para 2 vueltas. " +
             "Con más vueltas va proporcionalmente más rápida: tarda lo mismo en volver.")]
    public float velocidadRetorno = 300f;

    /// <summary>Grados totales de giro para abrir.</summary>
    public float AnguloObjetivo => Mathf.Max(1f, vueltasParaAbrir * 360f);
    private float VelocidadRetornoEfectiva => velocidadRetorno * AnguloObjetivo / AnguloReferenciaRetorno;

    [Header("Segundo Objeto (Activación)")]
    [Tooltip("El objeto que rotará al alcanzar el objetivo")]
    public Transform objetoSecundario;
    [Tooltip("Rotación final que tomará el objeto secundario al activarse")]
    public Vector3 rotacionObjetivoSecundario = new Vector3(-90f, 0f, 0f);
    [Tooltip("Cantidad a bajar en el eje Y (ej: -0.5) cuando la válvula 'suelta el aire' y el LED se descarga")]
    public float bajadaEjeYSecundario = -0.5f;

    [Header("Efectos al Completar (Explosión/Vaciado)")]
    [Tooltip("Tiempo en segundos que tarda el LED en apagarse (vaciarse) al llegar a 720 antes de iniciar el retorno")]
    public float tiempoDescarga = 1.5f;
    [Tooltip("Partículas que saltan en el momento exacto en el que la ruleta llega a su objetivo (720)")]
    public ParticleSystem[] particulasExplosion;

    [Header("Sistema de LED / Neón")]
    [Tooltip("Controlador de neón del LED asociado a esta ruleta (opcional)")]
    public ControladorNeon ledControlador;

    [Header("1. Estado Inicial (Enfriado)")]
    [ColorUsage(true, true)] public Color colorLedApagado = Color.black;
    public float intensidadInicio = 1f;
    public float parpadeoInicio = 2f;

    [Header("2. Estado Final (Punto Máximo / Cargado)")]
    [ColorUsage(true, true)] public Color colorLedEncendido = Color.green;
    public float intensidadFin = 5f;
    public float parpadeoFin = 25f;

    // Variables de Estado Internas
    private float gradosAcumuladosGiro = 0f;
    private Quaternion rotacionOrigen;
    // +1 o -1: para que el giro visual vaya en el mismo sentido que la mano en pantalla.
    private float signoVisual = 1f;

    // Variables para el objeto secundario (Tapa / Válvula)
    private Quaternion rotacionOrigenSecundario;
    private Vector3 posicionOrigenSecundario;

    // Máquina de estados
    private enum EstadoRuleta { Girando, Vaciando, Retornando, Completado }
    private EstadoRuleta estadoActual = EstadoRuleta.Girando;

    private bool estaSiendoGirada = false;

    // Sincronización del tiempo de retorno
    private float progresoSecundario = 0f;

    // --- Lectura para el indicador de sentido de giro (IndicadorGiroRuleta) ---
    /// <summary>Cuánto se ha girado hacia la apertura (0-1).</summary>
    public float Progreso => Mathf.Clamp01(gradosAcumuladosGiro / AnguloObjetivo);
    /// <summary>True desde que llega al objetivo (se vacía, se abre y queda bloqueada).</summary>
    public bool Abierta => estadoActual != EstadoRuleta.Girando;
    /// <summary>Último giro que mandó el jugador (positivo = hacia abrir, negativo = al revés).</summary>
    public float UltimoDeltaGiro { get; private set; }
    /// <summary>Time.time del último giro del jugador.</summary>
    public float TiempoUltimoGiro { get; private set; } = -999f;

    // Progreso del efecto de "Vaciado" (Descarga del LED y bajada del eje Y)
    private float progresoVaciado = 1f;

    void Start()
    {
        rotacionOrigen = transform.localRotation;
        CalcularSignoVisual(Camera.main);
        if (objetoSecundario != null)
        {
            rotacionOrigenSecundario = objetoSecundario.localRotation;
            posicionOrigenSecundario = objetoSecundario.localPosition;
        }
    }

    void Update()
    {
        // -------------------------------------------------------------
        // ESTADO 1: GIRANDO (Libre o apretando)
        // -------------------------------------------------------------
        if (estadoActual == EstadoRuleta.Girando)
        {
            // Si el jugador suelta el clic antes de alcanzar el objetivo, desenroscamos
            if (!estaSiendoGirada)
            {
                // Vuelve a 0 a velocidad constante
                gradosAcumuladosGiro = Mathf.MoveTowards(gradosAcumuladosGiro, 0f, VelocidadRetornoEfectiva * Time.deltaTime);
                AplicarRotacionRuleta();

                // LED se adapta al giro actual
                ActualizarLed(Progreso);
            }

            // Aseguramos que el objeto secundario esté cerrado
            if (objetoSecundario != null)
            {
                float tiempoDeRetornoTotal = AnguloObjetivo / VelocidadRetornoEfectiva;
                float velocidadProgresoSecundario = 1f / tiempoDeRetornoTotal;
                progresoSecundario = Mathf.MoveTowards(progresoSecundario, 0f, velocidadProgresoSecundario * Time.deltaTime);

                Quaternion rotacionDeseada = rotacionOrigenSecundario * Quaternion.Euler(rotacionObjetivoSecundario);
                objetoSecundario.localRotation = Quaternion.Slerp(rotacionOrigenSecundario, rotacionDeseada, progresoSecundario);
            }
        }

        // -------------------------------------------------------------
        // ESTADO 2: VACIANDO (Llegó a 720, tira partículas, luz se apaga y baja el objeto)
        // -------------------------------------------------------------
        else if (estadoActual == EstadoRuleta.Vaciando)
        {
            // El LED y el eje Y se "vacían" según el tiempo establecido
            if (tiempoDescarga <= 0.01f) tiempoDescarga = 0.01f;
            progresoVaciado -= Time.deltaTime / tiempoDescarga;

            // Actualizamos LED durante el vaciado
            ActualizarLed(progresoVaciado);

            // Bajar el objeto secundario en el eje Y
            if (objetoSecundario != null)
            {
                // Interpolamos desde Y original hasta (Y original + bajadaEjeYSecundario)
                float factorBajada = 1f - progresoVaciado;
                Vector3 posDeseada = posicionOrigenSecundario + new Vector3(0f, bajadaEjeYSecundario * factorBajada, 0f);
                objetoSecundario.localPosition = posDeseada;
            }

            // Cuando termina el vaciado, pasamos a la fase de Retorno
            if (progresoVaciado <= 0f)
            {
                progresoVaciado = 0f;
                estadoActual = EstadoRuleta.Retornando;
            }
        }

        // -------------------------------------------------------------
        // ESTADO 3: RETORNANDO (Se abren simultáneamente la ruleta y la rotación del secundario)
        // -------------------------------------------------------------
        else if (estadoActual == EstadoRuleta.Retornando)
        {
            // A. Recuperamos la altura Y del objeto secundario (hacia la original)
            if (objetoSecundario != null)
            {
                objetoSecundario.localPosition = Vector3.MoveTowards(objetoSecundario.localPosition, posicionOrigenSecundario, Time.deltaTime * Mathf.Abs(bajadaEjeYSecundario));
            }

            // B. La ruleta se desenrosca a velocidad constante
            gradosAcumuladosGiro = Mathf.MoveTowards(gradosAcumuladosGiro, 0f, VelocidadRetornoEfectiva * Time.deltaTime);
            AplicarRotacionRuleta();

            // C. El objeto secundario se ROTA abriéndose
            if (objetoSecundario != null)
            {
                float tiempoDeRetornoTotal = AnguloObjetivo / VelocidadRetornoEfectiva;
                float velocidadProgresoSecundario = 1f / tiempoDeRetornoTotal;

                progresoSecundario = Mathf.MoveTowards(progresoSecundario, 1f, velocidadProgresoSecundario * Time.deltaTime);

                Quaternion rotacionDeseada = rotacionOrigenSecundario * Quaternion.Euler(rotacionObjetivoSecundario);
                objetoSecundario.localRotation = Quaternion.Slerp(rotacionOrigenSecundario, rotacionDeseada, progresoSecundario);
            }

            // Si la ruleta ya volvió a cero por completo y el objeto recuperó su altura, el ciclo terminó.
            if (gradosAcumuladosGiro <= 0f)
            {
                gradosAcumuladosGiro = 0f;

                // ¡BLOQUEO PERMANENTE! 
                // A partir de aquí no hace nada más y se queda en su rotación final.
                estadoActual = EstadoRuleta.Completado;
            }
        }

        // Restablecemos el flag de interacción manual para el siguiente frame
        estaSiendoGirada = false;
    }

    // Método llamado desde GestorRuletasFPS.
    // deltaGiro: grados que ha girado la mano alrededor de la ruleta en pantalla (positivo = hacia abrir).
    // Se aplican tal cual, sin multiplicar: la ruleta va exactamente con la mano.
    public void ModificarDesdeGiroVisual(float deltaGiro, Camera camara)
    {
        // Si no estamos en la fase normal de giro, ignoramos el input del jugador por completo
        if (estadoActual != EstadoRuleta.Girando) return;

        estaSiendoGirada = true;
        UltimoDeltaGiro = deltaGiro;
        TiempoUltimoGiro = Time.time;
        CalcularSignoVisual(camara);

        gradosAcumuladosGiro += deltaGiro;
        if (gradosAcumuladosGiro < 0f) gradosAcumuladosGiro = 0f;

        // COMPROBAR ACTIVACIÓN (Alcanzar el ángulo objetivo)
        if (gradosAcumuladosGiro >= AnguloObjetivo)
        {
            gradosAcumuladosGiro = AnguloObjetivo;
            estaSiendoGirada = false; // Forzamos el soltado visual de la interacción

            // Activamos la fase de vaciado
            estadoActual = EstadoRuleta.Vaciando;
            progresoVaciado = 1f; // Reseteamos el progreso del vaciado para que empiece desde 100%

            // Disparamos partículas de explosión
            if (particulasExplosion != null && particulasExplosion.Length > 0)
            {
                foreach (ParticleSystem ps in particulasExplosion)
                {
                    if (ps != null)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        ps.Play();
                    }
                }
            }
        }

        // Aplicamos rotación física de la ruleta manual
        AplicarRotacionRuleta();

        // LED acompaña al giro manual
        ActualizarLed(Progreso);
    }

    private void AplicarRotacionRuleta()
    {
        transform.localRotation = rotacionOrigen * Quaternion.AngleAxis(gradosAcumuladosGiro * signoVisual, ejeGiroLocal);
    }

    // El gesto se mide en pantalla con ángulo antihorario positivo. Un giro positivo de Unity se ve
    // horario si el eje apunta hacia la cámara y antihorario si apunta hacia el otro lado.
    private void CalcularSignoVisual(Camera camara)
    {
        if (camara == null) return;
        Vector3 ejeMundo = transform.TransformDirection(ejeGiroLocal);
        float signo = Vector3.Dot(ejeMundo, camara.transform.forward) < 0f ? -1f : 1f;
        signoVisual = invertirGiroVisual ? -signo : signo;
    }

    void ActualizarLed(float progreso)
    {
        if (ledControlador != null)
        {
            ledControlador.colorNeon = Color.Lerp(colorLedApagado, colorLedEncendido, progreso);
            ledControlador.intensidad = Mathf.Lerp(intensidadInicio, intensidadFin, progreso);
            ledControlador.velocidadParpadeo = Mathf.Lerp(parpadeoInicio, parpadeoFin, progreso);
        }
    }
}