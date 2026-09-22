using System.Collections;
using UnityEngine;

public class LluviaMeteoritos : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El prefab de tu piedra/meteorito (Debe tener GeneradorPiedra, Rigidbody, DeformacionPiedra y MeteoritoVisual)")]
    public GameObject prefabPiedra;

    [Header("Inicio de partida")]
    public bool generarMeteoritoInicial = true;

    [Header("Ajustes de Altura (Personalizables)")]
    [Tooltip("Altura Y en el cielo donde nacen los meteoritos")]
    public float alturaCielo = 100f;

    [Tooltip("Altura Y aproximada del suelo donde impactan")]
    public float alturaSuelo = 0f;

    [Header("Ajustes de Aparición")]
    [Tooltip("Tag de los objetos del suelo donde pueden caer los meteoritos")]
    public string tagSuelo = "Floor";

    [Header("Cadencia")]
    [Tooltip("Segundos entre cada oleada")]
    public float tiempoEntreSpawns = 2f;

    [Min(1)]
    [Tooltip("Cantidad de meteoritos que aparecen en cada oleada")]
    public int cantidadPorOleada = 1;

    [Tooltip("Separación entre los meteoritos de una misma oleada")]
    public float tiempoEntreMeteoritosOleada = 0.15f;

    [Header("Aceleración por Curva (Custom Curve)")]
    [Tooltip("Curva que define la velocidad del meteorito desde el cielo (0) hasta el suelo (1).")]
    public AnimationCurve curvaAceleracion =
        AnimationCurve.Linear(0f, 1f, 1f, 3f);

    [Tooltip("Velocidad máxima pico a la que impactará contra el suelo")]
    public float velocidadMaximaImpacto = 50f;

    [Header("Seguridad de Colisión")]
    [Tooltip("LayerMask para el Raycast de impacto")]
    public LayerMask capaColisionSuelo;

    [Header("Estado")]
    [SerializeField] private bool sistemaActivo = true;

    private Coroutine rutinaLluvia;
    void Start()
    {
        // Siempre lanzamos un único meteorito inicial
        // para poder arrancar la economía.
        if (generarMeteoritoInicial)
        {
            SpawnearMeteorito();
        }

        // La lluvia continua solo empieza si está activa.
        if (sistemaActivo)
        {
            IniciarRutinaLluvia();
        }
    }

    private void IniciarRutinaLluvia()
    {
        if (rutinaLluvia == null)
        {
            rutinaLluvia = StartCoroutine(RutinaLluviaMeteoritos());
        }
    }

    IEnumerator RutinaLluviaMeteoritos()
    {
        while (sistemaActivo)
        {
            yield return new WaitForSeconds(tiempoEntreSpawns);

            for (int i = 0; i < cantidadPorOleada; i++)
            {
                if (!sistemaActivo)
                    break;

                SpawnearMeteorito();

                if (i < cantidadPorOleada - 1)
                {
                    yield return new WaitForSeconds(
                        tiempoEntreMeteoritosOleada
                    );
                }
            }
        }

        rutinaLluvia = null;
    }

    void SpawnearMeteorito()
    {
        if (prefabPiedra == null)
            return;

        // 1. Buscamos los suelos con el Tag "Floor"
        GameObject[] suelos =
            GameObject.FindGameObjectsWithTag(tagSuelo);

        if (suelos.Length == 0)
        {
            Debug.LogWarning(
                "LluviaMeteoritos: No se ha encontrado ningún objeto con el Tag 'Floor'."
            );

            return;
        }

        // 2. Elegimos un suelo al azar
        GameObject sueloElegido =
            suelos[Random.Range(0, suelos.Length)];

        Collider colSuelo =
            sueloElegido.GetComponent<Collider>();

        if (colSuelo == null)
            return;

        Bounds limites = colSuelo.bounds;

        float objetivoX =
            Random.Range(limites.min.x, limites.max.x);

        float objetivoZ =
            Random.Range(limites.min.z, limites.max.z);

        // 3. Posición de spawn
        Vector3 posicionSpawn =
            new Vector3(
                objetivoX,
                alturaCielo,
                objetivoZ
            );

        float alturaImpactoReal =
            colSuelo.bounds.max.y;

        Vector3 puntoImpacto =
            new Vector3(
                objetivoX,
                alturaImpactoReal,
                objetivoZ
            );

        Vector3 vectorTrayectoria =
            (puntoImpacto - posicionSpawn).normalized;

        // 4. Instanciamos meteorito
        GameObject meteorito =
            Instantiate(
                prefabPiedra,
                posicionSpawn,
                Random.rotation
            );

        // 5. Visual
        MeteoritoVisual visualMeteorito =
            meteorito.GetComponent<MeteoritoVisual>();

        if (visualMeteorito != null)
        {
            visualMeteorito.ConfigurarAlturas(
                alturaCielo,
                alturaImpactoReal
            );
        }

        // 6. Generación procedural
        GeneradorPiedra generador =
            meteorito.GetComponent<GeneradorPiedra>();

        if (generador != null)
        {
            generador.Generar();
        }

        // 7. Física
        Rigidbody rb =
            meteorito.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.useGravity = false;

            StartCoroutine(
                RutinaAceleracionYRaycast(
                    rb,
                    vectorTrayectoria,
                    alturaCielo,
                    alturaImpactoReal,
                    meteorito
                )
            );

            rb.AddTorque(
                Random.insideUnitSphere * 40f,
                ForceMode.Impulse
            );
        }
    }

    IEnumerator RutinaAceleracionYRaycast(
        Rigidbody rb,
        Vector3 direccion,
        float yCielo,
        float ySuelo,
        GameObject meteorito)
    {
        while (rb != null && !rb.isKinematic)
        {
            float alturaActual = rb.position.y;

            float progreso =
                1f -
                Mathf.Clamp01(
                    Mathf.InverseLerp(
                        ySuelo,
                        yCielo,
                        alturaActual
                    )
                );

            float multiplicadorCurva =
                curvaAceleracion.Evaluate(progreso);

            float velocidadFotograma =
                velocidadMaximaImpacto *
                multiplicadorCurva;

            float distanciaPaso =
                velocidadFotograma *
                Time.fixedDeltaTime;

            RaycastHit hit;

            if (Physics.Raycast(
                rb.position,
                direccion,
                out hit,
                distanciaPaso + 0.2f))
            {
                if (hit.collider.CompareTag(tagSuelo))
                {
                    rb.MovePosition(hit.point);

                    rb.linearVelocity = Vector3.zero;
                    rb.useGravity = true;

                    MeteoritoVisual visual =
                        meteorito.GetComponent<MeteoritoVisual>();

                    if (visual != null)
                    {
                        visual.ConfigurarAlturas(
                            yCielo,
                            hit.point.y
                        );
                    }

                    yield break;
                }
            }

            rb.linearVelocity =
                direccion * velocidadFotograma;

            if (alturaActual <= ySuelo)
            {
                rb.useGravity = true;
                yield break;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    // =====================================================
    // CONTROL DESDE EL SISTEMA DE FASES
    // =====================================================

    public void ConfigurarLluvia(
        float nuevoTiempoEntreSpawns,
        int nuevaCantidadPorOleada)
    {
        tiempoEntreSpawns =
            Mathf.Max(0.1f, nuevoTiempoEntreSpawns);

        cantidadPorOleada =
            Mathf.Max(1, nuevaCantidadPorOleada);

        Debug.Log(
            "Lluvia actualizada | Tiempo: " +
            tiempoEntreSpawns +
            " | Meteoritos por oleada: " +
            cantidadPorOleada
        );
    }

    public void ActivarLluvia(bool activar)
    {
        if (sistemaActivo == activar)
            return;

        sistemaActivo = activar;

        if (sistemaActivo)
        {
            IniciarRutinaLluvia();
        }
        else
        {
            if (rutinaLluvia != null)
            {
                StopCoroutine(rutinaLluvia);
                rutinaLluvia = null;
            }
        }
    }
}