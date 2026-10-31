
using System;

// Datos serializables de una partida.
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
    // La marca impide interpretar un guardado antiguo
    // como si tuviera una procesadora sin desbloquear.
    public bool hasProcessorData;
    public ProcessorSaveData processor;
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

// Estado permanente de la procesadora.
[Serializable]
public sealed class ProcessorSaveData
{
    public bool desbloqueada;
    public int nivelProcesado;

    // Construccion
    public int monedasConstruccionAportadas;
    public int monedasConstruccionNecesarias;

    // Siguiente mejora
    public int monedasMejoraAportadas;
    public int precioSiguienteMejora;
}
