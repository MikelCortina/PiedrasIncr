
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrBuildingConnectionsTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Analizar conexiones edificios (Play)"
    )]
    public static void Ejecutar()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[GUARDADO] Entra en Play para analizar conexiones."
            );
            return;
        }

        try
        {
            Scene escena = SceneManager.GetActiveScene();

            EdificioConstruido[] encontrados =
                UnityEngine.Object.FindObjectsByType<
                    EdificioConstruido>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            Dictionary<string, EdificioConstruido> edificios =
                new Dictionary<string, EdificioConstruido>();

            // Registrar los edificios reales de la escena.
            foreach (EdificioConstruido edificio in encontrados)
            {
                if (edificio == null ||
                    edificio.gameObject.scene != escena ||
                    edificio.GetComponent<
                        EfectoBloopDestruccion>() != null)
                {
                    continue;
                }

                Guid guid;

                if (!Guid.TryParseExact(
                    edificio.IdPersistente, "N", out guid))
                {
                    throw new InvalidOperationException(
                        "Edificio sin identidad valida: " +
                        edificio.name
                    );
                }

                if (edificios.ContainsKey(edificio.IdPersistente))
                {
                    throw new InvalidOperationException(
                        "ID duplicado: " + edificio.IdPersistente
                    );
                }

                edificios.Add(edificio.IdPersistente, edificio);
            }

            int enlacesInternos = 0;
            int enlacesExternos = 0;
            int dependencias = 0;
            int conectoresBloqueados = 0;

            foreach (EdificioConstruido edificio in edificios.Values)
            {
                // ====================================
                // CONECTOR UTILIZADO POR ESTE EDIFICIO
                // ====================================

                Collider conector = edificio.conectorUsado;

                if (conector != null)
                {
                    EdificioConstruido propietario =
                        conector.GetComponentInParent<
                            EdificioConstruido>();

                    if (propietario != null &&
                        edificios.ContainsKey(
                            propietario.IdPersistente))
                    {
                        enlacesInternos++;

                        Debug.Log(
                            "[GUARDADO] CONEXION INTERNA: " +
                            edificio.tipo +
                            " (" + edificio.IdPersistente + ")" +
                            " -> " + propietario.tipo +
                            " (" + propietario.IdPersistente + ")" +
                            ", conector=" +
                            RutaLocal(
                                conector.transform,
                                propietario.transform
                            ) +
                            ", habilitado=" + conector.enabled
                        );
                    }
                    else
                    {
                        enlacesExternos++;

                        Debug.Log(
                            "[GUARDADO] CONEXION EXTERNA: " +
                            edificio.tipo +
                            " (" + edificio.IdPersistente + ")" +
                            ", conector=" + conector.name +
                            ", objeto=" + conector.gameObject.name
                        );
                    }
                }

                // ====================================
                // EDIFICIOS DEPENDIENTES
                // ====================================

                if (edificio.edificiosDependientes != null)
                {
                    foreach (GameObject dependiente
                        in edificio.edificiosDependientes)
                    {
                        if (dependiente == null)
                        {
                            Debug.LogWarning(
                                "[GUARDADO] Dependencia desaparecida en " +
                                edificio.IdPersistente
                            );
                            continue;
                        }

                        dependencias++;

                        EdificioConstruido hijo =
                            dependiente.GetComponent<
                                EdificioConstruido>();

                        Debug.Log(
                            "[GUARDADO] DEPENDENCIA: " +
                            edificio.tipo +
                            " -> " +
                            (hijo != null
                                ? hijo.tipo + " (" +
                                  hijo.IdPersistente + ")"
                                : dependiente.name +
                                  " (sin EdificioConstruido)")
                        );
                    }
                }

                // ====================================
                // CONECTORES DE HIJOS BLOQUEADOS
                // ====================================

                if (edificio.conectoresHijosBloqueados != null)
                {
                    foreach (Collider bloqueado
                        in edificio.conectoresHijosBloqueados)
                    {
                        if (bloqueado == null)
                            continue;

                        conectoresBloqueados++;

                        Debug.Log(
                            "[GUARDADO] CONECTOR HIJO: " +
                            edificio.tipo +
                            ", nombre=" + bloqueado.name +
                            ", habilitado=" + bloqueado.enabled
                        );
                    }
                }
            }

            if (enlacesInternos + enlacesExternos == 0)
            {
                Debug.LogWarning(
                    "[GUARDADO] No se han encontrado conexiones. " +
                    "Coloca una pared enganchada al conector " +
                    "de una rampa y repite el analisis."
                );
                return;
            }

            Debug.Log(
                "[GUARDADO] CONEXIONES EDIFICIOS ANALIZADAS: " +
                "edificios=" + edificios.Count +
                ", enlacesInternos=" + enlacesInternos +
                ", enlacesExternos=" + enlacesExternos +
                ", dependencias=" + dependencias +
                ", conectoresHijosBloqueados=" +
                conectoresBloqueados +
                ". Sin modificaciones en la escena."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] ANALISIS CONEXIONES FALLIDO: " +
                ex
            );
        }
    }

    // Identifica un conector por su ruta en el prefab.
    // Incluye indices para distinguir hijos con igual nombre.
    private static string RutaLocal(
        Transform conector,
        Transform raiz)
    {
        List<string> partes = new List<string>();

        Transform actual = conector;

        while (actual != null && actual != raiz)
        {
            partes.Add(
                actual.name + "[" +
                actual.GetSiblingIndex() + "]"
            );

            actual = actual.parent;
        }

        if (actual != raiz)
            return "(fuera de la jerarquia)";

        partes.Reverse();

        return partes.Count == 0
            ? "(raiz)"
            : string.Join("/", partes.ToArray());
    }
}

#endif
