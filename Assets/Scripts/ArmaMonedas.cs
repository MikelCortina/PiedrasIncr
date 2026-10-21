using UnityEngine;
using System.Collections.Generic;

public class ArmaLanzadora : MonoBehaviour
{
    [Header("Controles")]
    public KeyCode teclaEquipar = KeyCode.Alpha3;
    public KeyCode teclaDisparo = KeyCode.Mouse0;
    public KeyCode teclaAspirar = KeyCode.Mouse1;

    [Header("Ajustes de Aspiradora")]
    [Tooltip("Distancia máxima desde la cual el arma puede atrapar monedas")]
    public float distanciaMaximaAspiracion = 15f;
    [Tooltip("Ángulo de apertura del cono de succión (Menor = más cerrado y preciso)")]
    [Range(10f, 90f)] public float anguloAspiracion = 30f;
    public float velocidadAspiracion = 25f;
    [Tooltip("Distancia al cañón a la que la moneda se considera absorbida y desaparece")]
    public float distanciaRecoleccion = 0.5f;

    [Tooltip("Giro mínimo que dan las monedas mientras son aspiradas")]
    public float velocidadRotacionMin = 400f;
    [Tooltip("Giro máximo que dan las monedas mientras son aspiradas")]
    public float velocidadRotacionMax = 1000f;

    public LayerMask capaMonedas;

    // Estructura para recordar el estado de la moneda al ser capturada
    private class DatosAspiracion
    {
        public GameObject objeto;
        public Vector3 escalaInicial;
        public float distanciaInicial;
        public float velocidadRotacion; // Cada moneda tendrá su propia velocidad de giro
    }

    // Lista de monedas que están siendo absorbidas
    private List<DatosAspiracion> monedasAspiradas = new List<DatosAspiracion>();

    [Header("Referencias")]
    public GameObject modeloArma;
    public Transform puntoDisparo;
    public GameObject prefabProyectil;

    [Header("Efectos Visuales (Giro con Inercia)")]
    public Transform objetoGiratorio;
    public float velocidadGiroZ = 500f;
    [Tooltip("Qué tan rápido alcanza la velocidad máxima al disparar")]
    public float aceleracionGiro = 5f;
    [Tooltip("Qué tan rápido frena al soltar el gatillo (valores bajos = más inercia)")]
    public float desaceleracionGiro = 2f;

    // ========================================================
    // SISTEMA DE NEONES RÁPIDOS
    // ========================================================
    [Header("Efectos Visuales (Neones)")]
    [Tooltip("Arrastra aquí las piezas del arma que tengan el script ControladorNeon")]
    public ControladorNeon[] neonesArma;

    [ColorUsage(true, true)]
    public Color colorNeonInicio = Color.cyan;

    [ColorUsage(true, true)]
    public Color colorNeonFinal = new Color(1f, 0.3f, 0f, 1f);

    [Range(0f, 10f)]
    public float intensidadNeonInicio = 1f;

    [Range(0f, 10f)]
    public float intensidadNeonFinal = 3f;

    [Tooltip("Velocidad de parpadeo extra que se sumará a los neones al disparar")]
    public float aumentoParpadeoActivo = 15f;

    [Tooltip("Velocidad a la que el neón hace la transición de encendido a apagado (20 es rápido)")]
    public float velocidadTransicionNeon = 20f;

    private float transicionNeonActual = 0f;
    private float[] intensidadesBaseNeones;
    private float[] parpadeosBaseNeones;
    // ========================================================

    [Header("Economía")]
    public Cartera cartera;

    [Min(1)]
    public int costePorDisparo = 1;

    [Header("Animación del Modelo (Sway & Bobbing)")]
    public float intensidadSway = 0.02f;
    public float limiteSway = 0.06f;
    public float intensidadInclinacion = 2f;
    public float velocidadBobbing = 10f;
    public float intensidadBobbing = 0.015f;
    public float suavidadAnimacion = 8f;

    [Header("Animación (Blend Shape)")]
    public SkinnedMeshRenderer mallaArma;
    public int indiceBlendShape = 1;
    public float pesoMaximoBlendShape = 100f;
    public float velocidadRecuperacionBlend = 15f;

    [Header("Ajustes de Disparo")]
    public float fuerzaDisparo = 25f;
    public float cadenciaDisparo = 0.2f;
    public float vidaProyectil = 10f;

    [Header("Dispersión")]
    public float anguloDispersion = 5f;

    [Header("Crecimiento del Proyectil")]
    public float escalaMaxima = 2.5f;
    public float velocidadCrecimiento = 8f;

    [Header("Inmunidad de Recogida")]
    public float tiempoInmunidad = 1.5f;

    [Header("Game Feel (Efectos)")]
    public ParticleSystem particulasDisparo;

    [Tooltip("Sistema de partículas exacto del sistema de Torbellino para la aspiradora")]
    public ParticleSystem particulasAspiradora;

    public AudioSource audioSource;
    public AudioClip sonidoEquipar;
    public AudioClip sonidoDisparo;

    public float fuerzaRetrocesoVisual = 0.3f;
    public float velocidadRecuperacionRetroceso = 15f;

    private bool armaEquipada = false;
    private float tiempoProximoDisparo = 0f;

    private Vector3 escalaOriginalArma;
    private float retrocesoActual = 0f;
    private float pesoActualBlend = 0f;

    // --- VARIABLES DE ANIMACIÓN ---
    private Vector3 posicionInicialModelo;
    private Quaternion rotacionInicialModelo;
    private float temporizadorBobbing = 0f;

    // --- VARIABLE DE VELOCIDAD ACTUAL ---
    private float velocidadGiroActual = 0f;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (cartera == null)
            cartera = GetComponentInParent<Cartera>();

        // --- INICIALIZAMOS LOS NEONES GUARDANDO SUS VALORES BASE ---
        if (neonesArma != null && neonesArma.Length > 0)
        {
            intensidadesBaseNeones = new float[neonesArma.Length];
            parpadeosBaseNeones = new float[neonesArma.Length];

            for (int i = 0; i < neonesArma.Length; i++)
            {
                if (neonesArma[i] != null)
                {
                    parpadeosBaseNeones[i] = neonesArma[i].velocidadParpadeo;
                }
            }
        }

        if (particulasAspiradora != null)
        {
            if (!particulasAspiradora.isPlaying) particulasAspiradora.Play(true);
            particulasAspiradora.Clear(true);
        }
        ControlarEmisionParticulasAspiradora(false);

        if (modeloArma != null)
        {
            escalaOriginalArma = modeloArma.transform.localScale;
            posicionInicialModelo = modeloArma.transform.localPosition;
            rotacionInicialModelo = modeloArma.transform.localRotation;

            modeloArma.SetActive(false);
        }
    }

    void Update()
    {
        if (armaEquipada)
        {
            ManejarDisparo();
            ManejarAspiradora();
            ManejarGiroObjeto();
            ManejarNeones();
            ManejarRetrocesoVisual();
            ManejarBlendShape();
            ActualizarAnimacionModelo();
        }
    }

    public void SetEquipada(bool equipada)
    {
        if (armaEquipada == equipada)
            return;

        armaEquipada = equipada;

        if (modeloArma != null)
        {
            modeloArma.SetActive(armaEquipada);

            if (armaEquipada)
            {
                modeloArma.transform.localPosition = posicionInicialModelo;
                modeloArma.transform.localRotation = rotacionInicialModelo;

                temporizadorBobbing = 0f;
                velocidadGiroActual = 0f;
                transicionNeonActual = 0f;
                ManejarNeones();
            }
        }

        if (!armaEquipada)
        {
            ControlarEmisionParticulasAspiradora(false);
            monedasAspiradas.Clear();
        }

        if (armaEquipada && sonidoEquipar != null && audioSource != null)
        {
            audioSource.PlayOneShot(sonidoEquipar, 0.8f);
        }
    }

    void ManejarAspiradora()
    {
        bool aspirando = Input.GetKey(teclaAspirar);

        if (aspirando)
        {
            // Partículas de aspirado activo
            if (particulasAspiradora != null && !particulasAspiradora.isPlaying)
            {
                particulasAspiradora.Play(true);
            }

            // 1. Detección por Cono Frontal (para larga y media distancia)
            Collider[] objetosCercanos = Physics.OverlapSphere(puntoDisparo.position, distanciaMaximaAspiracion, capaMonedas);

            foreach (Collider col in objetosCercanos)
            {
                GameObject objetoReal = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;

                // Verificamos que sea una moneda válida y que no esté ya en la lista
                bool yaEstaAspirada = false;
                foreach (var m in monedasAspiradas)
                {
                    if (m.objeto == objetoReal) { yaEstaAspirada = true; break; }
                }

                if (objetoReal.CompareTag("Coin") && !yaEstaAspirada)
                {
                    EdadMoneda edad = objetoReal.GetComponent<EdadMoneda>();
                    if (edad != null && !edad.HaSuperadoInmunidad(tiempoInmunidad))
                        continue;

                    Vector3 direccionHaciaMoneda = (objetoReal.transform.position - puntoDisparo.position).normalized;
                    float distanciaAlCanon = Vector3.Distance(puntoDisparo.position, objetoReal.transform.position);
                    float angulo = Vector3.Angle(puntoDisparo.forward, direccionHaciaMoneda);

                    // CONDICIÓN DE ATRACCIÓN: 
                    // O está dentro del cono frontal, o está muy cerca de ti (a menos de 3 metros, ej: bajo tus pies)
                    bool estaEnCono = (angulo <= anguloAspiracion);
                    bool estaMuyCerca = (distanciaAlCanon <= 3.0f);

                    if (estaEnCono || estaMuyCerca)
                    {
                        // Destruimos el script de crecimiento si acaba de ser disparada
                        CrecimientoProyectil crec = objetoReal.GetComponent<CrecimientoProyectil>();
                        if (crec != null) Destroy(crec);

                        // Desactivar físicas de la moneda
                        Rigidbody rb = objetoReal.GetComponent<Rigidbody>();
                        if (rb != null) rb.isKinematic = true;

                        Collider[] colliders = objetoReal.GetComponentsInChildren<Collider>();
                        foreach (Collider c in colliders) c.enabled = false;

                        float giroAleatorio = Random.Range(velocidadRotacionMin, velocidadRotacionMax);

                        monedasAspiradas.Add(new DatosAspiracion
                        {
                            objeto = objetoReal,
                            escalaInicial = objetoReal.transform.localScale,
                            distanciaInicial = Mathf.Max(distanciaAlCanon, 0.5f), // Evitamos división por cero
                            velocidadRotacion = giroAleatorio
                        });
                    }
                }
            }
        }
        else
        {
            // APAGADO INSTANTÁNEO AL SOLTAR EL CLIC
            ControlarEmisionParticulasAspiradora(false);
        }

        // 2. Mover las monedas aspiradas hacia el cañón del arma
        for (int i = monedasAspiradas.Count - 1; i >= 0; i--)
        {
            DatosAspiracion datos = monedasAspiradas[i];
            GameObject obj = datos.objeto;

            if (obj == null || !obj.activeInHierarchy)
            {
                monedasAspiradas.RemoveAt(i);
                continue;
            }

            if (!aspirando)
            {
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = false;

                Collider[] colliders = obj.GetComponentsInChildren<Collider>();
                foreach (Collider c in colliders) c.enabled = true;

                CrecimientoProyectil crec = obj.AddComponent<CrecimientoProyectil>();
                crec.ConfigurarDesdeActualVector(datos.escalaInicial, 15f);

                monedasAspiradas.RemoveAt(i);
                continue;
            }

            // Mover hacia el cañón suavemente
            obj.transform.position = Vector3.MoveTowards(
                obj.transform.position, puntoDisparo.position,
                velocidadAspiracion * Time.deltaTime);

            // Girar locamente mientras vuelan
            Vector3 direccionVuelo = (puntoDisparo.position - obj.transform.position).normalized;
            Vector3 ejeGiro = Vector3.Cross(Vector3.up, direccionVuelo) + new Vector3(0.1f, 0.3f, 0.1f);
            obj.transform.Rotate(ejeGiro.normalized * datos.velocidadRotacion * Time.deltaTime, Space.World);

            // ========================================================
            // REDUCCIÓN LINEAL SEGURA
            // ========================================================
            float distActual = Vector3.Distance(puntoDisparo.position, obj.transform.position);

            float t = (datos.distanciaInicial > 0.01f) ? Mathf.Clamp01(distActual / datos.distanciaInicial) : 0f;

            // Aseguramos que nunca empiece en tamaño 0 absoluto si se aspira muy cerca
            obj.transform.localScale = Vector3.Lerp(Vector3.zero, datos.escalaInicial, t);

            if (distActual <= distanciaRecoleccion + 0.05f || obj.transform.position == puntoDisparo.position)
            {
                monedasAspiradas.RemoveAt(i);
                RecogerMonedaAspirada(obj);
            }
        }
    }

    // Método modificado para corte seco y limpio al instante
    void ControlarEmisionParticulasAspiradora(bool activar)
    {
        if (particulasAspiradora != null)
        {
            if (activar)
            {
                if (!particulasAspiradora.isPlaying) particulasAspiradora.Play(true);
            }
            else
            {
                // Detiene la emisión y limpia las partículas activas de golpe
                particulasAspiradora.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private void RecogerMonedaAspirada(GameObject objeto)
    {
        Moneda moneda = objeto.GetComponent<Moneda>();
        if (moneda == null) moneda = objeto.GetComponentInChildren<Moneda>();

        if (moneda != null && cartera != null)
        {
            cartera.AnadirMonedas(moneda.valor);
        }

        if (sonidoEquipar != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(1.2f, 1.5f);
            audioSource.PlayOneShot(sonidoEquipar, 0.4f);
        }

        if (particulasDisparo != null) particulasDisparo.Play();
        retrocesoActual = fuerzaRetrocesoVisual * 0.3f;

        objeto.SetActive(false);
        Destroy(objeto);
    }

    void ManejarDisparo()
    {
        if (Input.GetKey(teclaDisparo) && Time.time >= tiempoProximoDisparo)
        {
            tiempoProximoDisparo = Time.time + cadenciaDisparo;
            Disparar();
        }
    }

    void ManejarGiroObjeto()
    {
        if (objetoGiratorio == null) return;

        if (Input.GetKey(teclaDisparo))
        {
            velocidadGiroActual = Mathf.Lerp(velocidadGiroActual, velocidadGiroZ, Time.deltaTime * aceleracionGiro);
        }
        else if (Input.GetKey(teclaAspirar))
        {
            velocidadGiroActual = Mathf.Lerp(velocidadGiroActual, -velocidadGiroZ * 0.7f, Time.deltaTime * aceleracionGiro);
        }
        else
        {
            velocidadGiroActual = Mathf.Lerp(velocidadGiroActual, 0f, Time.deltaTime * desaceleracionGiro);
        }

        if (Mathf.Abs(velocidadGiroActual) > 0.1f)
        {
            objetoGiratorio.Rotate(0f, 0f, velocidadGiroActual * Time.deltaTime, Space.Self);
        }
    }

    void ManejarNeones()
    {
        if (neonesArma == null || neonesArma.Length == 0) return;

        bool disparando = Input.GetKey(teclaDisparo) || Input.GetKey(teclaAspirar);
        float transicionObjetivo = disparando ? 1f : 0f;

        transicionNeonActual = Mathf.Lerp(transicionNeonActual, transicionObjetivo, Time.deltaTime * velocidadTransicionNeon);

        Color colorActualizado = Color.Lerp(colorNeonInicio, colorNeonFinal, transicionNeonActual);
        float intensidadActualizada = Mathf.Lerp(intensidadNeonInicio, intensidadNeonFinal, transicionNeonActual);

        for (int i = 0; i < neonesArma.Length; i++)
        {
            if (neonesArma[i] != null)
            {
                neonesArma[i].colorNeon = colorActualizado;
                neonesArma[i].intensidad = intensidadActualizada;
                neonesArma[i].velocidadParpadeo = parpadeosBaseNeones[i] + (aumentoParpadeoActivo * transicionNeonActual);
            }
        }
    }

    void ActualizarAnimacionModelo()
    {
        if (modeloArma == null) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        float movX = Input.GetAxis("Horizontal");
        float movY = Input.GetAxis("Vertical");

        float moveX = Mathf.Clamp(mouseX * intensidadSway, -limiteSway, limiteSway);
        float moveY = Mathf.Clamp(mouseY * intensidadSway, -limiteSway, limiteSway);
        Vector3 posicionObjetivo = posicionInicialModelo + new Vector3(-moveX, -moveY, 0f);

        if (Mathf.Abs(movX) > 0.1f || Mathf.Abs(movY) > 0.1f)
        {
            temporizadorBobbing += Time.deltaTime * velocidadBobbing;
            posicionObjetivo.y += Mathf.Sin(temporizadorBobbing) * intensidadBobbing;
            posicionObjetivo.x += Mathf.Cos(temporizadorBobbing * 0.5f) * (intensidadBobbing * 1.5f);
        }
        else
        {
            temporizadorBobbing = 0f;
        }

        float tiltZ = Mathf.Clamp((movX + mouseX) * intensidadInclinacion, -intensidadInclinacion * 2f, intensidadInclinacion * 2f);
        float tiltX = Mathf.Clamp(-mouseY * intensidadInclinacion, -intensidadInclinacion, intensidadInclinacion);
        Quaternion rotacionObjetivo = rotacionInicialModelo * Quaternion.Euler(tiltX, 0f, -tiltZ);

        modeloArma.transform.localPosition = Vector3.Lerp(modeloArma.transform.localPosition, posicionObjetivo, Time.deltaTime * suavidadAnimacion);
        modeloArma.transform.localRotation = Quaternion.Slerp(modeloArma.transform.localRotation, rotacionObjetivo, Time.deltaTime * suavidadAnimacion);
    }

    void Disparar()
    {
        if (prefabProyectil == null || puntoDisparo == null) return;

        if (cartera == null)
        {
            Debug.LogWarning("ArmaLanzadora: No se ha encontrado una Cartera.");
            return;
        }

        if (!cartera.GastarMonedas(costePorDisparo))
        {
            Debug.Log("No hay monedas suficientes para disparar.");
            return;
        }

        Vector2 dispersionAleatoria = Random.insideUnitCircle * anguloDispersion;

        Quaternion rotacionDispersion = Quaternion.Euler(
            dispersionAleatoria.y,
            dispersionAleatoria.x,
            0f
        );

        Vector3 direccionFinal = (puntoDisparo.rotation * rotacionDispersion) * Vector3.forward;

        Quaternion rotacionFrisbee = Quaternion.LookRotation(direccionFinal, puntoDisparo.up);

        GameObject proyectil = Instantiate(prefabProyectil, puntoDisparo.position, rotacionFrisbee);

        Moneda moneda = proyectil.GetComponent<Moneda>();
        if (moneda == null)
        {
            moneda = proyectil.AddComponent<Moneda>();
        }

        moneda.valor = costePorDisparo;
        if (proyectil.GetComponent<ProyectilArma>() == null)
        {
            proyectil.AddComponent<ProyectilArma>();
        }

        Rigidbody rb = proyectil.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(direccionFinal * fuerzaDisparo, ForceMode.Impulse);
            float fuerzaGiro = Random.Range(-15f, 15f);
            rb.AddRelativeTorque(new Vector3(0f, fuerzaGiro, 0f), ForceMode.Impulse);
        }

        CrecimientoProyectil animCrecimiento = proyectil.AddComponent<CrecimientoProyectil>();
        animCrecimiento.Configurar(escalaMaxima, velocidadCrecimiento);

        ProteccionMagnetica animInmunidad = proyectil.AddComponent<ProteccionMagnetica>();
        animInmunidad.Configurar(tiempoInmunidad);

        Destroy(proyectil, vidaProyectil);

        if (particulasDisparo != null)
        {
            particulasDisparo.Play();
        }

        if (audioSource != null && sonidoDisparo != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(sonidoDisparo, 0.7f);
        }

        retrocesoActual = fuerzaRetrocesoVisual;
        pesoActualBlend = pesoMaximoBlendShape;
    }

    void ManejarRetrocesoVisual()
    {
        if (modeloArma != null && fuerzaRetrocesoVisual > 0f)
        {
            retrocesoActual = Mathf.Lerp(retrocesoActual, 0f, Time.deltaTime * velocidadRecuperacionRetroceso);
            modeloArma.transform.localScale = new Vector3(
                escalaOriginalArma.x,
                escalaOriginalArma.y,
                escalaOriginalArma.z * (1f - retrocesoActual)
            );
        }
    }

    void ManejarBlendShape()
    {
        if (mallaArma != null)
        {
            pesoActualBlend = Mathf.Lerp(pesoActualBlend, 0f, Time.deltaTime * velocidadRecuperacionBlend);
            mallaArma.SetBlendShapeWeight(indiceBlendShape, pesoActualBlend);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (puntoDisparo == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(puntoDisparo.position, distanciaRecoleccion);

        Gizmos.color = Color.cyan;

        Vector3 forward = puntoDisparo.forward * distanciaMaximaAspiracion;
        Gizmos.DrawRay(puntoDisparo.position, forward);

        Vector3 arriba = Quaternion.AngleAxis(-anguloAspiracion, puntoDisparo.right) * forward;
        Vector3 abajo = Quaternion.AngleAxis(anguloAspiracion, puntoDisparo.right) * forward;
        Vector3 izquierda = Quaternion.AngleAxis(-anguloAspiracion, puntoDisparo.up) * forward;
        Vector3 derecha = Quaternion.AngleAxis(anguloAspiracion, puntoDisparo.up) * forward;

        Gizmos.DrawRay(puntoDisparo.position, arriba);
        Gizmos.DrawRay(puntoDisparo.position, abajo);
        Gizmos.DrawRay(puntoDisparo.position, izquierda);
        Gizmos.DrawRay(puntoDisparo.position, derecha);

        Gizmos.DrawLine(puntoDisparo.position + arriba, puntoDisparo.position + derecha);
        Gizmos.DrawLine(puntoDisparo.position + derecha, puntoDisparo.position + abajo);
        Gizmos.DrawLine(puntoDisparo.position + abajo, puntoDisparo.position + izquierda);
        Gizmos.DrawLine(puntoDisparo.position + izquierda, puntoDisparo.position + arriba);

        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(puntoDisparo.position, distanciaMaximaAspiracion);
    }
}

// ====================================================================
// --- MINI-SCRIPTS AUXILIARES ---
// ====================================================================

public class CrecimientoProyectil : MonoBehaviour
{
    private Vector3 escalaObjetivo;
    private float velocidad;

    public void Configurar(float multiplicadorEscala, float vel)
    {
        escalaObjetivo = transform.localScale * multiplicadorEscala;
        velocidad = vel;
    }

    public void ConfigurarDesdeActualVector(Vector3 escalaF, float vel)
    {
        escalaObjetivo = escalaF;
        velocidad = vel;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, escalaObjetivo, Time.deltaTime * velocidad);

        if (Vector3.Distance(transform.localScale, escalaObjetivo) < 0.01f)
        {
            transform.localScale = escalaObjetivo;
            Destroy(this);
        }
    }
}

public class ProteccionMagnetica : MonoBehaviour
{
    private string tagOriginal;

    public void Configurar(float tiempo)
    {
        tagOriginal = gameObject.tag;
        gameObject.tag = "Untagged";
        Invoke("RestaurarTag", tiempo);
    }

    private void RestaurarTag()
    {
        gameObject.tag = tagOriginal;
        Destroy(this);
    }
}