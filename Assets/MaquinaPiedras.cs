using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MaquinaPiedras : MonoBehaviour
{
    [System.Serializable]
    public struct TuberiaAbsorcion
    {
        public Transform puntoAbsorcion;

        [Header("Control y Efectos")]
        [Tooltip("Arrastra aquí el objeto del Botón que controla esta tubería")]
        public BotonTuberia botonControl;

        [Header("Zonas de Acción")]
        [Tooltip("Hasta dónde llega la 'porción de pizza' (Radio)")]
        public float radioAbsorcion;
        [Tooltip("El grosor/altura del cilindro (hacia arriba del transform)")]
        public float alturaAbsorcion;
        [Range(0, 360)]
        [Tooltip("La apertura del ángulo de la pizza")]
        public float anguloAbsorcion;
        [Tooltip("Velocidad constante a la que la piedra es atraída")]
        public float velocidadSuccion;
    }

    [Header("Configuración de Absorción")]
    public TuberiaAbsorcion[] tuberiasAbajo = new TuberiaAbsorcion[4];
    public LayerMask capaPiedras;
    public float distanciaConsumo = 0.5f;
    [Tooltip("Metros extra alrededor de cada tubería en los que se buscan piedras rápidas que podrían cruzar la zona entre dos pasos de física.")]
    [Min(0f)] public float margenDeteccionRapidas = 2f;

    [Header("Configuración de Lanzamiento")]
    public Transform puntoLanzamiento;
    public float fuerzaLanzamiento = 15f;
    public float ritmoLanzamiento = 0.1f;
    public float tiempoCrecimiento = 0.2f;

    // ========================================================
    // NUEVO: SISTEMA DE NEONES 
    // ========================================================
    [Header("Indicadores Visuales (Neones)")]
    [Tooltip("Arrastra aquí los objetos con el script ControladorNeon de esta máquina")]
    public ControladorNeon[] neonesMaquina;

    [Tooltip("La fuerza a la que el neón empezará a cambiar de color (fuerza mínima)")]
    public float fuerzaLanzamientoMinima = 5f;
    [Tooltip("La fuerza a la que el neón llegará a su color máximo")]
    public float fuerzaLanzamientoMaxima = 30f;

    [ColorUsage(true, true)]
    public Color colorFuerzaBaja = Color.green;
    [ColorUsage(true, true)]
    public Color colorFuerzaAlta = Color.red;

    [Tooltip("Velocidad a la que el LED reacciona a los cambios de fuerza")]
    public float velocidadTransicionLed = 10f;

    private float porcentajeFuerzaActual = 0f;
    // ========================================================

    private class DatosPiedra
    {
        public Rigidbody rb;
        public TuberiaAbsorcion tuberia;
        public Vector3 escalaOriginal;
        public float distanciaInicial;
        public EscalaPiedra escala;
    }

    private Dictionary<Rigidbody, DatosPiedra> piedrasEnSuccion = new Dictionary<Rigidbody, DatosPiedra>();
    private Queue<DatosPiedra> piedrasAlmacenadas = new Queue<DatosPiedra>();
    private float temporizadorLanzamiento = 0f;

    void FixedUpdate()
    {
        DetectarPiedrasNuevas();
        AplicarDictaduraSuccion();
    }

    void Update()
    {
        ManejarLanzamiento();
        ManejarLeds(); // Nueva función para los Leds
    }

    // ========================================================
    // LÓGICA DE LOS LEDS
    // ========================================================
    void ManejarLeds()
    {
        if (neonesMaquina == null || neonesMaquina.Length == 0) return;

        // 1. Calculamos en qué porcentaje estamos entre la fuerza mínima y máxima (0 a 1)
        float porcentajeObjetivo = Mathf.InverseLerp(fuerzaLanzamientoMinima, fuerzaLanzamientoMaxima, fuerzaLanzamiento);

        // 2. Transición suave del led
        porcentajeFuerzaActual = Mathf.Lerp(porcentajeFuerzaActual, porcentajeObjetivo, Time.deltaTime * velocidadTransicionLed);

        // 3. Calculamos el color final mezclando el color bajo y el alto
        Color colorFinal = Color.Lerp(colorFuerzaBaja, colorFuerzaAlta, porcentajeFuerzaActual);

        // 4. Se lo aplicamos a todos los neones que tenga la máquina
        foreach (ControladorNeon neon in neonesMaquina)
        {
            if (neon != null)
            {
                neon.colorNeon = colorFinal;
            }
        }
    }
    // ========================================================

    void DetectarPiedrasNuevas()
    {
        foreach (var tuberia in tuberiasAbajo)
        {
            if (tuberia.puntoAbsorcion == null) continue;

            // IGNORAR SI EL BOTÓN ESTÁ APAGADO
            if (tuberia.botonControl != null && !tuberia.botonControl.encendido) continue;

            float radioBusqueda = Mathf.Max(tuberia.radioAbsorcion, tuberia.alturaAbsorcion) + margenDeteccionRapidas;
            Collider[] piedrasCercanas = Physics.OverlapSphere(tuberia.puntoAbsorcion.position, radioBusqueda, capaPiedras);

            foreach (var col in piedrasCercanas)
            {
                // attachedRigidbody: también vale si el collider está en un hijo de la piedra.
                Rigidbody rb = col.attachedRigidbody;
                if (rb == null || !rb.gameObject.activeInHierarchy || piedrasEnSuccion.ContainsKey(rb)) continue;

                if (!PiedraCruzaZona(tuberia, rb, col)) continue;

                DatosPiedra datos = new DatosPiedra
                {
                    rb = rb,
                    tuberia = tuberia,
                    escala = EscalaPiedra.De(rb),
                    distanciaInicial = Vector3.Distance(rb.position, tuberia.puntoAbsorcion.position)
                };
                // Tamaño real, aunque la piedra aún estuviera creciendo; y deja de crecer.
                datos.escalaOriginal = datos.escala.TomarControl(this);
                piedrasEnSuccion.Add(rb, datos);
            }
        }
    }

    // La zona es una "loncha" fina: una piedra rápida (lanzada por otra máquina, la diana...) puede atravesarla
    // entera entre dos pasos de física. Por eso se comprueba todo el tramo recorrido en el último paso
    // y se tiene en cuenta el tamaño de la piedra, no solo su centro.
    bool PiedraCruzaZona(TuberiaAbsorcion tuberia, Rigidbody rb, Collider col)
    {
        float radioPiedra = col.bounds.extents.magnitude * 0.5f;
        Vector3 actual = rb.position;
        Vector3 anterior = actual - rb.linearVelocity * Time.fixedDeltaTime;

        int muestras = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(anterior, actual) / 0.1f), 1, 16);
        for (int i = 0; i <= muestras; i++)
        {
            if (PuntoEnZona(tuberia, Vector3.Lerp(anterior, actual, (float)i / muestras), radioPiedra))
                return true;
        }
        return false;
    }

    bool PuntoEnZona(TuberiaAbsorcion tuberia, Vector3 punto, float tolerancia)
    {
        Vector3 dirHaciaPunto = punto - tuberia.puntoAbsorcion.position;
        float alturaLocal = Vector3.Dot(dirHaciaPunto, tuberia.puntoAbsorcion.up);
        if (alturaLocal < -tolerancia || alturaLocal > tuberia.alturaAbsorcion + tolerancia) return false;

        Vector3 direccionPlana = dirHaciaPunto - (tuberia.puntoAbsorcion.up * alturaLocal);
        float distanciaPlana = direccionPlana.magnitude;
        if (distanciaPlana > tuberia.radioAbsorcion + tolerancia) return false;
        if (distanciaPlana <= 0.001f) return true;

        return Vector3.Angle(tuberia.puntoAbsorcion.forward, direccionPlana / distanciaPlana) <= tuberia.anguloAbsorcion / 2f;
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

            // Comprobar si apagaron la máquina en mitad del vuelo de esta piedra
            bool maquinaApagada = datos.tuberia.botonControl != null && !datos.tuberia.botonControl.encendido;

            if (rb == null || !rb.gameObject.activeInHierarchy || maquinaApagada)
            {
                if (rb != null && maquinaApagada)
                {
                    // Si se apaga, le devolvemos su tamaño original para que caiga de forma realista
                    datos.escala.RestaurarBase();
                }
                piedrasPerdidas.Add(rb);
                continue;
            }

            Vector3 vectorHaciaCentro = datos.tuberia.puntoAbsorcion.position - rb.position;
            float distanciaActual = vectorHaciaCentro.magnitude;

            if (distanciaActual <= distanciaConsumo)
            {
                piedrasParaTragar.Add(rb);
            }
            else
            {
                Vector3 direccionNormalizada = vectorHaciaCentro.normalized;
                if (!rb.isKinematic) rb.linearVelocity = direccionNormalizada * datos.tuberia.velocidadSuccion;

                float rangoTotal = datos.distanciaInicial - distanciaConsumo;
                if (rangoTotal > 0f)
                {
                    float progreso = (distanciaActual - distanciaConsumo) / rangoTotal;
                    float porcentajeEscala = Mathf.Clamp01(progreso);
                    rb.transform.localScale = datos.escalaOriginal * porcentajeEscala;
                }
            }
        }

        foreach (var rb in piedrasPerdidas) piedrasEnSuccion.Remove(rb);

        foreach (var rb in piedrasParaTragar)
        {
            DatosPiedra datos = piedrasEnSuccion[rb];
            piedrasEnSuccion.Remove(rb);

            rb.gameObject.SetActive(false);
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.transform.localScale = Vector3.zero;

            piedrasAlmacenadas.Enqueue(datos);
        }
    }

    void ManejarLanzamiento()
    {
        if (piedrasAlmacenadas.Count > 0)
        {
            temporizadorLanzamiento += Time.deltaTime;

            if (temporizadorLanzamiento >= ritmoLanzamiento)
            {
                temporizadorLanzamiento = 0f;
                LanzarPiedra();
            }
        }
        else
        {
            temporizadorLanzamiento = 0f;
        }
    }

    void LanzarPiedra()
    {
        if (puntoLanzamiento == null) return;

        DatosPiedra piedra = piedrasAlmacenadas.Dequeue();
        Rigidbody rb = piedra.rb;

        if (rb == null) return;

        rb.transform.position = puntoLanzamiento.position;
        rb.transform.rotation = puntoLanzamiento.rotation;
        rb.transform.localScale = Vector3.zero;
        rb.gameObject.SetActive(true);

        rb.linearVelocity = Vector3.zero;
        rb.AddForce(puntoLanzamiento.forward * fuerzaLanzamiento, ForceMode.Impulse);

        piedra.escala.Crecer(tiempoCrecimiento);
    }

    // ==========================================
    // GIZMOS
    // ==========================================
    void OnDrawGizmosSelected()
    {
        foreach (var tuberia in tuberiasAbajo)
        {
            if (tuberia.puntoAbsorcion != null)
            {
                // Si el botón está apagado, pintamos la zona de rojo en vez de verde
                bool encendido = (tuberia.botonControl == null) || tuberia.botonControl.encendido;
                Gizmos.color = encendido ? new Color(0f, 1f, 0f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);

                Vector3 origenAbajo = tuberia.puntoAbsorcion.position;
                Vector3 origenArriba = origenAbajo + tuberia.puntoAbsorcion.up * tuberia.alturaAbsorcion;
                Vector3 forward = tuberia.puntoAbsorcion.forward;
                Vector3 up = tuberia.puntoAbsorcion.up;

                float radio = tuberia.radioAbsorcion;
                float angulo = tuberia.anguloAbsorcion;

                DibujarArcoPizza(origenAbajo, forward, up, radio, angulo);
                DibujarArcoPizza(origenArriba, forward, up, radio, angulo);

                Quaternion rotIzquierda = Quaternion.AngleAxis(-angulo / 2f, up);
                Quaternion rotDerecha = Quaternion.AngleAxis(angulo / 2f, up);
                Vector3 bordeIzquierdo = rotIzquierda * forward;
                Vector3 bordeDerecho = rotDerecha * forward;

                Gizmos.DrawLine(origenAbajo, origenAbajo + bordeIzquierdo * radio);
                Gizmos.DrawLine(origenAbajo, origenAbajo + bordeDerecho * radio);

                Gizmos.DrawLine(origenArriba, origenArriba + bordeIzquierdo * radio);
                Gizmos.DrawLine(origenArriba, origenArriba + bordeDerecho * radio);

                Gizmos.DrawLine(origenAbajo, origenArriba);
                Gizmos.DrawLine(origenAbajo + bordeIzquierdo * radio, origenArriba + bordeIzquierdo * radio);
                Gizmos.DrawLine(origenAbajo + bordeDerecho * radio, origenArriba + bordeDerecho * radio);
            }
        }

        if (puntoLanzamiento != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(puntoLanzamiento.position, 0.2f);

            Vector3 direccionDisparo = puntoLanzamiento.forward;
            Gizmos.DrawRay(puntoLanzamiento.position, direccionDisparo * 3f);
            Gizmos.DrawRay(puntoLanzamiento.position + direccionDisparo * 3f, (puntoLanzamiento.rotation * Quaternion.Euler(150, 0, 0)) * Vector3.forward * 0.5f);
            Gizmos.DrawRay(puntoLanzamiento.position + direccionDisparo * 3f, (puntoLanzamiento.rotation * Quaternion.Euler(-150, 0, 0)) * Vector3.forward * 0.5f);
        }
    }

    void DibujarArcoPizza(Vector3 centro, Vector3 forward, Vector3 up, float radio, float anguloTotal)
    {
        int segmentos = Mathf.Max(10, Mathf.RoundToInt(anguloTotal / 5f));
        float paso = anguloTotal / segmentos;
        float anguloInicial = -anguloTotal / 2f;

        Vector3 puntoAnterior = centro + (Quaternion.AngleAxis(anguloInicial, up) * forward) * radio;

        for (int i = 1; i <= segmentos; i++)
        {
            float anguloActual = anguloInicial + (i * paso);
            Vector3 puntoActual = centro + (Quaternion.AngleAxis(anguloActual, up) * forward) * radio;
            Gizmos.DrawLine(puntoAnterior, puntoActual);
            puntoAnterior = puntoActual;
        }
    }
}