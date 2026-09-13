using UnityEngine;

public class ArmaLanzadora : MonoBehaviour
{
    [Header("Controles")]
    public KeyCode teclaEquipar = KeyCode.Alpha3;
    public KeyCode teclaDisparo = KeyCode.Mouse0;

    [Header("Referencias")]
    public GameObject modeloArma;
    public Transform puntoDisparo;
    public GameObject prefabProyectil;

    [Header("Animación (Blend Shape)")]
    public SkinnedMeshRenderer mallaArma;
    public int indiceBlendShape = 1;
    public float pesoMaximoBlendShape = 100f;
    public float velocidadRecuperacionBlend = 15f;

    [Header("Ajustes de Disparo")]
    public float fuerzaDisparo = 25f;
    public float cadenciaDisparo = 0.2f;
    public float vidaProyectil = 10f;

    [Header("Dispersión")]
    public float anguloDispersion = 5f;

    [Header("Crecimiento del Proyectil")]
    public float escalaMaxima = 2.5f;
    public float velocidadCrecimiento = 8f;

    [Header("Inmunidad de Recogida")]
    public float tiempoInmunidad = 1.5f;

    [Header("Game Feel (Efectos)")]
    public ParticleSystem particulasDisparo;
    public AudioSource audioSource;
    public AudioClip sonidoEquipar;
    public AudioClip sonidoDisparo;

    public float fuerzaRetrocesoVisual = 0.3f;
    public float velocidadRecuperacionRetroceso = 15f;

    private bool armaEquipada = false;
    private float tiempoProximoDisparo = 0f;
    private Vector3 escalaOriginalArma;

    private float retrocesoActual = 0f;
    private float pesoActualBlend = 0f;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (modeloArma != null)
        {
            escalaOriginalArma = modeloArma.transform.localScale;
            modeloArma.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(teclaEquipar)) AlternarArma();

        if (armaEquipada)
        {
            ManejarDisparo();
            ManejarRetrocesoVisual();
            ManejarBlendShape();
        }
    }

    void AlternarArma()
    {
        armaEquipada = !armaEquipada;
        if (modeloArma != null) modeloArma.SetActive(armaEquipada);
        if (armaEquipada && sonidoEquipar != null) audioSource.PlayOneShot(sonidoEquipar, 0.8f);
    }

    void ManejarDisparo()
    {
        if (Input.GetKey(teclaDisparo) && Time.time >= tiempoProximoDisparo)
        {
            tiempoProximoDisparo = Time.time + cadenciaDisparo;
            Disparar();
        }
    }

    void Disparar()
    {
        if (prefabProyectil != null && puntoDisparo != null)
        {
            // 1. Calcular la dirección final del disparo con la dispersión
            Vector2 dispersionAleatoria = Random.insideUnitCircle * anguloDispersion;
            Quaternion rotacionDispersion = Quaternion.Euler(dispersionAleatoria.y, dispersionAleatoria.x, 0f);
            Vector3 direccionFinal = (puntoDisparo.rotation * rotacionDispersion) * Vector3.forward;

            // --- ORIENTACIÓN TIPO FRISBEE ---
            // Para que actúe como un disco volador que se orienta a la cámara:
            // Usamos la rotación de la cámara (puntoDisparo) pero proyectada al vector de avance, 
            // asegurando que la cara del objeto (Eje X) acompañe el vuelo y el eje superior mire al cielo de la cámara.
            Quaternion rotacionFrisbee = Quaternion.LookRotation(direccionFinal, puntoDisparo.up);

            GameObject proyectil = Instantiate(prefabProyectil, puntoDisparo.position, rotacionFrisbee);

            Rigidbody rb = proyectil.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Empuje frontal en la dirección calculada
                rb.AddForce(direccionFinal * fuerzaDisparo, ForceMode.Impulse);

                // Giro puro en el eje Y local de la moneda (para que ruede como un frisbee en el aire)
                float fuerzaGiro = Random.Range(-15f, 15f);
                rb.AddRelativeTorque(new Vector3(0f, fuerzaGiro, 0f), ForceMode.Impulse);
            }

            CrecimientoProyectil animCrecimiento = proyectil.AddComponent<CrecimientoProyectil>();
            animCrecimiento.Configurar(escalaMaxima, velocidadCrecimiento);

            ProteccionMagnetica animInmunidad = proyectil.AddComponent<ProteccionMagnetica>();
            animInmunidad.Configurar(tiempoInmunidad);

            Destroy(proyectil, vidaProyectil);
        }

        // Efectos
        if (particulasDisparo != null) particulasDisparo.Play();

        if (audioSource != null && sonidoDisparo != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(sonidoDisparo, 0.7f);
        }

        retrocesoActual = fuerzaRetrocesoVisual;
        pesoActualBlend = pesoMaximoBlendShape;
    }

    void ManejarRetrocesoVisual()
    {
        if (modeloArma != null && fuerzaRetrocesoVisual > 0f)
        {
            retrocesoActual = Mathf.Lerp(retrocesoActual, 0f, Time.deltaTime * velocidadRecuperacionRetroceso);
            modeloArma.transform.localScale = new Vector3(
                escalaOriginalArma.x,
                escalaOriginalArma.y,
                escalaOriginalArma.z * (1f - retrocesoActual)
            );
        }
    }

    void ManejarBlendShape()
    {
        if (mallaArma != null)
        {
            pesoActualBlend = Mathf.Lerp(pesoActualBlend, 0f, Time.deltaTime * velocidadRecuperacionBlend);
            mallaArma.SetBlendShapeWeight(indiceBlendShape, pesoActualBlend);
        }
    }
}

// ====================================================================
// --- MINI-SCRIPTS AUXILIARES ---
// ====================================================================

public class CrecimientoProyectil : MonoBehaviour
{
    private Vector3 escalaObjetivo;
    private float velocidad;

    public void Configurar(float multiplicadorEscala, float vel)
    {
        escalaObjetivo = transform.localScale * multiplicadorEscala;
        velocidad = vel;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, escalaObjetivo, Time.deltaTime * velocidad);

        if (Vector3.Distance(transform.localScale, escalaObjetivo) < 0.01f)
        {
            transform.localScale = escalaObjetivo;
            Destroy(this);
        }
    }
}

public class ProteccionMagnetica : MonoBehaviour
{
    private string tagOriginal;

    public void Configurar(float tiempo)
    {
        tagOriginal = gameObject.tag;
        gameObject.tag = "Untagged";
        Invoke("RestaurarTag", tiempo);
    }

    private void RestaurarTag()
    {
        gameObject.tag = tagOriginal;
        Destroy(this);
    }
}