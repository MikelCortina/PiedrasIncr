using UnityEngine;

public class InteriorNaveEspacial : MonoBehaviour
{
    [Header("=== ALARMA DE NEONES ===")]
    [Tooltip("Lista de luces de neón que parpadearán simulando alarma")]
    public ControladorNeon[] neonesAlarma;

    [Tooltip("Velocidad a la que la luz oscila de apagado a encendido")]
    public float velocidadAlarma = 2f;
    public float intensidadMinima = 0f;
    public float intensidadMaxima = 1.5f;

    [Tooltip("Multiplicador global de la intensidad de la alarma (1 = valores del Inspector, 0 = apagada). Lo controla la intro si está asignado.")]
    [Range(0f, 1f)] public float multiplicadorLucesAlarma = 1f;

    [Header("=== MARCADORES ESTROPEADOS ===")]
    [Tooltip("Lista de objetos (agujas) que van a rebotar/temblar")]
    public AgujaMarcador[] agujasMarcadores = new AgujaMarcador[3];

    [Tooltip("Multiplicador global de la vibración de las agujas (1 = valores del Inspector, 0 = quietas). Lo controla la intro si está asignado.")]
    [Range(0f, 1f)] public float multiplicadorVibracionAgujas = 1f;

    [Tooltip("Energía de la nave para las agujas (1 = encendida, 0 = apagada). Frena las de rotación continua y aquieta las de temblor. Lo controla la intro al bajar la palanca.")]
    [Range(0f, 1f)] public float multiplicadorEnergiaAgujas = 1f;

    [Tooltip("Segundos que tarda una aguja pausada en volver suavemente a su rotación original")]
    public float duracionVueltaReposo = 0.3f;

    // =========================================================
    // CLASE PARA CONFIGURAR CADA AGUJA INDIVIDUALMENTE
    // =========================================================
    [System.Serializable]
    public class AgujaMarcador
    {
        [Tooltip("Nombre para organizarte en el inspector")]
        public string nombre = "Marcador Gasolina";

        [Tooltip("El Transform del objeto (la aguja) que va a rotar")]
        public Transform aguja;

        [Tooltip("El eje sobre el que gira (Ej: Z = Vector3.forward)")]
        public Vector3 ejeGiro = Vector3.forward;

        [Header("Modo de Movimiento")]
        [Tooltip("Si está marcado, la aguja dará vueltas en bucle. Si no, rebotará entre los ángulos mínimo y máximo.")]
        public bool rotacionContinua = false;

        [Tooltip("Velocidad de movimiento. (En rotación continua son los grados que avanza por segundo)")]
        public float velocidadTemblor = 15f;

        [Tooltip("El ángulo mínimo donde se atasca. (NO se usa si Rotación Continua está activada)")]
        public float anguloMinimo = -2f;

        [Tooltip("El ángulo máximo al que rebota. (NO se usa si Rotación Continua está activada)")]
        public float anguloMaximo = 5f;

        // Variables ocultas para que el script calcule la posición internamente
        [HideInInspector] public Quaternion rotacionBase;
        [HideInInspector] public float progresoGiro = 0f;
        [HideInInspector] public bool direccionIda = true;
        [HideInInspector] public float anguloContinuoActual = 0f;

        // Pausa individual (ej: la manilla que pasa a controlar una palanca)
        [HideInInspector] public bool pausada = false;
        [HideInInspector] public float factorActividad = 1f; // 1 = vibra normal, 0 = en reposo

        // Giro añadido desde fuera (ej: el volante girando con las curvas), que se suma a la vibración.
        [HideInInspector] public float giroExtra = 0f;
        [HideInInspector] public Vector3 ejeGiroExtra = Vector3.forward;
    }

    void Start()
    {
        // Guardamos la rotación original de todas las agujas para no desfasarlas
        for (int i = 0; i < agujasMarcadores.Length; i++)
        {
            if (agujasMarcadores[i].aguja != null)
            {
                agujasMarcadores[i].rotacionBase = agujasMarcadores[i].aguja.localRotation;
            }
        }
    }

    void Update()
    {
        ManejarNeonesAlarma();
        ManejarAgujasEstropeadas();
    }

    // =========================================================
    // PAUSA DE AGUJAS INDIVIDUALES
    // =========================================================

    // Pausa o reanuda la aguja cuyo Transform coincida. Devuelve false si no está en la lista.
    public bool PausarAguja(Transform objeto, bool pausar)
    {
        AgujaMarcador marcador = BuscarAguja(objeto);

        if (marcador == null)
            return false;

        marcador.pausada = pausar;
        return true;
    }

    // Suma un giro (en grados, sobre un eje local) a la aguja cuyo Transform coincida, encima de su vibración.
    // Así otro script puede girarla sin pisar su rotación. Devuelve false si no está en la lista.
    public bool EstablecerGiroExtra(Transform objeto, float grados, Vector3 eje)
    {
        AgujaMarcador marcador = BuscarAguja(objeto);

        if (marcador == null)
            return false;

        marcador.giroExtra = grados;
        marcador.ejeGiroExtra = eje;
        return true;
    }

    // True si la aguja no está en la lista o si ya ha vuelto del todo a su rotación original.
    public bool AgujaEnReposo(Transform objeto)
    {
        AgujaMarcador marcador = BuscarAguja(objeto);
        return marcador == null || (marcador.pausada && marcador.factorActividad <= 0f);
    }

    private AgujaMarcador BuscarAguja(Transform objeto)
    {
        if (objeto == null || agujasMarcadores == null)
            return null;

        for (int i = 0; i < agujasMarcadores.Length; i++)
        {
            if (agujasMarcadores[i].aguja == objeto)
                return agujasMarcadores[i];
        }

        return null;
    }

    // =========================================================
    // NEONES
    // =========================================================

    private void ManejarNeonesAlarma()
    {
        if (neonesAlarma == null || neonesAlarma.Length == 0) return;

        // Usamos una onda senoidal (Sin) para que el encendido y apagado sea suave como una sirena real
        float ola = (Mathf.Sin(Time.time * velocidadAlarma) + 1f) / 2f;

        // Convertimos ese valor de 0-1 a los valores deseados (0 a 1.5) y aplicamos el multiplicador global
        float intensidadActual = Mathf.Lerp(intensidadMinima, intensidadMaxima, ola) * Mathf.Clamp01(multiplicadorLucesAlarma);

        // Se lo aplicamos a todos los neones de la lista
        for (int i = 0; i < neonesAlarma.Length; i++)
        {
            if (neonesAlarma[i] != null)
            {
                neonesAlarma[i].intensidad = intensidadActual;
            }
        }
    }

    // =========================================================
    // AGUJAS
    // =========================================================

    private void ManejarAgujasEstropeadas()
    {
        if (agujasMarcadores == null || agujasMarcadores.Length == 0) return;

        // La energía se suma a la reducción por velocidad: con la nave apagada todo se detiene.
        float factorGlobal = Mathf.Clamp01(multiplicadorVibracionAgujas) * Mathf.Clamp01(multiplicadorEnergiaAgujas);
        float velocidadReposo = 1f / Mathf.Max(0.01f, duracionVueltaReposo);

        for (int i = 0; i < agujasMarcadores.Length; i++)
        {
            AgujaMarcador marcador = agujasMarcadores[i];

            if (marcador.aguja == null) continue;

            // Una aguja pausada que ya está en reposo deja de escribir su rotación,
            // así otro script (ej: PalancaTirador) puede controlar el objeto sin interferencias.
            if (marcador.pausada && marcador.factorActividad <= 0f) continue;

            marcador.factorActividad = Mathf.MoveTowards(
                marcador.factorActividad,
                marcador.pausada ? 0f : 1f,
                Time.deltaTime * velocidadReposo
            );

            float anguloActual;

            if (marcador.rotacionContinua)
            {
                // ============================================
                // MODO ROTACIÓN CONTINUA (Giro infinito)
                // ============================================
                // El multiplicador frena el giro; con energía 0 la aguja se queda parada donde esté.
                marcador.anguloContinuoActual += Time.deltaTime * marcador.velocidadTemblor * factorGlobal;

                // Mantenemos el ángulo dentro de los límites de 360 grados
                if (marcador.anguloContinuoActual >= 360f) marcador.anguloContinuoActual -= 360f;
                else if (marcador.anguloContinuoActual <= -360f) marcador.anguloContinuoActual += 360f;

                anguloActual = marcador.anguloContinuoActual;
            }
            else
            {
                // ============================================
                // MODO TEMBLOR / PING-PONG (Mecánica estropeada)
                // ============================================
                if (marcador.direccionIda)
                {
                    marcador.progresoGiro += Time.deltaTime * marcador.velocidadTemblor;
                    if (marcador.progresoGiro >= 1f)
                    {
                        marcador.progresoGiro = 1f;
                        marcador.direccionIda = false;
                    }
                }
                else
                {
                    marcador.progresoGiro -= Time.deltaTime * marcador.velocidadTemblor;
                    if (marcador.progresoGiro <= 0f)
                    {
                        marcador.progresoGiro = 0f;
                        marcador.direccionIda = true;
                    }
                }

                // El multiplicador global reduce la amplitud alrededor del centro del recorrido.
                float anguloCompleto = Mathf.Lerp(marcador.anguloMinimo, marcador.anguloMaximo, marcador.progresoGiro);
                float anguloCentro = (marcador.anguloMinimo + marcador.anguloMaximo) * 0.5f;
                anguloActual = Mathf.Lerp(anguloCentro, anguloCompleto, factorGlobal);
            }

            // Al pausar, el ángulo se lleva suavemente a 0 (rotación original exacta).
            float anguloAplicado = Mathf.LerpAngle(0f, anguloActual, marcador.factorActividad);

            marcador.aguja.localRotation =
                marcador.rotacionBase *
                Quaternion.AngleAxis(marcador.giroExtra, marcador.ejeGiroExtra) *
                Quaternion.AngleAxis(anguloAplicado, marcador.ejeGiro);
        }
    }
}