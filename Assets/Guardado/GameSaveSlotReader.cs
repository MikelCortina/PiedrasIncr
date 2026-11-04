
using System;
using UnityEngine;

public static class GameSaveSlotReader
{
    public static PlacedBuildingsRestoreResult RestaurarEdificios(
        SaveStorage almacenamiento,
        int ranura,
        SistemaConstruccion sistema,
        out GameSaveData partidaLeida,
        out bool desdeRespaldo)
    {
        partidaLeida = null;
        desdeRespaldo = false;

        if (!Application.isPlaying)
        {
            throw new InvalidOperationException(
                "La carga requiere estar en Play."
            );
        }

        if (almacenamiento == null)
            throw new ArgumentNullException(nameof(almacenamiento));

        if (sistema == null)
            throw new ArgumentNullException(nameof(sistema));

        GameSaveData candidata;
        bool respaldo;

        if (!almacenamiento.TryLoad(
            ranura,
            out candidata,
            out respaldo))
        {
            throw new InvalidOperationException(
                "No existe una partida valida en la ranura " +
                ranura
            );
        }

        PlacedBuildingsRestoreResult resultado =
            PlacedBuildingsSlotBridge.RestaurarEnEscenaVacia(
                sistema,
                candidata
            );

        partidaLeida = candidata;
        desdeRespaldo = respaldo;

        return resultado;
    }
}
