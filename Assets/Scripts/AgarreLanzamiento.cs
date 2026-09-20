using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AgarreLanzamiento : MonoBehaviour
{
    [Header("Ajustes de Agarre")]
    public float distanciaAgarre = 4f;
    public float radioDeAgarre = 0.5f;
    public float fuerzaLanzamiento = 15f;
    public Transform puntoAgarre;
    public LayerMask capaPiedra;

    [Header("Sonidos")]
    public AudioClip sonidoLanzamiento;
    [Range(0f, 1f)] public float volumenLanzamiento = 1f;

    private AudioSource audioSource;
    private Rigidbody objetoSostenido;
    private GestorEquipamiento gestorEquipamiento;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        gestorEquipamiento = FindObjectOfType<GestorEquipamiento>();
    }

    void Update()
    {
        // No permitimos agarrar piedras con una herramienta equipada
        if (gestorEquipamiento != null && !gestorEquipamiento.PuedeUsarManos())
        {
            return;
        }

        if (Input.GetMouseButtonDown(1) && objetoSostenido == null)
        {
            IntentarAgarrar();
        }

        if (Input.GetMouseButtonUp(1) && objetoSostenido != null)
        {
            LanzarObjeto();
        }
    }
    void LateUpdate()
    {
        if (gestorEquipamiento != null && !gestorEquipamiento.PuedeUsarManos())
        {
            return;
        }

        if (Input.GetMouseButton(1) && objetoSostenido != null)
        {
            SostenerObjeto();
        }
    }
    void IntentarAgarrar()
    {
        // --- 1. PRIORIDAD: COMPROBAR SI ESTÁ INSPECCIONANDO UNA ROCA CON EL AIM ASSIST ---
        // Verificamos si el sistema visual está activo y tiene una piedra fijada como objetivo
        if (AimAssistVisual.Instancia != null && AimAssistVisual.Instancia.objetivoActual != null)
        {
            Transform rocaInspeccionada = AimAssistVisual.Instancia.objetivoActual;

            // Comprobamos si está dentro del rango de alcance permitido para agarrar
            float distanciaAlJugador = Vector3.Distance(transform.position, rocaInspeccionada.position);
            if (distanciaAlJugador <= distanciaAgarre)
            {
                Rigidbody rbInspeccionado = rocaInspeccionada.GetComponentInChildren<Rigidbody>();
                if (rbInspeccionado != null)
                {
                    objetoSostenido = rbInspeccionado;
                    objetoSostenido.isKinematic = true;
                    objetoSostenido.interpolation = RigidbodyInterpolation.None;
                    return; // ¡Agarrada con éxito, salimos de la función!
                }
            }
        }

        // --- 2. MÉTODO TRADICIONAL (Por SphereCast si no hay ninguna inspeccionada) ---
        Ray rayo = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        if (Physics.SphereCast(rayo, radioDeAgarre, out RaycastHit golpe, distanciaAgarre, capaPiedra))
        {
            if (golpe.collider.CompareTag("Piedra"))
            {
                Rigidbody rbGolpeado = golpe.collider.GetComponent<Rigidbody>();
                if (rbGolpeado != null)
                {
                    objetoSostenido = rbGolpeado;
                    objetoSostenido.isKinematic = true;
                    objetoSostenido.interpolation = RigidbodyInterpolation.None;
                }
            }
        }
    }

    void SostenerObjeto()
    {
        objetoSostenido.transform.position = Vector3.Lerp(objetoSostenido.transform.position, puntoAgarre.position, 15f * Time.deltaTime);
        objetoSostenido.transform.rotation = Quaternion.Lerp(objetoSostenido.transform.rotation, puntoAgarre.rotation, 15f * Time.deltaTime);
    }

    void LanzarObjeto()
    {
        objetoSostenido.isKinematic = false;
        objetoSostenido.interpolation = RigidbodyInterpolation.Interpolate;

        Vector3 direccionLanzamiento = Camera.main.transform.forward;
        objetoSostenido.AddForce(direccionLanzamiento * fuerzaLanzamiento, ForceMode.Impulse);
        objetoSostenido.AddTorque(Camera.main.transform.right * 5f, ForceMode.Impulse);

        if (sonidoLanzamiento != null)
        {
            audioSource.PlayOneShot(sonidoLanzamiento, volumenLanzamiento);
        }

        objetoSostenido = null;
    }

    // Función pública para que el script de la patada pueda consultar
    public bool EstaSosteniendoPiedra()
    {
        return objetoSostenido != null;
    }
}