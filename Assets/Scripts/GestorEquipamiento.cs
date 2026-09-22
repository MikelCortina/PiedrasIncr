using UnityEngine;

public class GestorEquipamiento : MonoBehaviour
{
    public enum TipoEquipamiento
    {
        ManosVacias,
        ArmaLanzadora,
        Torbellino
    }

    [Header("Desbloqueos")]
    [SerializeField] private bool armaLanzadoraDesbloqueada = false;

    [Header("Estado")]
    [SerializeField] private TipoEquipamiento equipamientoActual = TipoEquipamiento.ManosVacias;

    [Header("Referencias")]
    public AgarreLanzamiento agarreLanzamiento;
    public ArmaLanzadora armaLanzadora;
    public HerramientaTorbellino herramientaTorbellino;

    [Header("Controles")]
    public KeyCode teclaArmaLanzadora = KeyCode.Alpha3;
    public KeyCode teclaTorbellino = KeyCode.Alpha4;

    public bool ManosLibres => equipamientoActual == TipoEquipamiento.ManosVacias;

    void Start()
    {
        AplicarEstado();
    }

    void Update()
    {
        if (Input.GetKeyDown(teclaArmaLanzadora))
        {
            if (armaLanzadoraDesbloqueada)
            {
                AlternarEquipamiento(TipoEquipamiento.ArmaLanzadora);
            }
            else
            {
                Debug.Log("El Arma de la Luna todavía no está desbloqueada.");
            }
        }

        if (Input.GetKeyDown(teclaTorbellino))
        {
            AlternarEquipamiento(TipoEquipamiento.Torbellino);
        }

    }

    void AlternarEquipamiento(TipoEquipamiento nuevoEquipamiento)
    {
        // Si llevamos una piedra en las manos, no dejamos sacar herramientas.
        if (agarreLanzamiento != null &&
            agarreLanzamiento.EstaSosteniendoPiedra())
        {
            return;
        }

        // Pulsar la misma tecla guarda la herramienta.
        if (equipamientoActual == nuevoEquipamiento)
        {
            equipamientoActual = TipoEquipamiento.ManosVacias;
        }
        else
        {
            equipamientoActual = nuevoEquipamiento;
        }

        AplicarEstado();
    }

    void AplicarEstado()
    {
        if (armaLanzadora != null)
        {
            armaLanzadora.SetEquipada(
                equipamientoActual == TipoEquipamiento.ArmaLanzadora
            );
        }

        if (herramientaTorbellino != null)
        {
            herramientaTorbellino.SetEquipada(
                equipamientoActual == TipoEquipamiento.Torbellino
            );
        }
    }

    public bool PuedeUsarManos()
    {
        return ManosLibres;
    }

    public TipoEquipamiento ObtenerEquipamientoActual()
    {
        return equipamientoActual;
    }

    public void DesbloquearArmaLanzadora()
    {
        armaLanzadoraDesbloqueada = true;

        Debug.Log("¡Arma de la Luna desbloqueada!");
    }

    public bool ArmaLanzadoraDesbloqueada()
    {
        return armaLanzadoraDesbloqueada;
    }
}