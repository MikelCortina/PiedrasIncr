
#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrBuildingInventorySceneTests
{
    private static string CarpetaPruebas => Path.Combine(
        Application.persistentDataPath,
        "PiedrasIncr",
        "DeveloperTests",
        "BuildingInventoryScene"
    );

    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar inventario edificios en escena (Play)"
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

        try
        {
            // Localizar el inventario real de la escena.
            InventarioEdificios[] inventarios =
                UnityEngine.Object.FindObjectsByType<InventarioEdificios>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            if (inventarios.Length != 1)
            {
                throw new InvalidOperationException(
                    "Se esperaba un inventario, encontrados: " +
                    inventarios.Length
                );
            }

            InventarioEdificios inventario = inventarios[0];

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

                if (carteras.Length == 1)
                    cartera = carteras[0];
            }

            if (cartera == null)
            {
                throw new InvalidOperationException(
                    "No se encuentra la Cartera del jugador."
                );
            }

            int monedasAntes = cartera.Monedas;

            // 1. CAPTURAR
            GameSaveData partida = new GameSaveData
            {
                sceneName = SceneManager.GetActiveScene().name
            };

            EconomySaveAdapter.Capture(cartera, partida);

            BuildingInventorySaveAdapter.Capture(
                inventario, partida
            );

            // 2. GUARDAR EN CARPETA DE DIAGNOSTICO
            SaveStorage storage = new SaveStorage(CarpetaPruebas);

            const int ranura = 1;

            GameSaveData anterior;
            bool respaldoAnterior;

            if (storage.TryLoad(
                ranura, out anterior, out respaldoAnterior))
            {
                if (anterior.sceneName == partida.sceneName)
                {
                    partida.saveId = anterior.saveId;
                    partida.createdUtc = anterior.createdUtc;
                }
            }

            storage.Save(ranura, partida);

            Debug.Log(
                "[GUARDADO] INVENTARIO GUARDADO TEST 1: " +
                Formato(partida)
            );

            // 3. LEER DESDE EL ARCHIVO
            GameSaveData cargada;
            bool desdeRespaldo;

            if (!storage.TryLoad(
                ranura, out cargada, out desdeRespaldo))
            {
                throw new InvalidOperationException(
                    "No se pudo leer el inventario guardado."
                );
            }

            if (cargada.sceneName != partida.sceneName)
            {
                throw new InvalidOperationException(
                    "El archivo corresponde a otra escena."
                );
            }

            BuildingInventorySaveData datos =
                BuildingInventorySaveAdapter.Read(cargada);

            // 4. COMPARAR CON EL INVENTARIO REAL
            int cantidadTipos = 0;

            foreach (SistemaConstruccion.TipoEdificio tipo
                in Enum.GetValues(
                    typeof(SistemaConstruccion.TipoEdificio)))
            {
                cantidadTipos++;

                InventarioEdificios.Articulo real =
                    inventario.Obtener(tipo);

                if (real == null)
                {
                    throw new InvalidOperationException(
                        "Falta el edificio " + tipo
                    );
                }

                BuildingInventoryItemSaveData guardado = null;

                foreach (var articulo in datos.articulos)
                {
                    if (articulo.tipo == tipo.ToString())
                    {
                        guardado = articulo;
                        break;
                    }
                }

                if (guardado == null ||
                    guardado.unidades != real.Unidades)
                {
                    throw new InvalidOperationException(
                        "Diferencia entre inventario real y guardado: " +
                        tipo
                    );
                }
            }

            if (datos.articulos.Count != cantidadTipos)
            {
                throw new InvalidOperationException(
                    "La cantidad de tipos de edificios no coincide."
                );
            }

            if (cartera.Monedas != monedasAntes ||
                cargada.economy.coins != monedasAntes)
            {
                throw new InvalidOperationException(
                    "La prueba ha modificado las monedas."
                );
            }

            Debug.Log(
                "[GUARDADO] INVENTARIO LECTURA TEST 1: " +
                Formato(cargada) +
                (desdeRespaldo ? " (respaldo)" : "")
            );

            Debug.Log(
                "[GUARDADO] INVENTARIO ESCENA SUPERADO: " +
                "5 tipos, unidades correctas, " +
                "monedas intactas y lectura verificada."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] INVENTARIO ESCENA FALLIDO: " + ex
            );
        }
    }

    private static string Formato(GameSaveData partida)
    {
        BuildingInventorySaveData datos =
            BuildingInventorySaveAdapter.Read(partida);

        StringBuilder texto = new StringBuilder();

        foreach (var articulo in datos.articulos)
        {
            if (texto.Length > 0)
                texto.Append(", ");

            texto.Append(articulo.tipo);
            texto.Append("=");
            texto.Append(articulo.unidades);
        }

        texto.Append(", monedas=");
        texto.Append(partida.economy.coins);
        texto.Append(".");

        return texto.ToString();
    }
}

#endif
