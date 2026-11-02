using System.Collections;
using UnityEngine;

// Tamaño "real" de una piedra y quién la está escalando en cada momento.
// - Las máquinas y animaciones encogen o hacen crecer la piedra respecto a Base, no respecto a su escala actual
//   (que puede estar a medias si aún estaba creciendo y acabaría guardándose como 0).
// - Solo el controlador actual escribe la escala: cuando otro script toma el control (el jugador, otra máquina...),
//   el anterior lo detecta con Controla() y deja de tocarla.
[DisallowMultipleComponent]
public class EscalaPiedra : MonoBehaviour
{
    // Serializados para que una copia (Instantiate) de una piedra conserve su tamaño real.
    [SerializeField, HideInInspector] private Vector3 escalaBase;
    [SerializeField, HideInInspector] private bool baseFijada;

    private Object controlador;
    private Coroutine rutinaCrecer;

    public Vector3 Base => baseFijada ? escalaBase : transform.localScale;

    public static EscalaPiedra De(Component componente)
    {
        EscalaPiedra escala = componente.GetComponent<EscalaPiedra>();
        if (escala == null) escala = componente.gameObject.AddComponent<EscalaPiedra>();
        return escala;
    }

    void Awake()
    {
        // Sin base registrada, se toma la escala con la que aparece (si no está ya encogida a 0).
        if (!baseFijada && transform.localScale.sqrMagnitude > 0.000001f)
            FijarBase(transform.localScale);
    }

    void OnDisable()
    {
        // Las corrutinas se paran al desactivarse: si estaba creciendo, se deja a su tamaño real.
        if (rutinaCrecer != null)
        {
            rutinaCrecer = null;
            transform.localScale = Base;
            Soltar(this);
        }
    }

    public void FijarBase(Vector3 escala)
    {
        escalaBase = escala;
        baseFijada = true;
    }

    // Quien llama pasa a ser el único que escala la piedra. Devuelve su tamaño real.
    public Vector3 TomarControl(Object quien)
    {
        DetenerCrecimiento();
        controlador = quien;
        return Base;
    }

    public bool Controla(Object quien) => controlador == quien;

    public void Soltar(Object quien)
    {
        if (controlador == quien) controlador = null;
    }

    // Vuelve a su tamaño real al instante y nadie la sigue escalando.
    public void RestaurarBase()
    {
        DetenerCrecimiento();
        controlador = null;
        transform.localScale = Base;
    }

    // Crece desde 0 hasta su tamaño real en "duracion" segundos. La piedra tiene que estar activa.
    public void Crecer(float duracion)
    {
        TomarControl(this);

        if (!isActiveAndEnabled || duracion <= 0f)
        {
            transform.localScale = Base;
            Soltar(this);
            return;
        }

        rutinaCrecer = StartCoroutine(RutinaCrecer(duracion));
    }

    IEnumerator RutinaCrecer(float duracion)
    {
        Vector3 escalaFinal = Base;
        float tiempo = 0f;
        transform.localScale = Vector3.zero;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, escalaFinal, tiempo / duracion);
            yield return null;
        }

        transform.localScale = escalaFinal;
        rutinaCrecer = null;
        Soltar(this);
    }

    void DetenerCrecimiento()
    {
        if (rutinaCrecer == null) return;

        StopCoroutine(rutinaCrecer);
        rutinaCrecer = null;
    }
}
