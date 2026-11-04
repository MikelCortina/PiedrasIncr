
#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PiedrasIncrFileIntegrityTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Paso 83 - Probar integridad de archivos"
    )]
    public static void Ejecutar()
    {
        string carpeta = Path.Combine(
            Path.GetTempPath(),
            "PiedrasIncr_Paso83_" +
            Guid.NewGuid().ToString("N")
        );

        try
        {
            SaveStorage storage = new SaveStorage(carpeta);

            // =====================================
            // 1. CREAR TRES PARTIDAS COMPLETAS
            // =====================================

            GameSaveData partida1 = CrearPartida(100);
            GameSaveData partida2 = CrearPartida(200);
            GameSaveData partida3 = CrearPartida(300);

            storage.Save(1, partida1);
            storage.Save(2, partida2);
            storage.Save(3, partida3);

            CompararRanura(storage, 1, partida1);
            CompararRanura(storage, 2, partida2);
            CompararRanura(storage, 3, partida3);

            Debug.Log(
                "[GUARDADO] PASO 83 RANURAS OK: " +
                "tres partidas completas e independientes."
            );

            // =====================================
            // 2. SOBRESCRITURA CON RESPALDO
            // =====================================

            GameSaveData actualizada = Copiar(partida1);

            actualizada.economy.coins = 150;
            actualizada.player.positionX = 15f;

            actualizada.toolUpgrades.nivelRadioTorbellino = 2;

            actualizada.processor.monedasConstruccionAportadas =
                6;

            actualizada.buildingInventory.articulos[0].unidades +=
                3;

            storage.Save(1, actualizada);

            string principal1 =
                storage.GetPrimaryPathForDiagnostics(1);

            string respaldo1 = principal1 + ".bak";

            Comprobar(
                File.Exists(respaldo1),
                "No se ha creado el respaldo."
            );

            CompararRanura(storage, 1, actualizada);

            Comprobar(
                partida1.saveId == actualizada.saveId &&
                partida1.createdUtc == actualizada.createdUtc,
                "La sobrescritura ha cambiado " +
                "la identidad de la partida."
            );

            string respaldoOriginal =
                File.ReadAllText(respaldo1);

            Debug.Log(
                "[GUARDADO] PASO 83 RESPALDO OK: " +
                "sobrescritura completa con respaldo previo."
            );

            // =====================================
            // 3. DETECTAR CHECKSUM ALTERADO
            // =====================================

            CorromperChecksum(principal1);

            GameSaveData recuperada;
            bool desdeRespaldo;

            Comprobar(
                storage.TryLoad(
                    1,
                    out recuperada,
                    out desdeRespaldo
                ),
                "No se ha recuperado la partida anterior."
            );

            Comprobar(
                desdeRespaldo,
                "No se ha detectado el archivo principal dañado."
            );

            CompararDatos(
                recuperada,
                partida1,
                "El respaldo no coincide con la partida anterior."
            );

            Debug.Log(
                "[GUARDADO] PASO 83 CHECKSUM OK: " +
                "archivo alterado rechazado y " +
                "respaldo completo recuperado."
            );

            // =====================================
            // 4. REPARAR EL PRINCIPAL DAÑADO
            // =====================================

            GameSaveData reparada = Copiar(actualizada);

            reparada.economy.coins = 175;
            reparada.player.positionX = 17f;

            storage.Save(1, reparada);

            CompararRanura(storage, 1, reparada);

            Comprobar(
                File.ReadAllText(respaldo1) ==
                    respaldoOriginal,
                "La reparacion ha sobrescrito " +
                "el respaldo valido."
            );

            Debug.Log(
                "[GUARDADO] PASO 83 RECUPERACION OK: " +
                "principal reparado sin destruir " +
                "el respaldo valido."
            );

            // =====================================
            // 5. NO UTILIZAR ARCHIVOS .TMP
            // =====================================

            string principal2 =
                storage.GetPrimaryPathForDiagnostics(2);

            string temporal2 = principal2 + ".tmp";

            File.WriteAllText(
                temporal2,
                "ARCHIVO TEMPORAL INCOMPLETO"
            );

            CompararRanura(storage, 2, partida2);

            Comprobar(
                File.Exists(temporal2),
                "El archivo temporal de prueba desaparecio."
            );

            Debug.Log(
                "[GUARDADO] PASO 83 TEMPORAL OK: " +
                "los archivos .tmp no sustituyen " +
                "partidas validas."
            );

            // =====================================
            // 6. RECHAZAR ARCHIVO DE OTRA RANURA
            // =====================================

            string principal3 =
                storage.GetPrimaryPathForDiagnostics(3);

            string original2 = File.ReadAllText(principal2);

            // Copiar el archivo valido de la ranura 3
            // encima del principal de la ranura 2.
            File.Copy(principal3, principal2, true);

            GameSaveData datoIncorrecto;
            bool respaldoIncorrecto;

            Comprobar(
                !storage.TryLoad(
                    2,
                    out datoIncorrecto,
                    out respaldoIncorrecto
                ),
                "Se ha aceptado un archivo " +
                "perteneciente a otra ranura."
            );

            // Recuperar el archivo original de ranura 2.
            File.WriteAllText(principal2, original2);

            CompararRanura(storage, 2, partida2);
            CompararRanura(storage, 3, partida3);

            Debug.Log(
                "[GUARDADO] PASO 83 AISLAMIENTO OK: " +
                "archivos de otras ranuras rechazados."
            );

            // =====================================
            // 7. BORRAR SOLO UNA RANURA
            // =====================================

            storage.DeleteSlot(2);

            Comprobar(
                !storage.HasValidSave(2) &&
                !File.Exists(principal2) &&
                !File.Exists(temporal2) &&
                !File.Exists(principal2 + ".bak"),
                "No se han eliminado todos " +
                "los archivos de la ranura 2."
            );

            CompararRanura(storage, 1, reparada);
            CompararRanura(storage, 3, partida3);

            Debug.Log(
                "[GUARDADO] PASO 83 BORRADO OK: " +
                "ranura 2 eliminada sin afectar a 1 y 3."
            );

            // =====================================
            // 8. PRINCIPAL Y RESPALDO CORRUPTOS
            // =====================================

            CorromperChecksum(principal1);
            CorromperChecksum(respaldo1);

            Comprobar(
                !storage.HasValidSave(1),
                "Se ha aceptado una partida " +
                "con ambos archivos dañados."
            );

            string principalCorrupto =
                File.ReadAllText(principal1);

            string respaldoCorrupto =
                File.ReadAllText(respaldo1);

            bool escrituraRechazada = false;

            try
            {
                storage.Save(1, CrearPartida(999));
            }
            catch (IOException)
            {
                escrituraRechazada = true;
            }

            Comprobar(
                escrituraRechazada,
                "El sistema ha permitido sobrescribir " +
                "una ranura sin respaldo valido."
            );

            Comprobar(
                File.ReadAllText(principal1) ==
                    principalCorrupto &&
                File.ReadAllText(respaldo1) ==
                    respaldoCorrupto,
                "Los archivos dañados han sido " +
                "sobrescritos inesperadamente."
            );

            CompararRanura(storage, 3, partida3);

            Debug.Log(
                "[GUARDADO] PASO 83 PROTECCION OK: " +
                "principal y respaldo corruptos, " +
                "sobrescritura bloqueada."
            );

            // =====================================
            // 9. RESULTADO FINAL
            // =====================================

            Debug.Log(
                "[GUARDADO] PASO 83 SUPERADO: " +
                "integridad SHA-256, respaldo, " +
                "recuperacion, temporales, " +
                "independencia de tres ranuras " +
                "y proteccion contra corrupcion " +
                "doble verificados."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PASO 83 FALLIDO: " + ex
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
                    "[GUARDADO] No se pudo limpiar " +
                    "la carpeta temporal: " + ex.Message
                );
            }
        }
    }

    // =====================================
    // CREAR PARTIDA DE DIAGNOSTICO COMPLETA
    // =====================================

    private static GameSaveData CrearPartida(int monedas)
    {
        GameSaveData partida = new GameSaveData
        {
            sceneName = "Piedra",
            totalPlaySeconds = monedas * 2.5,

            economy = new EconomySaveData
            {
                coins = monedas
            },

            player = new PlayerSaveData
            {
                hasSafePosition = true,
                positionX = monedas / 10f,
                positionY = 5f,
                positionZ = -2f,
                yawDegrees = 90f
            },

            hasEquipmentUnlocksData = true,

            equipmentUnlocks = new EquipmentUnlocksSaveData
            {
                armaLanzadoraDesbloqueada = true,
                torbellinoDesbloqueado = false,
                martilloDesbloqueado = true
            },

            hasToolUpgradesData = true,

            toolUpgrades = new ToolUpgradesSaveData
            {
                nivelRadioTorbellino = 1,
                nivelAlcanceTorbellino = 1,
                nivelMovilidadTorbellino = 1,
                imanDesbloqueado = false,
                nivelAlcanceIman = 1
            },

            hasProcessorData = true,

            processor = new ProcessorSaveData
            {
                desbloqueada = false,
                nivelProcesado = 1,
                monedasConstruccionAportadas = 5,
                monedasConstruccionNecesarias = 10,
                monedasMejoraAportadas = 0,
                precioSiguienteMejora = 0
            },

            hasBuildingInventoryData = true,
            buildingInventory = new BuildingInventorySaveData(),

            hasPlacedBuildingsData = true,
            placedBuildings = new PlacedBuildingsSaveData()
        };

        foreach (SistemaConstruccion.TipoEdificio tipo
            in Enum.GetValues(
                typeof(SistemaConstruccion.TipoEdificio)))
        {
            partida.buildingInventory.articulos.Add(
                new BuildingInventoryItemSaveData
                {
                    tipo = tipo.ToString(),
                    unidades = monedas / 100
                }
            );
        }

        return partida;
    }

    // =====================================
    // ALTERAR EL CHECKSUM SIN CAMBIAR DATOS
    // =====================================

    private static void CorromperChecksum(string ruta)
    {
        string contenido = File.ReadAllText(ruta);

        int clave = contenido.IndexOf(
            "\"sha256\"",
            StringComparison.Ordinal
        );

        Comprobar(
            clave >= 0,
            "No se encuentra sha256 en: " + ruta
        );

        int separador = contenido.IndexOf(':', clave);

        int comillaInicial =
            contenido.IndexOf('"', separador + 1);

        Comprobar(
            separador >= 0 &&
            comillaInicial >= 0 &&
            comillaInicial + 1 < contenido.Length,
            "Formato checksum inesperado."
        );

        char[] caracteres = contenido.ToCharArray();

        int posicion = comillaInicial + 1;

        caracteres[posicion] =
            caracteres[posicion] == 'a' ? 'b' : 'a';

        File.WriteAllText(
            ruta,
            new string(caracteres)
        );
    }

    // =====================================
    // COMPARACIONES
    // =====================================

    private static void CompararRanura(
        SaveStorage storage,
        int ranura,
        GameSaveData esperado)
    {
        GameSaveData leido;
        bool respaldo;

        Comprobar(
            storage.TryLoad(
                ranura,
                out leido,
                out respaldo
            ),
            "No se puede leer la ranura " + ranura
        );

        Comprobar(
            !respaldo,
            "Respaldo inesperado en ranura " + ranura
        );

        Comprobar(
            leido.slotIndex == ranura,
            "Indice de ranura incorrecto."
        );

        CompararDatos(
            leido,
            esperado,
            "Datos diferentes en ranura " + ranura
        );
    }

    private static void CompararDatos(
        GameSaveData actual,
        GameSaveData esperado,
        string mensaje)
    {
        Comprobar(
            JsonUtility.ToJson(actual) ==
                JsonUtility.ToJson(esperado),
            mensaje
        );
    }

    private static GameSaveData Copiar(GameSaveData datos)
    {
        return JsonUtility.FromJson<GameSaveData>(
            JsonUtility.ToJson(datos)
        );
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
