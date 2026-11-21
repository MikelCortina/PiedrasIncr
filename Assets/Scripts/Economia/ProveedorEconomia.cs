using UnityEngine;

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public class ProveedorEconomia : MonoBehaviour
{
    public const string RecursoPorDefecto = "EconomiaDemo";

    [SerializeField] private EconomiaDemo economia;

    private static ProveedorEconomia instancia;
    private static EconomiaDemo respaldo;
    private static bool buscado;

    public EconomiaDemo Economia
    {
        get { return economia; }
    }

    public static EconomiaDemo Actual
    {
        get
        {
            if (instancia != null && instancia.economia != null) return instancia.economia;
            if (respaldo != null) return respaldo;

            if (!buscado)
            {
                buscado = true;
                ProveedorEconomia enEscena = FindAnyObjectByType<ProveedorEconomia>();
                if (enEscena != null && enEscena.economia != null)
                {
                    instancia = enEscena;
                    return enEscena.economia;
                }
            }

            respaldo = Resources.Load<EconomiaDemo>(RecursoPorDefecto);
            if (respaldo == null)
            {
                Debug.LogWarning("ProveedorEconomia: no hay EconomiaDemo asignada ni en Resources/" + RecursoPorDefecto + ". Se usan los valores por defecto del Excel.");
                respaldo = ScriptableObject.CreateInstance<EconomiaDemo>();
                respaldo.hideFlags = HideFlags.DontSave;
            }
            return respaldo;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEstatico()
    {
        instancia = null;
        respaldo = null;
        buscado = false;
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Debug.LogWarning("ProveedorEconomia: hay más de uno en la escena. Se usa el de '" + instancia.name + "'.", this);
            return;
        }

        instancia = this;
        if (economia == null) Debug.LogWarning("ProveedorEconomia: falta asignar EconomiaDemo; se usará Resources/" + RecursoPorDefecto + ".", this);
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }
}
