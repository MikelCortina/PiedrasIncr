using System.Collections;
using UnityEngine;
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
    [Tooltip("Fuerza del temblor en grados para la nave")]
    public float intensidadVibracionNave = 0.8f;
    [Tooltip("Velocidad o frecuencia del temblor de la nave")]
    public float velocidadVibracionNave = 25f;

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

    [Header("Camaras")]
    public CinemachineCamera camaraExterior;
    public CinemachineCamera camaraInterior;

    [Tooltip("Opcional: camara Cinemachine normal del jugador")]
    public CinemachineCamera camaraJugador;

    public CinemachineBrain brainCamaraPrincipal;
    public Transform camaraPrincipal;

    [Header("Puntos de Corte y Eventos en el Spline (0 a 1)")]
    [Tooltip("Punto donde la camara exterior empieza a rotar para mirar a la nave")]
    [Range(0f, 1f)] public float puntoInicioSeguimientoExterior = 0.2f;
    [Tooltip("Velocidad con la que la camara exterior gira para centrar la nave")]
    public float velocidadGiroExterior = 3f;

    [Tooltip("0 = Inicio del spline, 1 = Final del spline")]
    [Range(0f, 1f)] public float puntoCorteCamaraInterior = 0.5f;
    [Tooltip("Punto donde empieza el fundido a negro y termina la intro")]
    [Range(0f, 1f)] public float puntoFinIntroduccion = 0.99f;

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

    [Tooltip("Scripts de movimiento, mirada e interaccion del jugador")]
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

    private float progresoPersonalizado = 0f;
    private Quaternion rotacionBaseSpline;
    private bool tieneRotacionBase;

    private void OnValidate()
    {
        if (!Application.isPlaying && movimientoNave != null && movimientoNave.Container != null)
        {
            movimientoNave.NormalizedTime = previsualizarPuntoSpline;

            Vector3 posicionPreview = movimientoNave.Container.EvaluatePosition(previsualizarPuntoSpline);
            movimientoNave.transform.position = posicionPreview;

            Vector3 tangente = movimientoNave.Container.EvaluateTangent(previsualizarPuntoSpline);
            Vector3 arriba = movimientoNave.Container.EvaluateUpVector(previsualizarPuntoSpline);
            if (tangente != Vector3.zero)
            {
                movimientoNave.transform.rotation = Quaternion.LookRotation(tangente, arriba);
            }
        }
    }

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
    }

    private void Start()
    {
        if (usarCurvaVelocidad && movimientoNave != null)
        {
            movimientoNave.Pause();
            progresoPersonalizado = 0f;
            movimientoNave.NormalizedTime = 0f;
        }
    }

    private void Update()
    {
        if (finalizando || movimientoNave == null)
            return;

        if (tieneRotacionBase)
        {
            movimientoNave.transform.rotation = rotacionBaseSpline;
        }

        float progreso;

        if (usarCurvaVelocidad)
        {
            float multiplicador = Mathf.Max(0.01f, curvaMultiplicadorVelocidad.Evaluate(progresoPersonalizado));
            float duracionSegura = Mathf.Max(0.1f, duracionBaseRecorrido);

            progresoPersonalizado += (Time.deltaTime / duracionSegura) * multiplicador;
            progresoPersonalizado = Mathf.Clamp01(progresoPersonalizado);

            movimientoNave.NormalizedTime = progresoPersonalizado;
            progreso = progresoPersonalizado;
        }
        else
        {
            progreso = movimientoNave.NormalizedTime;
        }

        previsualizarPuntoSpline = Mathf.Clamp01(progreso);

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

        if (progreso >= puntoFinIntroduccion)
        {
            finalizando = true;
            StartCoroutine(FinalizarIntroduccion());
            return;
        }

        if (cambioRealizado)
            MoverCamaraInterior();
    }

    private void LateUpdate()
    {
        if (finalizando)
            return;

        // 1. Vibración de la Nave en vuelo
        if (movimientoNave != null && vibrarNaveEnVuelo)
        {
            rotacionBaseSpline = movimientoNave.transform.rotation;
            tieneRotacionBase = true;

            float tNave = Time.time * velocidadVibracionNave;
            float ruidoX = (Mathf.PerlinNoise(tNave, 0f) - 0.5f) * 2f * intensidadVibracionNave;
            float ruidoY = (Mathf.PerlinNoise(0f, tNave) - 0.5f) * 2f * (intensidadVibracionNave * 0.5f);
            float ruidoZ = (Mathf.PerlinNoise(tNave, tNave) - 0.5f) * 2f * intensidadVibracionNave;

            movimientoNave.transform.rotation = rotacionBaseSpline * Quaternion.Euler(ruidoX, ruidoY, ruidoZ);
        }

        // 2. Vibración de la Cámara Interior (Solo actúa si ya estamos dentro de la cabina)
        if (cambioRealizado && pivoteCamaraCabina != null && vibrarCamaraInterior)
        {
            float tCam = Time.time * velocidadVibracionCamara;
            float camX = (Mathf.PerlinNoise(tCam, 50f) - 0.5f) * 2f * intensidadVibracionCamara;
            float camY = (Mathf.PerlinNoise(50f, tCam) - 0.5f) * 2f * intensidadVibracionCamara;

            // Se suma al cálculo del ratón que ya se aplicó en Update
            pivoteCamaraCabina.localRotation *= Quaternion.Euler(camX, camY, 0f);
        }
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