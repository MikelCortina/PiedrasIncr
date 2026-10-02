using System.Collections;
using UnityEngine;

public class DianaBloop : MonoBehaviour
{
    [Header("Referencias Visuales (Bloop)")]
    public Transform modeloVisual;
    public Transform modeloSalidaVisual;

    [Header("Animación Banderín (Sway / Corner Flag)")]
    public Transform modeloBanderin;
    public float inclinacionBanderin = 45f;
    public float velocidadBalanceo = 20f;
    public float amortiguacionBalanceo = 3.5f;

    [Header("Efectos de Impacto (VFX Generales)")]
    public GameObject prefabVFXImpacto;
    public float offsetSuperficieVFX = 0.08f;

    [Header("Sistema de Cooldown y Color")]
    public Renderer rendererVisual;
    public string propiedadColor = "_Color";
    public float tiempoCooldown = 2.5f;

    public Color colorDescargado = Color.red;
    public float intensidadDescargado = 0f;
    public Color colorListo = Color.cyan;
    public float intensidadListoMin = 1.5f;
    public float intensidadListoMax = 3.5f;
    public float velocidadOscilacion = 3f;

    // ====================================================================
    // SISTEMA DE PARTÍCULAS CON INVERSIÓN Y EXPLOSIÓN
    // ====================================================================
    [Header("Sistema de Partículas (Carga y Listo)")]
    [Tooltip("Sistema principal que succiona energía y luego la expulsa.")]
    public ParticleSystem psCargando;
    public float tasaEmisionCargandoMin = 5f;
    public float tasaEmisionCargandoMax = 40f;

    // Velocidades negativas para que la partícula vaya "hacia adentro" durante la carga
    public float startSpeedInicioMin = -2.5f;
    public float startSpeedInicioMax = -2.0f;
    public float startSpeedFinMin = -9.0f;
    public float startSpeedFinMax = -3.0f;

    [Tooltip("Multiplicador de velocidad para dar el efecto de latigazo/explosión antes de darse la vuelta")]
    public float factorAceleracionExplosion = 4.5f;
    [Tooltip("Tiempo en segundos que dura el corte de emisión y el latigazo")]
    public float tiempoCorteEmision = 0.15f;
    [Tooltip("Cantidad de partículas extra que explotan de golpe justo al recibir el impacto")]
    public int cantidadBurstImpacto = 100;

    [Tooltip("Explosión/Chispa de 1 solo uso SOLO al terminar de recargarse.")]
    public ParticleSystem psBurst;
    // ====================================================================

    private bool enCooldown = false;
    private Material materialInstanciado;
    private Coroutine corrutinaOscilacion;

    [Header("Lanzamiento de la Piedra")]
    public GameObject prefabPiedra;
    public Transform puntoSalidaPiedra;

    [Header("--- DIRECCIÓN CONCRETA ---")]
    public Vector3 direccionDeTiro = new Vector3(0f, 1f, 0f);
    public bool direccionLocal = true;

    [Header("--- FUERZAS Y ALEATORIEDAD ---")]
    public float fuerzaLanzamiento = 16f;
    [Range(0f, 45f)] public float dispersionAleatoria = 10f;
    public float fuerzaRotacionRandom = 15f;
    public float tiempoInmunidadPiedra = 0.5f;
    public float velocidadCrecimientoPiedra = 12f;

    [Header("Ajustes del Bloop - CUERPO PRINCIPAL")]
    public float retrasoExpulsion = 0.15f;
    public bool bloopEjeAleatorio = true;
    public float duracionBloop = 0.4f;
    [Range(0.0f, 0.9f)] public float aplastamiento = 0.55f;
    [Range(1.0f, 1.8f)] public float expansion = 1.35f;

    [Header("Ajustes del Bloop - BOQUILLA SALIDA")]
    [Tooltip("La boquilla de salida siempre se estirará en el eje Y.")]
    [Range(1.1f, 2.5f)] public float estiramientoYSalida = 1.6f;
    [Tooltip("Cuánto se estrechan los lados al estirarse.")]
    [Range(0.2f, 0.9f)] public float contraccionXZSalida = 0.7f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sonidoBloop;

    private Vector3 escalaOriginalPrincipal;
    private Vector3 escalaOriginalSalida;
    private Quaternion rotacionOriginalBanderin;

    private Coroutine corrutinaBloopPrincipal;
    private Coroutine corrutinaBloopSalida;
    private Coroutine corrutinaBanderin;

    private void Awake()
    {
        if (modeloVisual != null) escalaOriginalPrincipal = modeloVisual.localScale;
        if (modeloSalidaVisual != null) escalaOriginalSalida = modeloSalidaVisual.localScale;
        if (modeloBanderin != null) rotacionOriginalBanderin = modeloBanderin.localRotation;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (rendererVisual != null)
        {
            materialInstanciado = rendererVisual.material;
            corrutinaOscilacion = StartCoroutine(RutinaOscilacion());
        }

        // Estado inicial (Listo para ser golpeado)
        if (psBurst != null) psBurst.Stop();
        if (psCargando != null)
        {
            // Como empieza "Listo", sus partículas nacen con velocidad hacia AFUERA (positivas)
            var main = psCargando.main;
            float valMin = Mathf.Abs(startSpeedFinMin);
            float valMax = Mathf.Abs(startSpeedFinMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(Mathf.Min(valMin, valMax), Mathf.Max(valMin, valMax));

            var emision = psCargando.emission;
            emision.rateOverTime = new ParticleSystem.MinMaxCurve(tasaEmisionCargandoMax);
            emision.enabled = true;

            psCargando.Play();
        }
    }

    public void RecibirImpacto(Vector3 puntoExactoImpacto, Vector3 normalSuperficie = default)
    {
        if (prefabVFXImpacto != null)
        {
            Vector3 direccionSalida = normalSuperficie != Vector3.zero ? normalSuperficie : Vector3.up;
            Vector3 posicionVFX = puntoExactoImpacto + (direccionSalida * offsetSuperficieVFX);
            Quaternion rotacionVFX = direccionSalida != Vector3.zero ? Quaternion.LookRotation(direccionSalida) : Quaternion.Euler(-90f, 0f, 0f);

            GameObject vfx = Instantiate(prefabVFXImpacto, posicionVFX, rotacionVFX);
            vfx.SetActive(true);

            ParticleSystem[] emisores = vfx.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in emisores) ps.Play();
            Destroy(vfx, 3f);
        }

        if (audioSource != null && sonidoBloop != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.25f);
            audioSource.PlayOneShot(sonidoBloop);
        }

        if (modeloBanderin != null)
        {
            if (corrutinaBanderin != null) StopCoroutine(corrutinaBanderin);
            corrutinaBanderin = StartCoroutine(RutinaBanderin());
        }

        if (!enCooldown)
        {
            enCooldown = true;
            if (corrutinaOscilacion != null) StopCoroutine(corrutinaOscilacion);

            if (psCargando != null)
            {
                StartCoroutine(RutinaInvertirParticulasConExplosion(psCargando, factorAceleracionExplosion, tiempoCorteEmision, cantidadBurstImpacto, true));
            }

            if (modeloVisual != null)
            {
                if (corrutinaBloopPrincipal != null) StopCoroutine(corrutinaBloopPrincipal);
                corrutinaBloopPrincipal = StartCoroutine(RutinaBloopPrincipal(modeloVisual, escalaOriginalPrincipal, bloopEjeAleatorio));
            }

            StartCoroutine(RutinaSecuenciaExpulsion());
        }
    }

    private IEnumerator RutinaSecuenciaExpulsion()
    {
        yield return new WaitForSeconds(retrasoExpulsion);

        if (modeloSalidaVisual != null)
        {
            if (corrutinaBloopSalida != null) StopCoroutine(corrutinaBloopSalida);
            corrutinaBloopSalida = StartCoroutine(RutinaBloopSalida(modeloSalidaVisual, escalaOriginalSalida));
        }

        LanzarPiedra();
        StartCoroutine(RutinaRecargaCooldown());
    }

    private IEnumerator RutinaBanderin()
    {
        Vector3 direccionEmpuje = Vector3.forward;

        if (Camera.main != null)
            direccionEmpuje = modeloBanderin.position - Camera.main.transform.position;

        direccionEmpuje.y = 0f;

        if (direccionEmpuje.sqrMagnitude < 0.001f)
            direccionEmpuje = Vector3.forward;

        direccionEmpuje.Normalize();
        Vector3 ejeRotacionMundo = Vector3.Cross(Vector3.up, direccionEmpuje);

        Transform espacioPadre = modeloBanderin.parent;
        Vector3 ejeRotacionLocal = espacioPadre != null ? espacioPadre.InverseTransformDirection(ejeRotacionMundo) : ejeRotacionMundo;

        float tiempo = 0f;
        float maxAngulo = inclinacionBanderin;

        while (true)
        {
            tiempo += Time.deltaTime;
            float decaimiento = Mathf.Exp(-amortiguacionBalanceo * tiempo);

            if (maxAngulo * decaimiento < 0.1f) break;

            float oscilacion = Mathf.Cos(velocidadBalanceo * tiempo);
            float anguloActual = maxAngulo * decaimiento * oscilacion;

            modeloBanderin.localRotation = Quaternion.AngleAxis(anguloActual, ejeRotacionLocal) * rotacionOriginalBanderin;
            yield return null;
        }

        modeloBanderin.localRotation = rotacionOriginalBanderin;
    }

    private IEnumerator RutinaRecargaCooldown()
    {
        if (psCargando != null && !psCargando.isPlaying) psCargando.Play();

        if (materialInstanciado != null && materialInstanciado.HasProperty(propiedadColor))
            materialInstanciado.SetColor(propiedadColor, colorDescargado * intensidadDescargado);

        float t = 0f;
        while (t < tiempoCooldown)
        {
            t += Time.deltaTime;
            float progreso = t / tiempoCooldown;

            Color colorActual = Color.Lerp(colorDescargado, colorListo, progreso);
            float intensidadActual = Mathf.Lerp(intensidadDescargado, intensidadListoMax, progreso);

            if (materialInstanciado != null && materialInstanciado.HasProperty(propiedadColor))
                materialInstanciado.SetColor(propiedadColor, colorActual * intensidadActual);

            if (psCargando != null)
            {
                var emision = psCargando.emission;
                var main = psCargando.main;

                float tasaActual = Mathf.Lerp(tasaEmisionCargandoMin, tasaEmisionCargandoMax, progreso);
                emision.rateOverTime = new ParticleSystem.MinMaxCurve(tasaActual);

                float velMin = Mathf.Lerp(startSpeedInicioMin, startSpeedFinMin, progreso);
                float velMax = Mathf.Lerp(startSpeedInicioMax, startSpeedFinMax, progreso);
                main.startSpeed = new ParticleSystem.MinMaxCurve(velMin, velMax);
            }

            yield return null;
        }

        enCooldown = false;

        // ==============================================================
        // INVERSIÓN ORIGINAL QUE FUNCIONA PERFECTA AL RECARGARSE (SIN TOCAR)
        // ==============================================================
        if (psCargando != null)
        {
            InvertirVelocidadParticulasVivas(psCargando);

            var main = psCargando.main;
            float valMin = Mathf.Abs(startSpeedFinMin);
            float valMax = Mathf.Abs(startSpeedFinMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(Mathf.Min(valMin, valMax), Mathf.Max(valMin, valMax));

            var emision = psCargando.emission;
            emision.rateOverTime = new ParticleSystem.MinMaxCurve(tasaEmisionCargandoMax);
        }

        if (psBurst != null) psBurst.Play();

        corrutinaOscilacion = StartCoroutine(RutinaOscilacion());
    }

    // --- FUNCIÓN QUE FUNCIONABA PERFECTA AL RECARGARSE (SIN TOCAR) ---
    private void InvertirVelocidadParticulasVivas(ParticleSystem ps)
    {
        if (ps == null) return;

        ParticleSystem.Particle[] particulas = new ParticleSystem.Particle[ps.main.maxParticles];
        int vivas = ps.GetParticles(particulas);

        for (int i = 0; i < vivas; i++)
        {
            particulas[i].velocity = -particulas[i].velocity * 2;
        }

        ps.SetParticles(particulas, vivas);
    }

    // --- CORRUTINA ACTUALIZADA: FORZANDO BURST ALTA VELOCIDAD Y ESPERANDO 1 FOTOGRAMA ---
    private IEnumerator RutinaInvertirParticulasConExplosion(ParticleSystem ps, float aceleracion, float tiempoCorte, int cantidadBurst, bool haciaAdentro)
    {
        if (ps == null) yield break;

        var main = ps.main;
        var emision = ps.emission;

        // 1. Cortar emisión regular
        emision.enabled = false;

        // 2. Nos aseguramos de que el Emisor esté en modo expansión (hacia afuera)
        float valMin = Mathf.Abs(startSpeedFinMin);
        float valMax = Mathf.Abs(startSpeedFinMax);
        main.startSpeed = new ParticleSystem.MinMaxCurve(Mathf.Min(valMin, valMax), Mathf.Max(valMin, valMax));

        // 3. Emitir el Burst (Saldrá hacia afuera a velocidad base)
        if (cantidadBurst > 0)
        {
            ps.Emit(cantidadBurst);
        }

        // =========================================================================
        // EL TRUCO VITAL: Esperamos 1 fotograma para que Unity registre el Burst 
        // internamente en su memoria de simulación. 
        yield return null;
        // =========================================================================

        // 4. Capturar TODAS las partículas (las viejas flotantes + las nuevas del burst)
        ParticleSystem.Particle[] particulas = new ParticleSystem.Particle[main.maxParticles];
        int vivas = ps.GetParticles(particulas);

        // 5. Acelerar de golpe TODAS juntas. El latigazo será masivo y brutal en pantalla.
        for (int i = 0; i < vivas; i++)
        {
            particulas[i].velocity *= aceleracion;
        }
        ps.SetParticles(particulas, vivas);

        // 6. Mantenemos la onda expansiva el tiempo que pediste (restando el frame que ya esperamos)
        yield return new WaitForSeconds(Mathf.Max(0f, tiempoCorte - Time.deltaTime));

        // 7. Frenar e invertir ABSOLUTAMENTE TODAS las partículas para que reboten hacia adentro
        vivas = ps.GetParticles(particulas);
        for (int i = 0; i < vivas; i++)
        {
            particulas[i].velocity *= (-1f / aceleracion);
        }
        ps.SetParticles(particulas, vivas);

        // 8. Restaurar la velocidad base normal para las partículas que nazcan durante el cooldown
        if (haciaAdentro)
        {
            main.startSpeed = new ParticleSystem.MinMaxCurve(startSpeedInicioMin, startSpeedInicioMax);
        }
        else
        {
            main.startSpeed = new ParticleSystem.MinMaxCurve(Mathf.Min(valMin, valMax), Mathf.Max(valMin, valMax));
        }

        emision.enabled = true;
    }

    private IEnumerator RutinaOscilacion()
    {
        while (!enCooldown)
        {
            if (materialInstanciado != null && materialInstanciado.HasProperty(propiedadColor))
            {
                float onda = (Mathf.Sin(Time.time * velocidadOscilacion) + 1f) / 2f;
                float intensidadActual = Mathf.Lerp(intensidadListoMin, intensidadListoMax, onda);
                materialInstanciado.SetColor(propiedadColor, colorListo * intensidadActual);
            }
            yield return null;
        }
    }

    private IEnumerator RutinaBloopPrincipal(Transform objetivo, Vector3 escalaBase, bool ejeAleatorio)
    {
        float t = 0f;
        float chafar = aplastamiento;
        float expandir = expansion;
        float contraer = 1f / expansion;
        float alargar = 2f - aplastamiento;

        Vector3 escalaAplastada = new Vector3(escalaBase.x * expandir, escalaBase.y * chafar, escalaBase.z * expandir);
        Vector3 escalaEstirada = new Vector3(escalaBase.x * contraer, escalaBase.y * alargar, escalaBase.z * contraer);

        if (ejeAleatorio)
        {
            int eje = Random.Range(0, 3);
            if (eje == 0)
            {
                escalaAplastada = new Vector3(escalaBase.x * chafar, escalaBase.y * expandir, escalaBase.z * expandir);
                escalaEstirada = new Vector3(escalaBase.x * alargar, escalaBase.y * contraer, escalaBase.z * contraer);
            }
            else if (eje == 2)
            {
                escalaAplastada = new Vector3(escalaBase.x * expandir, escalaBase.y * expandir, escalaBase.z * chafar);
                escalaEstirada = new Vector3(escalaBase.x * contraer, escalaBase.y * contraer, escalaBase.z * alargar);
            }
        }

        float fase1 = duracionBloop * 0.25f;
        while (t < fase1)
        {
            t += Time.deltaTime;
            if (objetivo != null) objetivo.localScale = Vector3.Lerp(escalaBase, escalaAplastada, t / fase1);
            yield return null;
        }

        t = 0f;
        float fase2 = duracionBloop * 0.35f;
        while (t < fase2)
        {
            t += Time.deltaTime;
            if (objetivo != null) objetivo.localScale = Vector3.Lerp(escalaAplastada, escalaEstirada, t / fase2);
            yield return null;
        }

        t = 0f;
        float fase3 = duracionBloop * 0.4f;
        while (t < fase3)
        {
            t += Time.deltaTime;
            if (objetivo != null) objetivo.localScale = Vector3.Lerp(escalaEstirada, escalaBase, Mathf.SmoothStep(0f, 1f, t / fase3));
            yield return null;
        }

        if (objetivo != null) objetivo.localScale = escalaBase;
    }

    private IEnumerator RutinaBloopSalida(Transform objetivo, Vector3 escalaBase)
    {
        float t = 0f;

        Vector3 escalaEstirada = new Vector3(escalaBase.x * contraccionXZSalida, escalaBase.y * estiramientoYSalida, escalaBase.z * contraccionXZSalida);
        float alargarXZ = 2f - contraccionXZSalida;
        float contraerY = 1f / estiramientoYSalida;
        Vector3 escalaAplastada = new Vector3(escalaBase.x * alargarXZ, escalaBase.y * contraerY, escalaBase.z * alargarXZ);

        float fase1 = duracionBloop * 0.25f;
        while (t < fase1)
        {
            t += Time.deltaTime;
            if (objetivo != null) objetivo.localScale = Vector3.Lerp(escalaBase, escalaEstirada, t / fase1);
            yield return null;
        }

        t = 0f;
        float fase2 = duracionBloop * 0.35f;
        while (t < fase2)
        {
            t += Time.deltaTime;
            if (objetivo != null) objetivo.localScale = Vector3.Lerp(escalaEstirada, escalaAplastada, t / fase2);
            yield return null;
        }

        t = 0f;
        float fase3 = duracionBloop * 0.4f;
        while (t < fase3)
        {
            t += Time.deltaTime;
            if (objetivo != null) objetivo.localScale = Vector3.Lerp(escalaAplastada, escalaBase, Mathf.SmoothStep(0f, 1f, t / fase3));
            yield return null;
        }

        if (objetivo != null) objetivo.localScale = escalaBase;
    }

    private void LanzarPiedra()
    {
        if (prefabPiedra == null) return;

        Vector3 origenSpawn = ObtenerPuntoOrigen();
        Vector3 direccionBase = ObtenerDireccionLanzamiento();

        Quaternion rotacionAleatoria = Quaternion.AngleAxis(Random.Range(0f, dispersionAleatoria), Random.onUnitSphere);
        Vector3 direccionFinal = rotacionAleatoria * direccionBase;

        GameObject piedra = Instantiate(prefabPiedra, origenSpawn, Random.rotation);

        CrecimientoInicialPiedra crecimiento = piedra.AddComponent<CrecimientoInicialPiedra>();
        crecimiento.IniciarCrecimiento(prefabPiedra.transform.localScale, velocidadCrecimientoPiedra);

        if (tiempoInmunidadPiedra > 0f)
        {
            InmunidadTemporalPiedra inmunidad = piedra.AddComponent<InmunidadTemporalPiedra>();
            inmunidad.IniciarInmunidad(tiempoInmunidadPiedra);
        }

        Rigidbody rb = piedra.GetComponent<Rigidbody>();
        if (rb == null) rb = piedra.AddComponent<Rigidbody>();

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(direccionFinal * fuerzaLanzamiento, ForceMode.Impulse);

        Vector3 torque = new Vector3(
            Random.Range(-fuerzaRotacionRandom, fuerzaRotacionRandom),
            Random.Range(-fuerzaRotacionRandom, fuerzaRotacionRandom),
            Random.Range(-fuerzaRotacionRandom, fuerzaRotacionRandom)
        );
        rb.AddTorque(torque, ForceMode.Impulse);
    }

    public Vector3 ObtenerPuntoOrigen()
    {
        return puntoSalidaPiedra != null ? puntoSalidaPiedra.position : transform.position + (Vector3.up * 1f);
    }

    public Vector3 ObtenerDireccionLanzamiento()
    {
        Vector3 dirNormalizada = direccionDeTiro.normalized;
        if (dirNormalizada == Vector3.zero) dirNormalizada = Vector3.up;

        if (direccionLocal)
        {
            Transform referencia = puntoSalidaPiedra != null ? puntoSalidaPiedra : transform;
            return referencia.TransformDirection(dirNormalizada);
        }

        return dirNormalizada;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origen = ObtenerPuntoOrigen();
        Vector3 direccionBase = ObtenerDireccionLanzamiento();

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origen, direccionBase * 2.5f);

        if (dispersionAleatoria > 0.1f)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            Vector3 ortho1 = Vector3.Cross(direccionBase, Vector3.up);
            if (ortho1 == Vector3.zero) ortho1 = Vector3.right;
            Vector3 ortho2 = Vector3.Cross(direccionBase, ortho1);

            Vector3 ray1 = Quaternion.AngleAxis(dispersionAleatoria, ortho1) * direccionBase * 2.5f;
            Vector3 ray2 = Quaternion.AngleAxis(-dispersionAleatoria, ortho1) * direccionBase * 2.5f;
            Vector3 ray3 = Quaternion.AngleAxis(dispersionAleatoria, ortho2) * direccionBase * 2.5f;
            Vector3 ray4 = Quaternion.AngleAxis(-dispersionAleatoria, ortho2) * direccionBase * 2.5f;

            Gizmos.DrawRay(origen, ray1);
            Gizmos.DrawRay(origen, ray2);
            Gizmos.DrawRay(origen, ray3);
            Gizmos.DrawRay(origen, ray4);

            DibujarCirculoGizmo(origen + (direccionBase * 2.5f), direccionBase, Mathf.Tan(dispersionAleatoria * Mathf.Deg2Rad) * 2.5f, 16);
        }

        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.8f);
        DibujarParabola(origen, direccionBase * fuerzaLanzamiento);
    }

    private void DibujarParabola(Vector3 origen, Vector3 velocidadInicial)
    {
        Vector3 anterior = origen;
        Vector3 gravedad = Physics.gravity;
        int pasos = 25;
        float dt = 1.5f / pasos;

        for (int i = 1; i <= pasos; i++)
        {
            float t = i * dt;
            Vector3 actual = origen + (velocidadInicial * t) + (0.5f * gravedad * t * t);
            Gizmos.DrawLine(anterior, actual);
            anterior = actual;
        }
    }

    private void DibujarCirculoGizmo(Vector3 centro, Vector3 normal, float radio, int segmentos)
    {
        Vector3 ortho = Vector3.Cross(normal, Vector3.up).normalized;
        if (ortho == Vector3.zero) ortho = Vector3.right;

        float paso = 360f / segmentos;
        Vector3 previo = centro + (ortho * radio);

        for (int i = 1; i <= segmentos; i++)
        {
            Vector3 siguiente = centro + (Quaternion.AngleAxis(i * paso, normal) * ortho * radio);
            Gizmos.DrawLine(previo, siguiente);
            previo = siguiente;
        }
    }
}

// ====================================================================
// COMPONENTES AUXILIARES
// ====================================================================

public class InmunidadTemporalPiedra : MonoBehaviour
{
    private string tagOriginal;
    private int layerOriginal;

    public void IniciarInmunidad(float duracion)
    {
        tagOriginal = gameObject.tag;
        layerOriginal = gameObject.layer;

        gameObject.tag = "Untagged";
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        StartCoroutine(RutinaRestaurar(duracion));
    }

    private IEnumerator RutinaRestaurar(float duracion)
    {
        yield return new WaitForSeconds(duracion);

        if (this != null && gameObject != null)
        {
            gameObject.tag = tagOriginal;
            gameObject.layer = layerOriginal;
            Destroy(this);
        }
    }
}

public class CrecimientoInicialPiedra : MonoBehaviour
{
    private Vector3 escalaObjetivo;
    private float velocidad;

    public void IniciarCrecimiento(Vector3 escalaFinal, float vel)
    {
        escalaObjetivo = escalaFinal;
        velocidad = vel;
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, escalaObjetivo, Time.deltaTime * velocidad);

        if (Vector3.Distance(transform.localScale, escalaObjetivo) < 0.01f)
        {
            transform.localScale = escalaObjetivo;
            Destroy(this);
        }
    }
}