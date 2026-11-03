
using System;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Paso 93:
/// Menu principal conectado al sistema de guardado.
/// No modifica los archivos directamente.
/// </summary>
public class MenuPrincipalGuardado : MonoBehaviour
{
    [Header("Sistema de guardado")]
    [SerializeField]
    private GuardadoPartidaJuego sistemaGuardado;

    [Header("Menu principal")]
    [SerializeField]
    private Button botonContinuar;

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

    private SaveStorage almacenamiento;

    private enum ModoSeleccion
    {
        Ninguno,
        NuevaPartida,
        CargarPartida
    }

    private ModoSeleccion modoActual;

    private void Awake()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        almacenamiento = new SaveStorage();

        modoActual = ModoSeleccion.Ninguno;

        if (panelSeleccionRanura != null)
            panelSeleccionRanura.SetActive(false);
    }

    private void Start()
    {
        if (sistemaGuardado == null)
        {
            Debug.LogError(
                "[MENU] PASO 93: falta SistemaGuardadoMenu."
            );

            enabled = false;
            return;
        }

        ActualizarContinuar();

        Debug.Log(
            "[MENU] PASO 93 MENU PRINCIPAL PREPARADO"
        );
    }

    // =====================================
    // NUEVA PARTIDA
    // =====================================

    public void NuevaPartida()
    {
        modoActual = ModoSeleccion.NuevaPartida;

        if (tituloSeleccion != null)
            tituloSeleccion.text = "NUEVA PARTIDA";

        MostrarSeleccion();

        Debug.Log(
            "[MENU] PASO 93 SELECCION NUEVA PARTIDA"
        );
    }

    // =====================================
    // CARGAR PARTIDA
    // =====================================

    public void CargarPartida()
    {
        modoActual = ModoSeleccion.CargarPartida;

        if (tituloSeleccion != null)
            tituloSeleccion.text = "CARGAR PARTIDA";

        MostrarSeleccion();

        Debug.Log(
            "[MENU] PASO 93 SELECCION CARGAR PARTIDA"
        );
    }

    // =====================================
    // CONTINUAR PARTIDA
    // =====================================

    public void ContinuarPartida()
    {
        int ultimaRanura = ObtenerUltimaRanura();

        if (ultimaRanura == 0)
        {
            MostrarMensaje(
                "No hay partidas guardadas."
            );

            ActualizarContinuar();
            return;
        }

        Debug.Log(
            "[MENU] PASO 93 CONTINUAR: ranura=" +
            ultimaRanura
        );

        sistemaGuardado.CargarDesdeMenu(ultimaRanura);
    }

    // =====================================
    // BOTONES DE RANURA
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
        if (modoActual == ModoSeleccion.Ninguno)
            return;

        GameSaveData partida;
        bool desdeRespaldo;

        bool existe = almacenamiento.TryLoad(
            numero,
            out partida,
            out desdeRespaldo
        );

        if (modoActual == ModoSeleccion.CargarPartida)
        {
            if (!existe)
            {
                MostrarMensaje(
                    "La ranura no contiene una partida valida."
                );
                return;
            }

            Debug.Log(
                "[MENU] PASO 93 CARGAR: ranura=" +
                numero
            );

            sistemaGuardado.CargarDesdeMenu(numero);
            return;
        }

        if (modoActual == ModoSeleccion.NuevaPartida)
        {
            if (existe || TieneArchivos(numero))
            {
                MostrarMensaje(
                    "La ranura no esta vacia."
                );
                ActualizarRanuras();
                return;
            }

            Debug.Log(
                "[MENU] PASO 93 NUEVA: ranura=" +
                numero
            );

            sistemaGuardado.IniciarNuevaPartidaDesdeMenu(
                numero
            );
        }
    }

    // =====================================
    // PANEL
    // =====================================

    private void MostrarSeleccion()
    {
        if (panelSeleccionRanura == null)
        {
            Debug.LogError(
                "[MENU] Falta PanelSeleccionRanura."
            );
            return;
        }

        panelSeleccionRanura.SetActive(true);

        ActualizarRanuras();
    }

    public void Volver()
    {
        modoActual = ModoSeleccion.Ninguno;

        if (panelSeleccionRanura != null)
            panelSeleccionRanura.SetActive(false);

        ActualizarContinuar();
    }

    // =====================================
    // ACTUALIZAR LAS RANURAS
    // =====================================

    private void ActualizarRanuras()
    {
        ActualizarRanura(
            1, botonRanura1, textoRanura1
        );

        ActualizarRanura(
            2, botonRanura2, textoRanura2
        );

        ActualizarRanura(
            3, botonRanura3, textoRanura3
        );
    }

    private void ActualizarRanura(
        int numero,
        Button boton,
        TMP_Text texto)
    {
        GameSaveData partida;
        bool desdeRespaldo;

        bool existe = almacenamiento.TryLoad(
            numero,
            out partida,
            out desdeRespaldo
        );

        bool archivosPresentes = TieneArchivos(numero);

        bool disponible;

        if (modoActual == ModoSeleccion.NuevaPartida)
            disponible = !archivosPresentes;
        else
            disponible = existe;

        if (boton != null)
            boton.interactable = disponible;

        if (texto == null)
            return;

        string estado;

        if (existe)
        {
            int edificios = 0;

            if (partida.placedBuildings != null &&
                partida.placedBuildings.edificios != null)
            {
                edificios =
                    partida.placedBuildings.edificios.Count;
            }

            estado = desdeRespaldo
                ? "RESPALDO DISPONIBLE"
                : "OCUPADA";

            estado +=
                " | " + partida.economy.coins +
                " monedas | " + edificios + " edificios";
        }
        else if (archivosPresentes)
        {
            estado = "ARCHIVOS NO VALIDOS";
        }
        else
        {
            estado = "VACIA";
        }

        texto.text =
            "Ranura " + numero + " - " + estado;
    }

    private bool TieneArchivos(int numero)
    {
        string principal =
            almacenamiento.GetPrimaryPathForDiagnostics(
                numero
            );

        return File.Exists(principal) ||
               File.Exists(principal + ".bak") ||
               File.Exists(principal + ".tmp");
    }

    // =====================================
    // BUSCAR PARTIDA MAS RECIENTE
    // =====================================

    private int ObtenerUltimaRanura()
    {
        int mejorRanura = 0;

        DateTimeOffset mejorFecha =
            DateTimeOffset.MinValue;

        for (int numero = 1; numero <= 3; numero++)
        {
            GameSaveData partida;
            bool desdeRespaldo;

            if (!almacenamiento.TryLoad(
                numero,
                out partida,
                out desdeRespaldo))
            {
                continue;
            }

            DateTimeOffset fecha;

            if (!DateTimeOffset.TryParse(
                partida.savedUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out fecha))
            {
                continue;
            }

            if (mejorRanura == 0 ||
                fecha > mejorFecha)
            {
                mejorRanura = numero;
                mejorFecha = fecha;
            }
        }

        return mejorRanura;
    }

    private void ActualizarContinuar()
    {
        if (botonContinuar != null)
        {
            botonContinuar.interactable =
                ObtenerUltimaRanura() != 0;
        }
    }

    private void MostrarMensaje(string mensaje)
    {
        if (textoMensaje != null)
            textoMensaje.text = mensaje;

        Debug.LogWarning("[MENU] " + mensaje);
    }
}
