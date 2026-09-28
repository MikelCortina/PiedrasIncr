using System.Collections.Generic;
using UnityEngine;

public class GestorPiedras : MonoBehaviour
{
    public static GestorPiedras Instancia { get; private set; }

    public event System.Action<Rigidbody> OnPiedraRegistrada;

    // Todas las piedras que existen actualmente.
    private readonly HashSet<Rigidbody> piedrasRegistradas =
        new HashSet<Rigidbody>();

    // Piedra -> Bot que la tiene reservada.
    private readonly Dictionary<Rigidbody, BotRecolector> reservas =
        new Dictionary<Rigidbody, BotRecolector>();


    public int CantidadPiedrasRegistradas
    {
        get
        {
            LimpiarReferenciasInvalidas();
            return piedrasRegistradas.Count;
        }
    }


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        if (Instancia != null &&
            Instancia != this)
        {
            Debug.LogWarning(
                "Hay más de un GestorPiedras en la escena. " +
                "Se destruirá el duplicado."
            );

            Destroy(gameObject);
            return;
        }

        Instancia = this;
    }


    // =====================================================
    // REGISTRAR PIEDRA
    // =====================================================

    public void RegistrarPiedra(Rigidbody piedra)
    {
        if (piedra == null)
            return;


        bool esNueva =
            piedrasRegistradas.Add(piedra);


        // Solo avisamos si realmente acaba de aparecer.
        if (esNueva)
        {
            OnPiedraRegistrada?.Invoke(
                piedra
            );
        }
    }


    // =====================================================
    // DESREGISTRAR PIEDRA
    // =====================================================

    public void DesregistrarPiedra(Rigidbody piedra)
    {
        if (piedra == null)
        {
            LimpiarReferenciasInvalidas();
            return;
        }

        piedrasRegistradas.Remove(piedra);

        reservas.Remove(piedra);
    }


    // =====================================================
    // RESERVAR PIEDRA
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


        // Si por algún motivo todavía no estaba
        // registrada, la añadimos.
        piedrasRegistradas.Add(piedra);


        // Ya la tiene otro Bot.
        if (reservas.TryGetValue(
                piedra,
                out BotRecolector botActual))
        {
            // Si ya era nuestra, todo correcto.
            if (botActual == bot)
                return true;

            return false;
        }


        reservas.Add(
            piedra,
            bot
        );


        return true;
    }


    // =====================================================
    // LIBERAR RESERVA
    // =====================================================

    public void LiberarReserva(
        Rigidbody piedra,
        BotRecolector bot)
    {
        if (piedra == null)
        {
            LimpiarReferenciasInvalidas();
            return;
        }


        if (!reservas.TryGetValue(
                piedra,
                out BotRecolector botActual))
        {
            return;
        }


        // Solo puede liberar la reserva
        // el mismo Bot que la creó.
        if (botActual != bot)
            return;


        reservas.Remove(piedra);
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
            reservas.Remove(piedra);
        }
    }


    // =====================================================
    // ESTÁ RESERVADA
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


    // =====================================================
    // OBTENER PIEDRAS
    // =====================================================

    public List<Rigidbody> ObtenerPiedrasOrdenadas(
        Vector3 posicionBot,
        float radioPreferente)
    {
        LimpiarReferenciasInvalidas();


        List<Rigidbody> resultado =
            new List<Rigidbody>(
                piedrasRegistradas.Count
            );


        foreach (Rigidbody piedra in piedrasRegistradas)
        {
            if (piedra == null)
                continue;


            if (!piedra.gameObject.activeInHierarchy)
                continue;


            // Ya está trabajando otro Bot con ella.
            if (reservas.ContainsKey(piedra))
                continue;


            resultado.Add(piedra);
        }


        float radioCuadrado =
            radioPreferente *
            radioPreferente;


        // =================================================
        // ORDEN:
        //
        // 1. Piedras dentro del radio preferente.
        // 2. Las más cercanas primero.
        // 3. Después piedras fuera del radio,
        //    también de cercana a lejana.
        //
        // El radio YA NO es un límite.
        // =================================================

        resultado.Sort(
            (a, b) =>
            {
                float distanciaA =
                    (a.position -
                     posicionBot).sqrMagnitude;

                float distanciaB =
                    (b.position -
                     posicionBot).sqrMagnitude;


                bool aCerca =
                    distanciaA <= radioCuadrado;

                bool bCerca =
                    distanciaB <= radioCuadrado;


                if (aCerca && !bCerca)
                    return -1;


                if (!aCerca && bCerca)
                    return 1;


                return distanciaA.CompareTo(
                    distanciaB
                );
            }
        );


        return resultado;
    }


    // =====================================================
    // LIMPIEZA
    // =====================================================

    private void LimpiarReferenciasInvalidas()
    {
        piedrasRegistradas.RemoveWhere(
            piedra => piedra == null
        );


        List<Rigidbody> reservasInvalidas =
            new List<Rigidbody>();


        foreach (
            KeyValuePair<Rigidbody, BotRecolector> reserva
            in reservas)
        {
            if (reserva.Key == null ||
                reserva.Value == null)
            {
                reservasInvalidas.Add(
                    reserva.Key
                );
            }
        }


        foreach (Rigidbody piedra in reservasInvalidas)
        {
            reservas.Remove(piedra);
        }
    }


    // =====================================================
    // DESTROY
    // =====================================================

    private void OnDestroy()
    {
        if (Instancia == this)
        {
            Instancia = null;
        }
    }
}