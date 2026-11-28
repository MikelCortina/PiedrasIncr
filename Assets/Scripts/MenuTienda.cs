using TMPro;
using UnityEngine;

/// <summary>
/// Lo que comparten todas las tiendas (MesaTrabajo, GeneradorAranas, TiendaEdificiosTrigger):
///  - Abrir/Cerrar: pausa el juego, suelta el ratón, bloquea la cámara y guarda el arma. Al cerrar lo deja todo como estaba.
///  - El aviso "Pulsa E para..." (un único texto en pantalla, el de la MesaTrabajo).
/// Si se abren dos menús a la vez, el juego no se reanuda hasta cerrar el último.
/// </summary>
public static class MenuTienda
{
    private static int menusAbiertos;
    private static CamaraPrimeraPersona camara;
    private static ArmaLanzadora arma;
    private static bool armaEstabaActiva;

    private static TMP_Text textoAviso;
    private static object duenoAviso;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        menusAbiertos = 0;
        camara = null;
        arma = null;
        armaEstabaActiva = false;
        textoAviso = null;
        duenoAviso = null;
    }

    public static bool HayMenuAbierto => menusAbiertos > 0;

    // =====================================================
    // ABRIR / CERRAR
    // =====================================================

    public static void Abrir()
    {
        menusAbiertos++;
        if (menusAbiertos > 1) return;

        camara = Object.FindFirstObjectByType<CamaraPrimeraPersona>();
        if (camara != null) camara.SetBloqueadaPorUI(true);

        arma = Object.FindFirstObjectByType<ArmaLanzadora>();
        if (arma != null)
        {
            armaEstabaActiva = arma.enabled;
            arma.enabled = false;
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void Cerrar()
    {
        if (menusAbiertos == 0) return;
        menusAbiertos--;
        if (menusAbiertos > 0) return;

        Time.timeScale = 1f;
        if (camara != null) camara.SetBloqueadaPorUI(false);
        if (arma != null) arma.enabled = armaEstabaActiva;
        camara = null;
        arma = null;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // =====================================================
    // AVISO "PULSA E"
    // =====================================================

    /// <summary>El texto en pantalla que usan todos los avisos (lo registra la MesaTrabajo).</summary>
    public static void RegistrarTextoAviso(TMP_Text texto)
    {
        if (texto == null || textoAviso == texto) return;
        textoAviso = texto;
        duenoAviso = null;
        textoAviso.gameObject.SetActive(false);
    }

    /// <summary>Muestra el aviso. 'quien' es la tienda que lo pide: solo ella puede ocultarlo.</summary>
    public static void MostrarAviso(object quien, string mensaje)
    {
        if (textoAviso == null) return;
        duenoAviso = quien;
        textoAviso.text = mensaje;
        textoAviso.gameObject.SetActive(true);
    }

    public static void OcultarAviso(object quien)
    {
        if (textoAviso == null || duenoAviso != quien) return;
        duenoAviso = null;
        textoAviso.gameObject.SetActive(false);
    }
}
