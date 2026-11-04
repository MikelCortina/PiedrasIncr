
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrPlacedBuildingsTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar edificios colocados (Play)"
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
            "PlacedBuildings_" + Guid.NewGuid().ToString("N")
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
                "Se esperaba una Cartera. Encontradas: " +
                carteras.Length
            );

            Cartera cartera = carteras[0];
            int monedasOriginales = cartera.Monedas;

            // 1. CAPTURAR EDIFICIOS REALES

            GameSaveData partida = new GameSaveData
            {
                sceneName = SceneManager.GetActiveScene().name
            };

            EconomySaveAdapter.Capture(cartera, partida);

            PlacedBuildingsSaveAdapter.Capture(
                sistema, partida
            );

            PlacedBuildingsSaveData originales =
                PlacedBuildingsSaveAdapter.Read(partida);

            int cantidad = originales.edificios.Count;

            Comprobar(
                cantidad >= 2,
                "Coloca al menos 2 edificios antes de probar. " +
                "Encontrados: " + cantidad
            );

            Debug.Log(
                "[GUARDADO] EDIFICIOS GUARDADOS TEST 1: " +
                cantidad + " edificios, monedas=" +
                partida.economy.coins + "."
            );

            // 2. ESCRIBIR EN ARCHIVO DE DIAGNOSTICO

            SaveStorage storage = new SaveStorage(carpeta);
            storage.Save(1, partida);

            GameSaveData cargada;
            bool desdeRespaldo;

            Comprobar(
                storage.TryLoad(
                    1,
                    out cargada,
                    out desdeRespaldo
                ),
                "No se pudo leer el archivo."
            );

            Comprobar(
                !desdeRespaldo,
                "Se ha utilizado un respaldo inesperado."
            );

            Comprobar(
                cargada.sceneName == partida.sceneName,
                "El archivo pertenece a otra escena."
            );

            PlacedBuildingsSaveData leidos =
                PlacedBuildingsSaveAdapter.Read(cargada);

            ComprobarIguales(originales, leidos);

            Debug.Log(
                "[GUARDADO] EDIFICIOS LECTURA TEST 1: " +
                leidos.edificios.Count +
                " edificios, datos coincidentes."
            );

            // 3. PROBAR IDENTIFICADORES DUPLICADOS

            ProbarRechazo(
                originales,
                datos =>
                {
                    datos.edificios[1].idPersistente =
                        datos.edificios[0].idPersistente;
                },
                "identificadores duplicados"
            );

            // 4. PROBAR VARIANTE INVALIDA

            ProbarRechazo(
                originales,
                datos =>
                {
                    datos.edificios[0].variantePrefab = -1;
                },
                "variante negativa"
            );

            // 5. PROBAR POSICION INVALIDA

            ProbarRechazo(
                originales,
                datos =>
                {
                    datos.edificios[0].positionX = float.NaN;
                },
                "posicion invalida"
            );

            // 6. PROBAR ROTACION INVALIDA

            ProbarRechazo(
                originales,
                datos =>
                {
                    var edificio = datos.edificios[0];

                    edificio.rotationX = 0f;
                    edificio.rotationY = 0f;
                    edificio.rotationZ = 0f;
                    edificio.rotationW = 0f;
                },
                "rotacion invalida"
            );

            // 7. PROBAR ESCALA INVALIDA

            ProbarRechazo(
                originales,
                datos =>
                {
                    datos.edificios[0].scaleX = 0f;
                },
                "escala invalida"
            );

            // 8. PARTIDA SIN EDIFICIOS

            PlacedBuildingsSaveAdapter.Validate(
                new PlacedBuildingsSaveData()
            );

            // 9. PARTIDA ANTIGUA SIN ESTE MODULO

            bool antiguaRechazada = false;

            try
            {
                PlacedBuildingsSaveAdapter.Read(
                    new GameSaveData()
                );
            }
            catch (InvalidOperationException)
            {
                antiguaRechazada = true;
            }

            Comprobar(
                antiguaRechazada,
                "Se han inventado edificios en una partida antigua."
            );

            // 10. COMPROBAR QUE LA ESCENA NO HA CAMBIADO

            GameSaveData segundaCaptura = new GameSaveData();

            PlacedBuildingsSaveAdapter.Capture(
                sistema, segundaCaptura
            );

            ComprobarIguales(
                originales,
                PlacedBuildingsSaveAdapter.Read(
                    segundaCaptura
                )
            );

            Comprobar(
                cartera.Monedas == monedasOriginales &&
                cargada.economy.coins == monedasOriginales,
                "Las monedas han cambiado."
            );

            Debug.Log(
                "[GUARDADO] CAPTURA EDIFICIOS SUPERADA: " +
                cantidad + " edificios, IDs, tipos, variantes, " +
                "posiciones, rotaciones, escalas y curvaturas " +
                "correctas; datos invalidos rechazados, " +
                "monedas intactas y escena sin modificaciones."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] CAPTURA EDIFICIOS FALLIDA: " +
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
                    "de diagnostico: " + ex.Message
                );
            }
        }
    }

    private static void ProbarRechazo(
        PlacedBuildingsSaveData originales,
        Action<PlacedBuildingsSaveData> modificar,
        string descripcion)
    {
        // Trabajar sobre una copia, nunca sobre
        // los datos originales.
        PlacedBuildingsSaveData copia =
            JsonUtility.FromJson<PlacedBuildingsSaveData>(
                JsonUtility.ToJson(originales)
            );

        modificar(copia);

        bool rechazado = false;

        try
        {
            PlacedBuildingsSaveAdapter.Validate(copia);
        }
        catch (InvalidOperationException)
        {
            rechazado = true;
        }

        Comprobar(
            rechazado,
            "Se han aceptado datos invalidos: " +
            descripcion
        );
    }

    private static void ComprobarIguales(
        PlacedBuildingsSaveData esperado,
        PlacedBuildingsSaveData actual)
    {
        Comprobar(
            esperado.edificios.Count == actual.edificios.Count,
            "La cantidad de edificios no coincide."
        );

        Dictionary<string, PlacedBuildingSaveData> mapa =
            new Dictionary<string, PlacedBuildingSaveData>();

        foreach (PlacedBuildingSaveData edificio
            in actual.edificios)
        {
            mapa.Add(edificio.idPersistente, edificio);
        }

        foreach (PlacedBuildingSaveData a
            in esperado.edificios)
        {
            PlacedBuildingSaveData b;

            Comprobar(
                mapa.TryGetValue(a.idPersistente, out b),
                "Falta el edificio " + a.idPersistente
            );

            Comprobar(
                a.tipo == b.tipo &&
                a.variantePrefab == b.variantePrefab,
                "Tipo o variante incorrectos: " +
                a.idPersistente
            );

            ComprobarFloat(
                a.curvaturaRampa, b.curvaturaRampa,
                "curvatura"
            );

            ComprobarFloat(a.positionX, b.positionX, "positionX");
            ComprobarFloat(a.positionY, b.positionY, "positionY");
            ComprobarFloat(a.positionZ, b.positionZ, "positionZ");

            ComprobarFloat(a.rotationX, b.rotationX, "rotationX");
            ComprobarFloat(a.rotationY, b.rotationY, "rotationY");
            ComprobarFloat(a.rotationZ, b.rotationZ, "rotationZ");
            ComprobarFloat(a.rotationW, b.rotationW, "rotationW");

            ComprobarFloat(a.scaleX, b.scaleX, "scaleX");
            ComprobarFloat(a.scaleY, b.scaleY, "scaleY");
            ComprobarFloat(a.scaleZ, b.scaleZ, "scaleZ");
        }
    }

    private static void ComprobarFloat(
        float esperado,
        float actual,
        string campo)
    {
        Comprobar(
            Mathf.Abs(esperado - actual) <= 0.0001f,
            "Diferencia en " + campo +
            ": esperado=" + esperado +
            ", actual=" + actual
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
