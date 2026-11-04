
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Resultado del analisis previo a restaurar edificios.
/// </summary>
public sealed class PlacedBuildingsRestorePlan
{
    public int coincidentes;
    public int porCrear;
    public int sobrantes;
    public int conflictos;
    public int rampasCurvadas;

    public int TotalGuardados =>
        coincidentes + porCrear + conflictos;
}

/// <summary>
/// Compara los edificios del guardado con la escena.
///
/// No crea, destruye ni modifica objetos.
/// No toca monedas ni inventario.
/// </summary>
public static class PlacedBuildingsRestorePlanner
{
    public static PlacedBuildingsRestorePlan Analizar(
        SistemaConstruccion sistema,
        GameSaveData partida)
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException(
                "El analisis requiere estar en Play."
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
                "El guardado pertenece a otra escena: " +
                partida.sceneName
            );
        }

        PlacedBuildingsSaveData datos =
            PlacedBuildingsSaveAdapter.Read(partida);

        var resultado = new PlacedBuildingsRestorePlan();

        // ======================================
        // 1. INVENTARIAR EDIFICIOS ACTUALES
        // ======================================

        var actuales =
            new Dictionary<string, EdificioConstruido>(
                StringComparer.Ordinal
            );

        EdificioConstruido[] encontrados =
            UnityEngine.Object.FindObjectsByType<
                EdificioConstruido>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (EdificioConstruido edificio in encontrados)
        {
            if (edificio == null ||
                edificio.gameObject.scene != escena)
            {
                continue;
            }

            if (edificio.GetComponent<
                EfectoBloopDestruccion>() != null)
            {
                throw new InvalidOperationException(
                    "Hay un edificio en proceso de demolicion: " +
                    edificio.name
                );
            }

            // Ignorar componentes Bloop desactivados
            // que ya vienen incluidos en algunos prefabs.
            EfectoBloop[] efectos =
                edificio.GetComponents<EfectoBloop>();

            foreach (EfectoBloop efecto in efectos)
            {
                if (efecto != null &&
                    efecto.isActiveAndEnabled)
                {
                    throw new InvalidOperationException(
                        "Hay un edificio apareciendo: " +
                        edificio.name
                    );
                }
            }

            if (string.IsNullOrEmpty(
                edificio.IdPersistente))
            {
                throw new InvalidOperationException(
                    "Edificio sin identidad: " +
                    edificio.name
                );
            }

            if (!actuales.TryAdd(
                edificio.IdPersistente,
                edificio))
            {
                throw new InvalidOperationException(
                    "ID duplicado en la escena: " +
                    edificio.IdPersistente
                );
            }
        }

        // ======================================
        // 2. COMPARAR CON LOS DATOS GUARDADOS
        // ======================================

        HashSet<string> idsGuardados =
            new HashSet<string>(StringComparer.Ordinal);

        foreach (PlacedBuildingSaveData guardado
            in datos.edificios)
        {
            idsGuardados.Add(guardado.idPersistente);

            SistemaConstruccion.TipoEdificio tipo =
                (SistemaConstruccion.TipoEdificio)
                Enum.Parse(
                    typeof(SistemaConstruccion.TipoEdificio),
                    guardado.tipo
                );

            ComprobarPrefab(
                sistema,
                tipo,
                guardado.variantePrefab
            );

            if (guardado.curvaturaRampa != 0f)
                resultado.rampasCurvadas++;

            EdificioConstruido actual;

            if (!actuales.TryGetValue(
                guardado.idPersistente,
                out actual))
            {
                resultado.porCrear++;
                continue;
            }

            if (Coincide(actual, guardado, tipo))
                resultado.coincidentes++;
            else
                resultado.conflictos++;
        }

        // ======================================
        // 3. DETECTAR EDIFICIOS SOBRANTES
        // ======================================

        foreach (string id in actuales.Keys)
        {
            if (!idsGuardados.Contains(id))
                resultado.sobrantes++;
        }

        return resultado;
    }

    // ======================================
    // COMPROBAR TRANSFORMACION E IDENTIDAD
    // ======================================

    private static bool Coincide(
        EdificioConstruido actual,
        PlacedBuildingSaveData guardado,
        SistemaConstruccion.TipoEdificio tipo)
    {
        if (actual.tipo != tipo ||
            actual.VariantePrefab != guardado.variantePrefab ||
            !Mathf.Approximately(
                actual.CurvaturaRampa,
                guardado.curvaturaRampa))
        {
            return false;
        }

        Vector3 posicion = new Vector3(
            guardado.positionX,
            guardado.positionY,
            guardado.positionZ
        );

        Quaternion rotacion = new Quaternion(
            guardado.rotationX,
            guardado.rotationY,
            guardado.rotationZ,
            guardado.rotationW
        );

        Vector3 escala = new Vector3(
            guardado.scaleX,
            guardado.scaleY,
            guardado.scaleZ
        );

        Transform t = actual.transform;

        return
            Vector3.Distance(
                t.position, posicion) <= 0.001f &&
            Quaternion.Angle(
                t.rotation, rotacion) <= 0.1f &&
            Vector3.Distance(
                t.localScale, escala) <= 0.001f;
    }

    // ======================================
    // COMPROBAR CATALOGO DE PREFABS
    // ======================================

    private static void ComprobarPrefab(
        SistemaConstruccion sistema,
        SistemaConstruccion.TipoEdificio tipo,
        int variante)
    {
        int encontrados = 0;

        if (sistema.edificios == null)
        {
            throw new InvalidOperationException(
                "El catalogo de edificios no existe."
            );
        }

        foreach (SistemaConstruccion.InfoEdificio info
            in sistema.edificios)
        {
            if (info.tipo != tipo ||
                info.prefabsReales == null ||
                variante < 0 ||
                variante >= info.prefabsReales.Length)
            {
                continue;
            }

            if (info.prefabsReales[variante] != null)
                encontrados++;
        }

        if (encontrados != 1)
        {
            throw new InvalidOperationException(
                "Prefab inexistente o ambiguo: " +
                tipo + ", variante=" + variante +
                ", encontrados=" + encontrados
            );
        }
    }
}
