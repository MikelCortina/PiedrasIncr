using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class SistemaPatada : MonoBehaviour
{
    [Header("Ajustes de la Patada")]
    public float fuerzaPatada = 10f;
    [Tooltip("Cantidad máxima de piedras que puedes patear simultáneamente en un solo golpe.")]
    public int maxPiedrasPorPatada = 3;

    [Header("Limites de Elevación")]
    public float anguloFielMaximo = 30f;
    public float anguloElevacionMaximo = 45f;

    [Header("Detección de Golpe")]
    public float radioGolpe = 1.5f;
    public Transform puntoGolpe;
    public LayerMask capaPiedra;

    [Tooltip("Ángulo máximo hacia los lados desde el centro de tu visión donde puedes detectar piedras. 60 es similar a lo que ves en pantalla.")]
    public float anguloVisionMaximo = 60f;

    [Header("Sonidos")]
    public AudioClip sonidoPatada;
    [Range(0f, 1f)] public float volumenPatada = 1f;

    [Header("Efectos Visuales")]
    [Tooltip("Arrastra aquí el sistema de partículas que saltará al dar la patada")]
    public ParticleSystem particulasPatada;

    private AudioSource audioSource;
    private AgarreLanzamiento scriptAgarre;
    private GestorEquipamiento gestorEquipamiento;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        scriptAgarre = FindObjectOfType<AgarreLanzamiento>();
        gestorEquipamiento = FindObjectOfType<GestorEquipamiento>();
    }

    void Update()
    {
        // Si tenemos cualquier herramienta equipada, no podemos patear
        if (gestorEquipamiento != null && !gestorEquipamiento.PuedeUsarManos())
        {
            return;
        }

        // Si estamos sujetando una piedra tampoco podemos patear
        if (scriptAgarre != null && scriptAgarre.EstaSosteniendoPiedra())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            DarPatada();
        }
    }

    void DarPatada()
    {
        Collider[] objetosGolpeados = Physics.OverlapSphere(puntoGolpe.position, radioGolpe, capaPiedra);

        if (objetosGolpeados.Length == 0) return;

        List<Collider> listaPiedras = new List<Collider>();
        Transform camaraTrans = Camera.main.transform;

        // Obtenemos la dirección hacia adelante de la cámara aplastada en el plano X-Z (ignorando arriba/abajo)
        Vector3 forwardCamaraPlano = new Vector3(camaraTrans.forward.x, 0, camaraTrans.forward.z).normalized;

        // --- FILTRO DE VISIÓN (Solo delante del jugador) ---
        foreach (Collider col in objetosGolpeados)
        {
            // Vector desde la cámara hasta la piedra
            Vector3 direccionHaciaPiedra = col.bounds.center - camaraTrans.position;
            // Lo aplastamos también para ignorar si la piedra está en el suelo y la cámara muy arriba
            direccionHaciaPiedra.y = 0;

            if (direccionHaciaPiedra.sqrMagnitude > 0.001f)
            {
                direccionHaciaPiedra.Normalize();
                // Calculamos el ángulo entre donde miramos y donde está la piedra
                float angulo = Vector3.Angle(forwardCamaraPlano, direccionHaciaPiedra);

                // Si está dentro del cono de visión, es válida
                if (angulo <= anguloVisionMaximo)
                {
                    listaPiedras.Add(col);
                }
            }
            else
            {
                // Si la distancia horizontal es prácticamente 0, significa que está exactamente 
                // debajo de nosotros en la misma coordenada XZ. La damos por válida.
                listaPiedras.Add(col);
            }
        }

        // Si después de filtrar las de atrás y los lados no queda ninguna, salimos
        if (listaPiedras.Count == 0) return;

        // Ordenamos de más cercana a más lejana respecto al punto del golpe
        listaPiedras.Sort((a, b) =>
            Vector3.Distance(puntoGolpe.position, a.transform.position)
            .CompareTo(Vector3.Distance(puntoGolpe.position, b.transform.position))
        );

        // Limitamos la cantidad de piedras a procesar
        int cantidadAGolpear = Mathf.Min(listaPiedras.Count, maxPiedrasPorPatada);

        for (int i = 0; i < cantidadAGolpear; i++)
        {
            Collider colision = listaPiedras[i];

            DeformacionPiedra scriptPiedra = colision.GetComponent<DeformacionPiedra>();
            if (scriptPiedra != null) scriptPiedra.Despertar();

            Rigidbody rbPiedra = colision.GetComponent<Rigidbody>();

            if (rbPiedra != null)
            {
                Vector3 dirCamara = camaraTrans.forward;

                float anguloCamara = Mathf.Asin(Mathf.Clamp(dirCamara.y, -1f, 1f)) * Mathf.Rad2Deg;
                float anguloPatada = anguloCamara;

                if (anguloCamara > anguloFielMaximo)
                {
                    float rangoCamaraRestante = 90f - anguloFielMaximo;
                    if (rangoCamaraRestante <= 0.001f) rangoCamaraRestante = 0.001f;

                    float rangoPatadaRestante = anguloElevacionMaximo - anguloFielMaximo;

                    float t = (anguloCamara - anguloFielMaximo) / rangoCamaraRestante;
                    t = Mathf.Clamp01(t);

                    float suavizado = Mathf.Sin(t * Mathf.PI / 2f);
                    anguloPatada = anguloFielMaximo + (rangoPatadaRestante * suavizado);
                }

                Vector3 dirPlano = new Vector3(dirCamara.x, 0, dirCamara.z).normalized;
                if (dirPlano.sqrMagnitude < 0.001f) dirPlano = camaraTrans.up;

                Vector3 direccionFinal = (dirPlano * Mathf.Cos(anguloPatada * Mathf.Deg2Rad)) + (Vector3.up * Mathf.Sin(anguloPatada * Mathf.Deg2Rad));

                rbPiedra.AddForce(direccionFinal * fuerzaPatada, ForceMode.Impulse);
                rbPiedra.AddTorque(new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f)), ForceMode.Impulse);

                if (i == 0 && particulasPatada != null)
                {
                    particulasPatada.transform.position = rbPiedra.transform.position;
                    particulasPatada.Play();
                }
            }
        }

        if (sonidoPatada != null)
        {
            audioSource.PlayOneShot(sonidoPatada, volumenPatada);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (puntoGolpe != null)
        {
            // Dibujamos la esfera de detección
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(puntoGolpe.position, radioGolpe);

            // Dibujamos el cono de visión permitido en azul
            if (Camera.main != null)
            {
                Vector3 origen = puntoGolpe.position;
                Vector3 forward = Camera.main.transform.forward;
                forward.y = 0;
                forward.Normalize();

                Vector3 limiteDerecho = Quaternion.Euler(0, anguloVisionMaximo, 0) * forward;
                Vector3 limiteIzquierdo = Quaternion.Euler(0, -anguloVisionMaximo, 0) * forward;

                Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);
                Gizmos.DrawRay(origen, limiteDerecho * radioGolpe);
                Gizmos.DrawRay(origen, limiteIzquierdo * radioGolpe);
                Gizmos.DrawRay(origen, forward * radioGolpe);
            }
        }
    }
}