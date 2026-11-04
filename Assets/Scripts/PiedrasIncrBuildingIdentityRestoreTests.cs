
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PiedrasIncrBuildingIdentityRestoreTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar restaurar identidad edificios (aislado)"
    )]
    public static void Ejecutar()
    {
        List<GameObject> objetosTemporales =
            new List<GameObject>();

        HashSet<string> idsOriginales =
            new HashSet<string>(StringComparer.Ordinal);

        try
        {
            SistemaConstruccion.TipoEdificio[] tipos =
                (SistemaConstruccion.TipoEdificio[])
                Enum.GetValues(
                    typeof(SistemaConstruccion.TipoEdificio)
                );

            // Probar los cinco tipos de edificios.
            foreach (SistemaConstruccion.TipoEdificio tipo in tipos)
            {
                int variante =
                    tipo == SistemaConstruccion.TipoEdificio.Pared
                        ? 5
                        : 0;

                float curvatura =
                    tipo == SistemaConstruccion.TipoEdificio.Rampa
                        ? 1.25f
                        : 0f;

                // EDIFICIO ORIGINAL

                EdificioConstruido original =
                    CrearEdificioTemporal(
                        "Original_" + tipo,
                        objetosTemporales
                    );

                original.tipo = tipo;

                original.RegistrarComoNuevo(
                    variante,
                    curvatura
                );

                string idOriginal =
                    original.IdPersistente;

                Comprobar(
                    idsOriginales.Add(idOriginal),
                    "Se ha repetido un ID original."
                );

                // SIMULAR DATOS GUARDADOS EN JSON

                PlacedBuildingSaveData datos =
                    new PlacedBuildingSaveData
                    {
                        idPersistente = idOriginal,
                        tipo = tipo.ToString(),
                        variantePrefab = variante,
                        curvaturaRampa = curvatura
                    };

                string json = JsonUtility.ToJson(datos);

                PlacedBuildingSaveData cargados =
                    JsonUtility.FromJson<PlacedBuildingSaveData>(
                        json
                    );

                // EDIFICIO RECONSTRUIDO
                // Solo restauramos su identidad.
                // No instanciamos prefabs reales.

                EdificioConstruido restaurado =
                    CrearEdificioTemporal(
                        "Restaurado_" + tipo,
                        objetosTemporales
                    );

                SistemaConstruccion.TipoEdificio tipoGuardado =
                    (SistemaConstruccion.TipoEdificio)
                    Enum.Parse(
                        typeof(SistemaConstruccion.TipoEdificio),
                        cargados.tipo
                    );

                restaurado.RestaurarIdentidadGuardada(
                    cargados.idPersistente,
                    tipoGuardado,
                    cargados.variantePrefab,
                    cargados.curvaturaRampa
                );

                // COMPROBAR RESULTADOS

                Comprobar(
                    restaurado.IdPersistente == idOriginal,
                    "El ID no coincide para " + tipo
                );

                Comprobar(
                    restaurado.tipo == tipo,
                    "Tipo incorrecto para " + tipo
                );

                Comprobar(
                    restaurado.VariantePrefab == variante,
                    "Variante incorrecta para " + tipo
                );

                Comprobar(
                    Mathf.Approximately(
                        restaurado.CurvaturaRampa,
                        curvatura
                    ),
                    "Curvatura incorrecta para " + tipo
                );

                // IMPEDIR SOBRESCRIBIR EL ID

                bool sobrescrituraRechazada = false;

                try
                {
                    restaurado.RestaurarIdentidadGuardada(
                        Guid.NewGuid().ToString("N"),
                        tipo,
                        variante,
                        curvatura
                    );
                }
                catch (InvalidOperationException)
                {
                    sobrescrituraRechazada = true;
                }

                Comprobar(
                    sobrescrituraRechazada,
                    "Se ha permitido sobrescribir el ID."
                );

                Comprobar(
                    restaurado.IdPersistente == idOriginal,
                    "Se ha modificado la identidad original."
                );

                Debug.Log(
                    "[GUARDADO] IDENTIDAD RESTAURADA OK: " +
                    "tipo=" + tipo +
                    ", variante=" + variante +
                    ", curvatura=" + curvatura +
                    ", id=" + restaurado.IdPersistente
                );
            }

            // COMPROBAR ID INVALIDO

            EdificioConstruido invalido =
                CrearEdificioTemporal(
                    "IdentidadInvalida",
                    objetosTemporales
                );

            bool idRechazado = false;

            try
            {
                invalido.RestaurarIdentidadGuardada(
                    "ID_INVALIDO",
                    SistemaConstruccion.TipoEdificio.Rampa,
                    0,
                    0f
                );
            }
            catch (ArgumentException)
            {
                idRechazado = true;
            }

            Comprobar(
                idRechazado,
                "Se ha aceptado un ID invalido."
            );

            Comprobar(
                string.IsNullOrEmpty(invalido.IdPersistente),
                "Un intento fallido ha registrado un ID."
            );

            // COMPROBAR CURVATURA INVALIDA

            bool curvaturaRechazada = false;

            try
            {
                invalido.RestaurarIdentidadGuardada(
                    Guid.NewGuid().ToString("N"),
                    SistemaConstruccion.TipoEdificio.Pared,
                    0,
                    1.5f
                );
            }
            catch (ArgumentException)
            {
                curvaturaRechazada = true;
            }

            Comprobar(
                curvaturaRechazada,
                "Se ha aceptado una pared con curvatura."
            );

            Comprobar(
                string.IsNullOrEmpty(invalido.IdPersistente),
                "Un registro invalido ha cambiado la identidad."
            );

            Debug.Log(
                "[GUARDADO] RESTAURACION IDENTIDAD EDIFICIOS SUPERADA: " +
                "5 tipos, IDs originales conservados, variantes, " +
                "curvaturas, JSON y proteccion contra sobrescritura."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] RESTAURACION IDENTIDAD EDIFICIOS FALLIDA: " +
                ex
            );
        }
        finally
        {
            foreach (GameObject objeto in objetosTemporales)
            {
                if (objeto != null)
                    UnityEngine.Object.DestroyImmediate(objeto);
            }
        }
    }

    private static EdificioConstruido CrearEdificioTemporal(
        string nombre,
        List<GameObject> objetos)
    {
        GameObject objeto = new GameObject(
            "__TEST_" + nombre
        );

        objetos.Add(objeto);

        objeto.SetActive(false);
        objeto.hideFlags = HideFlags.HideAndDontSave;

        return objeto.AddComponent<EdificioConstruido>();
    }

    private static void Comprobar(
        bool condicion,
        string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException(mensaje);
    }
}

#endif
