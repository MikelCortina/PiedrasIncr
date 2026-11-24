using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PiedrasIncr.Audio;

/// <summary>
/// Fisionador (hoja OBJETOS Y MEJORAS + FINAL).
/// 1. Bloqueado hasta comprarlo (precio de EconomiaDemo, id Fisionador = 300 monedas).
/// 2. Solo acepta esferas perfectas (DesgastePiedra.EsEsferaPerfecta); el resto rebota con sonido de error.
/// 3. Muestra el progreso "3 / 8" en un contador gigante visible desde lejos.
/// 4. Al llegar a EconomiaDemo.esferasParaMisil expulsa el MISIL GRANDE, que el jugador lleva al CargadorMisil.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Fisionador : MonoBehaviour
{
    // Formato antiguo (varios niveles). Solo se conserva para migrar el prefab del misil
    // que ya estaba asignado en la escena; no se usa en la lógica.
    [System.Serializable]
    public class NivelFision
    {
        public int piedrasNecesarias = 5;
        public GameObject prefabAExpulsar;
    }

    [SerializeField, HideInInspector] private NivelFision[] niveles;

    [Header("Desbloqueo (compra)")]
    [Tooltip("Estado actual. Desmarcado = la máquina no acepta nada hasta comprarla.")]
    public bool desbloqueado = false;
    [Tooltip("Respaldo si EconomiaDemo no tiene el id 'Fisionador'.")]
    [Min(0)] public int precioDesbloqueo = 300;
    [Tooltip("Vacío = se busca en la escena.")]
    public Cartera cartera;
    public KeyCode teclaComprar = KeyCode.E;
    [Tooltip("Distancia al collider de la máquina para poder comprarla.")]
    [Min(0.5f)] public float distanciaCompra = 6f;
    [Tooltip("Vacío = la cámara principal.")]
    public Transform jugador;
    [Tooltip("Se ven mientras está bloqueada (ruinas, lona, cartel...).")]
    public GameObject[] objetosBloqueado;
    [Tooltip("Se activan al comprarla (la máquina en sí, neones, pantallas...).")]
    public GameObject[] objetosDesbloqueado;
    [Tooltip("Aviso en pantalla al acercarse: [E] Comprar Fisionador (300 monedas)")]
    public TMP_Text textoAvisoCompra;
    [Tooltip("Desmarcado: el cartel LED flotante (FISSION / LOCKED) no se ve hasta comprar la máquina.")]
    public bool mostrarPanelBloqueado = false;
    public ParticleSystem[] particulasDesbloqueo;
    [SoundName] public string sonidoDesbloqueo = "";

    [Header("Esferas")]
    [Tooltip("Etiqueta (Tag) de las piedras")]
    public string tagPiedra = "Piedra";
    [Tooltip("Respaldo si no hay EconomiaDemo (Final > Esferas Para Misil).")]
    [Min(1)] public int esferasParaMisilRespaldo = 8;
    [Tooltip("Tras expulsar un misil, vuelve a 0 / 8 para fabricar otro (una fase lunar por misil).")]
    public bool repetirTrasMisil = true;
    [SoundName] public string sonidoEsferaAceptada = "";

    [Header("Rechazo (piedra no perfecta)")]
    [Tooltip("Velocidad horizontal con la que sale despedida la piedra rechazada")]
    public float fuerzaRebote = 6f;
    [Tooltip("Velocidad hacia arriba de la piedra rechazada")]
    public float fuerzaReboteArriba = 3f;
    [SoundName] public string sonidoRechazo = "ErrorFisionador";
    [Tooltip("Segundos mínimos entre dos sonidos de error")]
    public float intervaloSonidoRechazo = 0.2f;

    [Header("Pantalla LED y contador gigante")]
    [Tooltip("Pantalla de la máquina (render texture)")]
    public TMP_Text textoPantalla;
    [Tooltip("Texto de estado de la máquina")]
    public TMP_Text textoEstado;
    [Tooltip("TextMeshPro 3D grande sobre la máquina, visible desde lejos")]
    public TMP_Text contadorGigante;
    [Tooltip("{0} = esferas dentro, {1} = esferas necesarias")]
    public string formatoContador = "{0} / {1}";
    [Tooltip("El contador gigante gira para mirar siempre a la cámara")]
    public bool contadorMiraCamara = true;

    [Header("Contador estilo cartel LED (como el escáner de piedras)")]
    [Tooltip("Dibuja el contador como los paneles de la P (placa violeta, neón, dígitos LED) y oculta el texto 3D.")]
    public bool contadorEstiloLED = true;
    [Tooltip("Punto del mundo sobre el que flota el panel. Vacío = la posición del Contador Gigante.")]
    public Transform anclaContador;
    [Tooltip("Tamaño del panel (referencia 1080p).")]
    public float escalaContador = 1f;
    [Tooltip("A partir de esta distancia el panel deja de encogerse.")]
    public float distanciaEncogerContador = 120f;
    [Tooltip("0 = se ve a cualquier distancia (meta siempre a la vista).")]
    public float distanciaMaximaContador = 0f;
    [Tooltip("Vacío = la fuente del escáner de piedras (DS-DIGI).")]
    public Font fuenteContador;

    [Header("Misil grande")]
    [Tooltip("MisilNuclear.prefab (Modelos/Fisionador). Tag 'Misil' para poder cogerlo y cargarlo.")]
    public GameObject prefabMisilGrande;
    [SoundName] public string sonidoMisil = "";

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

    [Header("Objeto Secundario (sube con el progreso)")]
    [Tooltip("El objeto que sube a medida que entran esferas")]
    public Transform objetoSecundario;
    [Tooltip("Eje y dirección hacia donde se moverá (Ej: 0, 1, 0 para arriba)")]
    public Vector3 ejeMovimiento = Vector3.up;
    [Tooltip("Distancia total recorrida al llenar el misil (8 / 8)")]
    public float distanciaMovimientoPorNivel = 1.5f;
    [Tooltip("Velocidad visual a la que se desplaza hacia su nueva posición")]
    public float velocidadMovimientoSecundario = 5f;

    [Header("Expulsión (Fisión)")]
    [Tooltip("Punto desde el cual nacerá el misil")]
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

    // --- Estado interno ---
    private int piedrasActuales = 0;
    private bool completado = false;
    private float progresoShape = 0f;
    private bool subiendoShape = true;
    private Vector3 posicionBaseSecundario;
    private Vector3 posicionObjetivoSecundario;
    private Collider colliderMaquina;
    private float ultimoSonidoRechazo = -999f;
    private readonly HashSet<GameObject> consumidasEsteFrame = new HashSet<GameObject>();
    private int frameConsumo = -1;
    private float popContador;
    private float rechazoHasta = -999f;
    private float misilHasta = -999f;
    private GUIStyle estiloLED;

    // --- Datos de economía ---
    public int EsferasNecesarias
    {
        get
        {
            int valor = ProveedorEconomia.Actual.esferasParaMisil;
            return valor > 0 ? valor : esferasParaMisilRespaldo;
        }
    }

    public int PrecioDesbloqueo =>
        ProveedorEconomia.Actual.CosteOPorDefecto(EconomiaDemo.Ids.Fisionador, 1, precioDesbloqueo);

    public int EsferasActuales => piedrasActuales;
    public bool Desbloqueado => desbloqueado;
    public bool Completado => completado;

    // Misiles expulsados que el jugador aún no ha cargado en el lanzador (el cargador los destruye).
    private readonly List<GameObject> misilesSueltos = new List<GameObject>();

    /// <summary>Misiles grandes que siguen en el mundo sin lanzar (para el guardado).</summary>
    public int MisilesPendientes
    {
        get
        {
            misilesSueltos.RemoveAll(m => m == null);
            return misilesSueltos.Count;
        }
    }

    private GameObject PrefabMisil
    {
        get
        {
            if (prefabMisilGrande != null) return prefabMisilGrande;
            if (niveles != null)
                foreach (NivelFision n in niveles)
                    if (n != null && n.prefabAExpulsar != null) return n.prefabAExpulsar;
            return null;
        }
    }

    private void OnValidate()
    {
        // Migración: el prefab del nivel antiguo pasa al campo nuevo.
        if (prefabMisilGrande == null && PrefabMisil != null)
            prefabMisilGrande = PrefabMisil;
    }

    private void Start()
    {
        colliderMaquina = GetComponent<Collider>();
        if (cartera == null) cartera = FindFirstObjectByType<Cartera>();

        if (objetoSecundario != null)
        {
            posicionBaseSecundario = objetoSecundario.localPosition;
            posicionObjetivoSecundario = posicionBaseSecundario;
        }

        if (PrefabMisil == null)
            Debug.LogWarning(name + ": falta asignar el Prefab Misil Grande (MisilNuclear).", this);

        AplicarEstadoVisualDesbloqueo();
        if (textoAvisoCompra != null) textoAvisoCompra.gameObject.SetActive(false);

        // Con el estilo LED el texto 3D solo sirve de ancla: se oculta.
        if (contadorEstiloLED && contadorGigante != null)
        {
            if (anclaContador == null) anclaContador = contadorGigante.transform;
            contadorGigante.gameObject.SetActive(false);
        }
        ActualizarPantalla();
    }

    private void Update()
    {
        ManejarCompra();
        popContador = Mathf.MoveTowards(popContador, 0f, Time.unscaledDeltaTime * 1.2f);

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
        if (desbloqueado && piedrasActuales > 0 && !completado)
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
                    EmitirBurst(particulasBurstAlMaximo);
                }
            }
            else
            {
                progresoShape -= Time.deltaTime * velocidadShapeKey;

                if (progresoShape <= 0f)
                {
                    progresoShape = 0f;
                    subiendoShape = true;
                    EmitirBurst(particulasBurstAlCero);
                }
            }
        }
        else if (progresoShape > 0f)
        {
            // Sin esferas, la máquina se desinfla suavemente hasta descansar en 0
            float progresoAnterior = progresoShape;
            progresoShape = Mathf.MoveTowards(progresoShape, 0f, Time.deltaTime * velocidadShapeKey);

            if (progresoShape <= 0f && progresoAnterior > 0f)
            {
                subiendoShape = true;
                EmitirBurst(particulasBurstAlCero);
            }
        }

        if (skinnedMeshRenderer != null)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(indiceBlendShape, progresoShape);
        }

        if (controladorNeon != null)
        {
            float factor = progresoShape / 100f;
            controladorNeon.intensidad = Mathf.Lerp(intensidadNeonDescargado, intensidadNeonCargado, factor);
        }
    }

    private void LateUpdate()
    {
        if (!contadorMiraCamara || contadorGigante == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Transform t = contadorGigante.transform;
        Vector3 direccion = t.position - cam.transform.position;
        direccion.y = 0f; // solo gira en horizontal: se lee bien desde cualquier lado
        if (direccion.sqrMagnitude > 0.0001f)
            t.rotation = Quaternion.LookRotation(direccion, Vector3.up);
    }

    private void EmitirBurst(ParticleSystem[] sistemas)
    {
        if (sistemas == null) return;
        foreach (ParticleSystem ps in sistemas)
        {
            if (ps != null) ps.Emit(1);
        }
    }

    // =====================================================
    // COMPRA / DESBLOQUEO
    // =====================================================

    private bool JugadorCerca()
    {
        Transform objetivo = jugador;
        if (objetivo == null && Camera.main != null) objetivo = Camera.main.transform;
        if (objetivo == null) return false;

        Vector3 punto = colliderMaquina != null
            ? colliderMaquina.ClosestPoint(objetivo.position)
            : transform.position;
        return Vector3.Distance(punto, objetivo.position) <= distanciaCompra;
    }

    private void ManejarCompra()
    {
        bool mostrarAviso = !desbloqueado && JugadorCerca();

        if (textoAvisoCompra != null)
        {
            if (textoAvisoCompra.gameObject.activeSelf != mostrarAviso)
                textoAvisoCompra.gameObject.SetActive(mostrarAviso);

            if (mostrarAviso)
            {
                int precio = PrecioDesbloqueo;
                int tengo = cartera != null ? cartera.Monedas : 0;
                textoAvisoCompra.text = "[" + teclaComprar + "] Comprar Fisionador (" + precio + " monedas)" +
                    (tengo < precio ? "\nTe faltan " + (precio - tengo) : "");
            }
        }

        if (mostrarAviso && Input.GetKeyDown(teclaComprar))
            ComprarDesbloqueo();
    }

    /// <summary>Paga el precio con la Cartera y activa la máquina. También sirve para un botón de tienda.</summary>
    public bool ComprarDesbloqueo()
    {
        if (desbloqueado) return false;
        if (cartera == null) cartera = FindFirstObjectByType<Cartera>();

        int precio = PrecioDesbloqueo;
        if (cartera == null || !cartera.GastarMonedas(precio))
        {
            Debug.Log("Fisionador: necesitas " + precio + " monedas.", this);
            if (!string.IsNullOrEmpty(sonidoRechazo)) AudioManager.Play(sonidoRechazo, transform.position);
            return false;
        }

        Desbloquear();
        EventosEconomia.NotificarCompra(EconomiaDemo.Ids.Fisionador, 1, precio);
        return true;
    }

    /// <summary>
    /// Guardado (F9): restaura compra, esferas dentro y misiles sin lanzar, sin cobrar,
    /// sin sonidos y sin lanzar OnCompraRealizada.
    /// </summary>
    public void RestaurarEstado(bool estaDesbloqueado, int esferas, bool estaCompletado, int misilesPendientes)
    {
        desbloqueado = estaDesbloqueado;
        completado = estaCompletado && !repetirTrasMisil;
        piedrasActuales = completado ? 0 : Mathf.Clamp(esferas, 0, Mathf.Max(0, EsferasNecesarias - 1));
        popContador = 0f;
        rechazoHasta = -999f;
        misilHasta = -999f;

        AplicarEstadoVisualDesbloqueo();
        ActualizarProgresoSecundario();
        if (objetoSecundario != null) objetoSecundario.localPosition = posicionObjetivoSecundario;
        ActualizarPantalla();

        foreach (GameObject misil in misilesSueltos)
            if (misil != null) Destroy(misil);
        misilesSueltos.Clear();

        GameObject prefab = PrefabMisil;
        if (prefab == null || puntoExpulsion == null) return;
        for (int i = 0; i < misilesPendientes; i++)
        {
            Vector3 posicion = puntoExpulsion.position + Vector3.up * (1.5f * i);
            misilesSueltos.Add(Instantiate(prefab, posicion, puntoExpulsion.rotation));
        }
    }

    /// <summary>Desbloquea sin cobrar (restaurar partida, trucos de testeo).</summary>
    public void Desbloquear()
    {
        if (desbloqueado) return;
        desbloqueado = true;

        AplicarEstadoVisualDesbloqueo();
        popContador = 0.25f;
        if (particulasDesbloqueo != null)
            foreach (ParticleSystem ps in particulasDesbloqueo)
                if (ps != null) ps.Play();
        if (!string.IsNullOrEmpty(sonidoDesbloqueo)) AudioManager.Play(sonidoDesbloqueo, transform.position);

        ActualizarPantalla();
    }

    private void AplicarEstadoVisualDesbloqueo()
    {
        if (objetosBloqueado != null)
            foreach (GameObject go in objetosBloqueado)
                if (go != null) go.SetActive(!desbloqueado);

        if (objetosDesbloqueado != null)
            foreach (GameObject go in objetosDesbloqueado)
                if (go != null) go.SetActive(desbloqueado);
    }

    // =====================================================
    // ENTRADA DE PIEDRAS
    // =====================================================

    private void OnTriggerEnter(Collider otro)
    {
        if (otro == null || !otro.CompareTag(tagPiedra)) return;

        Rigidbody rb = otro.attachedRigidbody;
        GameObject piedra = rb != null ? rb.gameObject : otro.gameObject;

        // Una piedra con varios colliders solo cuenta una vez.
        if (frameConsumo != Time.frameCount)
        {
            consumidasEsteFrame.Clear();
            frameConsumo = Time.frameCount;
        }
        if (consumidasEsteFrame.Contains(piedra)) return;

        if (!desbloqueado || completado || !EsEsferaPerfecta(piedra))
        {
            Rechazar(rb);
            return;
        }

        consumidasEsteFrame.Add(piedra);
        Destroy(piedra);
        piedrasActuales++;
        popContador = 0.18f;
        if (!string.IsNullOrEmpty(sonidoEsferaAceptada)) AudioManager.Play(sonidoEsferaAceptada, transform.position);

        if (piedrasActuales >= EsferasNecesarias)
            ExpulsarMisil();
        else
            ActualizarProgresoSecundario();

        ActualizarPantalla();
    }

    private static bool EsEsferaPerfecta(GameObject piedra)
    {
        DesgastePiedra desgaste = piedra.GetComponentInParent<DesgastePiedra>();
        if (desgaste == null) desgaste = piedra.GetComponentInChildren<DesgastePiedra>();
        return desgaste != null && desgaste.EsEsferaPerfecta;
    }

    private void Rechazar(Rigidbody rb)
    {
        rechazoHasta = Time.unscaledTime + 0.5f;
        if (rb != null && !rb.isKinematic)
        {
            Vector3 fuera = rb.position - transform.position;
            fuera.y = 0f;
            if (fuera.sqrMagnitude < 0.0001f) fuera = transform.forward;
            Vector3 velocidad = fuera.normalized * fuerzaRebote + Vector3.up * fuerzaReboteArriba;
            rb.linearVelocity = velocidad;
        }

        if (!string.IsNullOrEmpty(sonidoRechazo) && Time.time - ultimoSonidoRechazo >= intervaloSonidoRechazo)
        {
            ultimoSonidoRechazo = Time.time;
            AudioManager.Play(sonidoRechazo, transform.position);
        }
    }

    // =====================================================
    // MISIL
    // =====================================================

    private void ExpulsarMisil()
    {
        int esferasGastadas = EsferasNecesarias;
        GameObject prefab = PrefabMisil;

        if (prefab != null && puntoExpulsion != null)
        {
            GameObject misil = Instantiate(prefab, puntoExpulsion.position, puntoExpulsion.rotation);
            misilesSueltos.Add(misil);
            StartCoroutine(RutinaCrecimientoYExpulsion(misil));
        }
        else
        {
            Debug.LogWarning(name + ": no se puede expulsar el misil (falta prefab o punto de expulsión).", this);
        }

        if (!string.IsNullOrEmpty(sonidoMisil)) AudioManager.Play(sonidoMisil, transform.position);
        EventosEconomia.NotificarCompra(EconomiaDemo.Ids.MisilGrande, 1, esferasGastadas);

        piedrasActuales = 0;
        misilHasta = Time.unscaledTime + 4f;
        popContador = 0.25f;
        if (!repetirTrasMisil) completado = true;
        ActualizarProgresoSecundario();
    }

    private void ActualizarProgresoSecundario()
    {
        if (objetoSecundario == null) return;
        float progreso = Mathf.Clamp01(piedrasActuales / (float)EsferasNecesarias);
        posicionObjetivoSecundario = posicionBaseSecundario +
            ejeMovimiento.normalized * distanciaMovimientoPorNivel * progreso;
    }

    private void ActualizarPantalla()
    {
        int necesarias = EsferasNecesarias;
        string contador;
        string estado;

        if (!desbloqueado)
        {
            contador = "FISIONADOR\n" + PrecioDesbloqueo + " monedas";
            estado = "LOCKED";
        }
        else if (completado)
        {
            contador = "MAX";
            estado = "FINISHED";
        }
        else
        {
            contador = string.Format(formatoContador, piedrasActuales, necesarias);
            estado = piedrasActuales == 0 ? "NEED SPHERES" : "MORE SPHERES";
        }

        if (textoPantalla != null)
            textoPantalla.text = desbloqueado ? contador : "LOCKED";
        if (contadorGigante != null) contadorGigante.text = contador;
        if (textoEstado != null) textoEstado.text = estado;
    }

    // =====================================================
    // CONTADOR ESTILO CARTEL LED
    // Mismo lenguaje visual que el panel de desgaste del escáner (AimAssistVisual):
    // placa violeta, borde de neón con halo, esquinas de visor, dígitos de 7 segmentos
    // con los "8" apagados de fondo y barra de segmentos (uno por esfera).
    // =====================================================

    private void OnGUI()
    {
        if (!contadorEstiloLED || Event.current.type != EventType.Repaint) return;
        if (!desbloqueado && !mostrarPanelBloqueado) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 ancla = anclaContador != null ? anclaContador.position : transform.position + Vector3.up * 6f;
        Vector3 pantalla = cam.WorldToScreenPoint(ancla);
        if (pantalla.z <= 0f) return;
        if (distanciaMaximaContador > 0f && pantalla.z > distanciaMaximaContador) return;

        AimAssistVisual estilo = AimAssistVisual.Instancia;
        Color placa = estilo != null ? estilo.colorPlaca : new Color(0.035f, 0.025f, 0.09f, 0.88f);
        Color neon = estilo != null ? estilo.colorNeon : new Color(0.926f, 0.887f, 1f, 1f);
        Color bajo = estilo != null ? estilo.colorNeonBajo : new Color(0.926f, 0.075f, 0.102f, 1f);
        Color medio = estilo != null ? estilo.colorNeonMedio : new Color(0.579f, 0.387f, 0.943f, 1f);
        Color alto = estilo != null ? estilo.colorNeonAlto : new Color(0.914f, 0.616f, 0.992f, 1f);
        Color listo = estilo != null ? estilo.colorNeonListo : neon;
        Font fuente = fuenteContador != null ? fuenteContador : (estilo != null ? estilo.fuentePanel : null);

        if (estiloLED == null || estiloLED.font != fuente)
        {
            estiloLED = new GUIStyle { font = fuente, fontStyle = FontStyle.Normal, clipping = TextClipping.Overflow };
            estiloLED.normal.textColor = Color.white;
        }

        // Escala: resolución + un poco más pequeño cuanto más lejos (sin desaparecer).
        float lejania = Mathf.InverseLerp(8f, Mathf.Max(9f, distanciaEncogerContador), pantalla.z);
        float e = Screen.height / 1080f * escalaContador * Mathf.Lerp(1f, 0.7f, lejania);
        float k = e * (1f + popContador);

        float ancho = 230f * k, altoPanel = 104f * k;
        Vector2 punto = new Vector2(pantalla.x, Screen.height - pantalla.y);
        Rect r = new Rect(punto.x - ancho * 0.5f, punto.y - altoPanel - 14f * e, ancho, altoPanel);

        int necesarias = EsferasNecesarias;
        bool rechazando = Time.unscaledTime < rechazoHasta;
        bool avisoMisil = Time.unscaledTime < misilHasta;
        bool parpadeoOn = Mathf.Repeat(Time.unscaledTime * 1.6f, 1f) < 0.65f;

        string cabeceraIzq, cabeceraDer, cifras, fondoCifras, sufijo, textoEstado;
        float progreso;
        int segmentos;
        Color estado;

        if (!desbloqueado)
        {
            int precio = PrecioDesbloqueo;
            int tengo = cartera != null ? cartera.Monedas : 0;
            progreso = precio > 0 ? Mathf.Clamp01(tengo / (float)precio) : 1f;
            cabeceraIzq = "FISSION";
            cabeceraDer = "LOCKED";
            cifras = precio.ToString();
            fondoCifras = new string('8', cifras.Length);
            sufijo = "C";
            segmentos = 20;
            estado = tengo >= precio ? listo : Color.Lerp(bajo, medio, progreso);
            textoEstado = tengo >= precio ? "[" + teclaComprar + "] UNLOCK" : "COINS REQUIRED";
        }
        else if (completado)
        {
            progreso = 1f;
            cabeceraIzq = "FISSION";
            cabeceraDer = "MISSILE";
            cifras = necesarias + "/" + necesarias;
            fondoCifras = cifras.Replace('0', '8').Replace('1', '8').Replace('2', '8').Replace('3', '8').Replace('4', '8')
                .Replace('5', '8').Replace('6', '8').Replace('7', '8').Replace('9', '8');
            sufijo = "";
            segmentos = necesarias;
            estado = listo;
            textoEstado = "COMPLETE";
        }
        else
        {
            progreso = Mathf.Clamp01(piedrasActuales / (float)necesarias);
            cabeceraIzq = "FISSION";
            cabeceraDer = "MISSILE";
            string total = necesarias.ToString();
            cifras = piedrasActuales.ToString().PadLeft(total.Length) + "/" + total;
            fondoCifras = new string('8', total.Length) + "/" + new string('8', total.Length);
            sufijo = "";
            segmentos = necesarias;
            estado = progreso < 0.5f ? Color.Lerp(medio, alto, progreso * 2f) : Color.Lerp(alto, listo, (progreso - 0.5f) * 2f);
            textoEstado = avisoMisil ? "MISSILE READY" : "PERFECT SPHERES ONLY";
        }

        if (rechazando)
        {
            estado = bajo;
            textoEstado = "REJECTED";
        }

        float parpadeo = (rechazando || avisoMisil) ? (parpadeoOn ? 1f : 0.35f) : 1f;
        Color borde = rechazando ? bajo : neon;

        // Sombra, placa, líneas de barrido, borde de neón y esquinas de visor.
        AimAssistVisual.Rectangulo(new Rect(r.x + 4f * k, r.y + 6f * k, r.width, r.height), new Color(0f, 0f, 0f, 0.4f), 4f * k);
        AimAssistVisual.Rectangulo(r, placa, 4f * k);
        for (float ly = r.y + 3f * k; ly < r.yMax - 2f * k; ly += 3f * k)
            AimAssistVisual.Rectangulo(new Rect(r.x + 2f * k, ly, r.width - 4f * k, Mathf.Max(1f, 0.6f * k)), AimAssistVisual.ConAlfa(neon, 0.035f), 0f);
        AimAssistVisual.BordeNeon(r, AimAssistVisual.ConAlfa(borde, 0.75f), k, 4f * k);
        AimAssistVisual.EsquinasHUD(r, estado, k);

        float m = 14f * k;

        // Cabecera.
        TextoLED(new Rect(r.x + m, r.y + 8f * k, r.width - 2f * m, 16f * k), cabeceraIzq, 15f * k, TextAnchor.UpperLeft, AimAssistVisual.ConAlfa(neon, 0.85f), 0.5f);
        TextoLED(new Rect(r.x + m, r.y + 8f * k, r.width - 2f * m, 16f * k), cabeceraDer, 15f * k, TextAnchor.UpperRight, AimAssistVisual.ConAlfa(neon, 0.45f), 0f);

        // Dígitos de 7 segmentos con los "8" apagados detrás.
        float anchoSufijo = string.IsNullOrEmpty(sufijo) ? 0f : 26f * k;
        Rect zonaCifras = new Rect(r.x + m, r.y + 20f * k, r.width - 2f * m - anchoSufijo, 46f * k);
        TextoLED(zonaCifras, fondoCifras, 44f * k, TextAnchor.MiddleCenter, AimAssistVisual.ConAlfa(estado, 0.09f), 0f);
        TextoLED(zonaCifras, cifras, 44f * k, TextAnchor.MiddleCenter, AimAssistVisual.ConAlfa(estado, parpadeo), 1f);
        if (anchoSufijo > 0f)
            TextoLED(new Rect(zonaCifras.xMax, zonaCifras.y + 6f * k, anchoSufijo, 40f * k), sufijo, 22f * k, TextAnchor.MiddleLeft, AimAssistVisual.ConAlfa(estado, 0.9f), 0.6f);

        // Barra de segmentos: uno por esfera (o progreso de monedas si está bloqueado).
        Rect barra = new Rect(r.x + m, r.y + 70f * k, r.width - 2f * m, 8f * k);
        BarraLED(barra, progreso, segmentos, (segmentos > 10 ? 2f : 4f) * k, medio, alto, listo);

        // Estado.
        TextoLED(new Rect(r.x + m, barra.yMax + 5f * k, r.width - 2f * m, 16f * k), textoEstado, 14f * k, TextAnchor.UpperLeft, AimAssistVisual.ConAlfa(estado, parpadeo), 0.6f);

        // Línea que une el panel con la máquina, como las etiquetas de las piedras.
        AimAssistVisual.Rectangulo(new Rect(punto.x - 0.75f * e, r.yMax, 1.5f * e, 12f * e), AimAssistVisual.ConAlfa(estado, 0.7f), 0f);
    }

    private static void BarraLED(Rect barra, float progreso, int segmentos, float hueco, Color medio, Color alto, Color listo)
    {
        float anchoSegmento = (barra.width - hueco * (segmentos - 1)) / segmentos;
        float encendidos = progreso * segmentos;

        for (int i = 0; i < segmentos; i++)
        {
            Rect segmento = new Rect(barra.x + i * (anchoSegmento + hueco), barra.y, anchoSegmento, barra.height);
            float t = (i + 1f) / segmentos;
            Color color = t < 1f ? Color.Lerp(medio, alto, t) : listo;

            float encendido = Mathf.Clamp01(encendidos - i);
            AimAssistVisual.Rectangulo(segmento, AimAssistVisual.ConAlfa(color, Mathf.Lerp(0.1f, 1f, encendido)), 0.15f * barra.height);
            if (encendido > 0.5f)
                AimAssistVisual.Rectangulo(new Rect(segmento.x - 1f, segmento.y - 1f, segmento.width + 2f, segmento.height + 2f),
                    AimAssistVisual.ConAlfa(color, 0.18f * encendido), 1f);
        }
    }

    // Texto luminoso: copias tenues desplazadas (halo) y el texto nítido encima.
    private void TextoLED(Rect r, string texto, float tamano, TextAnchor anclaje, Color color, float halo)
    {
        estiloLED.fontSize = Mathf.Max(8, Mathf.RoundToInt(tamano));
        estiloLED.alignment = anclaje;

        Color guardado = GUI.color;
        if (halo > 0f)
        {
            float d = Mathf.Max(1f, tamano * 0.05f);
            GUI.color = AimAssistVisual.ConAlfa(color, color.a * 0.22f * halo);
            GUI.Label(new Rect(r.x - d, r.y, r.width, r.height), texto, estiloLED);
            GUI.Label(new Rect(r.x + d, r.y, r.width, r.height), texto, estiloLED);
            GUI.Label(new Rect(r.x, r.y - d, r.width, r.height), texto, estiloLED);
            GUI.Label(new Rect(r.x, r.y + d, r.width, r.height), texto, estiloLED);
        }
        GUI.color = color;
        GUI.Label(r, texto, estiloLED);
        GUI.color = guardado;
    }

    private IEnumerator RutinaCrecimientoYExpulsion(GameObject objetoExpulsado)
    {
        Transform objetivo = objetoExpulsado.transform;
        EscalaPiedra escala = EscalaPiedra.De(objetivo);
        Vector3 escalaFinal = escala.TomarControl(this);
        objetivo.localScale = Vector3.zero;

        // Fase 1: Crecimiento
        float t = 0f;
        while (t < 1f)
        {
            // Si otro script se lo ha llevado a medio crecer (p. ej. el jugador), deja de escalarlo.
            if (objetivo == null || !escala.Controla(this)) yield break;

            t += Time.deltaTime * velocidadCrecimientoExpulsado;
            objetivo.localScale = Vector3.Lerp(Vector3.zero, escalaFinal, t);
            yield return null;
        }

        if (objetivo != null)
        {
            objetivo.localScale = escalaFinal;
            escala.Soltar(this);

            // Fase 2: Físicas una vez ha crecido del todo
            if (aplicarFuerzaFisica)
            {
                Rigidbody rb = objetoExpulsado.GetComponent<Rigidbody>();
                if (rb == null) rb = objetoExpulsado.AddComponent<Rigidbody>();

                Vector3 direccionMundo = puntoExpulsion != null
                    ? puntoExpulsion.TransformDirection(direccionExpulsion.normalized)
                    : direccionExpulsion.normalized;

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
