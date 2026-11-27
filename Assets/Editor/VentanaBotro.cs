using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > PiedrasIncr > Botro: qué dice y cuándo. Todo lo que dice Botro (el único que habla).
///  · Intro: cada paso, en orden, con sus frases (★ = instrucción que se queda en pantalla).
///  · Durante la partida: momentos con su disparador (compra, esfera, piedra nº, minuto...), retraso y "solo una vez".
/// </summary>
public class VentanaBotro : EditorWindow
{
    private const string RutaAsset = "Assets/PiedrasIncr/Dialogue/Resources/GuionBotro.asset";

    private GuionBotro guion;
    private Vector2 scroll;
    private int pestana;
    private string busqueda = "";
    private GUIStyle estiloTexto;
    private GUIStyle estiloTitulo;
    private SecuenciaIntro.Paso[] pasos;
    private string[] nombresPasos;
    private GuionBotro.Cuando[] cuandos;
    private string[] nombresCuandos;
    private string[] idsCompras;
    private string[] nombresCompras;
    private SecuenciaIntro guionEnPlay;

    [MenuItem("Tools/PiedrasIncr/Botro: qué dice y cuándo")]
    public static void Abrir()
    {
        VentanaBotro ventana = GetWindow<VentanaBotro>("Botro");
        ventana.minSize = new Vector2(600f, 440f);
    }

    private void OnEnable()
    {
        guion = AssetDatabase.LoadAssetAtPath<GuionBotro>(RutaAsset);
        if (guion == null) guion = Resources.Load<GuionBotro>(GuionBotro.RutaResources);

        pasos = (SecuenciaIntro.Paso[])Enum.GetValues(typeof(SecuenciaIntro.Paso));
        nombresPasos = new string[pasos.Length];
        for (int i = 0; i < pasos.Length; i++) nombresPasos[i] = (i + 1) + " · " + ObjectNames.NicifyVariableName(pasos[i].ToString());

        cuandos = (GuionBotro.Cuando[])Enum.GetValues(typeof(GuionBotro.Cuando));
        nombresCuandos = new string[cuandos.Length];
        for (int i = 0; i < cuandos.Length; i++) nombresCuandos[i] = GuionBotro.NombreCuando(cuandos[i]);

        // Las mejoras que se pueden comprar (constantes de EconomiaDemo.Ids).
        List<string> ids = new List<string> { "" };
        List<string> nombres = new List<string> { "Cualquiera" };
        foreach (FieldInfo f in typeof(EconomiaDemo.Ids).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (!f.IsLiteral || f.FieldType != typeof(string)) continue;
            string id = (string)f.GetRawConstantValue();
            ids.Add(id);
            nombres.Add(EconomiaDemo.NombreVisible(id));
        }
        idsCompras = ids.ToArray();
        nombresCompras = nombres.ToArray();
    }

    private void Crear()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(RutaAsset));
        guion = CreateInstance<GuionBotro>();
        guion.momentos = GuionBotro.PorDefecto();
        AssetDatabase.CreateAsset(guion, RutaAsset);
        AssetDatabase.SaveAssets();
    }

    private void Cambio(string accion)
    {
        Undo.RecordObject(guion, accion);
        EditorUtility.SetDirty(guion);
    }

    private void OnGUI()
    {
        if (guion == null)
        {
            EditorGUILayout.HelpBox("No existe el guion de Botro (" + RutaAsset + ").", MessageType.Warning);
            if (GUILayout.Button("Crear con el guion por defecto", GUILayout.Height(28))) Crear();
            return;
        }

        if (estiloTexto == null) estiloTexto = new GUIStyle(EditorStyles.textArea) { wordWrap = true, fontSize = 13 };
        if (estiloTitulo == null) estiloTitulo = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        if (!Application.isPlaying) guionEnPlay = null;
        else if (guionEnPlay == null) guionEnPlay = FindFirstObjectByType<SecuenciaIntro>();

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        pestana = GUILayout.Toolbar(pestana, new[] { "Intro (en orden)", "Durante la partida" }, EditorStyles.toolbarButton, GUILayout.Width(260));
        busqueda = EditorGUILayout.TextField(busqueda, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Seleccionar archivo", EditorStyles.toolbarButton))
        {
            Selection.activeObject = guion;
            EditorGUIUtility.PingObject(guion);
        }
        if (GUILayout.Button("Guardar", EditorStyles.toolbarButton)) AssetDatabase.SaveAssets();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(pestana == 0
            ? "Botro es el único que habla. Cada paso de la intro dice sus frases al empezar; ★ = instrucción que se queda en pantalla hasta que la cumples."
            : "Momentos de la partida: elige CUÁNDO habla Botro (compra, esfera, piedra nº, minuto...) y QUÉ dice.", MessageType.None);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        if (pestana == 0)
        {
            for (int i = 0; i < pasos.Length; i++) DibujarPasoIntro(i);
        }
        else
        {
            DibujarMomentosPartida();
        }
        EditorGUILayout.EndScrollView();
    }

    // ===================== INTRO =====================

    private void DibujarPasoIntro(int indice)
    {
        SecuenciaIntro.Paso paso = pasos[indice];
        string nombre = paso.ToString();
        bool ahora = guionEnPlay != null && guionEnPlay.PasoActual == paso;

        List<GuionBotro.Momento> delPaso = new List<GuionBotro.Momento>();
        foreach (GuionBotro.Momento m in guion.momentos)
            if (m != null && m.Disparador == GuionBotro.Cuando.PasoIntro && m.paso == nombre && Pasa(m)) delPaso.Add(m);
        if (busqueda.Length > 0 && delPaso.Count == 0) return;

        EditorGUILayout.Space(6);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(nombresPasos[indice] + (ahora ? "   ◄ AHORA" : ""), estiloTitulo);
        EditorGUILayout.LabelField(GuionBotro.DescripcionPaso(paso), EditorStyles.wordWrappedMiniLabel);

        bool primero = true;
        foreach (GuionBotro.Momento m in delPaso)
        {
            if (DibujarMomento(m, true, primero)) break;
            primero = false;
        }

        if (delPaso.Count == 0 && GUILayout.Button("+ Botro dice algo en este paso", EditorStyles.miniButton))
        {
            Cambio("Nuevo momento");
            guion.momentos.Add(new GuionBotro.Momento(NuevoId("intro_" + nombre), GuionBotro.Cuando.PasoIntro, nombre, "", 0, 0f, true, 0f,
                new GuionBotro.Frase("Nueva frase de Botro")));
        }
        EditorGUILayout.EndVertical();
    }

    // ===================== PARTIDA =====================

    private void DibujarMomentosPartida()
    {
        if (GUILayout.Button("+ Nuevo momento", GUILayout.Height(24)))
        {
            Cambio("Nuevo momento");
            guion.momentos.Add(new GuionBotro.Momento(NuevoId("momento"), GuionBotro.Cuando.Compra, "", "", 0, 0f, true, 0f,
                new GuionBotro.Frase("Nueva frase de Botro")));
        }

        foreach (GuionBotro.Momento m in guion.momentos.ToArray())
        {
            if (m == null || m.Disparador == GuionBotro.Cuando.PasoIntro || !Pasa(m)) continue;
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool cambiado = DibujarMomento(m, false, true);
            EditorGUILayout.EndVertical();
            if (cambiado) break;
        }
    }

    // ===================== MOMENTO =====================

    // Devuelve true si la lista ha cambiado (dejar de dibujar este frame).
    private bool DibujarMomento(GuionBotro.Momento m, bool enIntro, bool seUsa)
    {
        // Cabecera: activo, id, restaurar / borrar
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        bool activo = EditorGUILayout.ToggleLeft(m.id, m.activo, EditorStyles.boldLabel, GUILayout.Width(200));
        if (EditorGUI.EndChangeCheck()) { Cambio("Activar momento"); m.activo = activo; }
        if (enIntro && !seUsa) EditorGUILayout.LabelField("no se usa: ya hay otro en este paso", EditorStyles.miniBoldLabel);
        GUILayout.FlexibleSpace();

        GuionBotro.Momento defecto = GuionBotro.BuscarPorDefecto(m.id);
        if (defecto != null)
        {
            if (GUILayout.Button("Restaurar", EditorStyles.miniButtonLeft, GUILayout.Width(70)))
            {
                Cambio("Restaurar momento");
                int i = guion.momentos.IndexOf(m);
                guion.momentos[i] = defecto.Copia();
                GUI.FocusControl(null);
                EditorGUILayout.EndHorizontal();
                return true;
            }
        }
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button(new GUIContent("▶ Probar", "Botro dice todas sus frases ahora (solo en Play)."),
                    defecto != null ? EditorStyles.miniButtonMid : EditorStyles.miniButtonLeft, GUILayout.Width(64)))
            {
                foreach (GuionBotro.Frase f in m.frases)
                    if (!string.IsNullOrWhiteSpace(f.texto)) BotroDice.Decir("prueba_" + m.id, f.texto, f.duracion);
            }
        }
        if (GUILayout.Button("Borrar", EditorStyles.miniButtonRight, GUILayout.Width(52)) &&
            EditorUtility.DisplayDialog("Borrar momento", "¿Borrar '" + m.id + "' y sus frases?", "Borrar", "Cancelar"))
        {
            Cambio("Borrar momento");
            guion.momentos.Remove(m);
            EditorGUILayout.EndHorizontal();
            return true;
        }
        EditorGUILayout.EndHorizontal();

        // CUÁNDO
        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 50;
        EditorGUI.BeginChangeCheck();
        int iCuando = Mathf.Max(0, Array.IndexOf(cuandos, m.Disparador));
        iCuando = EditorGUILayout.Popup("Cuándo", iCuando, nombresCuandos, GUILayout.MinWidth(220));
        GuionBotro.Cuando cuando = cuandos[iCuando];

        string paso = m.paso, compra = m.compra;
        int numero = m.numero;
        EditorGUIUtility.labelWidth = 46;
        switch (cuando)
        {
            case GuionBotro.Cuando.PasoIntro:
                int iPaso = Mathf.Max(0, Array.IndexOf(nombresPasosCrudos(), m.paso));
                iPaso = EditorGUILayout.Popup("Paso", iPaso, nombresPasos, GUILayout.MinWidth(200));
                paso = pasos[iPaso].ToString();
                break;
            case GuionBotro.Cuando.Compra:
                int iCompra = Mathf.Max(0, Array.IndexOf(idsCompras, m.compra ?? ""));
                iCompra = EditorGUILayout.Popup("Mejora", iCompra, nombresCompras, GUILayout.MinWidth(140));
                compra = idsCompras[iCompra];
                EditorGUIUtility.labelWidth = 40;
                numero = Mathf.Max(0, EditorGUILayout.IntField(new GUIContent("Nivel", "0 = cualquier nivel"), m.numero, GUILayout.Width(80)));
                break;
            case GuionBotro.Cuando.PiedraMetida:
                EditorGUIUtility.labelWidth = 70;
                numero = Mathf.Max(1, EditorGUILayout.IntField("Nº piedra", Mathf.Max(1, m.numero), GUILayout.Width(120)));
                break;
            case GuionBotro.Cuando.MinutoDePartida:
                EditorGUIUtility.labelWidth = 52;
                numero = Mathf.Max(1, EditorGUILayout.IntField("Minuto", Mathf.Max(1, m.numero), GUILayout.Width(110)));
                break;
        }
        EditorGUIUtility.labelWidth = 0;
        if (EditorGUI.EndChangeCheck())
        {
            Cambio("Cuándo habla Botro");
            m.Disparador = cuando;
            m.paso = paso;
            m.compra = compra;
            m.numero = numero;
            EditorGUILayout.EndHorizontal();
            return true; // puede haber cambiado de pestaña o de paso
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUIUtility.labelWidth = 120;
        EditorGUI.BeginChangeCheck();
        float retraso = Mathf.Max(0f, EditorGUILayout.FloatField(new GUIContent("Espera antes (s)", "Segundos desde que ocurre hasta que empieza a hablar."), m.retraso, GUILayout.Width(180)));
        bool unaVez = m.soloUnaVez;
        float repetir = m.repetirInstruccionCada;
        if (!enIntro)
        {
            EditorGUIUtility.labelWidth = 90;
            unaVez = EditorGUILayout.Toggle(new GUIContent("Solo una vez", "Solo la primera vez que ocurre."), m.soloUnaVez, GUILayout.Width(120));
        }
        else if (!GuionBotro.TerminaAlHablar(pasos[Mathf.Max(0, Array.IndexOf(nombresPasosCrudos(), m.paso))]))
        {
            EditorGUIUtility.labelWidth = 170;
            repetir = Mathf.Max(0f, EditorGUILayout.FloatField(new GUIContent("Repetir instrucción cada (s)",
                "Si el jugador tarda, Botro repite la última instrucción. 0 = nunca."), m.repetirInstruccionCada, GUILayout.Width(220)));
        }
        EditorGUIUtility.labelWidth = 0;
        if (EditorGUI.EndChangeCheck())
        {
            Cambio("Ajustes del momento");
            m.retraso = retraso;
            m.soloUnaVez = unaVez;
            m.repetirInstruccionCada = repetir;
        }
        EditorGUILayout.EndHorizontal();

        // QUÉ DICE
        for (int i = 0; i < m.frases.Count; i++)
            if (DibujarFrase(m, i, enIntro)) return true;

        if (GUILayout.Button("+ Frase", EditorStyles.miniButton, GUILayout.Width(70)))
        {
            Cambio("Nueva frase");
            m.frases.Add(new GuionBotro.Frase("Nueva frase de Botro"));
        }
        return false;
    }

    private bool DibujarFrase(GuionBotro.Momento m, int i, bool enIntro)
    {
        GuionBotro.Frase frase = m.frases[i];
        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        bool instruccion = frase.esInstruccion;
        if (enIntro)
            instruccion = GUILayout.Toggle(frase.esInstruccion, new GUIContent("★", "Instrucción: se queda en pantalla hasta que acaba el paso."),
                EditorStyles.miniButton, GUILayout.Width(24), GUILayout.Height(36));
        string texto = EditorGUILayout.TextArea(frase.texto, estiloTexto, GUILayout.MinHeight(36));
        if (EditorGUI.EndChangeCheck())
        {
            Cambio("Editar frase de Botro");
            frase.esInstruccion = instruccion;
            frase.texto = texto;
        }

        EditorGUILayout.BeginVertical(GUILayout.Width(118));
        EditorGUIUtility.labelWidth = 52;
        EditorGUI.BeginChangeCheck();
        float retraso = Mathf.Max(0f, EditorGUILayout.FloatField(new GUIContent("Retraso", "Segundos de espera antes de decirla."), frase.retraso));
        float automatica = DuracionAutomatica(frase.texto);
        float duracion = Mathf.Max(0f, EditorGUILayout.FloatField(new GUIContent("Durac.",
            "Segundos en pantalla si no se omite con G. 0 = automático (" + automatica.ToString("0.0") + " s)."), frase.duracion));
        if (EditorGUI.EndChangeCheck()) { Cambio("Tiempos de la frase"); frase.retraso = retraso; frase.duracion = duracion; }
        EditorGUIUtility.labelWidth = 0;
        if (frase.duracion <= 0f) EditorGUILayout.LabelField("auto ≈ " + automatica.ToString("0.0") + " s", EditorStyles.centeredGreyMiniLabel);
        if (enIntro)
        {
            EditorGUIUtility.labelWidth = 52;
            EditorGUI.BeginChangeCheck();
            GuionBotro.Enfoque enfoque = (GuionBotro.Enfoque)EditorGUILayout.EnumPopup(
                new GUIContent("Enfocar", "La cámara hace zoom a esto mientras Botro dice la frase."), frase.Enfocar);
            if (EditorGUI.EndChangeCheck()) { Cambio("Enfocar"); frase.Enfocar = enfoque; }
            EditorGUIUtility.labelWidth = 0;
        }

        bool cambiada = false;
        EditorGUILayout.BeginHorizontal();
        using (new EditorGUI.DisabledScope(i == 0))
            if (GUILayout.Button("▲", EditorStyles.miniButtonLeft)) { Cambio("Subir frase"); Mover(m, i, i - 1); cambiada = true; }
        using (new EditorGUI.DisabledScope(i == m.frases.Count - 1))
            if (GUILayout.Button("▼", EditorStyles.miniButtonMid)) { Cambio("Bajar frase"); Mover(m, i, i + 1); cambiada = true; }
        using (new EditorGUI.DisabledScope(!Application.isPlaying || string.IsNullOrWhiteSpace(frase.texto)))
            if (GUILayout.Button(new GUIContent("▶", "Botro la dice ahora (solo en Play)."), EditorStyles.miniButtonMid))
                BotroDice.Decir("prueba_frase", frase.texto, frase.duracion);
        if (GUILayout.Button("✕", EditorStyles.miniButtonRight)) { Cambio("Borrar frase"); m.frases.RemoveAt(i); cambiada = true; }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        return cambiada;
    }

    // ===================== AYUDAS =====================

    // Misma fórmula que BotroDice (segundos base + por letra), con los valores del BotroDice de la escena.
    private BotroDice botroDice;
    private bool botroBuscado;

    private float DuracionAutomatica(string texto)
    {
        if (!botroBuscado || (botroDice == null && Application.isPlaying))
        {
            botroDice = BotroDice.Instancia != null ? BotroDice.Instancia : FindFirstObjectByType<BotroDice>();
            botroBuscado = true;
        }
        int letras = texto != null ? texto.Length : 0;
        return botroDice != null ? botroDice.DuracionAutomatica(texto) : 1.8f + letras * 0.055f;
    }

    private void OnHierarchyChange()
    {
        botroBuscado = false;
    }

    private string[] crudos;
    private string[] nombresPasosCrudos()
    {
        if (crudos == null || crudos.Length != pasos.Length)
        {
            crudos = new string[pasos.Length];
            for (int i = 0; i < pasos.Length; i++) crudos[i] = pasos[i].ToString();
        }
        return crudos;
    }

    private bool Pasa(GuionBotro.Momento m)
    {
        string filtro = busqueda.Trim().ToLowerInvariant();
        if (filtro.Length == 0) return true;
        if (Contiene(m.id, filtro)) return true;
        foreach (GuionBotro.Frase f in m.frases)
            if (f != null && Contiene(f.texto, filtro)) return true;
        return false;
    }

    private static bool Contiene(string texto, string filtro)
    {
        return !string.IsNullOrEmpty(texto) && texto.ToLowerInvariant().Contains(filtro);
    }

    private string NuevoId(string baseId)
    {
        if (guion.Buscar(baseId) == null) return baseId;
        int n = 2;
        while (guion.Buscar(baseId + "_" + n) != null) n++;
        return baseId + "_" + n;
    }

    private static void Mover(GuionBotro.Momento m, int desde, int hasta)
    {
        GuionBotro.Frase frase = m.frases[desde];
        m.frases.RemoveAt(desde);
        m.frases.Insert(hasta, frase);
        GUI.FocusControl(null);
    }

    private void OnInspectorUpdate()
    {
        if (Application.isPlaying) Repaint(); // para el "◄ AHORA"
    }
}
