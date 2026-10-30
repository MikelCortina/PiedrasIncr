using System;
using UnityEngine;

/// <summary>
/// Solo CAPTURA y VALIDA la ultima pose segura actual del jugador.
/// No modifica Transform, CharacterController ni el rescate del vacio.
/// El restablecimiento de posicion llegara al integrar la carga global.
/// </summary>
public static class PlayerSaveAdapter
{
    public static void Capture(GameObject player, GameSaveData save)
    {
        if (player == null)
            throw new ArgumentNullException(nameof(player));
        if (save == null)
            throw new ArgumentNullException(nameof(save));

        CharacterController controller = player.GetComponent<CharacterController>();
        MovimientoPersonaje movement = player.GetComponent<MovimientoPersonaje>();
        RescateVacioJugador rescue = player.GetComponent<RescateVacioJugador>();

        if (controller == null || movement == null)
            throw new InvalidOperationException("El jugador necesita CharacterController y MovimientoPersonaje.");
        if (!controller.enabled || !controller.isGrounded || !movement.SueloDetectado)
            throw new InvalidOperationException("Ponte sobre suelo firme antes de capturar la posicion.");
        if (rescue != null && rescue.RescateActivo)
            throw new InvalidOperationException("No se puede guardar la posicion durante un rescate.");

        Collider floor = movement.ColliderSueloDetectado;
        if (floor == null)
            throw new InvalidOperationException("No se ha encontrado el suelo confirmado del jugador.");

        if (rescue != null)
        {
            if ((rescue.capasSuelo.value & (1 << floor.gameObject.layer)) == 0)
                throw new InvalidOperationException("El suelo actual no es valido para el sistema de rescate.");
            if (rescue.respetarPendienteMaxima &&
                Vector3.Angle(movement.NormalSueloDetectado, Vector3.up) > controller.slopeLimit)
                throw new InvalidOperationException("La pendiente no es segura para guardar la posicion.");
        }

        Vector3 pos = player.transform.position;
        float yaw = player.transform.eulerAngles.y;
        var snapshot = new PlayerSaveData
        {
            hasSafePosition = true,
            positionX = pos.x,
            positionY = pos.y,
            positionZ = pos.z,
            yawDegrees = yaw
        };
        Validate(snapshot);
        save.player = snapshot;
    }

    public static void Validate(PlayerSaveData snapshot)
    {
        if (snapshot == null || !snapshot.hasSafePosition)
            throw new InvalidOperationException("Esta partida todavia no tiene una posicion segura guardada.");
        if (!Finite(snapshot.positionX) || !Finite(snapshot.positionY) ||
            !Finite(snapshot.positionZ) || !Finite(snapshot.yawDegrees) ||
            snapshot.yawDegrees < 0f || snapshot.yawDegrees > 360f)
            throw new InvalidOperationException("La posicion u orientacion del jugador es invalida.");
    }

    public static Vector3 GetSavedPosition(PlayerSaveData snapshot)
    {
        Validate(snapshot);
        return new Vector3(snapshot.positionX, snapshot.positionY, snapshot.positionZ);
    }

    private static bool Finite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
