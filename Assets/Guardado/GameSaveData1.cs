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

    // Campo opcional en v1: archivos anteriores con solo economía siguen siendo legibles.
    public PlayerSaveData player = new PlayerSaveData();
}

[Serializable]
public sealed class EconomySaveData
{
    public int coins;
}

[Serializable]
public sealed class PlayerSaveData
{
    // False significa: la posición segura todavía no ha sido capturada.
    // Nunca debe interpretarse (0,0,0) como punto de aparición por defecto.
    public bool hasSafePosition;

    public float positionX;
    public float positionY;
    public float positionZ;
    public float yawDegrees;
}
