
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrBuildingInventoryRestoreTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar RESTAURAR inventario edificios (Play)"
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

        string carpeta = Path.Combine(
            Path.GetTempPath(),
            "PiedrasIncr_InventoryRestore_" +
            Guid.NewGuid().ToString("N")
        );

        InventarioEdificios inventario = null;
        Cartera cartera = null;

        Dictionary<SistemaConstruccion.TipoEdificio, int>
            estadoOriginal = null;

        int monedasOriginales = 0;
        bool estadoModificado = false;
        bool pruebaSuperada = false;
        bool recuperacionCorrecta = true;

        try
        {
            // LOCALIZAR INVENTARIO REAL
            var inventarios =
                UnityEngine.Object.FindObjectsByType<InventarioEdificios>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            Comprobar(
                inventarios.Length == 1,
                "Se esperaba un inventario, encontrados: " +
                inventarios.Length
            );

            inventario = inventarios[0];
            cartera = inventario.cartera;

            if (cartera == null)
                cartera = inventario.GetComponentInParent<Cartera>();

            if (cartera == null)
            {
                var carteras =
                    UnityEngine.Object.FindObjectsByType<Cartera>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None
                    );

                if (carteras.Length == 1)
                    cartera = carteras[0];
            }

            Comprobar(
                cartera != null,
                "No se encuentra la Cartera del jugador."
            );

            monedasOriginales = cartera.Monedas;

            // GUARDAR ESTADO ORIGINAL EN MEMORIA
            estadoOriginal = LeerCantidades(inventario);

            // CAPTURAR UNA PARTIDA DE DIAGNOSTICO
            GameSaveData partida = new GameSaveData
            {
                sceneName = SceneManager.GetActiveScene().name
            };

            EconomySaveAdapter.Capture(cartera, partida);

            BuildingInventorySaveAdapter.Capture(
                inventario, partida
            );

            // GUARDAR Y LEER JSON REAL
            SaveStorage storage = new SaveStorage(carpeta);

            storage.Save(1, partida);

            GameSaveData cargada;
            bool desdeRespaldo;

            Comprobar(
                storage.TryLoad(
                    1, out cargada, out desdeRespaldo
                ),
                "No se ha podido leer el guardado."
            );

            Comprobar(
                !desdeRespaldo,
                "Se ha cargado un respaldo inesperado."
            );

            Comprobar(
                cargada.sceneName ==
                    SceneManager.GetActiveScene().name,
                "El archivo pertenece a otra escena."
            );

            BuildingInventorySaveAdapter.Read(cargada);

            // PREPARAR CANTIDADES TEMPORALES DIFERENTES
            var cantidadesAlteradas =
                new Dictionary<
                    SistemaConstruccion.TipoEdificio, int>();

            foreach (var par in estadoOriginal)
            {
                cantidadesAlteradas.Add(
                    par.Key,
                    par.Value == int.MaxValue
                        ? par.Value - 1
                        : par.Value + 1
                );
            }

            // MODIFICAR EL INVENTARIO TEMPORALMENTE
            estadoModificado = true;

            inventario.RestaurarUnidadesGuardadas(
                cantidadesAlteradas
            );

            ComprobarCantidades(
                inventario,
                cantidadesAlteradas
            );

            // RESTAURAR DESDE EL GUARDADO
            BuildingInventorySaveAdapter.Restore(
                inventario, cargada
            );

            // COMPROBAR LAS CINCO CANTIDADES
            ComprobarCantidades(
                inventario,
                estadoOriginal
            );

            // COMPROBAR QUE NO SE HAN GASTADO MONEDAS
            Comprobar(
                cartera.Monedas == monedasOriginales,
                "La restauracion ha modificado las monedas."
            );

            Comprobar(
                cargada.economy.coins == monedasOriginales,
                "El archivo contiene monedas incorrectas."
            );

            Comprobar(
                estadoOriginal.Count == 5,
                "No se han encontrado los cinco tipos esperados."
            );

            pruebaSuperada = true;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] RESTAURACION INVENTARIO FALLIDA: " +
                ex
            );
        }
        finally
        {
            // RECUPERAR EL ESTADO ORIGINAL INCLUSO SI FALLA
            if (estadoModificado &&
                inventario != null &&
                estadoOriginal != null)
            {
                try
                {
                    inventario.RestaurarUnidadesGuardadas(
                        estadoOriginal
                    );

                    ComprobarCantidades(
                        inventario,
                        estadoOriginal
                    );
                }
                catch (Exception ex)
                {
                    recuperacionCorrecta = false;

                    Debug.LogError(
                        "[GUARDADO] ERROR RECUPERANDO " +
                        "EL INVENTARIO ORIGINAL: " + ex
                    );
                }
            }

            if (cartera != null &&
                cartera.Monedas != monedasOriginales)
            {
                recuperacionCorrecta = false;

                Debug.LogError(
                    "[GUARDADO] Las monedas han cambiado " +
                    "durante la prueba."
                );
            }

            // BORRAR SOLO LOS ARCHIVOS TEMPORALES
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

        if (pruebaSuperada && recuperacionCorrecta)
        {
            Debug.Log(
                "[GUARDADO] RESTAURACION INVENTARIO SUPERADA: " +
                "5 tipos, cantidades alteradas y recuperadas, " +
                "JSON verificado, monedas intactas=" +
                monedasOriginales +
                ", estado original recuperado."
            );
        }
    }

    private static Dictionary<
        SistemaConstruccion.TipoEdificio, int>
        LeerCantidades(InventarioEdificios inventario)
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
                "Falta el articulo " + tipo
            );

            resultado.Add(tipo, articulo.Unidades);
        }

        return resultado;
    }

    private static void ComprobarCantidades(
        InventarioEdificios inventario,
        Dictionary<SistemaConstruccion.TipoEdificio, int>
            esperado)
    {
        foreach (var par in esperado)
        {
            InventarioEdificios.Articulo articulo =
                inventario.Obtener(par.Key);

            Comprobar(
                articulo != null,
                "No existe el articulo " + par.Key
            );

            Comprobar(
                articulo.Unidades == par.Value,
                "Cantidad incorrecta de " + par.Key +
                ". Esperado=" + par.Value +
                ", actual=" + articulo.Unidades
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
