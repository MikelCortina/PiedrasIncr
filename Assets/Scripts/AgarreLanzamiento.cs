using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AgarreLanzamiento : MonoBehaviour
{
    [Header("Ajustes de Agarre (Piedras)")]
    public float distanciaAgarre = 4f;
    public float radioDeAgarre = 0.5f;
    public float fuerzaLanzamiento = 15f;
    [Tooltip("Punto donde flotará la piedra normal")]
    public Transform puntoAgarre;

    public LayerMask capaInteraccion;

    [Header("Ajustes Especiales: Misil")]
    [Tooltip("Punto de agarre exclusivo para el misil (para que no te tape la cara). Si lo dejas vacío, usará el normal.")]
    public Transform puntoAgarreMisil;
    [Tooltip("Velocidad a la que el misil girará sobre su propio eje Y de forma constante mientras lo tengas agarrado")]
    public float velocidadRotacionMisilY = 180f;

    [Header("Sonidos")]
    public AudioClip sonidoLanzamiento;
    [Range(0f, 1f)] public float volumenLanzamiento = 1f;

    private AudioSource audioSource;
    private Rigidbody objetoSostenido;
    private GestorEquipamiento gestorEquipamiento;

    // Variables de estado del misil
    private bool estaEnZonaCargaMisil = false;
    private CargadorMisil cargadorActual = null;
    private float anguloGiroMisil = 0f;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        gestorEquipamiento = FindObjectOfType<GestorEquipamiento>();
    }

    void Update()
    {
        if (gestorEquipamiento != null && !gestorEquipamiento.PuedeUsarManos())
            return;

        if (Input.GetMouseButtonDown(1) && objetoSostenido == null)
            IntentarAgarrar();

        if (Input.GetMouseButtonUp(1) && objetoSostenido != null)
        {
            if (objetoSostenido.CompareTag("Misil"))
            {
                // Comprobamos si está en la zona Y si la máquina tiene las puertas abiertas
                if (estaEnZonaCargaMisil && cargadorActual != null && cargadorActual.PuedeRecibirMisil())
                {
                    CargarMisilEnMaquina();
                }
                // Si la puerta está cerrada, ignoramos el soltar click (sigue agarrado)
            }
            else
            {
                LanzarObjeto();
            }
        }
    }

    void LateUpdate()
    {
        if (gestorEquipamiento != null && !gestorEquipamiento.PuedeUsarManos())
            return;

        if (objetoSostenido != null)
        {
            bool forzarAgarreMisil = objetoSostenido.CompareTag("Misil") && !Input.GetMouseButton(1);
            if (Input.GetMouseButton(1) || forzarAgarreMisil)
            {
                SostenerObjeto();
            }
        }
    }

    void IntentarAgarrar()
    {
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
                    PrepararObjetoAgarrado();
                    return;
                }
            }
        }

        if (Camera.main == null) return;
        Ray rayo = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));

        if (Physics.SphereCast(rayo, radioDeAgarre, out RaycastHit golpe, distanciaAgarre, capaInteraccion))
        {
            if (golpe.collider.CompareTag("Piedra") || golpe.collider.CompareTag("Misil"))
            {
                Rigidbody rbGolpeado = golpe.collider.GetComponent<Rigidbody>();
                if (rbGolpeado == null) rbGolpeado = golpe.collider.attachedRigidbody;

                if (rbGolpeado != null)
                {
                    objetoSostenido = rbGolpeado;
                    PrepararObjetoAgarrado();
                }
            }
        }
    }

    void PrepararObjetoAgarrado()
    {
        if (objetoSostenido == null) return;

        RescateVacioPiedra rescate = objetoSostenido.GetComponent<RescateVacioPiedra>();
        if (rescate != null) rescate.NotificarRecogida();

        objetoSostenido.isKinematic = true;
        objetoSostenido.interpolation = RigidbodyInterpolation.None;
        objetoSostenido.detectCollisions = false;

        // Reiniciamos el ángulo de giro al recogerlo para que no dé un salto brusco
        anguloGiroMisil = 0f;
    }

    void SostenerObjeto()
    {
        if (objetoSostenido == null) return;

        // Detectamos si es un misil y elegimos el punto de agarre correspondiente
        bool esMisil = objetoSostenido.CompareTag("Misil");
        Transform destinoAgarre = (esMisil && puntoAgarreMisil != null) ? puntoAgarreMisil : puntoAgarre;

        if (destinoAgarre == null) return;

        // 1. Interpolación de la Posición
        objetoSostenido.transform.position = Vector3.Lerp(
            objetoSostenido.transform.position, destinoAgarre.position, 15f * Time.deltaTime);

        // 2. Interpolación de la Rotación
        if (esMisil)
        {
            // Sumamos el giro en función de la velocidad configurable (mantenido entre 0 y 360 grados)
            anguloGiroMisil = (anguloGiroMisil + velocidadRotacionMisilY * Time.deltaTime) % 360f;

            // Calculamos la rotación base del punto de agarre + nuestro giro local en Y
            Quaternion rotacionDeseada = destinoAgarre.rotation * Quaternion.Euler(0f, anguloGiroMisil, 0f);

            objetoSostenido.transform.rotation = Quaternion.Lerp(
                objetoSostenido.transform.rotation, rotacionDeseada, 15f * Time.deltaTime);
        }
        else
        {
            // Rotación normal para la piedra (se queda quieta respecto a la cámara)
            objetoSostenido.transform.rotation = Quaternion.Lerp(
                objetoSostenido.transform.rotation, destinoAgarre.rotation, 15f * Time.deltaTime);
        }
    }

    void LanzarObjeto()
    {
        if (objetoSostenido == null) return;

        objetoSostenido.detectCollisions = true;
        objetoSostenido.isKinematic = false;
        objetoSostenido.interpolation = RigidbodyInterpolation.Interpolate;

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

    public void EntrarZonaCarga(CargadorMisil cargador)
    {
        estaEnZonaCargaMisil = true;
        cargadorActual = cargador;
    }

    public void SalirZonaCarga()
    {
        estaEnZonaCargaMisil = false;
        cargadorActual = null;
    }

    private void CargarMisilEnMaquina()
    {
        cargadorActual.RecibirMisil(objetoSostenido.transform);

        objetoSostenido = null;
        estaEnZonaCargaMisil = false;
        cargadorActual = null;
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