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
        // No permitimos agarrar piedras con una herramienta equipada.
        if (gestorEquipamiento != null && !gestorEquipamiento.PuedeUsarManos())
            return;

        if (Input.GetMouseButtonDown(1) && objetoSostenido == null)
            IntentarAgarrar();

        if (Input.GetMouseButtonUp(1) && objetoSostenido != null)
            LanzarObjeto();
    }

    void LateUpdate()
    {
        if (gestorEquipamiento != null && !gestorEquipamiento.PuedeUsarManos())
            return;

        if (Input.GetMouseButton(1) && objetoSostenido != null)
            SostenerObjeto();
    }

    void IntentarAgarrar()
    {
        // 1. AIM ASSIST: mantenemos tu sistema existente.
        if (AimAssistVisual.Instancia != null && AimAssistVisual.Instancia.objetivoActual != null)
        {
            Transform rocaInspeccionada = AimAssistVisual.Instancia.objetivoActual;
            float distanciaAlJugador = Vector3.Distance(transform.position, rocaInspeccionada.position);
            if (distanciaAlJugador <= distanciaAgarre)
            {
                Rigidbody rbInspeccionado = rocaInspeccionada.GetComponentInChildren<Rigidbody>();
                if (rbInspeccionado != null)
                {
                    objetoSostenido = rbInspeccionado;
                    PrepararPiedraAgarrada();
                    return;
                }
            }
        }

        // 2. SPHERECAST: también funciona con una piedra flotando (collider activo).
        if (Camera.main == null) return;
        Ray rayo = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        if (Physics.SphereCast(rayo, radioDeAgarre, out RaycastHit golpe, distanciaAgarre, capaPiedra))
        {
            if (golpe.collider.CompareTag("Piedra"))
            {
                Rigidbody rbGolpeado = golpe.collider.GetComponent<Rigidbody>();
                if (rbGolpeado == null) rbGolpeado = golpe.collider.attachedRigidbody;
                if (rbGolpeado != null)
                {
                    objetoSostenido = rbGolpeado;
                    PrepararPiedraAgarrada();
                }
            }
        }
    }

    void PrepararPiedraAgarrada()
    {
        if (objetoSostenido == null) return;

        // NUEVO: avisar al rescate ANTES de tomar control del Rigidbody.
        RescateVacioPiedra rescate = objetoSostenido.GetComponent<RescateVacioPiedra>();
        if (rescate != null) rescate.NotificarRecogida();

        objetoSostenido.isKinematic = true;
        objetoSostenido.interpolation = RigidbodyInterpolation.None;
        objetoSostenido.detectCollisions = false;
    }

    void SostenerObjeto()
    {
        if (objetoSostenido == null || puntoAgarre == null) return;
        objetoSostenido.transform.position = Vector3.Lerp(
            objetoSostenido.transform.position, puntoAgarre.position, 15f * Time.deltaTime);
        objetoSostenido.transform.rotation = Quaternion.Lerp(
            objetoSostenido.transform.rotation, puntoAgarre.rotation, 15f * Time.deltaTime);
    }

    void LanzarObjeto()
    {
        if (objetoSostenido == null) return;

        objetoSostenido.detectCollisions = true;
        objetoSostenido.isKinematic = false;
        objetoSostenido.interpolation = RigidbodyInterpolation.Interpolate;

        // NUEVO: el rescate deja de controlar la piedra y vuelve a detectar caídas.
        RescateVacioPiedra rescate = objetoSostenido.GetComponent<RescateVacioPiedra>();
        if (rescate != null) rescate.NotificarLiberacion();

        if (Camera.main != null)
        {
            Vector3 direccionLanzamiento = Camera.main.transform.forward;
            objetoSostenido.AddForce(direccionLanzamiento * fuerzaLanzamiento, ForceMode.Impulse);
            objetoSostenido.AddTorque(Camera.main.transform.right * 5f, ForceMode.Impulse);
        }

        if (sonidoLanzamiento != null)
            audioSource.PlayOneShot(sonidoLanzamiento, volumenLanzamiento);

        objetoSostenido = null;
    }

    public bool EstaSosteniendoPiedra()
    {
        return objetoSostenido != null;
    }

    public bool EstaSosteniendoPiedra(Rigidbody piedra)
    {
        return objetoSostenido != null && objetoSostenido == piedra;
    }
}
