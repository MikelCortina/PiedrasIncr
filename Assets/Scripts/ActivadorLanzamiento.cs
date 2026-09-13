using UnityEngine;

public class ActivadorLanzamiento : MonoBehaviour
{
    [SerializeField] private LanzadorEspadaExterno lanzador;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            lanzador.Lanzar();
        }
    }
}