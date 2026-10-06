using UnityEngine;

[System.Serializable]
public class ConfiguracionDeformacion // <-- SOLUCIÓN: Cambiado de struct a class
{
    [Tooltip("El índice del material en el Mesh Renderer (0, 1, 2...)")]
    public int indiceMaterial = 0;

    [Tooltip("Controla el avance de la panza por la tubería.")]
    [Range(-0.5f, 1.5f)]
    public float panner = -0.2f;

    [Tooltip("Lo estrecha que es la anilla. Mayor valor = anillo más fino.")]
    public float bulgeWidth = 15f;

    [Tooltip("La fuerza con la que la malla sale hacia afuera.")]
    public float budgePower = 0.3f;
}

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class ControladorDeformacionTuberia : MonoBehaviour
{
    [Header("Lista de Materiales a Deformar")]
    public ConfiguracionDeformacion[] configuraciones;

    private Renderer render;
    private MaterialPropertyBlock propBlock;

    private static readonly int PannerId = Shader.PropertyToID("_Panner");
    private static readonly int BulgeWidthId = Shader.PropertyToID("_BulgeWidth");
    private static readonly int BudgePowerId = Shader.PropertyToID("_BudgePower");

    void OnEnable()
    {
        AplicarPropiedades();
    }

    void OnValidate()
    {
        AplicarPropiedades();
    }

    void Update()
    {
        // Solo actualizamos en Play si otro script está moviendo el panner
        if (Application.isPlaying)
        {
            AplicarPropiedades();
        }
    }

    public void AplicarPropiedades()
    {
        if (render == null) render = GetComponent<Renderer>();
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        if (configuraciones == null || configuraciones.Length == 0) return;

        for (int i = 0; i < configuraciones.Length; i++)
        {
            var config = configuraciones[i];

            if (config == null) continue;

            // Evitar errores si el array de materiales es menor
            if (render.sharedMaterials == null || render.sharedMaterials.Length <= config.indiceMaterial) continue;

            Material matBase = render.sharedMaterials[config.indiceMaterial];
            if (matBase == null) continue;

            // 1. Obtenemos el bloque ACTUAL de este índice específico para no borrar lo que hace VariacionAlbedo
            render.GetPropertyBlock(propBlock, config.indiceMaterial);

            // 2. Inyectamos nuestros 3 valores de deformación
            propBlock.SetFloat(PannerId, config.panner);
            propBlock.SetFloat(BulgeWidthId, config.bulgeWidth);
            propBlock.SetFloat(BudgePowerId, config.budgePower);

            // 3. Aplicamos el bloque de vuelta al MISMO índice
            render.SetPropertyBlock(propBlock, config.indiceMaterial);
        }
    }

    /// <summary>
    /// Función para que tu script del meteorito (MaquinaPiedras) actualice el panner
    /// </summary>
    public void SetPannerGlobal(float progreso)
    {
        if (configuraciones == null) return;

        for (int i = 0; i < configuraciones.Length; i++)
        {
            if (configuraciones[i] != null)
            {
                configuraciones[i].panner = progreso;
            }
        }
        AplicarPropiedades();
    }
}