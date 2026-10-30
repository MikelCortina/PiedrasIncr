using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Optimiza todo lo pintado con los SceneObjectPainter de la escena abierta (plantas, rocas...),
// esté o no todavía en su lista de prefabs:
// - Pasa los valores de VariacionAlbedo (color, sombreado, sombras y contorno) a materiales compartidos
//   y quita el componente. Su MaterialPropertyBlock dejaba cada objeto fuera del SRP Batcher
//   (una llamada de dibujo cara por objeto). Los materiales nuevos tienen la instanciación por GPU activada.
// - Marca los objetos como Batching Static y les añade un LOD Group que los oculta cuando se ven pequeños.
// Los prefabs editables se guardan directamente. Los objetos que son un modelo (FBX) suelto se cambian
// en la propia escena, con Undo. Se puede ejecutar varias veces: lo ya optimizado se deja igual.
public static class OptimizarPlantasPintadas
{
    private const string CarpetaMateriales = "Assets/Materials/Plantas";
    // Altura relativa en pantalla por debajo de la cual el objeto deja de dibujarse (0.015 = 1,5 %).
    private const float AlturaCorteLOD = 0.015f;

    private struct InstanciaPendiente
    {
        public Renderer render;
        public string nombre;
        public Material[] materialesBase;
        public ConfiguracionMaterial[] configuraciones;
    }

    private static readonly Dictionary<string, Material> materialesCreados = new Dictionary<string, Material>();

    [MenuItem("Herramientas/Optimizar plantas pintadas")]
    private static void Optimizar()
    {
        materialesCreados.Clear();

        SceneObjectPainter[] pintores = Object.FindObjectsByType<SceneObjectPainter>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // Prefabs editables: los de las listas de los pintores y los de todo lo que ya está pintado.
        HashSet<GameObject> prefabs = new HashSet<GameObject>();
        List<GameObject> pintados = new List<GameObject>();
        foreach (SceneObjectPainter pintor in pintores)
        {
            if (pintor.prefabsToPaint != null)
                foreach (GameObject prefab in pintor.prefabsToPaint)
                    if (EsPrefabEditable(prefab)) prefabs.Add(prefab);

            foreach (Transform hijo in pintor.transform)
            {
                pintados.Add(hijo.gameObject);
                GameObject origen = OrigenPrefab(hijo.gameObject);
                if (EsPrefabEditable(origen)) prefabs.Add(origen);
            }
        }

        if (pintados.Count == 0 && prefabs.Count == 0)
        {
            EditorUtility.DisplayDialog("Optimizar plantas pintadas", "No hay ningún SceneObjectPainter con objetos pintados en la escena abierta.", "Vale");
            return;
        }

        // 1) Se leen las instancias antes de tocar los prefabs: al quitar VariacionAlbedo del prefab
        //    desaparecen los overrides de cada instancia (p. ej. las que tienen shadowPower a 0).
        List<InstanciaPendiente> pendientes = new List<InstanciaPendiente>();
        foreach (GameObject pintado in pintados)
        {
            GameObject origen = OrigenPrefab(pintado);
            string nombre = origen != null ? origen.name : pintado.name;

            foreach (VariacionAlbedo variacion in pintado.GetComponentsInChildren<VariacionAlbedo>(true))
            {
                Renderer render = variacion.GetComponent<Renderer>();
                pendientes.Add(new InstanciaPendiente
                {
                    render = render,
                    nombre = nombre,
                    materialesBase = render.sharedMaterials,
                    configuraciones = (ConfiguracionMaterial[])variacion.configuraciones.Clone()
                });
            }
        }

        // 2) Prefabs.
        foreach (GameObject prefab in prefabs)
        {
            string ruta = AssetDatabase.GetAssetPath(prefab);
            GameObject contenido = PrefabUtility.LoadPrefabContents(ruta);

            foreach (VariacionAlbedo variacion in contenido.GetComponentsInChildren<VariacionAlbedo>(true))
            {
                Renderer render = variacion.GetComponent<Renderer>();
                render.sharedMaterials = MaterialesHorneados(render.sharedMaterials, variacion.configuraciones, prefab.name);
                Object.DestroyImmediate(variacion);
            }

            HacerEstatico(contenido, false);
            if (contenido.GetComponent<LODGroup>() == null)
                ConfigurarLOD(contenido.AddComponent<LODGroup>(), contenido);

            PrefabUtility.SaveAsPrefabAsset(contenido, ruta);
            PrefabUtility.UnloadPrefabContents(contenido);
        }

        // 3) Instancias: las que tenían valores distintos a su prefab reciben su propio material.
        int instanciasConVariante = 0;
        foreach (InstanciaPendiente pendiente in pendientes)
        {
            if (pendiente.render == null) continue;

            Material[] deseados = MaterialesHorneados(pendiente.materialesBase, pendiente.configuraciones, pendiente.nombre);
            Material[] actuales = pendiente.render.sharedMaterials;

            if (!MismosMateriales(deseados, actuales))
            {
                Undo.RecordObject(pendiente.render, "Optimizar plantas pintadas");
                pendiente.render.sharedMaterials = deseados;
                PrefabUtility.RecordPrefabInstancePropertyModifications(pendiente.render);
                instanciasConVariante++;
            }

            // El bloque de propiedades no se guarda en la escena, pero sigue puesto hasta recargarla.
            pendiente.render.SetPropertyBlock(null);
            for (int i = 0; i < actuales.Length; i++)
                pendiente.render.SetPropertyBlock(null, i);

            // Si el componente se añadió en la propia instancia (o es un modelo suelto), no se fue con el prefab.
            VariacionAlbedo restante = pendiente.render.GetComponent<VariacionAlbedo>();
            if (restante != null) Undo.DestroyObjectImmediate(restante);
        }

        // 4) Lo pintado que no viene de un prefab editable (modelos FBX sueltos) se ajusta en la escena.
        int sueltos = 0;
        foreach (GameObject pintado in pintados)
        {
            if (pintado == null || EsPrefabEditable(OrigenPrefab(pintado))) continue;

            HacerEstatico(pintado, true);
            if (pintado.GetComponent<LODGroup>() == null)
                ConfigurarLOD(Undo.AddComponent<LODGroup>(pintado), pintado);
            sueltos++;
        }

        foreach (SceneObjectPainter pintor in pintores)
            EditorSceneManager.MarkSceneDirty(pintor.gameObject.scene);

        AssetDatabase.SaveAssets();
        Debug.Log($"Objetos pintados optimizados: {prefabs.Count} prefabs, {sueltos} modelos sueltos, {pendientes.Count} VariacionAlbedo quitados ({instanciasConVariante} con material propio), {materialesCreados.Count} materiales nuevos en {CarpetaMateriales}.");
    }

    private static GameObject OrigenPrefab(GameObject objeto)
    {
        GameObject raiz = PrefabUtility.GetOutermostPrefabInstanceRoot(objeto);
        return raiz != null ? PrefabUtility.GetCorrespondingObjectFromSource(raiz) : null;
    }

    // Los modelos (FBX) y los prefabs de paquetes no se pueden guardar como prefab.
    private static bool EsPrefabEditable(GameObject prefab)
    {
        if (prefab == null) return false;
        PrefabAssetType tipo = PrefabUtility.GetPrefabAssetType(prefab);
        return (tipo == PrefabAssetType.Regular || tipo == PrefabAssetType.Variant)
               && AssetDatabase.GetAssetPath(prefab).StartsWith("Assets/");
    }

    private static void HacerEstatico(GameObject raiz, bool conUndo)
    {
        foreach (Transform hijo in raiz.GetComponentsInChildren<Transform>(true))
        {
            StaticEditorFlags banderas = GameObjectUtility.GetStaticEditorFlags(hijo.gameObject);
            if ((banderas & StaticEditorFlags.BatchingStatic) != 0) continue;

            if (conUndo) Undo.RecordObject(hijo.gameObject, "Optimizar plantas pintadas");
            GameObjectUtility.SetStaticEditorFlags(hijo.gameObject, banderas | StaticEditorFlags.BatchingStatic);
        }
    }

    private static void ConfigurarLOD(LODGroup grupo, GameObject raiz)
    {
        grupo.SetLODs(new[] { new LOD(AlturaCorteLOD, raiz.GetComponentsInChildren<Renderer>(true)) });
        grupo.RecalculateBounds();
    }

    private static Material[] MaterialesHorneados(Material[] baseMats, ConfiguracionMaterial[] configuraciones, string nombre)
    {
        Material[] resultado = (Material[])baseMats.Clone();
        if (configuraciones == null) return resultado;

        foreach (ConfiguracionMaterial config in configuraciones)
        {
            if (config.indiceMaterial < 0 || config.indiceMaterial >= resultado.Length || resultado[config.indiceMaterial] == null) continue;
            resultado[config.indiceMaterial] = ObtenerMaterial(resultado[config.indiceMaterial], config, nombre);
        }

        return resultado;
    }

    // Un material por combinación de material base + valores: los objetos iguales comparten material.
    private static Material ObtenerMaterial(Material baseMat, ConfiguracionMaterial config, string nombre)
    {
        string clave = AssetDatabase.GetAssetPath(baseMat) + "|" + JsonUtility.ToJson(config);
        if (materialesCreados.TryGetValue(clave, out Material existente)) return existente;

        Material mat = new Material(baseMat);

        mat.SetColor("_BaseColor", config.nuevoColor);
        if (config.nuevaTextura != null) mat.SetTexture("_BaseMap", config.nuevaTextura);
        mat.SetColor("_ColorDim", config.colorSombreado);
        mat.SetFloat("_SelfShadingSize", config.selfShadingSize);
        mat.SetFloat("_ShadowEdgeSize", config.edgeSize);
        mat.SetFloat("_Flatness", config.localizedShading);

        // El pase de contorno es opaco: un color con alfa 0 se ve negro, no invisible. Se conserva tal cual.
        mat.SetColor("_OutlineColor", config.colorContorno);
        mat.SetFloat("_OutlineWidth", config.grosorContorno);

        if (config.modificarSombras)
        {
            mat.SetFloat("_UnityShadowPower", config.shadowPower);
            mat.SetColor("_UnityShadowColor", config.shadowColor);
            mat.SetFloat("_UnityShadowSharpness", Mathf.Max(1f, config.shadowSharpness));
        }

        mat.enableInstancing = true;

        if (!AssetDatabase.IsValidFolder(CarpetaMateriales))
            AssetDatabase.CreateFolder("Assets/Materials", "Plantas");

        string sufijo = config.modificarSombras && config.shadowPower <= 0f ? "_SinSombras" : "";
        string ruta = AssetDatabase.GenerateUniqueAssetPath($"{CarpetaMateriales}/Planta_{nombre}{sufijo}.mat");
        AssetDatabase.CreateAsset(mat, ruta);

        materialesCreados[clave] = mat;
        return mat;
    }

    private static bool MismosMateriales(Material[] a, Material[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }
}
