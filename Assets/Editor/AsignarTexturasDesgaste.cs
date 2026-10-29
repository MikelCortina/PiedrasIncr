using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Asigna las texturas de desgaste a los VariacionAlbedo de la escena (almacén, letras STELLAR OIL
// del edificio, base del mostrador y anillo de la tapa). Se hace desde el editor, con Undo, para no
// tocar el archivo de la escena mientras está abierta.
public static class AsignarTexturasDesgaste
{
    private struct Asignacion
    {
        public string objeto;      // nombre del GameObject, o null para no filtrar por nombre
        public string textura;
        public int indiceMaterial;   // -1 = cualquier configuración
        public string malla;         // nombre de la malla del MeshFilter, o null
        public bool reemplazar;      // false = solo rellena las configuraciones sin textura

        public Asignacion(string objeto, string textura, int indiceMaterial, string malla = null, bool reemplazar = false)
        {
            this.objeto = objeto;
            this.textura = textura;
            this.indiceMaterial = indiceMaterial;
            this.malla = malla;
            this.reemplazar = reemplazar;
        }
    }

    private static readonly Asignacion[] asignaciones =
    {
        new Asignacion("Almacen", "Assets/Modelos/Extras/Almacen_Desgaste.png", -1),
        new Asignacion("Text.003", "Assets/Modelos/Gasolinera/Text003_Desgaste.png", -1),
        new Asignacion("Mostrador", "Assets/Modelos/Gasolinera/Mostrador_Base_Desgaste.png", 0),
        // La ruleta de la tapa comparte textura con otros objetos: se le pone una copia desgastada solo a ella.
        new Asignacion(null, "Assets/Modelos/Tapa_Cylinder001_Desgaste.png", -1, "Tapa_Cylinder.001_Smoothed", true),
    };

    [MenuItem("Herramientas/Gasolinera/Asignar texturas de desgaste")]
    private static void Asignar()
    {
        int cambios = 0;

        foreach (Asignacion asignacion in asignaciones)
        {
            Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(asignacion.textura);
            if (textura == null)
            {
                Debug.LogWarning("AsignarTexturasDesgaste: no se encuentra " + asignacion.textura);
                continue;
            }

            foreach (VariacionAlbedo variacion in Object.FindObjectsByType<VariacionAlbedo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (variacion.configuraciones == null)
                    continue;

                if (asignacion.objeto != null && variacion.gameObject.name != asignacion.objeto)
                    continue;

                if (asignacion.malla != null && NombreMalla(variacion) != asignacion.malla)
                    continue;

                Undo.RecordObject(variacion, "Asignar textura de desgaste");
                bool cambiado = false;

                for (int i = 0; i < variacion.configuraciones.Length; i++)
                {
                    ConfiguracionMaterial config = variacion.configuraciones[i];
                    bool indiceValido = asignacion.indiceMaterial < 0 || config.indiceMaterial == asignacion.indiceMaterial;
                    bool libre = config.nuevaTextura == null || asignacion.reemplazar;

                    if (!indiceValido || !libre || config.nuevaTextura == textura)
                        continue;

                    config.nuevaTextura = textura;
                    variacion.configuraciones[i] = config;
                    cambiado = true;
                }

                if (!cambiado)
                    continue;

                PrefabUtility.RecordPrefabInstancePropertyModifications(variacion);
                EditorUtility.SetDirty(variacion);
                EditorSceneManager.MarkSceneDirty(variacion.gameObject.scene);

                // OnEnable vuelve a aplicar el MaterialPropertyBlock con la textura nueva.
                variacion.enabled = false;
                variacion.enabled = true;
                cambios++;
            }
        }

        Debug.Log("AsignarTexturasDesgaste: " + cambios + " objetos actualizados. Guarda la escena para conservarlo.");
    }

    private static string NombreMalla(VariacionAlbedo variacion)
    {
        MeshFilter filtro = variacion.GetComponent<MeshFilter>();
        return filtro != null && filtro.sharedMesh != null ? filtro.sharedMesh.name : null;
    }
}
