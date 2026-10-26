
using UnityEngine;
using UnityEngine.Splines;
using Unity.Cinemachine;

public class SincronizarCamaraIntro : MonoBehaviour
{
    public SplineAnimate movimientoNave;
    public CinemachineSplineDolly dollyCamara;

    void LateUpdate()
    {
        if (movimientoNave == null || dollyCamara == null)
            return;

        float progresoNave = movimientoNave.NormalizedTime;

        // La cámara completa su ruta en los
        // primeros 10 segundos del vuelo de 20.
        dollyCamara.CameraPosition =
            Mathf.Clamp01(progresoNave);
    }
}
