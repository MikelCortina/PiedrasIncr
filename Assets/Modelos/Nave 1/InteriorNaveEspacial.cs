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

    [Header("=== MARCADORES ESTROPEADOS ===")]
    [Tooltip("Lista de objetos (agujas) que van a rebotar/temblar")]
    public AgujaMarcador[] agujasMarcadores = new AgujaMarcador[3];

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

    private void ManejarNeonesAlarma()
    {
        if (neonesAlarma == null || neonesAlarma.Length == 0) return;

        // Usamos una onda senoidal (Sin) para que el encendido y apagado sea suave como una sirena real
        float ola = (Mathf.Sin(Time.time * velocidadAlarma) + 1f) / 2f;

        // Convertimos ese valor de 0-1 a los valores deseados (0 a 1.5)
        float intensidadActual = Mathf.Lerp(intensidadMinima, intensidadMaxima, ola);

        // Se lo aplicamos a todos los neones de la lista
        for (int i = 0; i < neonesAlarma.Length; i++)
        {
            if (neonesAlarma[i] != null)
            {
                neonesAlarma[i].intensidad = intensidadActual;
            }
        }
    }

    private void ManejarAgujasEstropeadas()
    {
        if (agujasMarcadores == null || agujasMarcadores.Length == 0) return;

        for (int i = 0; i < agujasMarcadores.Length; i++)
        {
            AgujaMarcador marcador = agujasMarcadores[i];

            if (marcador.aguja == null) continue;

            if (marcador.rotacionContinua)
            {
                // ============================================
                // MODO ROTACIÓN CONTINUA (Giro infinito)
                // ============================================
                marcador.anguloContinuoActual += Time.deltaTime * marcador.velocidadTemblor;

                // Mantenemos el ángulo dentro de los límites de 360 grados
                if (marcador.anguloContinuoActual >= 360f) marcador.anguloContinuoActual -= 360f;
                else if (marcador.anguloContinuoActual <= -360f) marcador.anguloContinuoActual += 360f;

                marcador.aguja.localRotation = marcador.rotacionBase * Quaternion.AngleAxis(marcador.anguloContinuoActual, marcador.ejeGiro);
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

                float anguloActual = Mathf.Lerp(marcador.anguloMinimo, marcador.anguloMaximo, marcador.progresoGiro);
                marcador.aguja.localRotation = marcador.rotacionBase * Quaternion.AngleAxis(anguloActual, marcador.ejeGiro);
            }
        }
    }
}