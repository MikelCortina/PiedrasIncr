
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrSlotBridgeTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar puente ranuras (Play)"
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

        try
        {
            // ======================================
            // 1. LOCALIZAR SISTEMAS
            // ======================================

            SistemaConstruccion sistema =
                SistemaConstruccion.Instancia;

            Comprobar(
                sistema != null,
                "No se encuentra SistemaConstruccion."
            );

            InventarioEdificios inventario =
                sistema.inventarioEdificios;

            Comprobar(
                inventario != null,
                "No se encuentra InventarioEdificios."
            );

            Cartera cartera = inventario.cartera;

            if (cartera == null)
            {
                cartera =
                    inventario.GetComponentInParent<Cartera>();
            }

            Comprobar(
                cartera != null,
                "No se encuentra Cartera."
            );

            int monedasAntes = cartera.Monedas;

            var inventarioAntes =
                CapturarInventario(inventario);

            string nombreEscena =
                SceneManager.GetActiveScene().name;

            // ======================================
            // 2. CAPTURAR LA ESCENA ORIGINAL
            // ======================================

            GameSaveData original = new GameSaveData
            {
                sceneName = nombreEscena
            };

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                original
            );

            PlacedBuildingsConnectionGraphReport grafoOriginal =
                PlacedBuildingsConnectionGraphValidator
                    .Analizar(original);

            Comprobar(
                grafoOriginal.edificios >= 2,
                "Necesitas al menos dos edificios construidos."
            );

            Comprobar(
                grafoOriginal.conexiones >= 1,
                "Necesitas al menos una conexion real, " +
                "por ejemplo una pared sobre RailRampa."
            );

            string edificiosAntes =
                JsonUtility.ToJson(original.placedBuildings);

            int cantidadAntes =
                ContarEdificios(nombreEscena);

            // ======================================
            // 3. CREAR DATOS GENERALES FICTICIOS
            // ======================================
            // Este objeto solo existe en memoria.
            // No modifica los datos reales del juego.

            GameSaveData partida = new GameSaveData
            {
                slotIndex = 3,
                saveId = Guid.NewGuid().ToString("N"),
                createdUtc = "2026-01-01T10:00:00Z",
                savedUtc = "2026-01-01T10:05:00Z",
                totalPlaySeconds = 1234.5,

                // El puente debe actualizar
                // el nombre a la escena actual.
                sceneName = "__ANTES_DE_CAPTURAR__"
            };

            partida.economy.coins = 123456;

            partida.player.hasSafePosition = true;
            partida.player.positionX = 12.5f;
            partida.player.positionY = 7.25f;
            partida.player.positionZ = -19f;
            partida.player.yawDegrees = 120f;

            partida.hasEquipmentUnlocksData = true;
            partida.equipmentUnlocks =
                new EquipmentUnlocksSaveData
                {
                    martilloDesbloqueado = true
                };

            partida.hasToolUpgradesData = true;
            partida.toolUpgrades =
                new ToolUpgradesSaveData
                {
                    imanDesbloqueado = true,
                    nivelAlcanceIman = 2
                };

            partida.hasProcessorData = true;
            partida.processor =
                new ProcessorSaveData
                {
                    desbloqueada = true,
                    nivelProcesado = 3
                };

            partida.hasBuildingInventoryData = true;
            partida.buildingInventory =
                new BuildingInventorySaveData();

            partida.buildingInventory.articulos.Add(
                new BuildingInventoryItemSaveData
                {
                    tipo = "Rampa",
                    unidades = 19
                }
            );

            // Guardar todos los datos que el puente
            // NO tiene permitido modificar.
            string datosGeneralesAntes =
                HuellaSinEdificios(partida);

            Comprobar(
                !PlacedBuildingsSlotBridge
                    .TieneDatosDeEdificios(partida),
                "La partida ficticia ya contiene edificios."
            );

            // ======================================
            // 4. CAPTURAR MEDIANTE EL PUENTE
            // ======================================

            int capturados =
                PlacedBuildingsSlotBridge
                    .CapturarParaRanura(
                        sistema,
                        partida
                    );

            Comprobar(
                capturados == grafoOriginal.edificios,
                "La cantidad capturada es incorrecta."
            );

            Comprobar(
                PlacedBuildingsSlotBridge
                    .TieneDatosDeEdificios(partida),
                "El puente no ha activado los datos " +
                "de edificios."
            );

            Comprobar(
                partida.sceneName == nombreEscena,
                "El nombre de la escena es incorrecto."
            );

            Comprobar(
                JsonUtility.ToJson(partida.placedBuildings) ==
                    edificiosAntes,
                "La captura del puente es distinta " +
                "a la captura original."
            );

            Comprobar(
                HuellaSinEdificios(partida) ==
                    datosGeneralesAntes,
                "El puente ha modificado otros datos " +
                "de la partida."
            );

            Debug.Log(
                "[GUARDADO] PUENTE CAPTURA OK: edificios=" +
                capturados +
                ", conexiones=" +
                grafoOriginal.conexiones +
                ", datos generales conservados."
            );

            // ======================================
            // 5. REPETIR LA CAPTURA
            // ======================================

            int segundaCaptura =
                PlacedBuildingsSlotBridge
                    .CapturarParaRanura(
                        sistema,
                        partida
                    );

            Comprobar(
                segundaCaptura == capturados &&
                JsonUtility.ToJson(partida.placedBuildings) ==
                    edificiosAntes,
                "La segunda captura ha cambiado edificios."
            );

            Comprobar(
                HuellaSinEdificios(partida) ==
                    datosGeneralesAntes,
                "La segunda captura ha modificado " +
                "otros datos."
            );

            Debug.Log(
                "[GUARDADO] PUENTE SEGUNDA CAPTURA OK: " +
                "sin cambios ni duplicados."
            );

            // ======================================
            // 6. COMPROBAR SERIALIZACION JSON
            // ======================================

            GameSaveData desdeJson =
                JsonUtility.FromJson<GameSaveData>(
                    JsonUtility.ToJson(partida)
                );

            PlacedBuildingsConnectionGraphReport grafoLeido =
                PlacedBuildingsConnectionGraphValidator
                    .Analizar(desdeJson);

            Comprobar(
                grafoLeido.edificios == capturados &&
                grafoLeido.conexiones ==
                    grafoOriginal.conexiones,
                "El JSON no conserva los edificios " +
                "o las conexiones."
            );

            Comprobar(
                HuellaSinEdificios(desdeJson) ==
                    datosGeneralesAntes,
                "El JSON no conserva los otros " +
                "datos de la partida."
            );

            Debug.Log(
                "[GUARDADO] PUENTE JSON OK: " +
                "edificios, conexiones, economia, jugador " +
                "y mejoras conservados."
            );

            // ======================================
            // 7. RECHAZAR PARTIDA ANTIGUA
            // ======================================

            GameSaveData antigua = new GameSaveData
            {
                sceneName = nombreEscena
            };

            Comprobar(
                !PlacedBuildingsSlotBridge
                    .TieneDatosDeEdificios(antigua),
                "La partida antigua aparece " +
                "como si tuviera edificios."
            );

            bool antiguaRechazada = false;

            try
            {
                PlacedBuildingsSlotBridge
                    .RestaurarEnEscenaVacia(
                        sistema,
                        antigua
                    );
            }
            catch (InvalidOperationException)
            {
                antiguaRechazada = true;
            }

            Comprobar(
                antiguaRechazada,
                "Se ha permitido restaurar una partida " +
                "sin datos de edificios."
            );

            Debug.Log(
                "[GUARDADO] PUENTE PARTIDA ANTIGUA OK: " +
                "datos ausentes detectados y restauracion " +
                "rechazada."
            );

            // ======================================
            // 8. IMPEDIR CARGA SOBRE EDIFICIOS
            // ======================================

            bool restauracionBloqueada = false;

            try
            {
                PlacedBuildingsSlotBridge
                    .RestaurarEnEscenaVacia(
                        sistema,
                        desdeJson
                    );
            }
            catch (InvalidOperationException)
            {
                restauracionBloqueada = true;
            }

            Comprobar(
                restauracionBloqueada,
                "El puente ha permitido restaurar " +
                "sobre una escena con construcciones."
            );

            Comprobar(
                ContarEdificios(nombreEscena) ==
                    cantidadAntes,
                "La restauracion bloqueada ha modificado " +
                "la cantidad de edificios."
            );

            Debug.Log(
                "[GUARDADO] PUENTE ESCENA OCUPADA OK: " +
                "restauracion bloqueada antes de crear " +
                "o sustituir edificios."
            );

            // ======================================
            // 9. VERIFICAR QUE NADA CAMBIO
            // ======================================

            GameSaveData comprobacionFinal =
                new GameSaveData();

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                comprobacionFinal
            );

            Comprobar(
                JsonUtility.ToJson(
                    comprobacionFinal.placedBuildings
                ) == edificiosAntes,
                "Las construcciones reales o sus " +
                "conexiones han cambiado."
            );

            Comprobar(
                cartera.Monedas == monedasAntes,
                "Se han modificado las monedas reales."
            );

            ComprobarInventario(
                inventario,
                inventarioAntes
            );

            Debug.Log(
                "[GUARDADO] PUENTE RANURAS SUPERADO: " +
                "captura y segunda captura correctas, " +
                "JSON completo, otros datos conservados, " +
                "partidas antiguas detectadas, " +
                "escena ocupada protegida, " +
                "edificios originales e inventario intactos, " +
                "monedas=" + monedasAntes + "."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PUENTE RANURAS FALLIDO: " +
                ex
            );
        }
    }

    // ======================================
    // COMPARAR LOS DEMAS DATOS DE LA PARTIDA
    // ======================================

    private static string HuellaSinEdificios(
        GameSaveData partida)
    {
        GameSaveData copia =
            JsonUtility.FromJson<GameSaveData>(
                JsonUtility.ToJson(partida)
            );

        // El puente puede actualizar la escena
        // y los datos de edificios.
        // Todo lo demas debe ser identico.
        copia.sceneName = "__IGNORAR_ESCENA__";
        copia.hasPlacedBuildingsData = false;
        copia.placedBuildings = null;

        return JsonUtility.ToJson(copia);
    }

    // ======================================
    // CONTAR CONSTRUCCIONES
    // ======================================

    private static int ContarEdificios(
        string nombreEscena)
    {
        int cantidad = 0;

        EdificioConstruido[] encontrados =
            UnityEngine.Object.FindObjectsByType<
                EdificioConstruido>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (EdificioConstruido edificio
            in encontrados)
        {
            if (edificio != null &&
                edificio.gameObject.scene.name ==
                    nombreEscena)
            {
                cantidad++;
            }
        }

        return cantidad;
    }

    // ======================================
    // INVENTARIO
    // ======================================

    private static Dictionary<
        SistemaConstruccion.TipoEdificio, int>
        CapturarInventario(
            InventarioEdificios inventario)
    {
        var resultado =
            new Dictionary<
                SistemaConstruccion.TipoEdificio, int>();

        foreach (SistemaConstruccion.TipoEdificio tipo
            in Enum.GetValues(
                typeof(SistemaConstruccion.TipoEdificio)))
        {
            InventarioEdificios.Articulo articulo =
                inventario.Obtener(tipo);

            Comprobar(
                articulo != null,
                "Falta el articulo: " + tipo
            );

            resultado.Add(
                tipo,
                articulo.Unidades
            );
        }

        return resultado;
    }

    private static void ComprobarInventario(
        InventarioEdificios inventario,
        Dictionary<SistemaConstruccion.TipoEdificio, int>
            esperado)
    {
        foreach (var par in esperado)
        {
            InventarioEdificios.Articulo articulo =
                inventario.Obtener(par.Key);

            Comprobar(
                articulo != null &&
                articulo.Unidades == par.Value,
                "El inventario ha cambiado: " +
                par.Key
            );
        }
    }

    private static void Comprobar(
        bool condicion,
        string mensaje)
    {
        if (!condicion)
        {
            throw new InvalidOperationException(mensaje);
        }
    }
}

#endif
