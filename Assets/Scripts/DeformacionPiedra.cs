using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(Rigidbody))]
public class DeformacionPiedra : MonoBehaviour
{
    [Header("Optimización: Sueño Espacial (Anti-Jitter)")]
    public float intervaloComprobacion = 0.5f;
    public float umbralDistancia = 0.05f;
    public float umbralRotacion = 5.0f;

    [Header("Ajustes de Erosión")]
    public float radioDeImpacto = 0.5f;
    public float fuerzaDeErosion = 0.1f;
    public float fuerzaMinimaChoque = 1.0f;

    // --- NUEVO: Multiplicador dinámico de erosión (el Torbellino lo modificará) ---
    [HideInInspector]
    public float multiplicadorErosion = 1f;

    [Header("Efectos Visuales y Sonido (Optimización Inicial)")]
    public float retrasoEfectosInicial = 3.0f;

    [Header("Efectos Visuales")]
    public ParticleSystem particulasPolvo;
    public float fuerzaMinimaParticulas = 4.0f;
    public float fuerzaMaximaParticulas = 20.0f;
    public int minParticulasPorGolpe = 2;
    public int maxParticulasPorGolpe = 50;

    [Header("Deslizamiento Visual")]
    public float velocidadMinimaDeslizamiento = 3.0f;
    public float tiempoEntrePolvo = 0.05f;

    [Header("Sonidos de Impacto")]
    public AudioClip[] sonidosRebote;
    public float fuerzaParaVolumenMaximo = 15f;
    public float distanciaMinimaSonido = 2f;
    public float distanciaMaximaSonido = 30f;

    [Header("Sonidos de Deslizamiento")]
    public AudioClip sonidoDeslizamiento;
    public float velocidadMinimaAudio = 2f;
    public float velocidadMaximaAudio = 15f;
    public float maximaRotacionParaDeslizar = 12f;
    public float tiempoMinimoParaSonar = 0.1f;
    [Range(0.1f, 1f)] public float pitchMinimo = 0.7f;
    [Range(1f, 3f)] public float pitchMaximo = 1.5f;

    private Mesh malla;
    private Vector3[] vertices;
    private float radioObjetivoMinimo;
    private Rigidbody rb;

    private bool tocandoSuelo = false;
    private float tiempoDeslizando = 0f;
    private float temporizadorPolvo = 0f;
    private float tiempoNacimiento;
    private bool mallaClonada = false;

    private Vector3 posicionAnterior;
    private Quaternion rotacionAnterior;
    private float temporizadorEspacial = 0f;

    void Start()
    {
        tiempoNacimiento = Time.time;
        malla = GetComponent<MeshFilter>().sharedMesh;
        vertices = malla.vertices;

        rb = GetComponent<Rigidbody>();

        // --- SOLUCIÓN AL TUNNELLING (Atravesar el suelo) ---
        // Forzamos al motor de físicas a predecir la trayectoria entre fotogramas
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        CalcularRadioObjetivo();

        posicionAnterior = transform.position;
        rotacionAnterior = transform.rotation;
    }

    public void Despertar()
    {
        if (rb.isKinematic)
        {
            rb.isKinematic = false;
            rb.WakeUp();
        }

        posicionAnterior = transform.position;
        rotacionAnterior = transform.rotation;
        temporizadorEspacial = 0f;
    }

    void Dormir()
    {
        rb.isKinematic = true;
        if (GestorAudioPiedras.Instancia != null)
        {
            GestorAudioPiedras.Instancia.DetenerDeslizamiento(this);
        }
    }

    void FixedUpdate()
    {
        if (!rb.isKinematic)
        {
            temporizadorEspacial += Time.fixedDeltaTime;

            if (temporizadorEspacial >= intervaloComprobacion)
            {
                float distanciaRecorrida = Vector3.Distance(transform.position, posicionAnterior);
                float gradosGirados = Quaternion.Angle(transform.rotation, rotacionAnterior);

                if (distanciaRecorrida < umbralDistancia && gradosGirados < umbralRotacion)
                {
                    Dormir();
                }
                else
                {
                    posicionAnterior = transform.position;
                    rotacionAnterior = transform.rotation;
                }

                temporizadorEspacial = 0f;
            }
        }
    }

    void Update()
    {
        bool efectosPermitidos = Time.time > tiempoNacimiento + retrasoEfectosInicial;

        if (efectosPermitidos && !rb.isKinematic && GestorAudioPiedras.Instancia != null && sonidoDeslizamiento != null)
        {
            float vel = rb.linearVelocity.magnitude;
            float rot = rb.angularVelocity.magnitude;

            if (tocandoSuelo && vel > velocidadMinimaAudio && rot < maximaRotacionParaDeslizar)
            {
                tiempoDeslizando += Time.deltaTime;
                if (tiempoDeslizando > tiempoMinimoParaSonar)
                {
                    float vol = Mathf.InverseLerp(velocidadMinimaAudio, velocidadMaximaAudio, vel);
                    float pct = Mathf.InverseLerp(velocidadMinimaAudio, velocidadMaximaAudio, vel);
                    float pitch = Mathf.Lerp(pitchMinimo, pitchMaximo, pct);

                    GestorAudioPiedras.Instancia.ActualizarDeslizamiento(this, sonidoDeslizamiento, transform.position, vol, pitch, distanciaMinimaSonido, distanciaMaximaSonido);
                }
            }
            else
            {
                tiempoDeslizando = 0f;
                GestorAudioPiedras.Instancia.DetenerDeslizamiento(this);
            }
        }
        else if (rb.isKinematic || !efectosPermitidos)
        {
            if (GestorAudioPiedras.Instancia != null)
            {
                GestorAudioPiedras.Instancia.DetenerDeslizamiento(this);
            }
        }
    }

    void CalcularRadioObjetivo()
    {
        radioObjetivoMinimo = float.MaxValue;
        foreach (Vector3 vertice in vertices)
        {
            float distanciaAlCentro = vertice.magnitude;
            if (distanciaAlCentro < radioObjetivoMinimo) radioObjetivoMinimo = distanciaAlCentro;
        }
    }

    void OnCollisionEnter(Collision colision)
    {
        float fuerzaChoque = colision.relativeVelocity.magnitude;

        if (rb.isKinematic)
        {
            if (colision.gameObject.CompareTag("Player") || fuerzaChoque > 0.5f) Despertar();
            else return;
        }

        if (colision.gameObject.CompareTag("Player")) return;

        tocandoSuelo = true;
        bool efectosPermitidos = Time.time > tiempoNacimiento + retrasoEfectosInicial;

        if (efectosPermitidos && fuerzaChoque > 0.1f && sonidosRebote.Length > 0 && GestorAudioPiedras.Instancia != null)
        {
            float porcentajeFuerza = Mathf.InverseLerp(0f, fuerzaParaVolumenMaximo, fuerzaChoque);
            float volumenExponencial = Mathf.Pow(porcentajeFuerza, 2f);
            float pitchAletorio = Random.Range(0.85f, 1.15f);
            int idx = Random.Range(0, sonidosRebote.Length);

            GestorAudioPiedras.Instancia.ReproducirImpacto(sonidosRebote[idx], transform.position, volumenExponencial, pitchAletorio, distanciaMinimaSonido, distanciaMaximaSonido);
        }

        if (efectosPermitidos && fuerzaChoque >= fuerzaMinimaParticulas && particulasPolvo != null)
        {
            ContactPoint contacto = colision.GetContact(0);
            EmitirPolvo(contacto.point, contacto.normal, fuerzaMinimaParticulas, fuerzaMaximaParticulas, fuerzaChoque, minParticulasPorGolpe, maxParticulasPorGolpe);
        }

        if (fuerzaChoque < fuerzaMinimaChoque) return;
        if (colision.gameObject.CompareTag("Piedra")) return;

        ContactPoint contactoDeformacion = colision.GetContact(0);
        DeformarMalla(transform.InverseTransformPoint(contactoDeformacion.point));
    }

    void OnCollisionStay(Collision colision)
    {
        if (colision.gameObject.CompareTag("Player") || colision.gameObject.CompareTag("Piedra")) return;

        tocandoSuelo = true;

        if (particulasPolvo == null || rb.isKinematic) return;
        if (Time.time < tiempoNacimiento + retrasoEfectosInicial) return;

        float velocidadActual = rb.linearVelocity.magnitude;
        if (velocidadActual > velocidadMinimaDeslizamiento)
        {
            temporizadorPolvo -= Time.fixedDeltaTime;
            if (temporizadorPolvo <= 0f)
            {
                ContactPoint contacto = colision.GetContact(0);
                EmitirPolvo(contacto.point, contacto.normal, velocidadMinimaDeslizamiento, velocidadMinimaDeslizamiento + 10f, velocidadActual, 1, 3);
                temporizadorPolvo = tiempoEntrePolvo;
            }
        }
    }

    void OnCollisionExit(Collision colision)
    {
        if (colision.gameObject.CompareTag("Player")) return;
        tocandoSuelo = false;
        tiempoDeslizando = 0f;
    }

    void EmitirPolvo(Vector3 pos, Vector3 norm, float minF, float maxF, float fActual, int minP, int maxP)
    {
        particulasPolvo.transform.position = pos;
        particulasPolvo.transform.rotation = Quaternion.LookRotation(norm);
        float pct = Mathf.InverseLerp(minF, maxF, fActual);
        particulasPolvo.Emit(Mathf.RoundToInt(Mathf.Lerp(minP, maxP, pct)));
    }

    public void DeformarMalla(Vector3 puntoImpactoLocal)
    {
        bool mallaModificada = false;

        float fuerzaErosionFinal = fuerzaDeErosion * multiplicadorErosion;

        for (int i = 0; i < vertices.Length; i++)
        {
            float dist = Vector3.Distance(vertices[i], puntoImpactoLocal);
            if (dist < radioDeImpacto)
            {
                float distCentro = vertices[i].magnitude;
                if (distCentro > radioObjetivoMinimo)
                {
                    float atenuacion = 1f - (dist / radioDeImpacto);
                    float nuevaDist = Mathf.Max(distCentro - (fuerzaErosionFinal * atenuacion), radioObjetivoMinimo);

                    vertices[i] = vertices[i].normalized * nuevaDist;
                    mallaModificada = true;
                }
            }
        }

        if (mallaModificada)
        {
            if (!mallaClonada)
            {
                malla = Instantiate(GetComponent<MeshFilter>().sharedMesh);
                mallaClonada = true;
            }

            malla.vertices = vertices;
            malla.RecalculateNormals();
            malla.RecalculateBounds();

            GetComponent<MeshFilter>().sharedMesh = malla;
            GetComponent<MeshCollider>().sharedMesh = malla;
        }
    }
    public void PulirUniformemente(float cantidadDesgaste)
    {
        bool mallaModificada = false;

        for (int i = 0; i < vertices.Length; i++)
        {
            float distCentro = vertices[i].magnitude;

            if (distCentro > radioObjetivoMinimo)
            {
                float nuevaDist = Mathf.Max(distCentro - cantidadDesgaste, radioObjetivoMinimo);
                vertices[i] = vertices[i].normalized * nuevaDist;
                mallaModificada = true;
            }
        }

        if (mallaModificada)
        {
            if (!mallaClonada)
            {
                malla = Instantiate(GetComponent<MeshFilter>().sharedMesh);
                mallaClonada = true;
            }

            malla.vertices = vertices;
            malla.RecalculateNormals();
            malla.RecalculateBounds();

            GetComponent<MeshFilter>().sharedMesh = malla;
            GetComponent<MeshCollider>().sharedMesh = malla;
        }
    }
    public float ObtenerPorcentajeDesgasteHaciaEsfera()
    {
        if (malla == null) return 0f;
        Vector3[] verts = malla.vertices;
        if (verts.Length == 0) return 0f;

        float sumaDistanciasActuales = 0f;
        foreach (Vector3 v in verts)
        {
            sumaDistanciasActuales += v.magnitude;
        }
        float radioActualPromedio = sumaDistanciasActuales / verts.Length;
        float radioMaximoInicial = radioObjetivoMinimo + (radioDeImpacto * 2f);
        float progreso = Mathf.InverseLerp(radioMaximoInicial, radioObjetivoMinimo, radioActualPromedio);

        return Mathf.Clamp01(progreso) * 100f;
    }

    void OnDestroy()
    {
        if (GestorAudioPiedras.Instancia != null)
        {
            GestorAudioPiedras.Instancia.DetenerDeslizamiento(this);
        }
    }
}