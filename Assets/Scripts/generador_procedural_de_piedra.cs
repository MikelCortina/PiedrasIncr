using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class GeneradorPiedra : MonoBehaviour
{
    [Header("Ajustes de Forma Base")]
    public float fuerzaDeformacionBase = 1.0f;
    public float escalaRuidoBase = 1.2f;

    [Header("Variación Automática (Pool de Variantes)")]
    [Tooltip("Permite que cada piedra varíe ligeramente su forma al nacer para crear un banco de modelos diversos.")]
    public bool usarVariacionAleatoria = true;
    [Range(0f, 0.5f)] public float rangoVariacionFuerza = 0.3f;
    [Range(0f, 0.5f)] public float rangoVariacionEscala = 0.3f;

    // Sistema de caché inteligente para GPU Instancing (Reutiliza mallas similares)
    private static Dictionary<string, Mesh> cacheMallas = new Dictionary<string, Mesh>();

    [ContextMenu("¡Generar Piedra Irregular!")]

    public void Awake()
    {
        Generar();
    }
    public void Generar()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshCollider mc = GetComponent<MeshCollider>();

        if (mf.sharedMesh == null) return;

        // Si el usuario quiere variedad, aplicamos un pequeño offset aleatorio controlado 
        // para redondear a 2 decimales, asegurando que se agrupen en un pool limitado de variantes.
        float fuerzaFinal = fuerzaDeformacionBase;
        float escalaFinal = escalaRuidoBase;

        if (usarVariacionAleatoria)
        {
            // Usamos la posición actual como semilla única para que la variante sea consistente al regenerar
            Random.InitState(gameObject.GetInstanceID());
            float varFuerza = Random.Range(-rangoVariacionFuerza, rangoVariacionFuerza);
            float varEscala = Random.Range(-rangoVariacionEscala, rangoVariacionEscala);

            fuerzaFinal = Mathf.Max(0.1f, fuerzaDeformacionBase + varFuerza);
            escalaFinal = Mathf.Max(0.1f, escalaRuidoBase + varEscala);

            // Redondeamos a 1 decimal para forzar que piedras con valores similares compartan la misma malla maestra (Pool)
            fuerzaFinal = Mathf.Round(fuerzaFinal * 10f) / 10f;
            escalaFinal = Mathf.Round(escalaFinal * 10f) / 10f;
        }

        // Creamos una clave de caché basada en estos valores redondeados
        string claveCache = $"Piedra_{fuerzaFinal}_{escalaFinal}";

        Mesh mallaFinal;

        if (cacheMallas.ContainsKey(claveCache))
        {
            // Si ya existe una malla maestra con esta variante exacta, la reutilizamos (Ahorro brutal de CPU y memoria)
            mallaFinal = cacheMallas[claveCache];
        }
        else
        {
            // Si no existe, la generamos por primera vez y la guardamos en el pool
            Mesh malla = Instantiate(mf.sharedMesh);
            Vector3[] vertices = malla.vertices;
            Vector3 semilla = new Vector3(Random.value * 100f, Random.value * 100f, Random.value * 100f);

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = vertices[i];
                float ruidoX = Mathf.PerlinNoise(p.x * escalaFinal + semilla.x, p.y * escalaFinal + semilla.y);
                float ruidoY = Mathf.PerlinNoise(p.y * escalaFinal + semilla.y, p.z * escalaFinal + semilla.z);
                float ruidoZ = Mathf.PerlinNoise(p.z * escalaFinal + semilla.z, p.x * escalaFinal + semilla.x);

                float intensidadRuido = (ruidoX + ruidoY + ruidoZ) / 3f;
                vertices[i] += p.normalized * (intensidadRuido * fuerzaFinal);
            }

            malla.vertices = vertices;
            HacerLowPoly(malla);
            mallaFinal = malla;

            cacheMallas.Add(claveCache, mallaFinal);
        }

        mf.sharedMesh = mallaFinal;
        mc.sharedMesh = mallaFinal;
        mc.convex = true;
    }

    void HacerLowPoly(Mesh malla)
    {
        Vector3[] verticesViejos = malla.vertices;
        int[] triangulosViejos = malla.triangles;
        Vector3[] verticesNuevos = new Vector3[triangulosViejos.Length];
        int[] triangulosNuevos = new int[triangulosViejos.Length];

        for (int i = 0; i < triangulosViejos.Length; i++)
        {
            verticesNuevos[i] = verticesViejos[triangulosViejos[i]];
            triangulosNuevos[i] = i;
        }

        malla.vertices = verticesNuevos;
        malla.triangles = triangulosNuevos;
        malla.RecalculateNormals();
        malla.RecalculateBounds();
    }
}