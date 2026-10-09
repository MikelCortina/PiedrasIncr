using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ConexionEscalada : MonoBehaviour
{
    // =====================================================
    // REGISTRO GLOBAL
    // =====================================================

    private static readonly List<ConexionEscalada>
        conexionesActivas =
            new List<ConexionEscalada>();


    public static IReadOnlyList<ConexionEscalada>
        ConexionesActivas
    {
        get { return conexionesActivas; }
    }


    // =====================================================
    // PUNTOS
    // =====================================================

    [Header("Puntos NavMesh")]

    [Tooltip(
        "Punto sobre el NavMesh inferior, justo delante de la pared."
    )]
    public Transform puntoInferiorNavMesh;


    [Tooltip(
        "Punto sobre el NavMesh superior, un poco hacia dentro de la plataforma."
    )]
    public Transform puntoSuperiorNavMesh;


    [Header("Transición superior")]

    [Tooltip(
        "Posición del ROOT del Bot cuando está vertical en la pared, " +
        "justo antes de pasar al suelo superior. Necesario para poder " +
        "bajar incluso si el Bot nunca ha subido antes por esta pared."
    )]
    public Transform puntoParedSuperior;


    // =====================================================
    // NORMAL
    // =====================================================

    [Header("Orientación de la pared")]

    [Tooltip(
        "Opcional. Su eje azul Z debe apuntar HACIA FUERA de la pared, " +
        "hacia el nivel inferior. Si queda vacío se calcula automáticamente."
    )]
    public Transform referenciaNormalPared;


    // =====================================================
    // AJUSTES
    // =====================================================

    [Header("Ajustes")]

    public bool bidireccional =
        true;


    [Min(0.05f)]
    public float radioMuestreoNavMesh =
        1.25f;


    [Header("Debug")]

    public bool mostrarGizmos =
        true;


    // =====================================================
    // PROPIEDADES
    // =====================================================

    public bool TieneDatosDescenso
    {
        get
        {
            return
                puntoInferiorNavMesh != null &&
                puntoSuperiorNavMesh != null &&
                puntoParedSuperior != null;
        }
    }


    public Vector3 NormalPared
    {
        get
        {
            if (referenciaNormalPared != null)
            {
                Vector3 normal =
                    referenciaNormalPared.forward;

                normal.y =
                    0f;


                if (normal.sqrMagnitude >
                    0.0001f)
                {
                    return normal.normalized;
                }
            }


            if (puntoParedSuperior != null &&
                puntoInferiorNavMesh != null)
            {
                Vector3 normal =
                    puntoInferiorNavMesh.position -
                    puntoParedSuperior.position;

                normal.y =
                    0f;


                if (normal.sqrMagnitude >
                    0.0001f)
                {
                    return normal.normalized;
                }
            }


            Vector3 alternativa =
                transform.forward;

            alternativa.y =
                0f;


            if (alternativa.sqrMagnitude <
                0.0001f)
            {
                alternativa =
                    Vector3.forward;
            }


            return alternativa.normalized;
        }
    }


    // =====================================================
    // REGISTRO
    // =====================================================

    private void OnEnable()
    {
        if (!conexionesActivas.Contains(
                this))
        {
            conexionesActivas.Add(
                this
            );
        }
    }


    private void OnDisable()
    {
        conexionesActivas.Remove(
            this
        );
    }


    private void OnDestroy()
    {
        conexionesActivas.Remove(
            this
        );
    }


    // =====================================================
    // AJUSTAR PUNTOS AL NAVMESH
    // =====================================================

    public bool IntentarObtenerPuntosNavMesh(
        int areaMask,
        out NavMeshHit inferior,
        out NavMeshHit superior)
    {
        inferior =
            new NavMeshHit();

        superior =
            new NavMeshHit();


        if (puntoInferiorNavMesh == null ||
            puntoSuperiorNavMesh == null)
        {
            return false;
        }


        int mascara =
            areaMask != 0
            ? areaMask
            : NavMesh.AllAreas;


        float radio =
            Mathf.Max(
                0.05f,
                radioMuestreoNavMesh
            );


        bool encontroInferior =
            NavMesh.SamplePosition(
                puntoInferiorNavMesh.position,
                out inferior,
                radio,
                mascara
            );


        bool encontroSuperior =
            NavMesh.SamplePosition(
                puntoSuperiorNavMesh.position,
                out superior,
                radio,
                mascara
            );


        return
            encontroInferior &&
            encontroSuperior;
    }


    // =====================================================
    // GIZMOS
    // =====================================================

    private void OnDrawGizmos()
    {
        if (!mostrarGizmos)
            return;


        if (puntoInferiorNavMesh != null)
        {
            Gizmos.color =
                Color.green;

            Gizmos.DrawWireSphere(
                puntoInferiorNavMesh.position,
                0.18f
            );
        }


        if (puntoSuperiorNavMesh != null)
        {
            Gizmos.color =
                Color.cyan;

            Gizmos.DrawWireSphere(
                puntoSuperiorNavMesh.position,
                0.18f
            );
        }


        if (puntoParedSuperior != null)
        {
            Gizmos.color =
                Color.yellow;

            Gizmos.DrawWireSphere(
                puntoParedSuperior.position,
                0.16f
            );
        }


        if (puntoInferiorNavMesh != null &&
            puntoSuperiorNavMesh != null)
        {
            Gizmos.color =
                Color.magenta;

            Gizmos.DrawLine(
                puntoInferiorNavMesh.position,
                puntoSuperiorNavMesh.position
            );
        }


        Vector3 origen =
            puntoParedSuperior != null
            ? puntoParedSuperior.position
            : transform.position;


        Gizmos.color =
            Color.blue;

        Gizmos.DrawRay(
            origen,
            NormalPared *
            0.8f
        );
    }
}
