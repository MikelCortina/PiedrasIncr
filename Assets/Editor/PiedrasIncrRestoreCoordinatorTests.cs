
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PiedrasIncrRestoreCoordinatorTests
{
    private sealed class EstadoPrueba
    {
        public SistemaConstruccion sistema;
        public InventarioEdificios inventario;
        public Cartera cartera;

        public int monedasAntes;
        public int cantidadAntes;

        public Dictionary<SistemaConstruccion.TipoEdificio, int>
            inventarioAntes;

        public Dictionary<string, string> originales;

        public GameSaveData partida;

        public string idRampa;
        public string idPared;

        public int fase;
        public int fotogramaAnterior;
    }

    private static EstadoPrueba estado;

    [MenuItem(
        "Tools/PiedrasIncr/Guardado/Probar coordinador restauracion (Play)"
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

        if (estado != null)
        {
            Debug.LogWarning(
                "[GUARDADO] Ya hay una prueba en ejecucion."
            );
            return;
        }

        try
        {
            estado = PrepararPrueba();

            // ======================================
            // 1. GRAFO CORRUPTO
            // ======================================

            GameSaveData grafoCorrupto =
                Copiar(estado.partida);

            grafoCorrupto.placedBuildings.edificios[1]
                .connectedToBuildingId =
                    Guid.NewGuid().ToString("N");

            bool rechazado = false;

            try
            {
                PlacedBuildingsRestoreCoordinator
                    .RestaurarNuevos(
                        estado.sistema,
                        grafoCorrupto
                    );
            }
            catch (InvalidOperationException)
            {
                rechazado = true;
            }

            Comprobar(
                rechazado,
                "El coordinador ha aceptado un grafo corrupto."
            );

            Comprobar(
                ContarEdificios() == estado.cantidadAntes &&
                Buscar(estado.idRampa) == null &&
                Buscar(estado.idPared) == null,
                "El grafo corrupto ha creado edificios."
            );

            Debug.Log(
                "[GUARDADO] COORDINADOR GRAFO CORRUPTO OK: " +
                "rechazado antes de crear prefabs."
            );

            // ======================================
            // 2. PROVOCAR FALLO EN LA CONEXION
            // ======================================
            // El grafo es valido, pero la ruta fisica
            // no existe en el prefab de la rampa.
            //
            // La reconstruccion comenzara y despues
            // debera retirarse por completo.

            GameSaveData conectorInexistente =
                Copiar(estado.partida);

            conectorInexistente.placedBuildings.edificios[1]
                .connectorPath = "Inexistente[999]";

            PlacedBuildingsConnectionGraphValidator.Analizar(
                conectorInexistente
            );

            bool falloConexion = false;

            try
            {
                PlacedBuildingsRestoreCoordinator
                    .RestaurarNuevos(
                        estado.sistema,
                        conectorInexistente
                    );
            }
            catch (InvalidOperationException)
            {
                falloConexion = true;
            }

            Comprobar(
                falloConexion,
                "Se ha aceptado una ruta fisica inexistente."
            );

            // Los objetos pendientes de Destroy
            // no deben permanecer activos.
            ComprobarTemporalInactivo(estado.idRampa);
            ComprobarTemporalInactivo(estado.idPared);

            Debug.Log(
                "[GUARDADO] COORDINADOR FALLO CONEXION OK: " +
                "limpieza solicitada para los prefabs nuevos."
            );

            // Esperar al siguiente fotograma para
            // comprobar que Destroy se ha completado.
            estado.fase = 1;
            estado.fotogramaAnterior = Time.frameCount;

            EditorApplication.update += ContinuarPrueba;
        }
        catch (Exception ex)
        {
            Fallar(ex);
        }
    }

    // ======================================
    // CONTINUAR DESPUES DE DESTROY
    // ======================================

    private static void ContinuarPrueba()
    {
        if (estado == null)
        {
            EditorApplication.update -= ContinuarPrueba;
            return;
        }

        if (!Application.isPlaying)
        {
            Fallar(new InvalidOperationException(
                "Se ha salido de Play durante la prueba."
            ));
            return;
        }

        if (Time.frameCount <= estado.fotogramaAnterior)
            return;

        try
        {
            if (estado.fase == 1)
            {
                VerificarLimpieza();

                Debug.Log(
                    "[GUARDADO] COORDINADOR ROLLBACK OK: " +
                    "los prefabs del intento fallido " +
                    "han desaparecido."
                );

                ProbarRestauracionCorrecta();

                // Esperar un segundo fotograma
                // para comprobar la limpieza final.
                estado.fase = 2;
                estado.fotogramaAnterior = Time.frameCount;

                return;
            }

            if (estado.fase == 2)
            {
                VerificarLimpieza();
                VerificarOriginales();

                Debug.Log(
                    "[GUARDADO] COORDINADOR RESTAURACION " +
                    "SUPERADO: grafo corrupto rechazado, " +
                    "fallo de conexion con rollback, " +
                    "2 prefabs reales reconstruidos, " +
                    "conexion Pared -> Rampa restaurada, " +
                    "IDs existentes protegidos, " +
                    "limpieza verificada, edificios " +
                    "originales e inventario intactos, " +
                    "monedas=" + estado.monedasAntes + "."
                );

                Finalizar();
            }
        }
        catch (Exception ex)
        {
            Fallar(ex);
        }
    }

    // ======================================
    // PREPARAR DATOS DE LA ESCENA
    // ======================================

    private static EstadoPrueba PrepararPrueba()
    {
        EstadoPrueba e = new EstadoPrueba();

        e.sistema = SistemaConstruccion.Instancia;

        Comprobar(
            e.sistema != null,
            "No se encuentra SistemaConstruccion."
        );

        e.inventario = e.sistema.inventarioEdificios;

        Comprobar(
            e.inventario != null,
            "No se encuentra InventarioEdificios."
        );

        e.cartera = e.inventario.cartera;

        if (e.cartera == null)
        {
            e.cartera =
                e.inventario.GetComponentInParent<Cartera>();
        }

        Comprobar(
            e.cartera != null,
            "No se encuentra Cartera."
        );

        e.monedasAntes = e.cartera.Monedas;
        e.cantidadAntes = ContarEdificios();

        e.inventarioAntes =
            CapturarInventario(e.inventario);

        GameSaveData captura = new GameSaveData
        {
            sceneName = SceneManager.GetActiveScene().name
        };

        PlacedBuildingsSaveAdapter.Capture(
            e.sistema,
            captura
        );

        e.originales =
            new Dictionary<string, string>(
                StringComparer.Ordinal
            );

        Dictionary<string, PlacedBuildingSaveData> mapa =
            new Dictionary<string, PlacedBuildingSaveData>(
                StringComparer.Ordinal
            );

        foreach (PlacedBuildingSaveData edificio
            in captura.placedBuildings.edificios)
        {
            e.originales.Add(
                edificio.idPersistente,
                JsonUtility.ToJson(edificio)
            );

            mapa.Add(
                edificio.idPersistente,
                edificio
            );
        }

        PlacedBuildingSaveData rampaOriginal = null;
        PlacedBuildingSaveData paredOriginal = null;

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

            if (mapa.TryGetValue(
                    edificio.connectedToBuildingId,
                    out candidata) &&
                candidata.tipo == "Rampa" &&
                candidata.curvaturaRampa == 0f)
            {
                paredOriginal = edificio;
                rampaOriginal = candidata;
                break;
            }
        }

        Comprobar(
            rampaOriginal != null &&
            paredOriginal != null,
            "Necesitas una rampa recta con una pared " +
            "conectada a RailRampa."
        );

        e.idRampa = Guid.NewGuid().ToString("N");
        e.idPared = Guid.NewGuid().ToString("N");

        PlacedBuildingSaveData rampa =
            JsonUtility.FromJson<PlacedBuildingSaveData>(
                JsonUtility.ToJson(rampaOriginal)
            );

        PlacedBuildingSaveData pared =
            JsonUtility.FromJson<PlacedBuildingSaveData>(
                JsonUtility.ToJson(paredOriginal)
            );

        rampa.idPersistente = e.idRampa;
        pared.idPersistente = e.idPared;

        // La copia de la rampa sera independiente.
        rampa.hasConnection = false;
        rampa.connectedToBuildingId = null;
        rampa.connectorPath = null;
        rampa.connectorTag = null;

        // La pared apuntara a la rampa temporal.
        pared.connectedToBuildingId = e.idRampa;

        // Mismo desplazamiento para conservar
        // la relacion espacial.
        Vector3 desplazamiento =
            new Vector3(45f, 25f, 45f);

        Desplazar(rampa, desplazamiento);
        Desplazar(pared, desplazamiento);

        e.partida = new GameSaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            hasPlacedBuildingsData = true,
            placedBuildings = new PlacedBuildingsSaveData()
        };

        e.partida.placedBuildings.edificios.Add(rampa);
        e.partida.placedBuildings.edificios.Add(pared);

        e.partida = Copiar(e.partida);

        return e;
    }

    // ======================================
    // RESTAURACION CORRECTA
    // ======================================

    private static void ProbarRestauracionCorrecta()
    {
        PlacedBuildingsRestoreResult resultado =
            PlacedBuildingsRestoreCoordinator
                .RestaurarNuevos(
                    estado.sistema,
                    estado.partida
                );

        Comprobar(
            resultado.edificios.Count == 2 &&
            resultado.conexionesRestauradas == 1,
            "El coordinador no ha restaurado " +
            "dos edificios y una conexion."
        );

        EdificioConstruido rampa =
            Buscar(estado.idRampa);

        EdificioConstruido pared =
            Buscar(estado.idPared);

        Comprobar(
            rampa != null && pared != null,
            "No se encuentran los prefabs reconstruidos."
        );

        Comprobar(
            rampa.tipo ==
                SistemaConstruccion.TipoEdificio.Rampa &&
            pared.tipo ==
                SistemaConstruccion.TipoEdificio.Pared,
            "Los tipos de los prefabs son incorrectos."
        );

        // Detener el comportamiento de las copias.
        rampa.gameObject.SetActive(false);
        pared.gameObject.SetActive(false);

        Collider rail = pared.conectorUsado;

        Comprobar(
            rail != null &&
            rail.CompareTag("RailRampa") &&
            !rail.enabled,
            "El RailRampa no esta ocupado."
        );

        Comprobar(
            rail.GetComponentInParent<
                EdificioConstruido>(true) == rampa,
            "El conector pertenece a otra rampa."
        );

        Comprobar(
            rampa.edificiosDependientes.Count == 1 &&
            rampa.edificiosDependientes[0] ==
                pared.gameObject,
            "La dependencia no se ha recuperado."
        );

        Comprobar(
            ContarEdificios() ==
                estado.cantidadAntes + 2,
            "Cantidad de edificios incorrecta."
        );

        Debug.Log(
            "[GUARDADO] COORDINADOR CARGA CORRECTA OK: " +
            "2 prefabs reales, 1 conexion, " +
            "RailRampa ocupado y dependencia registrada."
        );

        // ======================================
        // COMPROBAR PROTECCION DE IDs EXISTENTES
        // ======================================

        bool segundaCargaRechazada = false;

        try
        {
            PlacedBuildingsRestoreCoordinator
                .RestaurarNuevos(
                    estado.sistema,
                    estado.partida
                );
        }
        catch (InvalidOperationException)
        {
            segundaCargaRechazada = true;
        }

        Comprobar(
            segundaCargaRechazada,
            "El coordinador ha aceptado IDs existentes."
        );

        Comprobar(
            ContarEdificios() ==
                estado.cantidadAntes + 2 &&
            pared.conectorUsado == rail &&
            rampa.edificiosDependientes.Count == 1,
            "La segunda llamada ha alterado los edificios."
        );

        Debug.Log(
            "[GUARDADO] COORDINADOR IDS EXISTENTES OK: " +
            "segunda restauracion bloqueada sin duplicados."
        );

        VerificarOriginales();

        // Retirar las dos copias correctas.
        SolicitarLimpieza();
    }

    // ======================================
    // LIMPIEZA
    // ======================================

    private static void ComprobarTemporalInactivo(string id)
    {
        EdificioConstruido edificio = Buscar(id);

        // Destroy puede haberlo retirado ya.
        if (edificio == null)
            return;

        Comprobar(
            !edificio.gameObject.activeSelf,
            "Un edificio temporal sigue activo: " + id
        );
    }

    private static void SolicitarLimpieza()
    {
        if (estado == null)
            return;

        DestruirTemporal(estado.idPared);
        DestruirTemporal(estado.idRampa);
    }

    private static void DestruirTemporal(string id)
    {
        EdificioConstruido edificio = Buscar(id);

        if (edificio == null)
            return;

        edificio.gameObject.SetActive(false);

        UnityEngine.Object.Destroy(
            edificio.gameObject
        );
    }

    private static void VerificarLimpieza()
    {
        Comprobar(
            Buscar(estado.idRampa) == null &&
            Buscar(estado.idPared) == null,
            "Quedan edificios temporales " +
            "despues del fotograma de limpieza."
        );

        Comprobar(
            ContarEdificios() == estado.cantidadAntes,
            "La cantidad de edificios no ha vuelto " +
            "al valor original."
        );
    }

    private static void VerificarOriginales()
    {
        GameSaveData captura =
            new GameSaveData();

        PlacedBuildingsSaveAdapter.Capture(
            estado.sistema,
            captura
        );

        int originalesVerificados = 0;

        foreach (PlacedBuildingSaveData edificio
            in captura.placedBuildings.edificios)
        {
            string jsonOriginal;

            if (estado.originales.TryGetValue(
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
                    edificio.idPersistente == estado.idRampa ||
                    edificio.idPersistente == estado.idPared,
                    "Ha aparecido un edificio inesperado."
                );
            }
        }

        Comprobar(
            originalesVerificados == estado.originales.Count,
            "Falta un edificio original."
        );

        Comprobar(
            estado.cartera.Monedas == estado.monedasAntes,
            "Las monedas han cambiado."
        );

        ComprobarInventario(
            estado.inventario,
            estado.inventarioAntes
        );
    }

    private static void Finalizar()
    {
        EditorApplication.update -= ContinuarPrueba;
        estado = null;
    }

    private static void Fallar(Exception ex)
    {
        Debug.LogError(
            "[GUARDADO] COORDINADOR RESTAURACION FALLIDO: " +
            ex
        );

        SolicitarLimpieza();
        Finalizar();
    }

    // ======================================
    // UTILIDADES
    // ======================================

    private static GameSaveData Copiar(GameSaveData partida)
    {
        return JsonUtility.FromJson<GameSaveData>(
            JsonUtility.ToJson(partida)
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

    private static EdificioConstruido Buscar(string id)
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

    private static int ContarEdificios()
    {
        int cantidad = 0;

        Scene escena = SceneManager.GetActiveScene();

        foreach (EdificioConstruido edificio
            in UnityEngine.Object.FindObjectsByType<
                EdificioConstruido>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            ))
        {
            if (edificio != null &&
                edificio.gameObject.scene == escena)
            {
                cantidad++;
            }
        }

        return cantidad;
    }

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
                "Inventario modificado: " + par.Key
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
