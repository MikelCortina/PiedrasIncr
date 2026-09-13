using UnityEngine;

public class SwordSpinDetector : MonoBehaviour
{
    public enum TipoGiro
    {
        Ninguno,
        SobreElFilo,
        SobreLaCara
    }

    [Header("Referencias")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform ejeGiroFilo;
    [SerializeField] private Transform ejeGiroCara;

    [Header("Detección")]
    [SerializeField] private float velocidadAngularMinima = 0.5f;
    [SerializeField] private float margenDiferencia = 0.1f;

    [Header("Depuración")]
    [SerializeField] private bool mostrarRayos = true;
    [SerializeField] private bool mostrarMensajes = true;

    public TipoGiro GiroActual { get; private set; } =
        TipoGiro.Ninguno;

    public float VelocidadAngular { get; private set; }

    private TipoGiro giroAnterior;
    private bool primerGiroDetectado;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb == null)
        {
            Debug.LogError(
                "SwordSpinDetector necesita un Rigidbody.",
                this
            );
        }

        if (ejeGiroFilo == null)
        {
            Debug.LogError(
                "No se ha asignado el eje de giro sobre el filo.",
                this
            );
        }

        if (ejeGiroCara == null)
        {
            Debug.LogError(
                "No se ha asignado el eje de giro sobre la cara.",
                this
            );
        }
    }

    private void FixedUpdate()
    {
        DetectarTipoDeGiro();
    }

    private void DetectarTipoDeGiro()
    {
        if (rb == null ||
            ejeGiroFilo == null ||
            ejeGiroCara == null)
        {
            CambiarTipoDeGiro(TipoGiro.Ninguno);
            return;
        }

        Vector3 velocidadAngular = rb.angularVelocity;

        VelocidadAngular = velocidadAngular.magnitude;

        // Si gira demasiado despacio, no consideramos que esté girando.
        if (VelocidadAngular < velocidadAngularMinima)
        {
            CambiarTipoDeGiro(TipoGiro.Ninguno);
            return;
        }

        // Dirección del eje real de rotación.
        Vector3 ejeGiroActual =
            velocidadAngular.normalized;

        // Los ejes Z de estos objetos representan las referencias.
        Vector3 ejeFilo =
            ejeGiroFilo.forward.normalized;

        Vector3 ejeCara =
            ejeGiroCara.forward.normalized;

        // El valor absoluto permite detectar ambos sentidos de giro.
        float alineacionConFilo = Mathf.Abs(
            Vector3.Dot(ejeGiroActual, ejeFilo)
        );

        float alineacionConCara = Mathf.Abs(
            Vector3.Dot(ejeGiroActual, ejeCara)
        );

        TipoGiro nuevoTipoDeGiro;

        if (alineacionConFilo >
            alineacionConCara + margenDiferencia)
        {
            nuevoTipoDeGiro = TipoGiro.SobreElFilo;
        }
        else if (alineacionConCara >
                 alineacionConFilo + margenDiferencia)
        {
            nuevoTipoDeGiro = TipoGiro.SobreLaCara;
        }
        else
        {
            nuevoTipoDeGiro = TipoGiro.Ninguno;
        }

        CambiarTipoDeGiro(nuevoTipoDeGiro);

        MostrarRayosDeDepuracion(
            ejeGiroActual,
            ejeFilo,
            ejeCara
        );
    }

    private void CambiarTipoDeGiro(TipoGiro nuevoTipoDeGiro)
    {
        GiroActual = nuevoTipoDeGiro;

        // Evita mostrar el mismo mensaje continuamente.
        if (primerGiroDetectado &&
            GiroActual == giroAnterior)
        {
            return;
        }

        primerGiroDetectado = true;
        giroAnterior = GiroActual;

        if (!mostrarMensajes)
        {
            return;
        }

        string mensaje;

        switch (GiroActual)
        {
            case TipoGiro.SobreElFilo:
                mensaje =
                    "La espada está girando sobre el FILO.";
                break;

            case TipoGiro.SobreLaCara:
                mensaje =
                    "La espada está girando sobre la CARA.";
                break;

            default:
                mensaje =
                    "La espada no está girando claramente sobre el filo ni sobre la cara.";
                break;
        }

        Debug.Log(
            $"[SwordSpinDetector] {mensaje} " +
            $"Velocidad angular: {VelocidadAngular:F2}",
            this
        );
    }

    private void MostrarRayosDeDepuracion(
        Vector3 ejeGiroActual,
        Vector3 ejeFilo,
        Vector3 ejeCara)
    {
        if (!mostrarRayos)
        {
            return;
        }

        Vector3 origen = transform.position;

        // Blanco: eje de giro real de la espada.
        Debug.DrawRay(
            origen,
            ejeGiroActual * 2f,
            Color.white
        );

        // Rojo: eje definido como giro sobre el filo.
        Debug.DrawRay(
            origen,
            ejeFilo * 2f,
            Color.red
        );

        // Azul: eje definido como giro sobre la cara.
        Debug.DrawRay(
            origen,
            ejeCara * 2f,
            Color.blue
        );
    }

    public bool EstaGirandoSobreElFilo()
    {
        return GiroActual == TipoGiro.SobreElFilo;
    }

    public bool EstaGirandoSobreLaCara()
    {
        return GiroActual == TipoGiro.SobreLaCara;
    }

    public bool EstaGirando()
    {
        return GiroActual != TipoGiro.Ninguno;
    }
}