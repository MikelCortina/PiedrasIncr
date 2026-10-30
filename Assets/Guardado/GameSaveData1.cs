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

    // Campo de presencia explicito. JsonUtility puede materializar clases
    // serializables aunque su valor original fuese null o faltase en el JSON.
    // Los guardados antiguos carecen de esta marca (false por defecto).
    public bool hasEquipmentUnlocksData;

    // Solo es valido cuando hasEquipmentUnlocksData == true.
    // false + datos por defecto NO significa que el jugador no tenga mejoras.
    public EquipmentUnlocksSaveData equipmentUnlocks;
}

[Serializable]
public sealed class EconomySaveData
{
    public int coins;
}

[Serializable]
public sealed class PlayerSaveData
{
    // False significa: la posicion segura todavia no ha sido capturada.
    // Nunca debe interpretarse (0,0,0) como punto de aparicion por defecto.
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
