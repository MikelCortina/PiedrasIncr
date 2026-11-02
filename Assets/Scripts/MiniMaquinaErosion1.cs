using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniMaquinaErosion : MonoBehaviour
{
    // =====================================================
    // ESTRUCTURAS DE ABSORCIÓN
    // =====================================================
    [System.Serializable]
    public class TuberiaAbsorcion
    {
        public Transform puntoAbsorcion;

        [Header("Orientación del Cono")]
        [Tooltip("Dirección hacia la que apunta el cono localmente (ej: 0,0,1 es Forward, 0,-1,0 es Abajo).")]
        public Vector3 direccionAbsorcionLocal = new Vector3(0f, 0f, 1f);

        [Tooltip("El cono se apoya con su cara de abajo plana (como un lado contra el suelo): su eje sube la mitad de la apertura " +
                 "sobre la dirección indicada, así la órbita de las piedras nunca baja de esa cara y no chocan con el suelo. " +
                 "La cara de abajo sigue la inclinación de 'Direccion Absorcion Local': inclínala para orientar el cono arriba o abajo.")]
        public bool apoyarEnSuelo = true;

        [Header("Zona de Acción (Cónica)")]
        [Tooltip("La distancia máxima a la que la máquina detecta piedras (Largo del cono).")]
        public float distanciaAbsorcion = 5f;

        [Range(0, 180)]
        [Tooltip("La apertura del ángulo del cono de detección.")]
        public float anguloCono = 45f;

        [Header("Fuerzas de Torbellino")]
        [Tooltip("Velocidad constante a la que la piedra es atraída hacia el punto central.")]
        public float velocidadSuccion = 3f;

        [Tooltip("Velocidad INICIAL a la que la piedra orbita en los bordes del cono.")]
        public float velocidadOrbita = 15f;

        [Tooltip("Multiplicador de velocidad al acercarse al centro (Ej: 4 = girará 4 veces más rápido justo antes de entrar).")]
        public float multiplicadorAceleracionCentro = 4f;

        [Range(0.1f, 1f)]
        [Tooltip("Amplitud de la órbita respecto al tamaño del cono. (0.5 = orbitan a la mitad de distancia de la pared para no rozar el suelo).")]
        public float amplitudOrbita = 0.5f;
    }

    private class DatosPiedra
    {
        public Rigidbody rb;
        public TuberiaAbsorcion tuberia;
        public Vector3 escalaOriginal;
        public float distanciaInicial;
        public EscalaPiedra escala;
    }


    // =====================================================
    // REQUISITOS DE ENTRADA Y ABSORCIÓN
    // =====================================================

    [Header("Configuración de Absorción")]
    public TuberiaAbsorcion[] tuberiasAbajo = new TuberiaAbsorcion[1];
    public LayerMask capaPiedras;
    [Tooltip("Distancia al centro a la que la piedra se considera 'tragada' por completo (Escala 0)")]
    public float distanciaConsumo = 0.5f;

    [Header("Requisitos de Entrada")]
    [Tooltip("Si es verdadero, rechazará las piedras que ya estén pulidas al 100%.")]
    public bool rechazarPiedrasCompletadas = true;

    [Tooltip("Si es verdadero, una piedra NO podrá volver a procesarse en ESTA misma máquina. Obliga a construir cadenas de montaje.")]
    public bool evitarProcesadoRepetido = true;

    [Tooltip("El porcentaje EXTRA de pulido que se le añadirá a CADA piedra que pase por la máquina.")]
    [Range(0f, 100f)]
    public float porcentajeExtraAProcesar = 10f;


    // =====================================================
    // RECHAZO
    // =====================================================

    [Header("Punto y Fuerza de Rechazo (Conos Rojos)")]
    public Transform puntoRechazo;
    public Vector3 direccionRechazo = new Vector3(0f, 1f, -1f);
    public float fuerzaMinimaRechazo = 10f;
    public float fuerzaMaximaRechazo = 20f;
    [Range(0f, 90f)]
    public float anguloDispersionRechazo = 15f;


    // =====================================================
    // SALIDA
    // =====================================================

    [Header("Punto y Fuerza de Salida (Conos Verdes)")]
    public Transform puntoSalida;
    public Transform puntoEntregaBot;
    public Vector3 direccionSalida = new Vector3(0f, 1f, 1f);
    public float fuerzaMinimaExpulsion = 10f;
    public float fuerzaMaximaExpulsion = 20f;
    [Range(0f, 90f)]
    public float anguloDispersionSalida = 15f;

    [Tooltip("Tiempo que tarda la piedra en volver a su tamaño original al ser expulsada")]
    public float tiempoCrecimiento = 0.2f;


    // =====================================================
    // PROCESAMIENTO
    // =====================================================

    [Header("Procesamiento Interno")]
    [Tooltip("Tiempo en segundos que tarda la piedra en cruzar la máquina.")]
    public float tiempoProcesamiento = 3f;


    // =====================================================
    // EFECTOS Y ANIMACIÓN
    // =====================================================

    [Header("Efectos Visuales (Partículas)")]
    public ParticleSystem[] particulasTrabajando;
    public ParticleSystem[] particulasExpulsion;

    [Header("Animación de Rotación (Eje Z)")]
    public List<Transform> objetosGiratorios = new List<Transform>();

    [Tooltip("Rango de velocidad (X = Mínimo, Y = Máximo). Se elegirá un valor aleatorio al empezar.")]
    public List<Vector2> rangosVelocidadGiroZ = new List<Vector2>();

    private List<float> velocidadesActualesZ = new List<float>();


    // =====================================================
    // VARIABLES INTERNAS
    // =====================================================

    private Dictionary<Rigidbody, float> piedrasEnProceso = new Dictionary<Rigidbody, float>();
    private Dictionary<Rigidbody, DatosPiedra> piedrasEnSuccion = new Dictionary<Rigidbody, DatosPiedra>();

    // Aquí recordamos qué piedras ya han pasado por esta máquina para no dejarlas entrar de nuevo
    private HashSet<Rigidbody> historialPiedras = new HashSet<Rigidbody>();

    private float[] emisionesOriginales;


    // =====================================================
    // START, UPDATE & FIXED UPDATE
    // =====================================================

    private void Start()
    {
        // Configurar velocidades aleatorias de giro
        velocidadesActualesZ.Clear();
        for (int i = 0; i < rangosVelocidadGiroZ.Count; i++)
        {
            float velocidadElegida = Random.Range(rangosVelocidadGiroZ[i].x, rangosVelocidadGiroZ[i].y);
            velocidadesActualesZ.Add(velocidadElegida);
        }

        // Configurar partículas
        if (particulasTrabajando == null)
        {
            emisionesOriginales = new float[0];
            return;
        }

        emisionesOriginales = new float[particulasTrabajando.Length];
        for (int i = 0; i < particulasTrabajando.Length; i++)
        {
            if (particulasTrabajando[i] == null) continue;
            emisionesOriginales[i] = particulasTrabajando[i].emission.rateOverTimeMultiplier;
        }
    }

    private void Update()
    {
        // Girar objetos decorativos en eje Z
        for (int i = 0; i < objetosGiratorios.Count; i++)
        {
            if (objetosGiratorios[i] != null && i < velocidadesActualesZ.Count)
            {
                objetosGiratorios[i].Rotate(0f, 0f, velocidadesActualesZ[i] * Time.deltaTime, Space.Self);
            }
        }
    }

    private void FixedUpdate()
    {
        DetectarPiedrasNuevas();
        AplicarDictaduraSuccion();

        // Limpieza ocasional de la memoria por si el jugador destruye piedras
        if (Time.frameCount % 300 == 0)
        {
            historialPiedras.RemoveWhere(item => item == null);
        }
    }


    // =====================================================
    // LÓGICA DE ABSORCIÓN CÓNICA Y ÓRBITA CON OFFSET
    // =====================================================

    void DetectarPiedrasNuevas()
    {
        for (int i = 0; i < tuberiasAbajo.Length; i++)
        {
            TuberiaAbsorcion tuberia = tuberiasAbajo[i];
            if (tuberia == null || tuberia.puntoAbsorcion == null) continue;

            Collider[] piedrasCercanas = Physics.OverlapSphere(tuberia.puntoAbsorcion.position, tuberia.distanciaAbsorcion, capaPiedras);
            Vector3 direccionConoMundo = EjeCono(tuberia);
            Vector3 boquilla = tuberia.puntoAbsorcion.position;

            foreach (var col in piedrasCercanas)
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb == null || !rb.gameObject.activeInHierarchy || piedrasEnSuccion.ContainsKey(rb) || piedrasEnProceso.ContainsKey(rb)) continue;

                // Con el cono apoyado, una piedra en el suelo cuenta si su parte de arriba llega a la cara de abajo del cono.
                Vector3 puntoPiedra = rb.position;
                if (tuberia.apoyarEnSuelo && puntoPiedra.y < boquilla.y)
                    puntoPiedra.y = Mathf.Min(boquilla.y + 0.001f, puntoPiedra.y + col.bounds.extents.y);

                Vector3 dirHaciaPiedra = puntoPiedra - boquilla;
                float distancia = dirHaciaPiedra.magnitude;

                if (distancia <= tuberia.distanciaAbsorcion && distancia > 0.001f)
                {
                    float angulo = Vector3.Angle(direccionConoMundo, dirHaciaPiedra);

                    if (angulo <= tuberia.anguloCono / 2f)
                    {
                        // EXPULSIÓN INMEDIATA SI YA PASÓ POR ESTA MÁQUINA
                        if (evitarProcesadoRepetido && historialPiedras.Contains(rb))
                        {
                            RechazarPiedra(rb);
                            continue;
                        }

                        DatosPiedra datos = new DatosPiedra
                        {
                            rb = rb,
                            tuberia = tuberia,
                            escala = EscalaPiedra.De(rb),
                            distanciaInicial = distancia
                        };
                        // Tamaño real, aunque la piedra aún estuviera creciendo; y deja de crecer.
                        datos.escalaOriginal = datos.escala.TomarControl(this);
                        piedrasEnSuccion.Add(rb, datos);
                    }
                }
            }
        }
    }

    void AplicarDictaduraSuccion()
    {
        List<Rigidbody> piedrasParaTragar = new List<Rigidbody>();
        List<Rigidbody> piedrasPerdidas = new List<Rigidbody>();

        foreach (var kvp in piedrasEnSuccion)
        {
            Rigidbody rb = kvp.Key;
            DatosPiedra datos = kvp.Value;

            // Otro script (el jugador, otra máquina...) se ha llevado la piedra: se suelta sin tocar su escala.
            if (rb != null && datos.escala != null && !datos.escala.Controla(this))
            {
                piedrasPerdidas.Add(rb);
                continue;
            }

            if (rb == null || !rb.gameObject.activeInHierarchy)
            {
                piedrasPerdidas.Add(rb);
                continue;
            }

            Vector3 vectorHaciaPunta = datos.tuberia.puntoAbsorcion.position - rb.position;
            Vector3 dirHaciaPunta = vectorHaciaPunta.normalized;
            float distanciaActual = vectorHaciaPunta.magnitude;

            if (distanciaActual <= distanciaConsumo)
            {
                piedrasParaTragar.Add(rb);
            }
            else
            {
                Vector3 ejeConoMundo = EjeCono(datos.tuberia);

                Vector3 dirDesdePunta = -vectorHaciaPunta;
                float distanciaEje = Vector3.Dot(dirDesdePunta, ejeConoMundo);

                Vector3 puntoEnEje = datos.tuberia.puntoAbsorcion.position + ejeConoMundo * distanciaEje;
                Vector3 vectorHaciaEje = puntoEnEje - rb.position;
                float radioActual = vectorHaciaEje.magnitude;

                float radioMaximoLocal = Mathf.Abs(distanciaEje) * Mathf.Tan((datos.tuberia.anguloCono / 2f) * Mathf.Deg2Rad);
                float radioObjetivo = radioMaximoLocal * datos.tuberia.amplitudOrbita;

                Vector3 vectorAtraccion = dirHaciaPunta * datos.tuberia.velocidadSuccion;

                float errorRadio = radioActual - radioObjetivo;
                Vector3 direccionCorreccion = (radioActual > 0.001f) ? vectorHaciaEje.normalized : Vector3.zero;
                Vector3 vectorOffsetOrbita = direccionCorreccion * (errorRadio * datos.tuberia.velocidadSuccion * 3f);

                float rangoTotal = datos.distanciaInicial - distanciaConsumo;
                float progresoLejania = 1f;

                if (rangoTotal > 0f)
                {
                    progresoLejania = Mathf.Clamp01((distanciaActual - distanciaConsumo) / rangoTotal);
                    rb.transform.localScale = datos.escalaOriginal * progresoLejania;
                }

                float aceleradorReal = Mathf.Max(1f, datos.tuberia.multiplicadorAceleracionCentro);
                float velocidadOrbitaActual = Mathf.Lerp(datos.tuberia.velocidadOrbita * aceleradorReal, datos.tuberia.velocidadOrbita, progresoLejania);

                Vector3 direccionGiro = Vector3.Cross(ejeConoMundo, direccionCorreccion).normalized;

                if (direccionGiro.sqrMagnitude < 0.001f)
                {
                    Vector3 ejeAlt = Vector3.up;
                    if (Mathf.Abs(Vector3.Dot(ejeConoMundo, Vector3.up)) > 0.99f) ejeAlt = Vector3.right;
                    direccionGiro = Vector3.Cross(ejeConoMundo, ejeAlt).normalized;
                }

                Vector3 vectorGiro = direccionGiro * velocidadOrbitaActual;

                if (!rb.isKinematic) rb.linearVelocity = vectorAtraccion + vectorOffsetOrbita + vectorGiro;
            }
        }

        foreach (var rb in piedrasPerdidas) piedrasEnSuccion.Remove(rb);

        foreach (var rb in piedrasParaTragar)
        {
            DatosPiedra datos = piedrasEnSuccion[rb];
            piedrasEnSuccion.Remove(rb);

            EvaluarPiedraAbsorbida(rb, datos.escalaOriginal);
        }
    }

    void EvaluarPiedraAbsorbida(Rigidbody rb, Vector3 escalaOriginal)
    {
        DeformacionPiedra deformacion = ObtenerDeformacion(rb);
        if (deformacion == null) return;

        float porcentajeActual = deformacion.ObtenerPorcentajeDesgasteHaciaEsfera();

        if (PuedeProcesarPureza(porcentajeActual))
        {
            if (GestorPiedras.Instancia != null) GestorPiedras.Instancia.DesregistrarPiedra(rb);
            StartCoroutine(RutinaProcesarPiedra(rb, deformacion, porcentajeActual, escalaOriginal));
        }
        else
        {
            EscalaPiedra.De(rb).RestaurarBase();
            RechazarPiedra(rb);
        }
    }


    // =====================================================
    // CONSULTA PARA BOTS Y ENTRADA DIRECTA
    // =====================================================

    public bool PuedeProcesarPureza(float pureza)
    {
        if (rechazarPiedrasCompletadas && pureza >= 100f) return false;
        return true;
    }

    public bool PuedeAceptarPiedra(Rigidbody rb)
    {
        if (rb == null) return false;
        if (piedrasEnProceso.ContainsKey(rb)) return false;

        // Comprobación de seguridad por si el Bot o Trigger intenta meterla directamente
        if (evitarProcesadoRepetido && historialPiedras.Contains(rb)) return false;

        DeformacionPiedra deformacion = ObtenerDeformacion(rb);
        if (deformacion == null) return false;

        return PuedeProcesarPureza(deformacion.ObtenerPorcentajeDesgasteHaciaEsfera());
    }

    public bool RecibirPiedraBot(Rigidbody rb)
    {
        if (!PuedeAceptarPiedra(rb)) return false;

        DeformacionPiedra deformacion = ObtenerDeformacion(rb);
        if (deformacion == null) return false;

        float porcentajeActual = deformacion.ObtenerPorcentajeDesgasteHaciaEsfera();
        Vector3 escalaOriginal = EscalaPiedra.De(rb).TomarControl(this);

        if (GestorPiedras.Instancia != null) GestorPiedras.Instancia.DesregistrarPiedra(rb);

        rb.transform.SetParent(null, true);
        deformacion.enabled = true;

        rb.transform.localScale = Vector3.zero;

        StartCoroutine(RutinaProcesarPiedra(rb, deformacion, porcentajeActual, escalaOriginal));

        return true;
    }


    // =====================================================
    // OBTENER DEFORMACIÓN
    // =====================================================

    private DeformacionPiedra ObtenerDeformacion(Rigidbody rb)
    {
        if (rb == null) return null;

        DeformacionPiedra deformacion = rb.GetComponent<DeformacionPiedra>();
        if (deformacion == null) deformacion = rb.GetComponentInChildren<DeformacionPiedra>();
        if (deformacion == null) deformacion = rb.GetComponentInParent<DeformacionPiedra>();

        return deformacion;
    }


    // =====================================================
    // RECHAZAR Y PARTÍCULAS
    // =====================================================

    private Vector3 ObtenerDireccionConAngulo(Vector3 direccionBase, float anguloApertura)
    {
        if (anguloApertura <= 0f) return direccionBase;
        return Quaternion.AngleAxis(Random.Range(0f, anguloApertura), Random.onUnitSphere) * direccionBase;
    }

    private void RechazarPiedra(Rigidbody rb)
    {
        if (rb == null || puntoRechazo == null) return;

        rb.position = puntoRechazo.position;

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        rb.isKinematic = false;

        Vector3 direccionBase = puntoRechazo.TransformDirection(direccionRechazo.normalized);
        Vector3 direccionFinal = ObtenerDireccionConAngulo(direccionBase, anguloDispersionRechazo);
        float fuerzaAleatoria = Random.Range(fuerzaMinimaRechazo, fuerzaMaximaRechazo);

        rb.AddForce(direccionFinal * fuerzaAleatoria, ForceMode.Impulse);
    }

    private void ActualizarEmisionParticulas()
    {
        List<Rigidbody> clavesEliminar = new List<Rigidbody>();

        foreach (Rigidbody rb in piedrasEnProceso.Keys)
        {
            if (rb == null) clavesEliminar.Add(rb);
        }

        foreach (Rigidbody rb in clavesEliminar)
        {
            piedrasEnProceso.Remove(rb);
        }

        if (piedrasEnProceso.Count == 0)
        {
            if (particulasTrabajando != null)
            {
                foreach (ParticleSystem ps in particulasTrabajando) { if (ps != null) ps.Stop(); }
            }
            return;
        }

        if (particulasTrabajando == null) return;
        float factorEmision = 1f;

        for (int i = 0; i < particulasTrabajando.Length; i++)
        {
            ParticleSystem ps = particulasTrabajando[i];
            if (ps == null) continue;

            var emision = ps.emission;
            float emisionBase = 0f;

            if (emisionesOriginales != null && i < emisionesOriginales.Length)
            {
                emisionBase = emisionesOriginales[i];
            }

            emision.rateOverTimeMultiplier = emisionBase * factorEmision;

            if (!ps.isPlaying && factorEmision > 0f) ps.Play();
        }
    }


    // =====================================================
    // PROCESAR (CORRUTINA PRINCIPAL)
    // =====================================================

    private IEnumerator RutinaProcesarPiedra(Rigidbody rb, DeformacionPiedra deformacion, float porcentajeInicial, Vector3 escalaOriginal)
    {
        if (rb == null || deformacion == null) yield break;

        if (!piedrasEnProceso.ContainsKey(rb)) piedrasEnProceso.Add(rb, porcentajeInicial);

        // REGISTRAMOS LA PIEDRA EN LA MEMORIA DE LA MÁQUINA
        if (evitarProcesadoRepetido)
        {
            historialPiedras.Add(rb);
        }

        ActualizarEmisionParticulas();

        // Apagar físicas y mallas
        // Primero se para y luego se vuelve cinemática (a un cuerpo cinemático no se le puede poner velocidad).
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        rb.isKinematic = true;

        MeshRenderer[] renderizadores = rb.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer mr in renderizadores) { if (mr != null) mr.enabled = false; }

        Collider[] colisionadores = rb.GetComponentsInChildren<Collider>();
        foreach (Collider col in colisionadores) { if (col != null) col.enabled = false; }

        float tiempoTranscurrido = 0f;
        float cantidadRealAProcesar = porcentajeExtraAProcesar / 100f;
        float velocidadRealPulido = cantidadRealAProcesar / Mathf.Max(0.01f, tiempoProcesamiento);

        Transform transformPuntoCentral = tuberiasAbajo.Length > 0 ? tuberiasAbajo[0].puntoAbsorcion : puntoSalida;

        while (tiempoTranscurrido < tiempoProcesamiento)
        {
            if (rb == null || deformacion == null)
            {
                if (rb != null) piedrasEnProceso.Remove(rb);
                ActualizarEmisionParticulas();
                yield break;
            }

            tiempoTranscurrido += Time.deltaTime;
            float progreso = tiempoTranscurrido / Mathf.Max(0.01f, tiempoProcesamiento);

            if (transformPuntoCentral != null && puntoSalida != null)
            {
                rb.position = Vector3.Lerp(transformPuntoCentral.position, puntoSalida.position, progreso);
            }

            rb.rotation = Quaternion.Euler(
                Mathf.Lerp(0f, 360f, progreso * 5f),
                Mathf.Lerp(0f, 360f, progreso * 3f),
                0f
            );

            deformacion.PulirUniformemente(velocidadRealPulido * Time.deltaTime);

            yield return null;
        }

        if (rb == null)
        {
            ActualizarEmisionParticulas();
            yield break;
        }

        // =================================================
        // EXPULSIÓN DE LA PIEDRA
        // =================================================

        if (puntoSalida != null) rb.position = puntoSalida.position;

        foreach (MeshRenderer mr in renderizadores) { if (mr != null) mr.enabled = true; }
        foreach (Collider col in colisionadores) { if (col != null) col.enabled = true; }

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (puntoSalida != null)
        {
            Vector3 direccionBase = puntoSalida.TransformDirection(direccionSalida.normalized);
            Vector3 direccionFinal = ObtenerDireccionConAngulo(direccionBase, anguloDispersionSalida);
            float fuerzaAleatoria = Random.Range(fuerzaMinimaExpulsion, fuerzaMaximaExpulsion);

            rb.AddForce(direccionFinal * fuerzaAleatoria, ForceMode.Impulse);
        }

        // Llamamos a la animación de crecer el tamaño
        EscalaPiedra.De(rb).Crecer(tiempoCrecimiento);

        piedrasEnProceso.Remove(rb);
        ActualizarEmisionParticulas();

        if (deformacion != null)
        {
            deformacion.enabled = true;
            deformacion.Despertar();
        }

        if (GestorPiedras.Instancia != null) GestorPiedras.Instancia.RegistrarPiedra(rb);

        if (particulasExpulsion != null)
        {
            foreach (ParticleSystem ps in particulasExpulsion) { if (ps != null) ps.Play(); }
        }
    }


    // =====================================================
    // GIZMOS
    // =====================================================

    private void OnDrawGizmos()
    {
        // Cono de Absorción y Ruta de Órbita
        for (int i = 0; i < tuberiasAbajo.Length; i++)
        {
            TuberiaAbsorcion tuberia = tuberiasAbajo[i];
            if (tuberia != null && tuberia.puntoAbsorcion != null)
            {
                Vector3 direccionConoMundo = EjeCono(tuberia);

                // Dibujar el área de detección máxima (Verde)
                Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
                DibujarConoDeteccion(tuberia.puntoAbsorcion.position, direccionConoMundo, tuberia.distanciaAbsorcion, tuberia.anguloCono);

                // Dibujar el área de órbita ideal (Amarillo)
                Gizmos.color = new Color(1f, 0.9f, 0f, 0.6f);

                float anguloMitadExterior = (tuberia.anguloCono / 2f) * Mathf.Deg2Rad;
                float radioMaximoExterior = Mathf.Tan(anguloMitadExterior);
                float radioMaximoInterior = radioMaximoExterior * tuberia.amplitudOrbita;
                float anguloInteriorReal = Mathf.Atan(radioMaximoInterior) * Mathf.Rad2Deg * 2f;

                DibujarConoDeteccion(tuberia.puntoAbsorcion.position, direccionConoMundo, tuberia.distanciaAbsorcion, anguloInteriorReal);
            }
        }

        // Cono de Rechazo (Rojo)
        if (puntoRechazo != null && direccionRechazo != Vector3.zero)
        {
            Vector3 dirRealRechazo = puntoRechazo.TransformDirection(direccionRechazo.normalized);
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            DibujarConoGizmo(puntoRechazo.position, dirRealRechazo, fuerzaMinimaRechazo, anguloDispersionRechazo);
            Gizmos.color = Color.red;
            DibujarConoGizmo(puntoRechazo.position, dirRealRechazo, fuerzaMaximaRechazo, anguloDispersionRechazo);
        }

        // Cono de Salida (Verde)
        if (puntoSalida != null && direccionSalida != Vector3.zero)
        {
            Vector3 dirRealSalida = puntoSalida.TransformDirection(direccionSalida.normalized);
            Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
            DibujarConoGizmo(puntoSalida.position, dirRealSalida, fuerzaMinimaExpulsion, anguloDispersionSalida);
            Gizmos.color = Color.green;
            DibujarConoGizmo(puntoSalida.position, dirRealSalida, fuerzaMaximaExpulsion, anguloDispersionSalida);
        }

        if (puntoEntregaBot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(puntoEntregaBot.position, 0.35f);
        }
    }

    // Eje real del cono. Apoyado: la dirección indicada pasa a ser la cara de abajo del cono (con su inclinación,
    // para poder orientarlo hacia arriba o hacia abajo) y el eje sube la mitad de la apertura sobre ella.
    private Vector3 EjeCono(TuberiaAbsorcion tuberia)
    {
        Vector3 eje = tuberia.puntoAbsorcion.TransformDirection(tuberia.direccionAbsorcionLocal.normalized);
        if (!tuberia.apoyarEnSuelo) return eje;

        Vector3 horizontal = Vector3.ProjectOnPlane(eje, Vector3.up);
        if (horizontal.sqrMagnitude < 0.0001f) return eje; // Cono vertical: no hay "suelo" sobre el que apoyarlo.

        Vector3 derecha = Vector3.Cross(Vector3.up, horizontal.normalized);
        return Quaternion.AngleAxis(-tuberia.anguloCono / 2f, derecha) * eje;
    }

    // Pirámide de base cuadrada con un lado abajo (cara inferior plana), que envuelve al cono.
    private void DibujarConoDeteccion(Vector3 origen, Vector3 direccionCentral, float distancia, float anguloTotal)
    {
        if (distancia <= 0f) return;

        Vector3 finCentral = origen + (direccionCentral * distancia);
        Gizmos.DrawLine(origen, finCentral);

        if (anguloTotal <= 0f) return;

        Vector3 lateral = Vector3.Cross(Vector3.up, direccionCentral);
        if (lateral.magnitude < 0.01f) lateral = Vector3.right;
        lateral.Normalize();
        Vector3 arriba = Vector3.Cross(direccionCentral, lateral).normalized;

        float radio = Mathf.Tan(Mathf.Min(anguloTotal, 179f) / 2f * Mathf.Deg2Rad) * distancia;

        Vector3 abajoIzquierda = finCentral + (-lateral - arriba) * radio;
        Vector3 abajoDerecha = finCentral + (lateral - arriba) * radio;
        Vector3 arribaDerecha = finCentral + (lateral + arriba) * radio;
        Vector3 arribaIzquierda = finCentral + (-lateral + arriba) * radio;

        Gizmos.DrawLine(origen, abajoIzquierda);
        Gizmos.DrawLine(origen, abajoDerecha);
        Gizmos.DrawLine(origen, arribaDerecha);
        Gizmos.DrawLine(origen, arribaIzquierda);

        Gizmos.DrawLine(abajoIzquierda, abajoDerecha);
        Gizmos.DrawLine(abajoDerecha, arribaDerecha);
        Gizmos.DrawLine(arribaDerecha, arribaIzquierda);
        Gizmos.DrawLine(arribaIzquierda, abajoIzquierda);
    }

    private void DibujarConoGizmo(Vector3 origen, Vector3 direccionCentral, float fuerza, float angulo)
    {
        float tamañoVisual = Mathf.Clamp(fuerza * 0.15f, 0.5f, 7f);
        Vector3 finCentral = origen + (direccionCentral * tamañoVisual);
        Gizmos.DrawLine(origen, finCentral);

        if (angulo <= 0f) return;

        Vector3 ejeX = Vector3.Cross(direccionCentral, Vector3.up);
        if (ejeX.magnitude < 0.01f) ejeX = Vector3.right;
        ejeX.Normalize();

        Vector3 ejeY = Vector3.Cross(direccionCentral, ejeX).normalized;

        Vector3 p1 = origen + (Quaternion.AngleAxis(angulo, ejeX) * direccionCentral * tamañoVisual);
        Vector3 p2 = origen + (Quaternion.AngleAxis(-angulo, ejeX) * direccionCentral * tamañoVisual);
        Vector3 p3 = origen + (Quaternion.AngleAxis(angulo, ejeY) * direccionCentral * tamañoVisual);
        Vector3 p4 = origen + (Quaternion.AngleAxis(-angulo, ejeY) * direccionCentral * tamañoVisual);

        Gizmos.DrawLine(origen, p1);
        Gizmos.DrawLine(origen, p2);
        Gizmos.DrawLine(origen, p3);
        Gizmos.DrawLine(origen, p4);
        Gizmos.DrawLine(p1, p3);
        Gizmos.DrawLine(p3, p2);
        Gizmos.DrawLine(p2, p4);
        Gizmos.DrawLine(p4, p1);
    }
}