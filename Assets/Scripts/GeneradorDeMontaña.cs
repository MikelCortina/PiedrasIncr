using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GeneradorMontana : MonoBehaviour
{
    [Header("Configuración del Generador")]
    public GameObject prefabPiedra;
    public int cantidadPiedras = 100;
    public float radioDeSpawn = 5f;
    public float alturaMinima = 2f;
    public float alturaMaxima = 30f; // Súbela un poco para que tengan más espacio para caer

    [ContextMenu("1. GENERAR MONTAÑA")]
    public void GenerarPiedras()
    {
        if (prefabPiedra == null) return;

        LimpiarPiedras();

        // Calculamos cuánto espacio vertical le toca a cada piedra para que no se pisen (Anti-Explosiones)
        float espaciadoVertical = (alturaMaxima - alturaMinima) / cantidadPiedras;

        for (int i = 0; i < cantidadPiedras; i++)
        {
            Vector2 puntoEnCirculo = Random.insideUnitCircle * radioDeSpawn;

            // Asignamos una altura escalonada a cada piedra
            float alturaActual = alturaMinima + (i * espaciadoVertical);

            Vector3 posicionAparicion = transform.position + new Vector3(puntoEnCirculo.x, alturaActual, puntoEnCirculo.y);
            Quaternion rotacionAleatoria = Random.rotation;

#if UNITY_EDITOR
            GameObject nuevaPiedra = (GameObject)PrefabUtility.InstantiatePrefab(prefabPiedra);
            nuevaPiedra.transform.position = posicionAparicion;
            nuevaPiedra.transform.rotation = rotacionAleatoria;
            nuevaPiedra.transform.parent = this.transform;

            // ¡MAGIA! Forzamos la deformación EN EL EDITOR justo al crearla
            GeneradorPiedra scriptPiedra = nuevaPiedra.GetComponent<GeneradorPiedra>();
            if (scriptPiedra != null)
            {
                scriptPiedra.Generar();
            }
#endif
        }

        Debug.Log("Montaña generada y pre-calculada. ¡Lista para jugar sin lag!");
    }

    [ContextMenu("2. LIMPIAR MONTAÑA")]
    public void LimpiarPiedras()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * alturaMinima, radioDeSpawn);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * alturaMaxima, radioDeSpawn);
        Gizmos.DrawLine(transform.position + Vector3.up * alturaMinima, transform.position + Vector3.up * alturaMaxima);
    }
}