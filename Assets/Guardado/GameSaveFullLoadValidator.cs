
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Paso 78.
/// Valida una partida completa antes de restaurarla.
/// No crea edificios ni modifica componentes del juego.
/// </summary>
public static class GameSaveFullLoadValidator
{
    public static GameSaveData LeerYValidar(
        SaveStorage almacenamiento,
        int ranura,
        GameSaveSceneReferences referencias,
        out bool desdeRespaldo)
    {
        if (almacenamiento == null)
            throw new ArgumentNullException(nameof(almacenamiento));

        GameSaveData partida;

        if (!almacenamiento.TryLoad(
            ranura,
            out partida,
            out desdeRespaldo))
        {
            throw new InvalidOperationException(
                "No existe una partida valida en la ranura " +
                ranura
            );
        }

        ValidarDatos(partida, referencias);

        return partida;
    }

    public static void ValidarDatos(
        GameSaveData partida,
        GameSaveSceneReferences r)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException(
                "La validacion requiere Play."
            );
        }

        if (partida == null)
            throw new ArgumentNullException(nameof(partida));

        ValidarReferencias(r);

        if (partida.formatVersion !=
            GameSaveData.CurrentFormatVersion)
        {
            throw new InvalidOperationException(
                "Version de guardado incompatible."
            );
        }

        if (partida.sceneName !=
            SceneManager.GetActiveScene().name)
        {
            throw new InvalidOperationException(
                "La partida pertenece a otra escena."
            );
        }

        if (partida.economy == null ||
            partida.economy.coins < 0)
        {
            throw new InvalidOperationException(
                "Economia invalida."
            );
        }

        if (double.IsNaN(partida.totalPlaySeconds) ||
            double.IsInfinity(partida.totalPlaySeconds) ||
            partida.totalPlaySeconds < 0)
        {
            throw new InvalidOperationException(
                "Tiempo de partida invalido."
            );
        }

        // Posicion y orientacion guardadas.
        PlayerSaveAdapter.Validate(partida.player);

        // Desbloqueos.
        if (!partida.hasEquipmentUnlocksData ||
            partida.equipmentUnlocks == null)
        {
            throw new InvalidOperationException(
                "Faltan datos de equipamiento."
            );
        }

        // Mejoras.
        ToolUpgradesSaveAdapter.Read(partida);

        // Procesadora.
        ProcessorSaveAdapter.Read(partida);

        // Inventario.
        BuildingInventorySaveAdapter.Read(partida);

        // Edificios y conexiones.
        PlacedBuildingsSaveAdapter.Read(partida);

        PlacedBuildingsConnectionGraphValidator.Analizar(
            partida
        );
    }

    /// <summary>
    /// Segunda barrera: verifica si es seguro comenzar
    /// la restauracion en la escena actual.
    /// </summary>
    public static void ExigirEscenaPreparada(
        GameSaveData partida,
        GameSaveSceneReferences r)
    {
        ValidarDatos(partida, r);

        Scene escena = SceneManager.GetActiveScene();

        EdificioConstruido[] existentes =
            UnityEngine.Object.FindObjectsByType<
                EdificioConstruido>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (EdificioConstruido edificio in existentes)
        {
            if (edificio != null &&
                edificio.gameObject.scene == escena)
            {
                throw new InvalidOperationException(
                    "La escena ya contiene construcciones. " +
                    "La carga completa necesita una escena limpia."
                );
            }
        }

        // Comprobar catalogo de prefabs y curvaturas.
        PlacedBuildingsRestorePlan plan =
            PlacedBuildingsRestorePlanner.Analizar(
                r.sistema,
                partida
            );

        if (plan.rampasCurvadas > 0)
        {
            throw new InvalidOperationException(
                "La partida contiene rampas curvadas. " +
                "Su reconstruccion aun no esta implementada."
            );
        }

        if (plan.coincidentes != 0 ||
            plan.sobrantes != 0 ||
            plan.conflictos != 0 ||
            plan.porCrear !=
                partida.placedBuildings.edificios.Count)
        {
            throw new InvalidOperationException(
                "El estado de construcciones no esta preparado."
            );
        }

        // Comprobar requisitos de restauracion
        // de la procesadora.
        if (!r.construccionProcesadora.isActiveAndEnabled)
        {
            throw new InvalidOperationException(
                "El componente de construccion " +
                "de la procesadora no esta activo."
            );
        }

        ProcessorSaveData datos = partida.processor;

        if (r.construccionProcesadora.MonedasNecesarias !=
            datos.monedasConstruccionNecesarias)
        {
            throw new InvalidOperationException(
                "El precio de construccion de la " +
                "procesadora ha cambiado."
            );
        }

        int precioEsperado = 0;

        if (datos.desbloqueada)
        {
            if (datos.nivelProcesado == 1)
                precioEsperado =
                    r.construccionProcesadora.precioNivel2;

            if (datos.nivelProcesado == 2)
                precioEsperado =
                    r.construccionProcesadora.precioNivel3;
        }

        if (datos.precioSiguienteMejora != precioEsperado)
        {
            throw new InvalidOperationException(
                "El precio de mejora de la " +
                "procesadora ha cambiado."
            );
        }
    }

    private static void ValidarReferencias(
        GameSaveSceneReferences r)
    {
        if (r == null)
            throw new ArgumentNullException(nameof(r));

        if (r.jugador == null ||
            r.sistema == null ||
            r.cartera == null ||
            r.equipamiento == null ||
            r.torbellino == null ||
            r.iman == null ||
            r.procesadora == null ||
            r.construccionProcesadora == null ||
            r.inventario == null)
        {
            throw new InvalidOperationException(
                "Faltan referencias para restaurar la partida."
            );
        }

        if (r.sistema.inventarioEdificios != r.inventario)
        {
            throw new InvalidOperationException(
                "El inventario no corresponde al " +
                "sistema de construccion."
            );
        }

        if (r.construccionProcesadora.maquinaErosion !=
            r.procesadora)
        {
            throw new InvalidOperationException(
                "Los componentes de la procesadora " +
                "no corresponden."
            );
        }

        if (r.jugador.GetComponent<CharacterController>() == null ||
            r.jugador.GetComponent<MovimientoPersonaje>() == null ||
            r.jugador.GetComponent<RescateVacioJugador>() == null)
        {
            throw new InvalidOperationException(
                "El jugador no tiene todos los " +
                "componentes necesarios para restaurar."
            );
        }

        Scene escena = SceneManager.GetActiveScene();

        Component[] componentes =
        {
            r.sistema,
            r.cartera,
            r.equipamiento,
            r.torbellino,
            r.iman,
            r.procesadora,
            r.construccionProcesadora,
            r.inventario
        };

        if (r.jugador.scene != escena)
        {
            throw new InvalidOperationException(
                "El jugador pertenece a otra escena."
            );
        }

        foreach (Component componente in componentes)
        {
            if (componente.gameObject.scene != escena)
            {
                throw new InvalidOperationException(
                    "Una referencia pertenece a otra escena: " +
                    componente.GetType().Name
                );
            }
        }
    }
}
