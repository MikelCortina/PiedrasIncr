
using UnityEngine;
using UnityEngine.Splines;

[DefaultExecutionOrder(500)]
public class OrientarNaveSpline : MonoBehaviour
{
    [Header("Referencias")]
    public SplineAnimate movimientoNave;
    public SplineContainer ruta;
    public Transform frenteNave;

    [Header("Anticipacion de curvas")]
    [Range(0.001f, 0.05f)]
    public float anticipacion = 0.01f;

    void Awake()
    {
        if (movimientoNave == null)
            movimientoNave = GetComponent<SplineAnimate>();

        if (ruta == null && movimientoNave != null)
            ruta = movimientoNave.Container;
    }

    void LateUpdate()
    {
        if (movimientoNave == null ||
            ruta == null ||
            frenteNave == null)
            return;

        float t = Mathf.Clamp01(
            movimientoNave.NormalizedTime
        );

        float tFuturo = Mathf.Clamp01(
            t + anticipacion
        );

        // Posicion actual y punto hacia el que vamos
        Vector3 posicionActual = ruta.EvaluatePosition(t);
        Vector3 posicionFutura = ruta.EvaluatePosition(tFuturo);

        Vector3 direccion =
            posicionFutura - posicionActual;

        // Al llegar al final, usar el tramo anterior
        if (direccion.sqrMagnitude < 0.000001f)
        {
            float tAnterior = Mathf.Clamp01(t - anticipacion);

            direccion = posicionActual -
                        (Vector3)ruta.EvaluatePosition(tAnterior);
        }

        if (direccion.sqrMagnitude < 0.000001f)
            return;

        // Orientar el morro siguiendo la curva
        Quaternion orientacionRuta =
            Quaternion.LookRotation(
                direccion.normalized,
                Vector3.up
            );

        // Corregir el eje delantero del modelo
        Quaternion correccionMorro =
            Quaternion.Inverse(frenteNave.localRotation);

        transform.rotation =
            orientacionRuta * correccionMorro;
    }
}
