using System;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menú principal conectado al sistema de guardado.
///
/// - Continuar: carga la partida guardada más reciente.
/// - Nueva partida: elige ranura. Si está ocupada pide confirmación, la borra y empieza de cero.
/// - Cargar partida: solo se pueden elegir ranuras con una partida válida.
/// </summary>
public class MenuPrincipalGuardado : MonoBehaviour
{
    private const int Ranuras = 3;

    [Header("Sistema de guardado")]
    [SerializeField]
    private GuardadoPartidaJuego sistemaGuardado;

    [Header("Menu principal")]
    [Tooltip("Botones principales. Se ocultan al elegir ranura.")]
    [SerializeField]
    private GameObject panelPrincipal;

    [SerializeField]
    private Button botonContinuar;

    [SerializeField]
    private Button botonCargar;

    [Tooltip("Opcional: texto del boton Continuar. Dice que ranura se va a cargar.")]
    [SerializeField]
    private TMP_Text textoContinuar;

    [Tooltip("Avisos al jugador. No debe ser el texto de un boton.")]
    [SerializeField]
    private TMP_Text textoMensaje;

    [Header("Seleccion de ranura")]
    [SerializeField]
    private GameObject panelSeleccionRanura;

    [SerializeField]
    private TMP_Text tituloSeleccion;

    [Header("Botones de ranura")]
    [SerializeField]
    private Button botonRanura1;

    [SerializeField]
    private Button botonRanura2;

    [SerializeField]
    private Button botonRanura3;

    [Header("Textos de ranura")]
    [SerializeField]
    private TMP_Text textoRanura1;

    [SerializeField]
    private TMP_Text textoRanura2;

    [SerializeField]
    private TMP_Text textoRanura3;

    [Header("Confirmar sobrescritura")]
    [SerializeField]
    private GameObject panelConfirmacion;

    [SerializeField]
    private TMP_Text textoConfirmacion;

    private SaveStorage almacenamiento;

    private enum ModoSeleccion
    {
        Ninguno,
        NuevaPartida,
        CargarPartida
    }

    private ModoSeleccion modoActual;

    // Ranura ocupada que espera confirmación para empezar de cero (0 = ninguna).
    private int ranuraPorConfirmar;

    private void Awake()
    {
        // Se puede llegar desde una partida pausada o desde el final de la demo.
        Time.timeScale = 1f;
        AudioListener.pause = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        almacenamiento = new SaveStorage();
        modoActual = ModoSeleccion.Ninguno;

        Mostrar(panelConfirmacion, false);
        Mostrar(panelSeleccionRanura, false);
        Mostrar(panelPrincipal, true);
        MostrarMensaje("");
    }

    private void Start()
    {
        if (sistemaGuardado == null)
        {
            Debug.LogError("[MENU] Falta SistemaGuardadoMenu.");
            enabled = false;
            return;
        }

        ActualizarMenuPrincipal();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (ranuraPorConfirmar != 0) CancelarSobrescritura();
        else if (modoActual != ModoSeleccion.Ninguno) Volver();
    }

    // =====================================
    // MENU PRINCIPAL
    // =====================================

    public void NuevaPartida()
    {
        MostrarSeleccion(ModoSeleccion.NuevaPartida, "NUEVA PARTIDA");
    }

    public void CargarPartida()
    {
        MostrarSeleccion(ModoSeleccion.CargarPartida, "CARGAR PARTIDA");
    }

    public void ContinuarPartida()
    {
        int ultimaRanura = ObtenerUltimaRanura();

        if (ultimaRanura == 0)
        {
            MostrarMensaje("No hay partidas guardadas.");
            ActualizarMenuPrincipal();
            return;
        }

        Cargar(ultimaRanura);
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // =====================================
    // ELEGIR RANURA
    // =====================================

    public void ElegirRanura1()
    {
        ElegirRanura(1);
    }

    public void ElegirRanura2()
    {
        ElegirRanura(2);
    }

    public void ElegirRanura3()
    {
        ElegirRanura(3);
    }

    private void ElegirRanura(int numero)
    {
        if (modoActual == ModoSeleccion.CargarPartida)
        {
            if (!almacenamiento.HasValidSave(numero))
            {
                MostrarMensaje("La ranura " + numero + " no contiene una partida válida.");
                ActualizarRanuras();
                return;
            }

            Cargar(numero);
        }
        else if (modoActual == ModoSeleccion.NuevaPartida)
        {
            if (TieneArchivos(numero))
                PedirConfirmacion(numero);
            else
                Empezar(numero);
        }
    }

    private void Cargar(int numero)
    {
        MostrarMensaje("Cargando la ranura " + numero + "...");
        Debug.Log("[MENU] Cargar: ranura=" + numero);
        sistemaGuardado.CargarDesdeMenu(numero);
    }

    private void Empezar(int numero)
    {
        MostrarMensaje("Empezando una partida nueva...");
        Debug.Log("[MENU] Nueva partida: ranura=" + numero);
        sistemaGuardado.IniciarNuevaPartidaDesdeMenu(numero);
    }

    // =====================================
    // RANURA OCUPADA: CONFIRMAR
    // =====================================

    private void PedirConfirmacion(int numero)
    {
        if (panelConfirmacion == null)
        {
            MostrarMensaje("La ranura " + numero + " no está vacía.");
            return;
        }

        ranuraPorConfirmar = numero;

        if (textoConfirmacion != null)
        {
            textoConfirmacion.text =
                "La ranura " + numero + " ya tiene una partida.\n" +
                "Si empiezas de cero, se borrará y no se podrá recuperar.";
        }

        Mostrar(panelConfirmacion, true);
    }

    public void ConfirmarSobrescritura()
    {
        int numero = ranuraPorConfirmar;
        ranuraPorConfirmar = 0;
        Mostrar(panelConfirmacion, false);

        if (numero == 0) return;

        try
        {
            almacenamiento.DeleteSlot(numero);
        }
        catch (Exception ex)
        {
            Debug.LogError("[MENU] No se ha podido borrar la ranura " + numero + ": " + ex);
        }

        if (TieneArchivos(numero))
        {
            MostrarMensaje("No se ha podido borrar la ranura " + numero + ".");
            ActualizarRanuras();
            return;
        }

        Empezar(numero);
    }

    public void CancelarSobrescritura()
    {
        ranuraPorConfirmar = 0;
        Mostrar(panelConfirmacion, false);
    }

    // =====================================
    // PANELES
    // =====================================

    private void MostrarSeleccion(ModoSeleccion modo, string titulo)
    {
        modoActual = modo;

        if (tituloSeleccion != null)
            tituloSeleccion.text = titulo;

        MostrarMensaje("");
        Mostrar(panelPrincipal, false);
        Mostrar(panelSeleccionRanura, true);
        ActualizarRanuras();
    }

    public void Volver()
    {
        modoActual = ModoSeleccion.Ninguno;

        CancelarSobrescritura();
        MostrarMensaje("");
        Mostrar(panelSeleccionRanura, false);
        Mostrar(panelPrincipal, true);
        ActualizarMenuPrincipal();
    }

    private static void Mostrar(GameObject panel, bool visible)
    {
        if (panel != null && panel.activeSelf != visible)
            panel.SetActive(visible);
    }

    // El color de "desactivado" del botón no llega a su texto: se atenúa aquí.
    private static void Habilitar(Button boton, bool activo)
    {
        if (boton == null) return;

        boton.interactable = activo;

        TMP_Text texto = boton.GetComponentInChildren<TMP_Text>(true);
        if (texto != null) texto.alpha = activo ? 1f : 0.35f;
    }

    private void ActualizarMenuPrincipal()
    {
        int ultimaRanura = ObtenerUltimaRanura();

        Habilitar(botonContinuar, ultimaRanura != 0);
        Habilitar(botonCargar, ultimaRanura != 0);

        if (textoContinuar != null)
        {
            textoContinuar.text = ultimaRanura != 0
                ? "CONTINUAR  <size=60%>RANURA " + ultimaRanura + "</size>"
                : "CONTINUAR";
        }
    }

    // =====================================
    // RANURAS
    // =====================================

    private void ActualizarRanuras()
    {
        ActualizarRanura(1, botonRanura1, textoRanura1);
        ActualizarRanura(2, botonRanura2, textoRanura2);
        ActualizarRanura(3, botonRanura3, textoRanura3);
    }

    private void ActualizarRanura(int numero, Button boton, TMP_Text texto)
    {
        GameSaveData partida;
        bool desdeRespaldo;
        bool existe = almacenamiento.TryLoad(numero, out partida, out desdeRespaldo);

        // Para empezar de cero vale cualquier ranura (si está ocupada se confirma antes).
        Habilitar(boton, existe || modoActual == ModoSeleccion.NuevaPartida);

        if (texto == null) return;

        string detalle;

        if (existe)
        {
            detalle =
                partida.economy.coins + " monedas · " +
                FormatoTiempo(partida.totalPlaySeconds) + " jugados · " +
                FormatoFecha(partida.savedUtc);

            if (desdeRespaldo) detalle += " · respaldo";
        }
        else
        {
            detalle = TieneArchivos(numero) ? "Archivos dañados" : "Vacía";
        }

        texto.text = "RANURA " + numero + "\n<size=60%>" + detalle + "</size>";
    }

    private bool TieneArchivos(int numero)
    {
        string principal = almacenamiento.GetPrimaryPathForDiagnostics(numero);

        return File.Exists(principal) ||
               File.Exists(principal + ".bak") ||
               File.Exists(principal + ".tmp");
    }

    // Partida guardada más reciente (0 = no hay ninguna).
    private int ObtenerUltimaRanura()
    {
        int mejorRanura = 0;
        DateTimeOffset mejorFecha = DateTimeOffset.MinValue;

        for (int numero = 1; numero <= Ranuras; numero++)
        {
            GameSaveData partida;
            bool desdeRespaldo;

            if (!almacenamiento.TryLoad(numero, out partida, out desdeRespaldo))
                continue;

            DateTimeOffset fecha;

            if (!LeerFecha(partida.savedUtc, out fecha))
                continue;

            if (mejorRanura == 0 || fecha > mejorFecha)
            {
                mejorRanura = numero;
                mejorFecha = fecha;
            }
        }

        return mejorRanura;
    }

    // =====================================
    // TEXTOS
    // =====================================

    private static bool LeerFecha(string utc, out DateTimeOffset fecha)
    {
        return DateTimeOffset.TryParse(
            utc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out fecha);
    }

    private static string FormatoFecha(string utc)
    {
        DateTimeOffset fecha;
        return LeerFecha(utc, out fecha)
            ? fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
            : "fecha desconocida";
    }

    private static string FormatoTiempo(double segundos)
    {
        int minutos = Mathf.Max(0, (int)(segundos / 60.0));
        return minutos >= 60
            ? (minutos / 60) + " h " + (minutos % 60) + " min"
            : minutos + " min";
    }

    private void MostrarMensaje(string mensaje)
    {
        if (textoMensaje != null)
            textoMensaje.text = mensaje;
    }
}
