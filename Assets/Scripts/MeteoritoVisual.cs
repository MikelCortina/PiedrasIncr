using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteoritoVisual : MonoBehaviour
{
    [Header("Referencias Visuales")]
    [Tooltip("El objeto hijo cuyo tamaño se irá reduciendo hasta la mitad")]
    public Transform objetoHijoVisual;

    [Header("Control de Color (Smoke Material)")]
    [Tooltip("El Renderer del objeto principal que tiene asignado el SmokeMaterial")]
    public Renderer rendererSmoke;
    [Tooltip("Un segundo Renderer opcional al que también se le inyectará el color actual")]
    public Renderer rendererSmokeSecundario;
    [Tooltip("Nombre de la propiedad en el Shader Graph (suele ser _FireColor o FireColor)")]
    public string propiedadColorShader = "_FireColor";

    [ColorUsage(true, true)]
    [Tooltip("Color HDR al momento de nacer en el cielo")]
    public Color colorNacimiento = Color.cyan;

    [ColorUsage(true, true)]
    [Tooltip("Color HDR al llegar al suelo")]
    public Color colorImpacto = new Color(1f, 0.2f, 0f, 1f);

    [Header("Estelas (Trail Renderers)")]
    [Tooltip("Lista de Trail Renderers que lleva el meteorito")]
    public List<TrailRenderer> estelasMeteorito = new List<TrailRenderer>();

    [Header("Partículas de Fricción (Incremento al bajar)")]
    [Tooltip("Sistema de partículas principal cuya emisión aumentará al acercarse al suelo")]
    public ParticleSystem particulasFriccion;
    public float emisionMinima = 5f;
    public float emisionMaxima = 50f;

    [Header("Control de Partículas en Impacto")]
    [Tooltip("Lista de sistemas de partículas cuya tasa de emisión bajará a 0 al tocar el suelo")]
    public List<ParticleSystem> particulasParaApagarEnImpacto = new List<ParticleSystem>();

    [Header("Efectos y Destrucción al Impactar")]
    [Tooltip("Sistemas de partículas que se reproducirán una sola vez al chocar con el suelo")]
    public List<ParticleSystem> particulasImpacto = new List<ParticleSystem>();
    [Tooltip("Lista de objetos que se desactivarán al impactar")]
    public List<GameObject> objetosADesactivarAlImpactar = new List<GameObject>();
    [Tooltip("Lista de objetos que se DESTRUIRÁN por completo 0.5 segundos después de impactar")]
    public List<GameObject> objetosADestruirConRetraso = new List<GameObject>();
    [Tooltip("Tag del suelo para detectar la colisión de impacto")]
    public string tagSuelo = "Floor";

    [Header("Efecto de Temblor de Cámara (Shake por Radio)")]
    [Tooltip("Tag que identifica al jugador en la escena")]
    public string tagJugador = "Player";
    [Tooltip("Radio máximo desde el punto de impacto en el que el jugador sentirá el temblor")]
    public float radioImpactoShake = 15f;
    [Tooltip("Intensidad máxima del temblor si el jugador está justo en el epicentro")]
    public float magnitudShakeMax = 0.3f;
    [Tooltip("Duración en segundos que dura el temblor en la cámara")]
    public float duracionShake = 0.25f;

    [Header("Control de Alturas")]
    [Tooltip("Altura Y en el cielo donde nace")]
    public float alturaNacimiento = 100f;
    [Tooltip("Altura Y del suelo donde impacta")]
    public float alturaSuelo = 0f;

    private Vector3 escalaInicialHijo;
    private List<float> tiemposOriginalesEstelas = new List<float>();
    private bool activo = true;

    private Material materialSmokeInstanciado;
    private Material materialSmokeSecundarioInstanciado;

    void Start()
    {
        if (objetoHijoVisual != null)
        {
            escalaInicialHijo = objetoHijoVisual.localScale;
        }

        // Creamos instancias únicas de los materiales para no alterar los assets del proyecto
        if (rendererSmoke != null)
        {
            materialSmokeInstanciado = rendererSmoke.material;
        }

        if (rendererSmokeSecundario != null)
        {
            materialSmokeSecundarioInstanciado = rendererSmokeSecundario.material;
        }

        foreach (TrailRenderer trail in estelasMeteorito)
        {
            if (trail != null)
            {
                tiemposOriginalesEstelas.Add(trail.time);
            }
            else
            {
                tiemposOriginalesEstelas.Add(0f);
            }
        }

        if (particulasFriccion != null && !particulasFriccion.isPlaying)
        {
            particulasFriccion.Play();
        }
    }

    void Update()
    {
        if (!activo) return;

        float posYActual = transform.position.y;

        float progresoCaida = Mathf.InverseLerp(alturaSuelo, alturaNacimiento, posYActual);
        progresoCaida = Mathf.Clamp01(progresoCaida);

        // 1. Reducir escala del hijo visual
        if (objetoHijoVisual != null)
        {
            float multiplicadorEscala = Mathf.Lerp(0.5f, 1f, progresoCaida);
            objetoHijoVisual.localScale = escalaInicialHijo * multiplicadorEscala;
        }

        // 2. Transición del color HDR a los materiales configurados
        if (materialSmokeInstanciado != null || materialSmokeSecundarioInstanciado != null)
        {
            // progresoCaida vale 1 en el cielo y 0 en el suelo
            Color colorActual = Color.Lerp(colorImpacto, colorNacimiento, progresoCaida);

            if (materialSmokeInstanciado != null)
                materialSmokeInstanciado.SetColor(propiedadColorShader, colorActual);

            if (materialSmokeSecundarioInstanciado != null)
                materialSmokeSecundarioInstanciado.SetColor(propiedadColorShader, colorActual);
        }

        // 3. Aumentar emisión de partículas de fricción
        if (particulasFriccion != null)
        {
            var emision = particulasFriccion.emission;
            float factorDescenso = 1f - progresoCaida;
            emision.rateOverTime = Mathf.Lerp(emisionMinima, emisionMaxima, factorDescenso);
        }

        if (posYActual <= alturaSuelo)
        {
            EjecutarImpacto();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!activo) return;

        if (collision.gameObject.CompareTag(tagSuelo))
        {
            EjecutarImpacto();
        }
    }

    void EjecutarImpacto()
    {
        if (!activo) return;
        activo = false; // Evitamos ejecuciones múltiples

        // 1. Fijar escala final a la mitad y forzar el color de impacto
        if (objetoHijoVisual != null)
        {
            objetoHijoVisual.localScale = escalaInicialHijo * 0.5f;
        }

        if (materialSmokeInstanciado != null)
        {
            materialSmokeInstanciado.SetColor(propiedadColorShader, colorImpacto);
        }

        if (materialSmokeSecundarioInstanciado != null)
        {
            materialSmokeSecundarioInstanciado.SetColor(propiedadColorShader, colorImpacto);
        }

        // 2. Apagar estelas
        foreach (TrailRenderer trail in estelasMeteorito)
        {
            if (trail != null) trail.time = 0f;
        }

        // 3. Bajar a 0 el rateOverTime de las partículas continuas
        foreach (ParticleSystem ps in particulasParaApagarEnImpacto)
        {
            if (ps != null)
            {
                var emision = ps.emission;
                emision.rateOverTime = 0f;
            }
        }

        if (particulasFriccion != null)
        {
            var emisionFriccion = particulasFriccion.emission;
            emisionFriccion.rateOverTime = 0f;
        }

        // 4. Reproducir los sistemas de partículas de impacto (una vez)
        foreach (ParticleSystem ps in particulasImpacto)
        {
            if (ps != null)
            {
                // Lo soltamos del meteorito para que no herede su destrucción
                ps.transform.SetParent(null);

                // Forzamos su rotación independientemente de cómo cayó el meteorito
                ps.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

                ps.Play();
                Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
            }
        }

        // 5. COMPROBACIÓN DE RADIO Y SHAKE DE CÁMARA
        VerificarShakeJugador();

        // 6. Desactivar objetos inmediatos
        foreach (GameObject obj in objetosADesactivarAlImpactar)
        {
            if (obj != null) obj.SetActive(false);
        }

        // 7. Destrucción con retraso de 1.5 segundos
        StartCoroutine(RutinaDestruccionConRetraso());
    }

    void VerificarShakeJugador()
    {
        GameObject jugador = GameObject.FindWithTag(tagJugador);
        if (jugador != null && CameraShake.Instancia != null)
        {
            float distanciaAlImpacto = Vector3.Distance(transform.position, jugador.transform.position);

            if (distanciaAlImpacto <= radioImpactoShake)
            {
                float factorCercania = 1f - (distanciaAlImpacto / radioImpactoShake);
                float magnitudFinal = magnitudShakeMax * factorCercania;
                CameraShake.Instancia.Temblar(duracionShake, magnitudFinal);
            }
        }
    }

    IEnumerator RutinaDestruccionConRetraso()
    {
        yield return new WaitForSeconds(1.5f);

        foreach (GameObject obj in objetosADestruirConRetraso)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
    }

    public void ConfigurarAlturas(float cielo, float suelo)
    {
        alturaNacimiento = cielo;
        alturaSuelo = suelo;
    }
}