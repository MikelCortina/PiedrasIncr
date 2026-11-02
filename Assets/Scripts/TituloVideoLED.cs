using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Reproduce el vídeo del título (WebM VP8 con transparencia y el zumbido del LED) en la RawImage del Canvas.
// El volumen sigue a la transparencia de la RawImage, así el sonido se desvanece junto con el fundido
// de CambioCamaraIntro, y se para solo cuando la intro desactiva el objeto.
[RequireComponent(typeof(RawImage))]
public class TituloVideoLED : MonoBehaviour
{
    [Tooltip("Vídeo del logo con canal alfa (WebM VP8)")]
    public VideoClip video;
    [Tooltip("Repite el vídeo mientras el título siga en pantalla")]
    public bool enBucle = true;
    [Tooltip("Volumen del zumbido del LED")]
    [Range(0f, 1f)] public float volumen = 1f;
    [Tooltip("El volumen baja a la vez que la transparencia de la imagen (fundidos de la intro)")]
    public bool volumenSigueTransparencia = true;

    private RawImage imagen;
    private VideoPlayer reproductor;
    private AudioSource sonido;

    private void Awake()
    {
        imagen = GetComponent<RawImage>();

        // Sin textura la RawImage pintaría un recuadro blanco hasta que el vídeo esté listo.
        imagen.enabled = false;

        sonido = GetComponent<AudioSource>();
        if (sonido == null)
            sonido = gameObject.AddComponent<AudioSource>();

        sonido.playOnAwake = false;
        sonido.spatialBlend = 0f;

        reproductor = GetComponent<VideoPlayer>();
        if (reproductor == null)
            reproductor = gameObject.AddComponent<VideoPlayer>();

        reproductor.playOnAwake = false;
        reproductor.source = VideoSource.VideoClip;
        reproductor.clip = video;
        reproductor.isLooping = enBucle;
        reproductor.renderMode = VideoRenderMode.APIOnly;
        reproductor.audioOutputMode = VideoAudioOutputMode.AudioSource;
        reproductor.SetTargetAudioSource(0, sonido);
        reproductor.prepareCompleted += AlPrepararVideo;
    }

    private void OnEnable()
    {
        if (video == null)
        {
            Debug.LogWarning("TituloVideoLED: falta asignar el vídeo.", this);
            return;
        }

        reproductor.Prepare();
    }

    private void OnDestroy()
    {
        if (reproductor != null)
            reproductor.prepareCompleted -= AlPrepararVideo;
    }

    private void AlPrepararVideo(VideoPlayer preparado)
    {
        imagen.texture = preparado.texture;
        imagen.enabled = true;
        preparado.Play();
    }

    private void Update()
    {
        sonido.volume = volumen * (volumenSigueTransparencia ? imagen.color.a : 1f);
    }
}
