using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RegistroPiedraGlobal : MonoBehaviour
{
    private Rigidbody rb;


    private void Awake()
    {
        rb =
            GetComponent<Rigidbody>();
    }


    private void OnEnable()
    {
        IntentarRegistrar();
    }


    private void Start()
    {
        // Seguridad extra.
        //
        // Cuando empieza la escena, todos los Awake
        // ya deberían haberse ejecutado, así que
        // GestorPiedras ya debería existir.
        IntentarRegistrar();
    }


    private void IntentarRegistrar()
    {
        if (rb == null)
        {
            rb =
                GetComponent<Rigidbody>();
        }


        if (GestorPiedras.Instancia != null)
        {
            GestorPiedras.Instancia.RegistrarPiedra(
                rb
            );
        }
    }


    private void OnDisable()
    {
        if (GestorPiedras.Instancia != null)
        {
            GestorPiedras.Instancia.DesregistrarPiedra(
                rb
            );
        }
    }


    private void OnDestroy()
    {
        if (GestorPiedras.Instancia != null)
        {
            GestorPiedras.Instancia.DesregistrarPiedra(
                rb
            );
        }
    }
}