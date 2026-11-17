using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace PiedrasIncr.Audio
{
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library", order = 0)]
    public sealed class SoundLibrary : ScriptableObject
    {
        [Header("Mixer")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup grupoSFX;
        [SerializeField] private AudioMixerGroup grupoUI;
        [SerializeField] private AudioMixerGroup grupoMusica;
        [SerializeField] private AudioMixerGroup grupoAmbiente;

        [Header("Sonidos")]
        [SerializeField] private List<SoundEvent> sonidos = new List<SoundEvent>();

        private Dictionary<string, SoundEvent> indice;

        public AudioMixer Mixer => mixer;
        public IReadOnlyList<SoundEvent> Sonidos => sonidos;

        public AudioMixerGroup ObtenerGrupo(AudioBus bus)
        {
            switch (bus)
            {
                case AudioBus.UI: return grupoUI;
                case AudioBus.Musica: return grupoMusica;
                case AudioBus.Ambiente: return grupoAmbiente;
                default: return grupoSFX;
            }
        }

        public SoundEvent Buscar(string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return null;
            if (indice == null) ConstruirIndice();

            indice.TryGetValue(nombre, out SoundEvent sonido);
            return sonido;
        }

        private void ConstruirIndice()
        {
            indice = new Dictionary<string, SoundEvent>(System.StringComparer.Ordinal);

            foreach (SoundEvent sonido in sonidos)
            {
                if (sonido == null || string.IsNullOrEmpty(sonido.Nombre)) continue;

                if (indice.ContainsKey(sonido.Nombre))
                {
                    Debug.LogWarning("[Audio] Nombre de sonido repetido: " + sonido.Nombre, this);
                    continue;
                }

                indice.Add(sonido.Nombre, sonido);
            }
        }

        private void OnEnable()
        {
            indice = null;
        }

        private void OnValidate()
        {
            indice = null;

            foreach (SoundEvent sonido in sonidos)
            {
                if (sonido != null) sonido.Validar();
            }
        }
    }
}