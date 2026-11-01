
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PiedrasIncrBuildingInventoryTests
{
    private static readonly SistemaConstruccion.TipoEdificio[] Tipos =
    {
        SistemaConstruccion.TipoEdificio.Rampa,
        SistemaConstruccion.TipoEdificio.Pared,
        SistemaConstruccion.TipoEdificio.Libre,
        SistemaConstruccion.TipoEdificio.Procesadora,
        SistemaConstruccion.TipoEdificio.Ventilador
    };

    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar inventario edificios (aislado)"
    )]
    public static void Run()
    {
        string carpeta = Path.Combine(
            Path.GetTempPath(),
            "PiedrasIncr_BuildingInventory_" +
            Guid.NewGuid().ToString("N")
        );

        try
        {
            SaveStorage storage = new SaveStorage(carpeta);

            // Cada fila representa una ranura.
            // Orden: Rampa, Pared, Libre,
            // Procesadora, Ventilador.
            int[][] cantidades =
            {
                new int[] { 2, 1, 0, 4, 5 },
                new int[] { 0, 5, 3, 0, 1 },
                new int[] { 5, 0, 2, 1, 0 }
            };

            int[] monedas = { 100, 200, 300 };

            // Guardar tres inventarios diferentes.
            for (int ranura = 1; ranura <= 3; ranura++)
            {
                GameSaveData partida = CrearPartida(
                    cantidades[ranura - 1],
                    monedas[ranura - 1]
                );

                storage.Save(ranura, partida);
            }

            // Leer y verificar cada ranura.
            for (int ranura = 1; ranura <= 3; ranura++)
            {
                GameSaveData partida;
                bool desdeRespaldo;

                Comprobar(
                    storage.TryLoad(
                        ranura,
                        out partida,
                        out desdeRespaldo
                    ),
                    "No se puede leer la ranura " + ranura
                );

                Comprobar(
                    !desdeRespaldo,
                    "Respaldo inesperado en ranura " + ranura
                );

                BuildingInventorySaveData datos =
                    BuildingInventorySaveAdapter.Read(partida);

                Comprobar(
                    partida.economy.coins == monedas[ranura - 1],
                    "Monedas incorrectas en ranura " + ranura
                );

                Comprobar(
                    datos.articulos.Count == Tipos.Length,
                    "Cantidad de tipos incorrecta en ranura " +
                    ranura
                );

                for (int i = 0; i < Tipos.Length; i++)
                {
                    BuildingInventoryItemSaveData articulo =
                        Buscar(datos, Tipos[i].ToString());

                    Comprobar(
                        articulo != null,
                        "Falta " + Tipos[i] +
                        " en ranura " + ranura
                    );

                    Comprobar(
                        articulo.unidades ==
                            cantidades[ranura - 1][i],
                        "Unidades incorrectas de " +
                        Tipos[i] + " en ranura " + ranura
                    );
                }
            }

            // Comprobar compatibilidad con partidas antiguas.
            GameSaveData antigua = new GameSaveData();

            storage.Save(1, antigua);

            GameSaveData lecturaAntigua;
            bool respaldoAntiguo;

            Comprobar(
                storage.TryLoad(
                    1,
                    out lecturaAntigua,
                    out respaldoAntiguo
                ),
                "No se puede leer la partida antigua."
            );

            Comprobar(
                !lecturaAntigua.hasBuildingInventoryData,
                "Se han inventado datos de inventario."
            );

            bool rechazado = false;

            try
            {
                BuildingInventorySaveAdapter.Read(
                    lecturaAntigua
                );
            }
            catch (InvalidOperationException)
            {
                rechazado = true;
            }

            Comprobar(
                rechazado,
                "Se ha aceptado un inventario inexistente."
            );

            ProbarDatosInvalidos();

            Debug.Log(
                "[GUARDADO] INVENTARIO EDIFICIOS SUPERADO: " +
                "5 tipos, 3 ranuras independientes, " +
                "unidades correctas, economia intacta, " +
                "validacion y compatibilidad v1."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] INVENTARIO EDIFICIOS FALLIDO: " +
                ex
            );
            throw;
        }
        finally
        {
            if (Directory.Exists(carpeta))
                Directory.Delete(carpeta, true);
        }
    }

    private static GameSaveData CrearPartida(
        int[] cantidades,
        int monedas)
    {
        GameObject objeto = new GameObject(
            "__PRUEBA_INVENTARIO_EDIFICIOS__"
        );

        objeto.SetActive(false);
        objeto.hideFlags = HideFlags.HideAndDontSave;

        try
        {
            InventarioEdificios inventario =
                objeto.AddComponent<InventarioEdificios>();

            inventario.articulos =
                new List<InventarioEdificios.Articulo>();

            for (int i = 0; i < Tipos.Length; i++)
            {
                inventario.articulos.Add(
                    new InventarioEdificios.Articulo
                    {
                        tipo = Tipos[i],
                        precioMonedas = 1,
                        maximoEnMochila = 5
                    }
                );
            }

            // Asignar las cantidades de prueba.
            // "unidades" es un campo privado serializado.
            SerializedObject serializado =
                new SerializedObject(inventario);

            SerializedProperty lista =
                serializado.FindProperty("articulos");

            Comprobar(
                lista != null &&
                lista.arraySize == Tipos.Length,
                "No se encuentra la lista de articulos."
            );

            for (int i = 0; i < Tipos.Length; i++)
            {
                SerializedProperty unidades =
                    lista.GetArrayElementAtIndex(i)
                        .FindPropertyRelative("unidades");

                Comprobar(
                    unidades != null,
                    "No se encuentra el campo unidades."
                );

                unidades.intValue = cantidades[i];
            }

            serializado.ApplyModifiedPropertiesWithoutUndo();

            GameSaveData partida = new GameSaveData();

            partida.economy.coins = monedas;

            BuildingInventorySaveAdapter.Capture(
                inventario,
                partida
            );

            Comprobar(
                partida.economy.coins == monedas,
                "La captura ha modificado la economia."
            );

            return partida;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(objeto);
        }
    }

    private static BuildingInventoryItemSaveData Buscar(
        BuildingInventorySaveData datos,
        string tipo)
    {
        foreach (var articulo in datos.articulos)
        {
            if (articulo.tipo == tipo)
                return articulo;
        }

        return null;
    }

    private static void ProbarDatosInvalidos()
    {
        // Unidades negativas.
        BuildingInventorySaveData negativos =
            new BuildingInventorySaveData();

        negativos.articulos.Add(
            new BuildingInventoryItemSaveData
            {
                tipo = "Rampa",
                unidades = -1
            }
        );

        DebeRechazar(negativos, "unidades negativas");

        // Un mismo tipo repetido.
        BuildingInventorySaveData duplicados =
            new BuildingInventorySaveData();

        duplicados.articulos.Add(
            new BuildingInventoryItemSaveData
            {
                tipo = "Pared",
                unidades = 2
            }
        );

        duplicados.articulos.Add(
            new BuildingInventoryItemSaveData
            {
                tipo = "Pared",
                unidades = 3
            }
        );

        DebeRechazar(duplicados, "tipos duplicados");

        // Tipo desconocido.
        BuildingInventorySaveData desconocidos =
            new BuildingInventorySaveData();

        desconocidos.articulos.Add(
            new BuildingInventoryItemSaveData
            {
                tipo = "EdificioInexistente",
                unidades = 1
            }
        );

        DebeRechazar(desconocidos, "tipo desconocido");
    }

    private static void DebeRechazar(
        BuildingInventorySaveData datos,
        string descripcion)
    {
        bool rechazado = false;

        try
        {
            BuildingInventorySaveAdapter.Validate(datos);
        }
        catch (InvalidOperationException)
        {
            rechazado = true;
        }

        Comprobar(
            rechazado,
            "Se han aceptado datos invalidos: " +
            descripcion
        );
    }

    private static void Comprobar(
        bool condicion,
        string mensaje)
    {
        if (!condicion)
            throw new Exception(mensaje);
    }
}

#endif
