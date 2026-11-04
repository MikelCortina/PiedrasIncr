using System;

/// <summary>
/// Captura/restaura UNICAMENTE los desbloqueos del GestorEquipamiento.
/// No cobra monedas ni guarda archivos; esa tarea pertenece al coordinador global.
/// </summary>
public static class EquipmentUnlocksSaveAdapter
{
    public static void Capture(GestorEquipamiento equipment, GameSaveData save)
    {
        if (equipment == null) throw new ArgumentNullException(nameof(equipment));
        if (save == null) throw new ArgumentNullException(nameof(save));

        save.equipmentUnlocks = new EquipmentUnlocksSaveData
        {
            armaLanzadoraDesbloqueada = equipment.ArmaLanzadoraDesbloqueada(),
            torbellinoDesbloqueado = equipment.TorbellinoDesbloqueado(),
            martilloDesbloqueado = equipment.MartilloDesbloqueado()
        };
        // La marca diferencia "todas bloqueadas" de "datos inexistentes".
        save.hasEquipmentUnlocksData = true;
    }

    public static void Restore(GestorEquipamiento equipment, GameSaveData save)
    {
        if (equipment == null) throw new ArgumentNullException(nameof(equipment));
        if (save == null) throw new ArgumentNullException(nameof(save));
        if (!save.hasEquipmentUnlocksData || save.equipmentUnlocks == null)
            throw new InvalidOperationException(
                "El guardado es anterior a los desbloqueos de equipo; no se modificara el jugador.");

        EquipmentUnlocksSaveData snapshot = save.equipmentUnlocks;
        equipment.RestaurarDesbloqueos(
            snapshot.armaLanzadoraDesbloqueada,
            snapshot.torbellinoDesbloqueado,
            snapshot.martilloDesbloqueado);
    }
}
