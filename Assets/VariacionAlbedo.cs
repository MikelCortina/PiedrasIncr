using UnityEngine;

[System.Serializable]
public struct ConfiguracionMaterial
{
    [Tooltip("El índice del material en el Mesh Renderer (0, 1, 2...)")]
    public int indiceMaterial;

    [Header("Albedo")]
    public Color nuevoColor;
    public Texture nuevaTextura;

    [Header("Contorno")]
    public Color colorContorno;
    // Eliminado el [Range(0f, 10f)] para permitir valores infinitos
    public float grosorContorno;
}

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class VariacionAlbedo : MonoBehaviour
{
    [Header("Lista de Materiales a Modificar")]
    public ConfiguracionMaterial[] configuraciones;

    private Renderer render;
    private MaterialPropertyBlock propBlock;

    void OnEnable()
    {
        AplicarMaterial();
    }

    void OnValidate()
    {
        AplicarMaterial();
    }

    private void AplicarMaterial()
    {
        if (render == null) render = GetComponent<Renderer>();
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        if (configuraciones == null || configuraciones.Length == 0) return;

        for (int i = 0; i < configuraciones.Length; i++)
        {
            var config = configuraciones[i];

            if (render.sharedMaterials.Length <= config.indiceMaterial) continue;

            render.GetPropertyBlock(propBlock, config.indiceMaterial);

            propBlock.SetColor("_BaseColor", config.nuevoColor);
            if (config.nuevaTextura != null)
            {
                propBlock.SetTexture("_BaseMap", config.nuevaTextura);
            }

            propBlock.SetColor("_OutlineColor", config.colorContorno);
            propBlock.SetFloat("_OutlineWidth", config.grosorContorno);

            render.SetPropertyBlock(propBlock, config.indiceMaterial);
        }
    }
}