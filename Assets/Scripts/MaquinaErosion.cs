using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaquinaErosion : MonoBehaviour
{
    [Header("Requisitos de Entrada")]
    [Tooltip("Porcentaje mínimo de desgaste que debe tener la piedra para ser aceptada")]
    [Range(0f, 100f)]
    public float porcentajeMinimoRequerido = 50f;

    [Header("Punto y Fuerza de Rechazo (Conos Rojos)")]
    public Transform puntoRechazo;
    public Vector3 direccionRechazo = new Vector3(0f, 1f, -1f);
    public float fuerzaMinimaRechazo = 10f;
    public float fuerzaMaximaRechazo = 20f;
    [Range(0f, 90f)] public float anguloDispersionRechazo = 15f;

    [Header("Punto y Fuerza de Salida (Conos Verdes)")]
    public Transform puntoEntrada;
    public Transform puntoSalida;
    public Vector3 direccionSalida = new Vector3(0f, 1f, 1f);
    public float fuerzaMinimaExpulsion = 10f;
    public float fuerzaMaximaExpulsion = 20f;
    [Range(0f, 90f)] public float anguloDispersionSalida = 15f;

    [Header("Procesamiento Interno")]
    public float tiempoProcesamiento = 3f;
    public float velocidadDePulido = 0.05f;

    [Header("Desbloqueo Procesadora")]
    [SerializeField] private bool procesadoraDesbloqueada = false;

    public bool ProcesadoraDesbloqueada => procesadoraDesbloqueada;


    [Header("Mejora - Velocidad de Procesado")]
    [SerializeField, Range(1, 3)]
    private int nivelProcesado = 1;

    public float tiempoProcesadoNivel1 = 3f;
    public float tiempoProcesadoNivel2 = 2f;
    public float tiempoProcesadoNivel3 = 1f;

    public int NivelProcesado => nivelProcesado;

    [Header("Efectos Visuales (Partículas)")]
    public ParticleSystem[] particulasTrabajando;
    public ParticleSystem[] particulasExpulsion;

    // --- NUEVO: Diccionario para guardar la piedra y su % inicial, y Array para las emisiones base ---
    private Dictionary<Rigidbody, float> piedrasEnProceso = new Dictionary<Rigidbody, float>();
    private float[] emisionesOriginales;

    private void OnValidate()
    {
        nivelProcesado = Mathf.Clamp(nivelProcesado, 1, 3);

        AplicarNivelProcesado();
    }

    void Start()
    {
        AplicarNivelProcesado();
        // Guardamos la tasa de emisión original de cada sistema de partículas al empezar
        emisionesOriginales = new float[particulasTrabajando.Length];
        for (int i = 0; i < particulasTrabajando.Length; i++)
        {
            if (particulasTrabajando[i] != null)
            {
                emisionesOriginales[i] = particulasTrabajando[i].emission.rateOverTimeMultiplier;
            }
        }
    }

    public void RecibirPiedra(Collider otro)
    {
        if (otro.CompareTag("Piedra"))
        {
            Rigidbody rb = otro.GetComponent<Rigidbody>();
            DeformacionPiedra deformacion = otro.GetComponent<DeformacionPiedra>();

            if (rb != null && deformacion != null && !piedrasEnProceso.ContainsKey(rb))
            {
                float porcentajeActual = deformacion.ObtenerPorcentajeDesgasteHaciaEsfera();

                if (porcentajeActual >= porcentajeMinimoRequerido)
                {
                    StartCoroutine(RutinaProcesarPiedra(rb, deformacion, porcentajeActual));
                }
                else
                {
                    RechazarPiedra(rb);
                }
            }
        }
    }

    private Vector3 ObtenerDireccionConAngulo(Vector3 direccionBase, float anguloApertura)
    {
        if (anguloApertura <= 0f) return direccionBase;
        return Quaternion.AngleAxis(Random.Range(0f, anguloApertura), Random.onUnitSphere) * direccionBase;
    }

    void RechazarPiedra(Rigidbody rb)
    {
        if (puntoRechazo != null)
        {
            rb.position = puntoRechazo.position;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 direccionBase = puntoRechazo.TransformDirection(direccionRechazo.normalized);
            Vector3 direccionFinal = ObtenerDireccionConAngulo(direccionBase, anguloDispersionRechazo);

            float fuerzaAleatoria = Random.Range(fuerzaMinimaRechazo, fuerzaMaximaRechazo);
            rb.AddForce(direccionFinal * fuerzaAleatoria, ForceMode.Impulse);
        }
    }

    // --- NUEVO: Función que calcula y aplica la intensidad de las partículas ---
    void ActualizarEmisionParticulas()
    {
        // Limpiamos nulos por si alguna piedra se destruyó externamente
        List<Rigidbody> clavesEliminar = new List<Rigidbody>();
        foreach (var rb in piedrasEnProceso.Keys)
        {
            if (rb == null) clavesEliminar.Add(rb);
        }
        foreach (var rb in clavesEliminar) piedrasEnProceso.Remove(rb);

        // Si ya no hay piedras, apagamos la máquina
        if (piedrasEnProceso.Count == 0)
        {
            foreach (ParticleSystem ps in particulasTrabajando)
            {
                if (ps != null) ps.Stop();
            }
            return;
        }

        // Calculamos el promedio de desgaste de las piedras que están dentro
        float sumaPorcentajes = 0f;
        foreach (float pct in piedrasEnProceso.Values)
        {
            sumaPorcentajes += pct;
        }
        float promedioActual = sumaPorcentajes / piedrasEnProceso.Count;

        // MAGIA MATEMÁTICA: Si es 100% el factor es 0. Si es el mínimo requerido, el factor es 1.
        float factorEmision = Mathf.InverseLerp(100f, porcentajeMinimoRequerido, promedioActual);

        for (int i = 0; i < particulasTrabajando.Length; i++)
        {
            if (particulasTrabajando[i] != null)
            {
                var emision = particulasTrabajando[i].emission;

                // Multiplicamos la emisión original por nuestro factor
                emision.rateOverTimeMultiplier = emisionesOriginales[i] * factorEmision;

                if (!particulasTrabajando[i].isPlaying && factorEmision > 0f)
                {
                    particulasTrabajando[i].Play();
                }
            }
        }
    }

    IEnumerator RutinaProcesarPiedra(Rigidbody rb, DeformacionPiedra deformacion, float porcentajeInicial)
    {
        // Añadimos la piedra al registro con su porcentaje y actualizamos el humo
        piedrasEnProceso.Add(rb, porcentajeInicial);
        ActualizarEmisionParticulas();

        rb.isKinematic = true;

        MeshRenderer[] renderizadores = rb.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer mr in renderizadores) mr.enabled = false;

        Collider[] colisionadores = rb.GetComponentsInChildren<Collider>();
        foreach (Collider col in colisionadores) col.enabled = false;

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < tiempoProcesamiento)
        {
            if (rb == null || deformacion == null)
            {
                // Si la piedra se destruye a medio camino, actualizamos partículas y salimos
                ActualizarEmisionParticulas();
                yield break;
            }

            tiempoTranscurrido += Time.deltaTime;
            float progreso = tiempoTranscurrido / tiempoProcesamiento;

            rb.position = Vector3.Lerp(puntoEntrada.position, puntoSalida.position, progreso);
            rb.rotation = Quaternion.Euler(
                Mathf.Lerp(0, 360f, progreso * 5f),
                Mathf.Lerp(0, 360f, progreso * 3f),
                0f
            );

            deformacion.PulirUniformemente(velocidadDePulido * Time.deltaTime);

            yield return null;
        }

        if (deformacion != null) deformacion.PulirUniformemente(10f);

        if (rb != null)
        {
            rb.position = puntoSalida.position;
            rb.isKinematic = false;

            foreach (MeshRenderer mr in renderizadores) if (mr != null) mr.enabled = true;
            foreach (Collider col in colisionadores) if (col != null) col.enabled = true;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 direccionBase = puntoSalida.TransformDirection(direccionSalida.normalized);
            Vector3 direccionFinal = ObtenerDireccionConAngulo(direccionBase, anguloDispersionSalida);

            float fuerzaAleatoria = Random.Range(fuerzaMinimaExpulsion, fuerzaMaximaExpulsion);
            rb.AddForce(direccionFinal * fuerzaAleatoria, ForceMode.Impulse);

            // Eliminamos la piedra del registro y recalculamos el humo
            piedrasEnProceso.Remove(rb);
            ActualizarEmisionParticulas();

            foreach (ParticleSystem ps in particulasExpulsion)
            {
                if (ps != null) ps.Play();
            }
        }
    }


    public void DesbloquearProcesadora()
    {
        procesadoraDesbloqueada = true;

        gameObject.SetActive(true);

        Debug.Log("¡Procesadora desbloqueada!");
    }


    public bool MejorarProcesado()
    {
        if (nivelProcesado >= 3)
            return false;

        nivelProcesado++;

        AplicarNivelProcesado();

        Debug.Log(
            "Procesadora mejorada a nivel " +
            nivelProcesado +
            " | Tiempo: " +
            tiempoProcesamiento +
            " segundos"
        );

        return true;
    }


    private void AplicarNivelProcesado()
    {
        switch (nivelProcesado)
        {
            case 1:
                tiempoProcesamiento = tiempoProcesadoNivel1;
                break;

            case 2:
                tiempoProcesamiento = tiempoProcesadoNivel2;
                break;

            case 3:
                tiempoProcesamiento = tiempoProcesadoNivel3;
                break;
        }
    }


    public bool ProcesadoAlMaximo()
    {
        return nivelProcesado >= 3;
    }
    // ==========================================
    // SISTEMA DE GIZMOS VISUALES EN EL EDITOR
    // ==========================================
    void OnDrawGizmos()
    {
        if (puntoRechazo != null && direccionRechazo != Vector3.zero)
        {
            Vector3 dirRealRechazo = puntoRechazo.TransformDirection(direccionRechazo.normalized);

            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            DibujarConoGizmo(puntoRechazo.position, dirRealRechazo, fuerzaMinimaRechazo, anguloDispersionRechazo);

            Gizmos.color = Color.red;
            DibujarConoGizmo(puntoRechazo.position, dirRealRechazo, fuerzaMaximaRechazo, anguloDispersionRechazo);
        }

        if (puntoSalida != null && direccionSalida != Vector3.zero)
        {
            Vector3 dirRealSalida = puntoSalida.TransformDirection(direccionSalida.normalized);

            Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
            DibujarConoGizmo(puntoSalida.position, dirRealSalida, fuerzaMinimaExpulsion, anguloDispersionSalida);

            Gizmos.color = Color.green;
            DibujarConoGizmo(puntoSalida.position, dirRealSalida, fuerzaMaximaExpulsion, anguloDispersionSalida);
        }
    }

    void DibujarConoGizmo(Vector3 origen, Vector3 direccionCentral, float fuerza, float angulo)
    {
        float tamañoVisual = Mathf.Clamp(fuerza * 0.15f, 0.5f, 7f);
        Vector3 finCentral = origen + (direccionCentral * tamañoVisual);

        Gizmos.DrawLine(origen, finCentral);

        if (angulo > 0f)
        {
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
}