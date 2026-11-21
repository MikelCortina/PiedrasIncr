using System;

public static class EventosEconomia
{
    public static event Action<string, int, int> OnCompraRealizada;

    public static void NotificarCompra(string id, int nivel, int coste)
    {
        if (OnCompraRealizada != null) OnCompraRealizada(id, nivel, coste);
    }

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        OnCompraRealizada = null;
    }
}
