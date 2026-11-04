
using System;
using System.Collections.Generic;

/// <summary>
/// Captura, lee, valida y restaura el inventario
/// permanente de edificios del jugador.
///
/// No realiza compras, no gasta monedas
/// y no construye edificios en la escena.
/// </summary>
public static class BuildingInventorySaveAdapter
{
    // =====================================
    // CAPTURAR
    // =====================================

    public static void Capture(
        InventarioEdificios inventario,
        GameSaveData partida)
    {
        if (inventario == null)
            throw new ArgumentNullException(nameof(inventario));

        if (partida == null)
            throw new ArgumentNullException(nameof(partida));

        BuildingInventorySaveData datos =
            new BuildingInventorySaveData();

        foreach (SistemaConstruccion.TipoEdificio tipo
            in Enum.GetValues(
                typeof(SistemaConstruccion.TipoEdificio)))
        {
            InventarioEdificios.Articulo articulo =
                inventario.Obtener(tipo);

            if (articulo == null)
            {
                throw new InvalidOperationException(
                    "Falta el articulo " + tipo +
                    " en el inventario."
                );
            }

            if (articulo.Unidades < 0)
            {
                throw new InvalidOperationException(
                    "Unidades negativas en " + tipo
                );
            }

            datos.articulos.Add(
                new BuildingInventoryItemSaveData
                {
                    tipo = tipo.ToString(),
                    unidades = articulo.Unidades
                }
            );
        }

        Validate(datos);

        partida.buildingInventory = datos;
        partida.hasBuildingInventoryData = true;
    }

    // =====================================
    // LEER
    // =====================================

    public static BuildingInventorySaveData Read(
        GameSaveData partida)
    {
        if (partida == null)
            throw new ArgumentNullException(nameof(partida));

        if (!partida.hasBuildingInventoryData ||
            partida.buildingInventory == null)
        {
            throw new InvalidOperationException(
                "La partida no contiene inventario de edificios."
            );
        }

        Validate(partida.buildingInventory);

        return partida.buildingInventory;
    }

    // =====================================
    // RESTAURAR
    // =====================================

    public static void Restore(
        InventarioEdificios inventario,
        GameSaveData partida)
    {
        if (inventario == null)
            throw new ArgumentNullException(nameof(inventario));

        // Comprobar el guardado antes de tocar nada.
        BuildingInventorySaveData datos = Read(partida);

        Dictionary<SistemaConstruccion.TipoEdificio, int>
            cantidades =
            new Dictionary<SistemaConstruccion.TipoEdificio, int>();

        foreach (BuildingInventoryItemSaveData articulo
            in datos.articulos)
        {
            SistemaConstruccion.TipoEdificio tipo;

            if (!Enum.TryParse(
                articulo.tipo,
                false,
                out tipo))
            {
                throw new InvalidOperationException(
                    "Tipo desconocido: " + articulo.tipo
                );
            }

            cantidades.Add(tipo, articulo.unidades);
        }

        // Guardar el estado anterior por seguridad.
        Dictionary<SistemaConstruccion.TipoEdificio, int>
            estadoAnterior =
            new Dictionary<SistemaConstruccion.TipoEdificio, int>();

        foreach (SistemaConstruccion.TipoEdificio tipo
            in Enum.GetValues(
                typeof(SistemaConstruccion.TipoEdificio)))
        {
            InventarioEdificios.Articulo actual =
                inventario.Obtener(tipo);

            if (actual == null)
            {
                throw new InvalidOperationException(
                    "No existe " + tipo +
                    " en el inventario del jugador."
                );
            }

            estadoAnterior.Add(tipo, actual.Unidades);
        }

        try
        {
            // Aplicar las cantidades exactas del guardado.
            inventario.RestaurarUnidadesGuardadas(
                cantidades
            );

            // Verificar todos los resultados.
            foreach (var par in cantidades)
            {
                InventarioEdificios.Articulo actual =
                    inventario.Obtener(par.Key);

                if (actual == null ||
                    actual.Unidades != par.Value)
                {
                    throw new InvalidOperationException(
                        "No se ha restaurado correctamente " +
                        par.Key
                    );
                }
            }
        }
        catch
        {
            // Intentar recuperar las unidades originales.
            try
            {
                inventario.RestaurarUnidadesGuardadas(
                    estadoAnterior
                );
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(
                    "[GUARDADO] Error recuperando " +
                    "el inventario anterior: " + ex
                );
            }

            throw;
        }
    }

    // =====================================
    // VALIDAR
    // =====================================

    public static void Validate(
        BuildingInventorySaveData datos)
    {
        if (datos == null || datos.articulos == null)
        {
            throw new InvalidOperationException(
                "El inventario guardado no existe."
            );
        }

        HashSet<string> tiposRegistrados =
            new HashSet<string>(StringComparer.Ordinal);

        foreach (BuildingInventoryItemSaveData articulo
            in datos.articulos)
        {
            if (articulo == null ||
                string.IsNullOrEmpty(articulo.tipo))
            {
                throw new InvalidOperationException(
                    "Existe un articulo sin tipo."
                );
            }

            SistemaConstruccion.TipoEdificio tipo;

            if (!Enum.TryParse(
                    articulo.tipo, false, out tipo) ||
                !Enum.IsDefined(
                    typeof(SistemaConstruccion.TipoEdificio),
                    tipo) ||
                tipo.ToString() != articulo.tipo)
            {
                throw new InvalidOperationException(
                    "Tipo de edificio desconocido: " +
                    articulo.tipo
                );
            }

            if (!tiposRegistrados.Add(articulo.tipo))
            {
                throw new InvalidOperationException(
                    "Tipo de edificio duplicado: " +
                    articulo.tipo
                );
            }

            if (articulo.unidades < 0)
            {
                throw new InvalidOperationException(
                    "Unidades negativas en " +
                    articulo.tipo
                );
            }
        }

        // Comprobar que existen todos los tipos.
        foreach (SistemaConstruccion.TipoEdificio tipo
            in Enum.GetValues(
                typeof(SistemaConstruccion.TipoEdificio)))
        {
            if (!tiposRegistrados.Contains(tipo.ToString()))
            {
                throw new InvalidOperationException(
                    "Falta el edificio " + tipo +
                    " en el guardado."
                );
            }
        }
    }
}
