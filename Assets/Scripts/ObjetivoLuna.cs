using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DebugInfoPiedra : MonoBehaviour
{
    [Header("Activación")]
    public bool debugActivo = true;

    [Tooltip("Distancia a la que aparece la información.")]
    public float distanciaMostrar = 4f;


    [Header("Texto")]
    public TMP_Text textoDebug;

    [Tooltip("Altura del texto respecto a la piedra.")]
    public float alturaTexto = 1.2f;


    [Header("Actualización")]
    public float intervaloActualizacion = 0.15f;


    [Header("Movimiento")]
    [Tooltip("Por debajo de esta velocidad consideramos que la piedra está quieta.")]
    public float velocidadConsideradaQuieta = 0.15f;


    private Rigidbody rb;

    private DeformacionPiedra deformacion;

    private Transform jugador;

    private Camera camaraPrincipal;

    private float temporizador;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        rb =
            GetComponent<Rigidbody>();


        deformacion =
            GetComponent<DeformacionPiedra>();


        if (deformacion == null)
        {
            deformacion =
                GetComponentInParent<DeformacionPiedra>();
        }


        GameObject objetoJugador =
            GameObject.FindGameObjectWithTag("Player");


        if (objetoJugador != null)
        {
            jugador =
                objetoJugador.transform;
        }


        camaraPrincipal =
            Camera.main;


        if (textoDebug != null)
        {
            textoDebug.gameObject.SetActive(false);
        }
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        if (!debugActivo)
        {
            OcultarTexto();
            return;
        }


        if (jugador == null)
            return;


        float distancia =
            Vector3.Distance(
                jugador.position,
                transform.position
            );


        // =============================================
        // DEMASIADO LEJOS
        // =============================================

        if (distancia > distanciaMostrar)
        {
            OcultarTexto();
            return;
        }


        // =============================================
        // MOSTRAR
        // =============================================

        if (textoDebug != null &&
            !textoDebug.gameObject.activeSelf)
        {
            textoDebug.gameObject.SetActive(true);
        }


        // =============================================
        // POSICIÓN SOBRE LA PIEDRA
        // =============================================

        if (textoDebug != null)
        {
            textoDebug.transform.position =
                transform.position +
                Vector3.up * alturaTexto;
        }


        // =============================================
        // MIRAR HACIA LA CÁMARA
        // =============================================

        ActualizarRotacionTexto();


        // =============================================
        // NO ACTUALIZAR TEXTO TODOS LOS FRAMES
        // =============================================

        temporizador -=
            Time.deltaTime;


        if (temporizador > 0f)
            return;


        temporizador =
            intervaloActualizacion;


        ActualizarInformacion(
            distancia
        );
    }


    // =====================================================
    // INFORMACIÓN
    // =====================================================

    private void ActualizarInformacion(
        float distancia)
    {
        if (textoDebug == null)
            return;


        float pureza =
            0f;


        if (deformacion != null)
        {
            pureza =
                deformacion
                    .ObtenerPorcentajeDesgasteHaciaEsfera();
        }


        float velocidad =
            0f;


        if (rb != null)
        {
            velocidad =
                rb.linearVelocity.magnitude;
        }


        string estadoMovimiento;


        if (rb != null &&
            rb.isKinematic)
        {
            estadoMovimiento =
                "KINEMATIC";
        }
        else if (velocidad <=
                 velocidadConsideradaQuieta)
        {
            estadoMovimiento =
                "QUIETA";
        }
        else
        {
            estadoMovimiento =
                "MOVIENDOSE";
        }


        textoDebug.text =
            "<b>PIEDRA</b>" +
            "\nPureza: " +
            pureza.ToString("0.0") +
            "%" +
            "\nVelocidad: " +
            velocidad.ToString("0.0") +
            "\nEstado: " +
            estadoMovimiento +
            "\nDistancia: " +
            distancia.ToString("0.0") +
            " m";
    }


    // =====================================================
    // BILLBOARD
    // =====================================================

    private void ActualizarRotacionTexto()
    {
        if (textoDebug == null)
            return;


        if (camaraPrincipal == null)
        {
            camaraPrincipal =
                Camera.main;
        }


        if (camaraPrincipal == null)
            return;


        Vector3 direccion =
            textoDebug.transform.position -
            camaraPrincipal.transform.position;


        if (direccion.sqrMagnitude <
            0.001f)
        {
            return;
        }


        textoDebug.transform.rotation =
            Quaternion.LookRotation(
                direccion
            );
    }


    // =====================================================
    // OCULTAR
    // =====================================================

    private void OcultarTexto()
    {
        if (textoDebug != null &&
            textoDebug.gameObject.activeSelf)
        {
            textoDebug.gameObject.SetActive(false);
        }
    }
}