
using System.IO;
using TMPro;
using UnityEngine;

/// <summary>
/// Paso 87:
/// Panel de seleccion de tres ranuras.
///
/// F6 abre y cierra el panel.
/// F5 guarda en la ranura seleccionada.
/// F9 carga desde la ranura seleccionada.
/// </summary>
public class MenuRanurasGuardado : MonoBehaviour
{
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

    private SaveStorage almacenamiento;

    private bool abierto;

    private CursorLockMode bloqueoAnterior;
    private bool cursorVisibleAnterior;

    private float siguienteActualizacion;

    private void Awake()
    {
        almacenamiento = new SaveStorage();

        if (sistemaGuardado == null)
        {
            sistemaGuardado =
                FindFirstObjectByType<GuardadoPartidaJuego>();
        }

        if (panel != null)
            panel.SetActive(false);
    }

    private void Start()
    {
        if (sistemaGuardado == null)
        {
            Debug.LogError(
                "[GUARDADO] PASO 87: " +
                "No existe GuardadoPartidaJuego."
            );

            enabled = false;
            return;
        }

        ActualizarRanuras();
    }

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
        if (panel == null)
            return;

        if (abierto)
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
            panel.SetActive(false);

        Cursor.lockState = bloqueoAnterior;
        Cursor.visible = cursorVisibleAnterior;
    }

    // =====================================
    // SELECCIONAR RANURAS
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

    private void Seleccionar(int numero)
    {
        if (sistemaGuardado == null)
            return;

        sistemaGuardado.SeleccionarRanura(numero);

        ActualizarRanuras();

        Debug.Log(
            "[GUARDADO] PASO 87 SELECCION OK: ranura " +
            numero
        );
    }

    // =====================================
    // ACTUALIZAR INFORMACION
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

        string estado = ObtenerEstado(numero);

        texto.text =
            prefijo +
            "Ranura " + numero +
            " - " + estado;
    }

    // =====================================
    // CONSULTAR ESTADO DEL ARCHIVO
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

        // Distinguir una ranura vacia de
        // una que contiene archivos ilegibles.
        if (File.Exists(principal) ||
            File.Exists(principal + ".bak") ||
            File.Exists(principal + ".tmp"))
        {
            return "ARCHIVOS NO VALIDOS";
        }

        return "VACIA";
    }

    private void OnDisable()
    {
        if (abierto)
            CerrarPanel();
    }
}
