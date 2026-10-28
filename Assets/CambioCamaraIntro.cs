using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Splines;
using Unity.Cinemachine;

[DefaultExecutionOrder(-500)]
public class CambioCamaraIntro : MonoBehaviour
{
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
    [Tooltip("Porcentaje del fundido (0 a 1) en el que la nave empieza a recorrer el spline")]
    [Range(0f, 1f)] public float porcentajeInicioNave = 0.5f;
    [Tooltip("Desactiva los GameObjects de las imágenes al terminar el fundido")]
    public bool desactivarImagenesAlTerminar = true;

    [Header("Camaras")]
    public CinemachineCamera camaraExterior;
    public CinemachineCamera camaraInterior;

    [Tooltip("Opcional: camara Cinemachine normal del jugador")]
    public CinemachineCamera camaraJugador;

    public CinemachineBrain brainCamaraPrincipal;
    public Transform camaraPrincipal;

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

    private float rotacionHorizontal;
    private float rotacionVertical;
    private Quaternion rotacionInicialCabina;

    private Vector3 posicionInicialCamara;
    private Quaternion rotacionInicialCamara;

    private Vector3 posicionFijaCamaraExterior;
    private Quaternion rotacionFijaCamaraExterior;

    private bool[] controlesOriginalmenteActivos;
    private CinemachineBlendDefinition transicionOriginal;

    // CamaraPrimeraPersona que la intro ha apagado por su cuenta (null si no hizo falta).
    private CamaraPrimeraPersona camaraFPSDesactivadaPorIntro;

    private float progresoPersonalizado = 0f;
    private Quaternion rotacionBaseSpline;
    private bool tieneRotacionBase;

    private bool naveIniciada;
    private float[] alphasOriginalesImagenes;

    private float velocidadPicoNave;
    private float velocidadFinalNave;

    private bool esperandoPalanca;
    private bool palancaDesbloqueada;
    private bool palancaActivada;
    private Transform manillaPausada;

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

        GuardarAlphasOriginalesImagenes();

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
            // La nave espera quieta al inicio del spline hasta que el fundido
            // de las imágenes alcance porcentajeInicioNave.
            movimientoNave.Pause();
            progresoPersonalizado = 0f;
            movimientoNave.NormalizedTime = 0f;
        }

        // Velocidad que tendrá la nave al terminar la intro: ahí la vibración llega a su mínimo.
        velocidadFinalNave = Mathf.Max(0.01f, curvaMultiplicadorVelocidad.Evaluate(puntoFinIntroduccion));
        velocidadPicoNave = 0f;
        factorVibracionActual = 1f;
        AplicarVibracionAgujas();

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

    private void OnDestroy()
    {
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

        AplicarVibracionAgujas();

        if (cambioRealizado)
            MoverCamaraInterior();
    }

    private void LateUpdate()
    {
        if (finalizando)
            return;

        // 1. Vibración de la Nave en vuelo (y opcionalmente antes de despegar)
        bool puedeVibrar = naveIniciada || vibrarNaveAntesDeDespegar;

        if (movimientoNave != null && vibrarNaveEnVuelo && puedeVibrar)
        {
            rotacionBaseSpline = movimientoNave.transform.rotation;
            tieneRotacionBase = true;

            float intensidadNave = intensidadVibracionNave * FactorConMinimo(porcentajeMinimoVibracionNave);

            float tNave = Time.time * velocidadVibracionNave;
            float ruidoX = (Mathf.PerlinNoise(tNave, 0f) - 0.5f) * 2f * intensidadNave;
            float ruidoY = (Mathf.PerlinNoise(0f, tNave) - 0.5f) * 2f * (intensidadNave * 0.5f);
            float ruidoZ = (Mathf.PerlinNoise(tNave, tNave) - 0.5f) * 2f * intensidadNave;

            movimientoNave.transform.rotation = rotacionBaseSpline * Quaternion.Euler(ruidoX, ruidoY, ruidoZ);
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
        yield return ApagarLucesInteriores();

        if (esperaTrasApagarLuces > 0f)
            yield return new WaitForSeconds(esperaTrasApagarLuces);

        finalizando = true;
        yield return FinalizarIntroduccion();
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

    private void GuardarAlphasOriginalesImagenes()
    {
        if (imagenesIntro == null)
            return;

        alphasOriginalesImagenes = new float[imagenesIntro.Count];

        for (int i = 0; i < imagenesIntro.Count; i++)
        {
            Image imagen = imagenesIntro[i];

            if (imagen == null)
                continue;

            alphasOriginalesImagenes[i] = imagen.color.a;
            imagen.gameObject.SetActive(true);
        }
    }

    private IEnumerator FundidoImagenesIntro()
    {
        bool hayImagenes = imagenesIntro != null && imagenesIntro.Count > 0;

        if (!hayImagenes || duracionFundidoImagenes <= 0f)
        {
            AplicarProgresoImagenes(1f);
            TerminarImagenes();
            IniciarNave();
            yield break;
        }

        if (retrasoInicioImagenes > 0f)
            yield return new WaitForSeconds(retrasoInicioImagenes);

        if (porcentajeInicioNave <= 0f)
            IniciarNave();

        float tiempo = 0f;

        while (tiempo < duracionFundidoImagenes)
        {
            tiempo += Time.deltaTime;

            float t = Mathf.Clamp01(tiempo / duracionFundidoImagenes);
            AplicarProgresoImagenes(curvaFundidoImagenes.Evaluate(t));

            if (!naveIniciada && t >= porcentajeInicioNave)
                IniciarNave();

            yield return null;
        }

        AplicarProgresoImagenes(1f);
        TerminarImagenes();
        IniciarNave();
    }

    private void AplicarProgresoImagenes(float progreso)
    {
        if (imagenesIntro == null || alphasOriginalesImagenes == null)
            return;

        for (int i = 0; i < imagenesIntro.Count; i++)
        {
            Image imagen = imagenesIntro[i];

            if (imagen == null)
                continue;

            Color color = imagen.color;
            color.a = Mathf.LerpUnclamped(alphasOriginalesImagenes[i], alphaFinalImagenes, progreso);
            imagen.color = color;
        }
    }

    private void TerminarImagenes()
    {
        if (!desactivarImagenesAlTerminar || imagenesIntro == null)
            return;

        foreach (Image imagen in imagenesIntro)
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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        float tiempoFundido = Mathf.Max(0f, duracionFundido);

        yield return Fundido(0f, 1f, tiempoFundido);

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

        float espera = Mathf.Max(0f, duracionPantallaNegra - 2f * tiempoFundido);
        yield return new WaitForSeconds(espera);

        yield return Fundido(1f, 0f, tiempoFundido);

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