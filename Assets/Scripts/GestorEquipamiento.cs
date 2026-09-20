using UnityEngine;

public class GestorEquipamiento : MonoBehaviour
{
    public enum TipoEquipamiento
    {
        ManosVacias,
        ArmaLanzadora,
        Torbellino
    }

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
            AlternarEquipamiento(TipoEquipamiento.ArmaLanzadora);
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
}