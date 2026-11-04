
#if UNITY_EDITOR

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrSaveFileIntegrationTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar carga edificios desde archivo (Play)"
    )]
    public static void Ejecutar()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[GUARDADO] Esta prueba requiere Play."
            );
            return;
        }

        string carpeta = Path.Combine(
            Application.persistentDataPath,
            "PiedrasIncr",
            "DeveloperTests",
            "RestoreFromFile_" + Guid.NewGuid().ToString("N")
        );

        HashSet<string> idsTemporales =
            new HashSet<string>(StringComparer.Ordinal);

        try
        {
            // ======================================
            // 1. COMPROBAR SISTEMAS
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
                cartera = inventario.GetComponentInParent<Cartera>();

            Comprobar(
                cartera != null,
                "No se encuentra Cartera."
            );

            int monedasAntes = cartera.Monedas;

            var inventarioAntes =
                CapturarInventario(inventario);

            // ======================================
            // 2. CAPTURAR CONSTRUCCIONES REALES
            // ======================================

            GameSaveData captura = new GameSaveData
            {
                sceneName = SceneManager.GetActiveScene().name
            };

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                captura
            );

            Dictionary<string, PlacedBuildingSaveData> mapa =
                new Dictionary<string, PlacedBuildingSaveData>(
                    StringComparer.Ordinal
                );

            Dictionary<string, string> originales =
                new Dictionary<string, string>(
                    StringComparer.Ordinal
                );

            foreach (PlacedBuildingSaveData edificio
                in captura.placedBuildings.edificios)
            {
                mapa.Add(
                    edificio.idPersistente,
                    edificio
                );

                originales.Add(
                    edificio.idPersistente,
                    JsonUtility.ToJson(edificio)
                );
            }

            PlacedBuildingSaveData rampaOriginal = null;
            PlacedBuildingSaveData paredOriginal = null;

            foreach (PlacedBuildingSaveData edificio
                in captura.placedBuildings.edificios)
            {
                if (edificio.tipo != "Pared" ||
                    !edificio.hasConnection ||
                    edificio.connectorTag != "RailRampa")
                {
                    continue;
                }

                PlacedBuildingSaveData posibleRampa;

                if (mapa.TryGetValue(
                        edificio.connectedToBuildingId,
                        out posibleRampa) &&
                    posibleRampa.tipo == "Rampa" &&
                    posibleRampa.curvaturaRampa == 0f)
                {
                    rampaOriginal = posibleRampa;
                    paredOriginal = edificio;
                    break;
                }
            }

            Comprobar(
                rampaOriginal != null &&
                paredOriginal != null,
                "Necesitas una rampa recta " +
                "con una pared conectada a RailRampa."
            );

            // ======================================
            // 3. PREPARAR PARTIDA TEMPORAL
            // ======================================

            PlacedBuildingSaveData rampaGuardada =
                CopiarEdificio(rampaOriginal);

            PlacedBuildingSaveData paredGuardada =
                CopiarEdificio(paredOriginal);

            string idRampa = Guid.NewGuid().ToString("N");
            string idPared = Guid.NewGuid().ToString("N");

            idsTemporales.Add(idRampa);
            idsTemporales.Add(idPared);

            rampaGuardada.idPersistente = idRampa;
            paredGuardada.idPersistente = idPared;

            // La copia de la rampa no dependera
            // de edificios de la escena original.
            rampaGuardada.hasConnection = false;
            rampaGuardada.connectedToBuildingId = null;
            rampaGuardada.connectorPath = null;
            rampaGuardada.connectorTag = null;

            // La pared apuntara a la nueva rampa.
            paredGuardada.hasConnection = true;
            paredGuardada.connectedToBuildingId = idRampa;

            Vector3 desplazamiento =
                new Vector3(45f, 25f, 45f);

            Desplazar(rampaGuardada, desplazamiento);
            Desplazar(paredGuardada, desplazamiento);

            GameSaveData partida = new GameSaveData
            {
                sceneName = SceneManager.GetActiveScene().name,
                hasPlacedBuildingsData = true,
                placedBuildings = new PlacedBuildingsSaveData()
            };

            partida.economy.coins = monedasAntes;

            partida.placedBuildings.edificios.Add(
                rampaGuardada
            );

            partida.placedBuildings.edificios.Add(
                paredGuardada
            );

            // ======================================
            // 4. GUARDAR EN ARCHIVO TEMPORAL
            // ======================================

            SaveStorage storage = new SaveStorage(carpeta);

            storage.Save(1, partida);

            Comprobar(
                File.Exists(
                    storage.GetPrimaryPathForDiagnostics(1)
                ),
                "No se ha creado el archivo de guardado."
            );

            Debug.Log(
                "[GUARDADO] ARCHIVO TEMPORAL CREADO OK: " +
                "ranura de diagnostico 1, " +
                "2 edificios y 1 conexion."
            );

            // ======================================
            // 5. LEER DESDE EL DISCO
            // ======================================

            GameSaveData cargada;
            bool desdeRespaldo;

            Comprobar(
                storage.TryLoad(
                    1,
                    out cargada,
                    out desdeRespaldo
                ),
                "No se ha podido leer el archivo."
            );

            Comprobar(
                !desdeRespaldo,
                "Se ha utilizado un respaldo inesperado."
            );

            Comprobar(
                cargada.slotIndex == 1 &&
                cargada.sceneName == partida.sceneName &&
                cargada.economy.coins == monedasAntes,
                "Los datos generales no coinciden."
            );

            Comprobar(
                JsonUtility.ToJson(partida.placedBuildings) ==
                JsonUtility.ToJson(cargada.placedBuildings),
                "Los datos de edificios han cambiado en disco."
            );

            PlacedBuildingsConnectionGraphReport grafo =
                PlacedBuildingsConnectionGraphValidator
                    .Analizar(cargada);

            Comprobar(
                grafo.edificios == 2 &&
                grafo.conexiones == 1 &&
                grafo.paredesSobreRampas == 1,
                "El grafo leido es incorrecto."
            );

            Debug.Log(
                "[GUARDADO] LECTURA ARCHIVO OK: " +
                "JSON integro, IDs, variantes y " +
                "conexion Pared -> Rampa conservados."
            );

            // ======================================
            // 6. RESTAURAR DIRECTAMENTE DESDE
            //    LOS DATOS LEIDOS DEL DISCO
            // ======================================

            PlacedBuildingsRestoreResult resultado =
                PlacedBuildingsRestoreCoordinator
                    .RestaurarNuevos(sistema, cargada);

            Comprobar(
                resultado.edificios.Count == 2 &&
                resultado.conexionesRestauradas == 1,
                "El coordinador no ha restaurado " +
                "los edificios y su conexion."
            );

            EdificioConstruido rampa =
                BuscarEdificio(idRampa);

            EdificioConstruido pared =
                BuscarEdificio(idPared);

            Comprobar(
                rampa != null && pared != null,
                "Faltan los edificios reconstruidos."
            );

            // Desactivar las copias de diagnostico.
            rampa.gameObject.SetActive(false);
            pared.gameObject.SetActive(false);

            VerificarTransformacion(
                rampa,
                rampaGuardada
            );

            VerificarTransformacion(
                pared,
                paredGuardada
            );

            Collider rail = pared.conectorUsado;

            Comprobar(
                rail != null &&
                rail.CompareTag("RailRampa") &&
                !rail.enabled &&
                rail.GetComponentInParent<
                    EdificioConstruido>(true) == rampa,
                "La conexion fisica no se ha restaurado."
            );

            Comprobar(
                rampa.edificiosDependientes.Count == 1 &&
                rampa.edificiosDependientes[0] ==
                    pared.gameObject,
                "La dependencia no se ha restaurado."
            );

            Debug.Log(
                "[GUARDADO] RESTAURACION DESDE ARCHIVO OK: " +
                "2 prefabs reales reconstruidos, " +
                "RailRampa ocupado y dependencia correcta."
            );

            // ======================================
            // 7. PROTEGER LOS EDIFICIOS ORIGINALES
            // ======================================

            GameSaveData capturaFinal =
                new GameSaveData();

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                capturaFinal
            );

            int originalesVerificados = 0;
            int temporalesVerificados = 0;

            foreach (PlacedBuildingSaveData edificio
                in capturaFinal.placedBuildings.edificios)
            {
                string anterior;

                if (originales.TryGetValue(
                    edificio.idPersistente,
                    out anterior))
                {
                    Comprobar(
                        anterior == JsonUtility.ToJson(edificio),
                        "Ha cambiado un edificio original."
                    );

                    originalesVerificados++;
                }
                else
                {
                    Comprobar(
                        idsTemporales.Contains(
                            edificio.idPersistente),
                        "Ha aparecido un edificio inesperado."
                    );

                    temporalesVerificados++;
                }
            }

            Comprobar(
                originalesVerificados == originales.Count &&
                temporalesVerificados == 2,
                "Cantidad de edificios incorrecta."
            );

            Comprobar(
                cartera.Monedas == monedasAntes,
                "Las monedas han cambiado."
            );

            ComprobarInventario(
                inventario,
                inventarioAntes
            );

            Debug.Log(
                "[GUARDADO] CARGA DESDE ARCHIVO SUPERADA: " +
                "archivo guardado y leido correctamente, " +
                "2 edificios reconstruidos, " +
                "conexion restaurada, IDs y transformaciones " +
                "correctos, edificios originales e inventario " +
                "intactos, monedas=" + monedasAntes + "."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] CARGA DESDE ARCHIVO FALLIDA: " +
                ex
            );
        }
        finally
        {
            // Retirar exclusivamente los edificios
            // temporales de esta prueba.
            EdificioConstruido[] edificios =
                UnityEngine.Object.FindObjectsByType<
                    EdificioConstruido>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            foreach (EdificioConstruido edificio in edificios)
            {
                if (edificio == null ||
                    !idsTemporales.Contains(
                        edificio.IdPersistente))
                {
                    continue;
                }

                edificio.gameObject.SetActive(false);

                UnityEngine.Object.Destroy(
                    edificio.gameObject
                );
            }

            // Borrar la carpeta de diagnostico.
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

    // ======================================
    // BUSCAR EDIFICIO
    // ======================================

    private static EdificioConstruido BuscarEdificio(
        string id)
    {
        Scene escena = SceneManager.GetActiveScene();

        foreach (EdificioConstruido edificio
            in UnityEngine.Object.FindObjectsByType<
                EdificioConstruido>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            ))
        {
            if (edificio != null &&
                edificio.gameObject.scene == escena &&
                edificio.IdPersistente == id)
            {
                return edificio;
            }
        }

        return null;
    }

    // ======================================
    // COMPROBAR TRANSFORMACION
    // ======================================

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

    // ======================================
    // COPIAR Y DESPLAZAR
    // ======================================

    private static PlacedBuildingSaveData CopiarEdificio(
        PlacedBuildingSaveData edificio)
    {
        return JsonUtility.FromJson<PlacedBuildingSaveData>(
            JsonUtility.ToJson(edificio)
        );
    }

    private static void Desplazar(
        PlacedBuildingSaveData edificio,
        Vector3 desplazamiento)
    {
        edificio.positionX += desplazamiento.x;
        edificio.positionY += desplazamiento.y;
        edificio.positionZ += desplazamiento.z;
    }

    // ======================================
    // INVENTARIO
    // ======================================

    private static Dictionary<
        SistemaConstruccion.TipoEdificio, int>
        CapturarInventario(InventarioEdificios inventario)
    {
        var resultado = new Dictionary<
            SistemaConstruccion.TipoEdificio, int>();

        foreach (SistemaConstruccion.TipoEdificio tipo
            in Enum.GetValues(
                typeof(SistemaConstruccion.TipoEdificio)))
        {
            InventarioEdificios.Articulo articulo =
                inventario.Obtener(tipo);

            Comprobar(
                articulo != null,
                "Falta el articulo " + tipo
            );

            resultado.Add(tipo, articulo.Unidades);
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
                "Inventario modificado: " + par.Key
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
