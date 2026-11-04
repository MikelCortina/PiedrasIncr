
using System;
using UnityEngine;

public static class GameSaveSlotWriter
{
    public static GameSaveData GuardarConEdificios(
        SaveStorage almacenamiento,
        int ranura,
        SistemaConstruccion sistema,
        GameSaveData datosBase)
    {
        if (almacenamiento == null)
            throw new ArgumentNullException(nameof(almacenamiento));

        if (sistema == null)
            throw new ArgumentNullException(nameof(sistema));

        if (datosBase == null)
            throw new ArgumentNullException(nameof(datosBase));

        GameSaveData captura =
            JsonUtility.FromJson<GameSaveData>(
                JsonUtility.ToJson(datosBase)
            );

        PlacedBuildingsSlotBridge.CapturarParaRanura(
            sistema,
            captura
        );

        almacenamiento.Save(ranura, captura);

        return captura;
    }
}
