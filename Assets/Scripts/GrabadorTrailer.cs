using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
#endif

// Graba clips del Game view para el tráiler con el Unity Recorder: pulsa la tecla (9) para empezar y otra vez para parar.
// Solo funciona en el editor (el Recorder no existe en las builds); en una build el componente no hace nada.
// El indicador de grabación se muestra en el título de la pestaña Game y en la consola, no dentro del juego,
// para que no salga en el vídeo.
public class GrabadorTrailer : MonoBehaviour
{
    public enum CalidadClip
    {
        [Tooltip("Calidad de edición. ~1,5 GB por minuto a 1080p60. La que se recomienda para el tráiler.")]
        ProRes422HQ,
        [Tooltip("Máxima calidad (y tamaño): ~3 GB por minuto a 1080p60.")]
        ProRes4444XQ,
        [Tooltip("H.264 MP4 de alta calidad: archivos pequeños, pero con pérdida al volver a editarlos.")]
        MP4Alta
    }

    [Header("Controles")]
    public KeyCode teclaGrabar = KeyCode.Alpha9;

    [Header("Vídeo")]
    public int ancho = 1920;
    public int alto = 1080;
    [Tooltip("Fotogramas por segundo del clip. El juego avanza a ritmo constante: si el PC no llega, va más lento mientras graba, pero el vídeo sale fluido.")]
    public float fotogramasPorSegundo = 60f;
    public CalidadClip calidad = CalidadClip.ProRes422HQ;
    public bool grabarAudio = true;

    [Header("Archivos")]
    [Tooltip("Carpeta de los clips. Relativa a la carpeta del proyecto (fuera de Assets, para que Unity no importe los vídeos) o una ruta absoluta.")]
    public string carpetaClips = "Trailer/Clips";
    public string prefijoArchivo = "Clip";

    [Header("Fluidez")]
    [Tooltip("Mientras graba, la física avanza un paso por fotograma (1 / fps). Con el paso normal (0,02 s = 50 Hz) " +
             "y vídeo a 60 fps, uno de cada ~6 fotogramas sale repetido y el movimiento da tirones.")]
    public bool sincronizarFisicaConVideo = true;

    [Header("Copia para ver (MP4)")]
    [Tooltip("Al terminar cada clip ProRes crea una copia MP4 (H.264) que se ve bien en cualquier reproductor, " +
             "con la curva de color de la pantalla (sRGB) para que no se vea más oscura. El ProRes se queda para editar.")]
    public bool crearCopiaMP4 = true;
    [Tooltip("Ejecutable de ffmpeg: 'ffmpeg' si está en el PATH, o la ruta completa (p. ej. C:/Users/mkmik/Tools/ffmpeg/bin/ffmpeg.exe).")]
    public string rutaFfmpeg = "ffmpeg";

#if UNITY_EDITOR
    private RecorderController controlador;
    private string rutaActual;
    private float inicioGrabacion;
    private GUIContent tituloOriginalGame;
    private float pasoFisicaOriginal;

    void Update()
    {
        if (Input.GetKeyDown(teclaGrabar) || (teclaGrabar == KeyCode.Alpha9 && Input.GetKeyDown(KeyCode.Keypad9)))
        {
            if (EstaGrabando()) Parar();
            else Empezar();
        }

        if (EstaGrabando())
            ActualizarIndicador($"● REC {FormatoTiempo(Time.unscaledTime - inicioGrabacion)}");
    }

    void OnDisable()
    {
        if (EstaGrabando()) Parar();
    }

    bool EstaGrabando() => controlador != null && controlador.IsRecording();

    void Empezar()
    {
        string carpeta = Path.IsPathRooted(carpetaClips)
            ? carpetaClips
            : Path.Combine(Directory.GetParent(Application.dataPath).FullName, carpetaClips);
        Directory.CreateDirectory(carpeta);

        // Sin extensión: la pone el Recorder según el formato (.mov o .mp4).
        rutaActual = Path.Combine(carpeta, $"{prefijoArchivo}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");

        MovieRecorderSettings pelicula = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        pelicula.name = "Clip tráiler";
        pelicula.Enabled = true;
        pelicula.EncoderSettings = CrearCodificador();
        pelicula.ImageInputSettings = new GameViewInputSettings { OutputWidth = ancho, OutputHeight = alto };
        // ProRes solo admite audio estéreo.
        pelicula.CaptureAudio = grabarAudio && (calidad == CalidadClip.MP4Alta || AudioSettings.speakerMode == AudioSpeakerMode.Stereo);
        pelicula.OutputFile = rutaActual;

        RecorderControllerSettings ajustes = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        ajustes.AddRecorderSettings(pelicula);
        ajustes.SetRecordModeToManual();
        ajustes.FrameRatePlayback = FrameRatePlayback.Constant;
        ajustes.FrameRate = fotogramasPorSegundo;
        ajustes.CapFrameRate = true;

        controlador = new RecorderController(ajustes);
        controlador.PrepareRecording();
        if (!controlador.StartRecording())
        {
            Debug.LogError("[GrabadorTrailer] El Recorder no ha podido empezar a grabar. Revisa los errores de la consola.");
            controlador = null;
            return;
        }

        inicioGrabacion = Time.unscaledTime;

        pasoFisicaOriginal = Time.fixedDeltaTime;
        if (sincronizarFisicaConVideo) Time.fixedDeltaTime = 1f / fotogramasPorSegundo;
        Debug.Log($"[GrabadorTrailer] ● GRABANDO {ancho}x{alto} a {fotogramasPorSegundo} fps ({calidad}) → {rutaActual}");
        Notificar("● GRABANDO");
    }

    void Parar()
    {
        float duracion = Time.unscaledTime - inicioGrabacion;
        controlador.StopRecording();
        controlador = null;
        Time.fixedDeltaTime = pasoFisicaOriginal;

        if (crearCopiaMP4 && calidad != CalidadClip.MP4Alta)
            CrearCopiaMP4(rutaActual + ".mov", rutaActual + "_preview.mp4", rutaFfmpeg);

        ActualizarIndicador(null);
        Debug.Log($"[GrabadorTrailer] ■ Clip guardado ({FormatoTiempo(duracion)}): {rutaActual}");
        Notificar("■ Clip guardado");
    }

    // Convierte en segundo plano, cuando el Recorder ha terminado de escribir el archivo.
    static void CrearCopiaMP4(string origen, string destino, string ffmpeg)
    {
        Task.Run(() =>
        {
            if (!EsperarArchivoCerrado(origen, 120f))
            {
                UnityEngine.Debug.LogWarning($"[GrabadorTrailer] No se ha creado la copia MP4: el clip sigue en uso ({origen}).");
                return;
            }

            // Se etiqueta con la curva sRGB: el juego se renderiza en sRGB y, con la etiqueta Rec.709 del ProRes,
            // los reproductores aplican otra curva y las zonas oscuras se ven mucho más oscuras.
            string argumentos = $"-y -loglevel error -i \"{origen}\" -c:v libx264 -preset slow -crf 16 -pix_fmt yuv420p " +
                                "-color_primaries bt709 -color_trc iec61966-2-1 -colorspace bt709 -color_range tv " +
                                $"-movflags +faststart -c:a aac -b:a 320k \"{destino}\"";
            try
            {
                using (System.Diagnostics.Process proceso = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ffmpeg, argumentos)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                }))
                {
                    string errores = proceso.StandardError.ReadToEnd();
                    proceso.WaitForExit();

                    if (proceso.ExitCode == 0)
                        UnityEngine.Debug.Log($"[GrabadorTrailer] Copia MP4 lista: {destino}");
                    else
                        UnityEngine.Debug.LogWarning($"[GrabadorTrailer] ffmpeg ha fallado al crear la copia MP4:\n{errores}");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[GrabadorTrailer] No se ha podido ejecutar ffmpeg ('{ffmpeg}'). Pon la ruta completa en 'Ruta Ffmpeg'.\n{e.Message}");
            }
        });
    }

    static bool EsperarArchivoCerrado(string ruta, float segundosMaximos)
    {
        DateTime limite = DateTime.Now.AddSeconds(segundosMaximos);
        while (DateTime.Now < limite)
        {
            try
            {
                if (File.Exists(ruta))
                {
                    using (File.Open(ruta, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                    return true;
                }
            }
            catch (IOException) { }

            Thread.Sleep(500);
        }
        return false;
    }

    IEncoderSettings CrearCodificador()
    {
        switch (calidad)
        {
            case CalidadClip.ProRes4444XQ:
                return new ProResEncoderSettings { Format = ProResEncoderSettings.OutputFormat.ProRes4444XQ };
            case CalidadClip.MP4Alta:
                return new CoreEncoderSettings
                {
                    Codec = CoreEncoderSettings.OutputCodec.MP4,
                    EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
                };
            default:
                return new ProResEncoderSettings { Format = ProResEncoderSettings.OutputFormat.ProRes422HQ };
        }
    }

    // El indicador va en el título de la pestaña Game: se ve en el editor, pero no se graba.
    void ActualizarIndicador(string texto)
    {
        EditorWindow game = VentanaGame();
        if (game == null) return;

        if (tituloOriginalGame == null) tituloOriginalGame = new GUIContent(game.titleContent);

        game.titleContent = texto == null ? tituloOriginalGame : new GUIContent(texto, tituloOriginalGame.image);
        if (texto == null) tituloOriginalGame = null;
    }

    void Notificar(string texto)
    {
        EditorWindow game = VentanaGame();
        if (game != null) game.ShowNotification(new GUIContent(texto), 1.5);
    }

    static EditorWindow VentanaGame()
    {
        Type tipo = Type.GetType("UnityEditor.GameView,UnityEditor");
        if (tipo == null) return null;

        UnityEngine.Object[] ventanas = Resources.FindObjectsOfTypeAll(tipo);
        return ventanas.Length > 0 ? (EditorWindow)ventanas[0] : null;
    }

    static string FormatoTiempo(float segundos)
    {
        TimeSpan t = TimeSpan.FromSeconds(segundos);
        return $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
    }
#endif
}
