#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

public static class PiedrasIncrEconomiaDemoTests
{
    private static readonly int[] Fusil = { 8, 14, 26, 47, 84 };
    private static readonly int[] Torbellino = { 20, 38, 72, 137, 261 };
    private static readonly int[] Lanzapiedras = { 0, 30, 75 };
    private static readonly int[] ArmaLunar = { 90, 153, 260 };
    private static readonly int[] Fisionador = { 300 };
    private static readonly int[] MisilGrande = { 8 };
    private static readonly int[] Cinta = { 40, 52, 68, 88, 114, 149, 193, 251, 326, 424 };
    private static readonly int[] Pared = { 5, 7, 8, 11, 14, 19, 24, 31, 41, 53 };

    [MenuItem("Tools/PiedrasIncr/Economia/Probar EconomiaDemo (precios del Excel)")]
    public static void Run()
    {
        StringBuilder log = new StringBuilder();
        int fallos = 0;

        EconomiaDemo porDefecto = ScriptableObject.CreateInstance<EconomiaDemo>();
        try
        {
            log.Append("== Valores por defecto (hojas SIMULADOR / MEJORAS / PROGRESION + extras actuales de la escena)\n");
            fallos += Comprobar(porDefecto, log);
        }
        finally
        {
            Object.DestroyImmediate(porDefecto);
        }

        string[] guids = AssetDatabase.FindAssets("t:EconomiaDemo");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            EconomiaDemo asset = AssetDatabase.LoadAssetAtPath<EconomiaDemo>(path);
            if (asset == null) continue;
            log.Append("\n== Asset ").Append(path).Append(" (si lo habéis ajustado en F11, aquí es normal que falle)\n");
            fallos += Comprobar(asset, log);
        }
        if (guids.Length == 0) log.Append("\n(No hay ningún asset EconomiaDemo en el proyecto todavía.)\n");

        string titulo = fallos == 0 ? "EconomiaDemo: todas las comprobaciones han pasado." : "EconomiaDemo: " + fallos + " comprobaciones han fallado.";
        if (fallos == 0) Debug.Log(titulo + "\n\n" + log);
        else Debug.LogError(titulo + "\n\n" + log);
        EditorUtility.DisplayDialog("EconomiaDemo", titulo + "\n\nEl detalle está en la consola.", "Aceptar");
    }

    private static int Comprobar(EconomiaDemo e, StringBuilder log)
    {
        int fallos = 0;
        fallos += Tabla(e, EconomiaDemo.Ids.FusilMonedas, Fusil, log);
        fallos += Tabla(e, EconomiaDemo.Ids.Torbellino, Torbellino, log);
        fallos += Tabla(e, EconomiaDemo.Ids.Lanzapiedras, Lanzapiedras, log);
        fallos += Tabla(e, EconomiaDemo.Ids.ArmaLunar, ArmaLunar, log);
        fallos += Tabla(e, EconomiaDemo.Ids.Fisionador, Fisionador, log);
        fallos += Tabla(e, EconomiaDemo.Ids.MisilGrande, MisilGrande, log);
        fallos += Tabla(e, EconomiaDemo.Ids.Cinta, Cinta, log);
        fallos += Tabla(e, EconomiaDemo.Ids.Pared, Pared, log);

        fallos += Check(e.PagaCon(EconomiaDemo.Ids.MisilGrande) == PagoMejora.EsferasPerfectas, "Misil grande se paga con esferas perfectas", log);
        fallos += Check(e.PagaCon(EconomiaDemo.Ids.Fisionador) == PagoMejora.Monedas, "Fisionador se paga con monedas", log);
        fallos += Check(e.FragmentosPorPiedra == 40, "40 fragmentos por piedra (" + e.FragmentosPorPiedra + ")", log);
        fallos += Check(Cerca(e.FraccionALitros(e.GasolinaPorPiedra(0.5f)), 25f, 0.01f), "Piedra al 50 % → 25 L (" + e.FraccionALitros(e.GasolinaPorPiedra(0.5f)).ToString("0.###") + ")", log);
        fallos += Check(Cerca(e.FraccionALitros(e.GasolinaPorPiedra(1f)), 100f, 0.01f), "Piedra al 100 % → 100 L (" + e.FraccionALitros(e.GasolinaPorPiedra(1f)).ToString("0.###") + ")", log);
        fallos += Check(Cerca(e.EsperaLanzapiedras(1), 5f, 0.001f) && Cerca(e.EsperaLanzapiedras(2), 4f, 0.001f) && Cerca(e.EsperaLanzapiedras(3), 3f, 0.001f), "Lanzapiedras espera 5 / 4 / 3 s", log);
        fallos += Check(e.PiedrasPorGolpeLanzapiedras(1) == 1 && e.PiedrasPorGolpeLanzapiedras(3) == 3, "Lanzapiedras: N piedras por golpe", log);
        fallos += Check(Cerca(e.IntervaloArmaLunar(1), 30f, 0.01f) && Cerca(e.IntervaloArmaLunar(2), 24f, 0.01f) && Cerca(e.IntervaloArmaLunar(3), 19.2f, 0.01f), "Arma Lunar cada 30 / 24 / 19,2 s", log);
        fallos += Check(Cerca(e.TasaRecogida(0), 0.6f, 0.0001f) && Cerca(e.TasaRecogida(1), 0.7f, 0.0001f) && Cerca(e.TasaRecogida(5), 1f, 0.0001f), "Recogida 60 % a mano, 70 %…100 % con Fusil", log);
        fallos += Check(e.esferasParaMisil == 8, "8 esferas para el misil", log);

        int total = 0;
        int pasos = 0;
        for (int i = 0; i < e.ordenCompras.Count; i++)
        {
            PasoCompra paso = e.ordenCompras[i];
            pasos++;
            if (e.PagaCon(paso.id) == PagoMejora.Monedas) total += e.Coste(paso.id, paso.nivel);
        }
        fallos += Check(pasos == 28 && total == 2106, "PROGRESION: " + pasos + " compras, " + total + " monedas (Excel: 28 compras, 2.106 monedas)", log);

        fallos += Tabla(e, EconomiaDemo.Ids.Botas, new[] { 5, 10, 20 }, log);
        fallos += Tabla(e, EconomiaDemo.Ids.Jetpack, new[] { 5, 10, 20, 40 }, log);
        fallos += Tabla(e, EconomiaDemo.Ids.Iman, new[] { 3 }, log);
        fallos += Tabla(e, EconomiaDemo.Ids.ImanAlcance, new[] { 0, 5, 10 }, log);
        fallos += Tabla(e, EconomiaDemo.Ids.TorbellinoAlcance, new[] { 0, 5, 10 }, log);
        fallos += Tabla(e, EconomiaDemo.Ids.TorbellinoMovilidad, new[] { 0, 5, 10 }, log);
        fallos += Tabla(e, EconomiaDemo.Ids.Martillo, new[] { 10 }, log);
        fallos += Tabla(e, EconomiaDemo.Ids.Bot, new[] { 100, 150, 200, 250 }, log);
        return fallos;
    }

    private static int Tabla(EconomiaDemo e, string id, int[] esperado, StringBuilder log)
    {
        int max = e.NivelesMax(id);
        StringBuilder obtenido = new StringBuilder();
        bool ok = max == esperado.Length;
        for (int nivel = 1; nivel <= max; nivel++)
        {
            int coste = e.Coste(id, nivel);
            if (nivel > 1) obtenido.Append('/');
            obtenido.Append(coste);
            if (nivel > esperado.Length || coste != esperado[nivel - 1]) ok = false;
        }
        StringBuilder esperadoTexto = new StringBuilder();
        for (int i = 0; i < esperado.Length; i++) { if (i > 0) esperadoTexto.Append('/'); esperadoTexto.Append(esperado[i]); }
        return Check(ok, id + ": " + obtenido + (ok ? string.Empty : "   (Excel: " + esperadoTexto + ")"), log);
    }

    private static bool Cerca(float a, float b, float margen)
    {
        return Mathf.Abs(a - b) <= margen;
    }

    private static int Check(bool ok, string texto, StringBuilder log)
    {
        log.Append(ok ? "[OK]    " : "[FALLO] ").Append(texto).Append('\n');
        return ok ? 0 : 1;
    }
}
#endif
