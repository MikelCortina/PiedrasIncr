
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Paso 84:
/// Permite guardar una partida real desde el juego.
/// F5 guarda en la ranura seleccionada.
///
/// No restaura partidas ni modifica el mundo.
/// </summary>
public class GuardadoPartidaJuego : MonoBehaviour
{
    [Header("Guardado")]
    [Range(1, 3)]
    [SerializeField] private int ranura = 1;

    [SerializeField]
    private KeyCode teclaGuardar = KeyCode.F5;

    [Header("Seguridad")]
    [SerializeField]
    private bool permitirSobrescribir = false;

    private bool guardando;

    private void Update()
    {
        if (Input.GetKeyDown(teclaGuardar))
        {
            Guardar();
        }
    }

    // =====================================
    // GUARDAR EN LA RANURA CONFIGURADA
    // =====================================

    public void Guardar()
    {
        GuardarEnRanura(ranura);
    }

    // =====================================
    // GUARDAR EN UNA RANURA CONCRETA
    // =====================================

    public void GuardarEnRanura(int numeroRanura)
    {
        if (guardando)
            return;

        if (numeroRanura < 1 || numeroRanura > 3)
        {
            Debug.LogError(
                "[GUARDADO] Ranura invalida: " +
                numeroRanura
            );
            return;
        }

        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[GUARDADO] Debes estar en Play."
            );
            return;
        }

        guardando = true;

        try
        {
            SaveStorage almacenamiento =
                new SaveStorage();

            // Evitar sobrescribir una partida
            // existente sin permiso.
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

            // Buscar los componentes reales.
            GameSaveSceneReferences referencias =
                ObtenerReferencias();

            // Tiempo desde que se cargo esta escena.
            // La acumulacion entre sesiones se
            // integrara junto con la carga.
            double segundosJugados =
                Time.timeSinceLevelLoad;

            // Capturar y escribir el guardado completo.
            GameSaveData guardada =
                GameSaveFullWriter.GuardarEnRanura(
                    almacenamiento,
                    numeroRanura,
                    referencias,
                    segundosJugados
                );

            // Comprobar que se puede leer de disco.
            GameSaveData comprobacion;
            bool desdeRespaldo;

            if (!almacenamiento.TryLoad(
                numeroRanura,
                out comprobacion,
                out desdeRespaldo))
            {
                throw new InvalidOperationException(
                    "No se puede leer el archivo guardado."
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
                "[GUARDADO] PASO 84 SUPERADO: " +
                "partida real guardada en ranura " +
                numeroRanura +
                ". Monedas=" +
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
                "[GUARDADO] PASO 84 FALLIDO: " + ex
            );
        }
        finally
        {
            guardando = false;
        }
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
    // BUSCAR COMPONENTE EN ESCENA
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
