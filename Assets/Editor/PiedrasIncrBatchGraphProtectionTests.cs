
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrBatchGraphProtectionTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar proteccion grafo y reconstruccion (Play)"
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

            Comprobar(
                cartera != null,
                "No se encuentra Cartera."
            );

            int monedasAntes = cartera.Monedas;

            var inventarioAntes =
                CapturarInventario(inventario);

            int cantidadAntes = ContarEdificios();

            // ======================================
            // 1. CAPTURAR EDIFICIOS ORIGINALES
            // ======================================

            GameSaveData captura = new GameSaveData
            {
                sceneName =
                    SceneManager.GetActiveScene().name
            };

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                captura
            );

            Dictionary<string, string> originales =
                new Dictionary<string, string>(
                    StringComparer.Ordinal
                );

            Dictionary<string, PlacedBuildingSaveData> mapa =
                new Dictionary<string, PlacedBuildingSaveData>(
                    StringComparer.Ordinal
                );

            foreach (PlacedBuildingSaveData edificio
                in captura.placedBuildings.edificios)
            {
                originales.Add(
                    edificio.idPersistente,
                    JsonUtility.ToJson(edificio)
                );

                mapa.Add(
                    edificio.idPersistente,
                    edificio
                );
            }

            // Buscar una pared realmente conectada
            // al RailRampa de una rampa recta.
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

                PlacedBuildingSaveData candidata;

                if (!mapa.TryGetValue(
                    edificio.connectedToBuildingId,
                    out candidata))
                {
                    continue;
                }

                if (candidata.tipo == "Rampa" &&
                    candidata.curvaturaRampa == 0f)
                {
                    rampaOriginal = candidata;
                    paredOriginal = edificio;
                    break;
                }
            }

            Comprobar(
                rampaOriginal != null &&
                paredOriginal != null,
                "Necesitas una rampa sin curvatura " +
                "con una pared conectada a RailRampa."
            );

            // ======================================
            // 2. CREAR PARTIDA VALIDA TEMPORAL
            // ======================================

            string idRampa =
                Guid.NewGuid().ToString("N");

            string idPared =
                Guid.NewGuid().ToString("N");

            idsTemporales.Add(idRampa);
            idsTemporales.Add(idPared);

            PlacedBuildingSaveData rampaGuardada =
                CopiarEdificio(rampaOriginal);

            PlacedBuildingSaveData paredGuardada =
                CopiarEdificio(paredOriginal);

            rampaGuardada.idPersistente = idRampa;
            paredGuardada.idPersistente = idPared;

            // La rampa temporal sera independiente.
            rampaGuardada.hasConnection = false;
            rampaGuardada.connectedToBuildingId = null;
            rampaGuardada.connectorPath = null;
            rampaGuardada.connectorTag = null;

            // La pared temporal utilizara
            // el conector de la rampa temporal.
            paredGuardada.connectedToBuildingId =
                idRampa;

            Vector3 desplazamiento =
                new Vector3(40f, 25f, 40f);

            Desplazar(rampaGuardada, desplazamiento);
            Desplazar(paredGuardada, desplazamiento);

            GameSaveData valida = new GameSaveData
            {
                sceneName =
                    SceneManager.GetActiveScene().name,

                hasPlacedBuildingsData = true,

                placedBuildings =
                    new PlacedBuildingsSaveData()
            };

            valida.placedBuildings.edificios.Add(
                rampaGuardada
            );

            valida.placedBuildings.edificios.Add(
                paredGuardada
            );

            valida = CopiarPartida(valida);

            PlacedBuildingsConnectionGraphReport informe =
                PlacedBuildingsConnectionGraphValidator
                    .Analizar(valida);

            Comprobar(
                informe.edificios == 2 &&
                informe.conexiones == 1 &&
                informe.paredesSobreRampas == 1,
                "El grafo valido no se ha reconocido."
            );

            // ======================================
            // 3. ID DE PROPIETARIO INEXISTENTE
            // ======================================

            GameSaveData idInexistente =
                CopiarPartida(valida);

            idInexistente.placedBuildings.edificios[1]
                .connectedToBuildingId =
                    Guid.NewGuid().ToString("N");

            VerificarRechazo(
                sistema,
                idInexistente,
                cantidadAntes,
                idsTemporales,
                "PROPIETARIO INEXISTENTE"
            );

            // ======================================
            // 4. CONECTOR OCUPADO DOS VECES
            // ======================================

            GameSaveData duplicada =
                CopiarPartida(valida);

            PlacedBuildingSaveData terceraPared =
                CopiarEdificio(
                    duplicada.placedBuildings.edificios[1]
                );

            terceraPared.idPersistente =
                Guid.NewGuid().ToString("N");

            terceraPared.positionX += 5f;

            idsTemporales.Add(
                terceraPared.idPersistente
            );

            // Conserva el mismo ID de rampa
            // y la misma ruta RailRampa.
            duplicada.placedBuildings.edificios.Add(
                terceraPared
            );

            VerificarRechazo(
                sistema,
                duplicada,
                cantidadAntes,
                idsTemporales,
                "CONECTOR DUPLICADO"
            );

            // ======================================
            // 5. TIPO DE CONEXION INCOMPATIBLE
            // ======================================

            GameSaveData incompatible =
                CopiarPartida(valida);

            incompatible.placedBuildings.edificios[1]
                .connectorTag = "ConectorSalida";

            VerificarRechazo(
                sistema,
                incompatible,
                cantidadAntes,
                idsTemporales,
                "CONEXION INCOMPATIBLE"
            );

            // ======================================
            // 6. RECONSTRUIR LA PARTIDA VALIDA
            // ======================================

            Comprobar(
                ContarEdificios() == cantidadAntes,
                "Las pruebas invalidas alteraron la escena."
            );

            List<EdificioConstruido> reconstruidos =
                PlacedBuildingsBatchSpawnAdapter
                    .CrearORecuperarTodos(sistema, valida);

            Comprobar(
                reconstruidos.Count == 2,
                "No se reconstruyeron los dos edificios."
            );

            EdificioConstruido rampa =
                BuscarEdificio(idRampa);

            EdificioConstruido pared =
                BuscarEdificio(idPared);

            Comprobar(
                rampa != null && pared != null,
                "No se encuentran los prefabs reconstruidos."
            );

            Comprobar(
                rampa.tipo ==
                    SistemaConstruccion.TipoEdificio.Rampa &&
                pared.tipo ==
                    SistemaConstruccion.TipoEdificio.Pared,
                "Los tipos reconstruidos no coinciden."
            );

            // Desactivar las copias para evitar
            // su comportamiento normal durante el test.
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

            Comprobar(
                ContarEdificios() == cantidadAntes + 2,
                "Cantidad de edificios incorrecta."
            );

            Debug.Log(
                "[GUARDADO] GRAFO VALIDO RECONSTRUIDO: " +
                "2 prefabs reales, IDs y transformaciones correctos."
            );

            // ======================================
            // 7. RESTAURAR LA CONEXION
            // ======================================

            int conexiones =
                PlacedBuildingsConnectionsRestoreAdapter
                    .Restaurar(valida);

            Comprobar(
                conexiones == 1,
                "No se ha restaurado la conexion."
            );

            Collider rail = pared.conectorUsado;

            Comprobar(
                rail != null &&
                rail.CompareTag("RailRampa") &&
                !rail.enabled,
                "El conector RailRampa no esta ocupado."
            );

            Comprobar(
                rail.GetComponentInParent<
                    EdificioConstruido>(true) == rampa,
                "El conector no pertenece a la rampa temporal."
            );

            Comprobar(
                rampa.edificiosDependientes.Count == 1 &&
                rampa.edificiosDependientes[0] ==
                    pared.gameObject,
                "La dependencia no se ha recuperado."
            );

            Debug.Log(
                "[GUARDADO] GRAFO VALIDO CONECTADO: " +
                "Pared -> Rampa, RailRampa ocupado, " +
                "dependencia registrada."
            );

            // ======================================
            // 8. SEGUNDA RESTAURACION
            // ======================================

            List<EdificioConstruido> segundaCarga =
                PlacedBuildingsBatchSpawnAdapter
                    .CrearORecuperarTodos(sistema, valida);

            Comprobar(
                segundaCarga.Count == 2 &&
                BuscarEdificio(idRampa) == rampa &&
                BuscarEdificio(idPared) == pared,
                "La segunda carga ha duplicado edificios."
            );

            int segundaConexion =
                PlacedBuildingsConnectionsRestoreAdapter
                    .Restaurar(valida);

            Comprobar(
                segundaConexion == 1 &&
                pared.conectorUsado == rail &&
                !rail.enabled &&
                rampa.edificiosDependientes.Count == 1,
                "La segunda carga ha alterado las conexiones."
            );

            Comprobar(
                ContarEdificios() == cantidadAntes + 2,
                "Se han duplicado las construcciones."
            );

            // ======================================
            // 9. COMPROBAR DATOS ORIGINALES
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
                string jsonOriginal;

                if (originales.TryGetValue(
                    edificio.idPersistente,
                    out jsonOriginal))
                {
                    Comprobar(
                        jsonOriginal ==
                            JsonUtility.ToJson(edificio),
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
                "No coinciden los edificios originales " +
                "o temporales."
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
                "[GUARDADO] PROTECCION GRAFO Y RECONSTRUCCION " +
                "SUPERADA: 3 partidas corruptas rechazadas " +
                "antes de crear edificios, 2 prefabs reales " +
                "restaurados, conexion recuperada, " +
                "segunda carga sin duplicados, " +
                "edificios originales e inventario intactos, " +
                "monedas=" + monedasAntes + "."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PROTECCION GRAFO Y RECONSTRUCCION " +
                "FALLIDA: " + ex
            );
        }
        finally
        {
            // Eliminar solo los edificios temporales
            // identificados por sus IDs de prueba.
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
        }
    }

    // ======================================
    // COMPROBAR RECHAZO SIN CONSTRUCCIONES
    // ======================================

    private static void VerificarRechazo(
        SistemaConstruccion sistema,
        GameSaveData partida,
        int cantidadEsperada,
        HashSet<string> idsTemporales,
        string descripcion)
    {
        bool rechazado = false;

        try
        {
            PlacedBuildingsBatchSpawnAdapter
                .CrearORecuperarTodos(sistema, partida);
        }
        catch (InvalidOperationException)
        {
            rechazado = true;
        }

        Comprobar(
            rechazado,
            descripcion + ": la partida fue aceptada."
        );

        Comprobar(
            ContarEdificios() == cantidadEsperada,
            descripcion + ": se han creado edificios."
        );

        foreach (string id in idsTemporales)
        {
            Comprobar(
                BuscarEdificio(id) == null,
                descripcion + ": existe un prefab temporal."
            );
        }

        Debug.Log(
            "[GUARDADO] RECONSTRUCCION BLOQUEADA OK: " +
            descripcion +
            ", ningun edificio creado."
        );
    }

    // ======================================
    // BUSCAR Y CONTAR EDIFICIOS
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

    // ======================================
    // COPIAR Y DESPLAZAR
    // ======================================

    private static GameSaveData CopiarPartida(
        GameSaveData partida)
    {
        return JsonUtility.FromJson<GameSaveData>(
            JsonUtility.ToJson(partida)
        );
    }

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
            throw new InvalidOperationException(mensaje);
    }
}

#endif
