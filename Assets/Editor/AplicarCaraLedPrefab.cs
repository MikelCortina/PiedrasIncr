using UnityEditor;
using UnityEngine;

// Pone la cara LED (triste) guardada como material real en el slot de la pantalla del prefab Bot Variant,
// para que el prefab la lleve puesta también en el editor y no solo al darle a Play.
// Se hace solo al abrir/recompilar el editor si al prefab aún le falta; también desde el menú.
[InitializeOnLoad]
static class AplicarCaraLedPrefab
{
    const string RutaPrefab = "Assets/Bot Variant.prefab";

    static AplicarCaraLedPrefab()
    {
        EditorApplication.delayCall += AplicarSiFalta;
    }

    static void AplicarSiFalta()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += AplicarSiFalta;
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
        if (prefab == null) return;

        foreach (CaraLedArana cara in prefab.GetComponentsInChildren<CaraLedArana>(true))
        {
            if (!cara.TieneCaraGuardada())
            {
                Aplicar();
                return;
            }
        }
    }

    [MenuItem("Herramientas/Cara LED/Aplicar cara al prefab Bot Variant")]
    static void Aplicar()
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
        try
        {
            CaraLedArana[] caras = raiz.GetComponentsInChildren<CaraLedArana>(true);
            if (caras.Length == 0)
            {
                Debug.LogWarning("[CaraLed] " + RutaPrefab + " no tiene ningún CaraLedArana.");
                return;
            }

            foreach (CaraLedArana cara in caras) cara.GuardarCaraEnSlot();
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Debug.Log("[CaraLed] Cara aplicada a " + RutaPrefab + " (" + CaraLedArana.RutaMaterialTriste + ").");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(raiz);
        }
    }
}
