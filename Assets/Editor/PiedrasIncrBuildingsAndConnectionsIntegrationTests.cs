
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrBuildingsAndConnectionsIntegrationTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar edificios y conexiones juntos (Play)"
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
                "No se encuentra la Cartera."
            );

            int monedasAntes = cartera.Monedas;

            var inventarioAntes =
                CapturarInventario(inventario);

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

            var originales =
                new Dictionary<string, string>(
                    StringComparer.Ordinal
                );

            var registros =
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

                registros.Add(
                    edificio.idPersistente,
                    edificio
                );
            }

            // Buscar una pared conectada a una rampa
            // recta que exista realmente en la escena.
            PlacedBuildingSaveData paredOriginal = null;
            PlacedBuildingSaveData rampaOriginal = null;

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

                if (!registros.TryGetValue(
                    edificio.connectedToBuildingId,
                    out candidata))
                {
                    continue;
                }

                if (candidata.tipo != "Rampa" ||
                    candidata.curvaturaRampa != 0f)
                {
                    continue;
                }

                paredOriginal = edificio;
                rampaOriginal = candidata;
                break;
            }

            Comprobar(
                paredOriginal != null &&
                rampaOriginal != null,
                "Necesitas una pared conectada mediante " +
                "RailRampa a una rampa sin curvatura."
            );

            // ======================================
            // 2. PREPARAR COPIAS CON NUEVOS IDS
            // ======================================

            GameSaveData partida = new GameSaveData
            {
                sceneName =
                    SceneManager.GetActiveScene().name,

                hasPlacedBuildingsData = true,

                placedBuildings =
                    new PlacedBuildingsSaveData()
            };

            PlacedBuildingSaveData rampaGuardada =
                CopiarEdificio(rampaOriginal);

            PlacedBuildingSaveData paredGuardada =
                CopiarEdificio(paredOriginal);

            string idRampa =
                Guid.NewGuid().ToString("N");

            string idPared =
                Guid.NewGuid().ToString("N");

            idsTemporales.Add(idRampa);
            idsTemporales.Add(idPared);

            rampaGuardada.idPersistente = idRampa;
            paredGuardada.idPersistente = idPared;

            // Aislar la rampa de las conexiones
            // que pudiera tener en la escena real.
            rampaGuardada.hasConnection = false;
            rampaGuardada.connectedToBuildingId = null;
            rampaGuardada.connectorPath = null;
            rampaGuardada.connectorTag = null;

            // La pared apuntara a la nueva rampa,
            // no a la rampa original.
            paredGuardada.connectedToBuildingId = idRampa;

            // Mismo desplazamiento para conservar
            // la posicion relativa de los dos edificios.
            Vector3 desplazamiento =
                new Vector3(40f, 25f, 40f);

            Desplazar(rampaGuardada, desplazamiento);
            Desplazar(paredGuardada, desplazamiento);

            partida.placedBuildings.edificios.Add(
                rampaGuardada
            );

            partida.placedBuildings.edificios.Add(
                paredGuardada
            );

            // Comprobar la serializacion JSON.
            partida = JsonUtility.FromJson<GameSaveData>(
                JsonUtility.ToJson(partida)
            );

            PlacedBuildingsSaveAdapter.Read(partida);

            // ======================================
            // 3. RECONSTRUIR PREFABS REALES
            // ======================================

            List<EdificioConstruido> reconstruidos =
                PlacedBuildingsBatchSpawnAdapter
                    .CrearORecuperarTodos(sistema, partida);

            Comprobar(
                reconstruidos.Count == 2,
                "No se han reconstruido los dos edificios."
            );

            EdificioConstruido rampa =
                BuscarEdificio(idRampa);

            EdificioConstruido pared =
                BuscarEdificio(idPared);

            Comprobar(
                rampa != null && pared != null,
                "No se encuentran los edificios reconstruidos."
            );

            Comprobar(
                rampa.tipo ==
                    SistemaConstruccion.TipoEdificio.Rampa &&
                pared.tipo ==
                    SistemaConstruccion.TipoEdificio.Pared,
                "Los tipos reconstruidos son incorrectos."
            );

            // Detener cualquier comportamiento de las
            // copias. El adaptador de conexiones puede
            // trabajar con objetos inactivos.
            rampa.gameObject.SetActive(false);
            pared.gameObject.SetActive(false);

            ComprobarTransformacion(
                rampa,
                rampaGuardada
            );

            ComprobarTransformacion(
                pared,
                paredGuardada
            );

            Comprobar(
                pared.conectorUsado == null,
                "La pared temporal ya tiene una conexion."
            );

            Collider conectorEsperado =
                ResolverConector(
                    rampa.transform,
                    paredGuardada.connectorPath
                );

            Comprobar(
                conectorEsperado.CompareTag("RailRampa"),
                "El prefab no contiene el RailRampa esperado."
            );

            bool estadoInicialConector =
                conectorEsperado.enabled;

            Debug.Log(
                "[GUARDADO] PREFABS CONECTADOS CREADOS: " +
                "Rampa=" + idRampa +
                ", Pared=" + idPared +
                ", transformaciones correctas."
            );

            // ======================================
            // 4. RECHAZAR UNA RUTA INCORRECTA
            // ======================================

            GameSaveData invalida =
                JsonUtility.FromJson<GameSaveData>(
                    JsonUtility.ToJson(partida)
                );

            invalida.placedBuildings.edificios[1]
                .connectorPath = "Inexistente[999]";

            bool rechazado = false;

            try
            {
                PlacedBuildingsConnectionsRestoreAdapter
                    .Restaurar(invalida);
            }
            catch (InvalidOperationException)
            {
                rechazado = true;
            }

            Comprobar(
                rechazado,
                "Se ha aceptado una ruta incorrecta."
            );

            Comprobar(
                pared.conectorUsado == null &&
                conectorEsperado.enabled ==
                    estadoInicialConector &&
                rampa.edificiosDependientes.Count == 0,
                "El intento invalido modifico las conexiones."
            );

            // ======================================
            // 5. RESTAURAR CONEXION REAL
            // ======================================

            int restauradas =
                PlacedBuildingsConnectionsRestoreAdapter
                    .Restaurar(partida);

            Comprobar(
                restauradas == 1,
                "No se ha restaurado una conexion."
            );

            VerificarConexion(
                rampa,
                pared,
                conectorEsperado
            );

            Debug.Log(
                "[GUARDADO] CONEXION ENTRE PREFABS OK: " +
                "Pared -> Rampa, conector=" +
                paredGuardada.connectorPath +
                ", ocupado=True, dependencia restaurada."
            );

            // ======================================
            // 6. REPETIR AMBAS RESTAURACIONES
            // ======================================

            List<EdificioConstruido> segundaCarga =
                PlacedBuildingsBatchSpawnAdapter
                    .CrearORecuperarTodos(sistema, partida);

            Comprobar(
                segundaCarga.Count == 2 &&
                BuscarEdificio(idRampa) == rampa &&
                BuscarEdificio(idPared) == pared,
                "Se han duplicado los edificios."
            );

            int conexionesSegundaCarga =
                PlacedBuildingsConnectionsRestoreAdapter
                    .Restaurar(partida);

            Comprobar(
                conexionesSegundaCarga == 1,
                "Fallo al restaurar por segunda vez."
            );

            VerificarConexion(
                rampa,
                pared,
                conectorEsperado
            );

            // ======================================
            // 7. VERIFICAR ESCENA E INVENTARIO
            // ======================================

            Comprobar(
                cartera.Monedas == monedasAntes,
                "Se han modificado las monedas."
            );

            ComprobarInventario(
                inventario,
                inventarioAntes
            );

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
                "La cantidad de edificios es incorrecta."
            );

            Debug.Log(
                "[GUARDADO] INTEGRACION EDIFICIOS Y CONEXIONES " +
                "SUPERADA: 2 prefabs reales reconstruidos, " +
                "IDs y transformaciones correctos, " +
                "conexion Pared -> Rampa recuperada, " +
                "conector ocupado, dependencia unica, " +
                "segunda carga sin duplicados, " +
                "ruta invalida rechazada, " +
                "edificios originales e inventario intactos, " +
                "monedas=" + monedasAntes + "."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] INTEGRACION EDIFICIOS Y CONEXIONES " +
                "FALLIDA: " + ex
            );
        }
        finally
        {
            // Destruir exclusivamente los objetos
            // identificados como temporales.
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
    // COPIAR Y DESPLAZAR DATOS
    // ======================================

    private static PlacedBuildingSaveData CopiarEdificio(
        PlacedBuildingSaveData datos)
    {
        return JsonUtility.FromJson<PlacedBuildingSaveData>(
            JsonUtility.ToJson(datos)
        );
    }

    private static void Desplazar(
        PlacedBuildingSaveData datos,
        Vector3 desplazamiento)
    {
        datos.positionX += desplazamiento.x;
        datos.positionY += desplazamiento.y;
        datos.positionZ += desplazamiento.z;
    }

    // ======================================
    // BUSCAR EDIFICIO POR ID
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

    private static void ComprobarTransformacion(
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
            Vector3.Distance(
                edificio.transform.position,
                posicion
            ) < 0.001f,
            "La posicion reconstruida es incorrecta."
        );

        Comprobar(
            Quaternion.Angle(
                edificio.transform.rotation,
                rotacion
            ) < 0.1f,
            "La rotacion reconstruida es incorrecta."
        );

        Comprobar(
            Vector3.Distance(
                edificio.transform.localScale,
                escala
            ) < 0.001f,
            "La escala reconstruida es incorrecta."
        );

        Comprobar(
            edificio.IdPersistente == datos.idPersistente &&
            edificio.VariantePrefab == datos.variantePrefab,
            "La identidad o variante no coincide."
        );
    }

    // ======================================
    // LOCALIZAR CONECTOR GUARDADO
    // ======================================

    private static Collider ResolverConector(
        Transform raiz,
        string ruta)
    {
        Comprobar(
            raiz != null &&
            !string.IsNullOrEmpty(ruta),
            "Ruta de conector invalida."
        );

        Transform actual = raiz;

        if (ruta != "(raiz)")
        {
            string[] partes = ruta.Split('/');

            foreach (string parte in partes)
            {
                int apertura = parte.LastIndexOf('[');

                Comprobar(
                    apertura > 0 &&
                    parte.EndsWith("]"),
                    "Formato de ruta incorrecto."
                );

                string nombre =
                    parte.Substring(0, apertura);

                string indiceTexto = parte.Substring(
                    apertura + 1,
                    parte.Length - apertura - 2
                );

                int indice;

                Comprobar(
                    int.TryParse(
                        indiceTexto, out indice) &&
                    indice >= 0 &&
                    indice < actual.childCount,
                    "Indice de conector invalido."
                );

                actual = actual.GetChild(indice);

                Comprobar(
                    actual.name == nombre,
                    "Nombre de conector incorrecto."
                );
            }
        }

        Collider resultado =
            actual.GetComponent<Collider>();

        Comprobar(
            resultado != null,
            "El conector no tiene Collider."
        );

        return resultado;
    }

    // ======================================
    // COMPROBAR CONEXION
    // ======================================

    private static void VerificarConexion(
        EdificioConstruido rampa,
        EdificioConstruido pared,
        Collider conector)
    {
        Comprobar(
            pared.conectorUsado == conector,
            "La pared no reconoce el conector."
        );

        Comprobar(
            !conector.enabled,
            "El conector de la rampa sigue libre."
        );

        Comprobar(
            rampa.edificiosDependientes.Count == 1 &&
            rampa.edificiosDependientes[0] ==
                pared.gameObject,
            "La dependencia no es correcta o esta duplicada."
        );

        Collider[] colliders =
            pared.GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("ConectorParedEntrada") ||
                collider.CompareTag("ConectorParedSalida"))
            {
                Comprobar(
                    !collider.enabled,
                    "Un conector de la pared sigue activo."
                );
            }
        }
    }

    // ======================================
    // INVENTARIO
    // ======================================

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
                "El inventario ha cambiado: " + par.Key
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
