using UnityEngine;

public class GestorEquipamiento : MonoBehaviour
{
    public enum TipoEquipamiento
    {
        ManosVacias,
        ArmaLanzadora,
        Torbellino,
        Construccion
    }

    [Header("Desbloqueos")]
    [SerializeField] private bool armaLanzadoraDesbloqueada = false;
    [SerializeField] private bool torbellinoDesbloqueado = false;
    [SerializeField] private bool martilloDesbloqueado = false;

    [Header("Estado")]
    [SerializeField]
    private TipoEquipamiento equipamientoActual =
        TipoEquipamiento.ManosVacias;

    [Header("Referencias")]
    public AgarreLanzamiento agarreLanzamiento;
    public ArmaLanzadora armaLanzadora;
    public HerramientaTorbellino herramientaTorbellino;
    public SistemaConstruccion sistemaConstruccion;

    [Header("Controles")]
    public KeyCode teclaArmaLanzadora = KeyCode.Alpha3;
    public KeyCode teclaTorbellino = KeyCode.Alpha4;
    public KeyCode teclaConstruccion = KeyCode.B;

    public bool ManosLibres =>
        equipamientoActual == TipoEquipamiento.ManosVacias;


    private void Start()
    {
        AplicarEstado();
    }


    private void Update()
    {
        // =========================
        // ARMA DE LA LUNA
        // =========================

        if (Input.GetKeyDown(teclaArmaLanzadora))
        {
            if (armaLanzadoraDesbloqueada)
            {
                AlternarEquipamiento(
                    TipoEquipamiento.ArmaLanzadora
                );
            }
            else
            {
                Debug.Log(
                    "El Arma de la Luna todavía no está desbloqueada."
                );
            }
        }


        // =========================
        // TORBELLINO
        // =========================

        if (Input.GetKeyDown(teclaTorbellino))
        {
            if (torbellinoDesbloqueado)
            {
                AlternarEquipamiento(
                    TipoEquipamiento.Torbellino
                );
            }
            else
            {
                Debug.Log(
                    "El Torbellino todavía no está desbloqueado."
                );
            }
        }


        // =========================
        // MARTILLO / CONSTRUCCIÓN
        // =========================

        if (Input.GetKeyDown(teclaConstruccion))
        {
            if (martilloDesbloqueado)
            {
                AlternarEquipamiento(
                    TipoEquipamiento.Construccion
                );
            }
            else
            {
                Debug.Log(
                    "El Martillo todavía no está desbloqueado."
                );
            }
        }
    }


    private void AlternarEquipamiento(
        TipoEquipamiento nuevoEquipamiento
    )
    {
        // Si tenemos una piedra agarrada,
        // no dejamos sacar ninguna herramienta.
        if (agarreLanzamiento != null &&
            agarreLanzamiento.EstaSosteniendoPiedra())
        {
            return;
        }


        // Si pulsamos la tecla de la herramienta
        // que ya tenemos equipada, volvemos a manos vacías.
        if (equipamientoActual == nuevoEquipamiento)
        {
            equipamientoActual =
                TipoEquipamiento.ManosVacias;
        }
        else
        {
            equipamientoActual =
                nuevoEquipamiento;
        }


        AplicarEstado();
    }


    private void AplicarEstado()
    {
        // =========================
        // ARMA
        // =========================

        if (armaLanzadora != null)
        {
            armaLanzadora.SetEquipada(
                equipamientoActual ==
                TipoEquipamiento.ArmaLanzadora
            );
        }


        // =========================
        // TORBELLINO
        // =========================

        if (herramientaTorbellino != null)
        {
            herramientaTorbellino.SetEquipada(
                equipamientoActual ==
                TipoEquipamiento.Torbellino
            );
        }


        // =========================
        // CONSTRUCCIÓN
        // =========================

        if (sistemaConstruccion != null)
        {
            sistemaConstruccion.SetModoConstruccion(
                equipamientoActual ==
                TipoEquipamiento.Construccion
            );
        }
    }


    // =====================================================
    // MANOS
    // =====================================================

    public bool PuedeUsarManos()
    {
        return ManosLibres;
    }


    public TipoEquipamiento ObtenerEquipamientoActual()
    {
        return equipamientoActual;
    }


    // =====================================================
    // ARMA
    // =====================================================

    public void DesbloquearArmaLanzadora()
    {
        armaLanzadoraDesbloqueada = true;

        Debug.Log(
            "¡Arma de la Luna desbloqueada!"
        );
    }


    public bool ArmaLanzadoraDesbloqueada()
    {
        return armaLanzadoraDesbloqueada;
    }


    // =====================================================
    // TORBELLINO
    // =====================================================

    public void DesbloquearTorbellino()
    {
        torbellinoDesbloqueado = true;

        Debug.Log(
            "¡Torbellino desbloqueado!"
        );
    }


    public bool TorbellinoDesbloqueado()
    {
        return torbellinoDesbloqueado;
    }


    // =====================================================
    // MARTILLO
    // =====================================================

    public void DesbloquearMartillo()
    {
        martilloDesbloqueado = true;

        Debug.Log(
            "¡Martillo y modo construcción desbloqueados!"
        );
    }


    public bool MartilloDesbloqueado()
    {
        return martilloDesbloqueado;
    }
}