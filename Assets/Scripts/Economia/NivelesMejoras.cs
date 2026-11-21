using System;
using System.Collections.Generic;
using UnityEngine;

public static class NivelesMejoras
{
    private static readonly Dictionary<string, Func<int>> proveedores = new Dictionary<string, Func<int>>();
    private static GestorEquipamiento gestor;
    private static ConstruccionDianaBloopMonedas diana;
    private static ConstruccionAmetralladoraMonedas armaLunar;
    private static InventarioEdificios inventario;
    private static MovimientoPersonaje movimiento;
    private static RecolectorMagnetico iman;
    private static HerramientaTorbellino torbellino;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        proveedores.Clear();
        gestor = null;
        diana = null;
        armaLunar = null;
        inventario = null;
        movimiento = null;
        iman = null;
        torbellino = null;
    }

    public static void Registrar(string id, Func<int> proveedor)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (proveedor == null) proveedores.Remove(id);
        else proveedores[id] = proveedor;
    }

    private static T Buscar<T>(ref T cache) where T : UnityEngine.Object
    {
        if (cache == null) cache = UnityEngine.Object.FindAnyObjectByType<T>();
        return cache;
    }

    public static int Nivel(string id)
    {
        Func<int> proveedor;
        if (proveedores.TryGetValue(id, out proveedor)) return proveedor();

        switch (id)
        {
            case EconomiaDemo.Ids.FusilMonedas:
            {
                GestorEquipamiento g = Buscar(ref gestor);
                if (g == null || !g.ArmaLanzadoraDesbloqueada()) return 0;
                return g.armaLanzadora != null ? g.armaLanzadora.NivelFusil : 1;
            }
            case EconomiaDemo.Ids.Torbellino:
            {
                GestorEquipamiento g = Buscar(ref gestor);
                if (g == null || !g.TorbellinoDesbloqueado()) return 0;
                HerramientaTorbellino t = g.herramientaTorbellino != null ? g.herramientaTorbellino : Buscar(ref torbellino);
                return t != null ? t.NivelCapacidad : 1;
            }
            case EconomiaDemo.Ids.Lanzapiedras:
            {
                ConstruccionDianaBloopMonedas d = Buscar(ref diana);
                return d != null ? d.NivelActual : 0;
            }
            case EconomiaDemo.Ids.ArmaLunar:
            {
                ConstruccionAmetralladoraMonedas a = Buscar(ref armaLunar);
                return a != null ? a.NivelActual : 0;
            }
            case EconomiaDemo.Ids.Cinta:
            {
                InventarioEdificios inv = Buscar(ref inventario);
                return inv != null ? inv.CompradasTotal(SistemaConstruccion.TipoEdificio.Rampa) : 0;
            }
            case EconomiaDemo.Ids.Pared:
            {
                InventarioEdificios inv = Buscar(ref inventario);
                return inv != null ? inv.CompradasTotal(SistemaConstruccion.TipoEdificio.Pared) : 0;
            }
            case EconomiaDemo.Ids.Botas:
            {
                MovimientoPersonaje m = Buscar(ref movimiento);
                return m != null ? m.NivelBotas : 0;
            }
            case EconomiaDemo.Ids.Jetpack:
            {
                MovimientoPersonaje m = Buscar(ref movimiento);
                return m != null ? m.NivelJetpack : 0;
            }
            case EconomiaDemo.Ids.Martillo:
            {
                GestorEquipamiento g = Buscar(ref gestor);
                return g != null && g.MartilloDesbloqueado() ? 1 : 0;
            }
            case EconomiaDemo.Ids.Iman:
            {
                RecolectorMagnetico r = Buscar(ref iman);
                return r != null && r.ImanDesbloqueado ? 1 : 0;
            }
            default:
                return 0;
        }
    }

    public static PasoCompra SiguientePaso(EconomiaDemo economia)
    {
        if (economia == null || economia.ordenCompras == null) return null;
        for (int i = 0; i < economia.ordenCompras.Count; i++)
        {
            PasoCompra paso = economia.ordenCompras[i];
            if (paso == null || string.IsNullOrEmpty(paso.id)) continue;
            if (Nivel(paso.id) < paso.nivel) return paso;
        }
        return null;
    }
}
