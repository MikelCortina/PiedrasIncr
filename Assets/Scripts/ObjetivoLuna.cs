using UnityEngine;

public class ObjetivoLuna : MonoBehaviour
{
    [Header("Referencias")]
    public GestorFasesLunares gestorFases;

    private void OnCollisionEnter(Collision collision)
    {
        ProyectilArma proyectil =
            collision.gameObject.GetComponent<ProyectilArma>();

        if (proyectil == null)
            return;

        if (gestorFases != null)
        {
            gestorFases.RegistrarImpacto();
        }

        // La moneda ha impactado contra la Luna
        Destroy(collision.gameObject);
    }
}