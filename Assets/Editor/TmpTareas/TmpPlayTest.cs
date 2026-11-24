using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// TEMPORAL: se borra al terminar.
[InitializeOnLoad]
public static class TmpPlayTest
{
    const string Out = @"C:\Users\egoit\AppData\Local\Temp\claude\b6df86ffc997a27b\217b105a-07b3-4a2d-b00b-9902a72abb08\scratchpad\";
    static Stack<IEnumerator> pila;
    static int fallos;
    static readonly List<string> errores = new List<string>();

    static TmpPlayTest()
    {
        if (SessionState.GetBool("TMPTEST", false))
        {
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredPlayMode) Arrancar();
            };
        }
    }

    public static void Run()
    {
        File.WriteAllText(Out + "test.txt", "");
        SessionState.SetBool("TMPTEST", true);
        EditorSceneManager.OpenScene("Assets/MenuPrincipal.unity");
        EditorApplication.playModeStateChanged += s => { if (s == PlayModeStateChange.EnteredPlayMode && pila == null) Arrancar(); };
        EditorApplication.EnterPlaymode();
    }

    static void Arrancar()
    {
        if (pila != null) return;
        Application.runInBackground = true;
        Application.logMessageReceived += (m, st, t) =>
        {
            if (t == LogType.Error || t == LogType.Exception || t == LogType.Assert) { string e = t + ": " + m.Split('\n')[0] + " @ " + (st ?? "").Split('\n').FirstOrDefault(x => x.Contains("Assets/")); if (errores.Count < 60 && !errores.Contains(e)) { errores.Add(e); Log("   !! " + e); } }
        };
        pila = new Stack<IEnumerator>();
        pila.Push(Pruebas());
        EditorApplication.update += Tick;
    }

    static void Tick()
    {
        if (pila == null || pila.Count == 0) return;
        try
        {
            IEnumerator e = pila.Peek();
            if (!e.MoveNext()) { pila.Pop(); if (pila.Count == 0) Fin(); }
            else if (e.Current is IEnumerator hijo) pila.Push(hijo);
        }
        catch (Exception ex) { Log("EXCEPCION " + ex); fallos++; Fin(); }
    }

    static void Fin()
    {
        EditorApplication.update -= Tick;
        pila = new Stack<IEnumerator>();
        try { new SaveStorage().DeleteSlot(3); } catch { }
        Log("=== FIN fallos=" + fallos + " errores=" + errores.Count);
        SessionState.SetBool("TMPTEST", false);
        EditorApplication.Exit(fallos == 0 ? 0 : 2);
    }

    static void Log(string s) { File.AppendAllText(Out + "test.txt", s + "\n"); }
    static void Ok(bool c, string s) { Log((c ? "OK    " : "FALLO ") + s); if (!c) fallos++; }
    static object F(object o, string n) { var f = o.GetType().GetField(n, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance); return f.GetValue(o); }

    static IEnumerator Esperar(Func<bool> c, float seg, string que)
    {
        double fin = EditorApplication.timeSinceStartup + seg;
        while (!c() && EditorApplication.timeSinceStartup < fin) yield return null;
        if (!c()) { Log("   (tiempo agotado: " + que + ")"); }
    }
    static IEnumerator Seg(float s) { double f = EditorApplication.timeSinceStartup + s; while (EditorApplication.timeSinceStartup < f) yield return null; }
    static T U<T>() where T : UnityEngine.Object { return UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include); }
    static bool En(string e) { return SceneManager.GetActiveScene().name == e && SceneManager.GetActiveScene().isLoaded; }
    static void Clic(string n) { var b = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.name == n); if (!b.gameObject.activeInHierarchy || !b.interactable) throw new Exception("boton no pulsable " + n); b.onClick.Invoke(); }
    static bool Pulsable(string n) { var b = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.name == n); return b.gameObject.activeInHierarchy && b.interactable; }
    static string Txt(string n) { return UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(x => x.name == n).GetComponentInChildren<TMP_Text>(true).text.Replace("\n", " / "); }
    static bool Activo(string n) { var g = Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(x => x.name == n && x.gameObject.scene == SceneManager.GetActiveScene()); return g != null && g.gameObject.activeInHierarchy; }

    static IEnumerator IrAMenu()
    {
        SceneManager.LoadScene("MenuPrincipal");
        yield return null; yield return null;
        yield return Esperar(() => En("MenuPrincipal") && U<MenuPrincipalGuardado>() != null, 30, "menu");
        yield return Seg(0.3f);
    }
    static IEnumerator EsperarPiedra()
    {
        yield return Esperar(() => En("Piedra") && U<CambioCamaraIntro>() != null, 120, "Piedra");
        yield return Seg(1.5f);
    }

    static void Cam(string nombre, Vector3 pos, Vector3 mira)
    {
        var go = new GameObject("TmpCam"); var c = go.AddComponent<Camera>();
        c.transform.position = pos; c.transform.LookAt(mira); c.fieldOfView = 60; c.farClipPlane = 2000;
        Render(c, nombre); UnityEngine.Object.Destroy(go);
    }
    static void Render(Camera c, string nombre)
    {
        var prev = c.targetTexture;
        var rt = new RenderTexture(1600, 900, 24); c.targetTexture = rt; c.Render(); RenderTexture.active = rt;
        var t = new Texture2D(1600, 900, TextureFormat.RGB24, false); t.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); t.Apply();
        File.WriteAllBytes(Out + nombre, t.EncodeToPNG()); c.targetTexture = prev; RenderTexture.active = null;
    }

    static void ComprobarCero(string et)
    {
        var eq = U<GestorEquipamiento>();
        Ok(GuardadoPartidaJuego.InicioActual == GuardadoPartidaJuego.TipoInicio.NuevaPartida, et + " inicio=" + GuardadoPartidaJuego.InicioActual);
        Ok(!U<CambioCamaraIntro>().OmitirIntro && !CambioCamaraIntro.IntroTerminada, et + " la animación está en marcha (IntroTerminada=" + CambioCamaraIntro.IntroTerminada + ")");
        Ok(!SecuenciaIntro.TutorialTerminado, et + " tutorial sin hacer; paso=" + U<SecuenciaIntro>().PasoActual);
        Ok(U<Cartera>().Monedas == 0, et + " monedas=" + U<Cartera>().Monedas);
        Ok(!eq.ArmaLanzadoraDesbloqueada() && !eq.TorbellinoDesbloqueado() && !eq.MartilloDesbloqueado(), et + " herramientas bloqueadas");
        Ok(!SistemaConstruccion.Instancia.inventarioEdificios.construccionInfinita, et + " sin construcción infinita");
        Ok(!U<Fisionador>().Desbloqueado, et + " fisionador bloqueado");
        Ok(U<ContadorGasolinera>().FraccionActual == 0f, et + " tanque a 0");
        Ok(UnityEngine.Object.FindObjectsByType<BotRecolector>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0, et + " sin arañas en la escena");
    }

    static IEnumerator Pruebas()
    {
        Log("=== INICIO");
        yield return Esperar(() => U<MenuPrincipalGuardado>() != null, 20, "menu inicial");
        yield return Seg(0.5f);
        var st = new SaveStorage();
        Log("slots: " + string.Join(",", Enumerable.Range(1, 3).Select(i => i + "=" + st.HasValidSave(i))));
        bool s1 = st.HasValidSave(1), s2 = st.HasValidSave(2);

        // MENU
        Ok(Activo("PanelPrincipal") && !Activo("PanelSeleccionRanura") && !Activo("PanelConfirmacion"), "menú: solo se ven los botones principales");
        Ok(Pulsable("BotonContinuar") == (s1 || s2), "Continuar: " + Txt("BotonContinuar"));
        Clic("BotonCargarPartida"); yield return null;
        Ok(!Activo("PanelPrincipal") && Activo("PanelSeleccionRanura"), "Cargar abre la selección");
        for (int i = 1; i <= 3; i++) Ok(Pulsable("Ranura" + i) == st.HasValidSave(i), "cargar: ranura " + i + " pulsable=" + Pulsable("Ranura" + i) + " [" + Txt("Ranura" + i) + "]");
        Clic("Volver"); yield return null;
        Ok(Activo("PanelPrincipal") && !Activo("PanelSeleccionRanura"), "Volver");
        Clic("BotonNuevaPartida"); yield return null;
        for (int i = 1; i <= 3; i++) Ok(Pulsable("Ranura" + i), "nueva: ranura " + i + " pulsable [" + Txt("Ranura" + i) + "]");
        if (s1)
        {
            Clic("Ranura1"); yield return null;
            Ok(Activo("PanelConfirmacion"), "ranura ocupada pide confirmación");
            Clic("BotonCancelar"); yield return null;
            Ok(!Activo("PanelConfirmacion") && st.HasValidSave(1) && En("MenuPrincipal"), "cancelar no borra nada");
        }

        // NUEVA PARTIDA EN 3
        Clic("Ranura3");
        yield return EsperarPiedra();
        ComprobarCero("[nueva 3]");
        Ok(Activo("Cubo Fisionador"), "cubo del fisionador visible");
        Ok(Activo("Cubo Generador Arañas"), "cubo del generador visible");
        var gen = U<GeneradorAranas>(); var fis = U<Fisionador>();
        Ok(gen.GetComponentsInChildren<Renderer>(false).Count(r => r.enabled && r.gameObject.activeInHierarchy) == 0, "generador: modelo oculto");
        Log("   fisionador renderers visibles: " + string.Join(", ", fis.GetComponentsInChildren<Renderer>(false).Where(r => r.enabled).Select(r => r.name + ":" + r.GetType().Name)));
        Ok(!fis.mostrarPanelBloqueado, "fisionador: panel LED oculto bloqueado");
        string txt; bool puede = U<TiendaTrabajo>().PuedeComprarBot(out txt);
        Ok(!puede && txt == "GENERADOR BLOQUEADO", "tienda: no vende arañas (" + txt + ")");
        Render(Camera.main, "intro.png");

        // Adelantar la intro
        Time.timeScale = 8f;
        bool aterrizo = false; CambioCamaraIntro.OnNaveAterrizada += () => aterrizo = true;
        yield return Esperar(() => aterrizo, 150, "aterrizaje");
        Ok(aterrizo, "la nave recorre la animación y aterriza");
        yield return Seg(2f);
        Time.timeScale = 4f;
        ((PalancaTirador)F(U<CambioCamaraIntro>(), "palancaSalida")).AlLlegarAlTope.Invoke();
        yield return Esperar(() => CambioCamaraIntro.IntroTerminada, 60, "fin intro");
        Ok(CambioCamaraIntro.IntroTerminada, "al bajar la palanca termina la intro");
        Time.timeScale = 1f;
        yield return Seg(1f);
        Ok(!SecuenciaIntro.TutorialTerminado && U<SecuenciaIntro>().PasoActual == SecuenciaIntro.Paso.Manguera, "después empieza el tutorial; paso=" + U<SecuenciaIntro>().PasoActual);
        Ok(U<Cartera>().Monedas == 0, "monedas tras la intro=" + U<Cartera>().Monedas);

        Vector3 pf = fis.transform.position, pg = gen.transform.position, medio = (pf + pg) * 0.5f;
        Cam("cubos_general.png", medio + new Vector3(-16, 7, -4), medio);
        Cam("cubo_fis.png", pf + new Vector3(-9, 4, -9), pf);
        Cam("cubo_gen.png", pg + new Vector3(-9, 4, 9), pg);

        // GUARDAR
        U<Cartera>().AnadirMonedas(123);
        var g = U<GuardadoPartidaJuego>();
        Ok(g.RanuraSeleccionada == 3, "ranura activa=" + g.RanuraSeleccionada);
        yield return Esperar(() => U<MovimientoPersonaje>().SueloDetectado && U<CharacterController>().isGrounded, 10, "suelo");
        g.Guardar(); yield return null;
        GameSaveData d; bool r2;
        Ok(st.TryLoad(3, out d, out r2) && d.economy.coins == 123, "guardado en ranura 3 con 123 monedas");
        Vector3 pos = U<MovimientoPersonaje>().transform.position;
        U<Cartera>().AnadirMonedas(1); g.Guardar(); yield return null;
        Ok(st.TryLoad(3, out d, out r2) && d.economy.coins == 124, "segundo guardado sobrescribe la misma partida (124)");

        // CONTINUAR
        yield return IrAMenu();
        Ok(Txt("BotonContinuar").Contains("RANURA 3"), "Continuar apunta a la más reciente: " + Txt("BotonContinuar"));
        Clic("BotonContinuar");
        yield return EsperarPiedra();
        yield return Esperar(() => U<Cartera>().Monedas == 124, 15, "carga 3");
        Ok(U<Cartera>().Monedas == 124, "continuar: monedas=" + U<Cartera>().Monedas);
        Ok(GuardadoPartidaJuego.InicioActual == GuardadoPartidaJuego.TipoInicio.PartidaCargada && CambioCamaraIntro.IntroTerminada, "continuar: sin intro");
        Ok(SecuenciaIntro.TutorialTerminado, "continuar: sin tutorial");
        Ok(Vector3.Distance(U<MovimientoPersonaje>().transform.position, pos) < 1f, "continuar: jugador donde guardó (dist " + Vector3.Distance(U<MovimientoPersonaje>().transform.position, pos).ToString("F2") + ")");
        Ok(!U<GestorEquipamiento>().MartilloDesbloqueado() && !SistemaConstruccion.Instancia.inventarioEdificios.construccionInfinita, "continuar: sin ayudas de prueba");
        Ok(Activo("Cubo Fisionador") && Activo("Cubo Generador Arañas"), "continuar: cubos visibles");
        U<Cartera>().AnadirMonedas(6); U<GuardadoPartidaJuego>().Guardar(); yield return null;
        Ok(st.TryLoad(3, out d, out r2) && d.economy.coins == 130, "guardar sobre la partida cargada (130)");

        // DESBLOQUEOS
        fis = U<Fisionador>(); gen = U<GeneradorAranas>();
        fis.Desbloquear(); gen.Desbloquear(); yield return Seg(0.5f);
        Ok(!Activo("Cubo Fisionador") && fis.GetComponentsInChildren<SkinnedMeshRenderer>(false).Length > 0, "fisionador desbloqueado: se ve la máquina y no el cubo");
        Ok(!Activo("Cubo Generador Arañas") && gen.GetComponentsInChildren<MeshRenderer>(false).Length > 0, "generador desbloqueado: se ve la máquina y no el cubo");
        puede = U<TiendaTrabajo>().PuedeComprarBot(out txt);
        Ok(txt != "GENERADOR BLOQUEADO", "generador desbloqueado: la tienda vuelve a vender (" + txt.Replace("\n", " ") + ")");
        Cam("maquinas_general.png", medio + new Vector3(-16, 7, -4), medio);

        // SOBRESCRIBIR 3 (estáticos sucios de la partida anterior)
        yield return IrAMenu();
        Clic("BotonNuevaPartida"); yield return null;
        Clic("Ranura3"); yield return null;
        Ok(Activo("PanelConfirmacion"), "ranura 3 ocupada pide confirmación");
        Clic("BotonBorrarYEmpezar");
        yield return EsperarPiedra();
        Ok(!st.HasValidSave(3), "la partida anterior se ha borrado");
        ComprobarCero("[nueva 3 bis]");
        Ok(Activo("Cubo Fisionador") && Activo("Cubo Generador Arañas"), "[nueva 3 bis] cubos visibles otra vez");

        // RANURAS ANTIGUAS
        for (int i = 1; i <= 2; i++)
        {
            if (!st.HasValidSave(i)) continue;
            st.TryLoad(i, out d, out r2); int esperado = d.economy.coins;
            yield return IrAMenu();
            Clic("BotonCargarPartida"); yield return null;
            Clic("Ranura" + i);
            yield return EsperarPiedra();
            yield return Esperar(() => (int)F(U<GuardadoPartidaJuego>(), "ranuraActiva") != 0, 12, "carga " + i);
            Ok((int)F(U<GuardadoPartidaJuego>(), "ranuraActiva") == i && U<Cartera>().Monedas == esperado, "cargar ranura " + i + ": monedas=" + U<Cartera>().Monedas + " (esperado " + esperado + ")");
            Ok(CambioCamaraIntro.IntroTerminada, "cargar ranura " + i + ": sin intro");
        }

        // PLAY DIRECTO EN LA ESCENA
        SceneManager.LoadScene("Piedra"); yield return null; yield return null;
        yield return EsperarPiedra();
        Ok(GuardadoPartidaJuego.InicioActual == GuardadoPartidaJuego.TipoInicio.Directo && U<Cartera>().Monedas == 1000 && CambioCamaraIntro.IntroTerminada, "play directo: conserva los valores de prueba (monedas=" + U<Cartera>().Monedas + ", intro saltada=" + CambioCamaraIntro.IntroTerminada + ")");
    }
}
