
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Paso 85:
/// F5 guarda la partida.
/// F9 carga la partida desde una escena limpia.
/// </summary>
public class GuardadoPartidaJuego : MonoBehaviour
{
    [Header("Ranura")]
    [Range(1, 3)]
    [SerializeField] private int ranura = 1;

    [Header("Controles")]
    [SerializeField]
    private KeyCode teclaGuardar = KeyCode.F5;

    [SerializeField]
    private KeyCode teclaCargar = KeyCode.F9;

    [Header("Seguridad")]
    [SerializeField]
    private bool permitirSobrescribir = false;

    private bool operacionEnCurso;

    // Tiempo acumulado antes de esta sesion.
    private double segundosAcumulados;
    private float inicioSesion;

    private void Awake()
    {
        inicioSesion = Time.timeSinceLevelLoad;
    }

    private void Update()
    {
        if (operacionEnCurso)
            return;

        if (Input.GetKeyDown(teclaGuardar))
        {
            Guardar();
        }
        else if (Input.GetKeyDown(teclaCargar))
        {
            Cargar();
        }
    }

    // =====================================
    // GUARDAR
    // =====================================

    public void Guardar()
    {
        GuardarEnRanura(ranura);
    }

    public void GuardarEnRanura(int numeroRanura)
    {
        if (operacionEnCurso)
            return;

        if (!RanuraValida(numeroRanura))
            return;

        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[GUARDADO] Debes estar en Play."
            );
            return;
        }

        operacionEnCurso = true;

        try
        {
            SaveStorage almacenamiento =
                new SaveStorage();

            // Proteger las partidas existentes.
            if (!permitirSobrescribir &&
                almacenamiento.HasValidSave(numeroRanura))
            {
                Debug.LogWarning(
                    "[GUARDADO] La ranura " +
                    numeroRanura +
                    " ya contiene una partida. " +
                    "No se ha sobrescrito."
                );

                return;
            }

            GameSaveSceneReferences referencias =
                ObtenerReferencias();

            // Conservar el tiempo jugado antes
            // de la ultima carga.
            double tiempoSesion = Math.Max(
                0.0,
                Time.timeSinceLevelLoad - inicioSesion
            );

            double segundosJugados =
                segundosAcumulados + tiempoSesion;

            GameSaveData guardada =
                GameSaveFullWriter.GuardarEnRanura(
                    almacenamiento,
                    numeroRanura,
                    referencias,
                    segundosJugados
                );

            // Verificar lectura real del archivo.
            GameSaveData comprobacion;
            bool desdeRespaldo;

            if (!almacenamiento.TryLoad(
                numeroRanura,
                out comprobacion,
                out desdeRespaldo))
            {
                throw new InvalidOperationException(
                    "No se puede leer la partida guardada."
                );
            }

            if (desdeRespaldo ||
                comprobacion.saveId != guardada.saveId ||
                comprobacion.economy.coins !=
                    guardada.economy.coins ||
                comprobacion.placedBuildings.edificios.Count !=
                    guardada.placedBuildings.edificios.Count)
            {
                throw new InvalidOperationException(
                    "La verificacion del guardado ha fallado."
                );
            }

            Debug.Log(
                "[GUARDADO] PASO 85 GUARDADO OK: " +
                "ranura=" + numeroRanura +
                ", monedas=" + guardada.economy.coins +
                ", edificios=" +
                guardada.placedBuildings.edificios.Count +
                ", escena=" + guardada.sceneName
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] ERROR AL GUARDAR: " + ex
            );
        }
        finally
        {
            operacionEnCurso = false;
        }
    }

    // =====================================
    // CARGAR
    // =====================================

    public void Cargar()
    {
        CargarDesdeRanura(ranura);
    }

    public void CargarDesdeRanura(int numeroRanura)
    {
        if (operacionEnCurso)
            return;

        if (!RanuraValida(numeroRanura))
            return;

        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[CARGA] Debes estar en Play."
            );
            return;
        }

        operacionEnCurso = true;

        try
        {
            SaveStorage almacenamiento =
                new SaveStorage();

            if (!almacenamiento.HasValidSave(numeroRanura))
            {
                Debug.LogWarning(
                    "[CARGA] La ranura " +
                    numeroRanura +
                    " no contiene una partida valida."
                );
                return;
            }

            GameSaveSceneReferences referencias =
                ObtenerReferencias();

            // El coordinador se encarga de:
            //
            // 1. Validar el archivo.
            // 2. Comprobar que la escena esta limpia.
            // 3. Restaurar edificios y conexiones.
            // 4. Restaurar al jugador.
            // 5. Restaurar las mejoras.
            // 6. Restaurar los desbloqueos.
            // 7. Restaurar la procesadora.
            // 8. Restaurar el inventario.
            // 9. Restaurar las monedas.
            //
            // Ante un fallo, intenta recuperar
            // el estado anterior.

            GameSaveFullRestoreCoordinator.Resultado resultado =
                GameSaveFullRestoreCoordinator.Restaurar(
                    almacenamiento,
                    numeroRanura,
                    referencias
                );

            // Recuperar el tiempo acumulado
            // de la partida sin contar dos veces
            // el tiempo de esta sesion.
            segundosAcumulados =
                resultado.partida.totalPlaySeconds;

            inicioSesion = Time.timeSinceLevelLoad;

            int edificios =
                resultado.construcciones.edificios.Count;

            int conexiones =
                resultado.construcciones.conexionesRestauradas;

            Debug.Log(
                "[CARGA] PASO 85 SUPERADO: " +
                "partida real cargada. " +
                "Ranura=" + numeroRanura +
                ", monedas=" +
                resultado.partida.economy.coins +
                ", edificios=" + edificios +
                ", conexiones=" + conexiones +
                ", escena=" +
                resultado.partida.sceneName
            );

            if (resultado.desdeRespaldo)
            {
                Debug.LogWarning(
                    "[CARGA] La partida se ha recuperado " +
                    "desde el archivo de respaldo."
                );
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[CARGA] PASO 85 FALLIDO: " + ex
            );
        }
        finally
        {
            operacionEnCurso = false;
        }
    }

    // =====================================
    // VALIDAR RANURA
    // =====================================

    private bool RanuraValida(int numeroRanura)
    {
        if (numeroRanura >= 1 &&
            numeroRanura <= 3)
        {
            return true;
        }

        Debug.LogError(
            "[GUARDADO] Ranura invalida: " +
            numeroRanura
        );

        return false;
    }

    // =====================================
    // REFERENCIAS DEL JUEGO
    // =====================================

    private GameSaveSceneReferences ObtenerReferencias()
    {
        SistemaConstruccion sistema =
            SistemaConstruccion.Instancia;

        if (sistema == null)
        {
            throw new InvalidOperationException(
                "No existe SistemaConstruccion."
            );
        }

        InventarioEdificios inventario =
            sistema.inventarioEdificios;

        if (inventario == null)
        {
            throw new InvalidOperationException(
                "No existe InventarioEdificios."
            );
        }

        Cartera cartera = inventario.cartera;

        if (cartera == null)
        {
            cartera =
                inventario.GetComponentInParent<Cartera>();
        }

        if (cartera == null)
        {
            throw new InvalidOperationException(
                "No existe Cartera."
            );
        }

        DesbloqueoProcesadoraMonedas construccion =
            BuscarUnico<DesbloqueoProcesadoraMonedas>();

        if (construccion.maquinaErosion == null)
        {
            throw new InvalidOperationException(
                "No existe la referencia a MaquinaErosion."
            );
        }

        return new GameSaveSceneReferences
        {
            jugador =
                BuscarUnico<MovimientoPersonaje>().gameObject,

            sistema = sistema,
            cartera = cartera,

            equipamiento =
                BuscarUnico<GestorEquipamiento>(),

            torbellino =
                BuscarUnico<HerramientaTorbellino>(),

            iman =
                BuscarUnico<RecolectorMagnetico>(),

            procesadora =
                construccion.maquinaErosion,

            construccionProcesadora = construccion,

            inventario = inventario
        };
    }

    // =====================================
    // BUSCAR COMPONENTES DE ESCENA
    // =====================================

    private static T BuscarUnico<T>() where T : Component
    {
        T encontrado = null;
        int cantidad = 0;

        Scene escena = SceneManager.GetActiveScene();

        foreach (T componente
            in UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None))
        {
            if (componente == null ||
                componente.gameObject.scene != escena)
            {
                continue;
            }

            encontrado = componente;
            cantidad++;
        }

        if (cantidad != 1)
        {
            throw new InvalidOperationException(
                "Se esperaba un componente " +
                typeof(T).Name +
                ". Encontrados: " + cantidad
            );
        }

        return encontrado;
    }

    private void OnValidate()
    {
        ranura = Mathf.Clamp(ranura, 1, 3);
    }
}
