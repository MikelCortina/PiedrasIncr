
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Resultado de la reconstruccion conjunta.
/// </summary>
public sealed class PlacedBuildingsRestoreResult
{
    public List<EdificioConstruido> edificios =
        new List<EdificioConstruido>();

    public int conexionesRestauradas;
}

/// <summary>
/// Reconstruye edificios nuevos y restaura
/// sus conexiones en una unica operacion.
///
/// Esta primera version:
/// - Valida el grafo antes de crear prefabs.
/// - Rechaza IDs existentes en la escena.
/// - Reconstruye edificios sin curvatura.
/// - Restaura sus conexiones.
/// - Retira los nuevos objetos si falla.
///
/// No elimina ni modifica edificios originales.
/// No consume inventario ni monedas.
/// </summary>
public static class PlacedBuildingsRestoreCoordinator
{
    public static PlacedBuildingsRestoreResult RestaurarNuevos(
        SistemaConstruccion sistema,
        GameSaveData partida)
    {
        // ======================================
        // 1. COMPROBACIONES INICIALES
        // ======================================

        if (!Application.isPlaying)
        {
            throw new InvalidOperationException(
                "La restauracion requiere estar en Play."
            );
        }

        if (sistema == null)
            throw new ArgumentNullException(nameof(sistema));

        if (partida == null)
            throw new ArgumentNullException(nameof(partida));

        Scene escena = SceneManager.GetActiveScene();

        if (partida.sceneName != escena.name)
        {
            throw new InvalidOperationException(
                "La partida pertenece a otra escena: " +
                partida.sceneName
            );
        }

        PlacedBuildingsSaveData datos =
            PlacedBuildingsSaveAdapter.Read(partida);

        // ======================================
        // 2. VALIDAR EL GRAFO COMPLETO
        // ======================================

        PlacedBuildingsConnectionGraphReport informe =
            PlacedBuildingsConnectionGraphValidator.Analizar(
                partida
            );

        // ======================================
        // 3. PROTEGER LOS EDIFICIOS EXISTENTES
        // ======================================
        // Esta version solamente admite la creacion
        // de edificios que no existen en la escena.
        //
        // Si encuentra un ID ya presente, bloquea
        // la operacion antes de crear cualquier objeto.

        HashSet<string> idsGuardados =
            new HashSet<string>(StringComparer.Ordinal);

        foreach (PlacedBuildingSaveData edificio
            in datos.edificios)
        {
            idsGuardados.Add(edificio.idPersistente);
        }

        EdificioConstruido[] existentes =
            UnityEngine.Object.FindObjectsByType<
                EdificioConstruido>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (EdificioConstruido edificio in existentes)
        {
            if (edificio == null ||
                edificio.gameObject.scene != escena)
            {
                continue;
            }

            if (idsGuardados.Contains(
                edificio.IdPersistente))
            {
                throw new InvalidOperationException(
                    "La restauracion contiene un edificio " +
                    "que ya existe en la escena. ID=" +
                    edificio.IdPersistente +
                    ". Esta version no modifica " +
                    "construcciones preexistentes."
                );
            }
        }

        // ======================================
        // 4. RECONSTRUIR LOS PREFABS
        // ======================================

        List<EdificioConstruido> nuevos = null;

        try
        {
            // Este adaptador valida previamente
            // los prefabs y las curvaturas.
            //
            // Si falla mientras construye el lote,
            // elimina sus propias creaciones.

            nuevos =
                PlacedBuildingsBatchSpawnAdapter
                    .CrearORecuperarTodos(
                        sistema,
                        partida
                    );

            if (nuevos.Count != datos.edificios.Count)
            {
                throw new InvalidOperationException(
                    "La cantidad de edificios " +
                    "reconstruidos no coincide " +
                    "con el guardado."
                );
            }

            // ======================================
            // 5. RESTAURAR LAS CONEXIONES
            // ======================================

            int conexiones =
                PlacedBuildingsConnectionsRestoreAdapter
                    .Restaurar(partida);

            if (conexiones != informe.conexiones)
            {
                throw new InvalidOperationException(
                    "La cantidad de conexiones restauradas " +
                    "no coincide con el grafo validado."
                );
            }

            // ======================================
            // 6. DEVOLVER EL RESULTADO
            // ======================================

            PlacedBuildingsRestoreResult resultado =
                new PlacedBuildingsRestoreResult();

            resultado.edificios.AddRange(nuevos);

            resultado.conexionesRestauradas =
                conexiones;

            return resultado;
        }
        catch
        {
            // ======================================
            // 7. RETIRAR CREACIONES SI FALLA
            // ======================================
            //
            // Solo se eliminan los prefabs creados
            // durante esta operacion.
            //
            // Los edificios originales no se tocan.
            //
            // Si la reconstruccion por lotes fallo
            // antes de devolver su lista, ese adaptador
            // ya gestiona la limpieza de su propio lote.

            if (nuevos != null)
            {
                foreach (EdificioConstruido edificio
                    in nuevos)
                {
                    if (edificio == null)
                        continue;

                    GameObject objeto =
                        edificio.gameObject;

                    if (objeto == null)
                        continue;

                    objeto.SetActive(false);

                    UnityEngine.Object.Destroy(objeto);
                }
            }

            throw;
        }
    }
}
