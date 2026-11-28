using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Le dice al shader del suelo (StylizedFloorSurface 1) dónde está la boca de cada agujero, para que
/// no se dibuje SOLO ahí: dentro del círculo de la boca y a su altura.
/// Las bocas se sacan solas de los objetos con el material del agujero (shader "HoleShader/HoleURP"):
/// la tapa de arriba del cilindro es la boca. Funciona en el juego y en el editor, sin montar nada.
/// </summary>
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public static class MascaraAgujeros
{
    private const string NombreShaderAgujero = "HoleShader/HoleURP";
    private const int Maximo = 8; // el mismo tamaño que el array del shader

    /// <summary>
    /// Metros por debajo de la boca en los que se recorta el suelo. Si el agujero no se ve hueco, súbelo;
    /// si se ve a través de la plataforma desde abajo, bájalo (tiene que ser menor que su grosor).
    /// </summary>
    public static float toleranciaAltura = 0.5f;

    private static readonly int IdBocas = Shader.PropertyToID("_AgujerosBoca");
    private static readonly int IdNumero = Shader.PropertyToID("_NumAgujeros");
    private static readonly int IdTolerancia = Shader.PropertyToID("_ToleranciaAlturaAgujero");

    private static readonly List<Renderer> agujeros = new List<Renderer>();
    private static readonly Vector4[] bocas = new Vector4[Maximo];
    private static float proximaBusqueda;

#if UNITY_EDITOR
    static MascaraAgujeros()
    {
        EditorApplication.update -= Actualizar;
        EditorApplication.update += Actualizar;
        EditorSceneManagerHooks();
    }

    private static void EditorSceneManagerHooks()
    {
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += (escena, modo) => proximaBusqueda = 0f;
        EditorApplication.hierarchyChanged += () => proximaBusqueda = 0f;
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AlEmpezar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        proximaBusqueda = 0f;

        GameObject go = new GameObject("MascaraAgujeros") { hideFlags = HideFlags.HideAndDontSave };
        Object.DontDestroyOnLoad(go);
        go.AddComponent<Actualizador>();
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo) => proximaBusqueda = 0f;

    /// <summary>Recalcula las bocas y se las pasa al shader del suelo.</summary>
    public static void Actualizar()
    {
        float ahora = Application.isPlaying ? Time.unscaledTime : (float)TiempoEditor();
        if (ahora >= proximaBusqueda)
        {
            proximaBusqueda = ahora + 2f;
            Buscar();
        }

        int n = 0;
        foreach (Renderer r in agujeros)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy || n >= Maximo) continue;
            Bounds b = r.bounds;
            float radio = Mathf.Min(b.extents.x, b.extents.z);
            bocas[n++] = new Vector4(b.center.x, b.max.y, b.center.z, radio);
        }
        for (int i = n; i < Maximo; i++) bocas[i] = Vector4.zero;

        Shader.SetGlobalVectorArray(IdBocas, bocas);
        Shader.SetGlobalFloat(IdNumero, n);
        Shader.SetGlobalFloat(IdTolerancia, toleranciaAltura);
    }

    private static void Buscar()
    {
        agujeros.Clear();
        foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m != null && m.shader != null && m.shader.name == NombreShaderAgujero)
                {
                    agujeros.Add(r);
                    break;
                }
            }
        }
    }

    private static double TiempoEditor()
    {
#if UNITY_EDITOR
        return EditorApplication.timeSinceStartup;
#else
        return Time.realtimeSinceStartup;
#endif
    }

    // Actualiza las bocas cada frame en el juego (por si un agujero se mueve o se activa).
    private class Actualizador : MonoBehaviour
    {
        private void LateUpdate() => Actualizar();
    }
}
