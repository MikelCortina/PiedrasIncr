using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FinishZone : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si el objeto que entra tiene el controlador del avión, hemos llegado a la meta
        if (other.GetComponentInParent<PaperPlaneController>() != null)
        {
            LevelManager.Instance.EndLevel();
        }
    }
}