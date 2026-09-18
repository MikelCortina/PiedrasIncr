using System.Collections;
using UnityEngine;

public class LluviaMeteoritos : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El prefab de tu piedra/meteorito (Debe tener GeneradorPiedra, Rigidbody, DeformacionPiedra y MeteoritoVisual)")]
    public GameObject prefabPiedra;

    [Header("Ajustes de Altura (Personalizables)")]
    [Tooltip("Altura Y en el cielo donde nacen los meteoritos")]
    public float alturaCielo = 100f;
    [Tooltip("Altura Y aproximada del suelo donde impactan")]
    public float alturaSuelo = 0f;

    [Header("Ajustes de Aparición")]
    [Tooltip("Tag de los objetos del suelo donde pueden caer los meteoritos")]
    public string tagSuelo = "Floor";

    [Header("Cadencia (30 por minuto = 1 cada 2 segundos)")]
    public float tiempoEntreSpawns = 2f;

    [Header("Aceleración por Curva (Custom Curve)")]
    [Tooltip("Curva que define la velocidad del meteorito desde el cielo (0) hasta el suelo (1).")]
    public AnimationCurve curvaAceleracion = AnimationCurve.Linear(0f, 1f, 1f, 3f);
    [Tooltip("Velocidad máxima pico a la que impactará contra el suelo")]
    public float velocidadMaximaImpacto = 50f;

    [Header("Seguridad de Colisión")]
    [Tooltip("LayerMask para el Raycast de impacto (opcional, por defecto detecta todo lo que frene)")]
    public LayerMask capaColisionSuelo;

    private bool sistemaActivo = true;

    void Start()
    {
        StartCoroutine(RutinaLluviaMeteoritos());
    }

    IEnumerator RutinaLluviaMeteoritos()
    {
        while (sistemaActivo)
        {
            yield return new WaitForSeconds(tiempoEntreSpawns);
            SpawnearMeteorito();
        }
    }

    void SpawnearMeteorito()
    {
        if (prefabPiedra == null) return;

        // 1. Buscamos los suelos con el Tag "Floor"
        GameObject[] suelos = GameObject.FindGameObjectsWithTag(tagSuelo);
        if (suelos.Length == 0)
        {
            Debug.LogWarning("¡LluviaMeteoritos: No se ha encontrado ningún objeto con el Tag 'Floor' en la escena!");
            return;
        }

        // 2. Elegimos un suelo al azar y calculamos un punto de impacto exacto
        GameObject sueloElegido = suelos[Random.Range(0, suelos.Length)];
        Collider colSuelo = sueloElegido.GetComponent<Collider>();
        if (colSuelo == null) return;

        Bounds limites = colSuelo.bounds;
        float objetivoX = Random.Range(limites.min.x, limites.max.x);
        float objetivoZ = Random.Range(limites.min.z, limites.max.z);

        // 3. Posición de spawn directa en el cielo
        Vector3 posicionSpawn = new Vector3(objetivoX, alturaCielo, objetivoZ);

        float alturaImpactoReal = colSuelo.bounds.max.y;
        Vector3 puntoImpacto = new Vector3(objetivoX, alturaImpactoReal, objetivoZ);
        Vector3 vectorTrayectoria = (puntoImpacto - posicionSpawn).normalized;

        // 4. Instanciamos el meteorito
        GameObject meteorito = Instantiate(prefabPiedra, posicionSpawn, Random.rotation);

        // 5. Inyectamos y configuramos el script visual del hijo
        MeteoritoVisual visualMeteorito = meteorito.GetComponent<MeteoritoVisual>();
        if (visualMeteorito != null)
        {
            visualMeteorito.ConfigurarAlturas(alturaCielo, alturaImpactoReal);
        }

        // 6. Generación procedural de la piedra
        GeneradorPiedra generador = meteorito.GetComponent<GeneradorPiedra>();
        if (generador != null)
        {
            generador.Generar();
        }

        // 7. Físicas de caída guiada
        Rigidbody rb = meteorito.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Apagamos la gravedad para controlarlo mediante la curva
            rb.useGravity = false;
            StartCoroutine(RutinaAceleracionYRaycast(rb, vectorTrayectoria, alturaCielo, alturaImpactoReal, meteorito));
            rb.AddTorque(Random.insideUnitSphere * 40f, ForceMode.Impulse);
        }
    }

    IEnumerator RutinaAceleracionYRaycast(Rigidbody rb, Vector3 direccion, float yCielo, float ySuelo, GameObject meteorito)
    {
        while (rb != null && !rb.isKinematic)
        {
            float alturaActual = rb.position.y;
            float progreso = 1f - Mathf.Clamp01(Mathf.InverseLerp(ySuelo, yCielo, alturaActual));

            float multiplicadorCurva = curvaAceleracion.Evaluate(progreso);
            float velocidadFotograma = velocidadMaximaImpacto * multiplicadorCurva;
            float distanciaPaso = velocidadFotograma * Time.fixedDeltaTime;

            // --- RAYCAST PREDICTIVO DE SEGURIDAD ---
            RaycastHit hit;
            if (Physics.Raycast(rb.position, direccion, out hit, distanciaPaso + 0.2f))
            {
                if (hit.collider.CompareTag(tagSuelo))
                {
                    // Al impactar, lo situamos en la superficie
                    rb.MovePosition(hit.point);

                    // --- NUEVO: LIBERACIÓN DE FÍSICAS ---
                    // Quitamos la velocidad guiada y devolvemos el control a la gravedad de Unity
                    rb.linearVelocity = Vector3.zero;
                    rb.useGravity = true;

                    MeteoritoVisual visual = meteorito.GetComponent<MeteoritoVisual>();
                    if (visual != null)
                    {
                        visual.ConfigurarAlturas(yCielo, hit.point.y);
                    }

                    yield break; // Detenemos la corrutina de caída, la roca ya es "normal"
                }
            }

            rb.linearVelocity = direccion * velocidadFotograma;

            // Si falla el Raycast y cruza por fuerza bruta
            if (alturaActual <= ySuelo)
            {
                rb.useGravity = true; // Liberamos físicas
                yield break;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    public void ActivarLluvia(bool activar)
    {
        sistemaActivo = activar;
    }
}