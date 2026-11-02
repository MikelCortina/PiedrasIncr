
using System.IO;
using TMPro;
using UnityEngine;

/// <summary>
/// Paso 88:
///
/// F6 abre y cierra el panel.
/// Una ranura ocupada carga su partida.
/// Una ranura vacia solo se selecciona.
///
/// F5 y F9 siguen funcionando.
/// </summary>
public class MenuRanurasGuardado : MonoBehaviour
{
    // =====================================
    // REFERENCIAS
    // =====================================

    [Header("Sistema")]
    [SerializeField]
    private GuardadoPartidaJuego sistemaGuardado;

    [Header("Panel")]
    [SerializeField]
    private GameObject panel;

    [Header("Textos de ranuras")]
    [SerializeField]
    private TMP_Text textoRanura1;

    [SerializeField]
    private TMP_Text textoRanura2;

    [SerializeField]
    private TMP_Text textoRanura3;

    [Header("Controles")]
    [SerializeField]
    private KeyCode teclaMenu = KeyCode.F6;

    // =====================================
    // ESTADO INTERNO
    // =====================================

    private SaveStorage almacenamiento;

    private bool abierto;

    private CursorLockMode bloqueoAnterior;
    private bool cursorVisibleAnterior;

    private float siguienteActualizacion;

    // =====================================
    // AWAKE
    // =====================================

    private void Awake()
    {
        almacenamiento = new SaveStorage();

        if (sistemaGuardado == null)
        {
            sistemaGuardado =
                FindFirstObjectByType<GuardadoPartidaJuego>();
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    // =====================================
    // START
    // =====================================

    private void Start()
    {
        if (sistemaGuardado == null)
        {
            Debug.LogError(
                "[GUARDADO] PASO 88: " +
                "No existe GuardadoPartidaJuego."
            );

            enabled = false;
            return;
        }

        ActualizarRanuras();
    }

    // =====================================
    // UPDATE
    // =====================================

    private void Update()
    {
        if (Input.GetKeyDown(teclaMenu))
        {
            AlternarPanel();
        }

        if (!abierto)
            return;

        if (Time.unscaledTime >= siguienteActualizacion)
        {
            ActualizarRanuras();

            siguienteActualizacion =
                Time.unscaledTime + 1f;
        }
    }

    // =====================================
    // ABRIR Y CERRAR PANEL
    // =====================================

    public void AlternarPanel()
    {
        if (panel == null)
        {
            Debug.LogError(
                "[GUARDADO] Falta asignar el panel."
            );

            return;
        }

        if (abierto)
            CerrarPanel();
        else
            AbrirPanel();
    }

    public void AbrirPanel()
    {
        if (panel == null || abierto)
            return;

        bloqueoAnterior = Cursor.lockState;
        cursorVisibleAnterior = Cursor.visible;

        abierto = true;

        panel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ActualizarRanuras();
    }

    public void CerrarPanel()
    {
        if (!abierto)
            return;

        abierto = false;

        if (panel != null)
        {
            panel.SetActive(false);
        }

        Cursor.lockState = bloqueoAnterior;
        Cursor.visible = cursorVisibleAnterior;
    }

    // =====================================
    // BOTONES DE RANURAS
    // =====================================

    public void SeleccionarRanura1()
    {
        Seleccionar(1);
    }

    public void SeleccionarRanura2()
    {
        Seleccionar(2);
    }

    public void SeleccionarRanura3()
    {
        Seleccionar(3);
    }

    // =====================================
    // PASO 88: SELECCION Y CARGA
    // =====================================

    private void Seleccionar(int numero)
    {
        if (sistemaGuardado == null ||
            almacenamiento == null)
        {
            return;
        }

        // Primero seleccionar la ranura.
        sistemaGuardado.SeleccionarRanura(numero);

        ActualizarRanuras();

        GameSaveData partida;
        bool desdeRespaldo;

        // =====================================
        // RANURA OCUPADA
        // =====================================

        if (almacenamiento.TryLoad(
            numero,
            out partida,
            out desdeRespaldo))
        {
            Debug.Log(
                "[GUARDADO] PASO 88 CARGAR RANURA: " +
                numero
            );

            // Recargar la escena y restaurar
            // la partida seleccionada.
            sistemaGuardado.CargarDesdeMenu(numero);
        }
        else
        {
            // =====================================
            // RANURA VACIA O NO VALIDA
            // =====================================

            Debug.Log(
                "[GUARDADO] PASO 88 RANURA SIN " +
                "PARTIDA VALIDA: " + numero
            );

            // Por ahora no se crea una partida nueva.
            // La ranura permanece seleccionada.
        }
    }

    // =====================================
    // ACTUALIZAR LAS TRES RANURAS
    // =====================================

    public void ActualizarRanuras()
    {
        if (sistemaGuardado == null ||
            almacenamiento == null)
        {
            return;
        }

        ActualizarTexto(textoRanura1, 1);
        ActualizarTexto(textoRanura2, 2);
        ActualizarTexto(textoRanura3, 3);
    }

    private void ActualizarTexto(
        TMP_Text texto,
        int numero)
    {
        if (texto == null)
            return;

        bool seleccionada =
            sistemaGuardado.RanuraSeleccionada == numero;

        string prefijo =
            seleccionada ? "> " : "";

        string estado =
            ObtenerEstado(numero);

        texto.text =
            prefijo +
            "Ranura " + numero +
            " - " + estado;
    }

    // =====================================
    // CONSULTAR ESTADO DE UNA RANURA
    // =====================================

    private string ObtenerEstado(int numero)
    {
        GameSaveData partida;
        bool desdeRespaldo;

        if (almacenamiento.TryLoad(
            numero,
            out partida,
            out desdeRespaldo))
        {
            int edificios = 0;

            if (partida.placedBuildings != null &&
                partida.placedBuildings.edificios != null)
            {
                edificios =
                    partida.placedBuildings.edificios.Count;
            }

            string estado =
                desdeRespaldo
                    ? "RESPALDO DISPONIBLE"
                    : "OCUPADA";

            return estado +
                " | Monedas: " +
                partida.economy.coins +
                " | Edificios: " +
                edificios;
        }

        string principal =
            almacenamiento.GetPrimaryPathForDiagnostics(
                numero
            );

        // Distinguir una ranura vacia
        // de una con archivos corruptos.

        if (File.Exists(principal) ||
            File.Exists(principal + ".bak") ||
            File.Exists(principal + ".tmp"))
        {
            return "ARCHIVOS NO VALIDOS";
        }

        return "VACIA";
    }

    // =====================================
    // DESACTIVACION
    // =====================================

    private void OnDisable()
    {
        if (abierto)
        {
            CerrarPanel();
        }
    }
}
