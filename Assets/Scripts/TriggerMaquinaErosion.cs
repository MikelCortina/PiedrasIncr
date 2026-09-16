using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerMaquinaErosion : MonoBehaviour
{
    [Tooltip("Arrastra aquí el objeto principal que tiene el script MaquinaErosion")]
    public MaquinaErosion maquinaPrincipal;

    void OnTriggerEnter(Collider otro)
    {
        // Si detectamos algo, le pasamos la información a la máquina principal
        if (maquinaPrincipal != null)
        {
            maquinaPrincipal.RecibirPiedra(otro);
        }
    }
}