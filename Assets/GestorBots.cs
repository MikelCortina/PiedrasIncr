using System.Collections.Generic;
using UnityEngine;

public class GestorBots : MonoBehaviour
{
    // =====================================================
    // SINGLETON
    // =====================================================

    public static GestorBots Instancia
    {
        get;
        private set;
    }


    // =====================================================
    // CONFIGURACIÓN
    // =====================================================

    [Header("Configuración")]

    [Tooltip("Prefijo utilizado para nombrar automáticamente los Bots.")]
    public string prefijoNombre =
        "BOT-";


    [Tooltip(
        "Número de dígitos. Con 2 tendremos BOT-01, BOT-02..."
    )]
    [Range(1, 4)]
    public int cantidadDigitos =
        2;


    // =====================================================
    // INTERNAS
    // =====================================================

    private int siguienteId =
        1;


    private readonly Dictionary<
        ConfiguracionBot,
        int
    > botsRegistrados =
        new Dictionary<
            ConfiguracionBot,
            int
        >();


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        if (Instancia != null &&
            Instancia != this)
        {
            Destroy(
                gameObject
            );

            return;
        }


        Instancia =
            this;
    }


    // =====================================================
    // REGISTRAR BOT
    // =====================================================

    public int RegistrarBot(
        ConfiguracionBot bot)
    {
        if (bot == null)
            return -1;


        // Si ya estaba registrado,
        // devolvemos su ID actual.
        if (botsRegistrados.TryGetValue(
                bot,
                out int idExistente))
        {
            return idExistente;
        }


        int nuevoId =
            siguienteId;


        siguienteId++;


        botsRegistrados.Add(
            bot,
            nuevoId
        );


        bot.nombreBot =
            GenerarNombre(
                nuevoId
            );


        Debug.Log(
            "Bot registrado: " +
            bot.nombreBot
        );


        return nuevoId;
    }


    // =====================================================
    // ELIMINAR BOT
    // =====================================================

    public void DesregistrarBot(
        ConfiguracionBot bot)
    {
        if (bot == null)
            return;


        botsRegistrados.Remove(
            bot
        );
    }


    // =====================================================
    // GENERAR NOMBRE
    // =====================================================

    private string GenerarNombre(
        int id)
    {
        string formato =
            new string(
                '0',
                cantidadDigitos
            );


        return prefijoNombre +
               id.ToString(
                   formato
               );
    }


    // =====================================================
    // CONSULTAS
    // =====================================================

    public int ObtenerCantidadBots()
    {
        LimpiarBotsInvalidos();


        return botsRegistrados.Count;
    }


    public bool EstaRegistrado(
        ConfiguracionBot bot)
    {
        if (bot == null)
            return false;


        return botsRegistrados
            .ContainsKey(
                bot
            );
    }


    public int ObtenerId(
        ConfiguracionBot bot)
    {
        if (bot == null)
            return -1;


        if (botsRegistrados.TryGetValue(
                bot,
                out int id))
        {
            return id;
        }


        return -1;
    }


    // =====================================================
    // LIMPIEZA
    // =====================================================

    private void LimpiarBotsInvalidos()
    {
        List<ConfiguracionBot> eliminar =
            new List<ConfiguracionBot>();


        foreach (
            KeyValuePair<
                ConfiguracionBot,
                int
            > bot
            in botsRegistrados)
        {
            if (bot.Key == null)
            {
                eliminar.Add(
                    bot.Key
                );
            }
        }


        foreach (
            ConfiguracionBot bot
            in eliminar)
        {
            botsRegistrados.Remove(
                bot
            );
        }
    }
}