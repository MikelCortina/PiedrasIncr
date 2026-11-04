
#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrPlacedBuildingsSlotsTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar 3 ranuras edificios (Play)"
    )]
    public static void Ejecutar()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[GUARDADO] Entra en Play para ejecutar la prueba."
            );
            return;
        }

        string carpeta = Path.Combine(
            Application.persistentDataPath,
            "PiedrasIncr",
            "DeveloperTests",
            "PlacedBuildingsSlots_" +
                Guid.NewGuid().ToString("N")
        );

        try
        {
            SistemaConstruccion sistema =
                SistemaConstruccion.Instancia;

            Comprobar(
                sistema != null,
                "No se encuentra SistemaConstruccion."
            );

            Cartera[] carteras =
                UnityEngine.Object.FindObjectsByType<Cartera>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            Comprobar(
                carteras.Length == 1,
                "Se esperaba una cartera. Encontradas: " +
                carteras.Length
            );

            Cartera cartera = carteras[0];
            int monedasOriginales = cartera.Monedas;

            // CAPTURAR EDIFICIOS REALES

            GameSaveData captura = new GameSaveData();

            PlacedBuildingsSaveAdapter.Capture(
                sistema, captura
            );

            PlacedBuildingsSaveData originales =
                PlacedBuildingsSaveAdapter.Read(captura);

            Comprobar(
                originales.edificios.Count >= 2,
                "Coloca al menos 2 edificios. Encontrados: " +
                originales.edificios.Count
            );

            // TRES RANURAS CON DATOS DIFERENTES
            // Ranura 1: cero edificios
            // Ranura 2: un edificio
            // Ranura 3: todos los edificios

            int[] cantidades =
            {
                0,
                1,
                originales.edificios.Count
            };

            int[] monedasPrueba =
            {
                100,
                200,
                300
            };

            GameSaveData[] esperadas =
                new GameSaveData[3];

            SaveStorage storage =
                new SaveStorage(carpeta);

            // GUARDAR LAS TRES RANURAS

            for (int slot = 1; slot <= 3; slot++)
            {
                GameSaveData partida = new GameSaveData
                {
                    sceneName =
                        SceneManager.GetActiveScene().name,

                    hasPlacedBuildingsData = true,

                    placedBuildings =
                        new PlacedBuildingsSaveData()
                };

                // Monedas ficticias solo en los archivos
                // de diagnostico.
                partida.economy.coins =
                    monedasPrueba[slot - 1];

                for (int i = 0;
                    i < cantidades[slot - 1];
                    i++)
                {
                    PlacedBuildingSaveData copia =
                        JsonUtility.FromJson<
                            PlacedBuildingSaveData>(
                            JsonUtility.ToJson(
                                originales.edificios[i]
                            )
                        );

                    partida.placedBuildings.edificios.Add(
                        copia
                    );
                }

                PlacedBuildingsSaveAdapter.Validate(
                    partida.placedBuildings
                );

                storage.Save(slot, partida);

                esperadas[slot - 1] = partida;

                Debug.Log(
                    "[GUARDADO] EDIFICIOS RANURA " +
                    slot + " GUARDADA: " +
                    cantidades[slot - 1] +
                    " edificios, monedas=" +
                    monedasPrueba[slot - 1]
                );
            }

            // LEER LAS TRES RANURAS

            for (int slot = 1; slot <= 3; slot++)
            {
                GameSaveData cargada;
                bool desdeRespaldo;

                Comprobar(
                    storage.TryLoad(
                        slot,
                        out cargada,
                        out desdeRespaldo
                    ),
                    "No se puede leer la ranura " + slot
                );

                Comprobar(
                    !desdeRespaldo,
                    "Respaldo inesperado en ranura " + slot
                );

                Comprobar(
                    cargada.slotIndex == slot,
                    "Indice de ranura incorrecto."
                );

                Comprobar(
                    cargada.economy.coins ==
                        monedasPrueba[slot - 1],
                    "Monedas mezcladas en ranura " + slot
                );

                PlacedBuildingsSaveData datos =
                    PlacedBuildingsSaveAdapter.Read(
                        cargada
                    );

                Comprobar(
                    datos.edificios.Count ==
                        cantidades[slot - 1],
                    "Cantidad incorrecta en ranura " +
                    slot
                );

                // Comparar todos los datos serializados.
                string jsonEsperado =
                    JsonUtility.ToJson(
                        esperadas[slot - 1].placedBuildings
                    );

                string jsonLeido =
                    JsonUtility.ToJson(datos);

                Comprobar(
                    jsonEsperado == jsonLeido,
                    "Los datos de edificios no coinciden " +
                    "en ranura " + slot
                );

                Debug.Log(
                    "[GUARDADO] EDIFICIOS RANURA " +
                    slot + " VERIFICADA: " +
                    datos.edificios.Count +
                    " edificios, datos correctos."
                );
            }

            // COMPROBAR QUE NO SE MODIFICO LA ESCENA

            GameSaveData segundaCaptura =
                new GameSaveData();

            PlacedBuildingsSaveAdapter.Capture(
                sistema, segundaCaptura
            );

            string antes =
                JsonUtility.ToJson(originales);

            string despues =
                JsonUtility.ToJson(
                    PlacedBuildingsSaveAdapter.Read(
                        segundaCaptura
                    )
                );

            Comprobar(
                antes == despues,
                "La escena ha cambiado durante la prueba."
            );

            Comprobar(
                cartera.Monedas == monedasOriginales,
                "Se han modificado las monedas reales."
            );

            Debug.Log(
                "[GUARDADO] TRES RANURAS EDIFICIOS SUPERADAS: " +
                "0, 1 y " + originales.edificios.Count +
                " edificios independientes, " +
                "datos completos verificados, " +
                "monedas reales intactas=" +
                monedasOriginales +
                " y escena sin modificaciones."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] TRES RANURAS EDIFICIOS FALLIDAS: " +
                ex
            );
        }
        finally
        {
            try
            {
                if (Directory.Exists(carpeta))
                    Directory.Delete(carpeta, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "[GUARDADO] No se pudo borrar la carpeta " +
                    "temporal: " + ex.Message
                );
            }
        }
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
