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

    [Tooltip(
        "Permite que cada piedra varíe ligeramente su forma " +
        "al nacer para crear un banco de modelos diversos."
    )]
    public bool usarVariacionAleatoria = true;


    [Range(0f, 0.5f)]
    public float rangoVariacionFuerza = 0.3f;


    [Range(0f, 0.5f)]
    public float rangoVariacionEscala = 0.3f;


    // =====================================================
    // CACHE DE MALLAS
    // =====================================================

    private static readonly Dictionary<string, Mesh>
        cacheMallas =
        new Dictionary<string, Mesh>();


    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        Generar();
    }


    // =====================================================
    // GENERAR
    // =====================================================

    [ContextMenu("¡Generar Piedra Irregular!")]
    public void Generar()
    {
        MeshFilter mf =
            GetComponent<MeshFilter>();


        MeshCollider mc =
            GetComponent<MeshCollider>();


        if (mf.sharedMesh == null)
            return;


        // =================================================
        // VALORES BASE
        // =================================================

        float fuerzaFinal =
            fuerzaDeformacionBase;


        float escalaFinal =
            escalaRuidoBase;


        // =================================================
        // VARIACIÓN
        // =================================================

        if (usarVariacionAleatoria)
        {
            // Unity 6:
            // GetInstanceID() está obsoleto.
            //
            // GetEntityId() es el sustituto actual.
            // Convertimos ese identificador en un int
            // válido para Random.InitState().

            int semillaObjeto =
                gameObject
                    .GetEntityId()
                    .GetHashCode();


            Random.InitState(
                semillaObjeto
            );


            float varFuerza =
                Random.Range(
                    -rangoVariacionFuerza,
                    rangoVariacionFuerza
                );


            float varEscala =
                Random.Range(
                    -rangoVariacionEscala,
                    rangoVariacionEscala
                );


            fuerzaFinal =
                Mathf.Max(
                    0.1f,
                    fuerzaDeformacionBase +
                    varFuerza
                );


            escalaFinal =
                Mathf.Max(
                    0.1f,
                    escalaRuidoBase +
                    varEscala
                );


            // Redondeamos a 1 decimal para que
            // distintas piedras puedan compartir
            // variantes del mismo pool.

            fuerzaFinal =
                Mathf.Round(
                    fuerzaFinal * 10f
                ) / 10f;


            escalaFinal =
                Mathf.Round(
                    escalaFinal * 10f
                ) / 10f;
        }


        // =================================================
        // CLAVE DEL POOL
        // =================================================

        string claveCache =
            $"Piedra_{fuerzaFinal}_{escalaFinal}";


        Mesh mallaFinal;


        // =================================================
        // MALLA YA EXISTENTE
        // =================================================

        if (cacheMallas.TryGetValue(
                claveCache,
                out Mesh mallaCache))
        {
            mallaFinal =
                mallaCache;
        }

        // =================================================
        // GENERAR NUEVA VARIANTE
        // =================================================

        else
        {
            Mesh malla =
                Instantiate(
                    mf.sharedMesh
                );


            Vector3[] vertices =
                malla.vertices;


            // Offset para el ruido Perlin.
            Vector3 semillaRuido =
                new Vector3(
                    Random.value * 100f,
                    Random.value * 100f,
                    Random.value * 100f
                );


            for (int i = 0;
                 i < vertices.Length;
                 i++)
            {
                Vector3 p =
                    vertices[i];


                float ruidoX =
                    Mathf.PerlinNoise(
                        p.x * escalaFinal +
                        semillaRuido.x,

                        p.y * escalaFinal +
                        semillaRuido.y
                    );


                float ruidoY =
                    Mathf.PerlinNoise(
                        p.y * escalaFinal +
                        semillaRuido.y,

                        p.z * escalaFinal +
                        semillaRuido.z
                    );


                float ruidoZ =
                    Mathf.PerlinNoise(
                        p.z * escalaFinal +
                        semillaRuido.z,

                        p.x * escalaFinal +
                        semillaRuido.x
                    );


                float intensidadRuido =
                    (
                        ruidoX +
                        ruidoY +
                        ruidoZ
                    ) / 3f;


                vertices[i] +=
                    p.normalized *
                    (
                        intensidadRuido *
                        fuerzaFinal
                    );
            }


            malla.vertices =
                vertices;


            HacerLowPoly(
                malla
            );


            mallaFinal =
                malla;


            cacheMallas.Add(
                claveCache,
                mallaFinal
            );
        }


        // =================================================
        // APLICAR MALLA
        // =================================================

        mf.sharedMesh =
            mallaFinal;


        mc.sharedMesh =
            mallaFinal;


        mc.convex =
            true;
    }


    // =====================================================
    // LOW POLY
    // =====================================================

    private void HacerLowPoly(
        Mesh malla)
    {
        // Primero calculamos normales suaves.
        malla.RecalculateNormals();


        Vector3[] normalesSuaves =
            malla.normals;


        Vector3[] verticesViejos =
            malla.vertices;


        int[] triangulosViejos =
            malla.triangles;


        Vector3[] verticesNuevos =
            new Vector3[
                triangulosViejos.Length
            ];


        int[] triangulosNuevos =
            new int[
                triangulosViejos.Length
            ];


        List<Vector3> normalesParaFlatKit =
            new List<Vector3>(
                triangulosViejos.Length
            );


        for (int i = 0;
             i < triangulosViejos.Length;
             i++)
        {
            int indiceOriginal =
                triangulosViejos[i];


            verticesNuevos[i] =
                verticesViejos[
                    indiceOriginal
                ];


            triangulosNuevos[i] =
                i;


            normalesParaFlatKit.Add(
                normalesSuaves[
                    indiceOriginal
                ]
            );
        }


        malla.vertices =
            verticesNuevos;


        malla.triangles =
            triangulosNuevos;


        // UV3 / TEXCOORD2 para Flat Kit.
        malla.SetUVs(
            2,
            normalesParaFlatKit
        );


        // Normales reales facetadas.
        malla.RecalculateNormals();

        malla.RecalculateBounds();
    }
}