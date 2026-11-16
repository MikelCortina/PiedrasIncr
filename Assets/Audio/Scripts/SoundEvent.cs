using UnityEngine;

namespace PiedrasIncr.Audio
{
    public enum AudioBus { SFX, UI, Musica, Ambiente }

    [System.Serializable]
    public sealed class SoundEvent
    {
        [Tooltip("Nombre unico con el que el codigo pide este sonido.")]
        [SerializeField] private string nombre;

        [Header("Clips")]
        [Tooltip("Si hay varios, se elige uno al azar cada vez.")]
        [SerializeField] private AudioClip[] clips;
        [Tooltip("Evita que suene el mismo clip dos veces seguidas.")]
        [SerializeField] private bool evitarRepetir = true;

        [Header("Mezcla")]
        [Tooltip("Categoria de volumen a la que pertenece.")]
        [SerializeField] private AudioBus bus = AudioBus.SFX;
        [SerializeField, Range(0f, 1f)] private float volumenMin = 1f;
        [SerializeField, Range(0f, 1f)] private float volumenMax = 1f;
        [SerializeField, Range(0.1f, 3f)] private float pitchMin = 1f;
        [SerializeField, Range(0.1f, 3f)] private float pitchMax = 1f;
        [SerializeField] private bool bucle;

        [Header("Espacial")]
        [Tooltip("0 = 2D (se oye igual en todas partes). 1 = 3D (depende de la posicion).")]
        [SerializeField, Range(0f, 1f)] private float mezclaEspacial = 1f;
        [SerializeField, Min(0.01f)] private float distanciaMin = 1f;
        [SerializeField, Min(0.02f)] private float distanciaMax = 30f;
        [SerializeField] private AudioRolloffMode atenuacion = AudioRolloffMode.Logarithmic;

        [Header("Limites")]
        [Tooltip("Cuantas copias de este sonido pueden sonar a la vez. 0 = sin limite.")]
        [SerializeField, Min(0)] private int maxInstancias = 4;
        [Tooltip("Segundos minimos entre dos reproducciones. 0 = sin espera.")]
        [SerializeField, Min(0f)] private float intervaloMinimo = 0f;
        [Tooltip("0 = el mas importante, 256 = el menos. Si faltan voces se cortan los menos importantes.")]
        [SerializeField, Range(0, 256)] private int prioridad = 128;

        [Header("Fundidos (segundos)")]
        [SerializeField, Min(0f)] private float fadeIn = 0f;
        [SerializeField, Min(0f)] private float fadeOut = 0f;

        [System.NonSerialized] private int ultimoIndice = -1;

        public string Nombre => nombre;
        public AudioBus Bus => bus;
        public bool Bucle => bucle;
        public float MezclaEspacial => mezclaEspacial;
        public float DistanciaMin => distanciaMin;
        public float DistanciaMax => distanciaMax;
        public AudioRolloffMode Atenuacion => atenuacion;
        public int MaxInstancias => maxInstancias;
        public float IntervaloMinimo => intervaloMinimo;
        public int Prioridad => prioridad;
        public float FadeIn => fadeIn;
        public float FadeOut => fadeOut;

        public float ElegirVolumen() => Random.Range(volumenMin, volumenMax);
        public float ElegirPitch() => Random.Range(pitchMin, pitchMax);

        public AudioClip ElegirClip()
        {
            if (clips == null || clips.Length == 0) return null;
            if (clips.Length == 1) return clips[0];

            int i = Random.Range(0, clips.Length);
            if (evitarRepetir && i == ultimoIndice) i = (i + 1) % clips.Length;
            ultimoIndice = i;
            return clips[i];
        }

        // Lo llama la libreria cada vez que se edita en el Inspector.
        public void Validar()
        {
            // Una entrada recien anadida puede llegar con todo a cero: le damos valores utiles.
            if (pitchMax <= 0f)
            {
                volumenMin = volumenMax = 1f;
                pitchMin = pitchMax = 1f;
                mezclaEspacial = 1f;
                distanciaMin = 1f;
                distanciaMax = 30f;
                maxInstancias = 4;
                prioridad = 128;
                evitarRepetir = true;
            }

            if (volumenMax < volumenMin) volumenMax = volumenMin;
            if (pitchMax < pitchMin) pitchMax = pitchMin;
            if (distanciaMax <= distanciaMin) distanciaMax = distanciaMin + 0.01f;
        }
    }
}