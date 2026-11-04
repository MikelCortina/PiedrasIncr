
using UnityEngine;
using UnityEngine.AI;

public class BalanceoCuerpoArana : MonoBehaviour
{
    [Header("Movimiento vertical")]
    [SerializeField] private float alturaBalanceo = 0.025f;
    [SerializeField] private float frecuenciaBalanceo = 2f;
    [SerializeField] private float suavizado = 5f;

    private NavMeshAgent agente;
    private Vector3 posicionOriginal;
    private float intensidad;
    private float fase;

    private void Awake()
    {
        posicionOriginal = transform.localPosition;
        agente = GetComponentInParent<NavMeshAgent>();
    }

    private void LateUpdate()
    {
        float velocidad = 0f;

        if (agente != null &&
            agente.enabled &&
            agente.isOnNavMesh)
        {
            velocidad = agente.velocity.magnitude;
        }

        float intensidadObjetivo =
            Mathf.InverseLerp(0.05f, 1.5f, velocidad);

        intensidad = Mathf.MoveTowards(
            intensidad,
            intensidadObjetivo,
            suavizado * Time.deltaTime
        );

        fase += Time.deltaTime *
                frecuenciaBalanceo *
                Mathf.PI * 2f;

        float desplazamiento =
            Mathf.Sin(fase) *
            alturaBalanceo *
            intensidad;

        transform.localPosition =
            posicionOriginal +
            Vector3.up * desplazamiento;
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
            transform.localPosition = posicionOriginal;
    }
}
