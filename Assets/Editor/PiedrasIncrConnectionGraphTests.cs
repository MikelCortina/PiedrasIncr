
#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

public static class PiedrasIncrConnectionGraphTests
{
    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar grafo conexiones"
    )]
    public static void Ejecutar()
    {
        try
        {
            // ======================================
            // 1. CREAR UN GRAFO CORRECTO
            // ======================================

            GameSaveData original = CrearPartida();

            // Orden de los edificios:
            //
            // 0 - Rampa principal
            // 1 - Pared conectada a rampa
            // 2 - Rampa conectada a rampa principal
            // 3 - Pared conectada a otra pared

            string jsonOriginal =
                JsonUtility.ToJson(original);

            VerificarInforme(
                original,
                4, 3, 1, 1, 1,
                "GRAFO COMPLETO"
            );

            // ======================================
            // 2. GUARDAR Y LEER JSON
            // ======================================

            GameSaveData desdeJson =
                Copiar(original);

            VerificarInforme(
                desdeJson,
                4, 3, 1, 1, 1,
                "LECTURA JSON"
            );

            // ======================================
            // 3. EDIFICIOS SIN CONEXIONES
            // ======================================

            GameSaveData independientes =
                Copiar(original);

            foreach (PlacedBuildingSaveData edificio
                in independientes.placedBuildings.edificios)
            {
                edificio.hasConnection = false;
                edificio.connectedToBuildingId = null;
                edificio.connectorPath = null;
                edificio.connectorTag = null;
            }

            VerificarInforme(
                independientes,
                4, 0, 0, 0, 0,
                "EDIFICIOS INDEPENDIENTES"
            );

            // ======================================
            // 4. PARTIDA VACIA
            // ======================================

            GameSaveData vacia = Copiar(original);

            vacia.placedBuildings.edificios.Clear();

            VerificarInforme(
                vacia,
                0, 0, 0, 0, 0,
                "PARTIDA VACIA"
            );

            // ======================================
            // 5. PROPIETARIO INEXISTENTE
            // ======================================

            GameSaveData sinPropietario =
                Copiar(original);

            sinPropietario.placedBuildings.edificios[1]
                .connectedToBuildingId =
                    Guid.NewGuid().ToString("N");

            EsperarRechazo(
                sinPropietario,
                "PROPIETARIO INEXISTENTE"
            );

            // ======================================
            // 6. CONECTOR UTILIZADO DOS VECES
            // ======================================

            GameSaveData conectorDuplicado =
                Copiar(original);

            PlacedBuildingSaveData paredFinal =
                conectorDuplicado.placedBuildings.edificios[3];

            PlacedBuildingSaveData rampaPrincipal =
                conectorDuplicado.placedBuildings.edificios[0];

            // Intentar conectar dos paredes al
            // mismo RailRampa de la misma rampa.
            paredFinal.connectedToBuildingId =
                rampaPrincipal.idPersistente;

            paredFinal.connectorPath =
                "Rail[0]";

            paredFinal.connectorTag =
                "RailRampa";

            EsperarRechazo(
                conectorDuplicado,
                "CONECTOR DUPLICADO"
            );

            // ======================================
            // 7. CONEXION CIRCULAR
            // ======================================

            GameSaveData circular =
                Copiar(original);

            PlacedBuildingSaveData rampaBase =
                circular.placedBuildings.edificios[0];

            PlacedBuildingSaveData rampaHija =
                circular.placedBuildings.edificios[2];

            // La rampa hija depende de la principal.
            // Ahora hacemos que la principal dependa
            // de la hija, creando un ciclo.
            rampaBase.hasConnection = true;

            rampaBase.connectedToBuildingId =
                rampaHija.idPersistente;

            rampaBase.connectorPath =
                "Entrada[0]";

            rampaBase.connectorTag =
                "ConectorEntrada";

            EsperarRechazo(
                circular,
                "CONEXION CIRCULAR"
            );

            // ======================================
            // 8. TIPO DE CONEXION NO SOPORTADO
            // ======================================

            GameSaveData tipoIncorrecto =
                Copiar(original);

            // Una pared no puede conectarse a
            // ConectorSalida de una rampa utilizando
            // las reglas actualmente implementadas.
            tipoIncorrecto.placedBuildings.edificios[1]
                .connectorTag = "ConectorSalida";

            EsperarRechazo(
                tipoIncorrecto,
                "TIPO NO SOPORTADO"
            );

            // ======================================
            // 9. DATOS DE CONEXION INCONSISTENTES
            // ======================================

            GameSaveData camposInconsistentes =
                Copiar(original);

            // Indica que no hay conexion, pero
            // conserva ID, ruta y tag.
            camposInconsistentes.placedBuildings.edificios[1]
                .hasConnection = false;

            EsperarRechazo(
                camposInconsistentes,
                "CAMPOS INCONSISTENTES"
            );

            // ======================================
            // 10. CONEXION CONSIGO MISMO
            // ======================================

            GameSaveData consigoMismo =
                Copiar(original);

            PlacedBuildingSaveData pared =
                consigoMismo.placedBuildings.edificios[1];

            pared.connectedToBuildingId =
                pared.idPersistente;

            EsperarRechazo(
                consigoMismo,
                "CONEXION CONSIGO MISMO"
            );

            // ======================================
            // 11. IDENTIFICADOR DE EDIFICIO DUPLICADO
            // ======================================

            GameSaveData idDuplicado =
                Copiar(original);

            idDuplicado.placedBuildings.edificios[3]
                .idPersistente =
                    idDuplicado.placedBuildings.edificios[1]
                        .idPersistente;

            EsperarRechazo(
                idDuplicado,
                "ID DE EDIFICIO DUPLICADO"
            );

            // ======================================
            // 12. COMPROBAR DATOS ORIGINALES
            // ======================================

            Comprobar(
                JsonUtility.ToJson(original) ==
                    jsonOriginal,
                "Las pruebas han modificado " +
                "la partida original."
            );

            VerificarInforme(
                original,
                4, 3, 1, 1, 1,
                "DATOS ORIGINALES INTACTOS"
            );

            Debug.Log(
                "[GUARDADO] GRAFO CONEXIONES SUPERADO: " +
                "5 comprobaciones validas, " +
                "7 situaciones invalidas rechazadas, " +
                "conexiones circulares y duplicadas detectadas, " +
                "JSON correcto, datos originales intactos."
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] GRAFO CONEXIONES FALLIDO: " +
                ex
            );
        }
    }

    // ======================================
    // CREAR PARTIDA DE PRUEBA
    // ======================================

    private static GameSaveData CrearPartida()
    {
        GameSaveData partida = new GameSaveData
        {
            hasPlacedBuildingsData = true,

            placedBuildings =
                new PlacedBuildingsSaveData()
        };

        PlacedBuildingSaveData rampaPrincipal =
            CrearEdificio("Rampa");

        PlacedBuildingSaveData paredRampa =
            CrearEdificio("Pared");

        PlacedBuildingSaveData segundaRampa =
            CrearEdificio("Rampa");

        PlacedBuildingSaveData segundaPared =
            CrearEdificio("Pared");

        // Pared -> Rampa
        Conectar(
            paredRampa,
            rampaPrincipal,
            "Rail[0]",
            "RailRampa"
        );

        // Rampa -> Rampa
        Conectar(
            segundaRampa,
            rampaPrincipal,
            "Salida[1]",
            "ConectorSalida"
        );

        // Pared -> Pared
        Conectar(
            segundaPared,
            paredRampa,
            "SalidaPared[0]",
            "ConectorParedSalida"
        );

        partida.placedBuildings.edificios.Add(
            rampaPrincipal
        );

        partida.placedBuildings.edificios.Add(
            paredRampa
        );

        partida.placedBuildings.edificios.Add(
            segundaRampa
        );

        partida.placedBuildings.edificios.Add(
            segundaPared
        );

        return partida;
    }

    // ======================================
    // CREAR EDIFICIO
    // ======================================

    private static PlacedBuildingSaveData CrearEdificio(
        string tipo)
    {
        return new PlacedBuildingSaveData
        {
            idPersistente =
                Guid.NewGuid().ToString("N"),

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

    // ======================================
    // CONECTAR EDIFICIOS
    // ======================================

    private static void Conectar(
        PlacedBuildingSaveData hijo,
        PlacedBuildingSaveData padre,
        string ruta,
        string tag)
    {
        hijo.hasConnection = true;

        hijo.connectedToBuildingId =
            padre.idPersistente;

        hijo.connectorPath = ruta;
        hijo.connectorTag = tag;
    }

    // ======================================
    // VALIDAR RESULTADO CORRECTO
    // ======================================

    private static void VerificarInforme(
        GameSaveData partida,
        int edificios,
        int conexiones,
        int paredesSobreRampas,
        int rampasEntreSi,
        int paredesEntreSi,
        string descripcion)
    {
        PlacedBuildingsConnectionGraphReport informe =
            PlacedBuildingsConnectionGraphValidator
                .Analizar(partida);

        Comprobar(
            informe.edificios == edificios,
            descripcion + ": cantidad de edificios incorrecta."
        );

        Comprobar(
            informe.conexiones == conexiones,
            descripcion + ": cantidad de conexiones incorrecta."
        );

        Comprobar(
            informe.paredesSobreRampas == paredesSobreRampas,
            descripcion + ": paredes sobre rampas incorrectas."
        );

        Comprobar(
            informe.rampasEntreSi == rampasEntreSi,
            descripcion + ": conexiones entre rampas incorrectas."
        );

        Comprobar(
            informe.paredesEntreSi == paredesEntreSi,
            descripcion + ": conexiones entre paredes incorrectas."
        );

        Debug.Log(
            "[GUARDADO] GRAFO OK " + descripcion +
            ": edificios=" + informe.edificios +
            ", conexiones=" + informe.conexiones +
            ", pared-rampa=" + informe.paredesSobreRampas +
            ", rampa-rampa=" + informe.rampasEntreSi +
            ", pared-pared=" + informe.paredesEntreSi
        );
    }

    // ======================================
    // COMPROBAR RECHAZO
    // ======================================

    private static void EsperarRechazo(
        GameSaveData partida,
        string descripcion)
    {
        bool rechazado = false;

        try
        {
            PlacedBuildingsConnectionGraphValidator
                .Analizar(partida);
        }
        catch (InvalidOperationException)
        {
            rechazado = true;
        }

        Comprobar(
            rechazado,
            descripcion + ": el validador ha aceptado " +
            "datos incorrectos."
        );

        Debug.Log(
            "[GUARDADO] GRAFO RECHAZO OK: " +
            descripcion
        );
    }

    private static GameSaveData Copiar(GameSaveData partida)
    {
        return JsonUtility.FromJson<GameSaveData>(
            JsonUtility.ToJson(partida)
        );
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
