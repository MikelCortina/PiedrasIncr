using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// F10 · Ritmo de la demo.
///
/// 1. Apunta el minuto de cada compra (EventosEconomia.OnCompraRealizada) y de los hitos:
///    primera patada, primera moneda, fin de la intro, cada esfera perfecta, misil lanzado y final.
/// 2. Escribe un CSV con las columnas de la hoja PROGRESION (Paso; Compra; Nivel; Coste; Minuto de la demo)
///    en persistentDataPath/PiedrasIncr/Ritmo/. Se reescribe en cada evento, así nunca se pierde.
/// 3. Solo en el Editor y en Development Build: panel con el minuto, las compras hechas / 28,
///    la siguiente compra prevista con su desvío (verde ≤ 1,5 min · amarillo ≤ 2,5 · rojo) y el
///    semáforo de la hoja DISEÑO DEMO. Se muestra u oculta con F7.
///
/// Se crea solo en cualquier escena con SistemaConstruccion (no hace falta tocar la escena).
/// El minuto de la demo es el tiempo jugado de la partida (incluye lo guardado si se carga).
/// </summary>
public class RegistroRitmoDemo : MonoBehaviour
{
    [Header("CSV")]
    public bool guardarCsv = true;

    [Header("Panel de desarrollo (Editor / Development Build)")]
    public bool mostrarPanel = true;
    public KeyCode teclaPanel = KeyCode.F7;

    [Header("Semáforo (hoja DISEÑO DEMO)")]
    public float minutoMaximoTutorial = 2f;
    public float segundosMaximosPrimeraCompra = 60f;
    public float minutosMaximosEspera = 2.5f;
    public float minutoMaximoTorbellino = 5f;
    public float minutoMaximoCinta = 10f;
    public float minutoMaximoArmaLunar = 16f;
    public int comprasMinimas = 20;
    public float duracionObjetivo = 30f;
    public float margenDuracion = 5f;
    [Tooltip("Inicio del juego libre si el tutorial no avisa (minutos). En el Excel la intro dura 2 min.")]
    public float inicioJuegoLibrePorDefecto = 2f;

    [Header("Desvío respecto a PROGRESION (minutos)")]
    public float desvioVerde = 1.5f;
    public float desvioAmarillo = 2.5f;

    public enum Estado { Pendiente, Bien, Mal }

    public struct Regla
    {
        public string nombre;
        public Estado estado;
        public string detalle;
    }

    private class Fila
    {
        public string paso;
        public string compra;
        public string nivel;
        public string coste;
        public float minuto;
    }

    private readonly List<Fila> filas = new List<Fila>();
    private readonly List<float> minutosCompras = new List<float>();
    private readonly HashSet<string> pasosHechos = new HashSet<string>();

    private float minutoInicioSesion;
    private float minutoTutorial = -1f;
    private float minutoPrimeraCompra = -1f;
    private float minutoTorbellino = -1f;
    private float minutoCinta = -1f;
    private float minutoArmaLunar = -1f;
    private float minutoFinal = -1f;
    private float mayorEspera;
    private int comprasDelOrden;
    private int comprasExtra;
    private int esferas;
    private bool primeraPatada;
    private bool primeraMoneda;
    private int monedasPrevias;

    private Cartera cartera;
    private GestorFasesLunares luna;
    private GuardadoPartidaJuego guardado;
    private string rutaCsv;
    private bool panelVisible = true;
    private GUIStyle estiloPanel;

    public string RutaCsv => rutaCsv;

    // =====================================================
    // CREACIÓN AUTOMÁTICA
    // =====================================================

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Instalar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        CrearSiHaceFalta();
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        CrearSiHaceFalta();
    }

    private static void CrearSiHaceFalta()
    {
        if (FindAnyObjectByType<RegistroRitmoDemo>() != null) return;
        if (FindAnyObjectByType<SistemaConstruccion>() == null) return; // solo en la escena de juego
        new GameObject("RegistroRitmoDemo").AddComponent<RegistroRitmoDemo>();
    }

    // =====================================================
    // CICLO DE VIDA
    // =====================================================

    private void OnEnable()
    {
        EventosEconomia.OnCompraRealizada += AlComprar;
        DesgastePiedra.OnAlgunaEsferaPerfecta += AlConseguirEsfera;
        SistemaPatada.OnPiedraPateada += AlPatear;
        SecuenciaIntro.OnTutorialTerminado += AlTerminarTutorial;
        PiedraNivel2.OnPiedraNivel2Creada += AlEmpezarFinal;
    }

    private void OnDisable()
    {
        EventosEconomia.OnCompraRealizada -= AlComprar;
        DesgastePiedra.OnAlgunaEsferaPerfecta -= AlConseguirEsfera;
        SistemaPatada.OnPiedraPateada -= AlPatear;
        SecuenciaIntro.OnTutorialTerminado -= AlTerminarTutorial;
        PiedraNivel2.OnPiedraNivel2Creada -= AlEmpezarFinal;
        if (cartera != null) cartera.OnMonedasCambiadas -= AlCambiarMonedas;
        if (luna != null) luna.OnFaseCambiada -= AlCambiarFase;
    }

    private void Start()
    {
        guardado = FindAnyObjectByType<GuardadoPartidaJuego>();
        cartera = FindAnyObjectByType<Cartera>();
        luna = FindAnyObjectByType<GestorFasesLunares>();

        if (cartera != null)
        {
            monedasPrevias = cartera.Monedas;
            cartera.OnMonedasCambiadas += AlCambiarMonedas;
        }
        if (luna != null) luna.OnFaseCambiada += AlCambiarFase;

        minutoInicioSesion = MinutoActual;
        if (SecuenciaIntro.TutorialTerminado) minutoTutorial = minutoInicioSesion;

        string carpeta = Path.Combine(Application.persistentDataPath, "PiedrasIncr", "Ritmo");
        rutaCsv = Path.Combine(carpeta, "ritmo_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".csv");

        Hito("Inicio de la sesión");
        Debug.Log("[RITMO] Registrando el ritmo en " + rutaCsv);
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(teclaPanel)) panelVisible = !panelVisible;
#endif
    }

    private void OnApplicationQuit()
    {
        EscribirCsv();
    }

    // =====================================================
    // TIEMPO
    // =====================================================

    /// <summary>Minuto de la demo: tiempo jugado de la partida (incluye lo guardado).</summary>
    public float MinutoActual
    {
        get
        {
            double segundos = guardado != null ? guardado.SegundosJugados : Time.timeSinceLevelLoad;
            return (float)(segundos / 60.0);
        }
    }

    private float InicioJuegoLibre => minutoTutorial >= 0f ? minutoTutorial : inicioJuegoLibrePorDefecto;

    // =====================================================
    // EVENTOS
    // =====================================================

    private void AlComprar(string id, int nivel, int coste)
    {
        float minuto = MinutoActual;
        int paso = BuscarPaso(id, nivel);
        string clave = id + "#" + nivel;

        if (paso > 0 && pasosHechos.Add(clave)) comprasDelOrden++;
        else if (paso <= 0) comprasExtra++;

        // Esperas entre compras (desde el inicio del juego libre).
        float anterior = minutosCompras.Count > 0 ? minutosCompras[minutosCompras.Count - 1] : Mathf.Max(minutoInicioSesion, InicioJuegoLibre);
        mayorEspera = Mathf.Max(mayorEspera, minuto - anterior);
        minutosCompras.Add(minuto);

        if (minutoPrimeraCompra < 0f) minutoPrimeraCompra = minuto;
        if (id == EconomiaDemo.Ids.Torbellino && minutoTorbellino < 0f) minutoTorbellino = minuto;
        if (id == EconomiaDemo.Ids.Cinta && minutoCinta < 0f) minutoCinta = minuto;
        if (id == EconomiaDemo.Ids.ArmaLunar && minutoArmaLunar < 0f) minutoArmaLunar = minuto;

        filas.Add(new Fila
        {
            paso = paso > 0 ? paso.ToString(CultureInfo.InvariantCulture) : "extra",
            compra = EconomiaDemo.NombreVisible(id),
            nivel = nivel.ToString(CultureInfo.InvariantCulture),
            coste = coste.ToString(CultureInfo.InvariantCulture) + (id == EconomiaDemo.Ids.MisilGrande ? " esferas" : ""),
            minuto = minuto
        });
        EscribirCsv();
    }

    private void AlConseguirEsfera(DesgastePiedra piedra)
    {
        esferas++;
        Hito("Esfera perfecta " + esferas);
    }

    private void AlPatear(Rigidbody piedra)
    {
        if (primeraPatada) return;
        primeraPatada = true;
        Hito("Primera patada");
    }

    private void AlCambiarMonedas(int total)
    {
        if (!primeraMoneda && total > monedasPrevias)
        {
            primeraMoneda = true;
            Hito("Primera moneda");
        }
        monedasPrevias = total;
    }

    private void AlTerminarTutorial()
    {
        if (minutoTutorial < 0f) minutoTutorial = MinutoActual;
        Hito("Fin de la intro");
    }

    private void AlCambiarFase(int fase)
    {
        // También salta al cargar una partida (se restaura la fase).
        Hito("La luna pasa a la fase " + fase);
    }

    private void AlEmpezarFinal(PiedraNivel2 piedra)
    {
        if (minutoFinal >= 0f) return;
        minutoFinal = MinutoActual;
        Hito("Final: caen piedras de nivel 2");
    }

    private void Hito(string texto)
    {
        filas.Add(new Fila { paso = "", compra = "[Hito] " + texto, nivel = "", coste = "", minuto = MinutoActual });
        EscribirCsv();
    }

    // =====================================================
    // PROGRESION
    // =====================================================

    private static int BuscarPaso(string id, int nivel)
    {
        List<PasoCompra> orden = ProveedorEconomia.Actual.ordenCompras;
        if (orden == null) return 0;
        for (int i = 0; i < orden.Count; i++)
            if (orden[i] != null && orden[i].id == id && orden[i].nivel == nivel)
                return i + 1;
        return 0;
    }

    /// <summary>Pasos de la PROGRESION ya cumplidos según el estado real (vale también tras cargar).</summary>
    public int PasosCumplidos(out int total)
    {
        List<PasoCompra> orden = ProveedorEconomia.Actual.ordenCompras;
        total = orden != null ? orden.Count : 0;
        int hechos = 0;
        if (orden == null) return 0;
        foreach (PasoCompra paso in orden)
        {
            if (paso == null) continue;
            if (pasosHechos.Contains(paso.id + "#" + paso.nivel) || NivelesMejoras.Nivel(paso.id) >= paso.nivel)
                hechos++;
        }
        return hechos;
    }

    // =====================================================
    // SEMÁFORO
    // =====================================================

    public List<Regla> Semaforo()
    {
        float ahora = MinutoActual;
        List<Regla> reglas = new List<Regla>();

        reglas.Add(PorMinuto("Tutorial ≤ " + F(minutoMaximoTutorial) + " min", minutoTutorial, minutoMaximoTutorial, ahora));

        if (minutoPrimeraCompra >= 0f)
        {
            float segundos = (minutoPrimeraCompra - InicioJuegoLibre) * 60f;
            reglas.Add(new Regla
            {
                nombre = "Primera compra ≤ " + segundosMaximosPrimeraCompra.ToString("0") + " s de juego libre",
                estado = segundos <= segundosMaximosPrimeraCompra ? Estado.Bien : Estado.Mal,
                detalle = Mathf.Max(0f, segundos).ToString("0") + " s"
            });
        }
        else
        {
            float segundos = (ahora - InicioJuegoLibre) * 60f;
            reglas.Add(new Regla
            {
                nombre = "Primera compra ≤ " + segundosMaximosPrimeraCompra.ToString("0") + " s de juego libre",
                estado = segundos > segundosMaximosPrimeraCompra ? Estado.Mal : Estado.Pendiente,
                detalle = "aún no"
            });
        }

        float ultima = minutosCompras.Count > 0 ? minutosCompras[minutosCompras.Count - 1] : Mathf.Max(minutoInicioSesion, InicioJuegoLibre);
        float esperaActual = minutoFinal >= 0f || ahora < InicioJuegoLibre ? 0f : ahora - ultima;
        float peor = Mathf.Max(mayorEspera, esperaActual);
        reglas.Add(new Regla
        {
            nombre = "Ninguna espera > " + F(minutosMaximosEspera) + " min",
            estado = peor > minutosMaximosEspera ? Estado.Mal : (minutoFinal >= 0f ? Estado.Bien : Estado.Pendiente),
            detalle = "máx " + F(peor) + " · ahora " + F(esperaActual)
        });

        reglas.Add(PorMinuto("Torbellino ≤ min " + F(minutoMaximoTorbellino), minutoTorbellino, minutoMaximoTorbellino, ahora));
        reglas.Add(PorMinuto("Cinta ≤ min " + F(minutoMaximoCinta), minutoCinta, minutoMaximoCinta, ahora));
        reglas.Add(PorMinuto("Arma Lunar ≤ min " + F(minutoMaximoArmaLunar), minutoArmaLunar, minutoMaximoArmaLunar, ahora));

        int total;
        int hechos = PasosCumplidos(out total);
        reglas.Add(new Regla
        {
            nombre = "≥ " + comprasMinimas + " compras",
            estado = hechos >= comprasMinimas ? Estado.Bien : (minutoFinal >= 0f ? Estado.Mal : Estado.Pendiente),
            detalle = hechos + " / " + total + (comprasExtra > 0 ? " (+" + comprasExtra + " extra)" : "")
        });

        float minimo = duracionObjetivo - margenDuracion, maximo = duracionObjetivo + margenDuracion;
        Regla duracion = new Regla { nombre = "Duración " + F(duracionObjetivo) + " ± " + F(margenDuracion) + " min" };
        if (minutoFinal >= 0f)
        {
            duracion.estado = minutoFinal >= minimo && minutoFinal <= maximo ? Estado.Bien : Estado.Mal;
            duracion.detalle = F(minutoFinal) + " min";
        }
        else
        {
            duracion.estado = ahora > maximo ? Estado.Mal : Estado.Pendiente;
            duracion.detalle = "en curso " + F(ahora);
        }
        reglas.Add(duracion);

        return reglas;
    }

    private static Regla PorMinuto(string nombre, float minuto, float limite, float ahora)
    {
        if (minuto >= 0f)
            return new Regla { nombre = nombre, estado = minuto <= limite ? Estado.Bien : Estado.Mal, detalle = "min " + F(minuto) };
        return new Regla { nombre = nombre, estado = ahora > limite ? Estado.Mal : Estado.Pendiente, detalle = "aún no" };
    }

    // Coma decimal sin depender de que la plataforma tenga la cultura es-ES instalada.
    private static readonly NumberFormatInfo Decimales = new NumberFormatInfo { NumberDecimalSeparator = ",", NegativeSign = "-" };

    private static string F(float valor)
    {
        return valor.ToString("0.0", Decimales);
    }

    // =====================================================
    // CSV
    // =====================================================

    private void EscribirCsv()
    {
        if (!guardarCsv || string.IsNullOrEmpty(rutaCsv)) return;

        try
        {
            // Punto y coma y coma decimal: se abre bien en Excel en español.
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("Paso;Compra;Nivel;Coste;Minuto de la demo");
            foreach (Fila f in filas)
            {
                csv.Append(f.paso).Append(';')
                   .Append(Limpiar(f.compra)).Append(';')
                   .Append(f.nivel).Append(';')
                   .Append(f.coste).Append(';')
                   .AppendLine(F(f.minuto));
            }

            csv.AppendLine();
            csv.AppendLine("Semáforo;Estado;Detalle");
            foreach (Regla regla in Semaforo())
                csv.Append(Limpiar(regla.nombre)).Append(';').Append(regla.estado).Append(';').AppendLine(Limpiar(regla.detalle));

            Directory.CreateDirectory(Path.GetDirectoryName(rutaCsv));
            File.WriteAllText(rutaCsv, csv.ToString(), new UTF8Encoding(true));
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[RITMO] No se pudo escribir el CSV: " + ex.Message);
        }
    }

    private static string Limpiar(string texto)
    {
        return string.IsNullOrEmpty(texto) ? "" : texto.Replace(';', ',').Replace('\n', ' ');
    }

    // =====================================================
    // PANEL (solo desarrollo)
    // =====================================================

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void OnGUI()
    {
        if (!mostrarPanel || !panelVisible) return;

        if (estiloPanel == null)
        {
            estiloPanel = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13, wordWrap = false };
            estiloPanel.normal.textColor = Color.white;
        }

        float ahora = MinutoActual;
        int total;
        int hechos = PasosCumplidos(out total);
        StringBuilder t = new StringBuilder();
        t.Append("<b>RITMO DEMO</b>  (").Append(teclaPanel).Append(" oculta)\n");
        t.Append("Minuto ").Append(F(ahora)).Append("   ·   compras ").Append(hechos).Append(" / ").Append(total).Append('\n');

        PasoCompra siguiente = NivelesMejoras.SiguientePaso(ProveedorEconomia.Actual);
        if (siguiente != null)
        {
            float desvio = ahora - siguiente.minutoPrevisto;
            string color = Mathf.Abs(desvio) <= desvioVerde ? "#5DE36A" : (Mathf.Abs(desvio) <= desvioAmarillo ? "#F2D14B" : "#FF5A5A");
            int indice = ProveedorEconomia.Actual.ordenCompras.IndexOf(siguiente) + 1;
            t.Append("Siguiente: ").Append(indice).Append(". ").Append(EconomiaDemo.NombreVisible(siguiente.id)).Append(' ').Append(siguiente.nivel)
             .Append(" (min ").Append(F(siguiente.minutoPrevisto)).Append(")  <color=").Append(color).Append(">")
             .Append(desvio >= 0f ? "+" : "").Append(F(desvio)).Append(" min</color>\n");
        }
        else
        {
            t.Append("Siguiente: <color=#5DE36A>PROGRESION completa</color>\n");
        }

        t.Append('\n');
        foreach (Regla regla in Semaforo())
        {
            string color = regla.estado == Estado.Bien ? "#5DE36A" : (regla.estado == Estado.Mal ? "#FF5A5A" : "#9A9A9A");
            string marca = regla.estado == Estado.Bien ? "●" : (regla.estado == Estado.Mal ? "●" : "○");
            t.Append("<color=").Append(color).Append(">").Append(marca).Append("</color> ").Append(regla.nombre)
             .Append("  <color=#BBBBBB>").Append(regla.detalle).Append("</color>\n");
        }

        string texto = t.ToString();
        Vector2 tamano = estiloPanel.CalcSize(new GUIContent(texto));
        Rect r = new Rect(Screen.width - tamano.x - 30f, 20f, tamano.x + 20f, tamano.y + 16f);
        Color anterior = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = anterior;
        GUI.Label(new Rect(r.x + 10f, r.y + 8f, tamano.x, tamano.y), texto, estiloPanel);
    }
#endif
}
