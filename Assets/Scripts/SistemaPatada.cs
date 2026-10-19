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
    [Tooltip("Ángulo máximo hacia los lados desde el centro de tu visión donde puedes detectar piedras.")]
    public float anguloVisionMaximo = 60f;

    [Header("Sonidos")]
    public AudioClip sonidoPatada;
    [Range(0f, 1f)] public float volumenPatada = 1f;

    [Header("Efectos Visuales")]
    public ParticleSystem particulasPatada;

    private AudioSource audioSource;
    private AgarreLanzamiento scriptAgarre;
    private GestorEquipamiento gestorEquipamiento;
    private RecolectorMagnetico recolectorMagnetico;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        scriptAgarre = FindObjectOfType<AgarreLanzamiento>();
        gestorEquipamiento = FindObjectOfType<GestorEquipamiento>();
        recolectorMagnetico = GetComponentInParent<RecolectorMagnetico>();
        if (recolectorMagnetico == null)
            recolectorMagnetico = FindObjectOfType<RecolectorMagnetico>();
    }

    void Update()
    {
        if (gestorEquipamiento != null && !gestorEquipamiento.PuedeUsarManos())
            return;

        if (scriptAgarre != null && scriptAgarre.EstaSosteniendoPiedra())
            return;

        if (Input.GetMouseButtonDown(0))
        {
            // Si el clic recogió una moneda, NO patear en el mismo clic.
            // El recolector recuerda el resultado aunque su Update ya se ejecutara.
            if (recolectorMagnetico != null &&
                recolectorMagnetico.IntentarRecogerMonedaConClick())
                return;

            DarPatada();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            DarPatada();
        }
    }

    void DarPatada()
    {
        if (puntoGolpe == null || Camera.main == null) return;

        Collider[] objetosGolpeados = Physics.OverlapSphere(
            puntoGolpe.position, radioGolpe, capaPiedra);
        if (objetosGolpeados.Length == 0) return;

        List<Collider> listaPiedras = new List<Collider>();
        Transform camaraTrans = Camera.main.transform;
        Vector3 forwardCamaraPlano = new Vector3(
            camaraTrans.forward.x, 0, camaraTrans.forward.z).normalized;

        foreach (Collider col in objetosGolpeados)
        {
            // Mantener la protección de piedras que están rescatándose/levitando.
            RescateVacioPiedra rescate = col.GetComponentInParent<RescateVacioPiedra>();
            if (rescate == null && col.attachedRigidbody != null)
                rescate = col.attachedRigidbody.GetComponent<RescateVacioPiedra>();
            if (rescate != null && rescate.ProtegidaContraGolpes)
                continue;

            // Nunca tratar una moneda como una piedra que se puede patear.
            if (col.GetComponentInParent<Moneda>() != null)
                continue;

            Vector3 direccionHaciaPiedra = col.bounds.center - camaraTrans.position;
            direccionHaciaPiedra.y = 0;

            if (direccionHaciaPiedra.sqrMagnitude > 0.001f)
            {
                direccionHaciaPiedra.Normalize();
                float angulo = Vector3.Angle(forwardCamaraPlano, direccionHaciaPiedra);
                if (angulo <= anguloVisionMaximo)
                    listaPiedras.Add(col);
            }
            else
            {
                listaPiedras.Add(col);
            }
        }

        if (listaPiedras.Count == 0) return;

        listaPiedras.Sort((a, b) =>
            Vector3.Distance(puntoGolpe.position, a.transform.position)
            .CompareTo(Vector3.Distance(puntoGolpe.position, b.transform.position)));

        int cantidadAGolpear = Mathf.Min(listaPiedras.Count, maxPiedrasPorPatada);
        bool golpeReal = false;

        for (int i = 0; i < cantidadAGolpear; i++)
        {
            Collider colision = listaPiedras[i];
            Rigidbody rbPiedra = colision.attachedRigidbody;
            if (rbPiedra == null) rbPiedra = colision.GetComponent<Rigidbody>();
            if (rbPiedra == null) continue;

            DeformacionPiedra scriptPiedra = rbPiedra.GetComponent<DeformacionPiedra>();
            if (scriptPiedra != null) scriptPiedra.Despertar();

            Vector3 dirCamara = camaraTrans.forward;
            float anguloCamara = Mathf.Asin(Mathf.Clamp(dirCamara.y, -1f, 1f)) * Mathf.Rad2Deg;
            float anguloPatada = anguloCamara;

            if (anguloCamara > anguloFielMaximo)
            {
                float rangoCamaraRestante = 90f - anguloFielMaximo;
                if (rangoCamaraRestante <= 0.001f) rangoCamaraRestante = 0.001f;
                float rangoPatadaRestante = anguloElevacionMaximo - anguloFielMaximo;
                float t = Mathf.Clamp01((anguloCamara - anguloFielMaximo) / rangoCamaraRestante);
                float suavizado = Mathf.Sin(t * Mathf.PI / 2f);
                anguloPatada = anguloFielMaximo + (rangoPatadaRestante * suavizado);
            }

            Vector3 dirPlano = new Vector3(dirCamara.x, 0, dirCamara.z).normalized;
            if (dirPlano.sqrMagnitude < 0.001f) dirPlano = camaraTrans.up;

            Vector3 direccionFinal = (dirPlano * Mathf.Cos(anguloPatada * Mathf.Deg2Rad)) +
                                      (Vector3.up * Mathf.Sin(anguloPatada * Mathf.Deg2Rad));

            rbPiedra.AddForce(direccionFinal * fuerzaPatada, ForceMode.Impulse);
            rbPiedra.AddTorque(new Vector3(
                Random.Range(-5f, 5f), Random.Range(-5f, 5f),
                Random.Range(-5f, 5f)), ForceMode.Impulse);

            if (!golpeReal && particulasPatada != null)
            {
                particulasPatada.transform.position = rbPiedra.transform.position;
                particulasPatada.Play();
            }
            golpeReal = true;
        }

        if (golpeReal && sonidoPatada != null)
            audioSource.PlayOneShot(sonidoPatada, volumenPatada);
    }

    void OnDrawGizmosSelected()
    {
        if (puntoGolpe == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(puntoGolpe.position, radioGolpe);

        if (Camera.main == null) return;

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
