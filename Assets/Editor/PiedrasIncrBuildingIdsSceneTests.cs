
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PiedrasIncrBuildingIdsSceneTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar IDs edificios en escena (Play)"
    )]
    public static void Ejecutar()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[GUARDADO] Entra en Play para hacer la prueba."
            );
            return;
        }

        try
        {
            SistemaConstruccion sistema =
                SistemaConstruccion.Instancia;

            if (sistema == null)
            {
                throw new InvalidOperationException(
                    "No se encuentra SistemaConstruccion."
                );
            }

            if (sistema.edificios == null ||
                sistema.edificios.Length == 0)
            {
                throw new InvalidOperationException(
                    "El catalogo de edificios esta vacio."
                );
            }

            EdificioConstruido[] construidos =
                UnityEngine.Object.FindObjectsByType<
                    EdificioConstruido>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            if (construidos.Length < 2)
            {
                throw new InvalidOperationException(
                    "Coloca al menos 2 edificios " +
                    "durante Play antes de ejecutar la prueba. " +
                    "Encontrados: " + construidos.Length
                );
            }

            HashSet<string> identificadores =
                new HashSet<string>(StringComparer.Ordinal);

            int rampas = 0;
            int otros = 0;

            foreach (EdificioConstruido edificio in construidos)
            {
                if (edificio == null)
                    continue;

                // IDENTIFICADOR PERMANENTE

                if (string.IsNullOrEmpty(
                    edificio.IdPersistente))
                {
                    throw new InvalidOperationException(
                        "Edificio sin ID: " + edificio.name
                    );
                }

                Guid guid;

                if (!Guid.TryParseExact(
                    edificio.IdPersistente, "N", out guid))
                {
                    throw new InvalidOperationException(
                        "ID invalido: " + edificio.name
                    );
                }

                if (!identificadores.Add(
                    edificio.IdPersistente))
                {
                    throw new InvalidOperationException(
                        "ID duplicado: " +
                        edificio.IdPersistente
                    );
                }

                // COMPROBAR TIPO Y VARIANTE

                bool encontrado = false;

                foreach (SistemaConstruccion.InfoEdificio info
                    in sistema.edificios)
                {
                    if (info.tipo != edificio.tipo)
                        continue;

                    encontrado = true;

                    if (info.prefabsReales == null ||
                        edificio.VariantePrefab < 0 ||
                        edificio.VariantePrefab >=
                            info.prefabsReales.Length ||
                        info.prefabsReales[
                            edificio.VariantePrefab] == null)
                    {
                        throw new InvalidOperationException(
                            "Variante de prefab invalida: " +
                            edificio.name
                        );
                    }

                    break;
                }

                if (!encontrado)
                {
                    throw new InvalidOperationException(
                        "Tipo no encontrado en el catalogo: " +
                        edificio.tipo
                    );
                }

                // COMPROBAR CURVATURA

                float curva = edificio.CurvaturaRampa;

                if (float.IsNaN(curva) ||
                    float.IsInfinity(curva))
                {
                    throw new InvalidOperationException(
                        "Curvatura invalida: " +
                        edificio.name
                    );
                }

                if (edificio.tipo ==
                    SistemaConstruccion.TipoEdificio.Rampa)
                {
                    rampas++;
                }
                else
                {
                    otros++;

                    if (!Mathf.Approximately(curva, 0f))
                    {
                        throw new InvalidOperationException(
                            "Un edificio que no es rampa " +
                            "tiene curvatura: " +
                            edificio.name
                        );
                    }
                }

                Debug.Log(
                    "[GUARDADO] EDIFICIO REAL OK: " +
                    "tipo=" + edificio.tipo +
                    ", variante=" + edificio.VariantePrefab +
                    ", curvatura=" + curva +
                    ", id=" + edificio.IdPersistente
                );
            }

            Debug.Log(
                "[GUARDADO] IDS ESCENA SUPERADOS: " +
                identificadores.Count +
                " edificios reales, IDs unicos, " +
                "variantes validas, rampas=" + rampas +
                ", otros=" + otros + "."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] IDS ESCENA FALLIDOS: " +
                ex
            );
        }
    }
}

#endif
