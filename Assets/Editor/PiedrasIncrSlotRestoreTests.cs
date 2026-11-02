
#if UNITY_EDITOR

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrSlotRestoreTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar restauracion real ranura (Play)"
    )]
    public static void Ejecutar()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[GUARDADO] Entra en Play.");
            return;
        }

        Scene escenaOriginal = SceneManager.GetActiveScene();
        Scene escenaTemporal = default(Scene);

        string carpeta = Path.Combine(
            Application.persistentDataPath,
            "PiedrasIncr",
            "DeveloperTests",
            "SlotRestore_" + Guid.NewGuid().ToString("N")
        );

        try
        {
            SistemaConstruccion sistema =
                SistemaConstruccion.Instancia;

            Comprobar(
                sistema != null,
                "No existe SistemaConstruccion."
            );

            InventarioEdificios inventario =
                sistema.inventarioEdificios;

            Comprobar(
                inventario != null,
                "No existe InventarioEdificios."
            );

            Cartera cartera = inventario.cartera;

            if (cartera == null)
                cartera = inventario.GetComponentInParent<Cartera>();

            Comprobar(
                cartera != null,
                "No existe Cartera."
            );

            int monedasAntes = cartera.Monedas;

            GameSaveData original = new GameSaveData();

            EconomySaveAdapter.Capture(cartera, original);

            BuildingInventorySaveAdapter.Capture(
                inventario,
                original
            );

            PlacedBuildingsSlotBridge.CapturarParaRanura(
                sistema,
                original
            );

            string edificiosAntes =
                JsonUtility.ToJson(original.placedBuildings);

            string inventarioAntes =
                JsonUtility.ToJson(original.buildingInventory);

            // =====================================
            // 1. GUARDAR UNA PARTIDA REAL
            // =====================================

            SaveStorage storage = new SaveStorage(carpeta);

            GameSaveData guardada =
                GameSaveSlotWriter.GuardarConEdificios(
                    storage,
                    1,
                    sistema,
                    original
                );

            GameSaveData leida;
            bool respaldo;

            Comprobar(
                storage.TryLoad(1, out leida, out respaldo),
                "No se ha podido leer la captura inicial."
            );

            Comprobar(!respaldo, "Respaldo inesperado.");

            Debug.Log(
                "[GUARDADO] PASO 76 ARCHIVO OK: " +
                "captura original guardada y leida."
            );

            // =====================================
            // 2. SELECCIONAR DOS EDIFICIOS CONECTADOS
            // =====================================

            var mapa = new Dictionary<string, PlacedBuildingSaveData>();

            foreach (PlacedBuildingSaveData edificio
                in leida.placedBuildings.edificios)
            {
                mapa.Add(edificio.idPersistente, edificio);
            }

            PlacedBuildingSaveData rampaOrigen = null;
            PlacedBuildingSaveData paredOrigen = null;

            foreach (PlacedBuildingSaveData edificio
                in leida.placedBuildings.edificios)
            {
                if (edificio.tipo != "Pared" ||
                    !edificio.hasConnection ||
                    edificio.connectorTag != "RailRampa")
                    continue;

                PlacedBuildingSaveData candidata;

                if (mapa.TryGetValue(
                    edificio.connectedToBuildingId,
                    out candidata))
                {
                    if (candidata.tipo == "Rampa" &&
                        candidata.curvaturaRampa == 0f)
                    {
                        rampaOrigen = candidata;
                        paredOrigen = edificio;
                        break;
                    }
                }
            }

            Comprobar(
                rampaOrigen != null && paredOrigen != null,
                "Necesitas una rampa recta con una pared " +
                "conectada a RailRampa."
            );

            PlacedBuildingSaveData rampa =
                JsonUtility.FromJson<PlacedBuildingSaveData>(
                    JsonUtility.ToJson(rampaOrigen)
                );

            PlacedBuildingSaveData pared =
                JsonUtility.FromJson<PlacedBuildingSaveData>(
                    JsonUtility.ToJson(paredOrigen)
                );

            rampa.idPersistente = Guid.NewGuid().ToString("N");
            pared.idPersistente = Guid.NewGuid().ToString("N");

            // La rampa de prueba sera independiente.
            rampa.hasConnection = false;
            rampa.connectedToBuildingId = null;
            rampa.connectorPath = null;
            rampa.connectorTag = null;

            // La pared conserva su conexion fisica,
            // apuntando a la copia de la rampa.
            pared.hasConnection = true;
            pared.connectedToBuildingId = rampa.idPersistente;

            // =====================================
            // 3. CREAR ESCENA TEMPORAL VACIA
            // =====================================

            escenaTemporal = SceneManager.CreateScene(
                "PiedrasIncr_Paso76_" +
                Guid.NewGuid().ToString("N"),
                new CreateSceneParameters(
                    LocalPhysicsMode.Physics3D
                )
            );

            // Preparar una copia de diagnostico
            // asociada exclusivamente a esa escena.
            GameSaveData prueba =
                JsonUtility.FromJson<GameSaveData>(
                    JsonUtility.ToJson(guardada)
                );

            prueba.sceneName = escenaTemporal.name;
            prueba.placedBuildings =
                new PlacedBuildingsSaveData();

            prueba.placedBuildings.edificios.Add(rampa);
            prueba.placedBuildings.edificios.Add(pared);
            prueba.hasPlacedBuildingsData = true;

            PlacedBuildingsConnectionGraphReport grafo =
                PlacedBuildingsConnectionGraphValidator.Analizar(
                    prueba
                );

            Comprobar(
                grafo.edificios == 2 &&
                grafo.conexiones == 1,
                "El grafo temporal no es correcto."
            );

            storage.Save(2, prueba);

            Comprobar(
                SceneManager.SetActiveScene(escenaTemporal),
                "No se ha podido activar la escena temporal."
            );

            Debug.Log(
                "[GUARDADO] PASO 76 ESCENA VACIA OK."
            );

            // =====================================
            // 4. RESTAURAR DESDE LA RANURA
            // =====================================

            GameSaveData cargada;
            bool desdeBackup;

            PlacedBuildingsRestoreResult resultado =
                GameSaveSlotReader.RestaurarEdificios(
                    storage,
                    2,
                    sistema,
                    out cargada,
                    out desdeBackup
                );

            Comprobar(
                !desdeBackup &&
                resultado.edificios.Count == 2 &&
                resultado.conexionesRestauradas == 1,
                "La restauracion desde la ranura fallo."
            );

            EdificioConstruido rampaReal = null;
            EdificioConstruido paredReal = null;

            foreach (EdificioConstruido edificio
                in resultado.edificios)
            {
                Comprobar(
                    edificio != null &&
                    edificio.gameObject.scene == escenaTemporal,
                    "Un edificio no pertenece a la escena temporal."
                );

                if (edificio.IdPersistente == rampa.idPersistente)
                    rampaReal = edificio;

                if (edificio.IdPersistente == pared.idPersistente)
                    paredReal = edificio;
            }

            Comprobar(
                rampaReal != null && paredReal != null,
                "Faltan los edificios restaurados."
            );

            // Detener comportamientos de las copias.
            rampaReal.gameObject.SetActive(false);
            paredReal.gameObject.SetActive(false);

            // =====================================
            // 5. COMPROBAR TRANSFORMACIONES
            // =====================================

            VerificarTransformacion(rampaReal, rampa);
            VerificarTransformacion(paredReal, pared);

            Debug.Log(
                "[GUARDADO] PASO 76 PREFABS OK: " +
                "IDs, posiciones, rotaciones y escalas."
            );

            // =====================================
            // 6. COMPROBAR CONEXION FISICA
            // =====================================

            Collider rail = paredReal.conectorUsado;

            Comprobar(
                rail != null &&
                rail.CompareTag("RailRampa") &&
                !rail.enabled &&
                rail.GetComponentInParent<
                    EdificioConstruido>(true) == rampaReal,
                "La conexion RailRampa es incorrecta."
            );

            Comprobar(
                rampaReal.edificiosDependientes.Contains(
                    paredReal.gameObject
                ),
                "No se ha recuperado la dependencia."
            );

            Debug.Log(
                "[GUARDADO] PASO 76 CONEXION OK: " +
                "RailRampa ocupado y dependencia restaurada."
            );

            // =====================================
            // 7. VERIFICAR LA ESCENA ORIGINAL
            // =====================================

            Comprobar(
                SceneManager.SetActiveScene(escenaOriginal),
                "No se ha podido volver a la escena original."
            );

            GameSaveData comprobacion = new GameSaveData();

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                comprobacion
            );

            Comprobar(
                JsonUtility.ToJson(comprobacion.placedBuildings) ==
                edificiosAntes,
                "Los edificios originales han cambiado."
            );

            BuildingInventorySaveAdapter.Capture(
                inventario,
                comprobacion
            );

            Comprobar(
                JsonUtility.ToJson(comprobacion.buildingInventory) ==
                inventarioAntes,
                "El inventario original ha cambiado."
            );

            Comprobar(
                cartera.Monedas == monedasAntes,
                "Las monedas han cambiado."
            );

            Debug.Log(
                "[GUARDADO] PASO 76 SUPERADO: " +
                "guardado y carga reales desde disco, " +
                "dos edificios reconstruidos en escena aislada, " +
                "conexion fisica restaurada, " +
                "escena original, inventario y monedas intactos."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PASO 76 FALLIDO: " + ex
            );
        }
        finally
        {
            // Volver siempre a la escena original.
            if (escenaOriginal.IsValid() &&
                escenaOriginal.isLoaded)
            {
                SceneManager.SetActiveScene(escenaOriginal);
            }

            // Retirar exclusivamente la escena de prueba.
            if (escenaTemporal.IsValid() &&
                escenaTemporal.isLoaded)
            {
                SceneManager.UnloadSceneAsync(escenaTemporal);
            }

            // Borrar exclusivamente los archivos temporales.
            try
            {
                if (Directory.Exists(carpeta))
                    Directory.Delete(carpeta, true);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "[GUARDADO] Error limpiando archivos: " +
                    ex.Message
                );
            }
        }
    }

    private static void VerificarTransformacion(
        EdificioConstruido edificio,
        PlacedBuildingSaveData datos)
    {
        Vector3 posicion = new Vector3(
            datos.positionX,
            datos.positionY,
            datos.positionZ
        );

        Quaternion rotacion = new Quaternion(
            datos.rotationX,
            datos.rotationY,
            datos.rotationZ,
            datos.rotationW
        );

        Vector3 escala = new Vector3(
            datos.scaleX,
            datos.scaleY,
            datos.scaleZ
        );

        Comprobar(
            edificio.IdPersistente == datos.idPersistente &&
            edificio.VariantePrefab == datos.variantePrefab,
            "Identidad o variante incorrecta."
        );

        Comprobar(
            Vector3.Distance(
                edificio.transform.position,
                posicion
            ) < 0.001f,
            "Posicion incorrecta."
        );

        Comprobar(
            Quaternion.Angle(
                edificio.transform.rotation,
                rotacion
            ) < 0.1f,
            "Rotacion incorrecta."
        );

        Comprobar(
            Vector3.Distance(
                edificio.transform.localScale,
                escala
            ) < 0.001f,
            "Escala incorrecta."
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
