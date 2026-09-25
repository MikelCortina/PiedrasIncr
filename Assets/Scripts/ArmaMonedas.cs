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

    [Header("Efectos Visuales (Giro con Inercia)")]
    public Transform objetoGiratorio;
    public float velocidadGiroZ = 500f;
    [Tooltip("Qué tan rápido alcanza la velocidad máxima al disparar")]
    public float aceleracionGiro = 5f;
    [Tooltip("Qué tan rápido frena al soltar el gatillo (valores bajos = más inercia)")]
    public float desaceleracionGiro = 2f;

    [Header("Economía")]
    public Cartera cartera;

    [Min(1)]
    public int costePorDisparo = 1;

    [Header("Animación del Modelo (Sway & Bobbing)")]
    public float intensidadSway = 0.02f;
    public float limiteSway = 0.06f;
    public float intensidadInclinacion = 2f;
    public float velocidadBobbing = 10f;
    public float intensidadBobbing = 0.015f;
    public float suavidadAnimacion = 8f;

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

    // --- VARIABLES DE ANIMACIÓN ---
    private Vector3 posicionInicialModelo;
    private Quaternion rotacionInicialModelo;
    private float temporizadorBobbing = 0f;

    // --- VARIABLE DE VELOCIDAD ACTUAL ---
    private float velocidadGiroActual = 0f;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (cartera == null)
            cartera = GetComponentInParent<Cartera>();

        if (modeloArma != null)
        {
            escalaOriginalArma = modeloArma.transform.localScale;
            posicionInicialModelo = modeloArma.transform.localPosition;
            rotacionInicialModelo = modeloArma.transform.localRotation;

            modeloArma.SetActive(false);
        }
    }

    void Update()
    {
        if (armaEquipada)
        {
            ManejarDisparo();
            ManejarGiroObjeto();
            ManejarRetrocesoVisual();
            ManejarBlendShape();
            ActualizarAnimacionModelo();
        }
    }
    public void SetEquipada(bool equipada)
    {
        if (armaEquipada == equipada)
            return;

        armaEquipada = equipada;

        if (modeloArma != null)
        {
            modeloArma.SetActive(armaEquipada);

            if (armaEquipada)
            {
                modeloArma.transform.localPosition = posicionInicialModelo;
                modeloArma.transform.localRotation = rotacionInicialModelo;

                temporizadorBobbing = 0f;
                velocidadGiroActual = 0f;
            }
        }

        if (armaEquipada && sonidoEquipar != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonidoEquipar, 0.8f);
        }
    }

    void ManejarDisparo()
    {
        if (Input.GetKey(teclaDisparo) && Time.time >= tiempoProximoDisparo)
        {
            tiempoProximoDisparo = Time.time + cadenciaDisparo;
            Disparar();
        }
    }

    // --- NUEVO: SISTEMA DE INERCIA ---
    void ManejarGiroObjeto()
    {
        if (objetoGiratorio == null) return;

        if (Input.GetKey(teclaDisparo))
        {
            // Acelera suavemente hacia la velocidad máxima
            velocidadGiroActual = Mathf.Lerp(velocidadGiroActual, velocidadGiroZ, Time.deltaTime * aceleracionGiro);
        }
        else
        {
            // Frena suavemente hacia 0 cuando sueltas el botón
            velocidadGiroActual = Mathf.Lerp(velocidadGiroActual, 0f, Time.deltaTime * desaceleracionGiro);
        }

        // Si todavía tiene algo de velocidad residual, lo seguimos girando
        if (Mathf.Abs(velocidadGiroActual) > 0.1f)
        {
            objetoGiratorio.Rotate(0f, 0f, velocidadGiroActual * Time.deltaTime, Space.Self);
        }
    }

    void ActualizarAnimacionModelo()
    {
        if (modeloArma == null) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        float movX = Input.GetAxis("Horizontal");
        float movY = Input.GetAxis("Vertical");

        float moveX = Mathf.Clamp(mouseX * intensidadSway, -limiteSway, limiteSway);
        float moveY = Mathf.Clamp(mouseY * intensidadSway, -limiteSway, limiteSway);
        Vector3 posicionObjetivo = posicionInicialModelo + new Vector3(-moveX, -moveY, 0f);

        if (Mathf.Abs(movX) > 0.1f || Mathf.Abs(movY) > 0.1f)
        {
            temporizadorBobbing += Time.deltaTime * velocidadBobbing;
            posicionObjetivo.y += Mathf.Sin(temporizadorBobbing) * intensidadBobbing;
            posicionObjetivo.x += Mathf.Cos(temporizadorBobbing * 0.5f) * (intensidadBobbing * 1.5f);
        }
        else
        {
            temporizadorBobbing = 0f;
        }

        float tiltZ = Mathf.Clamp((movX + mouseX) * intensidadInclinacion, -intensidadInclinacion * 2f, intensidadInclinacion * 2f);
        float tiltX = Mathf.Clamp(-mouseY * intensidadInclinacion, -intensidadInclinacion, intensidadInclinacion);
        Quaternion rotacionObjetivo = rotacionInicialModelo * Quaternion.Euler(tiltX, 0f, -tiltZ);

        modeloArma.transform.localPosition = Vector3.Lerp(modeloArma.transform.localPosition, posicionObjetivo, Time.deltaTime * suavidadAnimacion);
        modeloArma.transform.localRotation = Quaternion.Slerp(modeloArma.transform.localRotation, rotacionObjetivo, Time.deltaTime * suavidadAnimacion);
    }

    void Disparar()
    {
        // Comprobamos que el arma está configurada correctamente
        if (prefabProyectil == null || puntoDisparo == null)
        {
            return;
        }

        // Comprobamos que tenemos una cartera
        if (cartera == null)
        {
            Debug.LogWarning("ArmaLanzadora: No se ha encontrado una Cartera.");
            return;
        }

        // Intentamos pagar el disparo
        if (!cartera.GastarMonedas(costePorDisparo))
        {
            Debug.Log("No hay monedas suficientes para disparar.");
            return;
        }

        // -------------------------
        // DISPERSIÓN DEL DISPARO
        // -------------------------
        Vector2 dispersionAleatoria =
            Random.insideUnitCircle * anguloDispersion;

        Quaternion rotacionDispersion =
            Quaternion.Euler(
                dispersionAleatoria.y,
                dispersionAleatoria.x,
                0f
            );

        Vector3 direccionFinal =
            (puntoDisparo.rotation * rotacionDispersion)
            * Vector3.forward;

        Quaternion rotacionFrisbee =
            Quaternion.LookRotation(
                direccionFinal,
                puntoDisparo.up
            );

        // -------------------------
        // CREAR PROYECTIL
        // -------------------------
        GameObject proyectil = Instantiate(
            prefabProyectil,
            puntoDisparo.position,
            rotacionFrisbee
        );

        Moneda moneda = proyectil.GetComponent<Moneda>();

        if (moneda == null)
        {
            moneda = proyectil.AddComponent<Moneda>();
        }

        moneda.valor = costePorDisparo;
        if (proyectil.GetComponent<ProyectilArma>() == null)
        {
            proyectil.AddComponent<ProyectilArma>();
        }
        // -------------------------
        // FÍSICAS DEL PROYECTIL
        // -------------------------
        Rigidbody rb = proyectil.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.AddForce(
                direccionFinal * fuerzaDisparo,
                ForceMode.Impulse
            );

            float fuerzaGiro = Random.Range(-15f, 15f);

            rb.AddRelativeTorque(
                new Vector3(0f, fuerzaGiro, 0f),
                ForceMode.Impulse
            );
        }

        // -------------------------
        // CRECIMIENTO DEL PROYECTIL
        // -------------------------
        CrecimientoProyectil animCrecimiento =
            proyectil.AddComponent<CrecimientoProyectil>();

        animCrecimiento.Configurar(
            escalaMaxima,
            velocidadCrecimiento
        );

        // -------------------------
        // INMUNIDAD AL IMÁN
        // -------------------------
        ProteccionMagnetica animInmunidad =
            proyectil.AddComponent<ProteccionMagnetica>();

        animInmunidad.Configurar(tiempoInmunidad);

        // Destruir proyectil pasado un tiempo
        Destroy(proyectil, vidaProyectil);

        // -------------------------
        // PARTÍCULAS
        // -------------------------
        if (particulasDisparo != null)
        {
            particulasDisparo.Play();
        }

        // -------------------------
        // SONIDO
        // -------------------------
        if (audioSource != null && sonidoDisparo != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(sonidoDisparo, 0.7f);
        }

        // -------------------------
        // RETROCESO VISUAL
        // -------------------------
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