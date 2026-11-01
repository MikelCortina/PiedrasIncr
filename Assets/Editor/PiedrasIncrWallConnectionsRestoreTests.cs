
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrWallConnectionsRestoreTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar conexiones paredes (Play)"
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

        List<GameObject> temporales =
            new List<GameObject>();

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

            Dictionary<SistemaConstruccion.TipoEdificio, int>
                inventarioAntes =
                    CapturarInventario(inventario);

            // ======================================
            // 1. GUARDAR ESTADO ORIGINAL
            // ======================================

            GameSaveData escenaAntes = new GameSaveData();

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                escenaAntes
            );

            Dictionary<string, string> originales =
                new Dictionary<string, string>(
                    StringComparer.Ordinal
                );

            foreach (PlacedBuildingSaveData edificio
                in escenaAntes.placedBuildings.edificios)
            {
                originales.Add(
                    edificio.idPersistente,
                    JsonUtility.ToJson(edificio)
                );
            }

            // ======================================
            // 2. CREAR CUATRO PAREDES TEMPORALES
            // ======================================

            string idPadreSalida =
                Guid.NewGuid().ToString("N");

            string idHijaEntrada =
                Guid.NewGuid().ToString("N");

            string idPadreEntrada =
                Guid.NewGuid().ToString("N");

            string idHijaSalida =
                Guid.NewGuid().ToString("N");

            EdificioConstruido padreSalida =
                CrearPared(
                    "__TEST_PARED_PADRE_SALIDA__",
                    idPadreSalida,
                    temporales
                );

            EdificioConstruido hijaEntrada =
                CrearPared(
                    "__TEST_PARED_HIJA_ENTRADA__",
                    idHijaEntrada,
                    temporales
                );

            EdificioConstruido padreEntrada =
                CrearPared(
                    "__TEST_PARED_PADRE_ENTRADA__",
                    idPadreEntrada,
                    temporales
                );

            EdificioConstruido hijaSalida =
                CrearPared(
                    "__TEST_PARED_HIJA_SALIDA__",
                    idHijaSalida,
                    temporales
                );

            // Caso A:
            // Salida del padre -> Entrada de la hija.

            Collider salidaPadre = CrearConector(
                padreSalida.gameObject,
                "ConexionParedSalida",
                "ConectorParedSalida"
            );

            Collider entradaHija = CrearConector(
                hijaEntrada.gameObject,
                "PuntoConexionPared_Entrada",
                "Untagged"
            );

            // Caso B:
            // Entrada del padre -> Salida de la hija.

            Collider entradaPadre = CrearConector(
                padreEntrada.gameObject,
                "ConexionParedEntrada",
                "ConectorParedEntrada"
            );

            Collider salidaHija = CrearConector(
                hijaSalida.gameObject,
                "PuntoConexionPared_Salida",
                "Untagged"
            );

            ComprobarEstadoInicial(
                padreSalida,
                hijaEntrada,
                padreEntrada,
                hijaSalida,
                salidaPadre,
                entradaHija,
                entradaPadre,
                salidaHija
            );

            // ======================================
            // 3. CREAR PARTIDA DE PRUEBA
            // ======================================

            GameSaveData partida = new GameSaveData
            {
                sceneName =
                    SceneManager.GetActiveScene().name,

                hasPlacedBuildingsData = true,

                placedBuildings =
                    new PlacedBuildingsSaveData()
            };

            partida.placedBuildings.edificios.Add(
                CrearRegistro(idPadreSalida)
            );

            PlacedBuildingSaveData registroHijaEntrada =
                CrearRegistro(idHijaEntrada);

            registroHijaEntrada.hasConnection = true;

            registroHijaEntrada.connectedToBuildingId =
                idPadreSalida;

            registroHijaEntrada.connectorPath =
                "ConexionParedSalida[0]";

            registroHijaEntrada.connectorTag =
                "ConectorParedSalida";

            partida.placedBuildings.edificios.Add(
                registroHijaEntrada
            );

            partida.placedBuildings.edificios.Add(
                CrearRegistro(idPadreEntrada)
            );

            PlacedBuildingSaveData registroHijaSalida =
                CrearRegistro(idHijaSalida);

            registroHijaSalida.hasConnection = true;

            registroHijaSalida.connectedToBuildingId =
                idPadreEntrada;

            registroHijaSalida.connectorPath =
                "ConexionParedEntrada[0]";

            registroHijaSalida.connectorTag =
                "ConectorParedEntrada";

            partida.placedBuildings.edificios.Add(
                registroHijaSalida
            );

            // Comprobar serializacion JSON.
            partida = Copiar(partida);

            PlacedBuildingsSaveAdapter.Read(partida);

            // ======================================
            // 4. RECHAZAR RUTA INCORRECTA
            // ======================================

            GameSaveData rutaIncorrecta =
                Copiar(partida);

            BuscarRegistro(
                rutaIncorrecta,
                idHijaEntrada
            ).connectorPath = "Inexistente[99]";

            bool rutaRechazada = false;

            try
            {
                PlacedBuildingsConnectionsRestoreAdapter
                    .Restaurar(rutaIncorrecta);
            }
            catch (InvalidOperationException)
            {
                rutaRechazada = true;
            }

            Comprobar(
                rutaRechazada,
                "Se ha aceptado una ruta incorrecta."
            );

            ComprobarEstadoInicial(
                padreSalida,
                hijaEntrada,
                padreEntrada,
                hijaSalida,
                salidaPadre,
                entradaHija,
                entradaPadre,
                salidaHija
            );

            Debug.Log(
                "[GUARDADO] PAREDES RUTA INVALIDA RECHAZADA."
            );

            // ======================================
            // 5. RECHAZAR CONECTOR DUPLICADO
            // ======================================

            GameSaveData duplicada = Copiar(partida);

            PlacedBuildingSaveData duplicado =
                BuscarRegistro(
                    duplicada,
                    idHijaSalida
                );

            duplicado.connectedToBuildingId =
                idPadreSalida;

            duplicado.connectorPath =
                "ConexionParedSalida[0]";

            duplicado.connectorTag =
                "ConectorParedSalida";

            bool duplicadoRechazado = false;

            try
            {
                PlacedBuildingsConnectionsRestoreAdapter
                    .Restaurar(duplicada);
            }
            catch (InvalidOperationException)
            {
                duplicadoRechazado = true;
            }

            Comprobar(
                duplicadoRechazado,
                "Dos paredes han ocupado " +
                "el mismo conector."
            );

            ComprobarEstadoInicial(
                padreSalida,
                hijaEntrada,
                padreEntrada,
                hijaSalida,
                salidaPadre,
                entradaHija,
                entradaPadre,
                salidaHija
            );

            Debug.Log(
                "[GUARDADO] PAREDES CONECTOR DUPLICADO RECHAZADO."
            );

            // ======================================
            // 6. RESTAURAR LAS DOS CONEXIONES
            // ======================================

            int restauradas =
                PlacedBuildingsConnectionsRestoreAdapter
                    .Restaurar(partida);

            Comprobar(
                restauradas == 2,
                "No se han restaurado ambas conexiones."
            );

            VerificarConexion(
                padreSalida,
                hijaEntrada,
                salidaPadre,
                entradaHija,
                "SALIDA -> ENTRADA"
            );

            VerificarConexion(
                padreEntrada,
                hijaSalida,
                entradaPadre,
                salidaHija,
                "ENTRADA -> SALIDA"
            );

            // ======================================
            // 7. REPETIR LA RESTAURACION
            // ======================================

            int segundaCarga =
                PlacedBuildingsConnectionsRestoreAdapter
                    .Restaurar(partida);

            Comprobar(
                segundaCarga == 2,
                "La segunda restauracion ha fallado."
            );

            VerificarConexion(
                padreSalida,
                hijaEntrada,
                salidaPadre,
                entradaHija,
                "SALIDA -> ENTRADA, SEGUNDA CARGA"
            );

            VerificarConexion(
                padreEntrada,
                hijaSalida,
                entradaPadre,
                salidaHija,
                "ENTRADA -> SALIDA, SEGUNDA CARGA"
            );

            // ======================================
            // 8. COMPROBAR ECONOMIA Y ESCENA
            // ======================================

            Comprobar(
                cartera.Monedas == monedasAntes,
                "Las monedas han cambiado."
            );

            ComprobarInventario(
                inventario,
                inventarioAntes
            );

            GameSaveData escenaDespues =
                new GameSaveData();

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                escenaDespues
            );

            HashSet<string> idsTemporales =
                new HashSet<string>
                {
                    idPadreSalida,
                    idHijaEntrada,
                    idPadreEntrada,
                    idHijaSalida
                };

            int verificados = 0;

            foreach (PlacedBuildingSaveData edificio
                in escenaDespues.placedBuildings.edificios)
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

                    verificados++;
                }
                else
                {
                    Comprobar(
                        idsTemporales.Contains(
                            edificio.idPersistente),
                        "Ha aparecido un edificio inesperado."
                    );
                }
            }

            Comprobar(
                verificados == originales.Count,
                "Ha desaparecido un edificio original."
            );

            Debug.Log(
                "[GUARDADO] RESTAURACION PAREDES SUPERADA: " +
                "2 conexiones, entrada y salida correctas, " +
                "conectores bloqueados, referencias unicas, " +
                "segunda carga sin duplicados, " +
                "datos invalidos rechazados, " +
                "escena e inventario intactos, monedas=" +
                monedasAntes + "."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] RESTAURACION PAREDES FALLIDA: " +
                ex
            );
        }
        finally
        {
            // Destruir exclusivamente las paredes
            // temporales creadas por esta prueba.
            foreach (GameObject objeto in temporales)
            {
                if (objeto == null)
                    continue;

                objeto.SetActive(false);

                UnityEngine.Object.Destroy(objeto);
            }
        }
    }

    // ======================================
    // CREAR PARED TEMPORAL
    // ======================================

    private static EdificioConstruido CrearPared(
        string nombre,
        string id,
        List<GameObject> temporales)
    {
        GameObject objeto = new GameObject(nombre);

        temporales.Add(objeto);

        objeto.SetActive(false);

        EdificioConstruido edificio =
            objeto.AddComponent<EdificioConstruido>();

        edificio.RestaurarIdentidadGuardada(
            id,
            SistemaConstruccion.TipoEdificio.Pared,
            0,
            0f
        );

        return edificio;
    }

    // ======================================
    // CREAR CONECTOR
    // ======================================

    private static Collider CrearConector(
        GameObject padre,
        string nombre,
        string tag)
    {
        GameObject hijo = new GameObject(nombre);

        hijo.transform.SetParent(
            padre.transform,
            false
        );

        hijo.tag = tag;

        return hijo.AddComponent<BoxCollider>();
    }

    // ======================================
    // CREAR REGISTRO DE GUARDADO
    // ======================================

    private static PlacedBuildingSaveData CrearRegistro(
        string id)
    {
        return new PlacedBuildingSaveData
        {
            idPersistente = id,
            tipo = "Pared",
            variantePrefab = 0,
            curvaturaRampa = 0f,

            positionX = 0f,
            positionY = 0f,
            positionZ = 0f,

            rotationX = 0f,
            rotationY = 0f,
            rotationZ = 0f,
            rotationW = 1f,

            scaleX = 1f,
            scaleY = 1f,
            scaleZ = 1f,

            hasConnection = false
        };
    }

    private static GameSaveData Copiar(
        GameSaveData partida)
    {
        return JsonUtility.FromJson<GameSaveData>(
            JsonUtility.ToJson(partida)
        );
    }

    private static PlacedBuildingSaveData BuscarRegistro(
        GameSaveData partida,
        string id)
    {
        foreach (PlacedBuildingSaveData registro
            in partida.placedBuildings.edificios)
        {
            if (registro.idPersistente == id)
                return registro;
        }

        throw new InvalidOperationException(
            "No se encuentra el registro: " + id
        );
    }

    // ======================================
    // VERIFICAR ESTADO INICIAL
    // ======================================

    private static void ComprobarEstadoInicial(
        EdificioConstruido padreA,
        EdificioConstruido hijaA,
        EdificioConstruido padreB,
        EdificioConstruido hijaB,
        Collider conectorPadreA,
        Collider conectorHijaA,
        Collider conectorPadreB,
        Collider conectorHijaB)
    {
        Comprobar(
            hijaA.conectorUsado == null &&
            hijaB.conectorUsado == null &&
            padreA.conectoresHijosBloqueados.Count == 0 &&
            padreB.conectoresHijosBloqueados.Count == 0 &&
            conectorPadreA.enabled &&
            conectorHijaA.enabled &&
            conectorPadreB.enabled &&
            conectorHijaB.enabled,
            "Una prueba invalida ha modificado conexiones."
        );
    }

    // ======================================
    // VERIFICAR CONEXION
    // ======================================

    private static void VerificarConexion(
        EdificioConstruido padre,
        EdificioConstruido hija,
        Collider conectorPadre,
        Collider conectorHija,
        string descripcion)
    {
        Comprobar(
            hija.conectorUsado == conectorPadre,
            descripcion + ": conector incorrecto."
        );

        Comprobar(
            !conectorPadre.enabled &&
            !conectorHija.enabled,
            descripcion + ": conectores no bloqueados."
        );

        Comprobar(
            padre.conectoresHijosBloqueados.Count == 1 &&
            padre.conectoresHijosBloqueados[0] ==
                conectorHija,
            descripcion + ": referencia bloqueada incorrecta."
        );

        Debug.Log(
            "[GUARDADO] PARED CONEXION OK " +
            descripcion +
            ": conector padre ocupado, " +
            "conector hijo bloqueado, " +
            "referencia registrada sin duplicados."
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
