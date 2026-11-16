using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PiedrasIncr.Audio
{
    /// <summary>Referencia a un sonido que esta sonando, para pararlo o cambiarle el volumen.</summary>
    public readonly struct AudioHandle
    {
        internal readonly int Id;
        internal AudioHandle(int id) { Id = id; }

        public bool IsPlaying => AudioManager.IsPlaying(this);
        public void Stop(float fundido = -1f) => AudioManager.Stop(this, fundido);
        public void SetVolume(float volumen) => AudioManager.SetVolume(this, volumen);
    }

    [DefaultExecutionOrder(-1000)]
    [AddComponentMenu("")]
    public sealed class AudioManager : MonoBehaviour
    {
        private const string RutaLibreria = "SoundLibrary";
        private const int VocesIniciales = 24;
        private const int VocesMaximas = 64;

        private sealed class Voz
        {
            public AudioSource fuente;
            public Transform tr;
            public SoundEvent sonido;
            public Transform seguir;
            public bool sigue;
            public int id;
            public bool activa;
            public bool persistente;
            public float inicio;
            public float volumenBase;
            public float multiplicador;
            public float fade;
            public float fadeDestino;
            public float fadeVelocidad;
            public bool liberarAlTerminarFade;
        }

        private static AudioManager instancia;
        private static bool cerrando;

        private readonly List<Voz> voces = new List<Voz>();
        private readonly Dictionary<int, Voz> porId = new Dictionary<int, Voz>();
        private readonly Dictionary<SoundEvent, float> ultimoUso = new Dictionary<SoundEvent, float>();
        private readonly HashSet<string> avisados = new HashSet<string>();

        private SoundLibrary libreria;
        private int siguienteId = 1;
        private Voz vozMusica;
        private string musicaActual;

        // ========================================
        // API PUBLICA
        // ========================================

        /// <summary>Sonido 2D: se oye igual en todas partes (interfaz, avisos...).</summary>
        public static AudioHandle Play(string nombre, float volumen = 1f)
        {
            AudioManager m = Instancia;
            return m != null ? m.Reproducir(nombre, Vector3.zero, null, false, volumen, false, -1f) : default;
        }

        /// <summary>Sonido 3D en un punto fijo del mundo.</summary>
        public static AudioHandle Play(string nombre, Vector3 posicion, float volumen = 1f)
        {
            AudioManager m = Instancia;
            return m != null ? m.Reproducir(nombre, posicion, null, true, volumen, false, -1f) : default;
        }

        /// <summary>Sonido 3D que sigue a un objeto mientras suena.</summary>
        public static AudioHandle PlayAttached(string nombre, Transform objetivo, float volumen = 1f)
        {
            if (objetivo == null) return default;
            AudioManager m = Instancia;
            return m != null ? m.Reproducir(nombre, objetivo.position, objetivo, true, volumen, false, -1f) : default;
        }

        /// <summary>Para un sonido. Con fundido negativo usa el fade out configurado en la lista.</summary>
        public static void Stop(AudioHandle handle, float fundido = -1f)
        {
            if (instancia != null && instancia.porId.TryGetValue(handle.Id, out Voz v))
                instancia.Detener(v, fundido);
        }

        /// <summary>Corta todos los efectos. La musica sigue.</summary>
        public static void StopAll()
        {
            if (instancia != null) instancia.LiberarNoPersistentes();
        }

        public static bool IsPlaying(AudioHandle handle)
        {
            return instancia != null && instancia.porId.ContainsKey(handle.Id);
        }

        public static void SetVolume(AudioHandle handle, float volumen)
        {
            if (instancia != null && instancia.porId.TryGetValue(handle.Id, out Voz v))
            {
                v.multiplicador = Mathf.Max(0f, volumen);
                AplicarVolumen(v);
            }
        }

        /// <summary>Pone musica en bucle, con fundido cruzado si ya sonaba otra.</summary>
        public static void PlayMusic(string nombre, float fundido = 1.5f)
        {
            AudioManager m = Instancia;
            if (m != null) m.ReproducirMusica(nombre, fundido);
        }

        public static void StopMusic(float fundido = 1.5f)
        {
            if (instancia == null || instancia.vozMusica == null) return;

            Voz anterior = instancia.vozMusica;
            instancia.vozMusica = null;
            instancia.musicaActual = null;
            instancia.Detener(anterior, fundido);
        }

        // ========================================
        // CICLO DE VIDA
        // ========================================

        private static AudioManager Instancia
        {
            get
            {
                if (instancia == null && !cerrando && Application.isPlaying)
                {
                    GameObject go = new GameObject("[AudioManager]");
                    DontDestroyOnLoad(go);
                    instancia = go.AddComponent<AudioManager>();
                }

                return instancia;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarEstaticos()
        {
            instancia = null;
            cerrando = false;
        }

        private void Awake()
        {
            if (instancia != null && instancia != this)
            {
                Destroy(this);
                return;
            }

            instancia = this;

            libreria = Resources.Load<SoundLibrary>(RutaLibreria);
            if (libreria == null)
                Debug.LogError("[Audio] No se encuentra Assets/Audio/Resources/" + RutaLibreria + ".asset");

            for (int i = 0; i < VocesIniciales; i++)
                CrearVoz();

            SceneManager.sceneLoaded += AlCargarEscena;
        }
        // ========================================
        // VOLUMENES
        // ========================================

        private const string PrefijoPrefs = "Audio.Volumen.";

        /// <summary>Volumen general de 0 a 1. Se recuerda entre sesiones.</summary>
        public static void SetMasterVolume(float volumen) => FijarVolumen("Master", volumen);

        /// <summary>Volumen de una categoria de 0 a 1. Se recuerda entre sesiones.</summary>
        public static void SetBusVolume(AudioBus bus, float volumen) => FijarVolumen(bus.ToString(), volumen);

        public static float GetMasterVolume() => PlayerPrefs.GetFloat(PrefijoPrefs + "Master", 1f);

        public static float GetBusVolume(AudioBus bus) => PlayerPrefs.GetFloat(PrefijoPrefs + bus, 1f);

        private static void FijarVolumen(string canal, float volumen)
        {
            volumen = Mathf.Clamp01(volumen);
            PlayerPrefs.SetFloat(PrefijoPrefs + canal, volumen);

            AudioManager m = Instancia;
            if (m != null) m.AplicarVolumenMixer(canal, volumen);
        }

        private void AplicarVolumenMixer(string canal, float volumen)
        {
            if (libreria == null || libreria.Mixer == null) return;

            // El mixer trabaja en decibelios: 1 = 0 dB, 0 = silencio.
            float db = volumen <= 0.0001f ? -80f : Mathf.Log10(volumen) * 20f;

            if (!libreria.Mixer.SetFloat("Volumen" + canal, db) && avisados.Add("param:" + canal))
                Debug.LogWarning("[Audio] El mixer no tiene expuesto el parametro 'Volumen" + canal + "'.");
        }

        private void Start()
        {
            AplicarVolumenMixer("Master", GetMasterVolume());

            foreach (AudioBus bus in System.Enum.GetValues(typeof(AudioBus)))
                AplicarVolumenMixer(bus.ToString(), GetBusVolume(bus));
        }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            if (instancia == this) instancia = null;
        }

        private void OnApplicationQuit()
        {
            cerrando = true;
        }

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (modo == LoadSceneMode.Single) LiberarNoPersistentes();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // Con el juego sin foco o el audio en pausa, isPlaying no es fiable.
            bool puedeLiberar = Application.isFocused && !AudioListener.pause;

            for (int i = 0; i < voces.Count; i++)
            {
                Voz v = voces[i];
                if (!v.activa) continue;

                if (v.sigue)
                {
                    if (v.seguir != null)
                    {
                        v.tr.position = v.seguir.position;
                    }
                    else
                    {
                        // El objeto al que seguia ya no existe.
                        v.sigue = false;
                        if (v.fuente.loop)
                        {
                            Detener(v, Mathf.Max(0.1f, v.sonido.FadeOut));
                        }
                    }
                }

                if (v.fade != v.fadeDestino)
                {
                    v.fade = Mathf.MoveTowards(v.fade, v.fadeDestino, v.fadeVelocidad * dt);
                    AplicarVolumen(v);

                    if (v.fade == v.fadeDestino && v.liberarAlTerminarFade)
                    {
                        Liberar(v);
                        continue;
                    }
                }

                if (puedeLiberar && !v.fuente.isPlaying)
                    Liberar(v);
            }
        }

        // ========================================
        // REPRODUCCION
        // ========================================

        private AudioHandle Reproducir(
            string nombre,
            Vector3 posicion,
            Transform seguir,
            bool espacial,
            float volumen,
            bool persistente,
            float fadeIn)
        {
            SoundEvent s = Buscar(nombre);
            if (s == null) return default;

            float ahora = Time.unscaledTime;

            if (s.IntervaloMinimo > 0f &&
                ultimoUso.TryGetValue(s, out float ultimo) &&
                ahora - ultimo < s.IntervaloMinimo)
            {
                return default;
            }

            AudioClip clip = s.ElegirClip();
            if (clip == null)
            {
                if (avisados.Add("clip:" + nombre))
                    Debug.LogWarning("[Audio] El sonido '" + nombre + "' no tiene clips asignados.");
                return default;
            }

            if (s.MaxInstancias > 0)
            {
                int cantidad = 0;
                Voz masAntigua = null;

                for (int i = 0; i < voces.Count; i++)
                {
                    Voz otra = voces[i];
                    if (!otra.activa || otra.sonido != s) continue;

                    cantidad++;
                    if (masAntigua == null || otra.inicio < masAntigua.inicio)
                        masAntigua = otra;
                }

                if (cantidad >= s.MaxInstancias)
                    Liberar(masAntigua);
            }

            Voz v = ObtenerVoz(s.Prioridad);
            if (v == null) return default;

            AudioSource f = v.fuente;
            f.clip = clip;
            f.loop = s.Bucle || persistente;
            f.pitch = s.ElegirPitch();
            f.priority = s.Prioridad;
            f.spatialBlend = espacial ? s.MezclaEspacial : 0f;
            f.minDistance = s.DistanciaMin;
            f.maxDistance = s.DistanciaMax;
            f.rolloffMode = s.Atenuacion;
            f.dopplerLevel = 0f;
            f.outputAudioMixerGroup = libreria.ObtenerGrupo(s.Bus);

            v.tr.position = posicion;
            v.sonido = s;
            v.seguir = seguir;
            v.sigue = seguir != null;
            v.persistente = persistente;
            v.inicio = ahora;
            v.volumenBase = s.ElegirVolumen();
            v.multiplicador = Mathf.Max(0f, volumen);
            v.liberarAlTerminarFade = false;

            float entrada = fadeIn >= 0f ? fadeIn : s.FadeIn;
            if (entrada > 0f)
            {
                v.fade = 0f;
                v.fadeDestino = 1f;
                v.fadeVelocidad = 1f / entrada;
            }
            else
            {
                v.fade = 1f;
                v.fadeDestino = 1f;
                v.fadeVelocidad = 0f;
            }

            v.id = siguienteId++;
            v.activa = true;
            porId[v.id] = v;
            ultimoUso[s] = ahora;

            AplicarVolumen(v);
            f.Play();

            return new AudioHandle(v.id);
        }

        private void ReproducirMusica(string nombre, float fundido)
        {
            if (vozMusica != null && vozMusica.activa && musicaActual == nombre)
                return;

            if (vozMusica != null)
            {
                Voz anterior = vozMusica;
                vozMusica = null;
                Detener(anterior, fundido);
            }

            AudioHandle handle = Reproducir(nombre, Vector3.zero, null, false, 1f, true, fundido);

            if (porId.TryGetValue(handle.Id, out Voz nueva))
            {
                vozMusica = nueva;
                musicaActual = nombre;
            }
            else
            {
                musicaActual = null;
            }
        }

        private SoundEvent Buscar(string nombre)
        {
            if (libreria == null || string.IsNullOrEmpty(nombre)) return null;

            SoundEvent s = libreria.Buscar(nombre);

            if (s == null && avisados.Add(nombre))
                Debug.LogWarning("[Audio] No existe el sonido '" + nombre + "' en la SoundLibrary.");

            return s;
        }

        // ========================================
        // VOCES
        // ========================================

        private Voz CrearVoz()
        {
            GameObject go = new GameObject("Voz " + voces.Count);
            go.transform.SetParent(transform, false);

            AudioSource fuente = go.AddComponent<AudioSource>();
            fuente.playOnAwake = false;

            Voz v = new Voz { fuente = fuente, tr = go.transform };
            voces.Add(v);
            return v;
        }

        private Voz ObtenerVoz(int prioridad)
        {
            for (int i = 0; i < voces.Count; i++)
            {
                if (!voces[i].activa) return voces[i];
            }

            if (voces.Count < VocesMaximas) return CrearVoz();

            // No quedan voces: se corta el sonido menos importante (y, a igualdad, el mas antiguo).
            Voz candidata = null;

            for (int i = 0; i < voces.Count; i++)
            {
                Voz v = voces[i];
                if (v.persistente) continue;

                if (candidata == null ||
                    v.sonido.Prioridad > candidata.sonido.Prioridad ||
                    (v.sonido.Prioridad == candidata.sonido.Prioridad && v.inicio < candidata.inicio))
                {
                    candidata = v;
                }
            }

            if (candidata == null || candidata.sonido.Prioridad < prioridad)
                return null;

            Liberar(candidata);
            return candidata;
        }

        private void Detener(Voz v, float fundido)
        {
            if (!v.activa) return;

            if (fundido < 0f) fundido = v.sonido.FadeOut;

            if (fundido <= 0f)
            {
                Liberar(v);
                return;
            }

            v.fadeDestino = 0f;
            v.fadeVelocidad = 1f / fundido;
            v.liberarAlTerminarFade = true;
        }

        private void Liberar(Voz v)
        {
            if (v == null || !v.activa) return;

            v.fuente.Stop();
            v.fuente.clip = null;
            porId.Remove(v.id);

            v.activa = false;
            v.sonido = null;
            v.seguir = null;
            v.sigue = false;
            v.persistente = false;

            if (vozMusica == v)
            {
                vozMusica = null;
                musicaActual = null;
            }
        }

        private void LiberarNoPersistentes()
        {
            for (int i = 0; i < voces.Count; i++)
            {
                if (voces[i].activa && !voces[i].persistente)
                    Liberar(voces[i]);
            }
        }

        private static void AplicarVolumen(Voz v)
        {
            v.fuente.volume = v.volumenBase * v.multiplicador * v.fade;
        }
    }
}