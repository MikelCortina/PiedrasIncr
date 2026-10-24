using UnityEngine;
using UnityEditor;

public class ActualizadorNeonesEditor
{
    // Esto creará un nuevo botón en la barra superior de Unity
    [MenuItem("Herramientas/Actualizar Todos los Neones a Valores por Defecto")]
    public static void ActualizarTodosLosNeones()
    {
        int contadorEscena = 0;
        int contadorPrefabs = 0;

        // 1. ACTUALIZAR OBJETOS EN LA ESCENA ACTUAL (Incluyendo los desactivados)
        ControladorNeon[] neonesEnEscena = Object.FindObjectsOfType<ControladorNeon>(true);
        foreach (ControladorNeon neon in neonesEnEscena)
        {
            // Ignoramos los prefabs guardados en proyecto para contarlos luego
            if (PrefabUtility.IsPartOfPrefabAsset(neon)) continue;

            AplicarValores(neon);
            contadorEscena++;
        }

        // 2. ACTUALIZAR EN TODOS LOS PREFABS DEL PROYECTO
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                ControladorNeon[] neonesEnPrefab = prefab.GetComponentsInChildren<ControladorNeon>(true);
                if (neonesEnPrefab.Length > 0)
                {
                    foreach (ControladorNeon neon in neonesEnPrefab)
                    {
                        AplicarValores(neon);
                        contadorPrefabs++;
                    }
                    // Le decimos a Unity que guarde los cambios en este Prefab
                    EditorUtility.SetDirty(prefab);
                }
            }
        }

        // Guardamos los assets modificados en el disco
        AssetDatabase.SaveAssets();

        Debug.Log($"<b>¡Actualización completada!</b> Se sobrescribieron los valores de {contadorEscena} neones en la Escena y {contadorPrefabs} en los Prefabs.");
    }

    private static void AplicarValores(ControladorNeon neon)
    {
        // Forzamos la creación del nuevo array con los valores exactos que querías
        neon.ajustesPorSlot = new AjustesNeonSlot[]
        {
            new AjustesNeonSlot()
            {
                nombre = "Pieza Neón",
                indiceMaterial = 0,
                colorNeon = new Color(236.19f/255f, 226.255f/255f, 1f), // Tu color azulado exacto
                intensidad = 1.5f,
                grosorOutline = 1.0f,
                cantidadZonasMuertas = 0f,
                velocidadParpadeo = 0f
            }
        };

        // Le decimos a Unity que este objeto específico ha cambiado
        EditorUtility.SetDirty(neon);
    }
}