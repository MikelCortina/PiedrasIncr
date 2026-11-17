using UnityEngine;

/// <summary>
/// Marca una piedra que ya ha pasado por la desgastadora (MaquinaErosion)
/// y le cambia los colores del interior (shader "FlatKit/Stylized Surface Balatro").
/// Se añade sola en tiempo de ejecución.
/// </summary>
[DisallowMultipleComponent]
public class PiedraProcesada : MonoBehaviour
{
    private static readonly int IdColor1 = Shader.PropertyToID("_BalatroColor1");
    private static readonly int IdColor2 = Shader.PropertyToID("_BalatroColor2");
    private static readonly int IdColor3 = Shader.PropertyToID("_BalatroColor3");
    private static MaterialPropertyBlock bloque;

    public static bool EstaProcesada(Component objeto)
    {
        return objeto != null && objeto.GetComponentInParent<PiedraProcesada>() != null;
    }

    public static void Marcar(
        GameObject piedra,
        Color color1,
        Color color2,
        Color color3,
        string nombreInterior)
    {
        if (piedra == null) return;

        if (piedra.GetComponent<PiedraProcesada>() == null)
            piedra.AddComponent<PiedraProcesada>();

        Renderer interior = BuscarInterior(piedra.transform, nombreInterior);

        if (interior == null)
        {
            Debug.LogWarning("[PiedraProcesada] No se encuentra el hijo '" + nombreInterior + "' en " + piedra.name, piedra);
            return;
        }

        // Si el interior ya lleva VariacionBalatro configurado, se cambia a través de él
        // para que no vuelva a pintar los colores antiguos al reactivarse.
        VariacionBalatro variacion = interior.GetComponent<VariacionBalatro>();

        if (variacion != null &&
            variacion.configuraciones != null &&
            variacion.configuraciones.Length > 0)
        {
            variacion.SetColors(color1, color2, color3);
            return;
        }

        if (bloque == null) bloque = new MaterialPropertyBlock();

        // Por índice de material, para no pisar otros valores que ya tenga el bloque.
        int cantidad = interior.sharedMaterials.Length;

        for (int i = 0; i < cantidad; i++)
        {
            interior.GetPropertyBlock(bloque, i);
            bloque.SetColor(IdColor1, color1);
            bloque.SetColor(IdColor2, color2);
            bloque.SetColor(IdColor3, color3);
            interior.SetPropertyBlock(bloque, i);
        }
    }

    private static Renderer BuscarInterior(Transform raiz, string nombre)
    {
        foreach (Transform hijo in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (hijo == raiz || hijo.name != nombre) continue;

            Renderer render = hijo.GetComponentInChildren<Renderer>(true);
            if (render != null) return render;
        }

        return null;
    }
}