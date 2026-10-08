using UnityEngine;

public class ValidadorClearanceConstruccion : MonoBehaviour
{
    [Header("Clearance")]
    [Tooltip("BoxCollider desactivado que define el volumen que debe quedar libre.")]
    public BoxCollider clearance;

    [Header("Capas que bloquean")]
    [Tooltip("Construcciones, paredes, máquinas, etc. NO incluyas Floor.")]
    public LayerMask capasBloqueo;

    [Header("Ajustes")]
    [Range(0.80f, 1f)]
    public float margenInterior = 0.96f;

    [Header("Debug")]
    public bool mostrarDebug = true;
    public bool debugDetallado = true;

    [HideInInspector]
    public GameObject estructuraAIgnorar;

    public bool HayBloqueo { get; private set; }

    private bool estadoAnterior;
    private bool primeraComprobacion = true;
    private float siguienteDebug = 0f;


    private void Awake()
    {
        if (clearance == null)
        {
            Transform encontrado = transform.Find("Clearance");

            if (encontrado != null)
            {
                clearance = encontrado.GetComponent<BoxCollider>();
            }
        }

        if (mostrarDebug)
        {
            if (clearance != null)
                Debug.Log("[CLEARANCE] Clearance encontrado correctamente: " + clearance.name);
            else
                Debug.LogError("[CLEARANCE] NO se ha encontrado el BoxCollider Clearance.");
        }
    }


    private void Update()
    {
        ComprobarClearance();

        if (mostrarDebug &&
            (primeraComprobacion || HayBloqueo != estadoAnterior))
        {
            Debug.Log(
                HayBloqueo
                    ? "[CLEARANCE] BLOQUEADO"
                    : "[CLEARANCE] LIBRE"
            );

            estadoAnterior = HayBloqueo;
            primeraComprobacion = false;
        }
    }


    public void ComprobarClearance()
    {
        HayBloqueo = false;

        if (clearance == null)
            return;

        // Importante porque el holograma cambia de posición constantemente.
        Physics.SyncTransforms();

        Vector3 centroMundo =
            clearance.transform.TransformPoint(clearance.center);

        Vector3 escala =
            clearance.transform.lossyScale;

        escala = new Vector3(
            Mathf.Abs(escala.x),
            Mathf.Abs(escala.y),
            Mathf.Abs(escala.z)
        );

        Vector3 mitadCaja =
            Vector3.Scale(
                clearance.size * 0.5f,
                escala
            );

        mitadCaja *= margenInterior;

        Collider[] encontrados = Physics.OverlapBox(
            centroMundo,
            mitadCaja,
            clearance.transform.rotation,
            capasBloqueo,
            QueryTriggerInteraction.Collide
        );

        // Debug limitado para no llenar la consola cada frame.
        if (debugDetallado && Time.time >= siguienteDebug)
        {
            siguienteDebug = Time.time + 0.5f;

            Debug.Log(
                "[CLEARANCE DEBUG] Colliders encontrados: " +
                encontrados.Length
            );

            foreach (Collider colDebug in encontrados)
            {
                if (colDebug == null)
                    continue;

                Debug.Log(
                    "[CLEARANCE DEBUG] -> " +
                    colDebug.name +
                    " | Layer: " +
                    LayerMask.LayerToName(colDebug.gameObject.layer) +
                    " | Root: " +
                    colDebug.transform.root.name
                );
            }
        }

        foreach (Collider col in encontrados)
        {
            if (col == null)
                continue;

            // Nuestro holograma nunca bloquea.
            if (col.transform.root == transform.root)
                continue;

            // La pieza exacta a la que hacemos snap se permitirá después.
            if (estructuraAIgnorar != null &&
                col.transform.root == estructuraAIgnorar.transform.root)
            {
                continue;
            }

            HayBloqueo = true;

            if (mostrarDebug)
            {
                Debug.DrawLine(
                    centroMundo,
                    col.bounds.center,
                    Color.red
                );
            }

            return;
        }
    }


    private void OnDrawGizmosSelected()
    {
        if (clearance == null)
            return;

        Gizmos.matrix =
            clearance.transform.localToWorldMatrix;

        Gizmos.color =
            HayBloqueo
                ? Color.red
                : Color.green;

        Gizmos.DrawWireCube(
            clearance.center,
            clearance.size * margenInterior
        );
    }
}