
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrBuildingConnectionsSaveTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar guardar conexiones (Play)"
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
            "BuildingConnections_" +
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

            int monedasOriginales = cartera.Monedas;

            Dictionary<SistemaConstruccion.TipoEdificio, int>
                mochilaOriginal = LeerInventario(inventario);

            // ======================================
            // 1. CAPTURAR LA ESCENA REAL
            // ======================================

            GameSaveData partida = new GameSaveData
            {
                sceneName = SceneManager.GetActiveScene().name
            };

            EconomySaveAdapter.Capture(cartera, partida);

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                partida
            );

            PlacedBuildingsSaveData originales =
                PlacedBuildingsSaveAdapter.Read(partida);

            Comprobar(
                originales.edificios.Count >= 2,
                "Coloca al menos 2 edificios."
            );

            int conexionesOriginales = 0;

            foreach (PlacedBuildingSaveData edificio
                in originales.edificios)
            {
                if (edificio.hasConnection)
                    conexionesOriginales++;
            }

            Comprobar(
                conexionesOriginales > 0,
                "No hay conexiones para probar. " +
                "Coloca una pared enganchada a una rampa."
            );

            // ======================================
            // 2. GUARDAR Y LEER JSON
            // ======================================

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
                "No se pudo leer el guardado."
            );

            Comprobar(
                !desdeRespaldo,
                "Se ha utilizado un respaldo inesperado."
            );

            Comprobar(
                cargada.sceneName == partida.sceneName,
                "El guardado pertenece a otra escena."
            );

            PlacedBuildingsSaveData leidos =
                PlacedBuildingsSaveAdapter.Read(cargada);

            Comprobar(
                JsonUtility.ToJson(originales) ==
                    JsonUtility.ToJson(leidos),
                "Los edificios leidos no coinciden " +
                "con los originales."
            );

            // ======================================
            // 3. COMPROBAR LAS CONEXIONES
            // ======================================

            Dictionary<string, PlacedBuildingSaveData>
                mapaGuardado =
                    new Dictionary<string, PlacedBuildingSaveData>();

            foreach (PlacedBuildingSaveData edificio
                in leidos.edificios)
            {
                mapaGuardado.Add(
                    edificio.idPersistente,
                    edificio
                );
            }

            int conexionesComprobadas = 0;
            int conectoresOcupados = 0;

            foreach (PlacedBuildingSaveData datos
                in leidos.edificios)
            {
                if (!datos.hasConnection)
                    continue;

                // El propietario debe estar en el JSON.
                Comprobar(
                    mapaGuardado.ContainsKey(
                        datos.connectedToBuildingId
                    ),
                    "Falta el propietario de una conexion."
                );

                EdificioConstruido hijo =
                    BuscarEdificio(datos.idPersistente);

                EdificioConstruido propietario =
                    BuscarEdificio(datos.connectedToBuildingId);

                Comprobar(
                    hijo != null && propietario != null,
                    "No se encuentran los edificios conectados."
                );

                Collider conectorReal = hijo.conectorUsado;

                Comprobar(
                    conectorReal != null,
                    "El edificio no tiene conector utilizado."
                );

                EdificioConstruido propietarioReal =
                    conectorReal.GetComponentInParent<
                        EdificioConstruido>(true);

                Comprobar(
                    propietarioReal == propietario,
                    "El conector pertenece a otro edificio."
                );

                // Recuperar el conector utilizando
                // exclusivamente la ruta guardada.
                Collider conectorRecuperado =
                    ResolverConector(
                        propietario.transform,
                        datos.connectorPath
                    );

                Comprobar(
                    conectorRecuperado == conectorReal,
                    "La ruta guardada no identifica " +
                    "el conector correcto."
                );

                Comprobar(
                    conectorRecuperado.gameObject.tag ==
                        datos.connectorTag,
                    "El tag del conector no coincide."
                );

                if (!conectorRecuperado.enabled)
                    conectoresOcupados++;

                conexionesComprobadas++;

                Debug.Log(
                    "[GUARDADO] CONEXION JSON OK: " +
                    hijo.tipo +
                    " (" + hijo.IdPersistente + ")" +
                    " -> " + propietario.tipo +
                    " (" + propietario.IdPersistente + ")" +
                    ", ruta=" + datos.connectorPath +
                    ", tag=" + datos.connectorTag +
                    ", ocupado=" +
                    (!conectorRecuperado.enabled)
                );
            }

            Comprobar(
                conexionesComprobadas == conexionesOriginales,
                "La cantidad de conexiones ha cambiado."
            );

            // ======================================
            // 4. COMPROBAR QUE LA ESCENA NO CAMBIO
            // ======================================

            GameSaveData segundaCaptura =
                new GameSaveData();

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                segundaCaptura
            );

            Comprobar(
                JsonUtility.ToJson(originales) ==
                JsonUtility.ToJson(
                    PlacedBuildingsSaveAdapter.Read(
                        segundaCaptura
                    )
                ),
                "La escena ha cambiado durante la prueba."
            );

            Comprobar(
                cartera.Monedas == monedasOriginales &&
                cargada.economy.coins == monedasOriginales,
                "Las monedas han cambiado."
            );

            ComprobarInventario(
                inventario,
                mochilaOriginal
            );

            Debug.Log(
                "[GUARDADO] CONEXIONES JSON SUPERADAS: " +
                leidos.edificios.Count +
                " edificios, " +
                conexionesComprobadas +
                " conexiones verificadas, " +
                conectoresOcupados +
                " conectores deshabilitados, " +
                "IDs, rutas y tags correctos, " +
                "escena, inventario y monedas intactos=" +
                monedasOriginales + "."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] CONEXIONES JSON FALLIDAS: " +
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

    // ======================================
    // RECUPERAR CONECTOR DESDE LA RUTA
    // ======================================

    private static Collider ResolverConector(
        Transform raiz,
        string ruta)
    {
        if (ruta == "(raiz)")
        {
            Collider colliderRaiz =
                raiz.GetComponent<Collider>();

            Comprobar(
                colliderRaiz != null,
                "La raiz no tiene Collider."
            );

            return colliderRaiz;
        }

        Comprobar(
            !string.IsNullOrEmpty(ruta),
            "La ruta del conector esta vacia."
        );

        Transform actual = raiz;

        string[] partes = ruta.Split('/');

        foreach (string parte in partes)
        {
            int apertura = parte.LastIndexOf('[');

            Comprobar(
                apertura > 0 && parte.EndsWith("]"),
                "Formato de ruta invalido: " + parte
            );

            string nombre = parte.Substring(0, apertura);

            string textoIndice = parte.Substring(
                apertura + 1,
                parte.Length - apertura - 2
            );

            int indice;

            Comprobar(
                int.TryParse(textoIndice, out indice),
                "Indice de conector invalido."
            );

            Comprobar(
                indice >= 0 && indice < actual.childCount,
                "El conector no existe en esa posicion."
            );

            actual = actual.GetChild(indice);

            Comprobar(
                actual.name == nombre,
                "El nombre del conector no coincide."
            );
        }

        Collider resultado = actual.GetComponent<Collider>();

        Comprobar(
            resultado != null,
            "El conector recuperado no tiene Collider."
        );

        return resultado;
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
    // INVENTARIO
    // ======================================

    private static Dictionary<
        SistemaConstruccion.TipoEdificio, int>
        LeerInventario(InventarioEdificios inventario)
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
            throw new InvalidOperationException(mensaje);
    }
}

#endif
