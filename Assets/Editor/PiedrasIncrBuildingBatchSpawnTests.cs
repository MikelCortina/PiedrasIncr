
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrBuildingBatchSpawnTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar lote edificios (Play)"
    )]
    public static void Ejecutar()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[GUARDADO] Entra en Play para realizar la prueba."
            );
            return;
        }

        HashSet<string> idsTemporales =
            new HashSet<string>(StringComparer.Ordinal);

        try
        {
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

            if (cartera == null)
            {
                Cartera[] carteras =
                    UnityEngine.Object.FindObjectsByType<Cartera>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None
                    );

                Comprobar(
                    carteras.Length == 1,
                    "No se encuentra una Cartera unica."
                );

                cartera = carteras[0];
            }

            int monedasAntes = cartera.Monedas;

            Dictionary<SistemaConstruccion.TipoEdificio, int>
                inventarioAntes = CapturarInventario(inventario);

            int edificiosAntes = ContarEdificios();

            // ==========================================
            // 1. CAPTURAR EDIFICIOS DE LA ESCENA
            // ==========================================

            GameSaveData captura = new GameSaveData();

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                captura
            );

            PlacedBuildingsSaveData originales =
                PlacedBuildingsSaveAdapter.Read(captura);

            // Seleccionar dos edificios sin curvatura.
            List<PlacedBuildingSaveData> referencias =
                new List<PlacedBuildingSaveData>();

            foreach (PlacedBuildingSaveData edificio
                in originales.edificios)
            {
                if (edificio.curvaturaRampa == 0f)
                {
                    referencias.Add(edificio);

                    if (referencias.Count == 2)
                        break;
                }
            }

            Comprobar(
                referencias.Count == 2,
                "Coloca al menos dos edificios sin curvatura."
            );

            // ==========================================
            // 2. PREPARAR PARTIDA TEMPORAL
            // ==========================================

            GameSaveData partida = new GameSaveData
            {
                hasPlacedBuildingsData = true,
                placedBuildings = new PlacedBuildingsSaveData()
            };

            for (int i = 0; i < referencias.Count; i++)
            {
                PlacedBuildingSaveData copia =
                    JsonUtility.FromJson<PlacedBuildingSaveData>(
                        JsonUtility.ToJson(referencias[i])
                    );

                // Crear nueva identidad para la copia.
                copia.idPersistente =
                    Guid.NewGuid().ToString("N");

                // Separar las copias de los originales.
                copia.positionX += 15f + i * 5f;

                idsTemporales.Add(copia.idPersistente);

                partida.placedBuildings.edificios.Add(copia);
            }

            // Simular la lectura de datos serializados.
            partida = JsonUtility.FromJson<GameSaveData>(
                JsonUtility.ToJson(partida)
            );

            // ==========================================
            // 3. RECONSTRUIR DOS EDIFICIOS
            // ==========================================

            List<EdificioConstruido> primeraCarga =
                PlacedBuildingsBatchSpawnAdapter
                    .CrearORecuperarTodos(sistema, partida);

            Comprobar(
                primeraCarga.Count == 2,
                "La primera carga no ha creado dos edificios."
            );

            Comprobar(
                ContarEdificios() == edificiosAntes + 2,
                "Cantidad incorrecta tras reconstruir."
            );

            for (int i = 0; i < 2; i++)
            {
                EdificioConstruido edificio =
                    primeraCarga[i];

                PlacedBuildingSaveData datos =
                    partida.placedBuildings.edificios[i];

                Comprobar(
                    edificio != null &&
                    edificio.IdPersistente == datos.idPersistente,
                    "Identidad restaurada incorrecta."
                );

                Comprobar(
                    edificio.tipo.ToString() == datos.tipo &&
                    edificio.VariantePrefab == datos.variantePrefab,
                    "Tipo o variante incorrectos."
                );

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

                Debug.Log(
                    "[GUARDADO] LOTE EDIFICIO OK: " +
                    "tipo=" + edificio.tipo +
                    ", variante=" + edificio.VariantePrefab +
                    ", id=" + edificio.IdPersistente
                );
            }

            // ==========================================
            // 4. REPETIR LA CARGA
            // ==========================================

            List<EdificioConstruido> segundaCarga =
                PlacedBuildingsBatchSpawnAdapter
                    .CrearORecuperarTodos(sistema, partida);

            Comprobar(
                segundaCarga.Count == primeraCarga.Count,
                "La segunda carga devolvio otra cantidad."
            );

            for (int i = 0; i < 2; i++)
            {
                Comprobar(
                    segundaCarga[i] == primeraCarga[i],
                    "La segunda carga ha creado un duplicado."
                );
            }

            Comprobar(
                ContarEdificios() == edificiosAntes + 2,
                "Se han duplicado los edificios."
            );

            // ==========================================
            // 5. COMPROBAR RECHAZO DE PREFAB INVALIDO
            // ==========================================

            GameSaveData invalida =
                JsonUtility.FromJson<GameSaveData>(
                    JsonUtility.ToJson(partida)
                );

            invalida.placedBuildings.edificios[1]
                .variantePrefab = int.MaxValue;

            bool rechazado = false;

            try
            {
                PlacedBuildingsBatchSpawnAdapter
                    .CrearORecuperarTodos(sistema, invalida);
            }
            catch (InvalidOperationException)
            {
                rechazado = true;
            }

            Comprobar(
                rechazado,
                "Se ha aceptado un prefab inexistente."
            );

            Comprobar(
                ContarEdificios() == edificiosAntes + 2,
                "La prueba invalida ha modificado la escena."
            );

            // ==========================================
            // 6. ECONOMIA E INVENTARIO
            // ==========================================

            Comprobar(
                cartera.Monedas == monedasAntes,
                "Se han modificado las monedas."
            );

            ComprobarInventario(
                inventario,
                inventarioAntes
            );

            // Comprobar que los originales siguen vivos.
            foreach (PlacedBuildingSaveData referencia
                in referencias)
            {
                Comprobar(
                    BuscarEdificio(referencia.idPersistente) != null,
                    "Ha desaparecido un edificio original."
                );
            }

            Debug.Log(
                "[GUARDADO] RECONSTRUCCION LOTE SUPERADA: " +
                "2 edificios reconstruidos, IDs, prefabs, " +
                "posiciones, rotaciones y escalas correctas; " +
                "segunda carga sin duplicados, " +
                "prefab invalido rechazado, " +
                "monedas e inventario intactos."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] RECONSTRUCCION LOTE FALLIDA: " +
                ex
            );
        }
        finally
        {
            // Eliminar exclusivamente las copias de prueba.
            EdificioConstruido[] edificios =
                UnityEngine.Object.FindObjectsByType<
                    EdificioConstruido>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            Scene escena = SceneManager.GetActiveScene();

            foreach (EdificioConstruido edificio in edificios)
            {
                if (edificio == null ||
                    edificio.gameObject.scene != escena ||
                    !idsTemporales.Contains(edificio.IdPersistente))
                {
                    continue;
                }

                edificio.gameObject.SetActive(false);

                UnityEngine.Object.Destroy(
                    edificio.gameObject
                );
            }
        }
    }

    private static EdificioConstruido BuscarEdificio(string id)
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

    private static int ContarEdificios()
    {
        int cantidad = 0;

        Scene escena = SceneManager.GetActiveScene();

        foreach (EdificioConstruido edificio
            in UnityEngine.Object.FindObjectsByType<
                EdificioConstruido>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            ))
        {
            if (edificio != null &&
                edificio.gameObject.scene == escena)
            {
                cantidad++;
            }
        }

        return cantidad;
    }

    private static Dictionary<
        SistemaConstruccion.TipoEdificio, int>
        CapturarInventario(InventarioEdificios inventario)
    {
        var datos = new Dictionary<
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

            datos.Add(tipo, articulo.Unidades);
        }

        return datos;
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
            throw new InvalidOperationException(mensaje);
    }
}

#endif
