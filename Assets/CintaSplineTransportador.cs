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

    public Vector2 direccionTextura =
        new Vector2(0f, -1f);

    public float multiplicadorSincronizacion =
        0.20f;


    private Material materialCinta;
    private Vector2 offsetTextura;


    private readonly Dictionary<Rigidbody, float>
        sleepThresholdOriginal =
        new Dictionary<Rigidbody, float>();


    private void Start()
    {
        if (spline == null)
        {
            spline =
                GetComponent<CintaSplineDinamica>();
        }


        if (animarTextura)
        {
            Renderer renderer =
                GetComponent<Renderer>();


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


    // =====================================================
    // ENTRA EN LA CINTA
    // =====================================================

    private void OnCollisionEnter(
        Collision colision)
    {
        DeformacionPiedra piedra =
            ObtenerPiedra(
                colision.gameObject
            );


        // MUY IMPORTANTE:
        // primero avisamos a la piedra de que está
        // sobre una cinta. Esto la despierta si estaba
        // en isKinematic.
        if (piedra != null)
        {
            piedra.SetSobreCintaTransportadora(
                true
            );
        }


        Rigidbody rb =
            ObtenerRigidbody(
                colision
            );


        if (rb == null)
        {
            return;
        }


        if (!sleepThresholdOriginal.ContainsKey(
                rb))
        {
            sleepThresholdOriginal.Add(
                rb,
                rb.sleepThreshold
            );
        }


        // Impedimos también el Sleep nativo de Unity
        // mientras está encima de la cinta.
        rb.sleepThreshold =
            0f;


        rb.WakeUp();
    }


    // =====================================================
    // PERMANECE EN LA CINTA
    // =====================================================

    private void OnCollisionStay(
        Collision colision)
    {
        DeformacionPiedra piedra =
            ObtenerPiedra(
                colision.gameObject
            );


        // Cada contacto confirma que sigue encima.
        if (piedra != null)
        {
            piedra.SetSobreCintaTransportadora(
                true
            );
        }


        Rigidbody rb =
            ObtenerRigidbody(
                colision
            );


        if (rb == null ||
            spline == null)
        {
            return;
        }


        // Si seguía kinematic por cualquier motivo,
        // damos otra oportunidad a Despertar().
        if (rb.isKinematic)
        {
            if (piedra != null)
            {
                piedra.Despertar();
            }


            if (rb.isKinematic)
            {
                Debug.LogWarning(
                    "[CINTA DEBUG] " +
                    colision.gameObject.name +
                    " sigue KINEMATIC incluso después de Despertar()."
                );

                return;
            }
        }


        rb.WakeUp();


        Vector3 puntoContacto =
            colision.contactCount > 0
            ? colision.GetContact(0).point
            : rb.position;


        Vector3 direccion =
            spline.ObtenerDireccionEnPunto(
                puntoContacto
            );


        if (direccion.sqrMagnitude <
            0.0001f)
        {
            Debug.LogWarning(
                "[CINTA DEBUG] " +
                colision.gameObject.name +
                " | Dirección spline inválida"
            );

            return;
        }


        direccion.Normalize();


        Vector3 velocidadActual =
            rb.linearVelocity;


        float velocidadEnDireccion =
            Vector3.Dot(
                velocidadActual,
                direccion
            );


        float diferenciaVelocidad =
            velocidad -
            velocidadEnDireccion;


        Vector3 fuerzaAplicada =
            direccion *
            diferenciaVelocidad;


        Debug.Log(
            "[CINTA DEBUG] Piedra: " +
            colision.gameObject.name +

            " | Tocando: SI" +

            " | Kinematic: " +
            rb.isKinematic +

            " | Velocidad: " +
            velocidadActual.ToString("F3") +

            " | Dirección: " +
            direccion.ToString("F3") +

            " | Velocidad dirección: " +
            velocidadEnDireccion.ToString("F3") +

            " | Fuerza: " +
            fuerzaAplicada.ToString("F3")
        );


        rb.AddForce(
            fuerzaAplicada,
            ForceMode.VelocityChange
        );
    }


    // =====================================================
    // SALE DE LA CINTA
    // =====================================================

    private void OnCollisionExit(
        Collision colision)
    {
        DeformacionPiedra piedra =
            ObtenerPiedra(
                colision.gameObject
            );


        if (piedra != null)
        {
            piedra.SetSobreCintaTransportadora(
                false
            );
        }


        Rigidbody rb =
            ObtenerRigidbody(
                colision
            );


        if (rb == null)
        {
            return;
        }


        if (sleepThresholdOriginal.TryGetValue(
                rb,
                out float thresholdOriginal))
        {
            rb.sleepThreshold =
                thresholdOriginal;


            sleepThresholdOriginal.Remove(
                rb
            );
        }
    }


    // =====================================================
    // UTILIDADES
    // =====================================================

    private DeformacionPiedra ObtenerPiedra(
        GameObject objeto)
    {
        if (objeto == null)
        {
            return null;
        }


        DeformacionPiedra piedra =
            objeto.GetComponent<DeformacionPiedra>();


        if (piedra == null)
        {
            piedra =
                objeto.GetComponentInParent<
                    DeformacionPiedra
                >();
        }


        return piedra;
    }


    private Rigidbody ObtenerRigidbody(
        Collision colision)
    {
        if (colision == null)
        {
            return null;
        }


        Rigidbody rb =
            colision.rigidbody;


        if (rb == null)
        {
            rb =
                colision.gameObject
                    .GetComponentInParent<Rigidbody>();
        }


        return rb;
    }


    // =====================================================
    // SEGURIDAD SI DESTRUIMOS LA CINTA
    // =====================================================

    private void OnDisable()
    {
        foreach (
            KeyValuePair<Rigidbody, float> par
            in sleepThresholdOriginal)
        {
            Rigidbody rb =
                par.Key;


            if (rb == null)
            {
                continue;
            }


            rb.sleepThreshold =
                par.Value;


            DeformacionPiedra piedra =
                rb.GetComponent<DeformacionPiedra>();


            if (piedra == null)
            {
                piedra =
                    rb.GetComponentInChildren<
                        DeformacionPiedra
                    >();
            }


            if (piedra != null)
            {
                piedra.SetSobreCintaTransportadora(
                    false
                );
            }
        }


        sleepThresholdOriginal.Clear();
    }
}