using UnityEngine;
using UnityEngine.Events;

public class PalancaTirador : MonoBehaviour
{
    public enum EspacioMovimiento
    {
        EjesLocalesDelObjeto, // El eje del propio tirador (gizmo en modo Local)
        EjesDelPadre          // El eje del padre del tirador
    }

    [Header("Configuración de Arrastre (Mirada)")]
    [Tooltip("Unidades imaginarias que hay que bajar la mirada para alcanzar el máximo del tirador")]
    public float distanciaArrastreMax = 150f;
    [Tooltip("Suavizado al soltar la palanca si te arrepientes antes de llegar al final")]
    public float velocidadRetorno = 5f;

    [Header("Sincronización de Animaciones")]
    [Tooltip("Segundos que tardarán las puertas en abrirse y el tirador en volver arriba a la vez")]
    public float tiempoDeAnimacion = 1.5f;

    [Header("Comportamiento")]
    [Tooltip("Si está bloqueada no se puede agarrar ni arrastrar (la mirilla tampoco la marca como interactiva)")]
    [SerializeField] private bool bloqueada = false;
    [Tooltip("Activado: tras bajarla se queda 'abierta' hasta que alguien llame a CerrarPuertasMecanismo (ej: CargadorMisil). " +
             "Desactivado: al terminar la animación vuelve sola a reposo y se puede usar otra vez.")]
    public bool esperarCierreManual = true;

    [Header("1. Movimiento del Tirador")]
    [Tooltip("Objeto que se desplazará físicamente (ej: el mango del tirador)")]
    public Transform objetoTirador;
    [Tooltip("En qué ejes se interpreta 'Eje Movimiento Tirador'. Local del objeto = el gizmo de Unity en modo Local con el tirador seleccionado.")]
    public EspacioMovimiento espacioMovimiento = EspacioMovimiento.EjesLocalesDelObjeto;
    [Tooltip("Eje por el que se moverá el tirador (ej: 0, -1, 0 para abajo)")]
    public Vector3 ejeMovimientoTirador = Vector3.down;
    [Tooltip("Cantidad máxima de unidades que se moverá el tirador (en unidades del padre del tirador)")]
    public float distanciaMaximaTirador = 0.5f;

    [Header("2. Rotación de las Puertas (Opcional)")]
    [Tooltip("Primer objeto a rotar (ej: Puerta Izquierda). Puede quedarse vacío.")]
    public Transform objetoRotacion1;
    [Tooltip("Eje local de rotación para el primer objeto")]
    public Vector3 ejeRotacion1 = Vector3.up;
    [Tooltip("Grados exactos que rotará al activarse (ej: 90)")]
    public float gradosARotar1 = 90f;

    [Space(10)]
    [Tooltip("Segundo objeto a rotar (ej: Puerta Derecha). Puede quedarse vacío.")]
    public Transform objetoRotacion2;
    [Tooltip("Eje local de rotación para el segundo objeto")]
    public Vector3 ejeRotacion2 = Vector3.up;
    [Tooltip("Grados exactos que rotará al activarse (ej: -90)")]
    public float gradosARotar2 = -90f;

    [Header("3. Feedback Visual (Neón, Opcional)")]
    [Tooltip("Controlador Neón que se encenderá al bajar la palanca y abrir las puertas. Puede quedarse vacío.")]
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

    // Dirección del recorrido expresada en el espacio del padre (localPosition).
    // Se calcula una sola vez para que otros scripts que roten el tirador (ej: vibración) no tuerzan el recorrido.
    private Vector3 direccionMovimientoPadre;

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

    public bool EstaBloqueada => bloqueada;

    private void Start()
    {
        if (objetoTirador != null)
        {
            posOriginalTirador = objetoTirador.localPosition;
            direccionMovimientoPadre = CalcularDireccionMovimientoPadre(objetoTirador.localRotation);
        }

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

    private Vector3 CalcularDireccionMovimientoPadre(Quaternion rotacionLocalTirador)
    {
        Vector3 eje = ejeMovimientoTirador.sqrMagnitude > 0.000001f ? ejeMovimientoTirador.normalized : Vector3.down;

        // localPosition vive en el espacio del padre: para seguir el eje propio del tirador
        // hay que girar el eje con la rotación local del tirador.
        return espacioMovimiento == EspacioMovimiento.EjesLocalesDelObjeto
            ? rotacionLocalTirador * eje
            : eje;
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
                // Con cierre manual se queda abierta esperando (misil); si no, vuelve a reposo.
                estado = esperarCierreManual ? 3 : 4;
            }
        }
        else if (estado == 3) // Abierto y esperando el misil
        {
            progresoTirador = 0f;
            progresoPuertas = 1f;
        }
        else if (estado == 4) // Cerrando las puertas
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
            objetoTirador.localPosition = posOriginalTirador + (direccionMovimientoPadre * distanciaMaximaTirador * progresoTirador);
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

    public void SetBloqueada(bool valor)
    {
        bloqueada = valor;

        // Si se bloquea mientras la están arrastrando, la soltamos para que vuelva arriba.
        if (bloqueada && estado == 1)
            estado = 0;
    }

    public void IniciarArrastre()
    {
        if (bloqueada)
            return;

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

    // Propiedad para avisar al cargador de que ya está todo cerrado y reseteado
    public bool EstanPuertasCerradas => (estado == 0);

    // =========================================================
    // GIZMO: dibuja el recorrido del tirador al seleccionarlo
    // =========================================================
    private void OnDrawGizmosSelected()
    {
        if (objetoTirador == null)
            return;

        Transform padre = objetoTirador.parent;

        // En Play usamos los datos guardados en Start; en el editor, la pose actual.
        Vector3 posicionLocal = Application.isPlaying ? posOriginalTirador : objetoTirador.localPosition;
        Vector3 direccion = Application.isPlaying ? direccionMovimientoPadre : CalcularDireccionMovimientoPadre(objetoTirador.localRotation);
        Vector3 finalLocal = posicionLocal + direccion * distanciaMaximaTirador;

        Vector3 inicio = padre != null ? padre.TransformPoint(posicionLocal) : posicionLocal;
        Vector3 fin = padre != null ? padre.TransformPoint(finalLocal) : finalLocal;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(inicio, fin);
        Gizmos.DrawWireSphere(fin, HandleSize(fin) * 0.05f);
    }

    private static float HandleSize(Vector3 posicion)
    {
#if UNITY_EDITOR
        return UnityEditor.HandleUtility.GetHandleSize(posicion);
#else
        return 0.1f;
#endif
    }
}