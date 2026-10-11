using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CintaSplineTransportador : MonoBehaviour
{
    public CintaSplineDinamica spline;

    public float velocidad =
        5f;

    public bool animarTextura =
        true;

    public Vector2 direccionTextura =
        new Vector2(
            0f,
            -1f
        );

    public float multiplicadorSincronizacion =
        0.20f;


    private Material materialCinta;

    private Vector2 offsetTextura;


    private void Start()
    {
        if (spline == null)
        {
            spline =
                GetComponent<
                    CintaSplineDinamica
                >();
        }


        if (animarTextura)
        {
            Renderer renderer =
                GetComponent<
                    Renderer
                >();


            if (renderer != null)
            {
                materialCinta =
                    renderer.material;
            }
        }
    }


    private void Update()
    {
        if (animarTextura &&
            materialCinta != null)
        {
            offsetTextura +=
                direccionTextura *
                (
                    velocidad *
                    multiplicadorSincronizacion *
                    Time.deltaTime
                );


            materialCinta.mainTextureOffset =
                offsetTextura;
        }
    }


    private void OnCollisionStay(
        Collision colision)
    {
        Rigidbody rb =
            colision.rigidbody;


        if (rb == null ||
            rb.isKinematic ||
            spline == null)
        {
            return;
        }


        DeformacionPiedra piedra =
            rb.GetComponent<
                DeformacionPiedra
            >();


        if (piedra != null)
        {
            piedra.Despertar();
        }


        Vector3 puntoContacto =
            colision.contactCount > 0
            ? colision.GetContact(0).point
            : rb.position;


        Vector3 direccion =
            spline.ObtenerDireccionEnPunto(
                puntoContacto
            );


        rb.MovePosition(
            rb.position +
            direccion *
            velocidad *
            Time.fixedDeltaTime
        );
    }
}
