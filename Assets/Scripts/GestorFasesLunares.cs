using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GestorFasesLunares : MonoBehaviour
{
    [Serializable]
    public class FaseLunar
    {
        [Header("Fase")]
        public string nombreFase;

        [Tooltip("El objeto raíz de esta fase (el que contiene todo)")]
        public GameObject visualFase;

        public Transform puntoImpactoMisil;

        // --- NUEVO: Lista de objetos específicos a vibrar ---
        [Header("Vibración")]
        [Tooltip("Arrastra aquí SOLAMENTE las mallas/modelos 3D de esta luna que deben temblar, para no afectar a la raíz ni a las partículas.")]
        public Transform[] objetosVibracion;

        [Header("Forma del Cinturón")]
        public CinturonAsteroides.FormaAnillo formaCinturon = new CinturonAsteroides.FormaAnillo
        {
            cantidadAsteroides = 200,
            radioInterior = 40f,
            radioExterior = 60f,
            elevacionCinturon = 10f,
            grosorAltura = 5f,
            escalaHumo = new Vector3(100f, 100f, 100f)
        };

        [Header("Efectos Visuales")]
        public ParticleSystem[] particulasExplosion;

        [Header("Meteoritos")]
        public bool lluviaActiva = true;
        public float tiempoEntreSpawns = 2f;
        [Min(1)] public int cantidadPorOleada = 1;
    }

    [Header("Fases")]
    public FaseLunar[] fases;

    [Header("Meteoritos y Cinturón")]
    public LluviaMeteoritos lluviaMeteoritos;
    public CinturonAsteroides cinturonAsteroides;

    [Header("Sistema de Misil (Mortero)")]
    public GameObject prefabMisil;
    public Transform puntoLanzamientoMisil;
    public float velocidadMisil = 100f;
    public float alturaArcoMisil = 80f;
    public AnimationCurve curvaAceleracionMisil = AnimationCurve.Linear(0f, 2f, 1f, 0.5f);

    [Header("Efectos del Misil")]
    public GameObject prefabImpactoMisil;
    public AudioClip sonidoLanzamientoMisil;
    public AudioClip sonidoImpactoMisil;

    [Header("Vibración de Impacto en la Luna")]
    public float duracionVibracionLuna = 0.25f;
    public float magnitudVibracionLuna = 0.5f;

    [Header("Controles y Tiempos")]
    public KeyCode teclaAvanzarFase = KeyCode.K;
    public KeyCode teclaResetFase = KeyCode.R;
    public float retrasoCambioModelo = 0.25f;

    [Header("Alineación de la Luna")]
    public KeyCode teclaAlinearLuna = KeyCode.L;
    public Vector3 rotacionFijada = Vector3.zero;

    [Header("Estado actual")]
    [SerializeField] private int faseActual = 0;

    public int FaseActual => faseActual;
    public event Action<int> OnFaseCambiada;

    private bool enTransicion = false;
    private bool lunaAlineada = false;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (fases != null)
        {
            foreach (FaseLunar fase in fases)
            {
                if (fase.particulasExplosion != null)
                {
                    foreach (ParticleSystem ps in fase.particulasExplosion)
                    {
                        if (ps != null) ps.gameObject.SetActive(false);
                    }
                }
            }

            if (cinturonAsteroides != null && fases.Length > 0)
            {
                cinturonAsteroides.AplicarFormaInmediata(fases[0].formaCinturon);
            }
        }

        AplicarFaseActual();
    }

    private void Update()
    {
        if (Input.GetKeyDown(teclaAvanzarFase) && !enTransicion)
        {
            if (fases != null && faseActual < fases.Length - 1)
            {
                StartCoroutine(RutinaLanzarMisilMortero());
            }
            else
            {
                Debug.Log("La Luna ya está en su fase máxima.");
            }
        }

        if (Input.GetKeyDown(teclaAlinearLuna) && !enTransicion && !lunaAlineada)
        {
            AlinearLuna();
        }

        if (Input.GetKeyDown(teclaResetFase) && !enTransicion)
        {
            ResetearFase();
        }
    }

    private void ResetearFase()
    {
        if (fases == null || fases.Length == 0) return;

        faseActual = 0;
        lunaAlineada = false;

        if (cinturonAsteroides != null)
        {
            cinturonAsteroides.rotacionPausada = false;
            cinturonAsteroides.AplicarFormaInmediata(fases[0].formaCinturon);
        }

        AplicarFaseActual();

        Debug.Log("Simulación reseteada. Volviendo a la fase: " + fases[faseActual].nombreFase);
        OnFaseCambiada?.Invoke(faseActual);
    }

    private void AlinearLuna()
    {
        lunaAlineada = true;
        if (fases[faseActual].visualFase != null)
        {
            fases[faseActual].visualFase.transform.rotation = Quaternion.Euler(rotacionFijada);
        }
        if (cinturonAsteroides != null)
        {
            cinturonAsteroides.rotacionPausada = true;
        }
    }

    private IEnumerator RutinaLanzarMisilMortero()
    {
        enTransicion = true;

        if (prefabMisil != null && puntoLanzamientoMisil != null && fases[faseActual].puntoImpactoMisil != null)
        {
            if (sonidoLanzamientoMisil != null && audioSource != null)
            {
                audioSource.PlayOneShot(sonidoLanzamientoMisil);
            }

            Vector3 origen = puntoLanzamientoMisil.position;
            Vector3 destino = fases[faseActual].puntoImpactoMisil.position;

            Vector3 puntoControl = origen + (destino - origen) / 2f + (Vector3.up * alturaArcoMisil);
            float distanciaTotal = Vector3.Distance(origen, destino);

            GameObject misil = Instantiate(prefabMisil, origen, Quaternion.identity);
            float t = 0f;

            while (misil != null && t < 1f)
            {
                float multiplicadorVelocidad = curvaAceleracionMisil.Evaluate(t);
                float velocidadFotograma = velocidadMisil * multiplicadorVelocidad;

                t += (velocidadFotograma / distanciaTotal) * Time.deltaTime;
                if (t > 1f) t = 1f;

                float u = 1f - t;
                Vector3 siguientePosicion = (u * u * origen) + (2f * u * t * puntoControl) + (t * t * destino);

                Vector3 direccionMovimiento = (siguientePosicion - misil.transform.position).normalized;
                if (direccionMovimiento != Vector3.zero)
                {
                    misil.transform.rotation = Quaternion.LookRotation(direccionMovimiento);
                }

                misil.transform.position = siguientePosicion;
                yield return null;
            }

            if (misil != null) Destroy(misil);

            if (prefabImpactoMisil != null)
            {
                GameObject impacto = Instantiate(prefabImpactoMisil, destino, Quaternion.identity);
                Destroy(impacto, 8f);
            }

            if (sonidoImpactoMisil != null && audioSource != null)
            {
                audioSource.PlayOneShot(sonidoImpactoMisil);
            }
        }

        // --- SOLUCIÓN ACTUALIZADA: Guardamos las posiciones originales de TODOS los objetos a vibrar ---
        Transform[] objetosAVibrar = fases[faseActual].objetosVibracion;
        Vector3[] posicionesGuardadas = new Vector3[0];
        Quaternion[] rotacionesGuardadas = new Quaternion[0];
        Coroutine corrutinaVibracion = null;

        if (objetosAVibrar != null && objetosAVibrar.Length > 0)
        {
            posicionesGuardadas = new Vector3[objetosAVibrar.Length];
            rotacionesGuardadas = new Quaternion[objetosAVibrar.Length];

            for (int i = 0; i < objetosAVibrar.Length; i++)
            {
                if (objetosAVibrar[i] != null)
                {
                    posicionesGuardadas[i] = objetosAVibrar[i].localPosition;
                    rotacionesGuardadas[i] = objetosAVibrar[i].localRotation;
                }
            }

            corrutinaVibracion = StartCoroutine(RutinaVibrarObjetos(objetosAVibrar, posicionesGuardadas, rotacionesGuardadas, duracionVibracionLuna, magnitudVibracionLuna));
        }

        yield return StartCoroutine(RutinaAvanzarFase(corrutinaVibracion, objetosAVibrar, posicionesGuardadas, rotacionesGuardadas));
    }

    // --- NUEVO: Corrutina que hace vibrar listas de objetos al unísono ---
    private IEnumerator RutinaVibrarObjetos(Transform[] objetos, Vector3[] posOriginales, Quaternion[] rotOriginales, float duracion, float magnitud)
    {
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracion)
        {
            // Calculamos un solo desfase (offset) global para este fotograma
            // Así todos los objetos se mueven en la misma dirección sin separarse
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitud;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitud;
            float z = UnityEngine.Random.Range(-1f, 1f) * magnitud;

            float rotMag = magnitud * 4f;
            float rX = UnityEngine.Random.Range(-1f, 1f) * rotMag;
            float rY = UnityEngine.Random.Range(-1f, 1f) * rotMag;
            float rZ = UnityEngine.Random.Range(-1f, 1f) * rotMag;

            Vector3 posOffset = new Vector3(x, y, z);
            Quaternion rotOffset = Quaternion.Euler(rX, rY, rZ);

            // Aplicamos el desfase a todos los objetos
            for (int i = 0; i < objetos.Length; i++)
            {
                if (objetos[i] != null)
                {
                    objetos[i].localPosition = posOriginales[i] + posOffset;
                    objetos[i].localRotation = rotOriginales[i] * rotOffset;
                }
            }

            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        // Restauramos al final si la rutina termina de forma natural
        for (int i = 0; i < objetos.Length; i++)
        {
            if (objetos[i] != null)
            {
                objetos[i].localPosition = posOriginales[i];
                objetos[i].localRotation = rotOriginales[i];
            }
        }
    }

    // --- ACTUALIZADO ---
    private IEnumerator RutinaAvanzarFase(Coroutine vibracion, Transform[] objetosVibrando, Vector3[] posicionesOriginales, Quaternion[] rotacionesOriginales)
    {
        int proximaFase = faseActual + 1;

        if (fases[faseActual].particulasExplosion != null)
        {
            foreach (ParticleSystem psOriginal in fases[faseActual].particulasExplosion)
            {
                if (psOriginal != null)
                {
                    ParticleSystem psClon = Instantiate(psOriginal, psOriginal.transform.position, psOriginal.transform.rotation);
                    psClon.transform.SetParent(null);
                    psClon.gameObject.SetActive(true);
                    psClon.Play(true);
                    Destroy(psClon.gameObject, 10f);
                }
            }
        }

        if (cinturonAsteroides != null)
        {
            cinturonAsteroides.CambiarForma(fases[proximaFase].formaCinturon);
            cinturonAsteroides.ExplotarCinturon();
        }

        yield return new WaitForSeconds(retrasoCambioModelo);

        // --- SEGURIDAD: Paramos la vibración y restauramos todos los objetos que temblaban ---
        if (vibracion != null) StopCoroutine(vibracion);

        if (objetosVibrando != null)
        {
            for (int i = 0; i < objetosVibrando.Length; i++)
            {
                if (objetosVibrando[i] != null)
                {
                    objetosVibrando[i].localPosition = posicionesOriginales[i];
                    objetosVibrando[i].localRotation = rotacionesOriginales[i];
                }
            }
        }

        // Como visualFase (la raíz) ya no tiembla, leer su rotación es 100% seguro y limpio
        Quaternion rotacionLimpia = Quaternion.identity;
        if (fases[faseActual].visualFase != null)
        {
            rotacionLimpia = fases[faseActual].visualFase.transform.rotation;
        }

        faseActual = proximaFase;

        if (fases[faseActual].visualFase != null)
        {
            fases[faseActual].visualFase.transform.rotation = rotacionLimpia;
        }

        AplicarFaseActual();

        lunaAlineada = false;
        if (cinturonAsteroides != null)
        {
            cinturonAsteroides.rotacionPausada = false;
        }

        Debug.Log("Luna avanzada a: " + fases[faseActual].nombreFase);
        OnFaseCambiada?.Invoke(faseActual);

        enTransicion = false;
    }

    private void AplicarFaseActual()
    {
        if (fases == null || fases.Length == 0 || faseActual < 0 || faseActual >= fases.Length) return;

        for (int i = 0; i < fases.Length; i++)
        {
            if (fases[i].visualFase != null)
            {
                bool esFaseActiva = (i == faseActual);
                fases[i].visualFase.SetActive(esFaseActiva);

                if (esFaseActiva && cinturonAsteroides != null)
                {
                    cinturonAsteroides.objetivoOrbita = fases[i].visualFase.transform;
                }
            }
        }

        if (lluviaMeteoritos != null)
        {
            FaseLunar fase = fases[faseActual];
            lluviaMeteoritos.ConfigurarLluvia(fase.tiempoEntreSpawns, fase.cantidadPorOleada);
            lluviaMeteoritos.ActivarLluvia(fase.lluviaActiva);
        }
    }
}