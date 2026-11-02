
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Paso 86:
/// F5 guarda la partida.
/// F9 carga la partida desde una escena limpia.
///
/// La sobrescritura solo se permite cuando
/// la partida corresponde a la ranura activa
/// y el archivo no ha cambiado desde entonces.
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

    // Identidad de la partida activa.
    private int ranuraActiva;
    private string idPartidaActiva;
    private string revisionActiva;

    // Tiempo acumulado antes de esta sesion.
    private double segundosAcumulados;
    private float inicioSesion;

    // =====================================
    // INICIO
    // =====================================

    private void Awake()
    {
        inicioSesion = Time.timeSinceLevelLoad;

        ranuraActiva = 0;
        idPartidaActiva = null;
        revisionActiva = null;
    }

    // =====================================
    // CONTROLES
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

                // Solo sobrescribir si:
                //
                // - Esta permitido.
                // - Es la partida activa.
                // - La revision no ha cambiado.
                // - El principal es valido.

                if (!permitirSobrescribir ||
                    !esPartidaActiva ||
                    cargadoDesdeRespaldo)
                {
                    Debug.LogWarning(
                        "[GUARDADO] PASO 86 " +
                        "SOBRESCRITURA BLOQUEADA. " +
                        "Ranura=" + numeroRanura +
                        ". Debes cargar primero esta " +
                        "misma partida, activar " +
                        "Permitir Sobrescribir y " +
                        "utilizar un archivo " +
                        "principal valido."
                    );

                    return;
                }
            }

            // =====================================
            // 2. OBTENER REFERENCIAS
            // =====================================

            GameSaveSceneReferences referencias =
                ObtenerReferencias();

            // =====================================
            // 3. CALCULAR TIEMPO JUGADO
            // =====================================

            double tiempoSesion = Math.Max(
                0.0,
                Time.timeSinceLevelLoad - inicioSesion
            );

            double segundosJugados =
                segundosAcumulados + tiempoSesion;

            // =====================================
            // 4. GUARDAR PARTIDA COMPLETA
            // =====================================

            GameSaveData guardada =
                GameSaveFullWriter.GuardarEnRanura(
                    almacenamiento,
                    numeroRanura,
                    referencias,
                    segundosJugados
                );

            // =====================================
            // 5. VERIFICAR ARCHIVO GUARDADO
            // =====================================

            GameSaveData comprobacion;
            bool verificacionDesdeRespaldo;

            if (!almacenamiento.TryLoad(
                numeroRanura,
                out comprobacion,
                out verificacionDesdeRespaldo))
            {
                throw new InvalidOperationException(
                    "No se puede leer la partida guardada."
                );
            }

            if (verificacionDesdeRespaldo ||
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

            ranuraActiva = numeroRanura;

            idPartidaActiva = guardada.saveId;

            revisionActiva = guardada.savedUtc;

            // =====================================
            // 7. RESULTADO
            // =====================================

            Debug.Log(
                "[GUARDADO] PASO 86 GUARDADO OK: " +
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
                "[GUARDADO] PASO 86 ERROR AL GUARDAR: " +
                ex
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

            // El coordinador valida la escena,
            // reconstruye edificios y conexiones
            // y restaura los sistemas del juego.
            //
            // Si una etapa falla, intenta recuperar
            // el estado anterior.

            GameSaveFullRestoreCoordinator.Resultado resultado =
                GameSaveFullRestoreCoordinator.Restaurar(
                    almacenamiento,
                    numeroRanura,
                    referencias
                );

            // =====================================
            // 4. REGISTRAR PARTIDA ACTIVA
            // =====================================

            // Solo se registra cuando la carga
            // ha terminado correctamente.

            ranuraActiva = numeroRanura;

            idPartidaActiva =
                resultado.partida.saveId;

            revisionActiva =
                resultado.partida.savedUtc;

            // =====================================
            // 5. RECUPERAR TIEMPO JUGADO
            // =====================================

            segundosAcumulados =
                resultado.partida.totalPlaySeconds;

            inicioSesion =
                Time.timeSinceLevelLoad;

            // =====================================
            // 6. COMPROBAR RESULTADO
            // =====================================

            int edificios =
                resultado.construcciones.edificios.Count;

            int conexiones =
                resultado.construcciones.conexionesRestauradas;

            Debug.Log(
                "[CARGA] PASO 86 CARGA OK: " +
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
                    "desde el archivo de respaldo. " +
                    "La sobrescritura automatica " +
                    "de esta ranura permanecera bloqueada " +
                    "hasta recuperar un principal valido."
                );
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[CARGA] PASO 86 ERROR AL CARGAR: " +
                ex
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
    // BUSCAR COMPONENTES EN ESCENA
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


    public int RanuraSeleccionada => ranura;

    public void SeleccionarRanura(int nuevaRanura)
    {
        if (nuevaRanura < 1 || nuevaRanura > 3)
        {
            Debug.LogError(
                "[GUARDADO] Ranura invalida: " + nuevaRanura
            );
            return;
        }

        if (operacionEnCurso)
            return;

        ranura = nuevaRanura;

        Debug.Log(
            "[GUARDADO] PASO 87 RANURA SELECCIONADA: " +
            ranura
        );
    }

}
