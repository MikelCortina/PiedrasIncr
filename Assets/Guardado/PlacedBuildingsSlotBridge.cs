
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Puente entre los datos de una ranura
/// y el guardado/restauracion de edificios.
///
/// No escribe archivos directamente.
/// No modifica inventario ni monedas.
///
/// La restauracion solo se permite en una
/// escena sin edificios ya registrados.
/// </summary>
public static class PlacedBuildingsSlotBridge
{
    // ======================================
    // PREPARAR DATOS ANTES DE GUARDAR
    // ======================================

    public static int CapturarParaRanura(
        SistemaConstruccion sistema,
        GameSaveData partida)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException(
                "La captura requiere estar en Play."
            );
        }

        if (sistema == null)
            throw new ArgumentNullException(nameof(sistema));

        if (partida == null)
            throw new ArgumentNullException(nameof(partida));

        // Capturar primero en un objeto temporal.
        // Si falla la validacion, no alteramos
        // los datos anteriores de la partida.
        GameSaveData temporal = new GameSaveData
        {
            sceneName = SceneManager.GetActiveScene().name
        };

        PlacedBuildingsSaveAdapter.Capture(
            sistema,
            temporal
        );

        PlacedBuildingsConnectionGraphValidator.Analizar(
            temporal
        );

        // La captura es valida: incorporar
        // exclusivamente los datos de edificios.
        // Los demas datos de GameSaveData
        // permanecen intactos.
        partida.sceneName = temporal.sceneName;

        partida.placedBuildings =
            temporal.placedBuildings;

        partida.hasPlacedBuildingsData = true;

        return partida.placedBuildings.edificios.Count;
    }

    // ======================================
    // COMPROBAR SI LA RANURA CONTIENE
    // INFORMACION DE CONSTRUCCIONES
    // ======================================

    public static bool TieneDatosDeEdificios(
        GameSaveData partida)
    {
        return partida != null &&
               partida.hasPlacedBuildingsData &&
               partida.placedBuildings != null;
    }

    // ======================================
    // RESTAURAR DESPUES DE LEER LA RANURA
    // ======================================

    public static PlacedBuildingsRestoreResult
        RestaurarEnEscenaVacia(
            SistemaConstruccion sistema,
            GameSaveData partida)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException(
                "La restauracion requiere estar en Play."
            );
        }

        if (sistema == null)
            throw new ArgumentNullException(nameof(sistema));

        if (partida == null)
            throw new ArgumentNullException(nameof(partida));

        if (!TieneDatosDeEdificios(partida))
        {
            throw new InvalidOperationException(
                "Esta partida no contiene datos " +
                "de edificios colocados. " +
                "No se puede reconstruir su estado."
            );
        }

        Scene escena = SceneManager.GetActiveScene();

        if (partida.sceneName != escena.name)
        {
            throw new InvalidOperationException(
                "La partida pertenece a otra escena: " +
                partida.sceneName
            );
        }

        // ======================================
        // VALIDAR LOS DATOS PRIMERO
        // ======================================

        PlacedBuildingsSaveAdapter.Read(partida);

        PlacedBuildingsConnectionGraphValidator.Analizar(
            partida
        );

        // ======================================
        // EVITAR RESTAURAR SOBRE EDIFICIOS
        // QUE YA ESTAN EN LA ESCENA
        // ======================================

        EdificioConstruido[] existentes =
            UnityEngine.Object.FindObjectsByType<
                EdificioConstruido>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (EdificioConstruido edificio in existentes)
        {
            if (edificio == null ||
                edificio.gameObject.scene != escena)
            {
                continue;
            }

            throw new InvalidOperationException(
                "La escena ya contiene construcciones. " +
                "No se restaurara la ranura sobre " +
                "edificios existentes. " +
                "Primero debe prepararse una escena limpia."
            );
        }

        // ======================================
        // RECONSTRUIR EDIFICIOS Y CONEXIONES
        // ======================================

        return PlacedBuildingsRestoreCoordinator
            .RestaurarNuevos(
                sistema,
                partida
            );
    }
}
