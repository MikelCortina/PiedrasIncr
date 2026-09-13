using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DestroyOnSwordContact : MonoBehaviour
{
    [Header("Destrucción del objeto original")]
    [SerializeField] private float retrasoDestruccion = 0f;

    [Header("Sonido de destrucción")]
    [SerializeField] private AudioClip sonidoDestruccion;

    [Range(0f, 1f)]
    [SerializeField] private float volumenSonidoDestruccion = 1f;

    [Tooltip("Activa el posicionamiento 3D del sonido.")]
    [SerializeField] private bool sonidoEn3D = true;

    [SerializeField] private float distanciaMinimaSonido = 1f;

    [SerializeField] private float distanciaMaximaSonido = 20f;

    [Header("Fragmentos de la explosión")]
    [Tooltip("Añade aquí los prefabs que pueden salir disparados.")]
    [SerializeField]
    private List<GameObject> objetosLanzables = new List<GameObject>();

    [SerializeField] private int numeroDeFragmentos = 8;

    [Header("Fuerza de la explosión")]
    [SerializeField] private float fuerzaMinima = 5f;

    [SerializeField] private float fuerzaMaxima = 15f;

    [SerializeField] private float inclinacionHaciaArriba = 0.5f;

    [SerializeField] private float distanciaInicialAleatoria = 0.1f;

    [Header("Rotación de los fragmentos")]
    [SerializeField] private float torqueMinimo = 2f;

    [SerializeField] private float torqueMaximo = 8f;

    [Header("Desaparición de fragmentos")]
    [SerializeField] private float tiempoHastaDesaparecer = 2f;

    private bool yaFueGolpeado;

    // === SE HA CAMBIADO DE OnCollisionEnter A OnTriggerEnter ===
    private void OnTriggerEnter(Collider other)
    {
        if (yaFueGolpeado)
        {
            return;
        }

        // Buscamos si lo que atravesó el trigger es la espada / avión
        PaperPlaneController avion = other.GetComponentInParent<PaperPlaneController>();

        if (avion == null)
        {
            return;
        }

        yaFueGolpeado = true;

        // Como los triggers no tienen Array de contactos, calculamos el punto más cercano 
        // de este collider al centro del avión para aproximar el lugar del impacto
        Vector3 puntoDeImpacto = other.ClosestPoint(transform.position);

        CrearExplosionDeFragmentos(puntoDeImpacto);

        ReproducirSonidoDestruccion(puntoDeImpacto);

        Destroy(gameObject, retrasoDestruccion);
    }

    private void ReproducirSonidoDestruccion(Vector3 posicion)
    {
        if (sonidoDestruccion == null)
        {
            return;
        }

        if (!sonidoEn3D)
        {
            AudioSource.PlayClipAtPoint(sonidoDestruccion, posicion, volumenSonidoDestruccion);
            return;
        }

        GameObject objetoAudio = new GameObject("SFX_Destruccion");
        objetoAudio.transform.position = posicion;

        AudioSource audioSource = objetoAudio.AddComponent<AudioSource>();

        audioSource.clip = sonidoDestruccion;
        audioSource.volume = volumenSonidoDestruccion;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = distanciaMinimaSonido;
        audioSource.maxDistance = distanciaMaximaSonido;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

        audioSource.Play();

        Destroy(objetoAudio, sonidoDestruccion.length + 0.1f);
    }

    private void CrearExplosionDeFragmentos(Vector3 puntoDeImpacto)
    {
        if (objetosLanzables == null || objetosLanzables.Count == 0)
        {
            Debug.LogWarning("No hay objetos en la lista de fragmentos.", this);
            return;
        }

        int cantidadFragmentos = Mathf.Max(0, numeroDeFragmentos);

        for (int i = 0; i < cantidadFragmentos; i++)
        {
            GameObject prefab = ElegirPrefabAleatorio();

            if (prefab == null)
            {
                continue;
            }

            Vector3 posicionInicial = puntoDeImpacto + Random.insideUnitSphere * distanciaInicialAleatoria;
            Quaternion rotacionInicial = Random.rotation;

            GameObject fragmento = Instantiate(prefab, posicionInicial, rotacionInicial);

            ConfigurarFragmento(fragmento);
        }
    }

    private GameObject ElegirPrefabAleatorio()
    {
        int indiceAleatorio = Random.Range(0, objetosLanzables.Count);
        return objetosLanzables[indiceAleatorio];
    }

    private void ConfigurarFragmento(GameObject fragmento)
    {
        Rigidbody rbFragmento = fragmento.GetComponent<Rigidbody>();

        if (rbFragmento == null)
        {
            rbFragmento = fragmento.AddComponent<Rigidbody>();
        }

        rbFragmento.isKinematic = false;
        rbFragmento.useGravity = true;
        rbFragmento.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Vector3 direccionAleatoria = Random.insideUnitSphere;
        direccionAleatoria += Vector3.up * inclinacionHaciaArriba;

        if (direccionAleatoria.sqrMagnitude < 0.001f)
        {
            direccionAleatoria = Vector3.up;
        }

        direccionAleatoria.Normalize();

        float fuerzaAleatoria = Random.Range(fuerzaMinima, fuerzaMaxima);

        rbFragmento.AddForce(direccionAleatoria * fuerzaAleatoria, ForceMode.Impulse);

        Vector3 torqueAleatorio = Random.insideUnitSphere;
        float intensidadTorque = Random.Range(torqueMinimo, torqueMaximo);

        rbFragmento.AddTorque(torqueAleatorio * intensidadTorque, ForceMode.Impulse);

        // Si existe tu script FragmentoDesaparece se lo añade para destruirlo después
        FragmentoDesaparece fragmentoDesaparece = fragmento.GetComponent<FragmentoDesaparece>();

        if (fragmentoDesaparece == null)
        {
            fragmentoDesaparece = fragmento.AddComponent<FragmentoDesaparece>();
        }

        // Comentado para evitar errores si tu script no tiene este método público exacto.
        // Asegúrate de que el método IniciarDesaparicion exista en tu FragmentoDesaparece.
        fragmentoDesaparece.IniciarDesaparicion(tiempoHastaDesaparecer);
    }
}