using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RecolectorMagnetico : MonoBehaviour
{
    [Header("Ajustes del Imán")]
    public float radioAtraccion = 5f;
    public float velocidadAtraccion = 15f;
    public float distanciaRecoleccion = 1.5f;

    [Header("Desbloqueo del Imán")]
    [SerializeField] private bool imanDesbloqueado = false;

    public bool ImanDesbloqueado => imanDesbloqueado;

    // =====================================================
    // MEJORA DE ALCANCE
    // =====================================================

    [Header("Mejora - Alcance del Imán")]
    [SerializeField, Range(1, 3)]
    private int nivelAlcance = 1;

    public float alcanceNivel1 = 5f;
    public float alcanceNivel2 = 8f;
    public float alcanceNivel3 = 12f;

    public int NivelAlcance => nivelAlcance;


    [Header("Protección de Creación")]
    [Tooltip("Tiempo mínimo que debe pasar desde que se crea la moneda para poder ser atraída")]
    public float tiempoInmunidadCreacion = 1.5f;

    [Header("Torque Magnético")]
    [Tooltip("Velocidad a la que dan volteretas las monedas mientras vuelan hacia ti")]
    public float velocidadRotacionVuelo = 720f;

    [Header("Filtros")]
    public LayerMask capaRecolectables;
    public string tagRecolectable = "Recolectable";

    [Header("Sonido (Opcional)")]
    public AudioClip sonidoRecoger;
    [Range(0f, 1f)]
    public float volumenSonido = 1f;

    [Header("Seguro de Sonido")]
    [Tooltip("Tiempo mínimo entre sonidos para evitar que estallen los oídos")]
    public float cooldownSonido = 0.05f;

    private float ultimoTiempoSonido = 0f;

    private Cartera cartera;
    private AudioSource audioSource;

    private List<GameObject> objetosAtrayendo =
        new List<GameObject>();


    // =====================================================
    // VALIDACIÓN INSPECTOR
    // =====================================================

    private void OnValidate()
    {
        nivelAlcance = Mathf.Clamp(nivelAlcance, 1, 3);

        AplicarNivelAlcance();
    }


    // =====================================================
    // START
    // =====================================================

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        cartera = GetComponent<Cartera>();

        AplicarNivelAlcance();
    }


    // =====================================================
    // UPDATE
    // =====================================================

    void Update()
    {
        if (!imanDesbloqueado)
        {
            RecogerMonedasEnContacto();
            return;
        }

        Collider[] objetosCercanos =
            Physics.OverlapSphere(
                transform.position,
                radioAtraccion,
                capaRecolectables
            );

        foreach (Collider col in objetosCercanos)
        {
            GameObject objetoReal =
                col.attachedRigidbody != null
                    ? col.attachedRigidbody.gameObject
                    : col.gameObject;

            if (objetoReal.CompareTag(tagRecolectable) &&
                !objetosAtrayendo.Contains(objetoReal))
            {
                EdadMoneda edad =
                    objetoReal.GetComponent<EdadMoneda>();

                if (edad != null &&
                    !edad.HaSuperadoInmunidad(
                        tiempoInmunidadCreacion
                    ))
                {
                    continue;
                }

                PrepararObjetoParaVolar(objetoReal);
            }
        }


        for (int i = objetosAtrayendo.Count - 1; i >= 0; i--)
        {
            GameObject obj = objetosAtrayendo[i];

            if (obj == null)
            {
                objetosAtrayendo.RemoveAt(i);
                continue;
            }

            // MOVER HACIA EL JUGADOR
            obj.transform.position =
                Vector3.MoveTowards(
                    obj.transform.position,
                    transform.position,
                    velocidadAtraccion * Time.deltaTime
                );


            // ROTACIÓN DURANTE EL VUELO
            Vector3 direccionVuelo =
                (transform.position -
                 obj.transform.position).normalized;

            Vector3 ejeGiro =
                Vector3.Cross(
                    Vector3.up,
                    direccionVuelo
                )
                +
                new Vector3(
                    0.1f,
                    0.3f,
                    0.1f
                );

            obj.transform.Rotate(
                ejeGiro.normalized *
                velocidadRotacionVuelo *
                Time.deltaTime,
                Space.World
            );


            // RECOGER
            if (Vector3.Distance(
                    transform.position,
                    obj.transform.position
                ) <= distanciaRecoleccion)
            {
                objetosAtrayendo.RemoveAt(i);

                Recoger(obj);
            }
        }
    }


    // =====================================================
    // PREPARAR OBJETO
    // =====================================================

    void PrepararObjetoParaVolar(GameObject objeto)
    {
        objetosAtrayendo.Add(objeto);

        Rigidbody rb =
            objeto.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        Collider[] colliders =
            objeto.GetComponentsInChildren<Collider>();

        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }
    }


    // =====================================================
    // RECOGER
    // =====================================================

    void Recoger(GameObject objeto)
    {
        Moneda moneda =
            objeto.GetComponent<Moneda>();

        if (moneda != null &&
            cartera != null)
        {
            cartera.AnadirMonedas(
                moneda.valor
            );
        }

        if (sonidoRecoger != null &&
            Time.time >=
            ultimoTiempoSonido +
            cooldownSonido)
        {
            audioSource.pitch =
                Random.Range(
                    0.90f,
                    1.10f
                );

            audioSource.PlayOneShot(
                sonidoRecoger,
                volumenSonido
            );

            ultimoTiempoSonido =
                Time.time;
        }

        Destroy(objeto);
    }


    // =====================================================
    // MEJORA DE ALCANCE
    // =====================================================

    public bool MejorarAlcance()
    {
        if (nivelAlcance >= 3)
        {
            return false;
        }

        nivelAlcance++;

        AplicarNivelAlcance();

        Debug.Log(
            "Imán mejorado a nivel " +
            nivelAlcance +
            " | Radio: " +
            radioAtraccion
        );

        return true;
    }


    private void AplicarNivelAlcance()
    {
        switch (nivelAlcance)
        {
            case 1:

                radioAtraccion =
                    alcanceNivel1;

                break;


            case 2:

                radioAtraccion =
                    alcanceNivel2;

                break;


            case 3:

                radioAtraccion =
                    alcanceNivel3;

                break;
        }
    }


    public bool AlcanceAlMaximo()
    {
        return nivelAlcance >= 3;
    }


    // =====================================================
    // DEBUG VISUAL
    // =====================================================

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            radioAtraccion
        );
    }

    public void DesbloquearIman()
    {
        imanDesbloqueado = true;

        Debug.Log("¡Imán desbloqueado!");
    }

    public bool EstaImanDesbloqueado()
    {
        return imanDesbloqueado;
    }

    private void RecogerMonedasEnContacto()
    {
        Collider[] objetosCercanos =
            Physics.OverlapSphere(
                transform.position,
                distanciaRecoleccion,
                capaRecolectables
            );

        foreach (Collider col in objetosCercanos)
        {
            GameObject objetoReal =
                col.attachedRigidbody != null
                    ? col.attachedRigidbody.gameObject
                    : col.gameObject;

            if (!objetoReal.CompareTag(tagRecolectable))
                continue;

            Recoger(objetoReal);
        }
    }
}