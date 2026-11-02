
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Paso 89:
/// F5 guarda.
/// F9 carga desde una escena limpia.
///
/// Permite cargar partidas existentes y
/// comenzar partidas nuevas en ranuras vacias.
/// </summary>
public class GuardadoPartidaJuego : MonoBehaviour
{
    [Header("Ranura")]
    [Range(1, 3)]
    [SerializeField]
    private int ranura = 1;

    [Header("Controles")]
    [SerializeField]
    private KeyCode teclaGuardar = KeyCode.F5;

    [SerializeField]
    private KeyCode teclaCargar = KeyCode.F9;

    [Header("Nueva partida")]
    [SerializeField]
    private string escenaNuevaPartida = "Piedra";

    [Header("Seguridad")]
    [SerializeField]
    private bool permitirSobrescribir = false;

    private bool operacionEnCurso;

    private int ranuraActiva;
    private string idPartidaActiva;
    private string revisionActiva;

    private double segundosAcumulados;
    private float inicioSesion;

    // Solicitudes que sobreviven a la recarga
    // de la escena.
    private static int ranuraCargaPendiente;
    private static string escenaCargaPendiente;

    private static int ranuraNuevaPendiente;
    private static string escenaNuevaPendiente;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarSolicitudes()
    {
        ranuraCargaPendiente = 0;
        escenaCargaPendiente = null;

        ranuraNuevaPendiente = 0;
        escenaNuevaPendiente = null;
    }

    private void Awake()
    {
        inicioSesion = Time.timeSinceLevelLoad;

        ranuraActiva = 0;
        idPartidaActiva = null;
        revisionActiva = null;
    }

    // =====================================
    // INICIO TRAS RECARGAR LA ESCENA
    // =====================================

    private IEnumerator Start()
    {
        // PASO 89: NUEVA PARTIDA
        if (ranuraNuevaPendiente != 0)
        {
            int nuevaRanura = ranuraNuevaPendiente;
            string escenaEsperada = escenaNuevaPendiente;

            ranuraNuevaPendiente = 0;
            escenaNuevaPendiente = null;

            if (SceneManager.GetActiveScene().name !=
                escenaEsperada)
            {
                Debug.LogError(
                    "[NUEVA PARTIDA] PASO 89 FALLIDO: " +
                    "escena incorrecta."
                );

                enabled = false;
                yield break;
            }

            SaveStorage almacenamiento =
                new SaveStorage();

            if (!RanuraValida(nuevaRanura) ||
                RanuraTieneArchivos(
                    almacenamiento,
                    nuevaRanura))
            {
                Debug.LogError(
                    "[NUEVA PARTIDA] PASO 89 BLOQUEADO: " +
                    "la ranura ya contiene archivos. " +
                    "No se ha sobrescrito nada."
                );

                enabled = false;
                yield break;
            }

            // Escena recien cargada:
            // conservar sus valores iniciales.
            ranura = nuevaRanura;

            ranuraActiva = 0;
            idPartidaActiva = null;
            revisionActiva = null;

            segundosAcumulados = 0;
            inicioSesion = Time.timeSinceLevelLoad;

            Debug.Log(
                "[NUEVA PARTIDA] PASO 89 SUPERADO: " +
                "ranura=" + nuevaRanura +
                ", escena=" + escenaEsperada +
                ", tiempoInicial=0"
            );

            yield break;
        }

        // PASO 88: CARGAR PARTIDA EXISTENTE
        if (ranuraCargaPendiente == 0)
            yield break;

        int ranuraACargar = ranuraCargaPendiente;
        string escenaCargada = escenaCargaPendiente;

        ranuraCargaPendiente = 0;
        escenaCargaPendiente = null;

        if (SceneManager.GetActiveScene().name !=
            escenaCargada)
        {
            Debug.LogError(
                "[CARGA] PASO 88 FALLIDO: " +
                "la escena cargada no coincide."
            );

            enabled = false;
            yield break;
        }

        const int maxIntentos = 180;

        for (int intento = 0;
             intento < maxIntentos;
             intento++)
        {
            yield return new WaitForFixedUpdate();

            MovimientoPersonaje movimiento =
                FindFirstObjectByType<MovimientoPersonaje>();

            if (movimiento == null)
                continue;

            CharacterController controlador =
                movimiento.GetComponent<CharacterController>();

            if (controlador == null ||
                !controlador.enabled ||
                !controlador.isGrounded ||
                !movimiento.SueloDetectado)
            {
                continue;
            }

            Debug.Log(
                "[CARGA] PASO 88 ESCENA PREPARADA: " +
                escenaCargada
            );

            CargarDesdeRanura(ranuraACargar);
            yield break;
        }

        Debug.LogError(
            "[CARGA] PASO 88 FALLIDO: " +
            "el jugador no ha alcanzado suelo seguro."
        );
    }

    private void Update()
    {
        if (operacionEnCurso)
            return;

        if (Input.GetKeyDown(teclaGuardar))
            Guardar();
        else if (Input.GetKeyDown(teclaCargar))
            Cargar();
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

            GameSaveData existente;
            bool cargadoDesdeRespaldo;

            bool existePartidaValida =
                almacenamiento.TryLoad(
                    numeroRanura,
                    out existente,
                    out cargadoDesdeRespaldo
                );

            if (existePartidaValida)
            {
                bool esPartidaActiva =
                    ranuraActiva == numeroRanura &&

                    string.Equals(
                        idPartidaActiva,
                        existente.saveId,
                        StringComparison.Ordinal
                    ) &&

                    string.Equals(
                        revisionActiva,
                        existente.savedUtc,
                        StringComparison.Ordinal
                    );

                if (!permitirSobrescribir ||
                    !esPartidaActiva ||
                    cargadoDesdeRespaldo)
                {
                    Debug.LogWarning(
                        "[GUARDADO] SOBRESCRITURA BLOQUEADA. " +
                        "Carga primero la misma partida " +
                        "y activa Permitir Sobrescribir."
                    );

                    return;
                }
            }
            else if (RanuraTieneArchivos(
                almacenamiento,
                numeroRanura))
            {
                // No escribir sobre archivos ilegibles,
                // respaldos o temporales pendientes.
                Debug.LogError(
                    "[GUARDADO] PASO 89 BLOQUEADO: " +
                    "existen archivos no validos " +
                    "en la ranura " + numeroRanura
                );

                return;
            }

            GameSaveSceneReferences referencias =
                ObtenerReferencias();

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
                comprobacion.savedUtc != guardada.savedUtc ||
                comprobacion.economy.coins !=
                    guardada.economy.coins ||
                comprobacion.placedBuildings.edificios.Count !=
                    guardada.placedBuildings.edificios.Count)
            {
                throw new InvalidOperationException(
                    "La verificacion del guardado ha fallado."
                );
            }

            ranura = numeroRanura;
            ranuraActiva = numeroRanura;

            idPartidaActiva = guardada.saveId;
            revisionActiva = guardada.savedUtc;

            Debug.Log(
                "[GUARDADO] PASO 89 GUARDADO OK: " +
                "ranura=" + numeroRanura +
                ", monedas=" + guardada.economy.coins +
                ", edificios=" +
                guardada.placedBuildings.edificios.Count +
                ", escena=" + guardada.sceneName
            );

            Debug.Log(
                "[GUARDADO] Archivo: " +
                almacenamiento.GetPrimaryPathForDiagnostics(
                    numeroRanura
                )
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[GUARDADO] PASO 89 ERROR: " + ex
            );
        }
        finally
        {
            operacionEnCurso = false;
        }
    }

    // =====================================
    // PASO 89: CREAR NUEVA PARTIDA
    // =====================================

    public void IniciarNuevaPartidaDesdeMenu(
        int numeroRanura)
    {
        if (operacionEnCurso ||
            !Application.isPlaying)
            return;

        if (!RanuraValida(numeroRanura))
            return;

        if (ranuraCargaPendiente != 0 ||
            ranuraNuevaPendiente != 0)
            return;

        SaveStorage almacenamiento =
            new SaveStorage();

        // Comprobar tanto guardados validos como
        // archivos que pudieran estar dañados.
        if (RanuraTieneArchivos(
            almacenamiento,
            numeroRanura))
        {
            Debug.LogWarning(
                "[NUEVA PARTIDA] PASO 89 BLOQUEADO: " +
                "la ranura " + numeroRanura +
                " no esta completamente vacia."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(
                escenaNuevaPartida) ||
            !Application.CanStreamedLevelBeLoaded(
                escenaNuevaPartida))
        {
            Debug.LogError(
                "[NUEVA PARTIDA] Escena no disponible " +
                "en Build Profiles: " +
                escenaNuevaPartida
            );

            return;
        }

        ranuraNuevaPendiente = numeroRanura;
        escenaNuevaPendiente = escenaNuevaPartida;

        Debug.Log(
            "[NUEVA PARTIDA] PASO 89 INICIANDO: " +
            "ranura=" + numeroRanura +
            ", escena=" + escenaNuevaPartida
        );

        try
        {
            SceneManager.LoadScene(
                escenaNuevaPartida,
                LoadSceneMode.Single
            );
        }
        catch (Exception ex)
        {
            ranuraNuevaPendiente = 0;
            escenaNuevaPendiente = null;

            Debug.LogError(
                "[NUEVA PARTIDA] PASO 89 ERROR: " + ex
            );
        }
    }

    // =====================================
    // PASO 88: CARGA DIRECTA DESDE MENU
    // =====================================

    public void CargarDesdeMenu(int numeroRanura)
    {
        if (operacionEnCurso ||
            !Application.isPlaying)
            return;

        if (!RanuraValida(numeroRanura))
            return;

        if (ranuraCargaPendiente != 0 ||
            ranuraNuevaPendiente != 0)
            return;

        SaveStorage almacenamiento =
            new SaveStorage();

        GameSaveData partida;
        bool desdeRespaldo;

        if (!almacenamiento.TryLoad(
            numeroRanura,
            out partida,
            out desdeRespaldo))
        {
            Debug.LogWarning(
                "[CARGA] La ranura " +
                numeroRanura +
                " no contiene una partida valida."
            );

            return;
        }

        if (string.IsNullOrEmpty(partida.sceneName) ||
            !Application.CanStreamedLevelBeLoaded(
                partida.sceneName))
        {
            Debug.LogError(
                "[CARGA] Escena no disponible: " +
                partida.sceneName
            );

            return;
        }

        ranuraCargaPendiente = numeroRanura;
        escenaCargaPendiente = partida.sceneName;

        Debug.Log(
            "[CARGA] PASO 88 INICIANDO: " +
            "ranura=" + numeroRanura +
            ", escena=" + partida.sceneName
        );

        try
        {
            SceneManager.LoadScene(
                partida.sceneName,
                LoadSceneMode.Single
            );
        }
        catch (Exception ex)
        {
            ranuraCargaPendiente = 0;
            escenaCargaPendiente = null;

            Debug.LogError(
                "[CARGA] PASO 88 ERROR: " + ex
            );
        }
    }

    // =====================================
    // CARGAR CON F9
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

            GameSaveFullRestoreCoordinator.Resultado resultado =
                GameSaveFullRestoreCoordinator.Restaurar(
                    almacenamiento,
                    numeroRanura,
                    referencias
                );

            ranura = numeroRanura;
            ranuraActiva = numeroRanura;

            idPartidaActiva =
                resultado.partida.saveId;

            revisionActiva =
                resultado.partida.savedUtc;

            segundosAcumulados =
                resultado.partida.totalPlaySeconds;

            inicioSesion = Time.timeSinceLevelLoad;

            int edificios =
                resultado.construcciones.edificios.Count;

            int conexiones =
                resultado.construcciones.conexionesRestauradas;

            Debug.Log(
                "[CARGA] PASO 89 CARGA OK: " +
                "ranura=" + numeroRanura +
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
                    "desde un respaldo. " +
                    "La sobrescritura sigue protegida."
                );
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[CARGA] PASO 89 FALLIDO: " + ex
            );
        }
        finally
        {
            operacionEnCurso = false;
        }
    }

    // =====================================
    // SELECCION DE RANURA
    // =====================================

    public int RanuraSeleccionada => ranura;

    public void SeleccionarRanura(int nuevaRanura)
    {
        if (!RanuraValida(nuevaRanura))
            return;

        if (operacionEnCurso)
            return;

        ranura = nuevaRanura;

        Debug.Log(
            "[GUARDADO] PASO 89 RANURA SELECCIONADA: " +
            ranura
        );
    }

    // =====================================
    // SEGURIDAD DE ARCHIVOS
    // =====================================

    private static bool RanuraTieneArchivos(
        SaveStorage almacenamiento,
        int numeroRanura)
    {
        string principal =
            almacenamiento.GetPrimaryPathForDiagnostics(
                numeroRanura
            );

        return File.Exists(principal) ||
               File.Exists(principal + ".bak") ||
               File.Exists(principal + ".tmp");
    }

    private bool RanuraValida(int numeroRanura)
    {
        if (numeroRanura >= 1 &&
            numeroRanura <= 3)
            return true;

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
    // BUSCAR COMPONENTE DE ESCENA
    // =====================================

    private static T BuscarUnico<T>()
        where T : Component
    {
        T encontrado = null;
        int cantidad = 0;

        Scene escena = SceneManager.GetActiveScene();

        foreach (T componente in
            UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None))
        {
            if (componente == null ||
                componente.gameObject.scene != escena)
                continue;

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
