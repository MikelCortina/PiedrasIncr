
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Paso 88:
///
/// F5: guardar partida.
/// F9: cargar partida desde una escena limpia.
///
/// Permite cargar directamente una partida
/// desde el menu de ranuras.
///
/// Si es necesario, recarga la escena antes
/// de restaurar los edificios y el progreso.
///
/// Mantiene la proteccion de sobrescritura
/// implementada en el paso 86.
/// </summary>
public class GuardadoPartidaJuego : MonoBehaviour
{
    // =====================================
    // CONFIGURACION
    // =====================================

    [Header("Ranura")]
    [Range(1, 3)]
    [SerializeField]
    private int ranura = 1;

    [Header("Controles")]
    [SerializeField]
    private KeyCode teclaGuardar = KeyCode.F5;

    [SerializeField]
    private KeyCode teclaCargar = KeyCode.F9;

    [Header("Seguridad")]
    [SerializeField]
    private bool permitirSobrescribir = false;

    // =====================================
    // ESTADO INTERNO
    // =====================================

    private bool operacionEnCurso;

    private int ranuraActiva;
    private string idPartidaActiva;
    private string revisionActiva;

    private double segundosAcumulados;
    private float inicioSesion;

    // =====================================
    // PASO 88: CARGA PENDIENTE
    // =====================================

    // Static permite conservar la solicitud
    // mientras se destruye y recarga la escena.

    private static int ranuraCargaPendiente;
    private static string escenaCargaPendiente;

    // =====================================
    // AWAKE
    // =====================================

    private void Awake()
    {
        inicioSesion = Time.timeSinceLevelLoad;

        ranuraActiva = 0;
        idPartidaActiva = null;
        revisionActiva = null;
    }

    // =====================================
    // START: CARGAR TRAS RECARGAR ESCENA
    // =====================================

    private IEnumerator Start()
    {
        if (ranuraCargaPendiente == 0)
            yield break;

        int ranuraACargar = ranuraCargaPendiente;
        string escenaEsperada = escenaCargaPendiente;

        ranuraCargaPendiente = 0;
        escenaCargaPendiente = null;

        if (SceneManager.GetActiveScene().name !=
            escenaEsperada)
        {
            Debug.LogError(
                "[CARGA] PASO 88 FALLIDO: " +
                "la escena cargada no coincide."
            );

            yield break;
        }

        // Esperar a que los componentes y la
        // fisica del jugador esten preparados.

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
                escenaEsperada
            );

            // Utilizar el coordinador completo
            // que ya hemos probado.
            CargarDesdeRanura(ranuraACargar);

            yield break;
        }

        Debug.LogError(
            "[CARGA] PASO 88 FALLIDO: " +
            "el jugador no ha alcanzado suelo seguro."
        );
    }

    // =====================================
    // UPDATE
    // =====================================

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

            // =====================================
            // 1. PROTEGER PARTIDAS EXISTENTES
            // =====================================

            GameSaveData existente;
            bool cargadoDesdeRespaldo;

            if (almacenamiento.TryLoad(
                numeroRanura,
                out existente,
                out cargadoDesdeRespaldo))
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
                        "Ranura=" + numeroRanura +
                        ". Carga primero la misma partida, " +
                        "activa Permitir Sobrescribir " +
                        "y utiliza un archivo principal valido."
                    );

                    return;
                }
            }

            // =====================================
            // 2. REFERENCIAS
            // =====================================

            GameSaveSceneReferences referencias =
                ObtenerReferencias();

            // =====================================
            // 3. TIEMPO DE PARTIDA
            // =====================================

            double tiempoSesion = Math.Max(
                0.0,
                Time.timeSinceLevelLoad - inicioSesion
            );

            double segundosJugados =
                segundosAcumulados + tiempoSesion;

            // =====================================
            // 4. GUARDADO COMPLETO
            // =====================================

            GameSaveData guardada =
                GameSaveFullWriter.GuardarEnRanura(
                    almacenamiento,
                    numeroRanura,
                    referencias,
                    segundosJugados
                );

            // =====================================
            // 5. VERIFICAR ARCHIVO
            // =====================================

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

            // =====================================
            // 6. REGISTRAR PARTIDA ACTIVA
            // =====================================

            ranura = numeroRanura;
            ranuraActiva = numeroRanura;

            idPartidaActiva = guardada.saveId;
            revisionActiva = guardada.savedUtc;

            // =====================================
            // 7. RESULTADO
            // =====================================

            Debug.Log(
                "[GUARDADO] PASO 88 GUARDADO OK: " +
                "ranura=" + numeroRanura +
                ", monedas=" +
                guardada.economy.coins +
                ", edificios=" +
                guardada.placedBuildings.edificios.Count +
                ", escena=" +
                guardada.sceneName
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
                "[GUARDADO] PASO 88 ERROR AL GUARDAR: " +
                ex
            );
        }
        finally
        {
            operacionEnCurso = false;
        }
    }

    // =====================================
    // PASO 88: CARGAR DESDE EL MENU
    // =====================================

    public void CargarDesdeMenu(int numeroRanura)
    {
        if (operacionEnCurso)
            return;

        if (!RanuraValida(numeroRanura))
            return;

        if (!Application.isPlaying)
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

        // =====================================
        // 1. COMPROBAR ESCENA
        // =====================================

        if (string.IsNullOrEmpty(partida.sceneName) ||
            !Application.CanStreamedLevelBeLoaded(
                partida.sceneName))
        {
            Debug.LogError(
                "[CARGA] PASO 88 FALLIDO: " +
                "la escena no esta disponible " +
                "en Build Profiles: " +
                partida.sceneName
            );

            return;
        }

        // =====================================
        // 2. REGISTRAR CARGA PENDIENTE
        // =====================================

        ranuraCargaPendiente = numeroRanura;
        escenaCargaPendiente = partida.sceneName;

        Debug.Log(
            "[CARGA] PASO 88 INICIANDO: " +
            "ranura=" + numeroRanura +
            ", escena=" + partida.sceneName
        );

        // =====================================
        // 3. RECARGAR ESCENA LIMPIA
        // =====================================

        try
        {
            // Incluso si ya estamos en Piedra,
            // recargamos la escena para no
            // restaurar sobre edificios existentes.

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

            // =====================================
            // 1. COMPROBAR RANURA
            // =====================================

            if (!almacenamiento.HasValidSave(numeroRanura))
            {
                Debug.LogWarning(
                    "[CARGA] La ranura " +
                    numeroRanura +
                    " no contiene una partida valida."
                );

                return;
            }

            // =====================================
            // 2. OBTENER REFERENCIAS
            // =====================================

            GameSaveSceneReferences referencias =
                ObtenerReferencias();

            // =====================================
            // 3. RESTAURAR PARTIDA
            // =====================================

            GameSaveFullRestoreCoordinator.Resultado resultado =
                GameSaveFullRestoreCoordinator.Restaurar(
                    almacenamiento,
                    numeroRanura,
                    referencias
                );

            // =====================================
            // 4. REGISTRAR PARTIDA ACTIVA
            // =====================================

            // Importante:
            // F5 seguira guardando en la ranura
            // que acabamos de cargar.

            ranura = numeroRanura;
            ranuraActiva = numeroRanura;

            idPartidaActiva =
                resultado.partida.saveId;

            revisionActiva =
                resultado.partida.savedUtc;

            // =====================================
            // 5. RESTAURAR TIEMPO
            // =====================================

            segundosAcumulados =
                resultado.partida.totalPlaySeconds;

            inicioSesion =
                Time.timeSinceLevelLoad;

            // =====================================
            // 6. RESULTADO
            // =====================================

            int edificios =
                resultado.construcciones.edificios.Count;

            int conexiones =
                resultado.construcciones.conexionesRestauradas;

            Debug.Log(
                "[CARGA] PASO 88 SUPERADO: " +
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
                    "[CARGA] Se ha recuperado la partida " +
                    "desde el archivo de respaldo. " +
                    "La sobrescritura de esta ranura " +
                    "permanecera bloqueada mientras " +
                    "el principal no sea valido."
                );
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[CARGA] PASO 88 FALLIDO: " + ex
            );
        }
        finally
        {
            operacionEnCurso = false;
        }
    }

    // =====================================
    // PASO 87: SELECCION DE RANURAS
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
            "[GUARDADO] PASO 88 RANURA SELECCIONADA: " +
            ranura
        );
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

        Cartera cartera =
            inventario.cartera;

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

        Scene escena =
            SceneManager.GetActiveScene();

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
                ". Encontrados: " +
                cantidad
            );
        }

        return encontrado;
    }

    // =====================================
    // INSPECTOR
    // =====================================

    private void OnValidate()
    {
        ranura = Mathf.Clamp(ranura, 1, 3);
    }
}
