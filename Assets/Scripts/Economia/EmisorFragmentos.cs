using UnityEngine;

[DisallowMultipleComponent]
public class EmisorFragmentos : MonoBehaviour
{
    [SerializeField] private float siguienteFragmento = -1f;
    [SerializeField] private int fragmentosSoltados;

    private DesgastePiedra desgaste;

    public float SiguienteFragmento { get { return siguienteFragmento; } }
    public int FragmentosSoltados { get { return fragmentosSoltados; } }

    private void Awake()
    {
        desgaste = GetComponent<DesgastePiedra>();
        if (desgaste == null) desgaste = gameObject.AddComponent<DesgastePiedra>();
        if (siguienteFragmento < 0f) siguienteFragmento = ProveedorEconomia.Actual.fragmentoCada;
    }

    private void OnEnable()
    {
        desgaste.OnDesgasteCambiado += AlCambiarDesgaste;
        desgaste.OnEsferaPerfecta += AlSerEsferaPerfecta;
        desgaste.OnDesgasteRestaurado += AlRestaurar;
    }

    private void OnDisable()
    {
        desgaste.OnDesgasteCambiado -= AlCambiarDesgaste;
        desgaste.OnEsferaPerfecta -= AlSerEsferaPerfecta;
        desgaste.OnDesgasteRestaurado -= AlRestaurar;
    }

    private void AlCambiarDesgaste(float antes, float despues)
    {
        EconomiaDemo economia = ProveedorEconomia.Actual;
        float paso = Mathf.Max(0.0001f, economia.fragmentoCada);
        int maximo = economia.FragmentosPorPiedra;
        int nuevos = 0;

        while (fragmentosSoltados < maximo && despues + 0.00001f >= (fragmentosSoltados + 1) * paso)
        {
            fragmentosSoltados++;
            nuevos++;
        }
        siguienteFragmento = (fragmentosSoltados + 1) * paso;

        if (nuevos > 0) GestorFragmentos.Instancia.Soltar(transform.position, nuevos, false);
    }

    private void AlSerEsferaPerfecta()
    {
        int bonus = ProveedorEconomia.Actual.bonusEsferaPerfecta;
        GestorFragmentos.Instancia.CelebrarEsferaPerfecta(transform.position, bonus);
    }

    private void AlRestaurar(float valor)
    {
        float paso = Mathf.Max(0.0001f, ProveedorEconomia.Actual.fragmentoCada);
        fragmentosSoltados = Mathf.Min(ProveedorEconomia.Actual.FragmentosPorPiedra, Mathf.FloorToInt((valor + 0.00001f) / paso));
        siguienteFragmento = (fragmentosSoltados + 1) * paso;
    }

    public void Restaurar(float siguiente, int soltados)
    {
        siguienteFragmento = siguiente;
        fragmentosSoltados = soltados;
    }
}
