using UnityEngine;
using System.Collections.Generic;

public class GestorAudioPiedras : MonoBehaviour
{
    public static GestorAudioPiedras Instancia;

    [Header("Ajustes de Optimización")]
    public int maxImpactosSimultaneos = 15;
    public int maxDeslizamientosSimultaneos = 10;

    [Header("Culling de Audio (Rendimiento)")]
    [Tooltip("Referencia al jugador o cámara que escucha. Si se deja vacío, buscará automáticamente la Main Camera.")]
    public Transform oyente;
    [Tooltip("Distancia máxima a partir de la cual las piedras no ocuparán reproductores de audio.")]
    public float distanciaMaximaEscucha = 100f;

    private List<AudioSource> poolImpactos = new List<AudioSource>();
    private List<AudioSource> poolDeslizamientos = new List<AudioSource>();

    // Diccionario para saber qué reproductor está usando cada piedra actualmente
    private Dictionary<DeformacionPiedra, AudioSource> deslizamientosActivos = new Dictionary<DeformacionPiedra, AudioSource>();

    void Awake()
    {
        if (Instancia == null) Instancia = this;
        else { Destroy(gameObject); return; }

        // 1. Pool para los golpes secos
        for (int i = 0; i < maxImpactosSimultaneos; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.playOnAwake = false;
            poolImpactos.Add(src);
        }

        // 2. Pool para los deslizamientos continuos
        for (int i = 0; i < maxDeslizamientosSimultaneos; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.playOnAwake = false;
            src.loop = true; // El deslizamiento siempre es en bucle
            poolDeslizamientos.Add(src);
        }
    }

    void Start()
    {
        // Si no asignaste un oyente manual en el Inspector, usa la cámara principal
        if (oyente == null && Camera.main != null)
        {
            oyente = Camera.main.transform;
        }
    }

    public void ReproducirImpacto(AudioClip clip, Vector3 posicion, float volumen, float pitch, float minDist, float maxDist)
    {
        // --- FILTRO DE DISTANCIA (CULLING) ---
        if (oyente != null && Vector3.Distance(oyente.position, posicion) > distanciaMaximaEscucha)
        {
            return; // Está muy lejos, abortamos para no gastar recursos
        }

        AudioSource candidato = poolImpactos[0];
        float mayorProgreso = -1f;

        // Buscamos un reproductor libre o reciclamos el que esté más cerca de terminar
        foreach (var src in poolImpactos)
        {
            if (!src.isPlaying) { candidato = src; break; }

            float progreso = src.time / src.clip.length;
            if (progreso > mayorProgreso)
            {
                mayorProgreso = progreso;
                candidato = src;
            }
        }

        candidato.transform.position = posicion;
        candidato.clip = clip;
        candidato.volume = volumen;
        candidato.pitch = pitch;
        candidato.minDistance = minDist;
        candidato.maxDistance = maxDist;
        candidato.Play();
    }

    public void ActualizarDeslizamiento(DeformacionPiedra piedra, AudioClip clip, Vector3 pos, float vol, float pitch, float minDist, float maxDist)
    {
        // --- FILTRO DE DISTANCIA (CULLING) ---
        if (oyente != null && Vector3.Distance(oyente.position, pos) > distanciaMaximaEscucha)
        {
            // Si la piedra estaba sonando pero se alejó demasiado, le apagamos el audio para reciclar el reproductor
            DetenerDeslizamiento(piedra);
            return;
        }

        AudioSource fuente;

        // Si la piedra ya tiene un reproductor asignado, lo usamos
        if (deslizamientosActivos.ContainsKey(piedra))
        {
            fuente = deslizamientosActivos[piedra];
        }
        else
        {
            // Si no, buscamos uno libre en el pool
            fuente = null;
            foreach (var src in poolDeslizamientos)
            {
                if (!src.isPlaying)
                {
                    fuente = src;
                    break;
                }
            }

            // Si hay uno libre, se lo asignamos a esta piedra
            if (fuente != null)
            {
                deslizamientosActivos[piedra] = fuente;
            }
            else return; // Si no hay reproductores libres, esta piedra no suena para no saturar
        }

        // Actualizamos los datos del reproductor
        if (fuente.clip != clip || !fuente.isPlaying)
        {
            fuente.clip = clip;
            fuente.minDistance = minDist;
            fuente.maxDistance = maxDist;
            fuente.Play();
        }

        fuente.transform.position = pos;
        fuente.volume = vol;
        fuente.pitch = pitch;
    }

    public void DetenerDeslizamiento(DeformacionPiedra piedra)
    {
        if (deslizamientosActivos.ContainsKey(piedra))
        {
            AudioSource fuente = deslizamientosActivos[piedra];
            if (fuente.isPlaying)
            {
                fuente.Stop();
            }
            // Liberamos el reproductor para que otra piedra pueda usarlo inmediatamente
            deslizamientosActivos.Remove(piedra);
        }
    }
}