using System;

// Datos puros serializables; sin referencias a objetos de la escena.
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

    public EconomySaveData economy = new EconomySaveData();

    // Campo opcional en v1: archivos anteriores con solo economia siguen siendo legibles.
    public PlayerSaveData player = new PlayerSaveData();

    // Marca explicita para distinguir datos inexistentes de 'todo bloqueado'.
    public bool hasEquipmentUnlocksData;
    public EquipmentUnlocksSaveData equipmentUnlocks;

    // Marca explicita: archivos anteriores a las mejoras NO contienen estos datos.
    // Nunca asumir nivel 1 o iman bloqueado si la marca es false.
    public bool hasToolUpgradesData;
    public ToolUpgradesSaveData toolUpgrades;
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
    public int nivelRadioTorbellino;      // 1..4
    public int nivelAlcanceTorbellino;    // 1..3
    public int nivelMovilidadTorbellino;  // 1..3
    public bool imanDesbloqueado;
    public int nivelAlcanceIman;          // 1..3
}
