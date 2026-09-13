using UnityEngine;

public class DetectorAgujero : MonoBehaviour
{
    [Tooltip("Arrastra aquí el objeto principal que tiene el script AgujeroSimple")]
    public AgujeroSimple agujeroPrincipal;

    private void OnTriggerEnter(Collider otro)
    {
        if (agujeroPrincipal == null) return;

        // 1. Si es una piedra, la cobramos
        if (otro.CompareTag("Piedra"))
        {
            agujeroPrincipal.RecibirPiedra(otro.gameObject);
        }
        // 2. Si es una moneda que ha vuelto a caer, la escupimos
        else if (otro.CompareTag("Coin"))
        {
            // Usamos attachedRigidbody por si el collider de la moneda estuviera en un hijo
            GameObject objetoMoneda = otro.attachedRigidbody != null ? otro.attachedRigidbody.gameObject : otro.gameObject;
            agujeroPrincipal.RebotarMoneda(objetoMoneda);
        }
    }
}