using System;

// Datos puros: ninguna referencia a GameObject, Transform ni componentes de Unity.
// El resto de sistemas del juego se incorporarán mediante DTOs versionados.
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

    // Primer sistema de prueba. Todavía NO se conecta a Cartera.
    public EconomySaveData economy = new EconomySaveData();
}

[Serializable]
public sealed class EconomySaveData
{
    public int coins;
}
