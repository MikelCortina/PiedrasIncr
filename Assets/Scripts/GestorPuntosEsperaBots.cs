using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GestorPuntosEsperaBots : MonoBehaviour
{
    [Header("Plazas del parking")]
    [Tooltip("Puntos donde pueden aparcar los Bots.")]
    public List<Transform> puntosEspera =
        new List<Transform>();


    [Header("Gizmos")]
    public float radioGizmo = 0.45f;

    [Tooltip("Distancia máxima para considerar que una plaza está sobre NavMesh.")]
    public float distanciaComprobacionNavMesh = 0.75f;

    public bool mostrarNombres = true;

    public bool dibujarLineas = false;


    // =====================================================
    // RESERVAS
    // =====================================================

    // Bot -> plaza
    private readonly Dictionary<BotRecolector, Transform>
        reservasPorBot =
        new Dictionary<BotRecolector, Transform>();


    // Plazas ya ocupadas/reservadas.
    //
    // Esto hace imposible que dos Bots reciban
    // la misma plaza.
    private readonly HashSet<Transform>
        plazasReservadas =
        new HashSet<Transform>();


    // =====================================================
    // RESERVAR PLAZA
    // =====================================================

    public Transform ReservarPunto(
        BotRecolector bot)
    {
        if (bot == null)
            return null;


        LimpiarReservasInvalidas();


        // =================================================
        // EL BOT YA TIENE PLAZA
        // =================================================

        if (reservasPorBot.TryGetValue(
                bot,
                out Transform plazaActual))
        {
            if (plazaActual != null)
            {
                return plazaActual;
            }


            reservasPorBot.Remove(
                bot
            );
        }


        // =================================================
        // BUSCAR PLAZA LIBRE
        // =================================================

        Transform mejorPlaza =
            null;


        float mejorDistancia =
            Mathf.Infinity;


        foreach (Transform plaza in puntosEspera)
        {
            if (plaza == null)
                continue;


            // Ya pertenece a otro Bot.
            if (plazasReservadas.Contains(
                    plaza))
            {
                continue;
            }


            // La plaza tiene que estar realmente
            // cerca de un NavMesh válido.
            if (!NavMesh.SamplePosition(
                    plaza.position,
                    out NavMeshHit hit,
                    distanciaComprobacionNavMesh,
                    NavMesh.AllAreas))
            {
                continue;
            }


            // Asignamos la plaza libre más cercana
            // a la posición actual del Bot.
            float distancia =
                (bot.transform.position -
                 hit.position).sqrMagnitude;


            if (distancia <
                mejorDistancia)
            {
                mejorDistancia =
                    distancia;


                mejorPlaza =
                    plaza;
            }
        }


        // =================================================
        // NO HAY PLAZAS LIBRES
        // =================================================

        if (mejorPlaza == null)
        {
            Debug.LogWarning(
                bot.name +
                ": no hay ninguna plaza libre " +
                "en el parking."
            );


            return null;
        }


        // =================================================
        // RESERVA ATÓMICA
        // =================================================
        //
        // Primero marcamos la plaza como ocupada.
        // Después la asociamos al Bot.
        //
        // El siguiente Bot ya no podrá cogerla.
        // =================================================

        plazasReservadas.Add(
            mejorPlaza
        );


        reservasPorBot.Add(
            bot,
            mejorPlaza
        );


        Debug.Log(
            bot.name +
            " ha reservado " +
            mejorPlaza.name
        );


        return mejorPlaza;
    }


    // =====================================================
    // LIBERAR PLAZA
    // =====================================================

    public void LiberarPunto(
        BotRecolector bot)
    {
        if (bot == null)
            return;


        if (!reservasPorBot.TryGetValue(
                bot,
                out Transform plaza))
        {
            return;
        }


        reservasPorBot.Remove(
            bot
        );


        if (plaza != null)
        {
            plazasReservadas.Remove(
                plaza
            );
        }
    }


    // =====================================================
    // CONSULTAS
    // =====================================================

    public bool BotTienePlaza(
        BotRecolector bot)
    {
        if (bot == null)
            return false;


        return reservasPorBot.ContainsKey(
            bot
        );
    }


    public Transform ObtenerPlazaDelBot(
        BotRecolector bot)
    {
        if (bot == null)
            return null;


        reservasPorBot.TryGetValue(
            bot,
            out Transform plaza
        );


        return plaza;
    }


    public bool PlazaEstaReservada(
        Transform plaza)
    {
        if (plaza == null)
            return false;


        return plazasReservadas.Contains(
            plaza
        );
    }


    // =====================================================
    // LIMPIEZA
    // =====================================================

    private void LimpiarReservasInvalidas()
    {
        List<BotRecolector> botsEliminar =
            new List<BotRecolector>();


        foreach (
            KeyValuePair<BotRecolector, Transform> reserva
            in reservasPorBot)
        {
            if (reserva.Key == null ||
                reserva.Value == null)
            {
                botsEliminar.Add(
                    reserva.Key
                );
            }
        }


        foreach (BotRecolector bot in botsEliminar)
        {
            if (bot != null &&
                reservasPorBot.TryGetValue(
                    bot,
                    out Transform plaza))
            {
                if (plaza != null)
                {
                    plazasReservadas.Remove(
                        plaza
                    );
                }
            }


            reservasPorBot.Remove(
                bot
            );
        }


        // Seguridad adicional:
        // reconstruimos el HashSet con las reservas válidas.
        plazasReservadas.Clear();


        foreach (
            KeyValuePair<BotRecolector, Transform> reserva
            in reservasPorBot)
        {
            if (reserva.Key != null &&
                reserva.Value != null)
            {
                plazasReservadas.Add(
                    reserva.Value
                );
            }
        }
    }


    // =====================================================
    // GIZMOS
    // =====================================================

    private void OnDrawGizmos()
    {
        if (puntosEspera == null)
            return;


        foreach (Transform plaza in puntosEspera)
        {
            if (plaza == null)
                continue;


            bool tieneNavMesh =
                NavMesh.SamplePosition(
                    plaza.position,
                    out NavMeshHit hit,
                    distanciaComprobacionNavMesh,
                    NavMesh.AllAreas
                );


            // =============================================
            // COLOR
            // =============================================

            if (!tieneNavMesh)
            {
                // Rojo = mal colocada.
                Gizmos.color =
                    Color.red;
            }
            else if (Application.isPlaying &&
                     plazasReservadas.Contains(plaza))
            {
                // Amarillo = reservada actualmente.
                Gizmos.color =
                    Color.yellow;
            }
            else
            {
                // Verde = libre y válida.
                Gizmos.color =
                    Color.green;
            }


            Gizmos.DrawWireSphere(
                plaza.position,
                radioGizmo
            );


            Gizmos.DrawLine(
                plaza.position -
                Vector3.up * 0.1f,
                plaza.position +
                Vector3.up * 0.8f
            );


            if (tieneNavMesh)
            {
                Gizmos.DrawSphere(
                    hit.position,
                    0.08f
                );


                Gizmos.DrawLine(
                    plaza.position,
                    hit.position
                );
            }


            if (dibujarLineas)
            {
                Gizmos.DrawLine(
                    transform.position,
                    plaza.position
                );
            }


#if UNITY_EDITOR

            if (mostrarNombres)
            {
                string estado;


                if (!tieneNavMesh)
                {
                    estado =
                        "  SIN NAVMESH";
                }
                else if (
                    Application.isPlaying &&
                    plazasReservadas.Contains(plaza))
                {
                    estado =
                        "  OCUPADA";
                }
                else
                {
                    estado =
                        "  LIBRE";
                }


                Handles.Label(
                    plaza.position +
                    Vector3.up * 1f,
                    plaza.name +
                    estado
                );
            }

#endif
        }
    }
}