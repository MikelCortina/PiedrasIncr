using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LimiteBoquillaManguera : MonoBehaviour
{
    [Header("Referencia")]
    public MangueraFisica manguera;

    private Rigidbody rb;
    private ConfigurableJoint union;

    void Start()
    {
        if (manguera == null || manguera.puntoFijo == null)
        {
            Debug.LogError("Falta asignar MangueraSistema.");
            enabled = false;
            return;
        }

        rb = GetComponent<Rigidbody>();

        rb.mass = 5f;
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        float distanciaInicial = Vector3.Distance(
            manguera.puntoFijo.position,
            transform.position
        );

        float longitudMaxima = distanciaInicial * manguera.holgura;

        union = gameObject.AddComponent<ConfigurableJoint>();

        union.connectedBody = null;
        union.autoConfigureConnectedAnchor = false;

        union.anchor = Vector3.zero;
        union.connectedAnchor = manguera.puntoFijo.position;

        // La boquilla puede moverse en todas las direcciones,
        // pero sin superar la longitud de la manguera.
        union.xMotion = ConfigurableJointMotion.Limited;
        union.yMotion = ConfigurableJointMotion.Limited;
        union.zMotion = ConfigurableJointMotion.Limited;

        union.angularXMotion = ConfigurableJointMotion.Free;
        union.angularYMotion = ConfigurableJointMotion.Free;
        union.angularZMotion = ConfigurableJointMotion.Free;

        SoftJointLimit limite = union.linearLimit;
        limite.limit = longitudMaxima;
        union.linearLimit = limite;

        union.projectionMode = JointProjectionMode.PositionAndRotation;
        union.projectionDistance = 0.05f;

        Debug.Log("Longitud máxima de manguera: " + longitudMaxima);
    }

    void FixedUpdate()
    {
        if (union != null && manguera != null && manguera.puntoFijo != null)
        {
            union.connectedAnchor = manguera.puntoFijo.position;
        }
    }
}