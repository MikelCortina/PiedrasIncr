using UnityEngine;

// Datos de la curva con la que se construyó una rampa (los pone SistemaConstruccion al colocarla curvada).
// Permite saber hacia dónde va la rampa en cada punto: la cinta transportadora empuja siguiendo la curva
// en lugar de en línea recta, que apretaba las piedras contra el lateral.
// La curva desplaza en X (espacio de la rampa): x += curvatura * t², con t = (maxZ - z) / (maxZ - minZ).
public class CurvaturaRampa : MonoBehaviour
{
    public float curvatura;
    public float minZ;
    public float maxZ;

    // Gira una dirección (en mundo) con la tangente de la curva en el punto indicado.
    public Vector3 CurvarDireccion(Vector3 direccionMundo, Vector3 puntoMundo)
    {
        float longitud = maxZ - minZ;
        if (Mathf.Abs(curvatura) < 0.0001f || longitud <= 0.0001f) return direccionMundo;

        float z = transform.InverseTransformPoint(puntoMundo).z;
        float t = Mathf.Clamp01((maxZ - z) / longitud);

        // Misma inclinación que se aplica a los conectores al curvar la rampa.
        float angulo = Mathf.Atan(2f * curvatura * t / longitud) * Mathf.Rad2Deg;
        return Quaternion.AngleAxis(angulo, transform.up) * direccionMundo;
    }
}
