using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(Rigidbody))]
public class DeformacionPiedra : MonoBehaviour
{
    [Header("Optimización: Sueño Espacial (Anti-Jitter)")]
    public float intervaloComprobacion = 0.5f;
    public float umbralDistancia = 0.05f;
    public float umbralRotacion = 5.0f;

    [Header("Optimización: Rendimiento con muchas piedras")]
    [Tooltip("Detección de colisión. ContinuousDynamic detecta bien los impactos (desgaste); ContinuousSpeculative es más barata pero suaviza los choques.")]
    public CollisionDetectionMode modoDeteccionColision = CollisionDetectionMode.ContinuousDynamic;
    [Tooltip("Segundos mínimos entre reconstrucciones del MeshCollider (rehacer el collider convexo es lo más caro). La malla visible se actualiza siempre al instante.")]
    [Min(0f)] public float intervaloActualizacionCollider = 0.3f;
    [Tooltip("Segundos mínimos entre dos deformaciones por choque de la misma piedra")]
    [Min(0f)] public float tiempoEntreDeformaciones = 0.08f;

    [Header("Desgaste por arrastre (rozando el suelo, p. ej. en el torbellino)")]
    [Tooltip("Velocidad mínima a la que la piedra se desgasta mientras roza algo que no es otra piedra")]
    [Min(0f)] public float velocidadMinimaErosionArrastre = 3f;
    [Tooltip("Segundos entre desgastes mientras roza (más bajo = se desgasta más rápido)")]
    [Min(0f)] public float intervaloErosionArrastre = 0.1f;

    [Header("Ajustes de Erosión")]
    public float radioDeImpacto = 0.5f;
    public float fuerzaDeErosion = 0.1f;
    public float fuerzaMinimaChoque = 1.0f;

    [Header("Forma Final (100% Desgaste)")]
    [Tooltip("El tamaño (radio local) exacto que tendrá la piedra al estar pulida al 100%. Ajústalo para que sea MÁS PEQUEÑO que tu esfera hija de color.")]
    public float radioFinalEsfera = 0.4f;

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

    [Header("Colores de Partículas (Desgaste > 95%)")]
    [ColorUsage(true, true)]
    public Color colorParticulasNormal = Color.white;
    [ColorUsage(true, true)]
    public Color colorParticulasErosionado = Color.yellow;

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

    private MeshFilter filtroMalla;
    private MeshCollider colliderMalla;
    private bool colliderPendiente;
    private float proximaActualizacionCollider;
    private float ultimaDeformacion = -999f;
    // El desgaste solo cambia al deformar: se cachea en vez de copiar todos los vértices en cada consulta.
    private float desgasteCacheado;
    private bool desgasteSucio = true;

    void Start()
    {
        tiempoNacimiento = Time.time;
        filtroMalla = GetComponent<MeshFilter>();
        malla = filtroMalla.sharedMesh;
        vertices = malla.vertices;
        desgasteSucio = true;

        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = modoDeteccionColision;

        colliderMalla = GetComponent<MeshCollider>();
        if (colliderMalla != null) colliderMalla.convex = true;

        CalcularRadioObjetivo();

        posicionAnterior = transform.position;
        rotacionAnterior = transform.rotation;
    }

    public void Despertar()
    {
        // CORRECCIÓN: Solo reseteamos las variables y la despertamos si estaba realmente dormida
        if (rb.isKinematic)
        {
            rb.isKinematic = false;
            rb.WakeUp();
            posicionAnterior = transform.position;
            rotacionAnterior = transform.rotation;
            temporizadorEspacial = 0f;
        }
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
        if (colliderPendiente && Time.time >= proximaActualizacionCollider)
            AplicarCollider();

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
        radioObjetivoMinimo = radioFinalEsfera;
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

        if (rb.isKinematic) return;
        if (Time.time < tiempoNacimiento + retrasoEfectosInicial) return;

        float velocidadActual = rb.linearVelocity.magnitude;

        // Al no reconstruir el collider en cada golpe, el contacto sigue como Stay y ya no se repite
        // OnCollisionEnter: el desgaste mientras se arrastra se hace aquí, con su propio ritmo.
        if (velocidadActual >= velocidadMinimaErosionArrastre && Time.time - ultimaDeformacion >= intervaloErosionArrastre)
            DeformarMalla(transform.InverseTransformPoint(colision.GetContact(0).point));

        if (particulasPolvo == null) return;

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
        // Con las partículas desactivadas no se calcula nada.
        if (!particulasPolvo.gameObject.activeInHierarchy) return;

        particulasPolvo.transform.position = pos;
        particulasPolvo.transform.rotation = Quaternion.LookRotation(norm);

        float desgaste = ObtenerPorcentajeDesgasteHaciaEsfera();
        var main = particulasPolvo.main;

        if (desgaste >= 95f)
        {
            main.startColor = colorParticulasErosionado;
        }
        else
        {
            main.startColor = colorParticulasNormal;
        }

        float pct = Mathf.InverseLerp(minF, maxF, fActual);
        particulasPolvo.Emit(Mathf.RoundToInt(Mathf.Lerp(minP, maxP, pct)));
    }

    public void DeformarMalla(Vector3 puntoImpactoLocal)
    {
        // En el torbellino una piedra choca muchas veces por segundo: se limita la frecuencia.
        if (Time.time - ultimaDeformacion < tiempoEntreDeformaciones) return;

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
            ultimaDeformacion = Time.time;
            ActualizarMalla();
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

        if (mallaModificada) ActualizarMalla();
    }

    // La malla visible se actualiza al instante; el MeshCollider convexo (caro de reconstruir) como mucho
    // cada intervaloActualizacionCollider segundos.
    void ActualizarMalla()
    {
        if (filtroMalla == null) filtroMalla = GetComponent<MeshFilter>();

        if (!mallaClonada)
        {
            malla = Instantiate(filtroMalla.sharedMesh);
            mallaClonada = true;
        }

        malla.vertices = vertices;
        malla.RecalculateNormals();
        malla.RecalculateBounds();
        filtroMalla.sharedMesh = malla;

        desgasteSucio = true;
        colliderPendiente = true;
    }

    void AplicarCollider()
    {
        colliderPendiente = false;
        proximaActualizacionCollider = Time.time + intervaloActualizacionCollider;

        if (colliderMalla == null) return;
        colliderMalla.sharedMesh = malla;
        colliderMalla.convex = true;
    }

    public float ObtenerPorcentajeDesgasteHaciaEsfera()
    {
        if (malla == null) return 0f;
        if (!desgasteSucio) return desgasteCacheado;

        Vector3[] verts = vertices ?? malla.vertices;
        if (verts.Length == 0) return 0f;

        float sumaDistanciasActuales = 0f;
        foreach (Vector3 v in verts)
        {
            sumaDistanciasActuales += v.magnitude;
        }
        float radioActualPromedio = sumaDistanciasActuales / verts.Length;
        float radioMaximoInicial = radioObjetivoMinimo + (radioDeImpacto * 2f);
        float progreso = Mathf.InverseLerp(radioMaximoInicial, radioObjetivoMinimo, radioActualPromedio);

        desgasteCacheado = Mathf.Clamp01(progreso) * 100f;
        desgasteSucio = false;
        return desgasteCacheado;
    }

    void OnDestroy()
    {
        if (GestorAudioPiedras.Instancia != null)
        {
            GestorAudioPiedras.Instancia.DetenerDeslizamiento(this);
        }
    }
}