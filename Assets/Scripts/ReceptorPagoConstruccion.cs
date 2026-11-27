using UnityEngine;

/// <summary>
/// Pasa a la construcción las monedas que golpean esta pieza (p. ej. las ruinas del generador de meteoritos),
/// para que paguen aunque no entren en la zona de pago. Lo añade ConstruccionDianaBloopMonedas a sus ruinas.
/// </summary>
public class ReceptorPagoConstruccion : MonoBehaviour
{
    public ConstruccionDianaBloopMonedas construccion;

    private void OnTriggerEnter(Collider otro)
    {
        if (construccion != null) construccion.IntentarCobrar(otro);
    }

    private void OnCollisionEnter(Collision choque)
    {
        if (construccion != null) construccion.IntentarCobrar(choque.collider);
    }
}
