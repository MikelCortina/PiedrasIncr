using System.Collections.Generic;
using UnityEngine;

public class HologramaColision : MonoBehaviour
{
    [Header("Capa de Obstáculos")]
    public LayerMask capaObstaculos;

    [HideInInspector] public GameObject rampaAIgnorar = null;

    // Lista de todo lo que estamos tocando físicamente
    private List<Collider> objetosChocando = new List<Collider>();

    public bool HayColision
    {
        get
        {
            // 1. Limpiamos objetos que hayan sido destruidos
            objetosChocando.RemoveAll(col => col == null || !col.gameObject.activeInHierarchy || !col.enabled);

            // 2. Revisamos todo lo que estamos tocando en TIEMPO REAL
            foreach (Collider col in objetosChocando)
            {
                // Si estamos imantados a una rampa, hacemos la vista gorda con ella
                if (rampaAIgnorar != null && (col.transform.root == rampaAIgnorar.transform || col.gameObject == rampaAIgnorar))
                {
                    continue;
                }

                // Si tocamos algo más que NO sea la rampa permitida, encendemos la alarma roja
                return true;
            }

            // Si la lista está vacía (o solo contiene la rampa permitida), podemos construir (verde)
            return false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Metemos en la lista TODO lo de la capa obstáculos que toquemos
        if (((1 << other.gameObject.layer) & capaObstaculos) != 0)
        {
            if (!objetosChocando.Contains(other))
            {
                objetosChocando.Add(other);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Lo sacamos de la lista al salir
        if (objetosChocando.Contains(other))
        {
            objetosChocando.Remove(other);
        }
    }

    void OnDisable()
    {
        objetosChocando.Clear();
        rampaAIgnorar = null;
    }
}