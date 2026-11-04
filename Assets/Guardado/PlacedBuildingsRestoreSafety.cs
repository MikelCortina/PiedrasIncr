
using System;
using System.Collections.Generic;

/// <summary>
/// Resultado de la comprobacion de seguridad.
///
/// No modifica ningun objeto de la escena.
/// </summary>
public sealed class PlacedBuildingsRestoreSafetyReport
{
    public PlacedBuildingsRestorePlan plan;

    public List<string> bloqueos =
        new List<string>();

    public bool SinCambiosPendientes =>
        plan != null &&
        plan.porCrear == 0 &&
        plan.sobrantes == 0 &&
        plan.conflictos == 0;

    public bool RequiereTrabajoAdicional =>
        bloqueos.Count > 0;

    public string Resumen()
    {
        if (plan == null)
            return "No existe un plan de restauracion.";

        return
            "coincidentes=" + plan.coincidentes +
            ", porCrear=" + plan.porCrear +
            ", sobrantes=" + plan.sobrantes +
            ", conflictos=" + plan.conflictos +
            ", rampasCurvadas=" + plan.rampasCurvadas +
            ", bloqueos=" + bloqueos.Count;
    }
}

/// <summary>
/// Proteccion previa a la restauracion completa.
///
/// No construye, mueve ni elimina edificios.
/// No modifica monedas ni inventario.
/// </summary>
public static class PlacedBuildingsRestoreSafety
{
    public static PlacedBuildingsRestoreSafetyReport Evaluar(
        SistemaConstruccion sistema,
        GameSaveData partida)
    {
        if (sistema == null)
            throw new ArgumentNullException(nameof(sistema));

        if (partida == null)
            throw new ArgumentNullException(nameof(partida));

        // El planificador ya comprueba:
        // - Escena correcta.
        // - Identificadores.
        // - Prefabs.
        // - Diferencias entre escena y guardado.
        PlacedBuildingsRestorePlan plan =
            PlacedBuildingsRestorePlanner.Analizar(
                sistema,
                partida
            );

        PlacedBuildingsRestoreSafetyReport resultado =
            new PlacedBuildingsRestoreSafetyReport
            {
                plan = plan
            };

        // 1. NO ELIMINAR EDIFICIOS SOBRANTES.
        if (plan.sobrantes > 0)
        {
            resultado.bloqueos.Add(
                "Hay " + plan.sobrantes +
                " edificios en la escena que no aparecen " +
                "en el guardado. No se eliminaran automaticamente."
            );
        }

        // 2. NO SOBRESCRIBIR EDIFICIOS CONFLICTIVOS.
        if (plan.conflictos > 0)
        {
            resultado.bloqueos.Add(
                "Hay " + plan.conflictos +
                " edificios con el mismo ID pero " +
                "datos diferentes. No se sobrescribiran."
            );
        }

        // 3. NO RECONSTRUIR TODAVIA RAMPAS CURVADAS.
        if (plan.rampasCurvadas > 0)
        {
            resultado.bloqueos.Add(
                "La partida contiene " +
                plan.rampasCurvadas +
                " rampas curvadas. Falta restaurar " +
                "correctamente su geometria."
            );
        }

        // 4. EVITAR RECONSTRUCCIONES INCOMPLETAS.
        if (plan.porCrear > 0)
        {
            resultado.bloqueos.Add(
                "Faltan " + plan.porCrear +
                " edificios por reconstruir. " +
                "Antes de aplicar una partida completa " +
                "debemos guardar y recuperar sus conexiones."
            );
        }

        return resultado;
    }

    // ==========================================
    // BLOQUEAR UNA RESTAURACION NO PREPARADA
    // ==========================================

    public static void ExigirRestauracionSinCambios(
        SistemaConstruccion sistema,
        GameSaveData partida)
    {
        PlacedBuildingsRestoreSafetyReport informe =
            Evaluar(sistema, partida);

        if (informe.RequiereTrabajoAdicional)
        {
            throw new InvalidOperationException(
                "Restauracion de edificios bloqueada: " +
                string.Join(
                    " | ",
                    informe.bloqueos.ToArray()
                )
            );
        }

        if (!informe.SinCambiosPendientes)
        {
            throw new InvalidOperationException(
                "El estado de la escena no coincide " +
                "con la partida guardada."
            );
        }
    }
}
