using UnityEngine;

namespace PiedrasIncr.Audio
{
    /// <summary>Reproduce un sonido de la lista sin escribir codigo.</summary>
    [AddComponentMenu("Audio/Sound Emitter")]
    public sealed class SoundEmitter : MonoBehaviour
    {
        [SoundName]
        [SerializeField] private string sonido;

        [Tooltip("Suena automaticamente cuando el objeto se activa.")]
        [SerializeField] private bool alActivar = true;

        [Tooltip("El sonido se mueve con el objeto. Si no, se queda donde empezo.")]
        [SerializeField] private bool seguirObjeto = true;

        [Tooltip("Activalo para bucles (motores, ambiente) que deban callarse al desactivar el objeto.")]
        [SerializeField] private bool pararAlDesactivar = false;

        private AudioHandle handle;

        private void OnEnable()
        {
            if (alActivar) Play();
        }

        private void OnDisable()
        {
            if (pararAlDesactivar) handle.Stop();
        }

        // Publicos para poder llamarlos desde un boton o un UnityEvent.
        public void Play()
        {
            handle = seguirObjeto
                ? AudioManager.PlayAttached(sonido, transform)
                : AudioManager.Play(sonido, transform.position);
        }

        public void Stop()
        {
            handle.Stop();
        }
    }
}