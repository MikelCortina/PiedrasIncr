
using System;
using System.IO;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;

/// <summary>
/// Paso 90:
///
/// Permite eliminar una ranura mediante
/// una ventana de confirmacion.
///
/// Borra el archivo principal, respaldo
/// y temporal de la ranura indicada.
///
/// Comprueba que los archivos no hayan
/// cambiado mientras se confirma.
/// </summary>
public class BorradoRanurasGuardado : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField]
    private GuardadoPartidaJuego sistemaGuardado;

    [SerializeField]
    private MenuRanurasGuardado menuRanuras;

    [Header("Interfaz")]
    [SerializeField]
    private GameObject panelMenu;

    [SerializeField]
    private GameObject panelConfirmacion;

    [SerializeField]
    private TMP_Text textoConfirmacion;

    private SaveStorage almacenamiento;

    private int ranuraPendiente;
    private string huellaPendiente;

    private void Awake()
    {
        almacenamiento = new SaveStorage();

        if (sistemaGuardado == null)
        {
            sistemaGuardado =
                FindFirstObjectByType<GuardadoPartidaJuego>();
        }

        if (menuRanuras == null)
        {
            menuRanuras =
                FindFirstObjectByType<MenuRanurasGuardado>();
        }

        ranuraPendiente = 0;
        huellaPendiente = null;

        if (panelConfirmacion != null)
            panelConfirmacion.SetActive(false);
    }

    private void Update()
    {
        // Si F6 cierra el menu, cancelar
        // cualquier borrado pendiente.
        if (ranuraPendiente != 0 &&
            panelMenu != null &&
            !panelMenu.activeInHierarchy)
        {
            CancelarBorrado();
        }
    }

    // =====================================
    // BOTONES DE ELIMINACION
    // =====================================

    public void EliminarRanura1()
    {
        SolicitarBorrado(1);
    }

    public void EliminarRanura2()
    {
        SolicitarBorrado(2);
    }

    public void EliminarRanura3()
    {
        SolicitarBorrado(3);
    }

    // =====================================
    // SOLICITAR BORRADO
    // =====================================

    private void SolicitarBorrado(int numeroRanura)
    {
        if (!Application.isPlaying)
            return;

        if (numeroRanura < 1 || numeroRanura > 3)
            return;

        if (panelConfirmacion == null ||
            textoConfirmacion == null)
        {
            Debug.LogError(
                "[BORRADO] PASO 90: " +
                "Faltan referencias del panel."
            );
            return;
        }

        if (!TieneArchivos(numeroRanura))
        {
            Debug.Log(
                "[BORRADO] PASO 90: " +
                "ranura " + numeroRanura +
                " ya esta vacia."
            );
            return;
        }

        try
        {
            // Guardamos una huella de los tres
            // archivos antes de pedir confirmacion.
            huellaPendiente =
                CalcularHuella(numeroRanura);

            ranuraPendiente = numeroRanura;

            textoConfirmacion.text =
                "¿Eliminar la partida de la ranura " +
                numeroRanura + "?\n\n" +
                "Se eliminaran el guardado, " +
                "su respaldo y el archivo temporal.\n\n" +
                "Esta operacion no se puede deshacer.";

            panelConfirmacion.SetActive(true);

            Debug.Log(
                "[BORRADO] PASO 90 CONFIRMACION SOLICITADA: " +
                "ranura=" + numeroRanura
            );
        }
        catch (Exception ex)
        {
            CancelarBorrado();

            Debug.LogError(
                "[BORRADO] PASO 90 ERROR: " + ex
            );
        }
    }

    // =====================================
    // CONFIRMAR BORRADO
    // =====================================

    public void ConfirmarBorrado()
    {
        if (!Application.isPlaying ||
            ranuraPendiente == 0)
        {
            return;
        }

        int numeroRanura = ranuraPendiente;

        try
        {
            // Verificar que el contenido no
            // cambio mientras se mostraba
            // la ventana de confirmacion.
            string huellaActual =
                CalcularHuella(numeroRanura);

            if (!string.Equals(
                huellaActual,
                huellaPendiente,
                StringComparison.Ordinal))
            {
                Debug.LogWarning(
                    "[BORRADO] PASO 90 CANCELADO: " +
                    "los archivos de la ranura " +
                    numeroRanura +
                    " cambiaron durante la confirmacion."
                );

                return;
            }

            // Utilizar el borrado ya
            // implementado en SaveStorage.
            almacenamiento.DeleteSlot(numeroRanura);

            // Verificar que desaparecieron
            // todos los archivos de la ranura.
            if (TieneArchivos(numeroRanura))
            {
                throw new IOException(
                    "La ranura conserva archivos " +
                    "despues del borrado."
                );
            }

            // Invalidar la partida activa
            // si se acaba de eliminar.
            if (sistemaGuardado != null)
            {
                sistemaGuardado.NotificarRanuraEliminada(
                    numeroRanura
                );
            }

            Debug.Log(
                "[BORRADO] PASO 90 SUPERADO: " +
                "ranura=" + numeroRanura +
                ", principal=eliminado" +
                ", respaldo=eliminado" +
                ", temporal=eliminado"
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[BORRADO] PASO 90 FALLIDO: " + ex
            );
        }
        finally
        {
            CancelarBorrado();

            if (menuRanuras != null)
                menuRanuras.ActualizarRanuras();
        }
    }

    // =====================================
    // CANCELAR
    // =====================================

    public void CancelarBorrado()
    {
        int anterior = ranuraPendiente;

        ranuraPendiente = 0;
        huellaPendiente = null;

        if (panelConfirmacion != null)
            panelConfirmacion.SetActive(false);

        if (anterior != 0)
        {
            Debug.Log(
                "[BORRADO] PASO 90 CONFIRMACION CERRADA: " +
                "ranura=" + anterior
            );
        }
    }

    // =====================================
    // CONSULTA DE ARCHIVOS
    // =====================================

    private bool TieneArchivos(int numeroRanura)
    {
        string principal =
            almacenamiento.GetPrimaryPathForDiagnostics(
                numeroRanura
            );

        return File.Exists(principal) ||
               File.Exists(principal + ".bak") ||
               File.Exists(principal + ".tmp");
    }

    // =====================================
    // HUELLA DE LOS ARCHIVOS
    // =====================================

    private string CalcularHuella(int numeroRanura)
    {
        string principal =
            almacenamiento.GetPrimaryPathForDiagnostics(
                numeroRanura
            );

        return HuellaArchivo(principal) + "|" +
               HuellaArchivo(principal + ".bak") + "|" +
               HuellaArchivo(principal + ".tmp");
    }

    private static string HuellaArchivo(string ruta)
    {
        if (!File.Exists(ruta))
            return "AUSENTE";

        using (SHA256 sha = SHA256.Create())
        {
            byte[] contenido = File.ReadAllBytes(ruta);

            byte[] hash = sha.ComputeHash(contenido);

            return Convert.ToBase64String(hash);
        }
    }

    private void OnDisable()
    {
        CancelarBorrado();
    }
}
