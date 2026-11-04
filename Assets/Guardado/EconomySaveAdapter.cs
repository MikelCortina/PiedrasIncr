using System;

/// <summary>
/// Conecta el estado de Cartera con el DTO economy de GameSaveData.
/// NO escribe archivos, NO selecciona ranuras y NO gestiona tiempos de carga.
/// Esa coordinacion correspondera al futuro GestorGuardado.
/// </summary>
public static class EconomySaveAdapter
{
    public static void Capture(Cartera cartera, GameSaveData data)
    {
        if (cartera == null)
            throw new ArgumentNullException(nameof(cartera));
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (data.economy == null)
            data.economy = new EconomySaveData();

        if (cartera.Monedas < 0)
            throw new InvalidOperationException("La cartera contiene un saldo negativo.");

        data.economy.coins = cartera.Monedas;
    }

    public static void Restore(Cartera cartera, GameSaveData data)
    {
        if (cartera == null)
            throw new ArgumentNullException(nameof(cartera));
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (data.economy == null || data.economy.coins < 0)
            throw new InvalidOperationException("Datos de economia inexistentes o invalidos.");

        cartera.RestaurarMonedas(data.economy.coins);
    }
}
