
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrBuildingConnectionsRestoreTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar restaurar conexiones (Play)"
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

        GameObject objetoRampa = null;
        GameObject objetoPared = null;

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

            // Capturar los edificios reales para comprobar
            // que no los modificamos durante la prueba.
            GameSaveData escenaAntes = new GameSaveData();

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                escenaAntes
            );

            Dictionary<string, string> edificiosOriginales =
                new Dictionary<string, string>(
                    StringComparer.Ordinal
                );

            foreach (PlacedBuildingSaveData edificio
                in escenaAntes.placedBuildings.edificios)
            {
                edificiosOriginales.Add(
                    edificio.idPersistente,
                    JsonUtility.ToJson(edificio)
                );
            }

            // ==========================================
            // 1. CREAR RAMPA TEMPORAL
            // ==========================================

            objetoRampa = new GameObject(
                "__TEST_GUARDADO_RAMPA__"
            );

            objetoRampa.SetActive(false);

            EdificioConstruido rampa =
                objetoRampa.AddComponent<EdificioConstruido>();

            string idRampa =
                Guid.NewGuid().ToString("N");

            rampa.RestaurarIdentidadGuardada(
                idRampa,
                SistemaConstruccion.TipoEdificio.Rampa,
                0,
                0f
            );

            // Crear un conector con la misma estructura
            // observada en el juego: GameObject[0].
            Collider rail = CrearColliderHijo(
                objetoRampa,
                "GameObject",
                "RailRampa"
            );

            Comprobar(
                rail.transform.GetSiblingIndex() == 0,
                "El conector no tiene el indice esperado."
            );

            // ==========================================
            // 2. CREAR PARED TEMPORAL
            // ==========================================

            objetoPared = new GameObject(
                "__TEST_GUARDADO_PARED__"
            );

            objetoPared.SetActive(false);

            EdificioConstruido pared =
                objetoPared.AddComponent<EdificioConstruido>();

            string idPared =
                Guid.NewGuid().ToString("N");

            pared.RestaurarIdentidadGuardada(
                idPared,
                SistemaConstruccion.TipoEdificio.Pared,
                0,
                0f
            );

            Collider salida = CrearColliderHijo(
                objetoPared,
                "Salida",
                "ConectorParedSalida"
            );

            Collider entrada = CrearColliderHijo(
                objetoPared,
                "Entrada",
                "ConectorParedEntrada"
            );

            // Estado inicial: sin conexiones.
            Comprobar(
                pared.conectorUsado == null &&
                rampa.edificiosDependientes.Count == 0 &&
                rail.enabled &&
                salida.enabled &&
                entrada.enabled,
                "Los edificios temporales no estan limpios."
            );

            // ==========================================
            // 3. PREPARAR GUARDADO SIMULADO
            // ==========================================

            GameSaveData partida = new GameSaveData
            {
                sceneName =
                    SceneManager.GetActiveScene().name,

                hasPlacedBuildingsData = true,

                placedBuildings =
                    new PlacedBuildingsSaveData()
            };

            PlacedBuildingSaveData datosRampa =
                CrearDatosBasicos(
                    idRampa,
                    "Rampa"
                );

            PlacedBuildingSaveData datosPared =
                CrearDatosBasicos(
                    idPared,
                    "Pared"
                );

            datosPared.hasConnection = true;
            datosPared.connectedToBuildingId = idRampa;
            datosPared.connectorPath = "GameObject[0]";
            datosPared.connectorTag = "RailRampa";

            partida.placedBuildings.edificios.Add(
                datosRampa
            );

            partida.placedBuildings.edificios.Add(
                datosPared
            );

            // Simular guardado y lectura JSON.
            partida = JsonUtility.FromJson<GameSaveData>(
                JsonUtility.ToJson(partida)
            );

            PlacedBuildingsSaveAdapter.Read(partida);

            // ==========================================
            // 4. PROBAR UNA RUTA INCORRECTA
            // ==========================================

            GameSaveData invalida =
                JsonUtility.FromJson<GameSaveData>(
                    JsonUtility.ToJson(partida)
                );

            invalida.placedBuildings.edificios[1]
                .connectorPath = "Inexistente[99]";

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

            // El error debe detectarse antes de tocar
            // cualquier conector o dependencia.
            Comprobar(
                pared.conectorUsado == null &&
                rampa.edificiosDependientes.Count == 0 &&
                rail.enabled &&
                salida.enabled &&
                entrada.enabled,
                "El intento invalido ha modificado la escena."
            );

            Debug.Log(
                "[GUARDADO] CONEXION INVALIDA RECHAZADA: " +
                "sin modificaciones en los objetos."
            );

            // ==========================================
            // 5. RESTAURAR LA CONEXION CORRECTA
            // ==========================================

            int restauradas =
                PlacedBuildingsConnectionsRestoreAdapter
                    .Restaurar(partida);

            Comprobar(
                restauradas == 1,
                "Se esperaba una conexion restaurada."
            );

            Comprobar(
                pared.conectorUsado == rail,
                "La pared no reconoce el conector."
            );

            Comprobar(
                !rail.enabled,
                "El conector de la rampa sigue libre."
            );

            Comprobar(
                rampa.edificiosDependientes.Count == 1 &&
                rampa.edificiosDependientes[0] == objetoPared,
                "La rampa no registra correctamente la pared."
            );

            Comprobar(
                !salida.enabled && !entrada.enabled,
                "Los conectores de la pared siguen activos."
            );

            Debug.Log(
                "[GUARDADO] CONEXION RESTAURADA OK: " +
                "Pared -> Rampa, conector=GameObject[0], " +
                "ocupado=True, dependencia registrada."
            );

            // ==========================================
            // 6. REPETIR LA RESTAURACION
            // ==========================================

            int segundaRestauracion =
                PlacedBuildingsConnectionsRestoreAdapter
                    .Restaurar(partida);

            Comprobar(
                segundaRestauracion == 1,
                "La segunda restauracion ha fallado."
            );

            Comprobar(
                pared.conectorUsado == rail &&
                !rail.enabled &&
                rampa.edificiosDependientes.Count == 1 &&
                rampa.edificiosDependientes[0] == objetoPared &&
                !salida.enabled &&
                !entrada.enabled,
                "La segunda restauracion ha alterado " +
                "las conexiones o duplicado dependencias."
            );

            // ==========================================
            // 7. COMPROBAR ESCENA, MONEDAS Y MOCHILA
            // ==========================================

            Comprobar(
                cartera.Monedas == monedasOriginales,
                "Se han modificado las monedas."
            );

            ComprobarInventario(
                inventario,
                mochilaOriginal
            );

            GameSaveData escenaDespues =
                new GameSaveData();

            PlacedBuildingsSaveAdapter.Capture(
                sistema,
                escenaDespues
            );

            int encontradosOriginales = 0;

            foreach (PlacedBuildingSaveData edificio
                in escenaDespues.placedBuildings.edificios)
            {
                string jsonAnterior;

                if (!edificiosOriginales.TryGetValue(
                    edificio.idPersistente,
                    out jsonAnterior))
                {
                    // Solo los dos objetos temporales
                    // pueden ser nuevos.
                    Comprobar(
                        edificio.idPersistente == idRampa ||
                        edificio.idPersistente == idPared,
                        "Ha aparecido un edificio inesperado."
                    );

                    continue;
                }

                Comprobar(
                    jsonAnterior == JsonUtility.ToJson(edificio),
                    "Ha cambiado un edificio real: " +
                    edificio.idPersistente
                );

                encontradosOriginales++;
            }

            Comprobar(
                encontradosOriginales ==
                    edificiosOriginales.Count,
                "Ha desaparecido un edificio original."
            );

            Debug.Log(
                "[GUARDADO] RESTAURACION CONEXIONES SUPERADA: " +
                "Pared -> RailRampa restaurada, " +
                "colliders correctos, dependencia unica, " +
                "segunda restauracion sin duplicados, " +
                "ruta invalida rechazada, edificios reales, " +
                "inventario y monedas intactos=" +
                monedasOriginales + "."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] RESTAURACION CONEXIONES FALLIDA: " +
                ex
            );
        }
        finally
        {
            // Retirar exclusivamente los objetos
            // temporales creados por esta prueba.
            if (objetoPared != null)
            {
                objetoPared.SetActive(false);
                UnityEngine.Object.Destroy(objetoPared);
            }

            if (objetoRampa != null)
            {
                objetoRampa.SetActive(false);
                UnityEngine.Object.Destroy(objetoRampa);
            }
        }
    }

    // ==========================================
    // CREAR COLLIDER TEMPORAL
    // ==========================================

    private static Collider CrearColliderHijo(
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

    // ==========================================
    // DATOS DE EDIFICIO
    // ==========================================

    private static PlacedBuildingSaveData CrearDatosBasicos(
        string id,
        string tipo)
    {
        return new PlacedBuildingSaveData
        {
            idPersistente = id,
            tipo = tipo,
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

    // ==========================================
    // INVENTARIO
    // ==========================================

    private static Dictionary<
        SistemaConstruccion.TipoEdificio, int>
        LeerInventario(InventarioEdificios inventario)
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
        {
            throw new InvalidOperationException(mensaje);
        }
    }
}

#endif
