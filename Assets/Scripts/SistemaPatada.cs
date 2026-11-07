using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class SistemaPatada : MonoBehaviour
{
    [Header("Ajustes de la Patada")]
    public float fuerzaPatada = 10f;
    public int maxPiedrasPorPatada = 3;

    [Header("Limites de Elevación")]
    public float anguloFielMaximo = 30f;
    public float anguloElevacionMaximo = 45f;

    [Header("Detección de Golpe")]
    public float radioGolpe = 1.5f;
    public Transform puntoGolpe;
    public LayerMask capaPiedra;
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

    private struct CandidataArana
    {
        public AranaPateable arana;
        public float distanciaCuadrada;
        public Vector3 puntoImpacto;
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        scriptAgarre = FindObjectOfType<AgarreLanzamiento>();
        gestorEquipamiento = FindObjectOfType<GestorEquipamiento>();
        recolectorMagnetico = GetComponentInParent<RecolectorMagnetico>();
        if (recolectorMagnetico == null)
            recolectorMagnetico = FindObjectOfType<RecolectorMagnetico>();
    }

    private void Update()
    {
        if (gestorEquipamiento != null && !gestorEquipamiento.PuedeUsarManos())
            return;

        if (scriptAgarre != null && scriptAgarre.EstaSosteniendoPiedra())
            return;

        if (Input.GetMouseButtonDown(0))
        {
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

    private void DarPatada()
    {
        Camera camara = Camera.main;
        if (puntoGolpe == null || camara == null)
            return;

        Transform camaraTrans = camara.transform;
        Vector3 forwardCamaraPlano = new Vector3(
            camaraTrans.forward.x,
            0f,
            camaraTrans.forward.z
        ).normalized;

        Collider[] objetosGolpeados = Physics.OverlapSphere(
            puntoGolpe.position,
            radioGolpe,
            capaPiedra
        );

        List<Collider> listaPiedras = new List<Collider>();

        foreach (Collider col in objetosGolpeados)
        {
            if (col.GetComponentInParent<AranaPateable>() != null)
                continue;

            RescateVacioPiedra rescate =
                col.GetComponentInParent<RescateVacioPiedra>();

            if (rescate == null && col.attachedRigidbody != null)
                rescate = col.attachedRigidbody.GetComponent<RescateVacioPiedra>();

            if (rescate != null && rescate.ProtegidaContraGolpes)
                continue;

            if (col.GetComponentInParent<Moneda>() != null)
                continue;

            if (EstaEnAnguloDeGolpe(col.bounds.center, camaraTrans, forwardCamaraPlano))
                listaPiedras.Add(col);
        }

        listaPiedras.Sort((a, b) =>
            Vector3.Distance(puntoGolpe.position, a.transform.position)
            .CompareTo(Vector3.Distance(puntoGolpe.position, b.transform.position)));

        float distanciaPiedraCuadrada = float.PositiveInfinity;

        foreach (Collider col in listaPiedras)
        {
            if (col.attachedRigidbody == null && col.GetComponent<Rigidbody>() == null)
                continue;

            distanciaPiedraCuadrada =
                (col.transform.position - puntoGolpe.position).sqrMagnitude;
            break;
        }

        Collider[] colisionadoresAranas = Physics.OverlapSphere(
            puntoGolpe.position,
            radioGolpe,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        Dictionary<AranaPateable, CandidataArana> candidatasUnicas =
            new Dictionary<AranaPateable, CandidataArana>();

        foreach (Collider col in colisionadoresAranas)
        {
            AranaPateable arana = col.GetComponentInParent<AranaPateable>();

            if (arana == null || !arana.isActiveAndEnabled)
                continue;

            if (!EstaEnAnguloDeGolpe(col.bounds.center, camaraTrans, forwardCamaraPlano))
                continue;

            float distanciaCuadrada =
                (col.transform.position - puntoGolpe.position).sqrMagnitude;

            if (!candidatasUnicas.TryGetValue(arana, out CandidataArana anterior) ||
                distanciaCuadrada < anterior.distanciaCuadrada)
            {
                candidatasUnicas[arana] = new CandidataArana
                {
                    arana = arana,
                    distanciaCuadrada = distanciaCuadrada,
                    puntoImpacto = col.bounds.center
                };
            }
        }

        List<CandidataArana> candidatasOrdenadas =
            new List<CandidataArana>(candidatasUnicas.Values);

        candidatasOrdenadas.Sort((a, b) =>
            a.distanciaCuadrada.CompareTo(b.distanciaCuadrada));

        foreach (CandidataArana candidata in candidatasOrdenadas)
        {
            if (candidata.distanciaCuadrada > distanciaPiedraCuadrada)
                break;

            if (candidata.arana.IntentarPatear(forwardCamaraPlano))
            {
                ReproducirEfectosPatada(candidata.puntoImpacto);
                return;
            }
        }

        if (listaPiedras.Count == 0)
            return;

        PatearPiedras(listaPiedras, camaraTrans);
    }

    private bool EstaEnAnguloDeGolpe(
        Vector3 punto,
        Transform camaraTrans,
        Vector3 forwardCamaraPlano)
    {
        Vector3 direccion = punto - camaraTrans.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude <= 0.001f)
            return true;

        direccion.Normalize();
        float angulo = Vector3.Angle(forwardCamaraPlano, direccion);
        return angulo <= anguloVisionMaximo;
    }

    private void PatearPiedras(List<Collider> listaPiedras, Transform camaraTrans)
    {
        int cantidadAGolpear = Mathf.Min(listaPiedras.Count, maxPiedrasPorPatada);
        bool golpeReal = false;

        for (int i = 0; i < cantidadAGolpear; i++)
        {
            Collider colision = listaPiedras[i];
            Rigidbody rbPiedra = colision.attachedRigidbody;
            if (rbPiedra == null)
                rbPiedra = colision.GetComponent<Rigidbody>();
            if (rbPiedra == null)
                continue;

            DeformacionPiedra scriptPiedra = rbPiedra.GetComponent<DeformacionPiedra>();
            if (scriptPiedra != null)
                scriptPiedra.Despertar();

            Vector3 dirCamara = camaraTrans.forward;
            float anguloCamara = Mathf.Asin(Mathf.Clamp(dirCamara.y, -1f, 1f)) * Mathf.Rad2Deg;
            float anguloPatada = anguloCamara;

            if (anguloCamara > anguloFielMaximo)
            {
                float rangoCamaraRestante = 90f - anguloFielMaximo;
                if (rangoCamaraRestante <= 0.001f)
                    rangoCamaraRestante = 0.001f;
                float rangoPatadaRestante = anguloElevacionMaximo - anguloFielMaximo;
                float t = Mathf.Clamp01((anguloCamara - anguloFielMaximo) / rangoCamaraRestante);
                float suavizado = Mathf.Sin(t * Mathf.PI / 2f);
                anguloPatada = anguloFielMaximo + (rangoPatadaRestante * suavizado);
            }

            Vector3 dirPlano = new Vector3(dirCamara.x, 0, dirCamara.z).normalized;
            if (dirPlano.sqrMagnitude < 0.001f)
                dirPlano = camaraTrans.up;

            Vector3 direccionFinal =
                dirPlano * Mathf.Cos(anguloPatada * Mathf.Deg2Rad) +
                Vector3.up * Mathf.Sin(anguloPatada * Mathf.Deg2Rad);

            rbPiedra.AddForce(direccionFinal * fuerzaPatada, ForceMode.Impulse);
            rbPiedra.AddTorque(new Vector3(
                Random.Range(-5f, 5f),
                Random.Range(-5f, 5f),
                Random.Range(-5f, 5f)
            ), ForceMode.Impulse);

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

    private void ReproducirEfectosPatada(Vector3 puntoImpacto)
    {
        if (particulasPatada != null)
        {
            particulasPatada.transform.position = puntoImpacto;
            particulasPatada.Play();
        }

        if (sonidoPatada != null)
            audioSource.PlayOneShot(sonidoPatada, volumenPatada);
    }

    private void OnDrawGizmosSelected()
    {
        if (puntoGolpe == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(puntoGolpe.position, radioGolpe);

        if (Camera.main == null)
            return;

        Vector3 origen = puntoGolpe.position;
        Vector3 forward = Camera.main.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 limiteDerecho = Quaternion.Euler(0, anguloVisionMaximo, 0) * forward;
        Vector3 limiteIzquierdo = Quaternion.Euler(0, -anguloVisionMaximo, 0) * forward;

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);
        Gizmos.DrawRay(origen, limiteDerecho * radioGolpe);
        Gizmos.DrawRay(origen, limiteIzquierdo * radioGolpe);
        Gizmos.DrawRay(origen, forward * radioGolpe);
    }
}
