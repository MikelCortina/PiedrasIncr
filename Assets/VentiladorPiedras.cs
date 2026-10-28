using System.Collections;
using UnityEngine;

public class VentiladorPiedras : MonoBehaviour
{
    [Header("Área de Viento (Caja Cuadrada)")]
    public Vector3 dimensionesCorriente = new Vector3(4f, 4f, 15f);
    public Vector3 offsetCorriente = new Vector3(0f, 0f, 7.5f);

    [Header("Fuerza y Físicas")]
    public float fuerzaViento = 40f;
    [Tooltip("Pequeña fuerza global hacia arriba para romper la fricción del suelo")]
    public float fuerzaLevantamiento = 0.2f;

    [Tooltip("Capa de las piedras a detectar")]
    public LayerMask capaPiedras;
    public bool empujando = true;

    [Tooltip("Eje LOCAL por el que sale el viento. Ej: X=(1,0,0) | Y=(0,1,0) | Z=(0,0,1)")]
    public Vector3 ejeViento = new Vector3(0, 0, 1);

    [Header("Animación de Aspas")]
    public Transform aspasVentilador;
    public Vector3 ejeRotacionAspas = new Vector3(1f, 0f, 0f);
    public float velocidadRotacionAspas = 1000f;
    [Tooltip("Inercia al cambiar de dirección. Cuanto menor sea, más tardará en frenar y girar al revés.")]
    public float aceleracionAspas = 2f;
    [Tooltip("Marca esto si las aspas giran al revés de lo que deberían visualmente")]
    public bool invertirGiroVisual = false;

    private float velocidadActualAspas = 0f;

    [Header("Botón de Interacción (Inversión)")]
    public Transform botonFisico;
    public float distanciaHundido = 0.15f;
    public float velocidadAnimacionBoton = 10f;
    public Vector3 ejeHundimientoBoton = new Vector3(0, -1, 0);

    [Header("Efectos Visuales")]
    public ParticleSystem particulasEmpuje;
    public ParticleSystem particulasAtraccion;
    public VariacionAlbedo scriptColorIndicador;
    public Color colorEmpuje = Color.cyan;
    public Color colorAtraccion = new Color(1f, 0.4f, 0f);

    [Header("Debug")]
    public bool imprimirDebug = false;
    private float timerDebug = 0f;

    private Vector3 posBotonReposo;
    private Vector3 posBotonHundido;
    private Coroutine animacionActual;

    private void Awake()
    {
        if (botonFisico != null)
        {
            posBotonReposo = botonFisico.localPosition;
            posBotonHundido = posBotonReposo + (ejeHundimientoBoton.normalized * distanciaHundido);
        }

        // Al iniciar, forzamos que la velocidad actual coincida de golpe con la deseada
        float multiplicadorVisual = invertirGiroVisual ? -1f : 1f;
        velocidadActualAspas = velocidadRotacionAspas * (empujando ? 1f : -1f) * multiplicadorVisual;

        AplicarEstadoVisual(true);
    }

    private void Update()
    {
        if (aspasVentilador != null)
        {
            // Calculamos hacia dónde debería estar girando y a qué velocidad
            float multiplicadorVisual = invertirGiroVisual ? -1f : 1f;
            float velocidadObjetivo = velocidadRotacionAspas * (empujando ? 1f : -1f) * multiplicadorVisual;

            // Interpolación suave (Inercia) para frenar y cambiar de dirección
            velocidadActualAspas = Mathf.Lerp(velocidadActualAspas, velocidadObjetivo, Time.deltaTime * aceleracionAspas);

            // Aplicamos la rotación con la velocidad inercial
            aspasVentilador.Rotate(ejeRotacionAspas.normalized * (velocidadActualAspas * Time.deltaTime), Space.Self);
        }
    }

    private void FixedUpdate()
    {
        AplicarViento();
    }

    private void AplicarViento()
    {
        Vector3 centroMundo = transform.position + (transform.rotation * offsetCorriente);
        Vector3 halfExtents = dimensionesCorriente / 2f;

        Collider[] collidersEnViento = Physics.OverlapBox(centroMundo, halfExtents, transform.rotation, capaPiedras, QueryTriggerInteraction.Collide);

        Vector3 direccionVientoMundo = transform.TransformDirection(ejeViento.normalized);
        Vector3 direccionFinal = direccionVientoMundo * (empujando ? 1f : -1f);

        Vector3 direccionFuerza = (direccionFinal + (Vector3.up * fuerzaLevantamiento)).normalized;

        if (imprimirDebug)
        {
            timerDebug += Time.fixedDeltaTime;
            if (timerDebug > 1f)
            {
                if (collidersEnViento.Length > 0)
                {
                    Debug.Log($"[Ventilador] Empujando {collidersEnViento.Length} objetos.");
                }
                timerDebug = 0f;
            }
        }

        foreach (Collider col in collidersEnViento)
        {
            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb == null) rb = col.GetComponentInParent<Rigidbody>();

            if (rb != null)
            {
                if (rb.isKinematic)
                {
                    DeformacionPiedra piedra = rb.GetComponent<DeformacionPiedra>();
                    if (piedra == null) piedra = rb.GetComponentInParent<DeformacionPiedra>();

                    if (piedra != null) piedra.Despertar();
                    rb.isKinematic = false;
                }

                rb.AddForce(direccionFuerza * fuerzaViento, ForceMode.Acceleration);
            }
        }
    }

    public void Interaccionar()
    {
        empujando = !empujando;

        if (botonFisico != null)
        {
            if (animacionActual != null) StopCoroutine(animacionActual);
            Vector3 destino = empujando ? posBotonReposo : posBotonHundido;
            animacionActual = StartCoroutine(AnimarBoton(destino));
        }

        AplicarEstadoVisual(false);
    }

    private void AplicarEstadoVisual(bool esInicio)
    {
        if (empujando)
        {
            if (particulasAtraccion != null) ApagarParticulas(particulasAtraccion, esInicio);
            if (particulasEmpuje != null) particulasEmpuje.Play();
        }
        else
        {
            if (particulasEmpuje != null) ApagarParticulas(particulasEmpuje, esInicio);
            if (particulasAtraccion != null) particulasAtraccion.Play();
        }

        if (scriptColorIndicador != null) scriptColorIndicador.ForzarColor(empujando ? colorEmpuje : colorAtraccion);
    }

    private void ApagarParticulas(ParticleSystem ps, bool esInicio)
    {
        if (esInicio) ps.Stop();
        else ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private IEnumerator AnimarBoton(Vector3 destino)
    {
        while (Vector3.Distance(botonFisico.localPosition, destino) > 0.001f)
        {
            botonFisico.localPosition = Vector3.Lerp(botonFisico.localPosition, destino, Time.deltaTime * velocidadAnimacionBoton);
            yield return null;
        }
        botonFisico.localPosition = destino;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Color colorViento = empujando ? Color.cyan : new Color(1f, 0.5f, 0f);

        Gizmos.color = new Color(colorViento.r, colorViento.g, colorViento.b, 0.2f);
        Gizmos.DrawCube(offsetCorriente, dimensionesCorriente);

        Gizmos.color = colorViento;
        Gizmos.DrawWireCube(offsetCorriente, dimensionesCorriente);

        Vector3 direccionFlecha = (empujando ? ejeViento.normalized : -ejeViento.normalized) * (dimensionesCorriente.magnitude * 0.4f);
        Gizmos.DrawLine(offsetCorriente, offsetCorriente + direccionFlecha);
        Gizmos.DrawSphere(offsetCorriente + direccionFlecha, 0.25f);
    }
}