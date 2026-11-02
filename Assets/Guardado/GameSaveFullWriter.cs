
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Referencias necesarias para capturar la partida.
/// </summary>
public sealed class GameSaveSceneReferences
{
    public GameObject jugador;
    public SistemaConstruccion sistema;
    public Cartera cartera;
    public GestorEquipamiento equipamiento;
    public HerramientaTorbellino torbellino;
    public RecolectorMagnetico iman;
    public MaquinaErosion procesadora;
    public DesbloqueoProcesadoraMonedas construccionProcesadora;
    public InventarioEdificios inventario;
}

/// <summary>
/// Paso 77: captura el estado completo implementado
/// hasta ahora y lo guarda mediante el sistema existente.
/// No restaura ni modifica componentes del juego.
/// </summary>
public static class GameSaveFullWriter
{
    public static GameSaveData CapturarEstado(
        GameSaveSceneReferences referencias,
        double segundosJugados)
    {
        GameSaveData partida =
            CapturarDatosBase(referencias, segundosJugados);

        PlacedBuildingsSlotBridge.CapturarParaRanura(
            referencias.sistema,
            partida
        );

        return partida;
    }

    public static GameSaveData GuardarEnRanura(
        SaveStorage almacenamiento,
        int ranura,
        GameSaveSceneReferences referencias,
        double segundosJugados)
    {
        if (almacenamiento == null)
            throw new ArgumentNullException(nameof(almacenamiento));

        GameSaveData partida =
            CapturarDatosBase(referencias, segundosJugados);

        // Conservar la identidad al sobrescribir
        // una partida existente de la misma escena.
        GameSaveData anterior;
        bool desdeRespaldo;

        if (almacenamiento.TryLoad(
            ranura,
            out anterior,
            out desdeRespaldo))
        {
            if (anterior.sceneName == partida.sceneName)
            {
                partida.saveId = anterior.saveId;
                partida.createdUtc = anterior.createdUtc;
            }
        }

        // Reutilizar el escritor del paso 74.
        // Captura edificios y escribe el archivo.
        return GameSaveSlotWriter.GuardarConEdificios(
            almacenamiento,
            ranura,
            referencias.sistema,
            partida
        );
    }

    private static GameSaveData CapturarDatosBase(
        GameSaveSceneReferences r,
        double segundosJugados)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException(
                "La captura completa requiere Play."
            );
        }

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
                "Faltan referencias para guardar la partida."
            );
        }

        if (r.sistema.inventarioEdificios != r.inventario)
        {
            throw new InvalidOperationException(
                "El inventario no corresponde al sistema."
            );
        }

        if (r.construccionProcesadora.maquinaErosion !=
            r.procesadora)
        {
            throw new InvalidOperationException(
                "La procesadora no corresponde a su construccion."
            );
        }

        if (double.IsNaN(segundosJugados) ||
            double.IsInfinity(segundosJugados) ||
            segundosJugados < 0)
        {
            throw new InvalidOperationException(
                "Tiempo de partida invalido."
            );
        }

        GameSaveData partida = new GameSaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            totalPlaySeconds = segundosJugados
        };

        // 1. Economia
        EconomySaveAdapter.Capture(
            r.cartera,
            partida
        );

        // 2. Posicion segura del jugador
        PlayerSaveAdapter.Capture(
            r.jugador,
            partida
        );

        // 3. Desbloqueos
        EquipmentUnlocksSaveAdapter.Capture(
            r.equipamiento,
            partida
        );

        // 4. Mejoras
        ToolUpgradesSaveAdapter.Capture(
            r.torbellino,
            r.iman,
            partida
        );

        // 5. Procesadora
        ProcessorSaveAdapter.Capture(
            r.procesadora,
            r.construccionProcesadora,
            partida
        );

        // 6. Inventario
        BuildingInventorySaveAdapter.Capture(
            r.inventario,
            partida
        );

        return partida;
    }
}
