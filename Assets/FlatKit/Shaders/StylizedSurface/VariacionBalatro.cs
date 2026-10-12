using UnityEngine;

[System.Serializable]
public struct ConfiguracionBalatro
{
    [Tooltip("El índice del material en el Mesh Renderer (0, 1, 2...)")]
    public int indiceMaterial;

    [Header("Colores Procedurales Balatro")]
    [ColorUsage(true, true)] // Fuerza el uso de HDR en el Inspector
    public Color color1;

    [ColorUsage(true, true)]
    public Color color2;

    [ColorUsage(true, true)]
    public Color color3;

    [Header("Ajustes de Efecto")]
    [Tooltip("Equivale a _BalatroIntensity")]
    public float intensidadGlobal;

    [Tooltip("Equivale a _DistortionSpeed")]
    public float velocidadAnimacion;

    [Tooltip("Equivale a _DistortionScale")]
    public float escalaPatron;
}

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class VariacionBalatro : MonoBehaviour
{
    [Header("Configuración Balatro por Material")]
    public ConfiguracionBalatro[] configuraciones;

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

    public void AplicarMaterial()
    {
        if (render == null) render = GetComponent<Renderer>();
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        if (configuraciones == null || configuraciones.Length == 0) return;

        for (int i = 0; i < configuraciones.Length; i++)
        {
            var config = configuraciones[i];

            // Evitar errores si el array de materiales es menor
            if (render.sharedMaterials == null || render.sharedMaterials.Length <= config.indiceMaterial) continue;

            Material matBase = render.sharedMaterials[config.indiceMaterial];
            if (matBase == null) continue;

            // 1. Obtener el bloque actual del material
            render.GetPropertyBlock(propBlock, config.indiceMaterial);

            // 2. Inyectar las variables específicas de Balatro
            propBlock.SetColor("_BalatroColor1", config.color1);
            propBlock.SetColor("_BalatroColor2", config.color2);
            propBlock.SetColor("_BalatroColor3", config.color3);

            propBlock.SetFloat("_BalatroIntensity", config.intensidadGlobal);
            propBlock.SetFloat("_DistortionSpeed", config.velocidadAnimacion);
            propBlock.SetFloat("_DistortionScale", config.escalaPatron);

            // 3. Aplicar el bloque modificado al renderizador
            render.SetPropertyBlock(propBlock, config.indiceMaterial);
        }
    }

    // ==========================================
    // FUNCIONES PARA ANIMAR DESDE OTROS SCRIPTS
    // ==========================================

    /// <summary>
    /// Cambia la velocidad de animación de todos los materiales configurados en tiempo real.
    /// </summary>
    public void SetGlobalSpeed(float nuevaVelocidad)
    {
        for (int i = 0; i < configuraciones.Length; i++)
        {
            configuraciones[i].velocidadAnimacion = nuevaVelocidad;
        }
        AplicarMaterial();
    }

    /// <summary>
    /// Cambia los 3 colores procedurales en tiempo real.
    /// </summary>
    public void SetColors(Color c1, Color c2, Color c3)
    {
        for (int i = 0; i < configuraciones.Length; i++)
        {
            configuraciones[i].color1 = c1;
            configuraciones[i].color2 = c2;
            configuraciones[i].color3 = c3;
        }
        AplicarMaterial();
    }
}