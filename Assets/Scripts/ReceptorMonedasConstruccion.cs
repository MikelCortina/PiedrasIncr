using UnityEngine;

/// <summary>
/// Reenvía a ConstruccionConMonedas las monedas que tocan este collider (trigger o sólido).
/// Lo añade ConstruccionConMonedas a sus ruinas y a sus piezas.
/// </summary>
public class ReceptorMonedasConstruccion : MonoBehaviour
{
    public ConstruccionConMonedas construccion;

    private void OnTriggerEnter(Collider otro)
    {
        if (construccion != null) construccion.IntentarCobrar(otro);
    }

    private void OnCollisionEnter(Collision choque)
    {
        if (construccion != null && choque != null) construccion.IntentarCobrar(choque.collider);
    }
}
