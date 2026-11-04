
using System;
using System.Collections.Generic;

/// <summary>
/// Resultado del analisis de conexiones guardadas.
/// </summary>
public sealed class PlacedBuildingsConnectionGraphReport
{
    public int edificios;
    public int conexiones;

    public int paredesSobreRampas;
    public int rampasEntreSi;
    public int paredesEntreSi;
}

/// <summary>
/// Comprueba que las conexiones guardadas
/// forman una estructura coherente.
///
/// Solo analiza datos.
/// No necesita objetos de Unity en la escena.
/// No crea, modifica ni destruye edificios.
/// </summary>
public static class PlacedBuildingsConnectionGraphValidator
{
    public static PlacedBuildingsConnectionGraphReport Analizar(
        GameSaveData partida)
    {
        // Read valida IDs, tipos, transformaciones
        // y los campos individuales de conexion.
        PlacedBuildingsSaveData datos =
            PlacedBuildingsSaveAdapter.Read(partida);

        var informe =
            new PlacedBuildingsConnectionGraphReport();

        informe.edificios = datos.edificios.Count;

        // ======================================
        // 1. INDEXAR TODOS LOS EDIFICIOS
        // ======================================

        var indice =
            new Dictionary<string, PlacedBuildingSaveData>(
                StringComparer.Ordinal
            );

        foreach (PlacedBuildingSaveData edificio
            in datos.edificios)
        {
            indice.Add(
                edificio.idPersistente,
                edificio
            );
        }

        // Identificamos cada conector mediante
        // ID del propietario y ruta dentro del prefab.
        var conectoresOcupados =
            new HashSet<string>(StringComparer.Ordinal);

        // ======================================
        // 2. VALIDAR LAS CONEXIONES
        // ======================================

        foreach (PlacedBuildingSaveData hijo
            in datos.edificios)
        {
            if (!hijo.hasConnection)
                continue;

            PlacedBuildingSaveData padre;

            if (!indice.TryGetValue(
                hijo.connectedToBuildingId,
                out padre))
            {
                throw new InvalidOperationException(
                    "El edificio " + hijo.idPersistente +
                    " apunta a un edificio inexistente: " +
                    hijo.connectedToBuildingId
                );
            }

            // Dos hijos no pueden ocupar el mismo
            // conector del mismo edificio padre.
            string claveConector =
                padre.idPersistente + "\n" +
                hijo.connectorPath;

            if (!conectoresOcupados.Add(claveConector))
            {
                throw new InvalidOperationException(
                    "Conector ocupado por varios edificios: " +
                    padre.idPersistente +
                    " / " + hijo.connectorPath
                );
            }

            // ==================================
            // PARED -> RAMPA
            // ==================================

            if (hijo.tipo == "Pared" &&
                padre.tipo == "Rampa" &&
                hijo.connectorTag == "RailRampa")
            {
                informe.paredesSobreRampas++;
            }

            // ==================================
            // RAMPA -> RAMPA
            // ==================================

            else if (hijo.tipo == "Rampa" &&
                     padre.tipo == "Rampa" &&
                     (hijo.connectorTag == "ConectorEntrada" ||
                      hijo.connectorTag == "ConectorSalida"))
            {
                informe.rampasEntreSi++;
            }

            // ==================================
            // PARED -> PARED
            // ==================================

            else if (hijo.tipo == "Pared" &&
                     padre.tipo == "Pared" &&
                     (hijo.connectorTag ==
                         "ConectorParedEntrada" ||
                      hijo.connectorTag ==
                         "ConectorParedSalida"))
            {
                informe.paredesEntreSi++;
            }

            else
            {
                throw new InvalidOperationException(
                    "Tipo de conexion no soportado: " +
                    hijo.tipo + " -> " + padre.tipo +
                    ", tag=" + hijo.connectorTag
                );
            }

            informe.conexiones++;
        }

        // ======================================
        // 3. DETECTAR CONEXIONES CIRCULARES
        // ======================================

        var comprobados =
            new HashSet<string>(StringComparer.Ordinal);

        foreach (PlacedBuildingSaveData inicio
            in datos.edificios)
        {
            string actualId = inicio.idPersistente;

            var recorrido =
                new HashSet<string>(StringComparer.Ordinal);

            while (!comprobados.Contains(actualId))
            {
                if (!recorrido.Add(actualId))
                {
                    throw new InvalidOperationException(
                        "Conexion circular detectada " +
                        "en el edificio: " + actualId
                    );
                }

                PlacedBuildingSaveData actual =
                    indice[actualId];

                // Un edificio sin conexion es
                // el inicio de una cadena valida.
                if (!actual.hasConnection)
                    break;

                actualId = actual.connectedToBuildingId;
            }

            // Los edificios recorridos ya tienen
            // una cadena de conexiones comprobada.
            comprobados.UnionWith(recorrido);
        }

        return informe;
    }
}
