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

    [Header("Sonidos")]
    public AudioClip sonidoPatada;
    [Range(0f, 1f)] public float volumenPatada = 1f;

    [Header("Efectos Visuales")]
    [Tooltip("Arrastra aquí el sistema de partículas que saltará al dar la patada")]
    public ParticleSystem particulasPatada;

    private AudioSource audioSource;
    private AgarreLanzamiento scriptAgarre;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        scriptAgarre = FindObjectOfType<AgarreLanzamiento>();
    }

    void Update()
    {
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

        // ¡FILTRO DE SEGURIDAD! Si no hay piedras en el radio, salimos al instante (no hay sonido ni acción)
        if (objetosGolpeados.Length == 0) return;

        // Convertimos el array en una lista para poder ordenarlas por cercanía
        List<Collider> listaPiedras = new List<Collider>(objetosGolpeados);

        // Ordenamos de más cercana a más lejana respecto al punto del golpe
        listaPiedras.Sort((a, b) =>
            Vector3.Distance(puntoGolpe.position, a.transform.position)
            .CompareTo(Vector3.Distance(puntoGolpe.position, b.transform.position))
        );

        // Limitamos la cantidad de piedras a procesar según el límite configurado
        int cantidadAGolpear = Mathf.Min(listaPiedras.Count, maxPiedrasPorPatada);

        for (int i = 0; i < cantidadAGolpear; i++)
        {
            Collider colision = listaPiedras[i];

            DeformacionPiedra scriptPiedra = colision.GetComponent<DeformacionPiedra>();
            if (scriptPiedra != null) scriptPiedra.Despertar();

            Rigidbody rbPiedra = colision.GetComponent<Rigidbody>();

            if (rbPiedra != null)
            {
                Vector3 dirCamara = Camera.main.transform.forward;

                // 1. CORRECCIÓN NaN: Usamos Mathf.Clamp 
                float anguloCamara = Mathf.Asin(Mathf.Clamp(dirCamara.y, -1f, 1f)) * Mathf.Rad2Deg;
                float anguloPatada = anguloCamara;

                // 2. Cálculo de Elevación
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

                // 3. Dirección horizontal
                Vector3 dirPlano = new Vector3(dirCamara.x, 0, dirCamara.z).normalized;
                if (dirPlano.sqrMagnitude < 0.001f) dirPlano = Camera.main.transform.up;

                // 4. Vector final
                Vector3 direccionFinal = (dirPlano * Mathf.Cos(anguloPatada * Mathf.Deg2Rad)) + (Vector3.up * Mathf.Sin(anguloPatada * Mathf.Deg2Rad));

                // 5. ¡Pateamos!
                rbPiedra.AddForce(direccionFinal * fuerzaPatada, ForceMode.Impulse);
                rbPiedra.AddTorque(new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f)), ForceMode.Impulse);

                // 6. Efectos visuales en la primera piedra golpeada
                if (i == 0 && particulasPatada != null)
                {
                    particulasPatada.transform.position = rbPiedra.transform.position;
                    particulasPatada.Play();
                }
            }
        }

        // 7. Sonido general de la patada (AHORA SÍ: Solo suena si encontró y golpeó al menos una piedra)
        if (sonidoPatada != null)
        {
            audioSource.PlayOneShot(sonidoPatada, volumenPatada);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (puntoGolpe != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(puntoGolpe.position, radioGolpe);
        }
    }
}