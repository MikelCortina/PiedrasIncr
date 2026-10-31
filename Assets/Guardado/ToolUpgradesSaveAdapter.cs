
using System;
using UnityEngine;

public static class ToolUpgradesSaveAdapter
{
    // Capturar las mejoras actuales.
    public static void Capture(
        HerramientaTorbellino torbellino,
        RecolectorMagnetico iman,
        GameSaveData save)
    {
        if (torbellino == null)
            throw new ArgumentNullException(nameof(torbellino));

        if (iman == null)
            throw new ArgumentNullException(nameof(iman));

        if (save == null)
            throw new ArgumentNullException(nameof(save));

        var data = new ToolUpgradesSaveData
        {
            nivelRadioTorbellino = torbellino.NivelRadio,
            nivelAlcanceTorbellino = torbellino.NivelAlcance,
            nivelMovilidadTorbellino = torbellino.NivelMovilidad,
            imanDesbloqueado = iman.EstaImanDesbloqueado(),
            nivelAlcanceIman = iman.NivelAlcance
        };

        Validate(data);

        save.toolUpgrades = data;
        save.hasToolUpgradesData = true;
    }

    // Leer sin modificar el jugador.
    public static ToolUpgradesSaveData Read(GameSaveData save)
    {
        if (save == null)
            throw new ArgumentNullException(nameof(save));

        if (!save.hasToolUpgradesData ||
            save.toolUpgrades == null)
        {
            throw new InvalidOperationException(
                "Esta partida no contiene mejoras guardadas.");
        }

        Validate(save.toolUpgrades);
        return save.toolUpgrades;
    }

    // Restaurar los niveles sin comprar mejoras.
    public static void Restore(
        HerramientaTorbellino torbellino,
        RecolectorMagnetico iman,
        GameSaveData save)
    {
        if (torbellino == null)
            throw new ArgumentNullException(nameof(torbellino));

        if (iman == null)
            throw new ArgumentNullException(nameof(iman));

        ToolUpgradesSaveData data = Read(save);

        // Estado anterior por seguridad.
        int radioAnterior = torbellino.NivelRadio;
        int alcanceAnterior = torbellino.NivelAlcance;
        int movilidadAnterior = torbellino.NivelMovilidad;

        bool imanAnterior = iman.EstaImanDesbloqueado();
        int alcanceImanAnterior = iman.NivelAlcance;

        try
        {
            torbellino.RestaurarNiveles(
                data.nivelRadioTorbellino,
                data.nivelAlcanceTorbellino,
                data.nivelMovilidadTorbellino
            );

            iman.RestaurarEstadoIman(
                data.imanDesbloqueado,
                data.nivelAlcanceIman
            );
        }
        catch
        {
            // Intentar recuperar el estado anterior.
            try
            {
                torbellino.RestaurarNiveles(
                    radioAnterior,
                    alcanceAnterior,
                    movilidadAnterior
                );
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            try
            {
                iman.RestaurarEstadoIman(
                    imanAnterior,
                    alcanceImanAnterior
                );
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            throw;
        }
    }

    // Comprobar que los niveles sean validos.
    public static void Validate(ToolUpgradesSaveData data)
    {
        if (data == null ||
            data.nivelRadioTorbellino < 1 ||
            data.nivelRadioTorbellino > 4 ||
            data.nivelAlcanceTorbellino < 1 ||
            data.nivelAlcanceTorbellino > 3 ||
            data.nivelMovilidadTorbellino < 1 ||
            data.nivelMovilidadTorbellino > 3 ||
            data.nivelAlcanceIman < 1 ||
            data.nivelAlcanceIman > 3)
        {
            throw new InvalidOperationException(
                "El guardado contiene niveles de herramientas invalidos.");
        }
    }
}
