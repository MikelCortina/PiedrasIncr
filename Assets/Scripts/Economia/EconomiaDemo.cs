using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PasoCompra
{
    public string id;
    public int nivel;
    public float minutoPrevisto;

    public PasoCompra()
    {
    }

    public PasoCompra(string id, int nivel, float minutoPrevisto)
    {
        this.id = id;
        this.nivel = nivel;
        this.minutoPrevisto = minutoPrevisto;
    }
}

public enum PagoMejora
{
    Monedas,
    EsferasPerfectas
}

[Serializable]
public class PrecioMejora
{
    public string id;
    public int costeBase;
    public float multiplicador = 1f;
    public int nivelesMax = 1;
    public PagoMejora pagaCon = PagoMejora.Monedas;
    public bool primerNivelGratis;
    [Tooltip("Opcional: precio exacto de cada nivel. Si tiene valores, sustituye a base × mult^(N-1).")]
    public int[] costesFijos;
    [Tooltip("Compras que no están en el Excel (se miden en F10/F11).")]
    public bool extra;

    public PrecioMejora()
    {
    }

    public PrecioMejora(string id, int costeBase, float multiplicador, int nivelesMax, PagoMejora pagaCon = PagoMejora.Monedas, bool primerNivelGratis = false)
    {
        this.id = id;
        this.costeBase = costeBase;
        this.multiplicador = multiplicador;
        this.nivelesMax = nivelesMax;
        this.pagaCon = pagaCon;
        this.primerNivelGratis = primerNivelGratis;
    }
}

[CreateAssetMenu(fileName = "EconomiaDemo", menuName = "PiedrasIncr/Economia Demo")]
public class EconomiaDemo : ScriptableObject
{
    public static class Ids
    {
        public const string FusilMonedas = "FusilMonedas";
        public const string Torbellino = "Torbellino";
        public const string Lanzapiedras = "Lanzapiedras";
        public const string ArmaLunar = "ArmaLunar";
        public const string Fisionador = "Fisionador";
        public const string MisilGrande = "MisilGrande";
        public const string Cinta = "Cinta";
        public const string Pared = "Pared";
        public const string Botas = "Botas";
        public const string Jetpack = "Jetpack";
        public const string Iman = "Iman";
        public const string ImanAlcance = "ImanAlcance";
        public const string TorbellinoAlcance = "TorbellinoAlcance";
        public const string TorbellinoMovilidad = "TorbellinoMovilidad";
        public const string Martillo = "Martillo";
        public const string Bot = "Bot";
    }

    public static string NombreVisible(string id)
    {
        switch (id)
        {
            case Ids.FusilMonedas: return "Fusil Monedas";
            case Ids.Torbellino: return "Torbellino";
            case Ids.Lanzapiedras: return "Lanzapiedras";
            case Ids.ArmaLunar: return "Arma Lunar";
            case Ids.Fisionador: return "Fisionador";
            case Ids.MisilGrande: return "Misil grande";
            case Ids.Cinta: return "Cinta";
            case Ids.Pared: return "Pared";
            case Ids.Botas: return "Botas";
            case Ids.Jetpack: return "Jetpack";
            case Ids.Iman: return "Imán";
            case Ids.ImanAlcance: return "Alcance del imán";
            case Ids.TorbellinoAlcance: return "Alcance del torbellino";
            case Ids.TorbellinoMovilidad: return "Movilidad del torbellino";
            case Ids.Martillo: return "Martillo";
            case Ids.Bot: return "Bot";
            default: return id;
        }
    }

    [Header("Piedra y desgaste")]
    public float golpePatada = 0.005f;
    public float desgastePorMetro = 0.001f;
    public float desgasteTorbellinoPorSegundo = 0.015f;

    [Header("Fragmentos")]
    public float fragmentoCada = 0.025f;
    public int monedasPorFragmento = 1;
    public int bonusEsferaPerfecta = 10;
    [Range(0f, 1f)] public float recogidaAMano = 0.6f;
    public float[] recogidaPorNivelFusil = { 0.7f, 0.8f, 0.9f, 1f, 1f };

    [Header("Gasolina")]
    public float gasolinaMaxPorPiedra = 0.0001f;
    public float curvaK = 2f;
    public float tanqueLitrosVisual = 1000000f;

    [Header("Final")]
    public int esferasParaMisil = 8;

    [Header("Lanzapiedras")]
    public float esperaNivel1 = 5f;
    public float menosPorNivel = 1f;

    [Header("Arma Lunar")]
    public float cadaNivel1 = 30f;
    public float cadenciaExtraPorNivel = 0.25f;

    [Header("Cinta")]
    public float velocidadCinta = 1f;

    [Header("Precios (coste nivel N = base × mult^(N-1))")]
    public List<PrecioMejora> precios = PreciosDelExcel();

    [Header("Orden de compras previsto (hoja PROGRESION)")]
    public List<PasoCompra> ordenCompras = OrdenDelExcel();

    public static List<PasoCompra> OrdenDelExcel()
    {
        return new List<PasoCompra>
        {
            new PasoCompra(Ids.FusilMonedas, 1, 2.9f), new PasoCompra(Ids.Torbellino, 1, 4.8f), new PasoCompra(Ids.Pared, 1, 5.1f),
            new PasoCompra(Ids.Pared, 2, 5.5f), new PasoCompra(Ids.Lanzapiedras, 2, 7.2f), new PasoCompra(Ids.FusilMonedas, 2, 7.8f),
            new PasoCompra(Ids.Pared, 3, 8.1f), new PasoCompra(Ids.Cinta, 1, 9.5f), new PasoCompra(Ids.Torbellino, 2, 10.7f),
            new PasoCompra(Ids.FusilMonedas, 3, 11.3f), new PasoCompra(Ids.Pared, 4, 11.6f), new PasoCompra(Ids.ArmaLunar, 1, 13.6f),
            new PasoCompra(Ids.Cinta, 2, 14.4f), new PasoCompra(Ids.Torbellino, 3, 15.5f), new PasoCompra(Ids.Pared, 5, 15.7f),
            new PasoCompra(Ids.Cinta, 3, 16.4f), new PasoCompra(Ids.Lanzapiedras, 3, 17.2f), new PasoCompra(Ids.FusilMonedas, 4, 17.7f),
            new PasoCompra(Ids.Torbellino, 4, 19.0f), new PasoCompra(Ids.ArmaLunar, 2, 20.1f), new PasoCompra(Ids.Cinta, 4, 20.7f),
            new PasoCompra(Ids.Pared, 6, 20.9f), new PasoCompra(Ids.Cinta, 5, 21.7f), new PasoCompra(Ids.Torbellino, 5, 23.5f),
            new PasoCompra(Ids.ArmaLunar, 3, 25.0f), new PasoCompra(Ids.Cinta, 6, 25.8f), new PasoCompra(Ids.Fisionador, 1, 27.5f),
            new PasoCompra(Ids.MisilGrande, 1, 29.7f)
        };
    }

    public static List<PrecioMejora> PreciosDelExcel()
    {
        return new List<PrecioMejora>
        {
            new PrecioMejora(Ids.FusilMonedas, 8, 1.8f, 5),
            new PrecioMejora(Ids.Torbellino, 20, 1.9f, 5),
            new PrecioMejora(Ids.Lanzapiedras, 30, 2.5f, 3, PagoMejora.Monedas, true),
            new PrecioMejora(Ids.ArmaLunar, 90, 1.7f, 3),
            new PrecioMejora(Ids.Fisionador, 300, 1f, 1),
            new PrecioMejora(Ids.MisilGrande, 8, 1f, 1, PagoMejora.EsferasPerfectas),
            new PrecioMejora(Ids.Cinta, 40, 1.3f, 10),
            new PrecioMejora(Ids.Pared, 5, 1.3f, 10),
            Extra(new PrecioMejora(Ids.Botas, 5, 2f, 3)),
            Extra(new PrecioMejora(Ids.Jetpack, 5, 2f, 4)),
            Extra(new PrecioMejora(Ids.Iman, 3, 1f, 1)),
            Extra(new PrecioMejora(Ids.ImanAlcance, 5, 2f, 3, PagoMejora.Monedas, true)),
            Extra(new PrecioMejora(Ids.TorbellinoAlcance, 5, 2f, 3, PagoMejora.Monedas, true)),
            Extra(new PrecioMejora(Ids.TorbellinoMovilidad, 5, 2f, 3, PagoMejora.Monedas, true)),
            Extra(new PrecioMejora(Ids.Martillo, 10, 1f, 1)),
            Extra(new PrecioMejora(Ids.Bot, 100, 1f, 4) { costesFijos = new[] { 100, 150, 200, 250 } })
        };
    }

    private static PrecioMejora Extra(PrecioMejora precio)
    {
        precio.extra = true;
        return precio;
    }

    public bool Tiene(string id)
    {
        return BuscarPrecio(id) != null;
    }

    public int CosteOPorDefecto(string id, int nivel, int porDefecto)
    {
        PrecioMejora precio = BuscarPrecio(id);
        if (precio == null || nivel < 1 || nivel > precio.nivelesMax) return porDefecto;
        return Coste(id, nivel);
    }

    public static int CalcularCoste(int costeBase, float multiplicador, int exponente)
    {
        double valor = costeBase * Math.Pow(multiplicador, exponente);
        valor = Math.Round(valor, 4);
        return (int)Math.Round(valor, 0, MidpointRounding.AwayFromZero);
    }

    public PrecioMejora BuscarPrecio(string id)
    {
        if (string.IsNullOrEmpty(id) || precios == null) return null;
        for (int i = 0; i < precios.Count; i++)
        {
            if (precios[i] != null && string.Equals(precios[i].id, id, StringComparison.Ordinal)) return precios[i];
        }
        return null;
    }

    public int NivelesMax(string id)
    {
        PrecioMejora precio = BuscarPrecio(id);
        return precio != null ? precio.nivelesMax : 0;
    }

    public PagoMejora PagaCon(string id)
    {
        PrecioMejora precio = BuscarPrecio(id);
        return precio != null ? precio.pagaCon : PagoMejora.Monedas;
    }

    public int Coste(string id, int nivel)
    {
        PrecioMejora precio = BuscarPrecio(id);
        if (precio == null)
        {
            Debug.LogWarning("EconomiaDemo: no hay precio para '" + id + "'.", this);
            return -1;
        }

        if (nivel < 1 || nivel > precio.nivelesMax)
        {
            Debug.LogWarning("EconomiaDemo: nivel " + nivel + " fuera de rango para '" + id + "' (1.." + precio.nivelesMax + ").", this);
            return -1;
        }

        if (precio.costesFijos != null && precio.costesFijos.Length > 0)
        {
            return precio.costesFijos[Mathf.Min(nivel, precio.costesFijos.Length) - 1];
        }

        if (precio.primerNivelGratis)
        {
            if (nivel == 1) return 0;
            return CalcularCoste(precio.costeBase, precio.multiplicador, nivel - 2);
        }

        return CalcularCoste(precio.costeBase, precio.multiplicador, nivel - 1);
    }

    public int FragmentosPorPiedra
    {
        get { return fragmentoCada > 0f ? Mathf.RoundToInt(1f / fragmentoCada) : 0; }
    }

    public float TasaRecogida(int nivelFusil)
    {
        if (nivelFusil <= 0 || recogidaPorNivelFusil == null || recogidaPorNivelFusil.Length == 0) return recogidaAMano;
        int indice = Mathf.Min(nivelFusil, recogidaPorNivelFusil.Length) - 1;
        return recogidaPorNivelFusil[indice];
    }

    public float GasolinaPorPiedra(float desgaste)
    {
        return gasolinaMaxPorPiedra * Mathf.Pow(Mathf.Clamp01(desgaste), curvaK);
    }

    public float FraccionALitros(float fraccionTanque)
    {
        return fraccionTanque * tanqueLitrosVisual;
    }

    public int PiedrasPorGolpeLanzapiedras(int nivel)
    {
        return Mathf.Max(1, nivel);
    }

    public float EsperaLanzapiedras(int nivel)
    {
        return Mathf.Max(0f, esperaNivel1 - menosPorNivel * (Mathf.Max(1, nivel) - 1));
    }

    public float IntervaloArmaLunar(int nivel)
    {
        return cadaNivel1 / Mathf.Pow(1f + cadenciaExtraPorNivel, Mathf.Max(1, nivel) - 1);
    }

    [ContextMenu("Restaurar precios y orden del Excel")]
    private void RestaurarPreciosDelExcel()
    {
        precios = PreciosDelExcel();
        ordenCompras = OrdenDelExcel();
    }

    private void OnValidate()
    {
        if (precios == null) return;
        HashSet<string> vistos = new HashSet<string>();
        for (int i = 0; i < precios.Count; i++)
        {
            PrecioMejora precio = precios[i];
            if (precio == null || string.IsNullOrEmpty(precio.id)) continue;
            if (!vistos.Add(precio.id)) Debug.LogWarning("EconomiaDemo: el id de precio '" + precio.id + "' está repetido.", this);
            if (precio.nivelesMax < 1) precio.nivelesMax = 1;
        }
    }
}
