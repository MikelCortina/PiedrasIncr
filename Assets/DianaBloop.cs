using System.Collections;
using UnityEngine;

public class DianaBloop : MonoBehaviour
{
    [Header("Referencias Visuales")]
    [Tooltip("El modelo 3D que hará la animación de bloop")]
    public Transform modeloVisual;

    [Header("Efectos de Impacto (VFX)")]
    public GameObject prefabVFXImpacto;
    [Tooltip("Ligero desplazamiento hacia afuera para evitar que el VFX nazca dentro de la malla")]
    public float offsetSuperficieVFX = 0.08f;

    [Header("Lanzamiento en Cono")]
    public GameObject prefabPiedra;
    [Tooltip("Punto desde el que nace la piedra (si es null, usa la parte superior)")]
    public Transform puntoSalidaPiedra;

    [Tooltip("Impulso hacia arriba")]
    public float fuerzaVertical = 16f;

    [Tooltip("Fuerza mínima de dispersión hacia los lados")]
    public float fuerzaHorizontalMin = 1.5f;

    [Tooltip("Fuerza máxima de dispersión hacia los lados (determina el ancho del cono)")]
    public float fuerzaHorizontalMax = 5.5f;

    public float fuerzaRotacionRandom = 15f;

    [Header("Inmunidad Inicial (0.5s)")]
    [Tooltip("Evita que el jugador o la patada interactúen con la piedra recién nacida")]
    public float tiempoInmunidadPiedra = 0.5f;

    [Header("Animación Bloop (Squash & Stretch)")]
    public float duracionBloop = 0.4f;
    [Range(0.2f, 0.9f)] public float aplastamientoY = 0.55f;
    [Range(1.1f, 1.8f)] public float expansionXZ = 1.35f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sonidoBloop;

    private Vector3 escalaOriginal;
    private Coroutine corrutinaBloop;

    private void Awake()
    {
        if (modeloVisual == null) modeloVisual = transform;
        escalaOriginal = modeloVisual.localScale;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Recibe el impacto. Acepta opcionalmente la normal de la superficie para orientar el VFX.
    /// </summary>
    public void RecibirImpacto(Vector3 puntoExactoImpacto, Vector3 normalSuperficie = default)
    {
        // 1. Instanciación y forzado de reproducción del VFX
        if (prefabVFXImpacto != null)
        {
            Vector3 direccionSalida = normalSuperficie != Vector3.zero ? normalSuperficie : Vector3.up;
            Vector3 posicionVFX = puntoExactoImpacto + (direccionSalida * offsetSuperficieVFX);

            Quaternion rotacionVFX = direccionSalida != Vector3.zero
                ? Quaternion.LookRotation(direccionSalida)
                : Quaternion.Euler(-90f, 0f, 0f);

            GameObject vfx = Instantiate(prefabVFXImpacto, posicionVFX, rotacionVFX);
            vfx.SetActive(true);

            // Forzamos el Play por si el prefab no tiene Play On Awake
            ParticleSystem[] emisores = vfx.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in emisores)
            {
                ps.Play();
            }

            Destroy(vfx, 3f);
        }

        // 2. Audio bloop
        if (audioSource != null && sonidoBloop != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.25f);
            audioSource.PlayOneShot(sonidoBloop);
        }

        // 3. Deformación Squash & Stretch
        if (corrutinaBloop != null) StopCoroutine(corrutinaBloop);
        corrutinaBloop = StartCoroutine(RutinaBloop());

        // 4. Expulsión en cono
        LanzarPiedraEnCono();
    }

    private IEnumerator RutinaBloop()
    {
        float t = 0f;

        Vector3 escalaAplastada = new Vector3(
            escalaOriginal.x * expansionXZ,
            escalaOriginal.y * aplastamientoY,
            escalaOriginal.z * expansionXZ
        );

        Vector3 escalaEstirada = new Vector3(
            escalaOriginal.x * (1f / expansionXZ),
            escalaOriginal.y * (2f - aplastamientoY),
            escalaOriginal.z * (1f / expansionXZ)
        );

        float fase1 = duracionBloop * 0.25f;
        while (t < fase1)
        {
            t += Time.deltaTime;
            modeloVisual.localScale = Vector3.Lerp(escalaOriginal, escalaAplastada, t / fase1);
            yield return null;
        }

        t = 0f;
        float fase2 = duracionBloop * 0.35f;
        while (t < fase2)
        {
            t += Time.deltaTime;
            modeloVisual.localScale = Vector3.Lerp(escalaAplastada, escalaEstirada, t / fase2);
            yield return null;
        }

        t = 0f;
        float fase3 = duracionBloop * 0.4f;
        while (t < fase3)
        {
            t += Time.deltaTime;
            modeloVisual.localScale = Vector3.Lerp(escalaEstirada, escalaOriginal, Mathf.SmoothStep(0f, 1f, t / fase3));
            yield return null;
        }

        modeloVisual.localScale = escalaOriginal;
    }

    private void LanzarPiedraEnCono()
    {
        if (prefabPiedra == null) return;

        Vector3 origenSpawn = ObtenerPuntoOrigen();

        // Dirección horizontal aleatoria en 360 grados con magnitud entre min y max
        Vector2 direccionPlana = Random.insideUnitCircle.normalized;
        float magnitudHorizontal = Random.Range(fuerzaHorizontalMin, fuerzaHorizontalMax);

        Vector3 vectorImpulso = (Vector3.up * fuerzaVertical) +
                                new Vector3(direccionPlana.x * magnitudHorizontal, 0f, direccionPlana.y * magnitudHorizontal);

        GameObject piedra = Instantiate(prefabPiedra, origenSpawn, Random.rotation);

        // Inmunidad temporal de 0.5s
        if (tiempoInmunidadPiedra > 0f)
        {
            InmunidadTemporalPiedra inmunidad = piedra.AddComponent<InmunidadTemporalPiedra>();
            inmunidad.IniciarInmunidad(tiempoInmunidadPiedra);
        }

        // Físicas
        Rigidbody rb = piedra.GetComponent<Rigidbody>();
        if (rb == null) rb = piedra.AddComponent<Rigidbody>();

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(vectorImpulso, ForceMode.Impulse);

        Vector3 torque = new Vector3(
            Random.Range(-fuerzaRotacionRandom, fuerzaRotacionRandom),
            Random.Range(-fuerzaRotacionRandom, fuerzaRotacionRandom),
            Random.Range(-fuerzaRotacionRandom, fuerzaRotacionRandom)
        );
        rb.AddTorque(torque, ForceMode.Impulse);
    }

    public Vector3 ObtenerPuntoOrigen()
    {
        return puntoSalidaPiedra != null
            ? puntoSalidaPiedra.position
            : transform.position + (Vector3.up * 1f);
    }

    // ====================================================================
    // GIZMOS: DIBUJO DEL CONO Y PARÁBOLAS EN ESCENA
    // ====================================================================
    private void OnDrawGizmosSelected()
    {
        Vector3 origen = ObtenerPuntoOrigen();

        // 1. Eje central del cono (Amarillo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origen, Vector3.up * 3f);

        // 2. Anillos superior del cono
        float alturaReferencia = 3f;
        float radioMin = (fuerzaHorizontalMin / Mathf.Max(fuerzaVertical, 0.1f)) * alturaReferencia;
        float radioMax = (fuerzaHorizontalMax / Mathf.Max(fuerzaVertical, 0.1f)) * alturaReferencia;
        Vector3 centroCirculo = origen + (Vector3.up * alturaReferencia);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        DibujarCirculoGizmo(centroCirculo, radioMax, 24);

        Gizmos.color = new Color(1f, 0.8f, 0f, 0.4f);
        DibujarCirculoGizmo(centroCirculo, radioMin, 16);

        // 3. Aristas del cono
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawLine(origen, centroCirculo + new Vector3(radioMax, 0f, 0f));
        Gizmos.DrawLine(origen, centroCirculo + new Vector3(-radioMax, 0f, 0f));
        Gizmos.DrawLine(origen, centroCirculo + new Vector3(0f, 0f, radioMax));
        Gizmos.DrawLine(origen, centroCirculo + new Vector3(0f, 0f, -radioMax));

        // 4. Simulación de trayectorias en los bordes del cono (Azul Cian)
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.6f);
        DibujarParabola(origen, (Vector3.up * fuerzaVertical) + (Vector3.right * fuerzaHorizontalMax));
        DibujarParabola(origen, (Vector3.up * fuerzaVertical) + (Vector3.left * fuerzaHorizontalMax));
        DibujarParabola(origen, (Vector3.up * fuerzaVertical) + (Vector3.forward * fuerzaHorizontalMax));
        DibujarParabola(origen, (Vector3.up * fuerzaVertical) + (Vector3.back * fuerzaHorizontalMax));
    }

    private void DibujarCirculoGizmo(Vector3 centro, float radio, int segmentos)
    {
        float paso = 360f / segmentos;
        Vector3 previo = centro + new Vector3(radio, 0f, 0f);

        for (int i = 1; i <= segmentos; i++)
        {
            float rad = i * paso * Mathf.Deg2Rad;
            Vector3 siguiente = centro + new Vector3(Mathf.Cos(rad) * radio, 0f, Mathf.Sin(rad) * radio);
            Gizmos.DrawLine(previo, siguiente);
            previo = siguiente;
        }
    }

    private void DibujarParabola(Vector3 origen, Vector3 velocidadInicial)
    {
        Vector3 anterior = origen;
        Vector3 gravedad = Physics.gravity;
        int pasos = 20;
        float dt = 1.4f / pasos;

        for (int i = 1; i <= pasos; i++)
        {
            float t = i * dt;
            Vector3 actual = origen + (velocidadInicial * t) + (0.5f * gravedad * t * t);
            Gizmos.DrawLine(anterior, actual);
            anterior = actual;
        }
    }
}

// Inmunidad temporal para evitar colisiones/patadas inmediatas
public class InmunidadTemporalPiedra : MonoBehaviour
{
    private string tagOriginal;
    private int layerOriginal;

    public void IniciarInmunidad(float duracion)
    {
        tagOriginal = gameObject.tag;
        layerOriginal = gameObject.layer;

        gameObject.tag = "Untagged";
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        StartCoroutine(RutinaRestaurar(duracion));
    }

    private IEnumerator RutinaRestaurar(float duracion)
    {
        yield return new WaitForSeconds(duracion);

        if (this != null && gameObject != null)
        {
            gameObject.tag = tagOriginal;
            gameObject.layer = layerOriginal;
            Destroy(this);
        }
    }
}