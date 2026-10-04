using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaquinaErosion : MonoBehaviour
{
    // =====================================================
    // REQUISITOS DE ENTRADA
    // =====================================================

    [Header("Requisitos de Entrada")]

    [Tooltip(
        "Porcentaje mínimo de desgaste que debe tener " +
        "la piedra para ser aceptada."
    )]
    [Range(0f, 100f)]
    public float porcentajeMinimoRequerido = 50f;


    // =====================================================
    // RECHAZO
    // =====================================================

    [Header("Punto y Fuerza de Rechazo (Conos Rojos)")]

    public Transform puntoRechazo;

    public Vector3 direccionRechazo =
        new Vector3(0f, 1f, -1f);

    public float fuerzaMinimaRechazo = 10f;

    public float fuerzaMaximaRechazo = 20f;

    [Range(0f, 90f)]
    public float anguloDispersionRechazo = 15f;


    // =====================================================
    // ENTRADA / SALIDA
    // =====================================================

    [Header("Punto y Fuerza de Salida (Conos Verdes)")]

    public Transform puntoEntrada;

    public Transform puntoSalida;


    [Tooltip(
        "Punto situado sobre el NavMesh al que caminarán " +
        "los Bots para entregar una piedra."
    )]
    public Transform puntoEntregaBot;


    public Vector3 direccionSalida =
        new Vector3(0f, 1f, 1f);

    public float fuerzaMinimaExpulsion = 10f;

    public float fuerzaMaximaExpulsion = 20f;

    [Range(0f, 90f)]
    public float anguloDispersionSalida = 15f;


    // =====================================================
    // PROCESAMIENTO
    // =====================================================

    [Header("Procesamiento Interno")]

    public float tiempoProcesamiento = 3f;

    public float velocidadDePulido = 0.05f;


    // =====================================================
    // DESBLOQUEO
    // =====================================================

    [Header("Desbloqueo Procesadora")]

    [SerializeField]
    private bool procesadoraDesbloqueada = false;


    public bool ProcesadoraDesbloqueada
        => procesadoraDesbloqueada;


    // =====================================================
    // MEJORAS
    // =====================================================

    [Header("Mejora - Velocidad de Procesado")]

    [SerializeField, Range(1, 3)]
    private int nivelProcesado = 1;


    public float tiempoProcesadoNivel1 = 3f;

    public float tiempoProcesadoNivel2 = 2f;

    public float tiempoProcesadoNivel3 = 1f;


    public int NivelProcesado
        => nivelProcesado;


    // =====================================================
    // EFECTOS
    // =====================================================

    [Header("Efectos Visuales (Partículas)")]

    public ParticleSystem[] particulasTrabajando;

    public ParticleSystem[] particulasExpulsion;


    // =====================================================
    // PIEDRAS EN PROCESO
    // =====================================================

    private Dictionary<Rigidbody, float>
        piedrasEnProceso =
        new Dictionary<Rigidbody, float>();


    private float[] emisionesOriginales;


    // =====================================================
    // VALIDACIÓN
    // =====================================================

    private void OnValidate()
    {
        nivelProcesado =
            Mathf.Clamp(
                nivelProcesado,
                1,
                3
            );


        AplicarNivelProcesado();
    }


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        AplicarNivelProcesado();


        if (particulasTrabajando == null)
        {
            emisionesOriginales =
                new float[0];

            return;
        }


        emisionesOriginales =
            new float[
                particulasTrabajando.Length
            ];


        for (int i = 0;
             i < particulasTrabajando.Length;
             i++)
        {
            if (particulasTrabajando[i] == null)
                continue;


            emisionesOriginales[i] =
                particulasTrabajando[i]
                    .emission
                    .rateOverTimeMultiplier;
        }
    }


    // =====================================================
    // CONSULTA PARA BOTS
    // =====================================================

    public bool PuedeProcesarPureza(
        float pureza)
    {
        if (!procesadoraDesbloqueada)
            return false;


        return pureza >=
               porcentajeMinimoRequerido;
    }


    public bool PuedeAceptarPiedra(
        Rigidbody rb)
    {
        if (!procesadoraDesbloqueada)
            return false;


        if (rb == null)
            return false;


        if (piedrasEnProceso.ContainsKey(
                rb))
        {
            return false;
        }


        DeformacionPiedra deformacion =
            ObtenerDeformacion(
                rb
            );


        if (deformacion == null)
            return false;


        float pureza =
            deformacion
                .ObtenerPorcentajeDesgasteHaciaEsfera();


        return PuedeProcesarPureza(
            pureza
        );
    }


    // =====================================================
    // RECIBIR PIEDRA DESDE BOT
    // =====================================================

    public bool RecibirPiedraBot(
        Rigidbody rb)
    {
        if (!PuedeAceptarPiedra(
                rb))
        {
            return false;
        }


        DeformacionPiedra deformacion =
            ObtenerDeformacion(
                rb
            );


        if (deformacion == null)
            return false;


        float porcentajeActual =
            deformacion
                .ObtenerPorcentajeDesgasteHaciaEsfera();


        // Mientras esté dentro de la máquina
        // ningún Bot debe poder seleccionarla.

        if (GestorPiedras.Instancia != null)
        {
            GestorPiedras.Instancia
                .DesregistrarPiedra(
                    rb
                );
        }


        rb.transform.SetParent(
            null,
            true
        );


        // El Bot desactiva este componente
        // mientras transporta la piedra.
        deformacion.enabled =
            true;


        StartCoroutine(
            RutinaProcesarPiedra(
                rb,
                deformacion,
                porcentajeActual
            )
        );


        return true;
    }


    // =====================================================
    // RECIBIR PIEDRA POR TRIGGER
    // =====================================================

    public void RecibirPiedra(
        Collider otro)
    {
        if (otro == null)
            return;


        if (!otro.CompareTag(
                "Piedra"))
        {
            return;
        }


        Rigidbody rb =
            otro.GetComponent<Rigidbody>();


        if (rb == null)
        {
            rb =
                otro.GetComponentInParent<Rigidbody>();
        }


        if (rb == null)
            return;


        DeformacionPiedra deformacion =
            ObtenerDeformacion(
                rb
            );


        if (deformacion == null)
            return;


        if (piedrasEnProceso.ContainsKey(
                rb))
        {
            return;
        }


        float porcentajeActual =
            deformacion
                .ObtenerPorcentajeDesgasteHaciaEsfera();


        if (porcentajeActual >=
            porcentajeMinimoRequerido)
        {
            if (GestorPiedras.Instancia != null)
            {
                GestorPiedras.Instancia
                    .DesregistrarPiedra(
                        rb
                    );
            }


            StartCoroutine(
                RutinaProcesarPiedra(
                    rb,
                    deformacion,
                    porcentajeActual
                )
            );
        }
        else
        {
            RechazarPiedra(
                rb
            );
        }
    }


    // =====================================================
    // OBTENER DEFORMACIÓN
    // =====================================================

    private DeformacionPiedra ObtenerDeformacion(
        Rigidbody rb)
    {
        if (rb == null)
            return null;


        DeformacionPiedra deformacion =
            rb.GetComponent<
                DeformacionPiedra
            >();


        if (deformacion == null)
        {
            deformacion =
                rb.GetComponentInChildren<
                    DeformacionPiedra
                >();
        }


        if (deformacion == null)
        {
            deformacion =
                rb.GetComponentInParent<
                    DeformacionPiedra
                >();
        }


        return deformacion;
    }


    // =====================================================
    // DIRECCIÓN CON DISPERSIÓN
    // =====================================================

    private Vector3 ObtenerDireccionConAngulo(
        Vector3 direccionBase,
        float anguloApertura)
    {
        if (anguloApertura <= 0f)
        {
            return direccionBase;
        }


        return Quaternion.AngleAxis(
                   Random.Range(
                       0f,
                       anguloApertura
                   ),
                   Random.onUnitSphere
               )
               *
               direccionBase;
    }


    // =====================================================
    // RECHAZAR
    // =====================================================

    private void RechazarPiedra(
        Rigidbody rb)
    {
        if (rb == null ||
            puntoRechazo == null)
        {
            return;
        }


        rb.position =
            puntoRechazo.position;


        if (!rb.isKinematic)
        {
            rb.linearVelocity =
                Vector3.zero;


            rb.angularVelocity =
                Vector3.zero;
        }


        rb.isKinematic =
            false;


        Vector3 direccionBase =
            puntoRechazo.TransformDirection(
                direccionRechazo.normalized
            );


        Vector3 direccionFinal =
            ObtenerDireccionConAngulo(
                direccionBase,
                anguloDispersionRechazo
            );


        float fuerzaAleatoria =
            Random.Range(
                fuerzaMinimaRechazo,
                fuerzaMaximaRechazo
            );


        rb.AddForce(
            direccionFinal *
            fuerzaAleatoria,
            ForceMode.Impulse
        );
    }


    // =====================================================
    // PARTÍCULAS
    // =====================================================

    private void ActualizarEmisionParticulas()
    {
        List<Rigidbody> clavesEliminar =
            new List<Rigidbody>();


        foreach (
            Rigidbody rb
            in piedrasEnProceso.Keys)
        {
            if (rb == null)
            {
                clavesEliminar.Add(
                    rb
                );
            }
        }


        foreach (
            Rigidbody rb
            in clavesEliminar)
        {
            piedrasEnProceso.Remove(
                rb
            );
        }


        if (piedrasEnProceso.Count == 0)
        {
            if (particulasTrabajando != null)
            {
                foreach (
                    ParticleSystem ps
                    in particulasTrabajando)
                {
                    if (ps != null)
                    {
                        ps.Stop();
                    }
                }
            }


            return;
        }


        float sumaPorcentajes =
            0f;


        foreach (
            float porcentaje
            in piedrasEnProceso.Values)
        {
            sumaPorcentajes +=
                porcentaje;
        }


        float promedioActual =
            sumaPorcentajes /
            piedrasEnProceso.Count;


        float factorEmision =
            Mathf.InverseLerp(
                100f,
                porcentajeMinimoRequerido,
                promedioActual
            );


        if (particulasTrabajando == null)
            return;


        for (int i = 0;
             i < particulasTrabajando.Length;
             i++)
        {
            ParticleSystem ps =
                particulasTrabajando[i];


            if (ps == null)
                continue;


            var emision =
                ps.emission;


            float emisionBase =
                0f;


            if (emisionesOriginales != null &&
                i < emisionesOriginales.Length)
            {
                emisionBase =
                    emisionesOriginales[i];
            }


            emision.rateOverTimeMultiplier =
                emisionBase *
                factorEmision;


            if (!ps.isPlaying &&
                factorEmision > 0f)
            {
                ps.Play();
            }
        }
    }


    // =====================================================
    // PROCESAR
    // =====================================================

    private IEnumerator RutinaProcesarPiedra(
        Rigidbody rb,
        DeformacionPiedra deformacion,
        float porcentajeInicial)
    {
        if (rb == null ||
            deformacion == null)
        {
            yield break;
        }


        // =================================================
        // REGISTRAR EN LA MÁQUINA
        // =================================================

        if (!piedrasEnProceso.ContainsKey(
                rb))
        {
            piedrasEnProceso.Add(
                rb,
                porcentajeInicial
            );
        }


        ActualizarEmisionParticulas();


        // =================================================
        // PREPARAR PIEDRA
        // =================================================

        rb.isKinematic =
            true;


        rb.linearVelocity =
            Vector3.zero;


        rb.angularVelocity =
            Vector3.zero;


        MeshRenderer[] renderizadores =
            rb.GetComponentsInChildren<
                MeshRenderer
            >();


        foreach (
            MeshRenderer mr
            in renderizadores)
        {
            if (mr != null)
            {
                mr.enabled =
                    false;
            }
        }


        Collider[] colisionadores =
            rb.GetComponentsInChildren<
                Collider
            >();


        foreach (
            Collider col
            in colisionadores)
        {
            if (col != null)
            {
                col.enabled =
                    false;
            }
        }


        // =================================================
        // PROCESAMIENTO
        // =================================================

        float tiempoTranscurrido =
            0f;


        while (tiempoTranscurrido <
               tiempoProcesamiento)
        {
            if (rb == null ||
                deformacion == null)
            {
                if (rb != null)
                {
                    piedrasEnProceso.Remove(
                        rb
                    );
                }


                ActualizarEmisionParticulas();


                yield break;
            }


            tiempoTranscurrido +=
                Time.deltaTime;


            float progreso =
                tiempoTranscurrido /
                Mathf.Max(
                    0.01f,
                    tiempoProcesamiento
                );


            // =============================================
            // MOVIMIENTO INTERNO
            // =============================================

            if (puntoEntrada != null &&
                puntoSalida != null)
            {
                rb.position =
                    Vector3.Lerp(
                        puntoEntrada.position,
                        puntoSalida.position,
                        progreso
                    );
            }


            rb.rotation =
                Quaternion.Euler(
                    Mathf.Lerp(
                        0f,
                        360f,
                        progreso * 5f
                    ),
                    Mathf.Lerp(
                        0f,
                        360f,
                        progreso * 3f
                    ),
                    0f
                );


            // =============================================
            // PULIDO
            // =============================================

            deformacion
                .PulirUniformemente(
                    velocidadDePulido *
                    Time.deltaTime
                );


            yield return null;
        }


        // =================================================
        // PULIDO FINAL
        // =================================================

        if (deformacion != null)
        {
            deformacion
                .PulirUniformemente(
                    10f
                );
        }


        if (rb == null)
        {
            ActualizarEmisionParticulas();

            yield break;
        }


        // =================================================
        // POSICIÓN FINAL
        // =================================================

        if (puntoSalida != null)
        {
            rb.position =
                puntoSalida.position;
        }


        // =================================================
        // VOLVER A ACTIVAR
        // =================================================

        foreach (
            MeshRenderer mr
            in renderizadores)
        {
            if (mr != null)
            {
                mr.enabled =
                    true;
            }
        }


        foreach (
            Collider col
            in colisionadores)
        {
            if (col != null)
            {
                col.enabled =
                    true;
            }
        }


        rb.isKinematic =
            false;


        rb.linearVelocity =
            Vector3.zero;


        rb.angularVelocity =
            Vector3.zero;


        // =================================================
        // EXPULSAR
        // =================================================

        if (puntoSalida != null)
        {
            Vector3 direccionBase =
                puntoSalida.TransformDirection(
                    direccionSalida.normalized
                );


            Vector3 direccionFinal =
                ObtenerDireccionConAngulo(
                    direccionBase,
                    anguloDispersionSalida
                );


            float fuerzaAleatoria =
                Random.Range(
                    fuerzaMinimaExpulsion,
                    fuerzaMaximaExpulsion
                );


            rb.AddForce(
                direccionFinal *
                fuerzaAleatoria,
                ForceMode.Impulse
            );
        }


        // =================================================
        // SACAR DEL REGISTRO INTERNO
        // =================================================

        piedrasEnProceso.Remove(
            rb
        );


        ActualizarEmisionParticulas();


        // =================================================
        // REACTIVAR DEFORMACIÓN
        // =================================================

        if (deformacion != null)
        {
            deformacion.enabled =
                true;


            deformacion.Despertar();
        }


        // =================================================
        // VOLVER A REGISTRAR PARA LOS BOTS
        // =================================================

        if (GestorPiedras.Instancia != null)
        {
            GestorPiedras.Instancia
                .RegistrarPiedra(
                    rb
                );
        }


        // =================================================
        // PARTÍCULAS DE SALIDA
        // =================================================

        if (particulasExpulsion != null)
        {
            foreach (
                ParticleSystem ps
                in particulasExpulsion)
            {
                if (ps != null)
                {
                    ps.Play();
                }
            }
        }
    }


    // =====================================================
    // DESBLOQUEAR
    // =====================================================

    public void DesbloquearProcesadora()
    {
        if (procesadoraDesbloqueada)
            return;


        procesadoraDesbloqueada =
            true;


        Debug.Log(
            "¡Procesadora desbloqueada!"
        );
    }

    // =====================================================
    // MEJORAR
    // =====================================================

    public bool MejorarProcesado()
    {
        if (nivelProcesado >= 3)
            return false;


        nivelProcesado++;


        AplicarNivelProcesado();


        Debug.Log(
            "Procesadora mejorada a nivel " +
            nivelProcesado +
            " | Tiempo: " +
            tiempoProcesamiento +
            " segundos"
        );


        return true;
    }


    // =====================================================
    // APLICAR NIVEL
    // =====================================================

    private void AplicarNivelProcesado()
    {
        switch (nivelProcesado)
        {
            case 1:

                tiempoProcesamiento =
                    tiempoProcesadoNivel1;

                break;


            case 2:

                tiempoProcesamiento =
                    tiempoProcesadoNivel2;

                break;


            case 3:

                tiempoProcesamiento =
                    tiempoProcesadoNivel3;

                break;
        }
    }


    // =====================================================
    // MAX
    // =====================================================

    public bool ProcesadoAlMaximo()
    {
        return nivelProcesado >= 3;
    }


    // =====================================================
    // GIZMOS
    // =====================================================

    private void OnDrawGizmos()
    {
        // =================================================
        // RECHAZO
        // =================================================

        if (puntoRechazo != null &&
            direccionRechazo !=
            Vector3.zero)
        {
            Vector3 dirRealRechazo =
                puntoRechazo.TransformDirection(
                    direccionRechazo.normalized
                );


            Gizmos.color =
                new Color(
                    1f,
                    0f,
                    0f,
                    0.4f
                );


            DibujarConoGizmo(
                puntoRechazo.position,
                dirRealRechazo,
                fuerzaMinimaRechazo,
                anguloDispersionRechazo
            );


            Gizmos.color =
                Color.red;


            DibujarConoGizmo(
                puntoRechazo.position,
                dirRealRechazo,
                fuerzaMaximaRechazo,
                anguloDispersionRechazo
            );
        }


        // =================================================
        // SALIDA
        // =================================================

        if (puntoSalida != null &&
            direccionSalida !=
            Vector3.zero)
        {
            Vector3 dirRealSalida =
                puntoSalida.TransformDirection(
                    direccionSalida.normalized
                );


            Gizmos.color =
                new Color(
                    0f,
                    1f,
                    0f,
                    0.4f
                );


            DibujarConoGizmo(
                puntoSalida.position,
                dirRealSalida,
                fuerzaMinimaExpulsion,
                anguloDispersionSalida
            );


            Gizmos.color =
                Color.green;


            DibujarConoGizmo(
                puntoSalida.position,
                dirRealSalida,
                fuerzaMaximaExpulsion,
                anguloDispersionSalida
            );
        }


        // =================================================
        // PUNTO BOT
        // =================================================

        if (puntoEntregaBot != null)
        {
            Gizmos.color =
                Color.cyan;


            Gizmos.DrawWireSphere(
                puntoEntregaBot.position,
                0.35f
            );
        }
    }


    // =====================================================
    // DIBUJAR CONO
    // =====================================================

    private void DibujarConoGizmo(
        Vector3 origen,
        Vector3 direccionCentral,
        float fuerza,
        float angulo)
    {
        float tamañoVisual =
            Mathf.Clamp(
                fuerza * 0.15f,
                0.5f,
                7f
            );


        Vector3 finCentral =
            origen +
            (
                direccionCentral *
                tamañoVisual
            );


        Gizmos.DrawLine(
            origen,
            finCentral
        );


        if (angulo <= 0f)
            return;


        Vector3 ejeX =
            Vector3.Cross(
                direccionCentral,
                Vector3.up
            );


        if (ejeX.magnitude < 0.01f)
        {
            ejeX =
                Vector3.right;
        }


        ejeX.Normalize();


        Vector3 ejeY =
            Vector3.Cross(
                direccionCentral,
                ejeX
            ).normalized;


        Vector3 p1 =
            origen +
            (
                Quaternion.AngleAxis(
                    angulo,
                    ejeX
                )
                *
                direccionCentral
                *
                tamañoVisual
            );


        Vector3 p2 =
            origen +
            (
                Quaternion.AngleAxis(
                    -angulo,
                    ejeX
                )
                *
                direccionCentral
                *
                tamañoVisual
            );


        Vector3 p3 =
            origen +
            (
                Quaternion.AngleAxis(
                    angulo,
                    ejeY
                )
                *
                direccionCentral
                *
                tamañoVisual
            );


        Vector3 p4 =
            origen +
            (
                Quaternion.AngleAxis(
                    -angulo,
                    ejeY
                )
                *
                direccionCentral
                *
                tamañoVisual
            );


        Gizmos.DrawLine(
            origen,
            p1
        );


        Gizmos.DrawLine(
            origen,
            p2
        );


        Gizmos.DrawLine(
            origen,
            p3
        );


        Gizmos.DrawLine(
            origen,
            p4
        );


        Gizmos.DrawLine(
            p1,
            p3
        );


        Gizmos.DrawLine(
            p3,
            p2
        );


        Gizmos.DrawLine(
            p2,
            p4
        );


        Gizmos.DrawLine(
            p4,
            p1
        );
    }
}