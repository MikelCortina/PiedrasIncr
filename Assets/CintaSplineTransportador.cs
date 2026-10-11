using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CintaSplineTransportador : MonoBehaviour
{
    public CintaSplineDinamica spline;

    [Header("Movimiento")]
    public float velocidad = 5f;

    [Header("Efecto Visual")]
    public bool animarTextura = true;
    public Vector2 direccionTextura = new Vector2(0f, -1f);
    public float multiplicadorSincronizacion = 0.20f;

    [Header("Detección robusta")]
    [Tooltip("Busca activamente piedras encima de la cinta. No depende de OnCollisionStay ni de triggers.")]
    public bool usarDeteccionVolumetrica = true;

    [Tooltip("Separación entre cajas de detección a lo largo de la spline.")]
    [Min(0.20f)]
    public float pasoDeteccion = 0.60f;

    [Tooltip("Altura total del volumen que detecta piedras sobre la cinta.")]
    [Min(0.20f)]
    public float alturaDeteccion = 1.20f;

    [Tooltip("Cuánto subimos el centro de detección respecto a la línea de la spline.")]
    public float elevacionCentroDeteccion = 0.30f;

    private readonly List<Material> materialesAnimados =
        new List<Material>();

    private readonly HashSet<int> rigidbodiesMovidosEstePaso =
        new HashSet<int>();

    private readonly Collider[] bufferDeteccion =
        new Collider[64];

    private int layerPiedra = -1;
    private Vector2 offsetTextura;


    private void Start()
    {
        if (spline == null)
        {
            spline = GetComponent<CintaSplineDinamica>();
        }

        layerPiedra = LayerMask.NameToLayer("Piedra");

        if (layerPiedra < 0)
        {
            Debug.LogError(
                "[CINTA SPLINE V9.5] No existe la layer 'Piedra'. " +
                "La piedra debe estar en esa layer para usar el transporte volumétrico."
            );
        }

        PrepararMaterialesAnimados();
    }


    private void FixedUpdate()
    {
        rigidbodiesMovidosEstePaso.Clear();

        if (usarDeteccionVolumetrica)
        {
            DetectarYTransportarPiedras();
        }
    }


    private void PrepararMaterialesAnimados()
    {
        materialesAnimados.Clear();

        if (!animarTextura)
            return;

        if (spline != null)
        {
            Renderer[] renderersBanda =
                spline.ObtenerRenderersBandaVisual();

            if (renderersBanda != null)
            {
                foreach (Renderer renderer in renderersBanda)
                {
                    if (renderer == null)
                        continue;

                    foreach (Material material in renderer.materials)
                    {
                        if (material != null &&
                            !materialesAnimados.Contains(material))
                        {
                            materialesAnimados.Add(material);
                        }
                    }
                }
            }
        }

        if (materialesAnimados.Count == 0)
        {
            Renderer renderer = GetComponent<Renderer>();

            if (renderer != null)
            {
                foreach (Material material in renderer.materials)
                {
                    if (material != null)
                        materialesAnimados.Add(material);
                }
            }
        }
    }


    private void Update()
    {
        if (!animarTextura ||
            materialesAnimados.Count == 0)
        {
            return;
        }

        offsetTextura +=
            direccionTextura *
            (velocidad * multiplicadorSincronizacion * Time.deltaTime);

        foreach (Material material in materialesAnimados)
        {
            if (material != null)
                material.mainTextureOffset = offsetTextura;
        }
    }


    private void DetectarYTransportarPiedras()
    {
        if (spline == null ||
            layerPiedra < 0)
        {
            return;
        }

        float longitud =
            spline.ObtenerLongitudAproximada();

        if (longitud <= 0.001f)
            return;

        float ancho =
            Mathf.Max(0.30f, spline.ObtenerAnchoActual());

        float paso =
            Mathf.Max(0.20f, pasoDeteccion);

        int muestras =
            Mathf.Max(1, Mathf.CeilToInt(longitud / paso));

        float largoCaja =
            longitud / muestras;

        for (int i = 0; i < muestras; i++)
        {
            float distancia =
                (i + 0.5f) * largoCaja;

            if (!spline.ObtenerPuntoYTangentePorDistancia(
                    distancia,
                    out Vector3 punto,
                    out Vector3 tangente))
            {
                continue;
            }

            Vector3 up =
                Vector3.ProjectOnPlane(Vector3.up, tangente);

            if (up.sqrMagnitude < 0.0001f)
                up = Vector3.up;

            up.Normalize();

            Quaternion rotacion =
                Quaternion.LookRotation(tangente, up);

            Vector3 centro =
                punto + up * elevacionCentroDeteccion;

            Vector3 halfExtents =
                new Vector3(
                    ancho * 0.48f,
                    alturaDeteccion * 0.5f,
                    largoCaja * 0.60f
                );

            int cantidad =
                Physics.OverlapBoxNonAlloc(
                    centro,
                    halfExtents,
                    bufferDeteccion,
                    rotacion,
                    ~0,
                    QueryTriggerInteraction.Ignore
                );

            for (int c = 0; c < cantidad; c++)
            {
                Collider col = bufferDeteccion[c];
                bufferDeteccion[c] = null;

                if (col == null)
                    continue;

                Rigidbody rb = col.attachedRigidbody;

                if (rb == null)
                    rb = col.GetComponentInParent<Rigidbody>();

                if (rb == null ||
                    rb.isKinematic)
                {
                    continue;
                }

                // La captura confirma que las piedras usan la layer "Piedra".
                // Aceptamos que esté puesta en el Rigidbody raíz o en su collider.
                bool esPiedra =
                    rb.gameObject.layer == layerPiedra ||
                    col.gameObject.layer == layerPiedra;

                if (!esPiedra)
                    continue;

                int id = rb.GetInstanceID();

                if (!rigidbodiesMovidosEstePaso.Add(id))
                    continue;

                DeformacionPiedra piedra =
                    rb.GetComponent<DeformacionPiedra>();

                if (piedra == null)
                    piedra = rb.GetComponentInChildren<DeformacionPiedra>();

                if (piedra != null)
                    piedra.Despertar();

                Vector3 direccion =
                    spline.ObtenerDireccionEnPunto(rb.position);

                if (direccion.sqrMagnitude < 0.0001f)
                    continue;

                rb.MovePosition(
                    rb.position +
                    direccion.normalized *
                    velocidad *
                    Time.fixedDeltaTime
                );
            }
        }
    }


    // Fallback por si desactivamos la detección volumétrica desde Inspector.
    private void OnCollisionStay(Collision colision)
    {
        if (usarDeteccionVolumetrica)
            return;

        Rigidbody rb = colision.rigidbody;

        if (rb == null ||
            rb.isKinematic ||
            spline == null)
        {
            return;
        }

        Vector3 puntoContacto =
            colision.contactCount > 0
            ? colision.GetContact(0).point
            : rb.position;

        Vector3 direccion =
            spline.ObtenerDireccionEnPunto(puntoContacto);

        rb.MovePosition(
            rb.position +
            direccion.normalized *
            velocidad *
            Time.fixedDeltaTime
        );
    }
}
