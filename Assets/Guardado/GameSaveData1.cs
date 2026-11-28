
using System;
using System.Collections.Generic;

[Serializable]
public sealed class GameSaveData
{
    // 2: añade los datos de la demo (F9). Las partidas de la versión 1 se siguen
    // cargando con valores por defecto para todo lo nuevo (ver Migrar).
    public const int CurrentFormatVersion = 2;
    public const int MinSupportedFormatVersion = 1;

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

    // Demo (F9): piedras, tanque, mejoras del Excel, Fisionador y luna.
    public bool hasDemoData;
    public DemoSaveData demo;

    /// <summary>
    /// Sube una partida antigua al formato actual en memoria. Lo nuevo queda "sin datos"
    /// (hasDemoData = false) y el juego conserva sus valores por defecto.
    /// Devuelve false si la versión no se puede leer.
    /// </summary>
    public static bool Migrar(GameSaveData data)
    {
        if (data == null) return false;
        if (data.formatVersion < MinSupportedFormatVersion ||
            data.formatVersion > CurrentFormatVersion)
            return false;

        if (data.formatVersion < 2)
        {
            data.hasDemoData = false;
            data.demo = null;
        }

        data.formatVersion = CurrentFormatVersion;
        return true;
    }
}

// ========================================
// DEMO (F9)
// ========================================

[Serializable]
public sealed class DemoSaveData
{
    // Tanque de gasolina (fracción 0-1).
    public float tanqueFraccion;

    // Mejoras del Excel con niveles propios (0 = sin datos, se deja como está).
    public int nivelFusil;
    public int nivelCapacidadTorbellino;

    // Lanzapiedras (diana) y Arma Lunar.
    public DemoConstruccionSaveData lanzapiedras = new DemoConstruccionSaveData();
    public DemoConstruccionSaveData armaLunar = new DemoConstruccionSaveData();

    // Unidades compradas en total de cada edificio (fijan el precio ×1,3 de la siguiente).
    public List<BuildingInventoryItemSaveData> edificiosComprados =
        new List<BuildingInventoryItemSaveData>();

    // Fisionador y misil grande.
    public bool hasFisionador;
    public bool fisionadorDesbloqueado;
    public int esferasEnFisionador;
    public bool fisionadorCompletado;
    public int misilesSinLanzar;

    // Luna.
    public bool hasFaseLunar;
    public int faseLunar;

    // Final.
    public bool demoCompletada;

    // Piedras del mundo con su desgaste.
    public bool hasPiedras;
    public List<DemoPiedraSaveData> piedras = new List<DemoPiedraSaveData>();

    // Fragmentos que estaban en el suelo, ya pagados en economy.coins al guardar.
    public int monedasPorFragmentosEnSuelo;

    // Edificios que se construyen lanzándoles monedas (ConstruccionConMonedas):
    // Generador de Arañas, Fisionador, tienda de edificios... Partidas antiguas: lista vacía (en ruinas).
    public List<DemoConstruccionMonedasSaveData> construccionesConMonedas =
        new List<DemoConstruccionMonedasSaveData>();
}

[Serializable]
public sealed class DemoConstruccionMonedasSaveData
{
    // ConstruccionConMonedas.IdGuardado (GeneradorAranas, Fisionador, TiendaEdificios o el nombre del objeto).
    public string id;
    // 0 = en ruinas.
    public int nivel;
    public int monedasAportadas;
}

[Serializable]
public sealed class DemoConstruccionSaveData
{
    public bool hasData;
    public bool construida;
    public int nivel;
    public int monedasAportadas;
}

[Serializable]
public sealed class DemoPiedraSaveData
{
    public float positionX;
    public float positionY;
    public float positionZ;

    public float rotationX;
    public float rotationY;
    public float rotationZ;
    public float rotationW;

    public float scaleX;
    public float scaleY;
    public float scaleZ;

    public float desgaste;
    public bool esferaPremiada;
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
