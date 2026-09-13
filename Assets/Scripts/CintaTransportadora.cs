using UnityEngine;

public class CintaTransportadora : MonoBehaviour
{
    [Header("Ajustes de la Cinta")]
    public float velocidad = 5f;
    [Tooltip("Cambia esto (ej: X=1, o Y=1) si la flecha verde en la escena no apunta hacia adelante")]
    public Vector3 direccionLocal = Vector3.forward;

    [Header("Efecto Visual (Game Feel)")]
    public bool animarTextura = true;
    public Vector2 direccionTextura = new Vector2(0f, -1f);

    [Tooltip("Ajusta este multiplicador hasta que la textura se mueva a la misma velocidad que los objetos sobre ella.")]
    public float multiplicadorSincronizacion = 0.2f; // ¡NUEVO! El sincronizador mágico

    private Material materialCinta;
    private Vector2 offsetTextura;

    void Start()
    {
        if (animarTextura)
        {
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                materialCinta = rend.material;
            }
        }
    }

    void Update()
    {
        if (animarTextura && materialCinta != null)
        {
            // Usamos tu multiplicador en lugar de un número fijo
            offsetTextura += direccionTextura * (velocidad * multiplicadorSincronizacion * Time.deltaTime);
            materialCinta.mainTextureOffset = offsetTextura;
        }
    }

    void OnCollisionStay(Collision colision)
    {
        DeformacionPiedra scriptPiedra = colision.gameObject.GetComponent<DeformacionPiedra>();
        if (scriptPiedra != null)
        {
            scriptPiedra.Despertar();
        }

        Rigidbody rb = colision.gameObject.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            Vector3 direccionMundo = transform.TransformDirection(direccionLocal).normalized;
            Vector3 nuevaPosicion = rb.position + (direccionMundo * velocidad * Time.fixedDeltaTime);
            rb.MovePosition(nuevaPosicion);
        }
    }

    void OnDrawGizmos()
    {
        Vector3 direccionMundo = transform.TransformDirection(direccionLocal).normalized;
        Vector3 centro = transform.position + Vector3.up * 0.5f;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(centro, direccionMundo * 2f);
        Gizmos.DrawWireSphere(centro + direccionMundo * 2f, 0.2f);
    }
}