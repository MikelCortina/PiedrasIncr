using System.Collections;
using UnityEngine;
using TMPro;

public class Fisionador : MonoBehaviour
{
    [System.Serializable]
    public class NivelFision
    {
        [Tooltip("Cantidad de piedras necesarias en este nivel para fisionar")]
        public int piedrasNecesarias = 5;
        [Tooltip("El objeto que será expulsado al completar este nivel")]
        public GameObject prefabAExpulsar;
    }

    [Header("Configuración de Niveles")]
    [Tooltip("Lista de niveles. Cada nivel pide X piedras para expulsar su prefab.")]
    public NivelFision[] niveles;

    [Tooltip("Etiqueta (Tag) que deben tener las piedras para ser detectadas")]
    public string tagPiedra = "Piedra";

    [Header("Pantalla LED (Contador de Piedras)")]
    [Tooltip("Texto TextMeshPro donde se mostrará cuántas piedras faltan")]
    public TMP_Text textoPantalla;
    [Tooltip("Texto TextMeshPro donde se mostrará el estado actual de la máquina")]
    public TMP_Text textoEstado;
    [Tooltip("Formato del texto. Pon '0' para números normales, '00' si quieres que el 5 se vea como 05.")]
    public string formatoNumeros = "0";

    [Header("Animación de Trabajo (Shape Key y Neón)")]
    [Tooltip("El modelo que se deformará mientras haya piedras dentro")]
    public SkinnedMeshRenderer skinnedMeshRenderer;
    [Tooltip("Índice del Blend Shape (0 suele ser el primero)")]
    public int indiceBlendShape = 0;
    [Tooltip("Velocidad a la que el Shape Key va de 0 a 100 y viceversa")]
    public float velocidadShapeKey = 150f;

    [Space(5)]
    [Tooltip("El controlador de neón que latirá al mismo ritmo que el Shape Key")]
    public ControladorNeon controladorNeon;
    [Tooltip("Intensidad del neón cuando la máquina está en reposo o descargada (Shape Key = 0)")]
    public float intensidadNeonDescargado = 0.5f;
    [Tooltip("Intensidad del neón en el punto máximo del latido (Shape Key = 100)")]
    public float intensidadNeonCargado = 3f;

    [Header("Efectos de Burst (Partículas)")]
    [Tooltip("Sistema principal que explotará con varias partículas al alcanzar el máximo (100)")]
    public ParticleSystem particulasBurst;
    [Tooltip("Cantidad de partículas emitidas en la cima del latido para el sistema principal")]
    public int cantidadParticulasPorBurst = 30;

    [Space(5)]
    [Tooltip("Lista de sistemas que emitirán 1 sola partícula al alcanzar el TOPE (100 / 1)")]
    public ParticleSystem[] particulasBurstAlMaximo;
    [Tooltip("Lista de sistemas que emitirán 1 sola partícula al volver al REPOSO (0)")]
    public ParticleSystem[] particulasBurstAlCero;

    [Header("Objeto Secundario (Movimiento)")]
    [Tooltip("El objeto que se moverá al completar un nivel")]
    public Transform objetoSecundario;
    [Tooltip("Eje y dirección hacia donde se moverá (Ej: 0, 1, 0 para arriba)")]
    public Vector3 ejeMovimiento = Vector3.up;
    [Tooltip("Cuánta distancia recorrerá en ese eje por cada nivel completado")]
    public float distanciaMovimientoPorNivel = 1.5f;
    [Tooltip("Velocidad visual a la que se desplaza hacia su nueva posición")]
    public float velocidadMovimientoSecundario = 5f;

    [Header("Expulsión (Fisión)")]
    [Tooltip("Punto desde el cual nacerá el prefab expulsado")]
    public Transform puntoExpulsion;
    [Tooltip("Velocidad a la que el objeto expulsado crece de 0 a 1")]
    public float velocidadCrecimientoExpulsado = 3f;

    [Header("Físicas del Objeto Expulsado")]
    [Tooltip("Activa esto si quieres que el objeto salga disparado al expulsarse.")]
    public bool aplicarFuerzaFisica = false;
    [Tooltip("La dirección hacia donde saldrá disparado (Local al Punto de Expulsión)")]
    public Vector3 direccionExpulsion = Vector3.up;
    [Tooltip("Fuerza con la que saldrá disparado (Impulso)")]
    public float fuerzaExpulsion = 10f;
    [Tooltip("Fuerza de rotación aleatoria que se le aplicará al salir")]
    public float fuerzaRotacionRandom = 5f;

    // --- Variables de Estado Internas ---
    private int nivelActual = 0;
    private int piedrasActuales = 0;
    private float progresoShape = 0f;
    private bool subiendoShape = true;
    private Vector3 posicionObjetivoSecundario;

    private void Start()
    {
        if (objetoSecundario != null)
        {
            posicionObjetivoSecundario = objetoSecundario.localPosition;
        }
        ActualizarPantalla();
    }

    private void Update()
    {
        // Movimiento suave del objeto secundario hacia su objetivo
        if (objetoSecundario != null)
        {
            objetoSecundario.localPosition = Vector3.MoveTowards(
                objetoSecundario.localPosition,
                posicionObjetivoSecundario,
                Time.deltaTime * velocidadMovimientoSecundario
            );
        }

        // Animación del Shape Key y el Neón (El "motor" trabajando)
        if (piedrasActuales > 0 && nivelActual < niveles.Length)
        {
            if (subiendoShape)
            {
                progresoShape += Time.deltaTime * velocidadShapeKey;

                if (progresoShape >= 100f)
                {
                    progresoShape = 100f;
                    subiendoShape = false;

                    if (particulasBurst != null)
                    {
                        particulasBurst.Emit(cantidadParticulasPorBurst);
                    }
                    EmitirBurstAlMaximo();
                }
            }
            else
            {
                progresoShape -= Time.deltaTime * velocidadShapeKey;

                if (progresoShape <= 0f)
                {
                    progresoShape = 0f;
                    subiendoShape = true;
                    EmitirBurstAlCero();
                }
            }
        }
        else if (progresoShape > 0f)
        {
            // Si no hay piedras, la máquina se desinfla suavemente hasta descansar en 0
            float progresoAnterior = progresoShape;
            progresoShape = Mathf.MoveTowards(progresoShape, 0f, Time.deltaTime * velocidadShapeKey);

            // Si justo en este frame acaba de tocar el 0
            if (progresoShape <= 0f && progresoAnterior > 0f)
            {
                subiendoShape = true;
                EmitirBurstAlCero();
            }
        }

        // Aplicamos el progreso al modelo y al neón
        if (skinnedMeshRenderer != null)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(indiceBlendShape, progresoShape);
        }

        if (controladorNeon != null)
        {
            // El Blend Shape va de 0 a 100, así que lo dividimos entre 100 para tener un valor de 0 a 1
            float factor = progresoShape / 100f;
            controladorNeon.intensidad = Mathf.Lerp(intensidadNeonDescargado, intensidadNeonCargado, factor);
        }
    }

    private void EmitirBurstAlMaximo()
    {
        if (particulasBurstAlMaximo == null || particulasBurstAlMaximo.Length == 0) return;

        foreach (ParticleSystem ps in particulasBurstAlMaximo)
        {
            if (ps != null) ps.Emit(1);
        }
    }

    private void EmitirBurstAlCero()
    {
        if (particulasBurstAlCero == null || particulasBurstAlCero.Length == 0) return;

        foreach (ParticleSystem ps in particulasBurstAlCero)
        {
            if (ps != null) ps.Emit(1);
        }
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (nivelActual >= niveles.Length) return;

        if (otro.CompareTag(tagPiedra))
        {
            Destroy(otro.gameObject);
            piedrasActuales++;
            ActualizarPantalla();

            if (piedrasActuales >= niveles[nivelActual].piedrasNecesarias)
            {
                CompletarNivelFision();
            }
        }
    }

    private void CompletarNivelFision()
    {
        if (niveles[nivelActual].prefabAExpulsar != null && puntoExpulsion != null)
        {
            GameObject expulsado = Instantiate(niveles[nivelActual].prefabAExpulsar, puntoExpulsion.position, puntoExpulsion.rotation);
            StartCoroutine(RutinaCrecimientoYExpulsion(expulsado));
        }

        if (objetoSecundario != null)
        {
            posicionObjetivoSecundario += ejeMovimiento.normalized * distanciaMovimientoPorNivel;
        }

        piedrasActuales = 0;
        nivelActual++;
        ActualizarPantalla();
    }

    private void ActualizarPantalla()
    {
        if (textoPantalla != null)
        {
            if (nivelActual >= niveles.Length)
            {
                textoPantalla.text = "MAX";
            }
            else
            {
                int piedrasRestantes = niveles[nivelActual].piedrasNecesarias - piedrasActuales;
                textoPantalla.text = Mathf.Max(0, piedrasRestantes).ToString(formatoNumeros);
            }
        }

        if (textoEstado != null)
        {
            if (nivelActual >= niveles.Length)
            {
                textoEstado.text = "FINISHED";
            }
            else if (piedrasActuales == 0)
            {
                textoEstado.text = "NEED STONES";
            }
            else
            {
                textoEstado.text = "MORE STONES";
            }
        }
    }

    private IEnumerator RutinaCrecimientoYExpulsion(GameObject objetoExpulsado)
    {
        Transform objetivo = objetoExpulsado.transform;
        Vector3 escalaFinal = objetivo.localScale;
        objetivo.localScale = Vector3.zero;

        // Fase 1: Crecimiento
        float t = 0f;
        while (t < 1f)
        {
            if (objetivo == null) yield break;

            t += Time.deltaTime * velocidadCrecimientoExpulsado;
            objetivo.localScale = Vector3.Lerp(Vector3.zero, escalaFinal, t);
            yield return null;
        }

        if (objetivo != null)
        {
            objetivo.localScale = escalaFinal;

            // Fase 2: Aplicar físicas (si está activado) una vez ha crecido del todo
            if (aplicarFuerzaFisica)
            {
                Rigidbody rb = objetoExpulsado.GetComponent<Rigidbody>();
                if (rb == null) rb = objetoExpulsado.AddComponent<Rigidbody>();

                // Calculamos la dirección basándonos en la rotación del punto de expulsión
                Vector3 direccionMundo = puntoExpulsion != null ? puntoExpulsion.TransformDirection(direccionExpulsion.normalized) : direccionExpulsion.normalized;

                rb.AddForce(direccionMundo * fuerzaExpulsion, ForceMode.Impulse);

                Vector3 torqueAleatorio = new Vector3(
                    Random.Range(-fuerzaRotacionRandom, fuerzaRotacionRandom),
                    Random.Range(-fuerzaRotacionRandom, fuerzaRotacionRandom),
                    Random.Range(-fuerzaRotacionRandom, fuerzaRotacionRandom)
                );
                rb.AddTorque(torqueAleatorio, ForceMode.Impulse);
            }
        }
    }
}