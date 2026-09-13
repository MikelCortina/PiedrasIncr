using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SwordWallCollision : MonoBehaviour
{
    [Header("Capas que se consideran paredes")]
    [SerializeField] private LayerMask capasPared;

    [Header("Clavado")]
    [SerializeField] private bool clavarEnLaPared = true;
    [SerializeField] private bool desactivarGravedadAlClavarse = true;

    [Header("Rebote")]
    [SerializeField] private float multiplicadorRebote = 1f;
    [SerializeField] private float velocidadMinimaRebote = 0.1f;

    [Header("Depuración")]
    [SerializeField] private bool mostrarMensajes = true;

    private Rigidbody rb;
    private bool estaClavada;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (estaClavada)
            return;

        if (!EsUnaPared(collision.gameObject))
            return;

        if (!EncontrarZonaDeImpacto(
                collision,
                out SwordHitZone zonaImpacto,
                out ContactPoint contacto))
        {
            return;
        }

        if (zonaImpacto.SeClava())
        {
            ClavarseEnLaPared(
                collision,
                contacto,
                zonaImpacto
            );
        }
        else if (zonaImpacto.Rebota())
        {
            RebotarContraLaPared(
                collision,
                contacto,
                zonaImpacto
            );
        }
    }

    private bool EsUnaPared(GameObject objeto)
    {
        int mascaraObjeto =
            1 << objeto.layer;

        return (capasPared.value & mascaraObjeto) != 0;
    }

    private bool EncontrarZonaDeImpacto(
        Collision collision,
        out SwordHitZone zonaEncontrada,
        out ContactPoint contactoEncontrado)
    {
        zonaEncontrada = null;
        contactoEncontrado = default;

        SwordHitZone zonaCaraOMango = null;
        ContactPoint contactoCaraOMango = default;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contacto =
                collision.GetContact(i);

            SwordHitZone zona =
                contacto.thisCollider
                    .GetComponentInParent<SwordHitZone>();

            if (zona == null)
                continue;

            // Se da prioridad al filo o a la punta.
            if (zona.SeClava())
            {
                zonaEncontrada = zona;
                contactoEncontrado = contacto;
                return true;
            }

            if (zona.Rebota() &&
                zonaCaraOMango == null)
            {
                zonaCaraOMango = zona;
                contactoCaraOMango = contacto;
            }
        }

        if (zonaCaraOMango != null)
        {
            zonaEncontrada = zonaCaraOMango;
            contactoEncontrado = contactoCaraOMango;
            return true;
        }

        return false;
    }

    private void ClavarseEnLaPared(
        Collision collision,
        ContactPoint contacto,
        SwordHitZone zona)
    {
        if (!clavarEnLaPared)
            return;

        estaClavada = true;

        // Detenemos completamente el movimiento.
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (desactivarGravedadAlClavarse)
        {
            rb.useGravity = false;
        }

        // Creamos una unión fija en la posición actual.
        FixedJoint union = gameObject.AddComponent<FixedJoint>();

        // Si la pared tiene Rigidbody, queda unido a ella.
        // Si no lo tiene, se queda unido al mundo.
        union.connectedBody = collision.rigidbody;

        union.breakForce = Mathf.Infinity;
        union.breakTorque = Mathf.Infinity;
        union.enableCollision = false;

        if (mostrarMensajes)
        {
            Debug.Log(
                $"La espada se ha clavado con el {zona.Tipo}.",
                this
            );
        }
    }

    private void RebotarContraLaPared(
        Collision collision,
        ContactPoint contacto,
        SwordHitZone zona)
    {
        Vector3 velocidadEnElPunto =
            rb.GetPointVelocity(contacto.point);

        float velocidadContraLaPared =
            ObtenerVelocidadHaciaLaPared(
                velocidadEnElPunto,
                contacto.normal
            );

        if (velocidadContraLaPared <= velocidadMinimaRebote)
            return;

        /*
         * La normal puede apuntar en el sentido incorrecto
         * dependiendo de la orientación de la colisión.
         */
        Vector3 normalRebote =
            ObtenerNormalDeRebote(
                velocidadEnElPunto,
                contacto.normal
            );

        /*
         * El impulso depende de:
         *
         * fuerza = masa * velocidad
         *
         * multiplicadorRebote permite ajustar el resultado.
         */
        float impulsoRebote =
            rb.mass *
            velocidadContraLaPared *
            multiplicadorRebote;

        rb.AddForceAtPosition(
            normalRebote * impulsoRebote,
            contacto.point,
            ForceMode.Impulse
        );

        if (mostrarMensajes)
        {
            Debug.Log(
                $"La espada ha rebotado con el {zona.Tipo}. " +
                $"Velocidad de impacto: {velocidadContraLaPared:F2}. " +
                $"Impulso de rebote: {impulsoRebote:F2}.",
                this
            );
        }
    }

    private float ObtenerVelocidadHaciaLaPared(
        Vector3 velocidad,
        Vector3 normal)
    {
        Vector3 normalCorregida =
            ObtenerNormalDeRebote(
                velocidad,
                normal
            );

        return Mathf.Max(
            0f,
            Vector3.Dot(
                velocidad,
                -normalCorregida
            )
        );
    }

    private Vector3 ObtenerNormalDeRebote(
        Vector3 velocidad,
        Vector3 normal)
    {
        /*
         * Queremos que la normal apunte contra el movimiento
         * de la espada.
         */
        if (Vector3.Dot(velocidad, normal) > 0f)
        {
            normal = -normal;
        }

        return normal.normalized;
    }

    public bool EstaClavada()
    {
        return estaClavada;
    }

    public void Desclavar()
    {
        FixedJoint union =
            GetComponent<FixedJoint>();

        if (union != null)
        {
            Destroy(union);
        }

        estaClavada = false;
        rb.isKinematic = false;
        rb.useGravity = true;
    }
}