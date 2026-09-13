using UnityEngine;

/// <summary>
/// Lanza una espada controlada por SwordPhysicsController
/// sin modificar el script original.
/// </summary>
public class LanzadorEspadaExterno : MonoBehaviour
{
    [Header("Referencia")]
    [Tooltip("El GameObject que tiene el componente SwordPhysicsController.")]
    [SerializeField] private SwordPhysicsController espada;

    [Header("Parámetros de lanzamiento")]
    [SerializeField] private Vector3 direccionLanzamiento = Vector3.forward;
    [SerializeField] private float fuerzaLanzamiento = 50f;

    [Tooltip("Si está activo, usa la posición de este transform como punto de lanzamiento. " +
             "Si no, usa la posición actual de la espada.")]
    [SerializeField] private bool usarPuntoPersonalizado = false;

    [SerializeField] private Transform puntoDeLanzamiento;

    [Tooltip("Si está activo, aplica torque para que gire al lanzar.")]
    [SerializeField] private bool aplicarTorque = true;

    [SerializeField] private float torqueMagnitud = 35f;

    private Rigidbody rbEspada;

    private void Awake()
    {
        if (espada == null)
        {
            Debug.LogError("LanzadorEspadaExterno: no hay SwordPhysicsController asignado.", this);
            return;
        }

        rbEspada = espada.GetComponent<Rigidbody>();

        if (rbEspada == null)
        {
            Debug.LogError("LanzadorEspadaExterno: la espada no tiene Rigidbody.", this);
        }
    }

    /// <summary>
    /// Lanza la espada en la dirección y desde el punto configurados en el inspector.
    /// </summary>
    public void Lanzar()
    {
        if (rbEspada == null)
        {
            Debug.LogWarning("LanzadorEspadaExterno: no hay Rigidbody en la espada.", this);
            return;
        }

        Vector3 direccion = direccionLanzamiento.normalized;

        if (direccion.sqrMagnitude < 0.001f)
        {
            Debug.LogWarning("LanzadorEspadaExterno: dirección de lanzamiento demasiado pequeña.", this);
            return;
        }

        Vector3 puntoMundo =
            usarPuntoPersonalizado && puntoDeLanzamiento != null
                ? puntoDeLanzamiento.position
                : espada.transform.position;

        // Fuerza
        rbEspada.AddForceAtPosition(
            direccion * fuerzaLanzamiento,
            puntoMundo,
            ForceMode.Impulse
        );

        // Torque opcional
        if (aplicarTorque)
        {
            // Usamos el eje Z del transform de la espada como eje de giro
            Vector3 ejeGiro = espada.transform.forward;

            rbEspada.AddTorque(
                ejeGiro * torqueMagnitud,
                ForceMode.Impulse
            );
        }
    }

    /// <summary>
    /// Versión sobrecargada para llamarla desde código con parámetros concretos.
    /// </summary>
    public void Lanzar(Vector3 direccion, Vector3 puntoMundo, float fuerza, bool conTorque = true, float torque = 35f)
    {
        if (rbEspada == null)
        {
            Debug.LogWarning("LanzadorEspadaExterno: no hay Rigidbody en la espada.", this);
            return;
        }

        Vector3 dir = direccion.normalized;

        if (dir.sqrMagnitude < 0.001f)
        {
            Debug.LogWarning("LanzadorEspadaExterno: dirección de lanzamiento demasiado pequeña.", this);
            return;
        }

        rbEspada.AddForceAtPosition(
            dir * fuerza,
            puntoMundo,
            ForceMode.Impulse
        );

        if (conTorque)
        {
            Vector3 ejeGiro = espada.transform.forward;
            rbEspada.AddTorque(ejeGiro * torque, ForceMode.Impulse);
        }
    }
}