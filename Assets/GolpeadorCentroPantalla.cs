using UnityEngine;

public class GolpeadorCentroPantalla : MonoBehaviour
{
    [Header("Controles")]
    public KeyCode teclaGolpe = KeyCode.Mouse0; // Clic izquierdo

    [Header("Configuración del Raycast")]
    public float distanciaMaxima = 100f;
    [Tooltip("Capas con las que puede interactuar")]
    public LayerMask capasInteractuables = ~0; // Todo por defecto

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetKeyDown(teclaGolpe))
        {
            EjecutarGolpeCentro();
        }
    }

    private void EjecutarGolpeCentro()
    {
        if (cam == null) return;

        // Genera un rayo desde el centro matemático del viewport (0.5, 0.5)
        Ray rayo = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, capasInteractuables))
        {
            // Busca si el objeto impactado tiene el componente DianaBloop (o en sus padres)
            DianaBloop diana = hit.collider.GetComponentInParent<DianaBloop>();

            if (diana != null)
            {
                // Pasa el punto exacto de la colisión para ubicar el VFX
                diana.RecibirImpacto(hit.point);
            }
        }
    }
}