using System.Collections.Generic;
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

    [Header("Arrastre")]
    [Tooltip("Rapidez con la que los objetos alcanzan la velocidad de la cinta (más alto = más inmediato).")]
    public float aceleracionArrastre = 20f;
    [Tooltip("La cinta empuja a lo que toque cualquier collider de la estructura (no solo el suyo).")]
    public bool arrastrarEnToda = true;

    [Header("Desatasco")]
    [Tooltip("Si un objeto sobre la cinta no avanza (escalón en una junta o en el final de la rampa), se le da un pequeño salto para superarlo.")]
    public bool desatascar = true;
    [Tooltip("Segundos sin avanzar antes de darle el salto.")]
    [Min(0.02f)] public float tiempoAtasco = 0.25f;
    [Tooltip("Por debajo de esta fracción de la velocidad de la cinta se considera que no avanza.")]
    [Range(0f, 1f)] public float umbralAtasco = 0.15f;
    [Tooltip("Velocidad hacia arriba (perpendicular a la cinta) del salto de desatasco.")]
    public float impulsoDesatasco = 3f;

    private struct EstadoAtasco { public float tiempo; public float ultimoPaso; }
    private readonly Dictionary<Rigidbody, EstadoAtasco> atascos = new Dictionary<Rigidbody, EstadoAtasco>();

    private Material materialCinta;
    private Vector2 offsetTextura;
    private CurvaturaRampa curva;

    // Dirección de la cinta en un punto: sigue la curva si la rampa está curvada.
    public Vector3 DireccionEn(Vector3 puntoMundo)
    {
        Vector3 direccion = transform.TransformDirection(direccionLocal).normalized;
        return curva != null ? curva.CurvarDireccion(direccion, puntoMundo) : direccion;
    }

    void Start()
    {
        curva = GetComponentInParent<CurvaturaRampa>();

        // Una piedra apoyada en otra malla de la rampa también debe avanzar.
        // Solo dentro de la propia estructura construida (nunca toda la jerarquía de la escena).
        Component estructura = GetComponentInParent<EdificioConstruido>();
        if (estructura == null) estructura = curva;
        if (arrastrarEnToda && estructura != null)
        {
            Transform raiz = estructura.transform;
            foreach (Collider col in raiz.GetComponentsInChildren<Collider>(true))
            {
                if (col.isTrigger || col.gameObject == gameObject) continue;
                if (col.GetComponent<CintaTransportadora>() != null || col.GetComponent<ReenvioCinta>() != null) continue;
                col.gameObject.AddComponent<ReenvioCinta>().cinta = this;
            }
        }

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
        Arrastrar(colision);
    }

    public void Arrastrar(Collision colision)
    {
        DeformacionPiedra scriptPiedra = colision.gameObject.GetComponent<DeformacionPiedra>();
        if (scriptPiedra != null)
        {
            scriptPiedra.Despertar();
        }

        Rigidbody rb = colision.rigidbody;
        if (rb != null && !rb.isKinematic)
        {
            Vector3 punto = colision.contactCount > 0 ? colision.GetContact(0).point : rb.position;
            Vector3 direccionMundo = DireccionEn(punto);

            // Se ajusta la velocidad en vez de mover la posición (MovePosition la "teletransportaba" sin respetar
            // colisiones y podía meterla en los raíles o en las juntas). Solo se toca la componente a lo largo de la cinta.
            Vector3 v = rb.linearVelocity;
            float aLoLargo = Vector3.Dot(v, direccionMundo);
            float factor = 1f - Mathf.Exp(-aceleracionArrastre * Time.fixedDeltaTime);
            rb.linearVelocity = v + direccionMundo * ((velocidad - aLoLargo) * factor);

            if (desatascar) ComprobarAtasco(rb, aLoLargo, direccionMundo);
        }
    }

    // Empujar por velocidad respeta las colisiones, así que un escalón entre colliders (la caja de la cinta y el cuerpo
    // de la rampa) puede frenar la piedra para siempre. Si no avanza durante un rato, salta por encima.
    void ComprobarAtasco(Rigidbody rb, float aLoLargo, Vector3 direccionMundo)
    {
        if (aLoLargo >= velocidad * umbralAtasco)
        {
            atascos.Remove(rb);
            return;
        }

        atascos.TryGetValue(rb, out EstadoAtasco estado);
        float paso = Time.fixedTime;
        if (estado.ultimoPaso == paso) return; // Ya contado en este paso (la tocan varios colliders de la estructura).
        if (paso - estado.ultimoPaso > Time.fixedDeltaTime * 3f) estado.tiempo = 0f; // Dejó de tocar la cinta un tiempo.

        estado.tiempo += Time.fixedDeltaTime;
        estado.ultimoPaso = paso;

        if (estado.tiempo >= tiempoAtasco)
        {
            Vector3 arriba = Vector3.ProjectOnPlane(Vector3.up, direccionMundo);
            if (arriba.sqrMagnitude < 0.0001f) arriba = Vector3.up;
            rb.linearVelocity = direccionMundo * velocidad + arriba.normalized * impulsoDesatasco;
            estado.tiempo = 0f;
        }

        atascos[rb] = estado;
    }

    void OnDrawGizmos()
    {
        Vector3 direccionMundo = DireccionEn(transform.position);
        Vector3 centro = transform.position + Vector3.up * 0.5f;

        Gizmos.color = Color.green;
        Gizmos.DrawRay(centro, direccionMundo * 2f);
        Gizmos.DrawWireSphere(centro + direccionMundo * 2f, 0.2f);
    }
}

// Pasa a la cinta los contactos de otro collider de la misma estructura.
public class ReenvioCinta : MonoBehaviour
{
    public CintaTransportadora cinta;

    void OnCollisionStay(Collision colision)
    {
        if (cinta != null && cinta.isActiveAndEnabled) cinta.Arrastrar(colision);
    }
}