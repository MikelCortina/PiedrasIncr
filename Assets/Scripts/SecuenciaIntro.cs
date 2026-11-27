using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guion de la INTRODUCCIÓN (objetivo ≤ 2 min). Todo te lo dice Botro.
/// Este script decide CUÁNDO empieza y acaba cada paso (por EVENTOS del juego, no por tiempo);
/// lo que dice Botro en cada paso está en GuionBotro (Tools > PiedrasIncr > Botro: qué dice y cuándo).
///  1 Llegada               (CambioCamaraIntro.OnNaveAterrizada)
///  2 Abrir la puerta       (CambioCamaraIntro.OnPuertaAbierta / OnIntroTerminada)
///  3 Patear la piedra que ha caído delante de la nave (MeteoritoInicial.PiedraLanzada; SistemaPatada.OnPiedraPateada)
///  4 Llevarla hasta Botro  (piedra y jugador cerca de Botro)
///  5 Botro explica         (termina cuando acaba de hablar)
///  6 Llevarla a la trampilla desgastándola (una piedra cerca del agujero)
///    └ Manguera, EN PARALELO: tras la primera patada de vuelta, si no está enchufada (marca la manguera)
///  7 Meterla               (AgujeroSimple.OnPiedraTragada)
///  8 Zoom a la gasolina    (desactivado por defecto: 'hacerZoomGasolina')
///  9 Recoger la moneda     (tras repostar; marca la moneda; Cartera.OnMonedasCambiadas)
/// 10 Reconstruir el generador de meteoritos (marca el generador; ConstruccionDianaBloopMonedas.EstaConstruida)
/// 11 Generador reconstruido: empieza al repararlo (marca la diana; termina al golpearla: DianaBloop.OnActivada)
/// Cada frase puede "Enfocar" algo: la cámara hace zoom mientras Botro la dice.
/// Si el jugador se adelanta, el paso se salta (y Botro no dice sus frases pendientes).
/// </summary>
public class SecuenciaIntro : MonoBehaviour
{
    // GuionBotro guarda los pasos por NOMBRE: renombrar un paso obliga a renombrarlo también en el guion.
    public enum Paso
    {
        EsperandoLlegada, AbrirPuerta, PatearPiedra, LlevarPiedraABotro, BotroExplica, LlevarATrampilla, Manguera,
        MeterPiedra, ZoomGasolina, RecogerRestos, ReconstruirGenerador, GeneradorReconstruido, Terminado
    }

    /// <summary>Cambio de paso (para UI, analíticas o pruebas).</summary>
    public static event Action<Paso> OnPasoCambiado;
    /// <summary>Intro completa: a partir de aquí hablan los hitos del intermedio.</summary>
    public static event Action OnTutorialTerminado;
    public static bool TutorialTerminado { get; private set; }

    [Header("General")]
    [Tooltip("Marca para no hacer el tutorial (partida cargada, pruebas).")]
    public bool omitirTutorial = false;
    [Tooltip("Escribe en la consola cada paso del guion ([GUION]).")]
    public bool registrarEnConsola = true;

    // Lo que dice Botro en cada paso está en GuionBotro (Tools > PiedrasIncr > Botro: qué dice y cuándo).

    [Header("Botro")]
    [Tooltip("El robot de la escena. Vacío = se busca el objeto llamado 'Botro'.")]
    public Transform botro;
    public string nombreObjetoBotro = "Botro";
    [Tooltip("Distancia (en horizontal) de la piedra a Botro para entregársela.")]
    public float distanciaEntregaBotro = 6f;
    [Tooltip("Distancia (en horizontal) del jugador a Botro para que empiece a explicar.")]
    public float distanciaJugadorBotro = 9f;
    [Tooltip("Segundos máximos esperando a que caiga la piedra; si no cae, vale cualquier piedra.")]
    public float esperaMaximaPiedraCaida = 6f;

    [Header("Distancias")]
    [Tooltip("Distancia de una piedra al agujero para que Botro hable de la trampilla.")]
    public float distanciaPiedraAgujero = 12f;

    [Header("Marcador del objetivo")]
    [Tooltip("Rombo en pantalla sobre la piedra, Botro o la trampilla, con la distancia.")]
    public bool mostrarMarcador = true;

    [Header("Zoom a la pantalla de gasolina")]
    [Tooltip("Al meter la primera piedra, la cámara hace zoom a la pantalla de la gasolinera. Desactivado: no hay zoom.")]
    public bool hacerZoomGasolina = false;
    [Tooltip("Vacío = se busca sola: el objeto que muestra la imagen de la cámara del cartel de gasoil.")]
    public Transform pantallaGasolina;
    [Tooltip("FOV máximo durante el zoom (se reduce más si hace falta para encuadrar la pantalla).")]
    public float fovZoom = 30f;
    [Tooltip("FOV mínimo del zoom, para que no se acerque demasiado.")]
    public float fovMinimoZoom = 8f;
    [Tooltip("Margen alrededor de la pantalla (1 = justa).")]
    public float margenEncuadre = 1.6f;
    public float duracionIdaVuelta = 0.6f;
    [Tooltip("Segundos mirando el +XX L.")]
    public float segundosMirando = 2.5f;
    [Tooltip("Scripts que se desactivan durante el zoom. Vacío = la mirada (MenuBotUI) y el movimiento.")]
    public MonoBehaviour[] bloquearDuranteZoom;

    [Header("Referencias (vacío = se buscan solas)")]
    public CambioCamaraIntro intro;
    public AgujeroSimple agujero;
    public ContadorGasolinera gasolinera;
    public Cartera cartera;
    public Transform jugador;
    [Tooltip("El generador de meteoritos (empieza destruido). Vacío = se busca.")]
    public ConstruccionDianaBloopMonedas generador;
    public AgarrarManguera manguera;

    [Header("Enfoque de cámara (opción 'Enfocar' de cada frase)")]
    [Tooltip("FOV máximo al enfocar algo mientras Botro habla (se reduce si hace falta para encuadrarlo).")]
    public float fovMaximoEnfoque = 35f;
    [Tooltip("Tras meter la piedra, segundos máximos esperando a que suba la gasolina antes del paso de la moneda.")]
    public float esperaMaximaRepostaje = 3f;

    [Header("Estilo (vacío = el del escáner de piedras)")]
    [Tooltip("Fuente con tildes y eñes para el objetivo. Vacío = la fuente por defecto.")]
    public Font fuenteTexto;
    public float escala = 1f;

    public Paso PasoActual { get; private set; } = Paso.EsperandoLlegada;

    private const string EvLlegada = "llegada";
    private const string EvPuerta = "puerta";
    private const string EvIntroFin = "intro_fin";
    private const string EvManguera = "manguera";
    private const string EvPatada = "patada";
    private const string EvTragada = "tragada";
    private const string EvGasolina = "gasolina";
    private const string EvMoneda = "moneda";
    private const string EvDiana = "diana";

    private readonly HashSet<string> ocurrido = new HashSet<string>();
    // Instrucción de Botro que se queda en pantalla hasta que acaba el paso.
    private string objetivo = "";
    private int contadorFrases;
    private int contadorPatadas;
    // Manguera en paralelo: su instrucción manda mientras esté pendiente; la del paso se guarda para después.
    private bool mangueraPendiente;
    private string instruccionPaso = "";
    private string instruccionManguera = "";
    private float objetivoDesde;
    private int monedasPrevias;
    private GUIStyle estiloLED;
    private GUIStyle estiloTexto;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEventos()
    {
        OnPasoCambiado = null;
        OnTutorialTerminado = null;
        TutorialTerminado = false;
    }

    private void OnEnable()
    {
        CambioCamaraIntro.OnNaveAterrizada += AlAterrizar;
        CambioCamaraIntro.OnPuertaAbierta += AlAbrirPuerta;
        CambioCamaraIntro.OnIntroTerminada += AlTerminarIntro;
        AgarrarManguera.OnMangueraConectada += AlConectarManguera;
        SistemaPatada.OnPiedraPateada += AlPatear;
        AgujeroSimple.OnPiedraTragada += AlTragar;
        ContadorGasolinera.OnGasolinaAnadida += AlAnadirGasolina;
        BotroDice.OnLineaTerminada += AlTerminarLinea;
        DianaBloop.OnActivada += AlActivarDiana;
    }

    private void OnDisable()
    {
        RestaurarZoom();
        CambioCamaraIntro.OnNaveAterrizada -= AlAterrizar;
        CambioCamaraIntro.OnPuertaAbierta -= AlAbrirPuerta;
        CambioCamaraIntro.OnIntroTerminada -= AlTerminarIntro;
        AgarrarManguera.OnMangueraConectada -= AlConectarManguera;
        SistemaPatada.OnPiedraPateada -= AlPatear;
        AgujeroSimple.OnPiedraTragada -= AlTragar;
        ContadorGasolinera.OnGasolinaAnadida -= AlAnadirGasolina;
        BotroDice.OnLineaTerminada -= AlTerminarLinea;
        DianaBloop.OnActivada -= AlActivarDiana;
        if (cartera != null) cartera.OnMonedasCambiadas -= AlCambiarMonedas;
    }

    private void Awake()
    {
        // Es estático: cada partida empieza con el tutorial sin hacer.
        TutorialTerminado = false;
    }

    private void AlAterrizar() { Marcar(EvLlegada); }
    private void AlAbrirPuerta() { Marcar(EvLlegada); Marcar(EvPuerta); }
    private void AlTerminarIntro() { Marcar(EvLlegada); Marcar(EvPuerta); Marcar(EvIntroFin); }
    private void AlConectarManguera() { Marcar(EvManguera); }
    private void AlPatear(Rigidbody piedra) { contadorPatadas++; Marcar(EvPatada); }
    private void AlTragar(AgujeroSimple a, float pureza, float gasolina) { Marcar(EvTragada); }
    private void AlAnadirGasolina(float fraccion, float litros) { Marcar(EvGasolina); }
    private void AlTerminarLinea(string id) { Marcar("linea:" + id); }
    private void AlActivarDiana(DianaBloop diana) { if (!GeneradorConstruido() || generador == null) return; Marcar(EvDiana); }

    private void AlCambiarMonedas(int total)
    {
        if (total > monedasPrevias) Marcar(EvMoneda);
        monedasPrevias = total;
    }

    private void Marcar(string evento)
    {
        if (ocurrido.Add(evento)) Registrar("Evento: " + evento);
    }
    private bool Ocurrio(string evento) { return ocurrido.Contains(evento); }

    private void Start()
    {
        if (intro == null) intro = FindFirstObjectByType<CambioCamaraIntro>();
        if (agujero == null) agujero = FindFirstObjectByType<AgujeroSimple>();
        if (gasolinera == null) gasolinera = FindFirstObjectByType<ContadorGasolinera>();
        if (cartera == null) cartera = FindFirstObjectByType<Cartera>();
        if (jugador == null)
        {
            GameObject j = GameObject.FindGameObjectWithTag("Player");
            if (j != null) jugador = j.transform;
        }
        BuscarBotro();
        if (generador == null) generador = FindFirstObjectByType<ConstruccionDianaBloopMonedas>();
        if (manguera == null) manguera = FindFirstObjectByType<AgarrarManguera>();
        if (cartera != null)
        {
            monedasPrevias = cartera.Monedas;
            cartera.OnMonedasCambiadas += AlCambiarMonedas;
        }

        // Sin intro (o intro saltada) no llegarán sus eventos.
        if (intro == null || !intro.isActiveAndEnabled || intro.OmitirIntro || CambioCamaraIntro.IntroTerminada)
        {
            Marcar(EvLlegada);
            Marcar(EvPuerta);
            if (intro == null || CambioCamaraIntro.IntroTerminada) Marcar(EvIntroFin);
        }

        Registrar("Iniciado. Intro: " + (intro == null ? "no encontrada" : (intro.OmitirIntro ? "saltada" : "normal")) +
                  " | Jugador: " + (jugador != null ? jugador.name : "NO ENCONTRADO") +
                  " | Agujero: " + (agujero != null) + " | Gasolinera: " + (gasolinera != null) +
                  " | Radio de Botro: " + (BotroDice.Instancia != null) + " | Botro: " + (botro != null ? botro.name : "NO ENCONTRADO"));

        if (omitirTutorial || GuardadoPartidaJuego.InicioActual == GuardadoPartidaJuego.TipoInicio.PartidaCargada)
        {
            Terminar();
            return;
        }

        StartCoroutine(Guion());
    }

    // =====================================================
    // GUION
    // =====================================================

    private IEnumerator Guion()
    {
        // 1 · Llegas al planeta
        yield return EjecutarPaso(Paso.EsperandoLlegada, () => Ocurrio(EvLlegada) || JugadorTieneControl());

        // 2 · Abres la puerta (bajas la palanca y la intro devuelve el control)
        if (!JugadorTieneControl())
            yield return EjecutarPaso(Paso.AbrirPuerta, () => Ocurrio(EvPuerta) || JugadorTieneControl());
        yield return new WaitUntil(() => Ocurrio(EvIntroFin) || JugadorTieneControl());

        // La piedra cae delante de la nave al final del vuelo; se espera a que toque el suelo.
        LanzarPiedraSiFalta();
        float limite = Time.time + esperaMaximaPiedraCaida;
        yield return new WaitUntil(() => PiedraCaidaEnElSuelo() || Time.time > limite);

        // 3 · Botro te pide la piedra: primera patada
        yield return EjecutarPaso(Paso.PatearPiedra, () => Ocurrio(EvPatada) || Ocurrio(EvTragada) || PiedraEntregadaABotro());

        // 4 · Llevas la piedra hasta Botro
        yield return EjecutarPaso(Paso.LlevarPiedraABotro, () => Ocurrio(EvTragada) || (PiedraEntregadaABotro() && JugadorCercaDeBotro()));

        // 5 · Botro explica (las frases pueden enfocar el generador, la gasolinera...)
        yield return EjecutarPaso(Paso.BotroExplica, null);

        // 6 · Llevas la piedra de vuelta a la gasolinera; mientras, si falta, la manguera (en paralelo)
        Coroutine rutinaManguera = StartCoroutine(MangueraMientrasLlevas());
        yield return EjecutarPaso(Paso.LlevarATrampilla, () => Ocurrio(EvTragada) || PiedraCercaDelAgujero());

        // 7 · La metes
        yield return EjecutarPaso(Paso.MeterPiedra, () => Ocurrio(EvTragada));
        if (rutinaManguera != null) StopCoroutine(rutinaManguera);
        TerminarManguera();

        // 8 · Zoom a la pantalla de gasolina (+XX L), solo si está activado
        if (hacerZoomGasolina)
        {
            IrA(Paso.ZoomGasolina);
            yield return ZoomGasolina();
        }

        // 9 · Tras repostar: recoger la moneda (marcada)
        float limiteRepostaje = Time.time + esperaMaximaRepostaje;
        yield return new WaitUntil(() => Ocurrio(EvGasolina) || Time.time > limiteRepostaje);
        yield return EjecutarPaso(Paso.RecogerRestos, () => Ocurrio(EvMoneda));

        // 10 · Reconstruir el generador de meteoritos lanzándole la moneda (marcado)
        yield return EjecutarPaso(Paso.ReconstruirGenerador, GeneradorConstruido);

        // 11 · Ya reparado: Botro habla de la diana (marcada) hasta que la golpeas
        if (generador != null)
            yield return EjecutarPaso(Paso.GeneradorReconstruido, () => Ocurrio(EvDiana));

        Terminar();
    }

    // Manguera en paralelo al paso 6: tras la primera patada de vuelta, si no está enchufada,
    // Botro dice las frases del paso "Manguera" y el marcador señala la manguera hasta que la enchufas.
    private IEnumerator MangueraMientrasLlevas()
    {
        if (MangueraConectada()) yield break;
        int patadas = contadorPatadas;
        yield return new WaitUntil(() => MangueraConectada() || contadorPatadas > patadas);
        if (MangueraConectada()) yield break;

        mangueraPendiente = true;
        instruccionManguera = "";
        Registrar("Manguera pendiente: Botro te pide enchufarla.");
        Coroutine frases = StartCoroutine(DecirFrases(Paso.Manguera, GuionBotro.ParaPaso(Paso.Manguera), true, false));
        yield return new WaitUntil(MangueraConectada);
        if (frases != null) StopCoroutine(frases);
        TerminarManguera();
    }

    private bool MangueraConectada()
    {
        return Ocurrio(EvManguera) || (manguera != null && manguera.Conectada);
    }

    private void TerminarManguera()
    {
        BotroDice.CancelarPendientes(PrefijoPaso(Paso.Manguera));
        RestaurarZoom();
        if (!mangueraPendiente) return;
        mangueraPendiente = false;
        instruccionManguera = "";
        MostrarInstruccion(instruccionPaso);
    }

    private bool GeneradorConstruido()
    {
        return generador == null || generador.EstaConstruida;
    }

    // Un paso: Botro dice sus frases (GuionBotro) mientras se espera a que pase 'fin'.
    // fin == null: el paso termina cuando Botro acaba de hablar. Si 'fin' ya se cumple, el paso se salta.
    private IEnumerator EjecutarPaso(Paso paso, Func<bool> fin)
    {
        if (fin != null && fin()) yield break;

        IrA(paso);
        instruccionPaso = "";
        GuionBotro.Momento guion = GuionBotro.ParaPaso(paso);

        if (fin == null)
        {
            yield return DecirFrases(paso, guion, false, true);
        }
        else
        {
            Coroutine frases = StartCoroutine(DecirFrases(paso, guion, true, true));
            yield return new WaitUntil(fin);
            // Si el paso no tiene frases, la corrutina acaba al instante y Unity devuelve null.
            if (frases != null) StopCoroutine(frases);
            BotroDice.CancelarPendientes(PrefijoPaso(paso));
            RestaurarZoom(); // por si se cortó a mitad de un enfoque
        }
        instruccionPaso = "";
        MostrarInstruccion(mangueraPendiente ? instruccionManguera : "");
    }

    private static string PrefijoPaso(Paso paso)
    {
        return "guion_" + paso + "_";
    }

    // principal = false: frases de la manguera en paralelo (su instrucción manda mientras esté pendiente).
    private IEnumerator DecirFrases(Paso paso, GuionBotro.Momento guion, bool repetir, bool principal)
    {
        if (guion == null) yield break;
        if (guion.retraso > 0f) yield return new WaitForSeconds(guion.retraso);

        string instruccion = null;
        float duracionInstruccion = 0f;
        foreach (GuionBotro.Frase frase in guion.frases)
        {
            if (frase == null || string.IsNullOrWhiteSpace(frase.texto)) continue;
            if (frase.retraso > 0f) yield return new WaitForSeconds(frase.retraso);

            IEnumerator decir = DecirYEsperar(PrefijoPaso(paso), frase.texto, frase.duracion);
            Bounds limites;
            string nombre;
            if (frase.Enfocar != GuionBotro.Enfoque.Nada && LimitesEnfoque(frase.Enfocar, out limites, out nombre))
                yield return HacerZoom(limites, nombre, fovMaximoEnfoque, decir);
            else
                yield return decir;

            if (frase.esInstruccion)
            {
                instruccion = frase.texto;
                duracionInstruccion = frase.duracion;
                if (principal)
                {
                    instruccionPaso = instruccion;
                    if (!mangueraPendiente) MostrarInstruccion(instruccion);
                }
                else
                {
                    instruccionManguera = instruccion;
                    MostrarInstruccion(instruccion);
                }
            }
        }

        // Si el jugador tarda, Botro repite la instrucción.
        if (!repetir || instruccion == null || guion.repetirInstruccionCada <= 0f) yield break;
        while (true)
        {
            yield return new WaitForSeconds(guion.repetirInstruccionCada);
            yield return DecirYEsperar(PrefijoPaso(paso), instruccion, duracionInstruccion);
        }
    }

    private void MostrarInstruccion(string texto)
    {
        if (texto == null) texto = "";
        if (objetivo == texto) return;
        objetivo = texto;
        objetivoDesde = Time.unscaledTime;
        if (!string.IsNullOrEmpty(texto)) Registrar("Instrucción: " + texto);
    }

    // Red de seguridad: si la cámara en primera persona ya está activa, la intro ha acabado
    // aunque no llegara su evento (la intro la apaga mientras dura la cinemática).
    private CamaraPrimeraPersona camaraJugador;
    private bool JugadorTieneControl()
    {
        if (CambioCamaraIntro.IntroTerminada) return true;
        if (camaraJugador == null && Camera.main != null) camaraJugador = Camera.main.GetComponent<CamaraPrimeraPersona>();
        return camaraJugador != null && camaraJugador.isActiveAndEnabled;
    }

    private void Registrar(string mensaje)
    {
        if (registrarEnConsola) Debug.Log("[GUION] " + mensaje, this);
    }

    private void Terminar()
    {
        IrA(Paso.Terminado);
        if (TutorialTerminado) return;
        TutorialTerminado = true;
        OnTutorialTerminado?.Invoke();
    }

    private void IrA(Paso paso)
    {
        if (PasoActual == paso) return;
        PasoActual = paso;
        Registrar("Paso → " + paso);
        OnPasoCambiado?.Invoke(paso);
    }

    private IEnumerator Esperar(string evento)
    {
        while (!Ocurrio(evento)) yield return null;
    }

    // Cada frase lleva un id único (para esperar a que acabe y poder cancelar las del paso).
    private IEnumerator DecirYEsperar(string prefijo, string texto, float duracion)
    {
        string id = prefijo + (contadorFrases++);
        BotroDice.Decir(id, texto, duracion);
        yield return Esperar("linea:" + id);
    }

    // ---------- La piedra que cae delante de la nave ----------

    // Si la intro se ha saltado, el meteorito no se ha lanzado: se lanza ahora.
    private void LanzarPiedraSiFalta()
    {
        if (MeteoritoInicial.PiedraLanzada != null) return;
        MeteoritoInicial meteorito = FindFirstObjectByType<MeteoritoInicial>();
        if (meteorito != null) meteorito.LanzarMeteorito();
    }

    private bool PiedraCaidaEnElSuelo()
    {
        Rigidbody piedra = MeteoritoInicial.PiedraLanzada;
        return piedra != null && !piedra.isKinematic;
    }

    // La piedra caída; si ya no existe (o no cayó), la piedra más cercana al jugador.
    private Rigidbody PiedraObjetivo()
    {
        Rigidbody caida = MeteoritoInicial.PiedraLanzada;
        if (caida != null) return caida;
        if (jugador == null || GestorPiedras.Instancia == null) return null;

        Rigidbody mejor = null;
        float mejorD2 = float.MaxValue;
        foreach (Rigidbody piedra in GestorPiedras.Instancia.Piedras)
        {
            if (piedra == null) continue;
            float d2 = (piedra.position - jugador.position).sqrMagnitude;
            if (d2 < mejorD2) { mejorD2 = d2; mejor = piedra; }
        }
        return mejor;
    }

    // ---------- Botro ----------

    private void BuscarBotro()
    {
        if (botro != null)
        {
            DesactivarConversacion();
            return;
        }
        GameObject objeto = GameObject.Find(nombreObjetoBotro);
        if (objeto != null) botro = objeto.transform;
        else Registrar("No encuentro a '" + nombreObjetoBotro + "': la piedra se entrega en la trampilla.");
        DesactivarConversacion();
    }

    // Botro es el único que habla: si tiene una conversación (NPCDialogueAgent), se apaga.
    private void DesactivarConversacion()
    {
        if (botro == null) return;
        foreach (NPCDialogueAgent conversacion in botro.GetComponentsInChildren<NPCDialogueAgent>(true))
        {
            conversacion.enabled = false;
            Registrar("Conversación de Botro desactivada: solo habla él (" + conversacion.name + ").");
        }
    }

    // Donde está Botro (o la trampilla si no hay Botro).
    private Vector3? PosicionBotro()
    {
        if (botro != null) return botro.position;
        if (agujero != null) return agujero.transform.position;
        return null;
    }

    private static float DistanciaHorizontal(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private bool PiedraEntregadaABotro()
    {
        Vector3? destino = PosicionBotro();
        if (destino == null) return true;

        Rigidbody caida = MeteoritoInicial.PiedraLanzada;
        if (caida != null) return DistanciaHorizontal(caida.position, destino.Value) <= distanciaEntregaBotro;

        // Sin piedra caída, vale cualquiera.
        if (GestorPiedras.Instancia == null) return false;
        foreach (Rigidbody piedra in GestorPiedras.Instancia.Piedras)
            if (piedra != null && DistanciaHorizontal(piedra.position, destino.Value) <= distanciaEntregaBotro)
                return true;
        return false;
    }

    private bool JugadorCercaDeBotro()
    {
        Vector3? destino = PosicionBotro();
        return destino == null || jugador == null || DistanciaHorizontal(jugador.position, destino.Value) <= distanciaJugadorBotro;
    }

    // Hacia dónde apunta el marcador en cada paso (la manguera pendiente tiene prioridad).
    private Vector3? PosicionMarcador()
    {
        if (mangueraPendiente) return PosicionManguera();

        switch (PasoActual)
        {
            case Paso.PatearPiedra:
                Rigidbody piedra = PiedraObjetivo();
                return piedra != null ? piedra.position : (Vector3?)null;
            case Paso.LlevarPiedraABotro:
                return PosicionBotro();
            case Paso.LlevarATrampilla:
            case Paso.MeterPiedra:
                return agujero != null ? agujero.transform.position : (Vector3?)null;
            case Paso.RecogerRestos:
                return MonedaMasCercana();
            case Paso.GeneradorReconstruido:
                if (generador != null && generador.dianaBloop != null) return generador.dianaBloop.transform.position;
                goto case Paso.ReconstruirGenerador;
            case Paso.ReconstruirGenerador:
                Bounds b;
                string n;
                return LimitesEnfoque(GuionBotro.Enfoque.Generador, out b, out n) ? b.center : (Vector3?)null;
            default:
                return null;
        }
    }

    // Boquilla de la manguera; si ya la llevas en la mano, la toma de la nave.
    private Vector3? PosicionManguera()
    {
        if (manguera == null) return null;
        if (manguera.Agarrada && manguera.puntoConexionNave != null) return manguera.puntoConexionNave.position;
        return manguera.transform.position;
    }

    // La moneda suelta más cercana al jugador (no las que se están lanzando). Se busca 4 veces por segundo.
    private Moneda monedaMarcada;
    private float siguienteBusquedaMoneda;

    private Vector3? MonedaMasCercana()
    {
        if (jugador == null) return null;
        if (Time.unscaledTime >= siguienteBusquedaMoneda || monedaMarcada == null)
        {
            siguienteBusquedaMoneda = Time.unscaledTime + 0.25f;
            monedaMarcada = null;
            float mejor = float.MaxValue;
            foreach (Moneda m in FindObjectsByType<Moneda>(FindObjectsSortMode.None))
            {
                if (m == null || !m.isActiveAndEnabled || m.GetComponent<ProyectilArma>() != null) continue;
                float d = (m.transform.position - jugador.position).sqrMagnitude;
                if (d < mejor) { mejor = d; monedaMarcada = m; }
            }
        }
        return monedaMarcada != null ? monedaMarcada.transform.position : (Vector3?)null;
    }

    // Qué encuadra la cámara para cada opción de "Enfocar".
    private bool LimitesEnfoque(GuionBotro.Enfoque enfoque, out Bounds limites, out string nombre)
    {
        limites = new Bounds();
        nombre = enfoque.ToString();
        switch (enfoque)
        {
            case GuionBotro.Enfoque.Generador:
                if (generador == null) return false;
                nombre = generador.name;
                return LimitesDe(generador.EstaConstruida ? generador.partesArmaDesbloqueada : generador.partesRuinas, out limites)
                       || LimitesDe(new[] { generador.gameObject }, out limites);
            case GuionBotro.Enfoque.Botro:
                return botro != null && LimitesDe(new[] { botro.gameObject }, out limites);
            case GuionBotro.Enfoque.Trampilla:
                return agujero != null && LimitesDe(new[] { agujero.gameObject }, out limites);
            case GuionBotro.Enfoque.Manguera:
                return manguera != null && LimitesDe(new[] { manguera.gameObject }, out limites);
            case GuionBotro.Enfoque.Piedra:
                Rigidbody piedra = PiedraObjetivo();
                return piedra != null && LimitesDe(new[] { piedra.gameObject }, out limites);
            case GuionBotro.Enfoque.PantallaGasolina:
                if (gasolinera == null) return false;
                bool ok = ResolverPantallaGasolina(out limites);
                nombre = nombrePantalla;
                return ok;
            default:
                return false;
        }
    }

    private static bool LimitesDe(GameObject[] objetos, out Bounds limites)
    {
        limites = new Bounds();
        bool hay = false;
        if (objetos == null) return false;
        foreach (GameObject o in objetos)
        {
            if (o == null || !o.activeInHierarchy) continue;
            foreach (Renderer r in o.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || r is ParticleSystemRenderer || r is TrailRenderer) continue;
                if (!hay) { limites = r.bounds; hay = true; }
                else limites.Encapsulate(r.bounds);
            }
        }
        return hay;
    }

    private bool PiedraCercaDelAgujero()
    {
        if (agujero == null || GestorPiedras.Instancia == null) return false;
        float d2 = distanciaPiedraAgujero * distanciaPiedraAgujero;
        Vector3 centro = agujero.transform.position;
        foreach (Rigidbody piedra in GestorPiedras.Instancia.Piedras)
            if (piedra != null && (piedra.position - centro).sqrMagnitude <= d2)
                return true;
        return false;
    }

    // =====================================================
    // ZOOM A LA GASOLINA
    // =====================================================

    private IEnumerator ZoomGasolina()
    {
        if (gasolinera == null) yield break;
        yield return Esperar(EvGasolina);

        Bounds pantalla;
        if (!ResolverPantallaGasolina(out pantalla))
        {
            Registrar("Zoom: no encuentro la pantalla de gasolina; asigna 'Pantalla Gasolina'.");
            yield break;
        }
        yield return HacerZoom(pantalla, nombrePantalla, fovZoom, new WaitForSeconds(segundosMirando));
    }

    // ---------- Zoom de cámara (gasolina y "Enfocar" de las frases) ----------

    // Estado para poder deshacer el zoom aunque se corte la corrutina (StopCoroutine no ejecuta 'finally').
    private bool zoomActivo;
    private Camera camaraZoom;
    private Quaternion rotacionAntesZoom;
    private float fovAntesZoom;
    private CamaraPrimeraPersona camaraFPSZoom;
    private bool camaraYaBloqueadaZoom;
    private readonly List<MonoBehaviour> desactivadosZoom = new List<MonoBehaviour>();

    // Mira a 'objetivo' con zoom mientras dura 'mientras' (frase de Botro, espera...) y vuelve.
    private IEnumerator HacerZoom(Bounds objetivo, string nombre, float fovMaximo, object mientras)
    {
        RestaurarZoom();
        Camera cam = Camera.main;
        if (cam == null)
        {
            yield return mientras;
            yield break;
        }

        // Bloquear la mirada (CamaraPrimeraPersona reescribe la rotación cada frame) y el movimiento.
        camaraZoom = cam;
        camaraFPSZoom = cam.GetComponent<CamaraPrimeraPersona>();
        camaraYaBloqueadaZoom = camaraFPSZoom != null && camaraFPSZoom.EstaBloqueadaPorUI();
        if (camaraFPSZoom != null) camaraFPSZoom.SetBloqueadaPorUI(true);

        List<MonoBehaviour> bloqueados = new List<MonoBehaviour>();
        if (bloquearDuranteZoom != null && bloquearDuranteZoom.Length > 0)
        {
            bloqueados.AddRange(bloquearDuranteZoom);
        }
        else
        {
            MenuBotUI menu = FindFirstObjectByType<MenuBotUI>();
            if (menu != null && menu.controladorMiradaJugador != null) bloqueados.Add(menu.controladorMiradaJugador);
            MovimientoPersonaje movimiento = FindFirstObjectByType<MovimientoPersonaje>();
            if (movimiento != null) bloqueados.Add(movimiento);
        }

        desactivadosZoom.Clear();
        foreach (MonoBehaviour script in bloqueados)
        {
            if (script != null && script.enabled && script != camaraFPSZoom)
            {
                script.enabled = false;
                desactivadosZoom.Add(script);
            }
        }

        Transform t = cam.transform;
        rotacionAntesZoom = t.localRotation;
        fovAntesZoom = cam.fieldOfView;
        zoomActivo = true;

        // Mirar al centro del objetivo y ajustar el FOV para encuadrarlo.
        Vector3 haciaObjetivo = objetivo.center - t.position;
        Quaternion rotacionZoomMundo = Quaternion.LookRotation(haciaObjetivo, Vector3.up);
        Quaternion rotacionZoom = t.parent != null ? Quaternion.Inverse(t.parent.rotation) * rotacionZoomMundo : rotacionZoomMundo;
        float distancia = Mathf.Max(0.5f, haciaObjetivo.magnitude);
        float tamano = Mathf.Max(objetivo.extents.x, objetivo.extents.y, objetivo.extents.z) * 2f;
        float fovEncuadre = 2f * Mathf.Atan(tamano * 0.5f * margenEncuadre / distancia) * Mathf.Rad2Deg;
        float fovObjetivo = Mathf.Clamp(fovEncuadre, fovMinimoZoom, Mathf.Min(fovMaximo, fovAntesZoom));

        Registrar("Zoom a '" + nombre + "' a " + distancia.ToString("0.0") + " m, FOV " + fovObjetivo.ToString("0"));

        yield return Interpolar(cam, rotacionAntesZoom, rotacionZoom, fovAntesZoom, fovObjetivo);
        yield return mientras;
        yield return Interpolar(cam, rotacionZoom, rotacionAntesZoom, fovObjetivo, fovAntesZoom);
        RestaurarZoom();
    }

    // Deja la cámara y los controles como estaban antes del zoom.
    private void RestaurarZoom()
    {
        if (!zoomActivo) return;
        zoomActivo = false;
        if (camaraZoom != null)
        {
            camaraZoom.transform.localRotation = rotacionAntesZoom;
            camaraZoom.fieldOfView = fovAntesZoom;
        }
        if (camaraFPSZoom != null) camaraFPSZoom.SetBloqueadaPorUI(camaraYaBloqueadaZoom);
        foreach (MonoBehaviour script in desactivadosZoom)
            if (script != null) script.enabled = true;
        desactivadosZoom.Clear();
    }

    private string nombrePantalla = "";

    // La pantalla física es la que muestra la RenderTexture de la cámara del cartel de gasoil.
    private bool ResolverPantallaGasolina(out Bounds limites)
    {
        limites = new Bounds();

        if (pantallaGasolina != null)
        {
            Renderer propio = pantallaGasolina.GetComponentInChildren<Renderer>();
            limites = propio != null ? propio.bounds : new Bounds(pantallaGasolina.position, Vector3.one);
            nombrePantalla = pantallaGasolina.name;
            return true;
        }

        Canvas lienzo = gasolinera.textoLED != null ? gasolinera.textoLED.canvas : gasolinera.GetComponentInParent<Canvas>();
        if (lienzo != null) lienzo = lienzo.rootCanvas;

        if (lienzo != null && lienzo.renderMode == RenderMode.WorldSpace)
        {
            limites = new Bounds(lienzo.transform.position, Vector3.one);
            nombrePantalla = lienzo.name;
            return true;
        }

        RenderTexture textura = lienzo != null && lienzo.worldCamera != null ? lienzo.worldCamera.targetTexture : null;
        if (textura == null) return false;

        foreach (Renderer r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            foreach (Material m in r.sharedMaterials)
            {
                if (m == null) continue;
                foreach (int id in m.GetTexturePropertyNameIDs())
                {
                    if (m.GetTexture(id) == textura)
                    {
                        limites = r.bounds;
                        nombrePantalla = r.name;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private IEnumerator Interpolar(Camera cam, Quaternion desde, Quaternion hasta, float fovDesde, float fovHasta)
    {
        float tiempo = 0f;
        while (tiempo < duracionIdaVuelta)
        {
            tiempo += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / duracionIdaVuelta));
            cam.transform.localRotation = Quaternion.Slerp(desde, hasta, k);
            cam.fieldOfView = Mathf.Lerp(fovDesde, fovHasta, k);
            yield return null;
        }
    }

    // =====================================================
    // OBJETIVO EN PANTALLA (estilo cartel LED)
    // =====================================================

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        bool hayObjetivo = !string.IsNullOrEmpty(objetivo);
        if (!hayObjetivo && (!mostrarMarcador || PasoActual == Paso.Terminado)) return;

        AimAssistVisual estilo = AimAssistVisual.Instancia;
        Color placa = estilo != null ? estilo.colorPlaca : new Color(0.035f, 0.025f, 0.09f, 0.88f);
        Color neon = estilo != null ? estilo.colorNeon : new Color(0.926f, 0.887f, 1f, 1f);
        Color alto = estilo != null ? estilo.colorNeonAlto : new Color(0.914f, 0.616f, 0.992f, 1f);
        Font fuenteLED = estilo != null ? estilo.fuentePanel : null;

        if (estiloLED == null || estiloLED.font != fuenteLED)
        {
            estiloLED = new GUIStyle { font = fuenteLED, clipping = TextClipping.Overflow };
            estiloLED.normal.textColor = Color.white;
        }
        if (estiloTexto == null || estiloTexto.font != fuenteTexto)
        {
            estiloTexto = new GUIStyle { font = fuenteTexto, fontStyle = FontStyle.Bold, wordWrap = true, clipping = TextClipping.Overflow };
            estiloTexto.normal.textColor = Color.white;
        }

        float e = Screen.height / 1080f * escala;
        DibujarMarcador(e);
        if (!hayObjetivo) return;

        float aparicion = Mathf.Clamp01((Time.unscaledTime - objetivoDesde) / 0.25f);
        float pop = 1f + (1f - aparicion) * 0.12f;
        float k = e * pop;

        // El panel crece hacia abajo con el texto de la instrucción.
        float ancho = 560f * k;
        estiloTexto.fontSize = Mathf.Max(8, Mathf.RoundToInt(21f * k));
        float altoTexto = estiloTexto.CalcHeight(new GUIContent(objetivo), ancho - 32f * k);
        float altoPanel = Mathf.Max(62f * k, 30f * k + altoTexto + 8f * k);
        Rect r = new Rect((Screen.width - ancho) * 0.5f, 28f * e, ancho, altoPanel);

        AimAssistVisual.Rectangulo(r, AimAssistVisual.ConAlfa(placa, placa.a * aparicion), 4f * k);
        AimAssistVisual.BordeNeon(r, AimAssistVisual.ConAlfa(neon, 0.7f * aparicion), k, 4f * k);
        AimAssistVisual.EsquinasHUD(r, AimAssistVisual.ConAlfa(alto, aparicion), k);

        float m = 16f * k;
        AimAssistVisual.TextoNeon(estiloLED, new Rect(r.x + m, r.y + 6f * k, r.width - 2f * m, 16f * k), (BotroDice.Instancia != null ? BotroDice.Instancia.nombre : "BOTRO") + "  ·  OBJETIVO", 14f * k, TextAnchor.UpperLeft, AimAssistVisual.ConAlfa(alto, aparicion), 0.5f);
        AimAssistVisual.TextoNeon(estiloTexto, new Rect(r.x + m, r.y + 24f * k, r.width - 2f * m, altoTexto + 4f * k), objetivo, 21f * k, TextAnchor.MiddleCenter, AimAssistVisual.ConAlfa(neon, aparicion), 0.3f);
    }

    // Rombo de neón sobre el destino del paso actual, con la distancia debajo.
    private void DibujarMarcador(float e)
    {
        if (!mostrarMarcador || jugador == null) return;
        Vector3? destino = PosicionMarcador();
        Camera cam = Camera.main;
        if (destino == null || cam == null) return;

        float distancia = Vector3.Distance(jugador.position, destino.Value);
        if (distancia < 3f) return;

        Vector3 p = cam.WorldToScreenPoint(destino.Value + Vector3.up * 1.5f);
        if (p.z <= 0f) return;
        Vector2 centro = new Vector2(p.x, Screen.height - p.y);

        TemaUINeon tema = TemaUINeon.Actual;
        float a = Mathf.Clamp01((Time.unscaledTime - objetivoDesde) / 0.25f);
        float lado = 16f * e * (1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.1f);
        Rect rombo = new Rect(centro.x - lado * 0.5f, centro.y - lado * 0.5f, lado, lado);

        Matrix4x4 matriz = GUI.matrix;
        GUIUtility.RotateAroundPivot(45f, centro);
        EstiloNeonIMGUI.Rectangulo(rombo, EstiloNeonIMGUI.ConAlfa(tema.colorPlaca, tema.colorPlaca.a * a), 2f * e);
        EstiloNeonIMGUI.BordeNeon(rombo, EstiloNeonIMGUI.ConAlfa(tema.colorNeonAlto, a), e, 2f * e);
        GUI.matrix = matriz;

        EstiloNeonIMGUI.TextoNeon(estiloLED, new Rect(centro.x - 60f * e, centro.y + lado * 0.9f, 120f * e, 20f * e),
            Mathf.RoundToInt(distancia) + " M", 15f * e, TextAnchor.UpperCenter, EstiloNeonIMGUI.ConAlfa(tema.colorNeon, a), 0.5f);
    }
}
