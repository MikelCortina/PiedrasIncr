using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RecolectorMagnetico : MonoBehaviour
{
    [Header("Ajustes del Imán")]
    public float radioAtraccion = 5f;
    public float velocidadAtraccion = 15f;
    public float distanciaRecoleccion = 1.5f;

    [Header("Protección de Creación")]
    [Tooltip("Tiempo mínimo que debe pasar desde que se crea la moneda para poder ser atraída")]
    public float tiempoInmunidadCreacion = 1.5f;

    [Header("Torque Magnético")]
    [Tooltip("Velocidad a la que dan volteretas las monedas mientras vuelan hacia ti")]
    public float velocidadRotacionVuelo = 720f;

    [Header("Filtros")]
    public LayerMask capaRecolectables;
    public string tagRecolectable = "Recolectable";

    [Header("Sonido (Opcional)")]
    public AudioClip sonidoRecoger;
    [Range(0f, 1f)] public float volumenSonido = 1f;

    [Header("Seguro de Sonido")]
    [Tooltip("Tiempo mínimo entre sonidos para evitar que estallen los oídos")]
    public float cooldownSonido = 0.05f;
    private float ultimoTiempoSonido = 0f;

    private AudioSource audioSource;
    private List<GameObject> objetosAtrayendo = new List<GameObject>();

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        Collider[] objetosCercanos = Physics.OverlapSphere(transform.position, radioAtraccion, capaRecolectables);

        foreach (Collider col in objetosCercanos)
        {
            GameObject objetoReal = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;

            if (objetoReal.CompareTag(tagRecolectable) && !objetosAtrayendo.Contains(objetoReal))
            {
                // --- NUEVO: COMPROBACIÓN DE EDAD DE LA MONEDA ---
                // Buscamos si tiene el componente de registro de tiempo de creación
                EdadMoneda edad = objetoReal.GetComponent<EdadMoneda>();

                // Si tiene el componente y aún no ha pasado el tiempo de inmunidad, lo ignoramos
                if (edad != null && !edad.HaSuperadoInmunidad(tiempoInmunidadCreacion))
                {
                    continue; // Salta a la siguiente moneda sin atraer esta
                }

                PrepararObjetoParaVolar(objetoReal);
            }
        }

        for (int i = objetosAtrayendo.Count - 1; i >= 0; i--)
        {
            GameObject obj = objetosAtrayendo[i];

            if (obj == null)
            {
                objetosAtrayendo.RemoveAt(i);
                continue;
            }

            // 1. MOVER hacia el jugador
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, transform.position, velocidadAtraccion * Time.deltaTime);

            // 2. SIMULAR TORQUE (Giro hacia el jugador)
            Vector3 direccionVuelo = (transform.position - obj.transform.position).normalized;
            Vector3 ejeGiro = Vector3.Cross(Vector3.up, direccionVuelo) + new Vector3(0.1f, 0.3f, 0.1f);
            obj.transform.Rotate(ejeGiro.normalized * velocidadRotacionVuelo * Time.deltaTime, Space.World);

            // 3. RECOGER
            if (Vector3.Distance(transform.position, obj.transform.position) <= distanciaRecoleccion)
            {
                objetosAtrayendo.RemoveAt(i);
                Recoger(obj);
            }
        }
    }

    void PrepararObjetoParaVolar(GameObject objeto)
    {
        objetosAtrayendo.Add(objeto);

        Rigidbody rb = objeto.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider[] colliders = objeto.GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }
    }

    void Recoger(GameObject objeto)
    {
        if (sonidoRecoger != null && Time.time >= ultimoTiempoSonido + cooldownSonido)
        {
            audioSource.pitch = Random.Range(0.90f, 1.10f);
            audioSource.PlayOneShot(sonidoRecoger, volumenSonido);
            ultimoTiempoSonido = Time.time;
        }

        Destroy(objeto);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioAtraccion);
    }
}

