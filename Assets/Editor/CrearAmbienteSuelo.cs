using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Herramientas de ambiente:
// - Quemaduras de los impactos de meteoritos (GestorQuemaduras), con su textura y material.
// - Partículas ambientales globales (polvo y destellos) que acompañan a la cámara.
// - Limpieza de los decals de prueba: los quita de la escena y quita la función Decal de los renderers.
// Todo lo que se crea en la escena admite Undo. Los assets van a Assets/Materials/Ambiente y, si ya existen, se reutilizan.
public static class CrearAmbienteSuelo
{
    private const string CarpetaMateriales = "Assets/Materials/Ambiente";
    private const string RutaTexturaQuemadura = CarpetaMateriales + "/Quemadura.png";
    private const string RutaMaterialQuemadura = CarpetaMateriales + "/Quemadura.mat";
    private const string RutaTexturaParticula = CarpetaMateriales + "/ParticulaSuave.png";
    private const string RutaMaterialPolvo = CarpetaMateriales + "/ParticulaPolvo.mat";
    private const string RutaMaterialDestellos = CarpetaMateriales + "/ParticulaDestello.mat";

    // ==========================================
    // QUEMADURAS DE METEORITOS
    // ==========================================

    [MenuItem("Herramientas/Ambiente/Crear gestor de quemaduras de meteoritos")]
    private static void CrearGestorQuemaduras()
    {
        GestorQuemaduras existente = Object.FindAnyObjectByType<GestorQuemaduras>(FindObjectsInactive.Include);
        if (existente != null)
        {
            if (existente.materialQuemadura == null)
            {
                Undo.RecordObject(existente, "Asignar material de quemadura");
                existente.materialQuemadura = ObtenerMaterialQuemadura();
            }
            Selection.activeGameObject = existente.gameObject;
            Debug.Log("Ya hay un gestor de quemaduras en la escena: se ha seleccionado el objeto.");
            return;
        }

        GameObject objeto = new GameObject("Gestor Quemaduras");
        Undo.RegisterCreatedObjectUndo(objeto, "Crear gestor de quemaduras");
        GestorQuemaduras gestor = objeto.AddComponent<GestorQuemaduras>();
        gestor.materialQuemadura = ObtenerMaterialQuemadura();

        Selection.activeGameObject = objeto;
        EditorSceneManager.MarkSceneDirty(objeto.scene);
    }

    // Transparente sin iluminación: la marca es casi negra y así se ve igual con el sombreado de FlatKit.
    private static Material ObtenerMaterialQuemadura()
    {
        Material existente = AssetDatabase.LoadAssetAtPath<Material>(RutaMaterialQuemadura);
        if (existente != null) return existente;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetTexture("_BaseMap", ObtenerTexturaQuemadura());
        mat.SetColor("_BaseColor", Color.white);
        HacerTransparente(mat, false);

        CrearCarpeta(CarpetaMateriales);
        AssetDatabase.CreateAsset(mat, RutaMaterialQuemadura);
        return mat;
    }

    // Marca oscura radial con el borde deshilachado en rayos.
    private static Texture2D ObtenerTexturaQuemadura()
    {
        Texture2D existente = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexturaQuemadura);
        if (existente != null) return existente;

        const int tamano = 256;
        const int semilla = 37;
        Texture2D textura = new Texture2D(tamano, tamano, TextureFormat.RGBA32, false);
        for (int y = 0; y < tamano; y++)
        {
            for (int x = 0; x < tamano; x++)
            {
                float u = (x + 0.5f) / tamano * 2f - 1f;
                float v = (y + 0.5f) / tamano * 2f - 1f;
                float angulo = Mathf.Atan2(v, u);

                float rayos = Fbm(Mathf.Cos(angulo) * 3f + 10f, Mathf.Sin(angulo) * 3f + 10f, semilla, 3);
                float d = Mathf.Sqrt(u * u + v * v) - (rayos - 0.5f) * 0.5f + (Fbm(u * 4f, v * 4f, semilla + 5) - 0.5f) * 0.2f;
                float alfa = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.85f, d));

                float borde = Mathf.Max(Mathf.Abs(u), Mathf.Abs(v));
                alfa *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.9f, 1f, borde));

                Color color = Color.Lerp(new Color(0.03f, 0.025f, 0.02f), new Color(0.2f, 0.14f, 0.1f), Mathf.InverseLerp(0.1f, 0.8f, d));
                color.a = alfa * 0.92f;
                textura.SetPixel(x, y, color);
            }
        }

        GuardarTextura(textura, RutaTexturaQuemadura);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexturaQuemadura);
    }

    // Ruido fractal en [0, 1].
    private static float Fbm(float x, float y, int semilla, int octavas = 4)
    {
        float suma = 0f, amplitud = 0.5f, total = 0f, frecuencia = 1f;
        for (int i = 0; i < octavas; i++)
        {
            suma += Mathf.PerlinNoise(x * frecuencia + semilla * 7.31f, y * frecuencia + semilla * 3.17f) * amplitud;
            total += amplitud;
            amplitud *= 0.5f;
            frecuencia *= 2f;
        }
        return suma / total;
    }

    // ==========================================
    // LIMPIEZA DE LOS DECALS DE PRUEBA
    // ==========================================

    [MenuItem("Herramientas/Ambiente/Quitar decals (escena y renderers)")]
    private static void QuitarDecals()
    {
        int proyectores = 0;
        foreach (DecalProjector proyector in Object.FindObjectsByType<DecalProjector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (proyector == null) continue;
            Undo.DestroyObjectImmediate(proyector.gameObject);
            proyectores++;
        }

        // Los contenedores que quedaron vacíos.
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t != null && t.name == "Decals Suelo" && t.childCount == 0)
                Undo.DestroyObjectImmediate(t.gameObject);

        int funciones = 0;
        foreach (ScriptableRendererData datos in RenderersEnUso())
        {
            SerializedObject renderer = new SerializedObject(datos);
            SerializedProperty lista = renderer.FindProperty("m_RendererFeatures");
            SerializedProperty mapa = renderer.FindProperty("m_RendererFeatureMap");

            for (int i = lista.arraySize - 1; i >= 0; i--)
            {
                if (!(lista.GetArrayElementAtIndex(i).objectReferenceValue is DecalRendererFeature funcion)) continue;

                // Dos pasos: el primero solo vacía la referencia de un elemento de objeto.
                lista.GetArrayElementAtIndex(i).objectReferenceValue = null;
                lista.DeleteArrayElementAtIndex(i);
                if (i < mapa.arraySize) mapa.DeleteArrayElementAtIndex(i);
                renderer.ApplyModifiedPropertiesWithoutUndo();

                Object.DestroyImmediate(funcion, true);
                funciones++;
            }

            datos.SetDirty();
            EditorUtility.SetDirty(datos);
            AssetDatabase.SaveAssetIfDirty(datos);
        }

        if (proyectores > 0)
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log($"Decals quitados: {proyectores} proyectores de la escena y {funciones} funciones Decal de los renderers.");
    }

    private static HashSet<ScriptableRendererData> RenderersEnUso()
    {
        HashSet<ScriptableRendererData> renderers = new HashSet<ScriptableRendererData>();
        AnadirRenderers(GraphicsSettings.defaultRenderPipeline, renderers);
        for (int i = 0; i < QualitySettings.count; i++)
            AnadirRenderers(QualitySettings.GetRenderPipelineAssetAt(i), renderers);
        return renderers;
    }

    private static void AnadirRenderers(RenderPipelineAsset asset, HashSet<ScriptableRendererData> renderers)
    {
        if (!(asset is UniversalRenderPipelineAsset)) return;

        SerializedProperty lista = new SerializedObject(asset).FindProperty("m_RendererDataList");
        for (int i = 0; i < lista.arraySize; i++)
            if (lista.GetArrayElementAtIndex(i).objectReferenceValue is ScriptableRendererData datos)
                renderers.Add(datos);
    }

    // ==========================================
    // PARTÍCULAS AMBIENTALES
    // ==========================================

    [MenuItem("Herramientas/Ambiente/Crear partículas ambientales")]
    private static void CrearParticulas()
    {
        ParticulasAmbientales existentes = Object.FindAnyObjectByType<ParticulasAmbientales>(FindObjectsInactive.Include);
        if (existentes != null)
        {
            Selection.activeGameObject = existentes.gameObject;
            Debug.Log("Ya hay partículas ambientales en la escena: se ha seleccionado el objeto.");
            return;
        }

        Texture2D textura = ObtenerTexturaParticula();
        Material polvo = ObtenerMaterialParticula(RutaMaterialPolvo, textura, false);
        Material destellos = ObtenerMaterialParticula(RutaMaterialDestellos, textura, true);

        GameObject objeto = new GameObject("Particulas Ambientales");
        Undo.RegisterCreatedObjectUndo(objeto, "Crear partículas ambientales");
        if (Camera.main != null) objeto.transform.position = Camera.main.transform.position;

        ParticulasAmbientales ambiente = objeto.AddComponent<ParticulasAmbientales>();
        ambiente.materialPolvo = polvo;
        ambiente.materialDestellos = destellos;
        ambiente.Configurar();

        Selection.activeGameObject = objeto;
        EditorSceneManager.MarkSceneDirty(objeto.scene);
    }

    // Círculo blanco con borde muy suave.
    private static Texture2D ObtenerTexturaParticula()
    {
        Texture2D existente = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexturaParticula);
        if (existente != null) return existente;

        const int tamano = 64;
        Texture2D textura = new Texture2D(tamano, tamano, TextureFormat.RGBA32, false);
        for (int y = 0; y < tamano; y++)
        {
            for (int x = 0; x < tamano; x++)
            {
                float dx = (x + 0.5f) / tamano * 2f - 1f;
                float dy = (y + 0.5f) / tamano * 2f - 1f;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float alfa = Mathf.Pow(1f - d, 2f);
                textura.SetPixel(x, y, new Color(1f, 1f, 1f, alfa));
            }
        }

        GuardarTextura(textura, RutaTexturaParticula);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(RutaTexturaParticula);
    }

    private static Material ObtenerMaterialParticula(string ruta, Texture2D textura, bool aditivo)
    {
        Material existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (existente != null) return existente;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        mat.SetTexture("_BaseMap", textura);
        mat.SetColor("_BaseColor", Color.white);
        // Mezcla normal para el polvo y aditiva para que los destellos brillen.
        HacerTransparente(mat, aditivo);

        CrearCarpeta(CarpetaMateriales);
        AssetDatabase.CreateAsset(mat, ruta);
        return mat;
    }

    // ==========================================
    // UTILIDADES
    // ==========================================

    private static void HacerTransparente(Material mat, bool aditivo)
    {
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", aditivo ? 2f : 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)(aditivo ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", (float)(aditivo ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        mat.SetFloat("_ZWrite", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.enableInstancing = true;
    }

    private static void GuardarTextura(Texture2D textura, string ruta)
    {
        textura.Apply();
        CrearCarpeta(Path.GetDirectoryName(ruta).Replace('\\', '/'));
        File.WriteAllBytes(ruta, textura.EncodeToPNG());
        Object.DestroyImmediate(textura);
        AssetDatabase.ImportAsset(ruta);

        TextureImporter importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
        importador.alphaIsTransparency = true;
        importador.wrapMode = TextureWrapMode.Clamp;
        importador.SaveAndReimport();
    }

    private static void CrearCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;

        string padre = Path.GetDirectoryName(ruta).Replace('\\', '/');
        CrearCarpeta(padre);
        AssetDatabase.CreateFolder(padre, Path.GetFileName(ruta));
    }
}
