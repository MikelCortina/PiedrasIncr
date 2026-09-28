using System;
using System.Collections.Generic;
using UnityEngine;

public class GestorPiedras : MonoBehaviour
{
    public static GestorPiedras Instancia
    {
        get;
        private set;
    }


    // =====================================================
    // EVENTOS
    // =====================================================

    public event Action<Rigidbody> OnPiedraRegistrada;


    // =====================================================
    // DATOS
    // =====================================================

    private readonly HashSet<Rigidbody> piedrasRegistradas =
        new HashSet<Rigidbody>();


    // Piedra -> Bot propietario de la reserva.
    private readonly Dictionary<Rigidbody, BotRecolector> reservas =
        new Dictionary<Rigidbody, BotRecolector>();


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        if (Instancia != null &&
            Instancia != this)
        {
            Debug.LogWarning(
                "Hay más de un GestorPiedras en la escena."
            );

            return;
        }


        Instancia = this;
    }


    // =====================================================
    // REGISTRAR
    // =====================================================

    public void RegistrarPiedra(
        Rigidbody piedra)
    {
        if (piedra == null)
            return;


        bool nueva =
            piedrasRegistradas.Add(
                piedra
            );


        if (nueva)
        {
            OnPiedraRegistrada?.Invoke(
                piedra
            );
        }
    }


    // =====================================================
    // DESREGISTRAR
    // =====================================================

    public void DesregistrarPiedra(
        Rigidbody piedra)
    {
        if (piedra == null)
            return;


        piedrasRegistradas.Remove(
            piedra
        );


        reservas.Remove(
            piedra
        );
    }


    // =====================================================
    // OBTENER TODAS LAS DISPONIBLES
    // =====================================================

    public List<Rigidbody> ObtenerPiedrasDisponibles(
        BotRecolector botSolicitante)
    {
        LimpiarReferenciasInvalidas();


        List<Rigidbody> resultado =
            new List<Rigidbody>();


        foreach (Rigidbody piedra in piedrasRegistradas)
        {
            if (piedra == null)
                continue;


            if (!piedra.gameObject.activeInHierarchy)
                continue;


            // =============================================
            // ¿ESTÁ RESERVADA?
            // =============================================

            if (reservas.TryGetValue(
                    piedra,
                    out BotRecolector botReserva))
            {
                // Puede verla si la reserva es suya.
                if (botReserva != null &&
                    botReserva != botSolicitante)
                {
                    continue;
                }
            }


            resultado.Add(
                piedra
            );
        }


        return resultado;
    }


    // =====================================================
    // RESERVAR
    // =====================================================

    public bool IntentarReservarPiedra(
        Rigidbody piedra,
        BotRecolector bot)
    {
        if (piedra == null ||
            bot == null)
        {
            return false;
        }


        LimpiarReferenciasInvalidas();


        if (!piedrasRegistradas.Contains(
                piedra))
        {
            return false;
        }


        // =============================================
        // YA ESTÁ RESERVADA
        // =============================================

        if (reservas.TryGetValue(
                piedra,
                out BotRecolector propietario))
        {
            // Ya es nuestra.
            if (propietario == bot)
            {
                return true;
            }


            // Es de otro Bot.
            if (propietario != null)
            {
                return false;
            }


            reservas.Remove(
                piedra
            );
        }


        // =============================================
        // RESERVA
        // =============================================

        reservas[piedra] =
            bot;


        return true;
    }


    // =====================================================
    // LIBERAR UNA RESERVA
    // =====================================================

    public void LiberarReserva(
        Rigidbody piedra,
        BotRecolector bot)
    {
        if (piedra == null)
            return;


        if (!reservas.TryGetValue(
                piedra,
                out BotRecolector propietario))
        {
            return;
        }


        // Solo el Bot propietario puede liberarla.
        if (propietario == bot ||
            propietario == null)
        {
            reservas.Remove(
                piedra
            );
        }
    }


    // =====================================================
    // LIBERAR TODO LO DE UN BOT
    // =====================================================

    public void LiberarReservasDeBot(
        BotRecolector bot)
    {
        if (bot == null)
            return;


        List<Rigidbody> eliminar =
            new List<Rigidbody>();


        foreach (
            KeyValuePair<Rigidbody, BotRecolector> reserva
            in reservas)
        {
            if (reserva.Value == bot)
            {
                eliminar.Add(
                    reserva.Key
                );
            }
        }


        foreach (Rigidbody piedra in eliminar)
        {
            reservas.Remove(
                piedra
            );
        }
    }


    // =====================================================
    // CONSULTAS
    // =====================================================

    public bool EstaReservada(
        Rigidbody piedra)
    {
        if (piedra == null)
            return false;


        LimpiarReferenciasInvalidas();


        return reservas.ContainsKey(
            piedra
        );
    }


    public bool EstaReservadaPorOtro(
        Rigidbody piedra,
        BotRecolector bot)
    {
        if (piedra == null)
            return false;


        if (!reservas.TryGetValue(
                piedra,
                out BotRecolector propietario))
        {
            return false;
        }


        return propietario != null &&
               propietario != bot;
    }


    public BotRecolector ObtenerBotReservante(
        Rigidbody piedra)
    {
        if (piedra == null)
            return null;


        reservas.TryGetValue(
            piedra,
            out BotRecolector bot
        );


        return bot;
    }


    public int CantidadPiedrasRegistradas()
    {
        LimpiarReferenciasInvalidas();

        return piedrasRegistradas.Count;
    }


    // =====================================================
    // COMPATIBILIDAD CON CÓDIGO ANTERIOR
    // =====================================================

    public List<Rigidbody> ObtenerPiedrasOrdenadas(
        Vector3 posicion,
        float radioIgnorado)
    {
        List<Rigidbody> piedras =
            ObtenerPiedrasDisponibles(
                null
            );


        piedras.Sort(
            (a, b) =>
            {
                if (a == null)
                    return 1;

                if (b == null)
                    return -1;


                float da =
                    (a.position -
                     posicion).sqrMagnitude;


                float db =
                    (b.position -
                     posicion).sqrMagnitude;


                return da.CompareTo(
                    db
                );
            }
        );


        return piedras;
    }


    // =====================================================
    // LIMPIEZA
    // =====================================================

    private void LimpiarReferenciasInvalidas()
    {
        piedrasRegistradas.RemoveWhere(
            piedra => piedra == null
        );


        List<Rigidbody> reservasEliminar =
            new List<Rigidbody>();


        foreach (
            KeyValuePair<Rigidbody, BotRecolector> reserva
            in reservas)
        {
            if (reserva.Key == null ||
                reserva.Value == null ||
                !piedrasRegistradas.Contains(
                    reserva.Key))
            {
                reservasEliminar.Add(
                    reserva.Key
                );
            }
        }


        foreach (Rigidbody piedra in reservasEliminar)
        {
            reservas.Remove(
                piedra
            );
        }
    }


    private void OnDestroy()
    {
        if (Instancia == this)
        {
            Instancia = null;
        }
    }
}