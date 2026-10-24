using UnityEngine;

public class RuletaInteractivaGasoil : MonoBehaviour
{
    [Header("Ajustes de Giro de la Ruleta")]
    [Tooltip("Ángulo objetivo para activar el mecanismo (Ej: 720 grados = 2 vueltas completas)")]
    public float anguloObjetivo = 720f;
    [Tooltip("Eje local sobre el cual girará la ruleta (Normalmente Vector3.forward, Vector3.up o Vector3.right)")]
    public Vector3 ejeGiroLocal = Vector3.forward;

    [Min(1f)]
    [Tooltip("Velocidad lineal a la que la ruleta se desenrosca (grados por segundo)")]
    public float velocidadRetorno = 300f;

    [Header("Sensación Física (Game Feel)")]
    [Tooltip("Multiplicador de velocidad cuando está a punto de llegar al objetivo (0.15 = muy dura, 1 = suave siempre)")]
    [Range(0.01f, 1f)] public float suavidadFinal = 0.15f;

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

    // Variables para el objeto secundario (Tapa / Válvula)
    private Quaternion rotacionOrigenSecundario;
    private Vector3 posicionOrigenSecundario;

    // Máquina de estados
    private enum EstadoRuleta { Girando, Vaciando, Retornando, Completado }
    private EstadoRuleta estadoActual = EstadoRuleta.Girando;

    private bool estaSiendoGirada = false;

    // Sincronización del tiempo de retorno
    private float progresoSecundario = 0f;

    // Progreso del efecto de "Vaciado" (Descarga del LED y bajada del eje Y)
    private float progresoVaciado = 1f;

    void Start()
    {
        rotacionOrigen = transform.localRotation;
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
                gradosAcumuladosGiro = Mathf.MoveTowards(gradosAcumuladosGiro, 0f, velocidadRetorno * Time.deltaTime);
                transform.localRotation = rotacionOrigen * Quaternion.AngleAxis(gradosAcumuladosGiro, ejeGiroLocal);

                // LED se adapta al giro actual
                float progresoGiro = Mathf.Clamp01(gradosAcumuladosGiro / anguloObjetivo);
                ActualizarLed(progresoGiro);
            }

            // Aseguramos que el objeto secundario esté cerrado
            if (objetoSecundario != null)
            {
                float tiempoDeRetornoTotal = anguloObjetivo / velocidadRetorno;
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
            gradosAcumuladosGiro = Mathf.MoveTowards(gradosAcumuladosGiro, 0f, velocidadRetorno * Time.deltaTime);
            transform.localRotation = rotacionOrigen * Quaternion.AngleAxis(gradosAcumuladosGiro, ejeGiroLocal);

            // C. El objeto secundario se ROTA abriéndose
            if (objetoSecundario != null)
            {
                float tiempoDeRetornoTotal = anguloObjetivo / velocidadRetorno;
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

    // Método llamado desde GestorRuletasFPS
    public void ModificarDesdeGiroVisual(float deltaGiro)
    {
        // Si no estamos en la fase normal de giro, ignoramos el input del jugador por completo
        if (estadoActual != EstadoRuleta.Girando) return;

        estaSiendoGirada = true;

        // TENSIÓN
        float progresoActual = Mathf.Clamp01(gradosAcumuladosGiro / anguloObjetivo);
        float multiplicadorTension = Mathf.Lerp(1f, suavidadFinal, progresoActual);

        // Solo aplicamos resistencia si estamos intentando avanzar
        if (deltaGiro > 0)
        {
            deltaGiro *= multiplicadorTension;
        }

        gradosAcumuladosGiro += deltaGiro;
        if (gradosAcumuladosGiro < 0f) gradosAcumuladosGiro = 0f;

        // COMPROBAR ACTIVACIÓN (Alcanzar los 720 grados)
        if (gradosAcumuladosGiro >= anguloObjetivo)
        {
            gradosAcumuladosGiro = anguloObjetivo;
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
        transform.localRotation = rotacionOrigen * Quaternion.AngleAxis(gradosAcumuladosGiro, ejeGiroLocal);

        // LED acompaña al giro manual
        float progresoLed = Mathf.Clamp01(gradosAcumuladosGiro / anguloObjetivo);
        ActualizarLed(progresoLed);
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