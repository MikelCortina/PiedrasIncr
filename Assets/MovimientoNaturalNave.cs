
using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
[RequireComponent(typeof(SplineAnimate))]
public class MovimientoNaturalNave : MonoBehaviour
{
    [Header("Referencias")]
    public SplineAnimate animacion;
    public SplineContainer spline;

    [Header("Orientacion")]
    public float suavidadGiro = 5f;
    public float correccionMorro = 0f;

    [Header("Inclinacion en curvas")]
    [Range(0f, 30f)]
    public float inclinacionMaxima = 12f;

    [Range(0f, 3f)]
    public float intensidadInclinacion = 1f;

    [Range(0.005f, 0.1f)]
    public float anticipacionCurva = 0.03f;

    private void OnEnable()
    {
        if (animacion == null)
            animacion = GetComponent<SplineAnimate>();

        if (animacion != null)
            animacion.Updated += ActualizarOrientacion;
    }

    private void OnDisable()
    {
        if (animacion != null)
            animacion.Updated -= ActualizarOrientacion;
    }

    private void ActualizarOrientacion(
        Vector3 posicion, Quaternion rotacion)
    {
        if (animacion == null) return;

        if (spline == null)
            spline = animacion.Container;

        if (spline == null) return;

        float t = Mathf.Clamp01(animacion.NormalizedTime);

        Vector3 direccion = spline.EvaluateTangent(t);

        if (direccion.sqrMagnitude < 0.0001f)
            return;

        direccion.Normalize();

        float antes = Mathf.Clamp01(t - anticipacionCurva);
        float despues = Mathf.Clamp01(t + anticipacionCurva);

        Vector3 tangenteAntes = spline.EvaluateTangent(antes);
        Vector3 tangenteDespues = spline.EvaluateTangent(despues);

        tangenteAntes.y = 0f;
        tangenteDespues.y = 0f;

        float giro = 0f;

        if (tangenteAntes.sqrMagnitude > 0.0001f &&
            tangenteDespues.sqrMagnitude > 0.0001f)
        {
            giro = Vector3.SignedAngle(
                tangenteAntes,
                tangenteDespues,
                Vector3.up
            );
        }

        float inclinacion = Mathf.Clamp(
            -giro * intensidadInclinacion,
            -inclinacionMaxima,
            inclinacionMaxima
        );

        Quaternion objetivo =
            Quaternion.LookRotation(direccion, Vector3.up) *
            Quaternion.Euler(0f, correccionMorro, inclinacion);

        float delta = Application.isPlaying
            ? Time.deltaTime
            : 1f / 60f;

        float suavizado =
            1f - Mathf.Exp(-suavidadGiro * delta);

        transform.rotation = objetivo;
    }
}
