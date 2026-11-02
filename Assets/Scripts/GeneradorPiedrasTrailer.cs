using System.Collections.Generic;
using UnityEngine;

// Generador de piedras para grabar el tráiler: mientras "generar" está activo, crea piedras en un punto concreto
// a un ritmo fijo y las lanza con una velocidad concreta. Se activa con la casilla del inspector (también en Play)
// o con una tecla opcional.
public class GeneradorPiedrasTrailer : MonoBehaviour
{
    [Header("Activación")]
    [Tooltip("Mientras esté marcado se generan piedras.")]
    public bool generar = false;
    [Tooltip("Tecla para activar/desactivar en Play (None = solo con la casilla).")]
    public KeyCode teclaAlternar = KeyCode.None;

    [Header("Piedra")]
    public GameObject prefabPiedra;
    [Tooltip("Dónde aparecen y hacia dónde se lanzan (su eje Z azul). Vacío = este objeto.")]
    public Transform puntoGeneracion;
    [Tooltip("Multiplicador aleatorio del tamaño (mín, máx). (1, 1) = tamaño del prefab.")]
    public Vector2 escalaAleatoria = Vector2.one;
    [Tooltip("Segundos en crecer desde 0 al aparecer (0 = aparecen ya a tamaño completo).")]
    [Min(0f)] public float tiempoCrecimiento = 0.2f;

    [Header("Ritmo y velocidad")]
    [Tooltip("Piedras que se generan por segundo.")]
    [Min(0.01f)] public float piedrasPorSegundo = 5f;
    [Tooltip("Velocidad de salida (m/s) en la dirección del punto de generación.")]
    [Min(0f)] public float velocidad = 8f;
    [Tooltip("Desviación aleatoria máxima de la dirección (grados).")]
    [Range(0f, 90f)] public float dispersion = 5f;
    [Tooltip("Radio en el que varía al azar el punto de aparición (0 = siempre el mismo punto).")]
    [Min(0f)] public float radioAparicion = 0f;
    [Tooltip("Giro aleatorio máximo al salir (rad/s).")]
    [Min(0f)] public float giroAleatorio = 3f;

    [Header("Límite")]
    [Tooltip("Piedras generadas vivas como máximo: al pasarse se borra la más antigua (0 = sin límite).")]
    [Min(0)] public int maximoPiedras = 150;

    private readonly Queue<GameObject> generadas = new Queue<GameObject>();
    private float acumulado;

    Transform Punto => puntoGeneracion != null ? puntoGeneracion : transform;

    void Update()
    {
        if (teclaAlternar != KeyCode.None && Input.GetKeyDown(teclaAlternar))
            generar = !generar;

        if (!generar || prefabPiedra == null)
        {
            acumulado = 0f;
            return;
        }

        // Ritmo exacto aunque los frames varíen (o se graben a otra velocidad): se acumula el tiempo.
        acumulado += Time.deltaTime * piedrasPorSegundo;
        while (acumulado >= 1f)
        {
            acumulado -= 1f;
            Generar();
        }
    }

    void Generar()
    {
        Transform punto = Punto;
        Vector3 posicion = punto.position + (radioAparicion > 0f ? Random.insideUnitSphere * radioAparicion : Vector3.zero);
        Vector3 direccion = Quaternion.AngleAxis(Random.Range(0f, dispersion), Random.onUnitSphere) * punto.forward;

        GameObject piedra = Instantiate(prefabPiedra, posicion, Random.rotation);
        piedra.transform.localScale = prefabPiedra.transform.localScale * Random.Range(escalaAleatoria.x, escalaAleatoria.y);

        // El tamaño final queda registrado para que las máquinas que la absorban la escalen bien.
        EscalaPiedra escala = EscalaPiedra.De(piedra.transform);
        escala.FijarBase(piedra.transform.localScale);
        if (tiempoCrecimiento > 0f) escala.Crecer(tiempoCrecimiento);

        Rigidbody rb = piedra.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = direccion * velocidad;
            rb.angularVelocity = Random.insideUnitSphere * giroAleatorio;
        }

        if (maximoPiedras <= 0) return;
        generadas.Enqueue(piedra);
        while (generadas.Count > maximoPiedras)
        {
            GameObject antigua = generadas.Dequeue();
            if (antigua != null) Destroy(antigua);
        }
    }

    void OnDrawGizmosSelected()
    {
        Transform punto = Punto;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(punto.position, Mathf.Max(0.15f, radioAparicion));
        Gizmos.DrawRay(punto.position, punto.forward * Mathf.Max(1f, velocidad * 0.25f));
    }
}
