
using System;
using System.Collections.Generic;

[Serializable]
public sealed class GameSaveData
{
    public const int CurrentFormatVersion = 1;

    public int formatVersion = CurrentFormatVersion;
    public int slotIndex;
    public string saveId;
    public string createdUtc;
    public string savedUtc;
    public double totalPlaySeconds;
    public string sceneName = "Piedra";

    // Economia
    public EconomySaveData economy = new EconomySaveData();

    // Jugador
    public PlayerSaveData player = new PlayerSaveData();

    // Desbloqueos de herramientas
    public bool hasEquipmentUnlocksData;
    public EquipmentUnlocksSaveData equipmentUnlocks;

    // Mejoras del Torbellino e Iman
    public bool hasToolUpgradesData;
    public ToolUpgradesSaveData toolUpgrades;

    // Procesadora
    public bool hasProcessorData;
    public ProcessorSaveData processor;

    // Inventario de edificios
    public bool hasBuildingInventoryData;
    public BuildingInventorySaveData buildingInventory;

    // Edificios colocados y sus conexiones
    public bool hasPlacedBuildingsData;
    public PlacedBuildingsSaveData placedBuildings;
}

[Serializable]
public sealed class EconomySaveData
{
    public int coins;
}

[Serializable]
public sealed class PlayerSaveData
{
    public bool hasSafePosition;
    public float positionX;
    public float positionY;
    public float positionZ;
    public float yawDegrees;
}

[Serializable]
public sealed class EquipmentUnlocksSaveData
{
    public bool armaLanzadoraDesbloqueada;
    public bool torbellinoDesbloqueado;
    public bool martilloDesbloqueado;
}

[Serializable]
public sealed class ToolUpgradesSaveData
{
    public int nivelRadioTorbellino;
    public int nivelAlcanceTorbellino;
    public int nivelMovilidadTorbellino;

    public bool imanDesbloqueado;
    public int nivelAlcanceIman;
}

[Serializable]
public sealed class ProcessorSaveData
{
    public bool desbloqueada;
    public int nivelProcesado;

    public int monedasConstruccionAportadas;
    public int monedasConstruccionNecesarias;

    public int monedasMejoraAportadas;
    public int precioSiguienteMejora;
}

// ========================================
// INVENTARIO DE EDIFICIOS
// ========================================

[Serializable]
public sealed class BuildingInventorySaveData
{
    public List<BuildingInventoryItemSaveData> articulos =
        new List<BuildingInventoryItemSaveData>();
}

[Serializable]
public sealed class BuildingInventoryItemSaveData
{
    public string tipo;
    public int unidades;
}

// ========================================
// EDIFICIOS COLOCADOS
// ========================================

[Serializable]
public sealed class PlacedBuildingsSaveData
{
    public List<PlacedBuildingSaveData> edificios =
        new List<PlacedBuildingSaveData>();
}

[Serializable]
public sealed class PlacedBuildingSaveData
{
    // Identidad y prefab
    public string idPersistente;
    public string tipo;
    public int variantePrefab;
    public float curvaturaRampa;

    // Posicion global
    public float positionX;
    public float positionY;
    public float positionZ;

    // Rotacion global
    public float rotationX;
    public float rotationY;
    public float rotationZ;
    public float rotationW;

    // Escala local
    public float scaleX;
    public float scaleY;
    public float scaleZ;

    // ========================================
    // NUEVO: CONEXION ENTRE EDIFICIOS
    // ========================================

    // Indica si este edificio esta conectado
    // a otro mediante un conector.
    public bool hasConnection;

    // ID permanente del edificio propietario
    // del conector utilizado.
    public string connectedToBuildingId;

    // Ruta del conector dentro del prefab.
    // Ejemplo: "GameObject[0]"
    public string connectorPath;

    // Tag del conector utilizado.
    // Ejemplos: RailRampa, ConectorSalida,
    // ConectorParedEntrada, etc.
    public string connectorTag;
}
