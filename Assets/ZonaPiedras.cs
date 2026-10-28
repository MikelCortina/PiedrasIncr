using UnityEngine;

/// <summary>
/// Área física donde las arañas almacenan piedras sin consumirlas.
/// Coloca el objeto en el suelo; el BoxCollider debe ser Is Trigger.
/// PuntoLlegadaBot debe estar situado sobre NavMesh, fuera del montón.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class ZonaPiedras : MonoBehaviour
{
    [Header("Llegada de las arañas")]
    [Tooltip("Punto accesible sobre NavMesh. Si se deja vacío, se usa el centro de esta zona.")]
    public Transform puntoLlegadaBot;

    [Header("Depósito de piedras")]
    [Tooltip("BoxCollider que define el espacio horizontal de almacenamiento; marcar Is Trigger.")]
    public BoxCollider volumen;

    [Tooltip("Margen desde los bordes del BoxCollider para que las piedras no caigan fuera.")]
    [Min(0f)] public float margenBordes = 0.25f;

    [Tooltip("Altura sobre la cara superior del volumen a la que termina el lanzamiento, antes de caer con física.")]
    [Min(0f)] public float alturaSuelta = 0.35f;

    public Transform PuntoLlegada => puntoLlegadaBot != null ? puntoLlegadaBot : transform;

    private void Awake()
    {
        if (volumen == null) volumen = GetComponent<BoxCollider>();
        if (volumen != null && !volumen.isTrigger)
            Debug.LogWarning(name + ": marca Is Trigger en el BoxCollider de Zona Piedras.", this);
    }

    public Vector3 ElegirPuntoDeposito()
    {
        if (volumen == null) volumen = GetComponent<BoxCollider>();
        if (volumen == null) return transform.position + Vector3.up * alturaSuelta;

        // Punto aleatorio dentro de la caja, respetando su rotación/escala.
        float halfX = Mathf.Max(0f, volumen.size.x * 0.5f - margenBordes);
        float halfZ = Mathf.Max(0f, volumen.size.z * 0.5f - margenBordes);
        Vector3 local = volumen.center + new Vector3(
            Random.Range(-halfX, halfX),
            volumen.size.y * 0.5f,
            Random.Range(-halfZ, halfZ)
        );

        Vector3 mundo = volumen.transform.TransformPoint(local);
        mundo.y = Mathf.Max(mundo.y, volumen.bounds.max.y) + alturaSuelta;
        return mundo;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(PuntoLlegada.position, 0.3f);
        Gizmos.DrawLine(PuntoLlegada.position, transform.position);
    }
}
