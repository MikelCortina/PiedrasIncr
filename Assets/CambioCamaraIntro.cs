
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

    [Header("Camaras")]
    public CinemachineCamera camaraExterior;
    public CinemachineCamera camaraInterior;

    [Tooltip("Opcional: camara Cinemachine normal del jugador")]
    public CinemachineCamera camaraJugador;

    public CinemachineBrain brainCamaraPrincipal;
    public Transform camaraPrincipal;

    [Header("Camara interior")]
    public Transform pivoteCamaraCabina;
    public float sensibilidadRaton = 2f;
    public float limiteHorizontal = 45f;
    public float limiteVertical = 20f;

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

    private bool[] controlesOriginalmenteActivos;

    private void Awake()
    {
        if (camaraPrincipal == null && Camera.main != null)
            camaraPrincipal = Camera.main.transform;

        if (brainCamaraPrincipal == null &&
            camaraPrincipal != null)
        {
            brainCamaraPrincipal =
                camaraPrincipal.GetComponent<CinemachineBrain>();
        }

        if (camaraPrincipal != null)
        {
            posicionInicialCamara = camaraPrincipal.localPosition;
            rotacionInicialCamara = camaraPrincipal.localRotation;
        }

        // Activar Cinemachine solo para la introduccion
        if (brainCamaraPrincipal != null)
        {
            brainCamaraPrincipal.enabled = true;
        }

        if (camaraExterior != null)
            camaraExterior.Priority = 30;

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

        // Bloquear controles durante la introduccion.
        if (controlesJugador != null)
        {
            controlesOriginalmenteActivos =
                new bool[controlesJugador.Length];

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

    private void Update()
    {
        if (finalizando || movimientoNave == null)
            return;

        float progreso = movimientoNave.NormalizedTime;

        // Mitad del vuelo: pasar a la cabina.
        if (!cambioRealizado && progreso >= 0.5f)
        {
            if (camaraInterior != null)
                camaraInterior.Priority = 40;

            cambioRealizado = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (progreso >= 0.999f)
        {
            finalizando = true;
            StartCoroutine(FinalizarIntroduccion());
            return;
        }

        if (cambioRealizado)
            MoverCamaraInterior();
    }

    private void MoverCamaraInterior()
    {
        if (pivoteCamaraCabina == null)
            return;

        rotacionHorizontal +=
            Input.GetAxis("Mouse X") * sensibilidadRaton;

        rotacionVertical -=
            Input.GetAxis("Mouse Y") * sensibilidadRaton;

        rotacionHorizontal = Mathf.Clamp(
            rotacionHorizontal, -limiteHorizontal, limiteHorizontal);

        rotacionVertical = Mathf.Clamp(
            rotacionVertical, -limiteVertical, limiteVertical);

        pivoteCamaraCabina.localRotation =
            rotacionInicialCabina *
            Quaternion.Euler(rotacionVertical, rotacionHorizontal, 0f);
    }

    private IEnumerator FinalizarIntroduccion()
    {
        float tiempoFundido = Mathf.Max(0f, duracionFundido);

        // 1. Ocultar la escena.
        yield return Fundido(0f, 1f, tiempoFundido);

        // DETENER LA NAVE Y SU ORIENTACION
        if (movimientoNave != null)
        {
            OrientarNaveSpline orientador =
                movimientoNave.GetComponent<OrientarNaveSpline>();

            if (orientador != null)
                orientador.enabled = false;

            movimientoNave.enabled = false;

            // COLOCAR LA NAVE EN SU APARCAMIENTO DEFINITIVO
            if (puntoAparcamientoNave != null)
            {
                movimientoNave.transform.SetPositionAndRotation(
                    puntoAparcamientoNave.position,
                    puntoAparcamientoNave.rotation
                );

                Debug.Log("NAVE APARCADA CORRECTAMENTE");
            }
        }

        // 4. Colocar al jugador en su punto de salida.
        if (jugador != null && puntoSalidaJugador != null)
        {
            CharacterController controlador =
                jugador.GetComponent<CharacterController>();

            bool estabaActivo =
                controlador != null && controlador.enabled;

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

        // 5. RECUPERAR LA CAMARA ORIGINAL DEL PLAYER

        if (brainCamaraPrincipal != null)
            brainCamaraPrincipal.enabled = false;

        if (camaraPrincipal != null)
        {
            CamaraPrimeraPersona camaraFPS =
                camaraPrincipal.GetComponent<CamaraPrimeraPersona>();

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

        // 6. Mantener la pantalla negra hasta completar 2 segundos.
        float espera = Mathf.Max(
            0f, duracionPantallaNegra - 2f * tiempoFundido);

        yield return new WaitForSeconds(espera);

        // 7. Volver a mostrar el juego.
        yield return Fundido(1f, 0f, tiempoFundido);

        // 8. Recuperar los controles originales.
        if (controlesJugador != null &&
            controlesOriginalmenteActivos != null)
        {
            for (int i = 0; i < controlesJugador.Length; i++)
            {
                if (controlesJugador[i] != null)
                    controlesJugador[i].enabled =
                        controlesOriginalmenteActivos[i];
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log("Introduccion terminada. Jugador y nave preparados.");
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

            pantallaNegraIntro.alpha = Mathf.Lerp(
                desde, hasta, Mathf.Clamp01(tiempo / duracion));

            yield return null;
        }

        pantallaNegraIntro.alpha = hasta;
    }
}
