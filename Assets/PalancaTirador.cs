using UnityEngine;
using UnityEngine.Events;

public class PalancaTirador : MonoBehaviour
{
    [Header("Configuración de Arrastre (Mirada)")]
    [Tooltip("Unidades imaginarias que hay que bajar la mirada para alcanzar el máximo del tirador")]
    public float distanciaArrastreMax = 150f;
    [Tooltip("Suavizado al soltar la palanca si te arrepientes antes de llegar al final")]
    public float velocidadRetorno = 5f;

    [Header("Sincronización de Animaciones")]
    [Tooltip("Segundos que tardarán las puertas en abrirse y el tirador en volver arriba a la vez")]
    public float tiempoDeAnimacion = 1.5f;

    [Header("1. Movimiento del Tirador")]
    [Tooltip("Objeto que se desplazará físicamente (ej: el mango del tirador)")]
    public Transform objetoTirador;
    [Tooltip("Eje local por el que se moverá el tirador (ej: 0, -1, 0 para abajo)")]
    public Vector3 ejeMovimientoTirador = Vector3.down;
    [Tooltip("Cantidad máxima de unidades reales que se moverá el tirador en Unity")]
    public float distanciaMaximaTirador = 0.5f;

    [Header("2. Rotación de las Puertas")]
    [Tooltip("Primer objeto a rotar (ej: Puerta Izquierda)")]
    public Transform objetoRotacion1;
    [Tooltip("Eje local de rotación para el primer objeto")]
    public Vector3 ejeRotacion1 = Vector3.up;
    [Tooltip("Grados exactos que rotará al activarse (ej: 90)")]
    public float gradosARotar1 = 90f;

    [Space(10)]
    [Tooltip("Segundo objeto a rotar (ej: Puerta Derecha)")]
    public Transform objetoRotacion2;
    [Tooltip("Eje local de rotación para el segundo objeto")]
    public Vector3 ejeRotacion2 = Vector3.up;
    [Tooltip("Grados exactos que rotará al activarse (ej: -90)")]
    public float gradosARotar2 = -90f;

    [Header("3. Feedback Visual (Neón)")]
    [Tooltip("Controlador Neón que se encenderá al bajar la palanca y abrir las puertas")]
    public ControladorNeon controladorNeon;
    [Tooltip("Intensidad mínima (progreso 0)")]
    public float intensidadNeonMinima = 0.5f;
    [Tooltip("Intensidad máxima (progreso 1)")]
    public float intensidadNeonMaxima = 3f;

    [Header("Eventos Opcionales")]
    public UnityEvent AlLlegarAlTope;
    public UnityEvent AlSoltar;

    // --- Variables Internas ---
    private Vector3 posOriginalTirador;

    // Rotaciones base y objetivo
    private Quaternion rotacionOriginal1;
    private Quaternion rotacionObjetivo1;
    private Quaternion rotacionOriginal2;
    private Quaternion rotacionObjetivo2;

    // Progresos independientes (de 0 a 1)
    private float progresoTirador = 0f;
    private float progresoPuertas = 0f;

    // ESTADOS DE LA MAQUINA:
    // 0 = Reposo
    // 1 = Arrastrando (Usuario controlando)
    // 2 = Retornando Tirador y Abriendo Puertas (Animación sincronizada)
    // 3 = Abierto (Esperando Misil)
    // 4 = Cerrando Puertas
    private int estado = 0;

    private void Start()
    {
        if (objetoTirador != null) posOriginalTirador = objetoTirador.localPosition;

        if (objetoRotacion1 != null)
        {
            rotacionOriginal1 = objetoRotacion1.localRotation;
            rotacionObjetivo1 = rotacionOriginal1 * Quaternion.AngleAxis(gradosARotar1, ejeRotacion1);
        }

        if (objetoRotacion2 != null)
        {
            rotacionOriginal2 = objetoRotacion2.localRotation;
            rotacionObjetivo2 = rotacionOriginal2 * Quaternion.AngleAxis(gradosARotar2, ejeRotacion2);
        }

        if (controladorNeon != null) controladorNeon.intensidad = intensidadNeonMinima;
    }

    private void Update()
    {
        float velocidadAnimacion = 1f / Mathf.Max(0.1f, tiempoDeAnimacion);

        if (estado == 0) // Reposo
        {
            progresoTirador = Mathf.MoveTowards(progresoTirador, 0f, Time.deltaTime * velocidadRetorno);
            progresoPuertas = 0f;
        }
        else if (estado == 1) // Arrastrando
        {
            progresoPuertas = 0f;
        }
        else if (estado == 2) // Animación Simultánea: Tirador sube y Puertas abren
        {
            progresoTirador = Mathf.MoveTowards(progresoTirador, 0f, Time.deltaTime * velocidadAnimacion);
            progresoPuertas = Mathf.MoveTowards(progresoPuertas, 1f, Time.deltaTime * velocidadAnimacion);

            if (progresoTirador == 0f && progresoPuertas == 1f)
            {
                estado = 3; // Totalmente abierto
            }
        }
        else if (estado == 3) // Abierto y esperando el misil
        {
            progresoTirador = 0f;
            progresoPuertas = 1f;
        }
        else if (estado == 4) // Cerrando las puertas tras recibir el misil
        {
            progresoPuertas = Mathf.MoveTowards(progresoPuertas, 0f, Time.deltaTime * velocidadAnimacion);
            if (progresoPuertas == 0f)
            {
                estado = 0; // Vuelve al reposo inicial
            }
        }

        // --- 1. APLICAR TRANSFORMACIÓN AL TIRADOR ---
        if (objetoTirador != null)
        {
            Vector3 direccionOrientada = objetoTirador.localRotation * ejeMovimientoTirador.normalized;
            objetoTirador.localPosition = posOriginalTirador + (direccionOrientada * distanciaMaximaTirador * progresoTirador);
        }

        // --- 2. APLICAR ROTACIÓN A LAS PUERTAS ---
        if (objetoRotacion1 != null)
        {
            objetoRotacion1.localRotation = Quaternion.Slerp(rotacionOriginal1, rotacionObjetivo1, progresoPuertas);
        }

        if (objetoRotacion2 != null)
        {
            objetoRotacion2.localRotation = Quaternion.Slerp(rotacionOriginal2, rotacionObjetivo2, progresoPuertas);
        }

        // --- 3. APLICAR LUZ AL NEÓN ---
        if (controladorNeon != null)
        {
            float factorNeon = Mathf.Max(progresoTirador, progresoPuertas);
            controladorNeon.intensidad = Mathf.Lerp(intensidadNeonMinima, intensidadNeonMaxima, factorNeon);
        }
    }

    public void IniciarArrastre()
    {
        if (estado == 0 || estado == 1)
        {
            estado = 1;
            progresoTirador = 0f;
        }
    }

    public void ActualizarArrastre(float acumuladorMirada)
    {
        if (estado == 1)
        {
            progresoTirador = Mathf.Clamp01(acumuladorMirada / distanciaArrastreMax);

            if (progresoTirador >= 1f)
            {
                AlLlegarAlTope?.Invoke();
                estado = 2; // Disparamos la apertura
            }
        }
    }

    public void SoltarArrastre()
    {
        if (estado == 1)
        {
            estado = 0;
            AlSoltar?.Invoke();
        }
    }

    public void CerrarPuertasMecanismo()
    {
        if (estado == 3 || estado == 2)
        {
            estado = 4; // Comienza a cerrar
        }
    }

    public bool EstanPuertasAbiertas => (estado == 3);

    // NUEVO: Propiedad para avisar al cargador de que ya está todo cerrado y reseteado
    public bool EstanPuertasCerradas => (estado == 0);
}