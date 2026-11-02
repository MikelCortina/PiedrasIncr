
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reconstruye varios edificios de una partida.
///
/// Valida todas las conexiones antes de crear edificios.
/// No gasta monedas ni consume inventario.
/// No duplica IDs existentes.
///
/// Por ahora solo admite edificios sin curvatura.
/// Las conexiones se restauran mediante otro adaptador.
/// </summary>
public static class PlacedBuildingsBatchSpawnAdapter
{
#if UNITY_EDITOR
    // PASO 82:
    // Permite provocar errores despues de crear
    // cada prefab durante las pruebas del Editor.
    public static Action<int> FalloDiagnostico;
#endif

    public static List<EdificioConstruido> CrearORecuperarTodos(
        SistemaConstruccion sistema,
        GameSaveData partida)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException(
                "La reconstruccion requiere estar en Play."
            );
        }

        if (sistema == null)
            throw new ArgumentNullException(nameof(sistema));

        if (partida == null)
            throw new ArgumentNullException(nameof(partida));

        // ======================================
        // 1. LEER Y VALIDAR LA PARTIDA
        // ======================================

        PlacedBuildingsSaveData datos =
            PlacedBuildingsSaveAdapter.Read(partida);

        // ======================================
        // 2. VALIDAR TODAS LAS CONEXIONES
        // ======================================

        PlacedBuildingsConnectionGraphValidator.Analizar(
            partida
        );

        // ======================================
        // 3. VALIDAR TODOS LOS PREFABS PRIMERO
        // ======================================

        foreach (PlacedBuildingSaveData edificio
            in datos.edificios)
        {
            SistemaConstruccion.TipoEdificio tipo =
                (SistemaConstruccion.TipoEdificio)
                Enum.Parse(
                    typeof(SistemaConstruccion.TipoEdificio),
                    edificio.tipo
                );

            if (edificio.curvaturaRampa != 0f)
            {
                throw new InvalidOperationException(
                    "Todavia no se pueden reconstruir " +
                    "rampas curvadas. ID=" +
                    edificio.idPersistente
                );
            }

            ComprobarPrefab(
                sistema,
                tipo,
                edificio.variantePrefab
            );
        }

        // ======================================
        // 4. REGISTRAR LOS IDs YA EXISTENTES
        // ======================================

        HashSet<string> idsExistentes =
            new HashSet<string>(StringComparer.Ordinal);

        Scene escena = SceneManager.GetActiveScene();

        EdificioConstruido[] edificiosActuales =
            UnityEngine.Object.FindObjectsByType<
                EdificioConstruido>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (EdificioConstruido edificio
            in edificiosActuales)
        {
            if (edificio == null ||
                edificio.gameObject.scene != escena)
            {
                continue;
            }

            if (string.IsNullOrEmpty(
                edificio.IdPersistente))
            {
                continue;
            }

            if (!idsExistentes.Add(
                edificio.IdPersistente))
            {
                throw new InvalidOperationException(
                    "La escena ya contiene un ID duplicado: " +
                    edificio.IdPersistente
                );
            }
        }

        // ======================================
        // 5. RECONSTRUIR LOS EDIFICIOS
        // ======================================

        List<EdificioConstruido> resultado =
            new List<EdificioConstruido>();

        List<GameObject> nuevos =
            new List<GameObject>();

        try
        {
            foreach (PlacedBuildingSaveData edificioGuardado
                in datos.edificios)
            {
                bool existiaAntes =
                    idsExistentes.Contains(
                        edificioGuardado.idPersistente
                    );

                EdificioConstruido edificio =
                    PlacedBuildingsSpawnAdapter.CrearORecuperar(
                        sistema,
                        edificioGuardado
                    );

                if (edificio == null)
                {
                    throw new InvalidOperationException(
                        "No se pudo recuperar el edificio " +
                        edificioGuardado.idPersistente
                    );
                }

                resultado.Add(edificio);

                // Registrar solamente los objetos nuevos.
                // Nunca eliminar edificios preexistentes.
                if (!existiaAntes)
                {
                    nuevos.Add(edificio.gameObject);

#if UNITY_EDITOR
                    // PASO 82:
                    // El prefab ya esta registrado
                    // para su limpieza si hay un fallo.
                    FalloDiagnostico?.Invoke(nuevos.Count);
#endif
                }
            }

            return resultado;
        }
        catch
        {
            // ======================================
            // 6. RECUPERACION ANTE FALLOS
            // ======================================

            // Eliminar solo los prefabs creados
            // durante esta operacion.
            foreach (GameObject objeto in nuevos)
            {
                if (objeto != null)
                {
                    objeto.SetActive(false);

                    UnityEngine.Object.Destroy(
                        objeto
                    );
                }
            }

            throw;
        }
    }

    // ======================================
    // COMPROBAR PREFAB EN EL CATALOGO
    // ======================================

    private static void ComprobarPrefab(
        SistemaConstruccion sistema,
        SistemaConstruccion.TipoEdificio tipo,
        int variante)
    {
        if (sistema.edificios == null)
        {
            throw new InvalidOperationException(
                "No existe el catalogo de edificios."
            );
        }

        int coincidencias = 0;

        foreach (SistemaConstruccion.InfoEdificio info
            in sistema.edificios)
        {
            if (info.tipo != tipo)
                continue;

            if (info.prefabsReales == null ||
                variante < 0 ||
                variante >= info.prefabsReales.Length ||
                info.prefabsReales[variante] == null)
            {
                continue;
            }

            coincidencias++;
        }

        if (coincidencias != 1)
        {
            throw new InvalidOperationException(
                "Se esperaba un unico prefab para " +
                tipo +
                ", variante=" + variante +
                ". Coincidencias=" + coincidencias
            );
        }
    }
}
