using System.Collections.Generic;
using UnityEngine;

public class HologramaColision : MonoBehaviour
{
    [Header("Capa de Obstáculos")]
    public LayerMask capaObstaculos;

    [HideInInspector]
    public GameObject rampaAIgnorar = null;

    private List<Collider> objetosChocando =
        new List<Collider>();

    public bool HayColision
    {
        get
        {
            objetosChocando.RemoveAll(
                col =>
                    col == null ||
                    !col.gameObject.activeInHierarchy ||
                    !col.enabled
            );

            foreach (Collider col in objetosChocando)
            {
                // Permitimos tocar la construcción
                // concreta a la que estamos haciendo snap.
                if (rampaAIgnorar != null &&
                    (col.transform.root ==
                     rampaAIgnorar.transform.root ||
                     col.gameObject == rampaAIgnorar))
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) &
             capaObstaculos.value) != 0)
        {
            if (!objetosChocando.Contains(other))
            {
                objetosChocando.Add(other);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (objetosChocando.Contains(other))
        {
            objetosChocando.Remove(other);
        }
    }

    private void OnDisable()
    {
        objetosChocando.Clear();
        rampaAIgnorar = null;
    }
}