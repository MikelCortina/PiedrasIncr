
using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Paso 92:
/// Verifica la independencia de las tres ranuras.
/// Utiliza una carpeta temporal aislada.
/// No modifica las partidas reales.
/// </summary>
public static class PiedrasIncrSaveSlotIsolationTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Paso 92 - Probar independencia")]
    public static void Ejecutar()
    {
        string carpeta = Path.Combine(
            Path.GetTempPath(),
            "PiedrasIncr_Paso92_" +
            Guid.NewGuid().ToString("N")
        );

        try
        {
            SaveStorage storage = new SaveStorage(carpeta);

            // =====================================
            // 1. CREAR TRES PARTIDAS INDEPENDIENTES
            // =====================================

            GameSaveData partida1 = CrearPartida(100);
            GameSaveData partida2 = CrearPartida(200);
            GameSaveData partida3 = CrearPartida(300);

            storage.Save(1, partida1);
            storage.Save(2, partida2);
            storage.Save(3, partida3);

            // Generar tambien los respaldos.
            partida1.economy.coins = 101;
            partida2.economy.coins = 201;
            partida3.economy.coins = 301;

            storage.Save(1, partida1);
            storage.Save(2, partida2);
            storage.Save(3, partida3);

            string identidadAnterior2 = partida2.saveId;

            Comprobar(
                File.Exists(Ruta(storage, 1) + ".bak") &&
                File.Exists(Ruta(storage, 2) + ".bak") &&
                File.Exists(Ruta(storage, 3) + ".bak"),
                "No se generaron los tres respaldos."
            );

            // Capturar una huella exacta de los
            // archivos de las ranuras 1 y 3.
            string huella1 = HuellaRanura(storage, 1);
            string huella3 = HuellaRanura(storage, 3);

            Debug.Log(
                "[GUARDADO] PASO 92 CREACION OK: " +
                "tres ranuras y tres respaldos."
            );

            // =====================================
            // 2. SOBRESCRIBIR SOLO RANURA 2
            // =====================================

            partida2.economy.coins = 999;
            storage.Save(2, partida2);

            ComprobarCarga(storage, 2, 999);

            VerificarSinCambios(
                storage, huella1, huella3
            );

            Debug.Log(
                "[GUARDADO] PASO 92 SOBRESCRITURA OK: " +
                "ranuras 1 y 3 intactas."
            );

            // =====================================
            // 3. ELIMINAR SOLO RANURA 2
            // =====================================

            storage.DeleteSlot(2);

            Comprobar(
                !File.Exists(Ruta(storage, 2)) &&
                !File.Exists(Ruta(storage, 2) + ".bak") &&
                !File.Exists(Ruta(storage, 2) + ".tmp"),
                "La ranura 2 conserva archivos."
            );

            VerificarSinCambios(
                storage, huella1, huella3
            );

            Debug.Log(
                "[GUARDADO] PASO 92 BORRADO OK: " +
                "solo la ranura 2 eliminada."
            );

            // =====================================
            // 4. REUTILIZAR RANURA 2
            // =====================================

            GameSaveData nuevaPartida2 =
                CrearPartida(500);

            storage.Save(2, nuevaPartida2);

            ComprobarCarga(storage, 2, 500);

            Comprobar(
                nuevaPartida2.saveId != identidadAnterior2,
                "La nueva partida reutiliza la identidad antigua."
            );

            VerificarSinCambios(
                storage, huella1, huella3
            );

            // =====================================
            // 5. COMPROBAR LAS OTRAS PARTIDAS
            // =====================================

            ComprobarCarga(storage, 1, 101);
            ComprobarCarga(storage, 3, 301);

            Debug.Log(
                "[GUARDADO] PASO 92 SUPERADO: " +
                "creacion, sobrescritura, borrado " +
                "y reutilizacion independientes. " +
                "Ranuras 1 y 3 sin modificaciones."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PASO 92 FALLIDO: " + ex
            );
        }
        finally
        {
            // Limpiar exclusivamente la carpeta
            // temporal creada por esta prueba.
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

    private static GameSaveData CrearPartida(int monedas)
    {
        return new GameSaveData
        {
            sceneName = "Piedra",
            economy = new EconomySaveData
            {
                coins = monedas
            }
        };
    }

    private static void ComprobarCarga(
        SaveStorage storage,
        int ranura,
        int monedasEsperadas)
    {
        GameSaveData partida;
        bool desdeRespaldo;

        Comprobar(
            storage.TryLoad(
                ranura,
                out partida,
                out desdeRespaldo
            ),
            "No se pudo cargar la ranura " + ranura
        );

        Comprobar(
            !desdeRespaldo &&
            partida.economy.coins == monedasEsperadas,
            "Contenido incorrecto en ranura " + ranura
        );
    }

    private static void VerificarSinCambios(
        SaveStorage storage,
        string huella1,
        string huella3)
    {
        Comprobar(
            HuellaRanura(storage, 1) == huella1,
            "Se modificaron archivos de la ranura 1."
        );

        Comprobar(
            HuellaRanura(storage, 3) == huella3,
            "Se modificaron archivos de la ranura 3."
        );
    }

    private static string Ruta(
        SaveStorage storage,
        int ranura)
    {
        return storage.GetPrimaryPathForDiagnostics(
            ranura
        );
    }

    private static string HuellaRanura(
        SaveStorage storage,
        int ranura)
    {
        string principal = Ruta(storage, ranura);

        return HuellaArchivo(principal) + "|" +
               HuellaArchivo(principal + ".bak") + "|" +
               HuellaArchivo(principal + ".tmp");
    }

    private static string HuellaArchivo(string ruta)
    {
        if (!File.Exists(ruta))
            return "AUSENTE";

        using (SHA256 sha = SHA256.Create())
        {
            return Convert.ToBase64String(
                sha.ComputeHash(File.ReadAllBytes(ruta))
            );
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
