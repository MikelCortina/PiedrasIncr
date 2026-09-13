using UnityEngine;
using System.Collections.Generic;

public class HoleHandler : MonoBehaviour
{
    [Header("Configuración de Layers")]
    [Tooltip("Despliega el menú y marca TODAS las capas que pueden caer")]
    public LayerMask normalSphereLayers;

    [Tooltip("Despliega el menú y marca SOLO LA CAPA a la que cambiarán al caer")]
    public LayerMask fallingSphereLayer;

    // Usaremos esto para guardar el número real (int) de la capa de caída
    private int fallingLayerIndex;

    // Memoria para recordar la layer original exacta de cada objeto
    private Dictionary<GameObject, int> memoriaLayers = new Dictionary<GameObject, int>();

    void Start()
    {
        // Seguridad: Convertimos el LayerMask desplegable al número 'int' que Unity requiere.
        // OJO: Asegúrate de marcar solo UNA capa en el inspector para 'fallingSphereLayer'
        if (fallingSphereLayer.value != 0)
        {
            fallingLayerIndex = (int)Mathf.Log(fallingSphereLayer.value, 2);
        }
        else
        {
            Debug.LogError("¡Aviso! No has seleccionado ninguna capa en Falling Sphere Layer.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Magia bitwise: Comprueba si la capa del objeto chocado está marcada dentro de nuestro LayerMask 'normalSphereLayers'
        if ((normalSphereLayers.value & (1 << other.gameObject.layer)) != 0)
        {
            // Guardamos su layer original en la memoria
            if (!memoriaLayers.ContainsKey(other.gameObject))
            {
                memoriaLayers.Add(other.gameObject, other.gameObject.layer);
            }

            // Lo cambiamos a la layer de caída
            other.gameObject.layer = fallingLayerIndex;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Si este objeto está registrado en nuestra memoria...
        if (memoriaLayers.ContainsKey(other.gameObject))
        {
            // Solo se la restauramos si actualmente tiene la layer de caída
            if (other.gameObject.layer == fallingLayerIndex)
            {
                other.gameObject.layer = memoriaLayers[other.gameObject];
            }

            // Lo borramos de la memoria
            memoriaLayers.Remove(other.gameObject);
        }
    }
}