using UnityEngine;

public class RecogerMonedaClick : MonoBehaviour
{
    // =====================================================
    // REFERENCIAS
    // =====================================================

    [Header("Referencias")]

    [Tooltip("Cámara desde la que se apunta.")]
    public Camera camaraJugador;

    [Tooltip("Cartera donde se añaden las monedas.")]
    public Cartera cartera;


    // =====================================================
    // CONFIGURACIÓN
    // =====================================================

    [Header("Configuración")]

    [Tooltip("Distancia máxima desde la que se puede recoger.")]
    public float distanciaRecogida = 3f;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        if (camaraJugador == null)
        {
            camaraJugador =
                Camera.main;
        }


        if (cartera == null)
        {
            cartera =
                FindFirstObjectByType<Cartera>();
        }
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            IntentarRecogerMoneda();
        }
    }


    // =====================================================
    // RECOGER
    // =====================================================

    private void IntentarRecogerMoneda()
    {
        if (camaraJugador == null ||
            cartera == null)
        {
            return;
        }


        Ray rayo =
            new Ray(
                camaraJugador.transform.position,
                camaraJugador.transform.forward
            );


        if (!Physics.Raycast(
                rayo,
                out RaycastHit hit,
                distanciaRecogida))
        {
            return;
        }


        // Intentamos encontrar Moneda
        // en el objeto golpeado.
        Moneda moneda =
            hit.collider.GetComponent<Moneda>();


        // Por si el collider está en un hijo.
        if (moneda == null)
        {
            moneda =
                hit.collider
                    .GetComponentInParent<Moneda>();
        }


        if (moneda == null)
            return;


        RecogerMoneda(
            moneda
        );
    }


    // =====================================================
    // SUMAR MONEDA
    // =====================================================

    private void RecogerMoneda(
        Moneda moneda)
    {
        if (moneda == null)
            return;


        int valor =
            moneda.valor;


        cartera.AnadirMonedas(
            valor
        );


        Debug.Log(
            "Moneda recogida: +" +
            valor
        );


        Destroy(
            moneda.gameObject
        );
    }
}