
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PiedrasIncrBuildingIdsTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar IDs edificios (aislado)"
    )]
    public static void Ejecutar()
    {
        List<GameObject> objetosPrueba =
            new List<GameObject>();

        HashSet<string> identificadores =
            new HashSet<string>(StringComparer.Ordinal);

        try
        {
            SistemaConstruccion.TipoEdificio[] tipos =
                (SistemaConstruccion.TipoEdificio[])
                Enum.GetValues(
                    typeof(SistemaConstruccion.TipoEdificio)
                );

            // Crear 30 edificios temporales.
            for (int i = 0; i < 30; i++)
            {
                GameObject objeto = new GameObject(
                    "__TEST_EDIFICIO_" + i
                );

                objetosPrueba.Add(objeto);

                objeto.SetActive(false);
                objeto.hideFlags =
                    HideFlags.HideAndDontSave;

                EdificioConstruido edificio =
                    objeto.AddComponent<EdificioConstruido>();

                SistemaConstruccion.TipoEdificio tipo =
                    tipos[i % tipos.Length];

                int variante = i % 3;

                float curvatura =
                    tipo == SistemaConstruccion.TipoEdificio.Rampa
                        ? 1.25f
                        : 0f;

                edificio.tipo = tipo;

                edificio.RegistrarComoNuevo(
                    variante,
                    curvatura
                );

                // ID generado correctamente.
                Comprobar(
                    !string.IsNullOrEmpty(
                        edificio.IdPersistente
                    ),
                    "Un edificio no tiene ID."
                );

                Guid guid;

                Comprobar(
                    Guid.TryParseExact(
                        edificio.IdPersistente,
                        "N",
                        out guid
                    ),
                    "El identificador no es un GUID valido."
                );

                // No puede repetirse.
                Comprobar(
                    identificadores.Add(
                        edificio.IdPersistente
                    ),
                    "Se ha generado un ID duplicado."
                );

                // Debe mantener sus datos.
                Comprobar(
                    edificio.tipo == tipo,
                    "Tipo de edificio incorrecto."
                );

                Comprobar(
                    edificio.VariantePrefab == variante,
                    "Variante de prefab incorrecta."
                );

                Comprobar(
                    Mathf.Approximately(
                        edificio.CurvaturaRampa,
                        curvatura
                    ),
                    "Curvatura incorrecta."
                );

                // Intentar registrar otra vez
                // debe producir un error.
                string idOriginal =
                    edificio.IdPersistente;

                bool rechazado = false;

                try
                {
                    edificio.RegistrarComoNuevo(
                        variante,
                        curvatura
                    );
                }
                catch (InvalidOperationException)
                {
                    rechazado = true;
                }

                Comprobar(
                    rechazado,
                    "Se ha permitido registrar dos veces."
                );

                Comprobar(
                    edificio.IdPersistente == idOriginal,
                    "Se ha modificado un ID existente."
                );
            }

            // Probar que se rechazan valores invalidos.
            GameObject objetoInvalido = new GameObject(
                "__TEST_EDIFICIO_INVALIDO__"
            );

            objetosPrueba.Add(objetoInvalido);

            objetoInvalido.SetActive(false);
            objetoInvalido.hideFlags =
                HideFlags.HideAndDontSave;

            EdificioConstruido invalido =
                objetoInvalido.AddComponent<EdificioConstruido>();

            bool varianteRechazada = false;

            try
            {
                invalido.RegistrarComoNuevo(-1, 0f);
            }
            catch (ArgumentOutOfRangeException)
            {
                varianteRechazada = true;
            }

            Comprobar(
                varianteRechazada,
                "Se ha aceptado una variante negativa."
            );

            bool curvaturaRechazada = false;

            try
            {
                invalido.RegistrarComoNuevo(
                    0,
                    float.NaN
                );
            }
            catch (ArgumentException)
            {
                curvaturaRechazada = true;
            }

            Comprobar(
                curvaturaRechazada,
                "Se ha aceptado una curvatura invalida."
            );

            Comprobar(
                string.IsNullOrEmpty(
                    invalido.IdPersistente
                ),
                "Un registro fallido ha generado un ID."
            );

            Debug.Log(
                "[GUARDADO] IDS EDIFICIOS SUPERADOS: " +
                "30 identificadores unicos, " +
                "5 tipos, variantes, curvaturas, " +
                "proteccion contra duplicados " +
                "y validacion de valores."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] IDS EDIFICIOS FALLIDOS: " +
                ex
            );

            throw;
        }
        finally
        {
            // Eliminar todos los objetos temporales.
            foreach (GameObject objeto in objetosPrueba)
            {
                if (objeto != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        objeto
                    );
                }
            }
        }
    }

    private static void Comprobar(
        bool condicion,
        string mensaje)
    {
        if (!condicion)
        {
            throw new Exception(mensaje);
        }
    }
}

#endif
