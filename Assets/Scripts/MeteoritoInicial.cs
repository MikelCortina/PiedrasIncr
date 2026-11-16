
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class MeteoritoInicial : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject prefabMeteorito;
    public Transform puntoImpacto;
    public PlayableDirector directorIntro;

    [Header("Trayectoria")]
    public Vector3 desplazamientoInicial = new Vector3(-20f, 65f, -15f);

    [Min(0.1f)]
    public float duracionCaida = 2.5f;

    [Min(0f)]
    public float velocidadGiro = 100f;

    [Header("Estado")]
    [SerializeField]
    private bool meteoritoLanzado;

    private void OnEnable()
    {
        if (directorIntro != null)
            directorIntro.stopped += AlTerminarIntro;
    }

    private void OnDisable()
    {
        if (directorIntro != null)
            directorIntro.stopped -= AlTerminarIntro;
    }

    private void AlTerminarIntro(PlayableDirector director)
    {
        LanzarMeteorito();
    }

    [ContextMenu("Lanzar meteorito inicial")]
    public void LanzarMeteorito()
    {
        if (!Application.isPlaying)
            return;

        if (meteoritoLanzado)
            return;

        if (prefabMeteorito == null || puntoImpacto == null)
        {
            Debug.LogError(
                "MeteoritoInicial: faltan referencias."
            );

            return;
        }

        Vector3 posicionInicial =
            puntoImpacto.position + desplazamientoInicial;

        GameObject meteorito = Instantiate(
            prefabMeteorito,
            posicionInicial,
            Random.rotation
        );

        Rigidbody rb = meteorito.GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError(
                "MeteoritoInicial: el prefab necesita Rigidbody."
            );

            Destroy(meteorito);
            return;
        }

        Collider colision = meteorito.GetComponent<Collider>();

        float alturaPiedra = colision != null
            ? Mathf.Max(0.1f, colision.bounds.extents.y)
            : 0.5f;

        Vector3 destino =
            puntoImpacto.position + Vector3.up * alturaPiedra;

        MeteoritoVisual visual =
            meteorito.GetComponent<MeteoritoVisual>();

        if (visual != null)
        {
            visual.ConfigurarAlturas(
                posicionInicial.y,
                destino.y
            );
        }

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;

        rb.angularVelocity =
            Random.onUnitSphere * velocidadGiro * Mathf.Deg2Rad;

        meteoritoLanzado = true;

        StartCoroutine(
            AnimarCaida(rb, posicionInicial, destino)
        );

        Debug.Log(
            "Meteorito inicial lanzado hacia " + puntoImpacto.position
        );
    }

    private IEnumerator AnimarCaida(
        Rigidbody rb,
        Vector3 origen,
        Vector3 destino)
    {
        float tiempo = 0f;

        while (rb != null && tiempo < duracionCaida)
        {
            tiempo += Time.fixedDeltaTime;

            float progreso = Mathf.Clamp01(
                tiempo / duracionCaida
            );

            float aceleracion = progreso * progreso;

            Vector3 posicion =
                Vector3.Lerp(origen, destino, aceleracion);

            rb.MovePosition(posicion);

            yield return new WaitForFixedUpdate();
        }

        if (rb == null)
            yield break;

        rb.MovePosition(destino);
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = true;
    }
}
