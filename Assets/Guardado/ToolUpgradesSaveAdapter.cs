using System;

/// <summary>
/// Fase de captura: copia los niveles permanentes del Torbellino y del Iman.
/// No modifica herramientas, compras, monedas, escenas ni ranuras reales.
/// La restauracion se incorporara en el siguiente modulo, tras probar la captura.
/// </summary>
public static class ToolUpgradesSaveAdapter
{
    public static void Capture(
        HerramientaTorbellino torbellino,
        RecolectorMagnetico iman,
        GameSaveData save)
    {
        if (torbellino == null) throw new ArgumentNullException(nameof(torbellino));
        if (iman == null) throw new ArgumentNullException(nameof(iman));
        if (save == null) throw new ArgumentNullException(nameof(save));

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

    /// <summary>
    /// Lee un snapshot valido. Nunca aplica cambios al jugador.
    /// </summary>
    public static ToolUpgradesSaveData Read(GameSaveData save)
    {
        if (save == null) throw new ArgumentNullException(nameof(save));
        if (!save.hasToolUpgradesData || save.toolUpgrades == null)
            throw new InvalidOperationException(
                "La partida no contiene datos de mejoras de herramientas.");

        Validate(save.toolUpgrades);
        return save.toolUpgrades;
    }

    public static void Validate(ToolUpgradesSaveData data)
    {
        if (data == null ||
            data.nivelRadioTorbellino < 1 || data.nivelRadioTorbellino > 4 ||
            data.nivelAlcanceTorbellino < 1 || data.nivelAlcanceTorbellino > 3 ||
            data.nivelMovilidadTorbellino < 1 || data.nivelMovilidadTorbellino > 3 ||
            data.nivelAlcanceIman < 1 || data.nivelAlcanceIman > 3)
        {
            throw new InvalidOperationException(
                "El archivo contiene niveles de herramientas fuera de rango.");
        }
    }
}
