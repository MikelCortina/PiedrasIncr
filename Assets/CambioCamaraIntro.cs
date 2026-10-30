using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Splines;
using Unity.Cinemachine;

[DefaultExecutionOrder(-500)]
public class CambioCamaraIntro : MonoBehaviour
{
    [Header("Pruebas (Editor)")]
    [Tooltip("Activado: omite la cinemática, aparca la nave y coloca al jugador listo para jugar desde Play.")]
    public bool saltarIntroParaPruebas = false;

    [Header("Nave")]
    public SplineAnimate movimientoNave;
    public Transform puntoAparcamientoNave;

    [Header("Vibración de la Nave (Compatible con Spline)")]
    [Tooltip("Activa la vibración de la nave sumándola a la orientación del spline sin romperla")]
    public bool vibrarNaveEnVuelo = true;
    [Tooltip("Si está activo, la nave también vibra mientras espera a despegar (motores encendidos)")]
    public bool vibrarNaveAntesDeDespegar = true;
    [Tooltip("Fuerza del temblor en grados para la nave")]
    public float intensidadVibracionNave = 0.8f;
    [Tooltip("Velocidad o frecuencia del temblor de la nave")]
    public float velocidadVibracionNave = 25f;

    [Header("Alabeo en Curvas")]
    [Tooltip("Inclina la nave sobre su eje de alabeo hacia el lado al que gira el spline")]
    public bool alabearEnCurvas = true;
    [Tooltip("Eje local sobre el que se inclina la nave (Z = morro)")]
    public Vector3 ejeAlabeo = Vector3.forward;
    [Tooltip("Grados de inclinación por cada grado de giro que tiene la curva en la ventana de anticipación")]
    public float intensidadAlabeo = 1f;
    [Tooltip("Inclinación máxima en grados")]
    [Range(0f, 60f)] public float alabeoMaximo = 15f;
    [Tooltip("Cuánto spline (0 a 1) se mira antes y después de la nave para medir la curva. Más alto = anticipa antes y suaviza más")]
    [Range(0.001f, 0.1f)] public float anticipacionAlabeo = 0.02f;
    [Tooltip("Rapidez con la que la nave entra y sale de la inclinación")]
    public float suavidadAlabeo = 3f;
    [Tooltip("Actívalo si la nave se inclina hacia fuera de la curva en lugar de hacia dentro")]
    public bool invertirAlabeo = false;
    [Tooltip("La inclinación crece con la velocidad de la nave: al máximo de la curva de velocidad se inclina del todo y cerca de velocidad 0 casi nada")]
    public bool alabeoSegunVelocidad = true;

    [Header("Volante")]
    [Tooltip("Objeto del volante. Puede estar en la lista de agujas de 'Interior Nave': su giro se suma a la vibración sin pisarla")]
    public Transform volante;
    [Tooltip("Eje local sobre el que gira el volante")]
    public Vector3 ejeGiroVolante = Vector3.forward;
    [Tooltip("Grados que gira el volante por cada grado de giro de la curva en la ventana de anticipación (usa la misma ventana que el alabeo)")]
    public float intensidadVolante = 3f;
    [Tooltip("Giro máximo del volante hacia cada lado, en grados")]
    [Range(0f, 360f)] public float giroMaximoVolante = 90f;
    [Tooltip("Rapidez con la que el volante sigue a la curva y vuelve al centro")]
    public float suavidadVolante = 4f;
    [Tooltip("Actívalo si el volante gira hacia el lado contrario de la curva")]
    public bool invertirVolante = false;

    [Header("Interior de la Nave")]
    [Tooltip("Script del interior: la intro controla la vibración de sus agujas y apaga sus neones de alarma al bajar la palanca")]
    public InteriorNaveEspacial interiorNave;

    [Header("Reducción de Vibración por Velocidad")]
    [Tooltip("La vibración baja a medida que la nave frena, hasta su mínimo al final del recorrido")]
    public bool reducirVibracionConVelocidad = true;
    [Tooltip("Vibración mínima de la nave respecto a su valor de Inspector (0 = se apaga, 0.25 = queda un 25%)")]
    [Range(0f, 1f)] public float porcentajeMinimoVibracionNave = 0f;
    [Tooltip("Vibración mínima de la cámara interior respecto a su valor de Inspector (0 = se apaga, 0.25 = queda un 25%)")]
    [Range(0f, 1f)] public float porcentajeMinimoVibracionCamara = 0f;
    [Tooltip("Vibración mínima de las agujas del interior respecto a sus valores de Inspector (0 = quietas, 0.25 = queda un 25%)")]
    [Range(0f, 1f)] public float porcentajeMinimoVibracionAgujas = 0f;
    [Tooltip("Solo lectura: factor de velocidad actual (1 = valores del Inspector, 0 = mínimo configurado)")]
    [SerializeField, Range(0f, 1f)] private float factorVibracionActual = 1f;

    [Header("Control de Velocidad en el Spline")]
    [Tooltip("Activa esto para controlar las aceleraciones y frenadas con la curva sin saltos bruscos")]
    public bool usarCurvaVelocidad = true;
    [Tooltip("Tiempo base en segundos que tardaría en recorrer el spline a velocidad 1")]
    public float duracionBaseRecorrido = 10f;
    [Tooltip("Eje X (0 a 1) = Posición en el spline | Eje Y = Multiplicador de velocidad")]
    public AnimationCurve curvaMultiplicadorVelocidad = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("Previsualización en Editor (0 a 1)")]
    [Tooltip("Mueve este deslizador sin dar a Play para ver dónde está la nave en cada punto (0 a 1). No altera la cámara.")]
    [Range(0f, 1f)] public float previsualizarPuntoSpline = 0f;

    [Header("Imágenes UI de Inicio")]
    [Tooltip("Imágenes del Canvas que se desvanecen al empezar la intro")]
    public List<Image> imagenesIntro = new List<Image>();
    [Tooltip("Segundos de espera antes de empezar a desvanecer las imágenes")]
    public float retrasoInicioImagenes = 0f;
    [Tooltip("Duración en segundos del fundido de las imágenes")]
    public float duracionFundidoImagenes = 3f;
    [Tooltip("Opacidad final de las imágenes (0 = invisibles)")]
    [Range(0f, 1f)] public float alphaFinalImagenes = 0f;
    [Tooltip("Eje X = tiempo normalizado del fundido | Eje Y = progreso del fundido (0 = alpha original, 1 = alpha final)")]
    public AnimationCurve curvaFundidoImagenes = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Segundos que espera la nave, una vez terminado el fundido de las imágenes, antes de empezar a recorrer el spline (0 = justo al terminar)")]
    [Min(0f)] public float retrasoInicioNaveTrasFundido = 0f;
    [Tooltip("Desactiva los GameObjects de las imágenes al terminar el fundido")]
    public bool desactivarImagenesAlTerminar = true;

    [Header("Imágenes UI de la Transición")]
    [Tooltip("Imágenes del Canvas que se ven desde el inicio y se desvanecen cuando la nave llega al punto indicado del spline")]
    public List<Image> imagenesTransicion = new List<Image>();
    [Tooltip("Punto del spline (0 a 1) en el que empiezan a desvanecerse")]
    [Range(0f, 1f)] public float puntoFundidoTransicion = 0.4f;
    [Tooltip("Duración en segundos del fundido")]
    [Min(0f)] public float duracionFundidoTransicion = 2f;
    [Tooltip("Opacidad final de las imágenes (0 = invisibles)")]
    [Range(0f, 1f)] public float alphaFinalTransicion = 0f;
    [Tooltip("Eje X = tiempo normalizado del fundido | Eje Y = progreso del fundido (0 = alpha original, 1 = alpha final)")]
    public AnimationCurve curvaFundidoTransicion = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Desactiva los GameObjects de las imágenes al terminar el fundido")]
    public bool desactivarTransicionAlTerminar = true;

    [Header("Camaras")]
    public CinemachineCamera camaraExterior;
    public CinemachineCamera camaraInterior;

    [Tooltip("Opcional: camara Cinemachine normal del jugador")]
    public CinemachineCamera camaraJugador;

    public CinemachineBrain brainCamaraPrincipal;
    public Transform camaraPrincipal;

    [Header("Layers de la Cámara Exterior")]
    [Tooltip("Filtra qué layers se renderizan mientras se ve la cámara exterior del inicio. Se aplica al Culling Mask de la cámara principal (las CinemachineCamera no tienen uno propio) y se restaura al pasar a la cabina.")]
    public bool filtrarLayersCamaraExterior = false;
    [Tooltip("Layers que se ven con la cámara exterior del inicio")]
    public LayerMask layersVisiblesCamaraExterior = ~0;

    [Header("FOV del Jugador")]
    [Tooltip("Al terminar la intro, devuelve a la cámara principal el FOV del jugador. Sin esto se queda con el FOV de la última cámara Cinemachine (la de cabina).")]
    public bool restaurarFOVJugador = true;
    [Tooltip("FOV vertical que tendrá la cámara del jugador tras la intro")]
    [Range(1f, 179f)] public float fovJugador = 60f;

    [Header("Puntos de Corte y Eventos en el Spline (0 a 1)")]
    [Tooltip("Punto donde la camara exterior empieza a rotar para mirar a la nave")]
    [Range(0f, 1f)] public float puntoInicioSeguimientoExterior = 0.2f;
    [Tooltip("Velocidad con la que la camara exterior gira para centrar la nave")]
    public float velocidadGiroExterior = 3f;

    [Tooltip("0 = Inicio del spline, 1 = Final del spline")]
    [Range(0f, 1f)] public float puntoCorteCamaraInterior = 0.5f;
    [Tooltip("Punto donde la nave se detiene. Con palanca asignada, aquí empieza la espera; sin palanca, aquí termina la intro.")]
    [Range(0f, 1f)] public float puntoFinIntroduccion = 0.99f;

    [Header("Salida con Palanca")]
    [Tooltip("Palanca de la cabina que el jugador debe bajar para salir. Vacía = la intro termina sola como antes.")]
    public PalancaTirador palancaSalida;
    [Tooltip("Bloquea la palanca durante el vuelo para que no se pueda usar antes de tiempo")]
    public bool bloquearPalancaHastaFinal = true;
    [Tooltip("Si la manilla de la palanca también es una aguja vibrante de 'Interior Nave', deja de vibrar (volviendo suave a su reposo) antes de poder usarla")]
    public bool pausarVibracionManillaPalanca = true;

    [Header("Apagado de Luces Interiores")]
    [Tooltip("Los neones de alarma de 'Interior Nave' se apagan siempre. Aquí puedes añadir además Lights de Unity de la cabina (opcional).")]
    public List<Light> lucesInteriores = new List<Light>();
    [Tooltip("Al apagar la nave, las agujas de 'Interior Nave' también se detienen (las de rotación continua frenan hasta 0)")]
    public bool detenerAgujasAlApagar = true;
    [Tooltip("Segundos que tardan las luces (y las agujas) en llegar a 0")]
    public float duracionApagadoLuces = 1.5f;
    [Tooltip("Eje X = tiempo normalizado | Eje Y = progreso del apagado (0 = intensidad original, 1 = apagadas)")]
    public AnimationCurve curvaApagadoLuces = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Segundos a oscuras antes de empezar el fundido a negro y el cambio al jugador")]
    public float esperaTrasApagarLuces = 0.5f;
    [Tooltip("Sistema de partículas (ej: propulsores) que se detiene al bajar la palanca. Incluye a sus hijos.")]
    public ParticleSystem particulasApagarConNave;
    [Tooltip("Activado: las partículas desaparecen al instante. Desactivado: dejan de emitir y las que ya existen se desvanecen solas")]
    public bool limpiarParticulasAlApagar = false;

    [Header("Cara Frontal de la Nave")]
    [Tooltip("Pieza frontal que se abre al apagar la nave. El cambio de plano espera a que termine de abrirse, y al cambiar a la cámara del jugador vuelve a su pose inicial.")]
    public Transform caraFrontal;
    [Tooltip("Desplazamiento total, en los ejes locales de la pieza tal y como está al empezar (ej: 0, 0, 1 = un metro hacia su frente)")]
    public Vector3 desplazamientoCaraFrontal = new Vector3(0f, 0f, 1f);
    [Tooltip("Velocidad del desplazamiento en unidades por segundo (0 = instantáneo)")]
    public float velocidadMovimientoCaraFrontal = 1f;
    [Tooltip("Grados que rota sobre su eje X local (negativo = hacia el otro lado)")]
    public float gradosRotacionCaraFrontalX = 45f;
    [Tooltip("Velocidad de la rotación en grados por segundo (0 = instantáneo)")]
    public float velocidadRotacionCaraFrontal = 30f;
    [Tooltip("Segundos desde que se baja la palanca hasta que empieza a abrirse (0 = a la vez que se apagan las luces)")]
    public float retrasoAperturaCaraFrontal = 0f;

    [Header("Camara interior y Vibración")]
    public Transform pivoteCamaraCabina;
    public float sensibilidadRaton = 2f;
    public float limiteHorizontal = 45f;
    public float limiteVertical = 20f;
    [Tooltip("Activa el temblor de cámara dentro de la cabina")]
    public bool vibrarCamaraInterior = true;
    public float intensidadVibracionCamara = 0.15f;
    public float velocidadVibracionCamara = 30f;

    [Header("Jugador")]
    public Transform jugador;
    public Transform puntoSalidaJugador;

    [Tooltip("Scripts de movimiento, mirada e interaccion del jugador (la CamaraPrimeraPersona de la cámara principal se desactiva sola aunque no esté aquí)")]
    public MonoBehaviour[] controlesJugador;

    [Header("Pantalla negra")]
    public CanvasGroup pantallaNegraIntro;
    public float duracionPantallaNegra = 2f;
    public float duracionFundido = 0.3f;

    private bool cambioRealizado;
    private bool finalizando;
    private bool omitiendoIntroEstaSesion;

    private float rotacionHorizontal;
    private float rotacionVertical;
    private Quaternion rotacionInicialCabina;

    private Vector3 posicionInicialCamara;
    private Quaternion rotacionInicialCamara;

    private Vector3 posicionFijaCamaraExterior;
    private Quaternion rotacionFijaCamaraExterior;

    // Culling Mask original de la cámara principal mientras el filtro de la exterior está aplicado.
    private Camera camaraConLayersFiltradas;
    private int cullingMaskOriginal;

    private bool[] controlesOriginalmenteActivos;
    private CinemachineBlendDefinition transicionOriginal;

    // CamaraPrimeraPersona que la intro ha apagado por su cuenta (null si no hizo falta).
    private CamaraPrimeraPersona camaraFPSDesactivadaPorIntro;

    private float progresoPersonalizado = 0f;
    private Quaternion rotacionBaseSpline;
    private bool tieneRotacionBase;

    private bool naveIniciada;
    private float[] alphasOriginalesImagenes;
    private float[] alphasOriginalesTransicion;
    private bool fundidoTransicionIniciado;

    private float velocidadPicoNave;
    private float velocidadFinalNave;

    private bool esperandoPalanca;
    private bool palancaDesbloqueada;
    private bool palancaActivada;
    private Transform manillaPausada;

    private float alabeoActual;
    private float velocidadMaximaCurva = 1f;

    // Giro de la curva en grados (positivo = a la derecha) que comparten el alabeo y el volante.
    private float giroCurvaActual;
    private float giroVolanteActual;
    private Quaternion rotacionBaseVolante;

    private Vector3 posicionInicialCaraFrontal;
    private Quaternion rotacionInicialCaraFrontal;

    private void OnValidate()
    {
#if UNITY_EDITOR
        // OnValidate también salta al cargar la escena o recompilar, antes de que SplineAnimate esté listo.
        // Aplazamos la previsualización al siguiente tick del editor y no tocamos NormalizedTime.
        if (Application.isPlaying)
            return;

        UnityEditor.EditorApplication.delayCall -= PrevisualizarNaveEnEditor;
        UnityEditor.EditorApplication.delayCall += PrevisualizarNaveEnEditor;
#endif
    }

#if UNITY_EDITOR
    private void PrevisualizarNaveEnEditor()
    {
        // El objeto puede haberse destruido o haber entrado en Play antes de que llegue el delayCall.
        if (this == null || Application.isPlaying || movimientoNave == null)
            return;

        SplineContainer ruta = movimientoNave.Container;

        if (ruta == null || ruta.Splines.Count == 0 || ruta.Spline == null || ruta.Spline.Count < 2)
            return;

        float t = Mathf.Clamp01(previsualizarPuntoSpline);

        Vector3 posicionPreview = ruta.EvaluatePosition(t);
        Vector3 tangente = ruta.EvaluateTangent(t);
        Vector3 arriba = ruta.EvaluateUpVector(t);

        movimientoNave.transform.position = posicionPreview;

        if (tangente.sqrMagnitude > 0.000001f)
            movimientoNave.transform.rotation = Quaternion.LookRotation(tangente, arriba);
    }
#endif

    private void Awake()
    {
        if (camaraPrincipal == null && Camera.main != null)
            camaraPrincipal = Camera.main.transform;

        if (brainCamaraPrincipal == null && camaraPrincipal != null)
        {
            brainCamaraPrincipal = camaraPrincipal.GetComponent<CinemachineBrain>();
        }

        if (camaraPrincipal != null)
        {
            posicionInicialCamara = camaraPrincipal.localPosition;
            rotacionInicialCamara = camaraPrincipal.localRotation;
        }

        if (brainCamaraPrincipal != null)
        {
            brainCamaraPrincipal.enabled = true;
            transicionOriginal = brainCamaraPrincipal.DefaultBlend;
        }

        if (camaraExterior != null)
        {
            camaraExterior.Priority = 30;
            posicionFijaCamaraExterior = camaraExterior.transform.position;
            rotacionFijaCamaraExterior = camaraExterior.transform.rotation;

            camaraExterior.Follow = null;
            camaraExterior.LookAt = null;

            AplicarLayersCamaraExterior();
        }

        if (camaraInterior != null)
            camaraInterior.Priority = 10;

        if (camaraJugador != null)
            camaraJugador.Priority = 0;

        if (pivoteCamaraCabina != null)
            rotacionInicialCabina = pivoteCamaraCabina.localRotation;

        if (pantallaNegraIntro != null)
        {
            pantallaNegraIntro.alpha = 0f;
            pantallaNegraIntro.blocksRaycasts = false;
        }

        alphasOriginalesImagenes = GuardarAlphasOriginales(imagenesIntro);
        alphasOriginalesTransicion = GuardarAlphasOriginales(imagenesTransicion);

        if (controlesJugador != null)
        {
            controlesOriginalmenteActivos = new bool[controlesJugador.Length];

            for (int i = 0; i < controlesJugador.Length; i++)
            {
                MonoBehaviour control = controlesJugador[i];

                if (control == null || control == this)
                    continue;

                controlesOriginalmenteActivos[i] = control.enabled;
                control.enabled = false;
            }
        }

        // La CamaraPrimeraPersona mueve la cámara principal en Update. Aunque el Brain la recoloque en LateUpdate,
        // cualquier script que lea la cámara en Update (ej: el raycast de GestorRuletasFPS) la vería en la cabeza
        // del jugador. Si no se ha desactivado ya por la lista de controles, la apagamos aquí.
        if (camaraPrincipal != null)
        {
            CamaraPrimeraPersona camaraFPS = camaraPrincipal.GetComponent<CamaraPrimeraPersona>();

            if (camaraFPS != null && camaraFPS.enabled)
            {
                camaraFPS.enabled = false;
                camaraFPSDesactivadaPorIntro = camaraFPS;
            }
        }
    }

    private void Start()
    {
        if (movimientoNave != null)
        {
            // La nave espera quieta al inicio del spline hasta que termine el fundido
            // de las imágenes y pase retrasoInicioNaveTrasFundido.
            movimientoNave.Pause();
            progresoPersonalizado = 0f;
            movimientoNave.NormalizedTime = 0f;
        }

        // Velocidad que tendrá la nave al terminar la intro: ahí la vibración llega a su mínimo.
        velocidadFinalNave = Mathf.Max(0.01f, curvaMultiplicadorVelocidad.Evaluate(puntoFinIntroduccion));
        velocidadPicoNave = 0f;
        factorVibracionActual = 1f;
        AplicarVibracionAgujas();

        velocidadMaximaCurva = CalcularVelocidadMaximaCurva();

        if (volante != null)
            rotacionBaseVolante = volante.localRotation;

        if (caraFrontal != null)
        {
            posicionInicialCaraFrontal = caraFrontal.localPosition;
            rotacionInicialCaraFrontal = caraFrontal.localRotation;
        }

        // Modo de desarrollo: reutilizamos la misma finalización que la intro real,
        // pero sin esperar al spline, a la palanca ni a los fundidos.
        if (saltarIntroParaPruebas && Application.isEditor)
        {
            SaltarIntroduccionParaPruebas();
            return;
        }

        if (palancaSalida != null)
        {
            palancaSalida.AlLlegarAlTope.AddListener(AlBajarPalancaSalida);

            if (bloquearPalancaHastaFinal)
                palancaSalida.SetBloqueada(true);

            if (interiorNave == null)
                Debug.LogWarning("CambioCamaraIntro: falta asignar 'Interior Nave'; los neones de alarma no se apagarán al bajar la palanca.");
        }

        StartCoroutine(FundidoImagenesIntro());
    }

    private void SaltarIntroduccionParaPruebas()
    {
        // Evitar que Update/LateUpdate sigan animando la nave y las cámaras.
        finalizando = true;
        omitiendoIntroEstaSesion = true;
        naveIniciada = false;

        if (movimientoNave != null)
        {
            movimientoNave.Pause();
            // Posición de respaldo si no hay Punto Aparcamiento Nave.
            movimientoNave.NormalizedTime = Mathf.Clamp01(puntoFinIntroduccion);
        }

        // Sin intro no debe quedar ninguna imagen inicial tapando la partida.
        AplicarProgresoImagenes(imagenesIntro, alphasOriginalesImagenes,
                               alphaFinalImagenes, 1f);
        TerminarImagenes(imagenesIntro, true);

        AplicarProgresoImagenes(imagenesTransicion, alphasOriginalesTransicion,
                               alphaFinalTransicion, 1f);
        TerminarImagenes(imagenesTransicion, true);
        fundidoTransicionIniciado = true;

        if (pantallaNegraIntro != null)
        {
            pantallaNegraIntro.alpha = 0f;
            pantallaNegraIntro.blocksRaycasts = false;
        }

        // Estado de motores/cabina tras apagar la nave.
        ApagarParticulasNave();
        if (interiorNave != null)
        {
            interiorNave.multiplicadorLucesAlarma = 0f;
            interiorNave.multiplicadorVibracionAgujas = 0f;
            if (detenerAgujasAlApagar)
                interiorNave.multiplicadorEnergiaAgujas = 0f;
        }

        foreach (Light luz in lucesInteriores)
            if (luz != null) luz.intensity = 0f;

        // Reutiliza exactamente el acoplamiento de nave, el spawn del jugador
        // y la restauración de cámara y controles de la intro normal.
        StartCoroutine(FinalizarIntroduccion());
    }

    private void OnDestroy()
    {
        RestaurarLayersCamaraExterior();

        if (palancaSalida != null)
            palancaSalida.AlLlegarAlTope.RemoveListener(AlBajarPalancaSalida);
    }

    private void Update()
    {
        if (finalizando || movimientoNave == null)
            return;

        if (tieneRotacionBase)
        {
            movimientoNave.transform.rotation = rotacionBaseSpline;
        }

        if (!naveIniciada)
        {
            factorVibracionActual = 1f;
            AplicarVibracionAgujas();
            MantenerCamaraExteriorFija();
            return;
        }

        // Mientras esperamos la palanca la nave ya no avanza: solo seguimos mirando por la cabina.
        if (!esperandoPalanca)
        {
            float progreso = AvanzarNave();

            previsualizarPuntoSpline = Mathf.Clamp01(progreso);

            ActualizarFactorVibracion(progreso);

            if (!fundidoTransicionIniciado && progreso >= puntoFundidoTransicion)
            {
                fundidoTransicionIniciado = true;
                StartCoroutine(FundidoImagenesTransicion());
            }

            if (!cambioRealizado && camaraExterior != null)
            {
                camaraExterior.transform.position = posicionFijaCamaraExterior;

                if (progreso < puntoInicioSeguimientoExterior)
                {
                    camaraExterior.transform.rotation = rotacionFijaCamaraExterior;
                }
                else
                {
                    RotarCamaraExteriorHaciaNave();
                }
            }

            if (!cambioRealizado && progreso >= puntoCorteCamaraInterior)
            {
                ActivarCamaraInterior();
            }

            if (progreso >= puntoFinIntroduccion)
            {
                if (palancaSalida == null)
                {
                    // Comportamiento antiguo: sin palanca, la intro termina sola.
                    finalizando = true;
                    StartCoroutine(FinalizarIntroduccion());
                    return;
                }

                EmpezarEsperaPalanca();
            }
        }
        else if (!palancaDesbloqueada && ManillaEnReposo())
        {
            // La manilla ya dejó de vibrar: ahora sí puede usarse sin mezclar movimientos.
            palancaDesbloqueada = true;
            palancaSalida.SetBloqueada(false);
        }

        // Curva, alabeo y volante se calculan aquí (antes del Update de InteriorNaveEspacial,
        // que es quien aplica el giro del volante si vibra) para que no vayan un frame tarde.
        ActualizarAlabeo();
        ActualizarVolante();

        AplicarVibracionAgujas();

        if (cambioRealizado)
            MoverCamaraInterior();
    }

    private void LateUpdate()
    {
        if (finalizando)
            return;

        // 1. Rotación final de la nave: orientación del spline + alabeo en curvas + vibración
        bool puedeVibrar = naveIniciada || vibrarNaveAntesDeDespegar;
        bool vibraNave = vibrarNaveEnVuelo && puedeVibrar;

        if (movimientoNave != null && (vibraNave || alabearEnCurvas))
        {
            // Guardamos la rotación pura del spline; Update la restaura cada frame para que
            // el alabeo y la vibración no se acumulen.
            rotacionBaseSpline = movimientoNave.transform.rotation;
            tieneRotacionBase = true;

            Quaternion ruido = Quaternion.identity;

            if (vibraNave)
            {
                float intensidadNave = intensidadVibracionNave * FactorConMinimo(porcentajeMinimoVibracionNave);

                float tNave = Time.time * velocidadVibracionNave;
                float ruidoX = (Mathf.PerlinNoise(tNave, 0f) - 0.5f) * 2f * intensidadNave;
                float ruidoY = (Mathf.PerlinNoise(0f, tNave) - 0.5f) * 2f * (intensidadNave * 0.5f);
                float ruidoZ = (Mathf.PerlinNoise(tNave, tNave) - 0.5f) * 2f * intensidadNave;

                ruido = Quaternion.Euler(ruidoX, ruidoY, ruidoZ);
            }

            Quaternion alabeo = Quaternion.AngleAxis(alabeoActual, ejeAlabeo);

            movimientoNave.transform.rotation = rotacionBaseSpline * alabeo * ruido;
        }

        // 2. Vibración de la Cámara Interior (Solo actúa si ya estamos dentro de la cabina)
        if (cambioRealizado && pivoteCamaraCabina != null && vibrarCamaraInterior)
        {
            float intensidadCamara = intensidadVibracionCamara * FactorConMinimo(porcentajeMinimoVibracionCamara);

            float tCam = Time.time * velocidadVibracionCamara;
            float camX = (Mathf.PerlinNoise(tCam, 50f) - 0.5f) * 2f * intensidadCamara;
            float camY = (Mathf.PerlinNoise(50f, tCam) - 0.5f) * 2f * intensidadCamara;

            // Se suma al cálculo del ratón que ya se aplicó en Update
            pivoteCamaraCabina.localRotation *= Quaternion.Euler(camX, camY, 0f);
        }
    }

    // =====================================================
    // AVANCE DE LA NAVE
    // =====================================================

    private float AvanzarNave()
    {
        if (!usarCurvaVelocidad)
            return movimientoNave.NormalizedTime;

        float multiplicador = Mathf.Max(0.01f, curvaMultiplicadorVelocidad.Evaluate(progresoPersonalizado));
        float duracionSegura = Mathf.Max(0.1f, duracionBaseRecorrido);

        progresoPersonalizado += (Time.deltaTime / duracionSegura) * multiplicador;
        progresoPersonalizado = Mathf.Clamp01(progresoPersonalizado);

        movimientoNave.NormalizedTime = progresoPersonalizado;
        return progresoPersonalizado;
    }

    // Mide cuánto gira el spline alrededor de la nave (en horizontal) y calcula la inclinación,
    // suavizada para que entre y salga de las curvas sin tirones. Parada o sin despegar, vuelve a 0.
    private void ActualizarAlabeo()
    {
        float alabeoObjetivo = 0f;
        giroCurvaActual = 0f;

        bool enVuelo = naveIniciada && !esperandoPalanca;

        if (enVuelo && movimientoNave != null && movimientoNave.Container != null)
        {
            SplineContainer ruta = movimientoNave.Container;
            float t = Mathf.Clamp01(movimientoNave.NormalizedTime);

            Vector3 tangenteAntes = ruta.EvaluateTangent(Mathf.Clamp01(t - anticipacionAlabeo));
            Vector3 tangenteDespues = ruta.EvaluateTangent(Mathf.Clamp01(t + anticipacionAlabeo));

            tangenteAntes.y = 0f;
            tangenteDespues.y = 0f;

            if (tangenteAntes.sqrMagnitude > 0.0001f && tangenteDespues.sqrMagnitude > 0.0001f)
            {
                // Positivo = la curva gira a la derecha vista desde arriba.
                giroCurvaActual = Vector3.SignedAngle(tangenteAntes, tangenteDespues, Vector3.up);

                if (alabearEnCurvas)
                {
                    // Girar a la derecha baja el ala derecha, que es una rotación negativa sobre +Z.
                    alabeoObjetivo = Mathf.Clamp(-giroCurvaActual * intensidadAlabeo, -alabeoMaximo, alabeoMaximo);

                    if (invertirAlabeo)
                        alabeoObjetivo = -alabeoObjetivo;

                    // Escalamos después del límite: el máximo solo se alcanza a la velocidad punta.
                    alabeoObjetivo *= FactorVelocidadAlabeo(t);
                }
            }
        }

        float suavizado = 1f - Mathf.Exp(-Mathf.Max(0f, suavidadAlabeo) * Time.deltaTime);
        alabeoActual = Mathf.Lerp(alabeoActual, alabeoObjetivo, suavizado);
    }

    // Gira el volante según la curva (no depende de la velocidad: una curva cerrada pide mucho volante).
    // Si el volante es una aguja de InteriorNaveEspacial, le pasamos el giro para que lo sume a su vibración;
    // si no, lo giramos aquí directamente.
    private void ActualizarVolante()
    {
        if (volante == null)
            return;

        float giroObjetivo = Mathf.Clamp(-giroCurvaActual * intensidadVolante, -giroMaximoVolante, giroMaximoVolante);

        if (invertirVolante)
            giroObjetivo = -giroObjetivo;

        float suavizado = 1f - Mathf.Exp(-Mathf.Max(0f, suavidadVolante) * Time.deltaTime);
        giroVolanteActual = Mathf.Lerp(giroVolanteActual, giroObjetivo, suavizado);

        bool giradoPorInterior = interiorNave != null &&
                                 interiorNave.EstablecerGiroExtra(volante, giroVolanteActual, ejeGiroVolante);

        if (!giradoPorInterior)
            volante.localRotation = rotacionBaseVolante * Quaternion.AngleAxis(giroVolanteActual, ejeGiroVolante);
    }

    // 0 a 1 según la velocidad actual respecto a la máxima de la curva de velocidad.
    // Sin curva, SplineAnimate avanza a velocidad constante y el alabeo no se escala.
    private float FactorVelocidadAlabeo(float progreso)
    {
        if (!alabeoSegunVelocidad || !usarCurvaVelocidad)
            return 1f;

        float velocidadActual = Mathf.Max(0.01f, curvaMultiplicadorVelocidad.Evaluate(progreso));
        return Mathf.Clamp01(velocidadActual / velocidadMaximaCurva);
    }

    // Muestrea la curva para conocer su multiplicador más alto (la velocidad punta de la nave).
    private float CalcularVelocidadMaximaCurva()
    {
        const int muestras = 64;
        float maximo = 0.01f;

        for (int i = 0; i <= muestras; i++)
            maximo = Mathf.Max(maximo, curvaMultiplicadorVelocidad.Evaluate(i / (float)muestras));

        return maximo;
    }

    // =====================================================
    // SALIDA CON PALANCA
    // =====================================================

    private void EmpezarEsperaPalanca()
    {
        esperandoPalanca = true;

        // La nave se queda parada en el punto final con la vibración al mínimo.
        factorVibracionActual = 0f;

        if (!usarCurvaVelocidad)
            movimientoNave.Pause();

        // Si por configuración aún no estábamos en la cabina, entramos ahora para poder ver la palanca.
        if (!cambioRealizado)
            ActivarCamaraInterior();

        PausarVibracionManilla();

        // Sin manilla vibrante la palanca se desbloquea ya; si no, Update espera a que llegue al reposo.
        if (ManillaEnReposo())
        {
            palancaDesbloqueada = true;
            palancaSalida.SetBloqueada(false);
        }
    }

    // Si la manilla de la palanca está en la lista de agujas de InteriorNaveEspacial, la pausamos
    // para que deje de escribir su rotación y solo la mueva PalancaTirador.
    private void PausarVibracionManilla()
    {
        manillaPausada = null;

        if (!pausarVibracionManillaPalanca || interiorNave == null)
            return;

        if (palancaSalida.objetoTirador != null && interiorNave.PausarAguja(palancaSalida.objetoTirador, true))
        {
            manillaPausada = palancaSalida.objetoTirador;
        }
        else if (interiorNave.PausarAguja(palancaSalida.transform, true))
        {
            manillaPausada = palancaSalida.transform;
        }
    }

    private bool ManillaEnReposo()
    {
        return manillaPausada == null || interiorNave == null || interiorNave.AgujaEnReposo(manillaPausada);
    }

    private void AlBajarPalancaSalida()
    {
        // Ignoramos la palanca si aún no estamos esperando o si ya se ha usado.
        if (!esperandoPalanca || palancaActivada || finalizando)
            return;

        palancaActivada = true;
        palancaSalida.SetBloqueada(true);

        StartCoroutine(SecuenciaSalidaPalanca());
    }

    private IEnumerator SecuenciaSalidaPalanca()
    {
        ApagarParticulasNave();

        // La cara frontal se abre en paralelo al apagado de luces; el cambio de plano espera a ambos.
        Coroutine apertura = caraFrontal != null ? StartCoroutine(AbrirCaraFrontal()) : null;

        yield return ApagarLucesInteriores();

        if (apertura != null)
            yield return apertura;

        if (esperaTrasApagarLuces > 0f)
            yield return new WaitForSeconds(esperaTrasApagarLuces);

        finalizando = true;
        yield return FinalizarIntroduccion();
    }

    private IEnumerator AbrirCaraFrontal()
    {
        if (retrasoAperturaCaraFrontal > 0f)
            yield return new WaitForSeconds(retrasoAperturaCaraFrontal);

        // El desplazamiento se interpreta en los ejes locales de la pieza tal y como estaba al empezar,
        // así la rotación en X no tuerce el recorrido.
        Vector3 posicionDestino = posicionInicialCaraFrontal + rotacionInicialCaraFrontal * desplazamientoCaraFrontal;
        float anguloActual = 0f;

        while (true)
        {
            Vector3 posicion = velocidadMovimientoCaraFrontal > 0f
                ? Vector3.MoveTowards(caraFrontal.localPosition, posicionDestino, velocidadMovimientoCaraFrontal * Time.deltaTime)
                : posicionDestino;

            anguloActual = velocidadRotacionCaraFrontal > 0f
                ? Mathf.MoveTowards(anguloActual, gradosRotacionCaraFrontalX, velocidadRotacionCaraFrontal * Time.deltaTime)
                : gradosRotacionCaraFrontalX;

            caraFrontal.localPosition = posicion;
            caraFrontal.localRotation = rotacionInicialCaraFrontal * Quaternion.AngleAxis(anguloActual, Vector3.right);

            if (posicion == posicionDestino && Mathf.Approximately(anguloActual, gradosRotacionCaraFrontalX))
                yield break;

            yield return null;
        }
    }

    private void RestaurarCaraFrontal()
    {
        if (caraFrontal == null)
            return;

        caraFrontal.localPosition = posicionInicialCaraFrontal;
        caraFrontal.localRotation = rotacionInicialCaraFrontal;
    }

    private void ApagarParticulasNave()
    {
        if (particulasApagarConNave == null)
            return;

        particulasApagarConNave.Stop(
            true,
            limpiarParticulasAlApagar
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting
        );
    }

    private IEnumerator ApagarLucesInteriores()
    {
        // Los neones de alarma y las agujas los escribe InteriorNaveEspacial cada frame,
        // así que los apagamos bajando sus multiplicadores en lugar de tocarlos directamente.
        float multiplicadorAlarmaInicial = interiorNave != null ? interiorNave.multiplicadorLucesAlarma : 0f;
        float energiaAgujasInicial = interiorNave != null ? interiorNave.multiplicadorEnergiaAgujas : 0f;

        float[] intensidadesLuces = new float[lucesInteriores.Count];

        for (int i = 0; i < lucesInteriores.Count; i++)
            intensidadesLuces[i] = lucesInteriores[i] != null ? lucesInteriores[i].intensity : 0f;

        float duracion = Mathf.Max(0f, duracionApagadoLuces);
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            AplicarApagadoLuces(curvaApagadoLuces.Evaluate(Mathf.Clamp01(tiempo / duracion)), multiplicadorAlarmaInicial, energiaAgujasInicial, intensidadesLuces);
            yield return null;
        }

        AplicarApagadoLuces(1f, multiplicadorAlarmaInicial, energiaAgujasInicial, intensidadesLuces);
    }

    private void AplicarApagadoLuces(float progreso, float multiplicadorAlarmaInicial, float energiaAgujasInicial, float[] intensidadesLuces)
    {
        if (interiorNave != null)
        {
            interiorNave.multiplicadorLucesAlarma = Mathf.LerpUnclamped(multiplicadorAlarmaInicial, 0f, progreso);

            if (detenerAgujasAlApagar)
                interiorNave.multiplicadorEnergiaAgujas = Mathf.LerpUnclamped(energiaAgujasInicial, 0f, progreso);
        }

        for (int i = 0; i < lucesInteriores.Count; i++)
        {
            if (lucesInteriores[i] != null)
                lucesInteriores[i].intensity = Mathf.LerpUnclamped(intensidadesLuces[i], 0f, progreso);
        }
    }

    // =====================================================
    // REDUCCIÓN DE VIBRACIÓN POR VELOCIDAD
    // =====================================================

    private void ActualizarFactorVibracion(float progreso)
    {
        if (!reducirVibracionConVelocidad)
        {
            factorVibracionActual = 1f;
            return;
        }

        // Respaldo: baja linealmente con el avance hasta 0 en puntoFinIntroduccion.
        float puntoFinSeguro = Mathf.Max(0.0001f, puntoFinIntroduccion);
        float factorPorProgreso = 1f - Mathf.Clamp01(progreso / puntoFinSeguro);

        if (!usarCurvaVelocidad)
        {
            factorVibracionActual = factorPorProgreso;
            return;
        }

        float velocidadActual = Mathf.Max(0.01f, curvaMultiplicadorVelocidad.Evaluate(progreso));

        // Mientras la nave acelera, el pico sube con ella y el factor se mantiene en 1.
        velocidadPicoNave = Mathf.Max(velocidadPicoNave, velocidadActual);

        if (velocidadPicoNave - velocidadFinalNave > 0.0001f)
        {
            // Pico de velocidad = valores del Inspector | velocidad final = mínimo configurado.
            factorVibracionActual = Mathf.InverseLerp(velocidadFinalNave, velocidadPicoNave, velocidadActual);
        }
        else
        {
            // La curva no frena (o acelera hasta el final): usamos el avance.
            factorVibracionActual = factorPorProgreso;
        }
    }

    // Convierte el factor de velocidad (1 a 0) en un multiplicador que va de 1 al mínimo indicado.
    private float FactorConMinimo(float porcentajeMinimo)
    {
        if (!reducirVibracionConVelocidad)
            return 1f;

        return Mathf.Lerp(porcentajeMinimo, 1f, factorVibracionActual);
    }

    private void AplicarVibracionAgujas()
    {
        if (interiorNave == null)
            return;

        interiorNave.multiplicadorVibracionAgujas = FactorConMinimo(porcentajeMinimoVibracionAgujas);
    }

    // =====================================================
    // IMÁGENES UI DE INICIO
    // =====================================================

    private static float[] GuardarAlphasOriginales(List<Image> imagenes)
    {
        if (imagenes == null)
            return null;

        float[] alphas = new float[imagenes.Count];

        for (int i = 0; i < imagenes.Count; i++)
        {
            Image imagen = imagenes[i];

            if (imagen == null)
                continue;

            alphas[i] = imagen.color.a;
            imagen.gameObject.SetActive(true);
        }

        return alphas;
    }

    private IEnumerator FundidoImagenesIntro()
    {
        bool hayImagenes = imagenesIntro != null && imagenesIntro.Count > 0;

        if (hayImagenes && duracionFundidoImagenes > 0f)
        {
            if (retrasoInicioImagenes > 0f)
                yield return new WaitForSeconds(retrasoInicioImagenes);

            float tiempo = 0f;

            while (tiempo < duracionFundidoImagenes)
            {
                tiempo += Time.deltaTime;

                float t = Mathf.Clamp01(tiempo / duracionFundidoImagenes);
                AplicarProgresoImagenes(imagenesIntro, alphasOriginalesImagenes, alphaFinalImagenes, curvaFundidoImagenes.Evaluate(t));

                yield return null;
            }
        }

        AplicarProgresoImagenes(imagenesIntro, alphasOriginalesImagenes, alphaFinalImagenes, 1f);
        TerminarImagenes(imagenesIntro, desactivarImagenesAlTerminar);

        // Sin imágenes el fundido "termina" al instante, así que el retraso cuenta desde el inicio.
        if (retrasoInicioNaveTrasFundido > 0f)
            yield return new WaitForSeconds(retrasoInicioNaveTrasFundido);

        IniciarNave();
    }

    // Se lanza cuando la nave llega a puntoFundidoTransicion. No bloquea el vuelo.
    private IEnumerator FundidoImagenesTransicion()
    {
        if (imagenesTransicion == null || imagenesTransicion.Count == 0)
            yield break;

        float tiempo = 0f;

        while (tiempo < duracionFundidoTransicion)
        {
            tiempo += Time.deltaTime;

            float t = Mathf.Clamp01(tiempo / duracionFundidoTransicion);
            AplicarProgresoImagenes(imagenesTransicion, alphasOriginalesTransicion, alphaFinalTransicion, curvaFundidoTransicion.Evaluate(t));

            yield return null;
        }

        AplicarProgresoImagenes(imagenesTransicion, alphasOriginalesTransicion, alphaFinalTransicion, 1f);
        TerminarImagenes(imagenesTransicion, desactivarTransicionAlTerminar);
    }

    private static void AplicarProgresoImagenes(List<Image> imagenes, float[] alphasOriginales, float alphaFinal, float progreso)
    {
        if (imagenes == null || alphasOriginales == null)
            return;

        for (int i = 0; i < imagenes.Count && i < alphasOriginales.Length; i++)
        {
            Image imagen = imagenes[i];

            if (imagen == null)
                continue;

            Color color = imagen.color;
            color.a = Mathf.LerpUnclamped(alphasOriginales[i], alphaFinal, progreso);
            imagen.color = color;
        }
    }

    private static void TerminarImagenes(List<Image> imagenes, bool desactivar)
    {
        if (!desactivar || imagenes == null)
            return;

        foreach (Image imagen in imagenes)
        {
            if (imagen != null)
                imagen.gameObject.SetActive(false);
        }
    }

    private void IniciarNave()
    {
        if (naveIniciada)
            return;

        naveIniciada = true;

        if (movimientoNave == null)
            return;

        // Con curva de velocidad el avance lo controla Update.
        // Sin curva, dejamos que SplineAnimate avance por sí mismo.
        if (!usarCurvaVelocidad)
            movimientoNave.Play();
    }

    private void MantenerCamaraExteriorFija()
    {
        if (camaraExterior == null)
            return;

        camaraExterior.transform.SetPositionAndRotation(
            posicionFijaCamaraExterior,
            rotacionFijaCamaraExterior
        );
    }

    // =====================================================
    // CÁMARAS
    // =====================================================

    private void ActivarCamaraInterior()
    {
        cambioRealizado = true;

        if (brainCamaraPrincipal != null)
        {
            brainCamaraPrincipal.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
        }

        if (camaraInterior != null)
            camaraInterior.Priority = 40;

        RestaurarLayersCamaraExterior();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Las CinemachineCamera no tienen Culling Mask: el filtro se aplica a la Camera real del Brain.
    private void AplicarLayersCamaraExterior()
    {
        if (!filtrarLayersCamaraExterior || camaraPrincipal == null || camaraConLayersFiltradas != null)
            return;

        Camera camara = camaraPrincipal.GetComponent<Camera>();

        if (camara == null)
            return;

        camaraConLayersFiltradas = camara;
        cullingMaskOriginal = camara.cullingMask;
        camara.cullingMask = layersVisiblesCamaraExterior;
    }

    private void RestaurarLayersCamaraExterior()
    {
        if (camaraConLayersFiltradas == null)
            return;

        camaraConLayersFiltradas.cullingMask = cullingMaskOriginal;
        camaraConLayersFiltradas = null;
    }

    private void RotarCamaraExteriorHaciaNave()
    {
        Vector3 direccionHaciaNave = movimientoNave.transform.position - camaraExterior.transform.position;

        if (direccionHaciaNave.sqrMagnitude > 0.0001f)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccionHaciaNave);

            camaraExterior.transform.rotation = Quaternion.Slerp(
                camaraExterior.transform.rotation,
                rotacionObjetivo,
                velocidadGiroExterior * Time.deltaTime
            );
        }
    }

    private void MoverCamaraInterior()
    {
        if (pivoteCamaraCabina == null)
            return;

        rotacionHorizontal += Input.GetAxis("Mouse X") * sensibilidadRaton;
        rotacionVertical -= Input.GetAxis("Mouse Y") * sensibilidadRaton;

        rotacionHorizontal = Mathf.Clamp(rotacionHorizontal, -limiteHorizontal, limiteHorizontal);
        rotacionVertical = Mathf.Clamp(rotacionVertical, -limiteVertical, limiteVertical);

        pivoteCamaraCabina.localRotation =
            rotacionInicialCabina *
            Quaternion.Euler(rotacionVertical, rotacionHorizontal, 0f);
    }

    // El CinemachineBrain copia el lens de la cámara virtual activa en la Camera real.
    // Al desactivarlo, la Camera se queda con el FOV de la cabina, así que lo devolvemos aquí.
    private void RestaurarFOVJugador()
    {
        if (!restaurarFOVJugador || camaraPrincipal == null)
            return;

        Camera camara = camaraPrincipal.GetComponent<Camera>();

        if (camara != null)
            camara.fieldOfView = fovJugador;
    }

    // =====================================================
    // FINAL DE LA INTRO
    // =====================================================

    private IEnumerator FinalizarIntroduccion()
    {
        // Si la intro acaba antes de llegar al punto de la transición, esas imágenes no se quedan en pantalla.
        if (!fundidoTransicionIniciado)
        {
            fundidoTransicionIniciado = true;
            AplicarProgresoImagenes(imagenesTransicion, alphasOriginalesTransicion, alphaFinalTransicion, 1f);
            TerminarImagenes(imagenesTransicion, desactivarTransicionAlTerminar);
        }

        float tiempoFundido = Mathf.Max(0f, duracionFundido);

        if (!omitiendoIntroEstaSesion)
            yield return Fundido(0f, 1f, tiempoFundido);

        // Por si la intro termina sin haber pasado por la cabina.
        RestaurarLayersCamaraExterior();

        if (movimientoNave != null)
        {
            OrientarNaveSpline orientador = movimientoNave.GetComponent<OrientarNaveSpline>();

            if (orientador != null)
                orientador.enabled = false;

            movimientoNave.enabled = false;
            vibrarNaveEnVuelo = false;
            vibrarCamaraInterior = false;

            if (puntoAparcamientoNave != null)
            {
                movimientoNave.transform.SetPositionAndRotation(
                    puntoAparcamientoNave.position,
                    puntoAparcamientoNave.rotation
                );
            }
        }

        // Con la pantalla en negro, la cara frontal vuelve a como estaba antes de abrirse.
        RestaurarCaraFrontal();

        if (jugador != null && puntoSalidaJugador != null)
        {
            CharacterController controlador = jugador.GetComponent<CharacterController>();
            bool estabaActivo = controlador != null && controlador.enabled;

            if (controlador != null)
                controlador.enabled = false;

            jugador.SetPositionAndRotation(
                puntoSalidaJugador.position,
                puntoSalidaJugador.rotation
            );

            Rigidbody cuerpo = jugador.GetComponent<Rigidbody>();

            if (cuerpo != null)
            {
                cuerpo.position = puntoSalidaJugador.position;
                cuerpo.rotation = puntoSalidaJugador.rotation;

                if (!cuerpo.isKinematic)
                {
                    cuerpo.linearVelocity = Vector3.zero;
                    cuerpo.angularVelocity = Vector3.zero;
                }
            }

            Physics.SyncTransforms();

            if (controlador != null)
                controlador.enabled = estabaActivo;
        }

        if (brainCamaraPrincipal != null)
        {
            brainCamaraPrincipal.DefaultBlend = transicionOriginal;
            brainCamaraPrincipal.enabled = false;
        }

        RestaurarFOVJugador();

        if (camaraPrincipal != null)
        {
            CamaraPrimeraPersona camaraFPS = camaraPrincipal.GetComponent<CamaraPrimeraPersona>();

            if (camaraFPS != null)
            {
                camaraFPS.RestaurarTrasIntro(
                    posicionInicialCamara,
                    rotacionInicialCamara
                );
            }
            else
            {
                camaraPrincipal.localPosition = posicionInicialCamara;
                camaraPrincipal.localRotation = rotacionInicialCamara;
            }
        }

        if (!omitiendoIntroEstaSesion)
        {
            float espera = Mathf.Max(0f, duracionPantallaNegra - 2f * tiempoFundido);
            if (espera > 0f)
                yield return new WaitForSeconds(espera);

            yield return Fundido(1f, 0f, tiempoFundido);
        }

        if (controlesJugador != null && controlesOriginalmenteActivos != null)
        {
            for (int i = 0; i < controlesJugador.Length; i++)
            {
                if (controlesJugador[i] != null)
                    controlesJugador[i].enabled = controlesOriginalmenteActivos[i];
            }
        }

        // Devolvemos la cámara del jugador si la apagó la intro por su cuenta.
        if (camaraFPSDesactivadaPorIntro != null)
        {
            camaraFPSDesactivadaPorIntro.enabled = true;
            camaraFPSDesactivadaPorIntro = null;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private IEnumerator Fundido(float desde, float hasta, float duracion)
    {
        if (pantallaNegraIntro == null)
            yield break;

        if (duracion <= 0f)
        {
            pantallaNegraIntro.alpha = hasta;
            yield break;
        }

        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            pantallaNegraIntro.alpha = Mathf.Lerp(desde, hasta, Mathf.Clamp01(tiempo / duracion));
            yield return null;
        }

        pantallaNegraIntro.alpha = hasta;
    }
}