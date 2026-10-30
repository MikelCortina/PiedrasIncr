using System;
using UnityEngine;

/// <summary>
/// Restituye solo la posicion/orientacion del jugador, tras validar que sigue
/// habiendo suelo seguro en las coordenadas del guardado.
/// El resto del mundo debe estar construido ANTES de llamar a Restore.
/// No modifica monedas, escenas, edificios, bots ni archivos de guardado.
/// </summary>
public static class PlayerRestoreAdapter
{
    public static void Restore(GameObject player, GameSaveData data)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        if (data == null) throw new ArgumentNullException(nameof(data));
        PlayerSaveAdapter.Validate(data.player);

        CharacterController controller = player.GetComponent<CharacterController>();
        MovimientoPersonaje movement = player.GetComponent<MovimientoPersonaje>();
        RescateVacioJugador rescue = player.GetComponent<RescateVacioJugador>();
        if (controller == null || movement == null || rescue == null)
            throw new InvalidOperationException(
                "El Player necesita CharacterController, MovimientoPersonaje y RescateVacioJugador.");
        if (!controller.enabled || !movement.enabled || !rescue.enabled || rescue.RescateActivo)
            throw new InvalidOperationException("El jugador debe estar activo y fuera de un rescate.");

        Vector3 destination = PlayerSaveAdapter.GetSavedPosition(data.player);
        Quaternion orientation = Quaternion.Euler(0f, data.player.yawDegrees, 0f);

        // Evita teletransportarse a un lugar donde el suelo ya no existe
        // (por ejemplo, sobre un edificio que todavia no se ha reconstruido).
        ValidateGround(player, controller, rescue, destination);

        Vector3 oldPosition = player.transform.position;
        Quaternion oldRotation = player.transform.rotation;
        bool changed = false;
        try
        {
            controller.enabled = false;
            changed = true;
            player.transform.SetPositionAndRotation(destination, orientation);
            Physics.SyncTransforms();
            movement.RestablecerEstadoTrasCarga();
            rescue.RestablecerEstadoTrasCarga();
            controller.enabled = true;
            Physics.SyncTransforms();
        }
        catch
        {
            if (changed)
            {
                controller.enabled = false;
                player.transform.SetPositionAndRotation(oldPosition, oldRotation);
                Physics.SyncTransforms();
                movement.RestablecerEstadoTrasCarga();
                rescue.RestablecerEstadoTrasCarga();
                controller.enabled = true;
            }
            throw;
        }
    }

    private static void ValidateGround(GameObject player, CharacterController controller,
        RescateVacioJugador rescue, Vector3 destination)
    {
        Physics.SyncTransforms();

        float heightScale = Mathf.Abs(player.transform.lossyScale.y);
        float feetY = destination.y +
            (controller.center.y - controller.height * 0.5f) * heightScale;
        Vector3 start = new Vector3(destination.x, feetY + 0.75f, destination.z);
        RaycastHit[] hits = Physics.RaycastAll(start, Vector3.down, 1.5f,
            rescue.capasSuelo.value, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.transform == player.transform ||
                hit.collider.transform.IsChildOf(player.transform))
                continue;

            // El suelo debe estar aproximadamente bajo los pies. Un piso
            // superior o muy inferior no valida la posicion almacenada.
            if (Mathf.Abs(hit.point.y - feetY) > 0.35f)
                continue;

            if (rescue.respetarPendienteMaxima &&
                Vector3.Angle(hit.normal, Vector3.up) > controller.slopeLimit)
                continue;

            return;
        }

        throw new InvalidOperationException(
            "No se ha encontrado suelo valido bajo la posicion guardada. " +
            "No se ha movido al jugador; comprueba si faltan construcciones o terreno.");
    }
}
