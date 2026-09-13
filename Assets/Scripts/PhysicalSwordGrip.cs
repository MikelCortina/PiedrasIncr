using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicalSwordGrip : MonoBehaviour
{
    [Header("Manos físicas")]
    [SerializeField] private Rigidbody manoDerecha;
    [SerializeField] private Rigidbody manoIzquierda;

    [Header("Puntos de agarre de la espada")]
    [SerializeField] private Transform gripDerechoEspada;
    [SerializeField] private Transform gripIzquierdoEspada;

    [Header("Puntos de agarre de las manos")]
    [SerializeField] private Transform gripDerechoMano;
    [SerializeField] private Transform gripIzquierdoMano;

    [Header("Configuración")]
    [SerializeField] private bool conectarAlComenzar = true;
    [SerializeField] private bool ignorarColisionConLasManos = true;

    [Header("Estabilidad")]
    [SerializeField] private bool utilizarProyeccion = true;
    [SerializeField] private float distanciaProyeccion = 0.05f;
    [SerializeField] private float anguloProyeccion = 5f;

    private Rigidbody rbEspada;

    private ConfigurableJoint jointDerecho;
    private ConfigurableJoint jointIzquierdo;

    private void Awake()
    {
        rbEspada = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (conectarAlComenzar)
        {
            ConectarAmbasManos();
        }
    }

    public void ConectarAmbasManos()
    {
        DesconectarAmbasManos();

        if (manoDerecha != null &&
            gripDerechoEspada != null &&
            gripDerechoMano != null)
        {
            jointDerecho =
                CrearJointDeAgarre(
                    manoDerecha,
                    gripDerechoMano,
                    gripDerechoEspada
                );
        }

        if (manoIzquierda != null &&
            gripIzquierdoEspada != null &&
            gripIzquierdoMano != null)
        {
            jointIzquierdo =
                CrearJointDeAgarre(
                    manoIzquierda,
                    gripIzquierdoMano,
                    gripIzquierdoEspada
                );
        }

        if (ignorarColisionConLasManos)
        {
            IgnorarColisionesConLasManos();
        }
    }

    private ConfigurableJoint CrearJointDeAgarre(
        Rigidbody mano,
        Transform gripMano,
        Transform gripEspada)
    {
        ConfigurableJoint joint =
            mano.gameObject.AddComponent<ConfigurableJoint>();

        joint.connectedBody =
            rbEspada;

        joint.autoConfigureConnectedAnchor =
            false;

        // Posición del punto de agarre respecto a la mano.
        joint.anchor =
            mano.transform.InverseTransformPoint(
                gripMano.position
            );

        // Posición del punto de agarre respecto a la espada.
        joint.connectedAnchor =
            rbEspada.transform.InverseTransformPoint(
                gripEspada.position
            );

        // Bloquear el desplazamiento.
        joint.xMotion =
            ConfigurableJointMotion.Locked;

        joint.yMotion =
            ConfigurableJointMotion.Locked;

        joint.zMotion =
            ConfigurableJointMotion.Locked;

        // Bloquear la rotación.
        joint.angularXMotion =
            ConfigurableJointMotion.Locked;

        joint.angularYMotion =
            ConfigurableJointMotion.Locked;

        joint.angularZMotion =
            ConfigurableJointMotion.Locked;

        joint.breakForce =
            Mathf.Infinity;

        joint.breakTorque =
            Mathf.Infinity;

        joint.enableCollision =
            false;

        if (utilizarProyeccion)
        {
            joint.projectionMode =
                JointProjectionMode.PositionAndRotation;

            joint.projectionDistance =
                distanciaProyeccion;

            joint.projectionAngle =
                anguloProyeccion;
        }

        return joint;
    }

    private void IgnorarColisionesConLasManos()
    {
        Collider[] collidersEspada =
            rbEspada.GetComponentsInChildren<Collider>();

        IgnorarColisionesConMano(
            collidersEspada,
            manoDerecha
        );

        IgnorarColisionesConMano(
            collidersEspada,
            manoIzquierda
        );
    }

    private void IgnorarColisionesConMano(
        Collider[] collidersEspada,
        Rigidbody mano)
    {
        if (mano == null)
            return;

        Collider[] collidersMano =
            mano.GetComponentsInChildren<Collider>();

        foreach (Collider colliderEspada in collidersEspada)
        {
            foreach (Collider colliderMano in collidersMano)
            {
                Physics.IgnoreCollision(
                    colliderEspada,
                    colliderMano,
                    true
                );
            }
        }
    }

    public void DesconectarAmbasManos()
    {
        if (jointDerecho != null)
        {
            Destroy(jointDerecho);
            jointDerecho = null;
        }

        if (jointIzquierdo != null)
        {
            Destroy(jointIzquierdo);
            jointIzquierdo = null;
        }
    }

    public bool EstaAgarrada()
    {
        return jointDerecho != null ||
               jointIzquierdo != null;
    }
}