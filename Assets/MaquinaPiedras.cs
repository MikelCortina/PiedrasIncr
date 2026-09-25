using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MaquinaPiedras : MonoBehaviour
{
    [System.Serializable]
    public struct TuberiaAbsorcion
    {
        public Transform puntoAbsorcion;
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

    [Header("Configuración de Lanzamiento")]
    public Transform puntoLanzamiento;
    public float fuerzaLanzamiento = 15f;
    public float ritmoLanzamiento = 0.1f;
    public float tiempoCrecimiento = 0.2f;

    private class DatosPiedra
    {
        public Rigidbody rb;
        public TuberiaAbsorcion tuberia;
        public Vector3 escalaOriginal;
        public float distanciaInicial;
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
    }

    void DetectarPiedrasNuevas()
    {
        foreach (var tuberia in tuberiasAbajo)
        {
            if (tuberia.puntoAbsorcion == null) continue;

            // Buscamos a lo bruto en una esfera que englobe todo, y luego filtramos matemáticamente
            Collider[] piedrasCercanas = Physics.OverlapSphere(tuberia.puntoAbsorcion.position, Mathf.Max(tuberia.radioAbsorcion, tuberia.alturaAbsorcion), capaPiedras);

            foreach (var col in piedrasCercanas)
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb == null || !rb.gameObject.activeInHierarchy || piedrasEnSuccion.ContainsKey(rb)) continue;

                Vector3 dirHaciaPiedra = rb.position - tuberia.puntoAbsorcion.position;

                // 1. Filtrar por Altura (Eje Y local del punto de absorción)
                float alturaLocal = Vector3.Dot(dirHaciaPiedra, tuberia.puntoAbsorcion.up);

                // Si está apoyado sobre su parte plana, medimos desde 0 hasta la altura máxima
                if (alturaLocal >= 0f && alturaLocal <= tuberia.alturaAbsorcion)
                {
                    // 2. Proyectar sobre el plano 2D (quitando la altura)
                    Vector3 direccionPlana = dirHaciaPiedra - (tuberia.puntoAbsorcion.up * alturaLocal);
                    float distanciaPlana = direccionPlana.magnitude;

                    // 3. Filtrar por Radio (largo de la pizza)
                    if (distanciaPlana <= tuberia.radioAbsorcion)
                    {
                        float angulo = 0f;
                        if (distanciaPlana > 0.001f)
                        {
                            angulo = Vector3.Angle(tuberia.puntoAbsorcion.forward, direccionPlana.normalized);
                        }

                        // 4. Filtrar por Ángulo (ancho de la pizza)
                        if (angulo <= tuberia.anguloAbsorcion / 2f)
                        {
                            DatosPiedra datos = new DatosPiedra
                            {
                                rb = rb,
                                tuberia = tuberia,
                                escalaOriginal = rb.transform.localScale,
                                distanciaInicial = dirHaciaPiedra.magnitude // Guardamos la distancia real 3D
                            };
                            piedrasEnSuccion.Add(rb, datos);
                        }
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

            if (rb == null || !rb.gameObject.activeInHierarchy)
            {
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
                rb.linearVelocity = direccionNormalizada * datos.tuberia.velocidadSuccion;

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
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
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

        StartCoroutine(CrecerPiedra(rb.transform, piedra.escalaOriginal));
    }

    IEnumerator CrecerPiedra(Transform t, Vector3 escalaFinal)
    {
        float tiempoRecorrido = 0f;

        while (tiempoRecorrido < tiempoCrecimiento)
        {
            if (t == null) yield break;

            tiempoRecorrido += Time.deltaTime;
            float progreso = tiempoRecorrido / tiempoCrecimiento;

            t.localScale = Vector3.Lerp(Vector3.zero, escalaFinal, progreso);
            yield return null;
        }

        if (t != null) t.localScale = escalaFinal;
    }

    // ==========================================
    // GIZMOS (Dibuja la porción de pizza en 3D)
    // ==========================================
    void OnDrawGizmosSelected()
    {
        foreach (var tuberia in tuberiasAbajo)
        {
            if (tuberia.puntoAbsorcion != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.4f);

                Vector3 origenAbajo = tuberia.puntoAbsorcion.position;
                Vector3 origenArriba = origenAbajo + tuberia.puntoAbsorcion.up * tuberia.alturaAbsorcion;
                Vector3 forward = tuberia.puntoAbsorcion.forward;
                Vector3 up = tuberia.puntoAbsorcion.up;

                float radio = tuberia.radioAbsorcion;
                float angulo = tuberia.anguloAbsorcion;

                // Dibujar arcos curvos (Base y Techo)
                DibujarArcoPizza(origenAbajo, forward, up, radio, angulo);
                DibujarArcoPizza(origenArriba, forward, up, radio, angulo);

                // Calcular vectores de los bordes izquierdo y derecho
                Quaternion rotIzquierda = Quaternion.AngleAxis(-angulo / 2f, up);
                Quaternion rotDerecha = Quaternion.AngleAxis(angulo / 2f, up);
                Vector3 bordeIzquierdo = rotIzquierda * forward;
                Vector3 bordeDerecho = rotDerecha * forward;

                // Líneas rectas de la base plana
                Gizmos.DrawLine(origenAbajo, origenAbajo + bordeIzquierdo * radio);
                Gizmos.DrawLine(origenAbajo, origenAbajo + bordeDerecho * radio);

                // Líneas rectas del techo
                Gizmos.DrawLine(origenArriba, origenArriba + bordeIzquierdo * radio);
                Gizmos.DrawLine(origenArriba, origenArriba + bordeDerecho * radio);

                // Pilares conectores (Líneas verticales)
                Gizmos.DrawLine(origenAbajo, origenArriba); // Centro
                Gizmos.DrawLine(origenAbajo + bordeIzquierdo * radio, origenArriba + bordeIzquierdo * radio); // Esquina Izquierda
                Gizmos.DrawLine(origenAbajo + bordeDerecho * radio, origenArriba + bordeDerecho * radio); // Esquina Derecha
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
        // Adaptar la cantidad de segmentos según lo grande que sea el ángulo
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