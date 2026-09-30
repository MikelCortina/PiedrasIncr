using System.Collections.Generic;
using UnityEngine;

public class GestorPuntosEntregaBots : MonoBehaviour
{
    [Header("Puntos alrededor del agujero")]
    [Tooltip("Añade aquí los puntos donde pueden colocarse los Bots para entregar piedras.")]
    public List<Transform> puntosEntrega = new List<Transform>();


    // Cada Bot puede tener reservado un único punto.
    private readonly Dictionary<BotRecolector, Transform> reservas =
        new Dictionary<BotRecolector, Transform>();


    // =====================================================
    // RESERVAR PUNTO
    // =====================================================

    public Transform ReservarPunto(BotRecolector bot)
    {
        if (bot == null)
            return null;


        LimpiarReservasInvalidas();


        // Si este Bot ya tiene un punto reservado,
        // seguimos utilizando el mismo.
        if (reservas.TryGetValue(bot, out Transform puntoExistente))
        {
            if (puntoExistente != null)
                return puntoExistente;

            reservas.Remove(bot);
        }


        Transform mejorPunto = null;

        float mejorDistancia =
            Mathf.Infinity;


        foreach (Transform punto in puntosEntrega)
        {
            if (punto == null)
                continue;


            if (EstaPuntoReservado(punto))
                continue;


            float distancia =
                (bot.transform.position -
                 punto.position).sqrMagnitude;


            if (distancia < mejorDistancia)
            {
                mejorDistancia =
                    distancia;

                mejorPunto =
                    punto;
            }
        }


        if (mejorPunto != null)
        {
            reservas[bot] =
                mejorPunto;
        }


        return mejorPunto;
    }


    // =====================================================
    // LIBERAR PUNTO
    // =====================================================

    public void LiberarPunto(BotRecolector bot)
    {
        if (bot == null)
            return;


        reservas.Remove(bot);
    }


    // =====================================================
    // COMPROBAR SI ESTÁ OCUPADO
    // =====================================================

    private bool EstaPuntoReservado(Transform punto)
    {
        foreach (KeyValuePair<BotRecolector, Transform> reserva in reservas)
        {
            if (reserva.Value == punto)
                return true;
        }


        return false;
    }


    // =====================================================
    // LIMPIEZA
    // =====================================================

    private void LimpiarReservasInvalidas()
    {
        List<BotRecolector> botsInvalidos =
            new List<BotRecolector>();


        foreach (KeyValuePair<BotRecolector, Transform> reserva in reservas)
        {
            if (reserva.Key == null ||
                reserva.Value == null)
            {
                botsInvalidos.Add(
                    reserva.Key
                );
            }
        }


        foreach (BotRecolector bot in botsInvalidos)
        {
            reservas.Remove(bot);
        }
    }


    // =====================================================
    // DEBUG
    // =====================================================

    private void OnDrawGizmos()
    {
        if (puntosEntrega == null)
            return;


        Gizmos.color =
            Color.green;


        foreach (Transform punto in puntosEntrega)
        {
            if (punto == null)
                continue;


            Gizmos.DrawWireSphere(
                punto.position,
                0.4f
            );


            Gizmos.DrawLine(
                transform.position,
                punto.position
            );
        }
    }
}