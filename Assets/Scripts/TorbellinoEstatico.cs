using UnityEngine;
using System.Collections.Generic;

public class TorbellinoEstatico : MonoBehaviour
{
    [Header("Área de Efecto (Cilindro)")]
    [Tooltip("El radio del torbellino")]
    public float radioAtraccion = 15f;
    [Tooltip("El radio del anillo interior donde orbitarán las piedras")]
    public float radioOjoTornado = 3f;
    [Tooltip("La altura máxima hasta la que llega la succión")]
    public float alturaTorbellino = 10f;

    [Header("Físicas del Vórtice")]
    [Tooltip("Velocidad a la que giran las piedras")]
    public float velocidadRotacion = 25f;
    [Tooltip("Velocidad constante a la que las piedras son empujadas hacia el suelo")]
    public float velocidadDescenso = 5f;

    [Header("Desgaste")]
    [Tooltip("Multiplicador de erosión de la piedra mientras está atrapada (0.5 = mitad de desgaste)")]
    public float multiplicadorErosionTornado = 0.5f;
    public string tagPiedra = "Piedra";

    // Registro de piedras actualmente atrapadas
    private HashSet<Rigidbody> piedrasAtrapadas = new HashSet<Rigidbody>();

    void FixedUpdate()
    {
        ActualizarTorbellino();
    }

    void ActualizarTorbellino()
    {
        // 1. Definimos la forma del cilindro (Cápsula matemática)
        Vector3 puntoInferior = transform.position;
        Vector3 puntoSuperior = transform.position + (Vector3.up * alturaTorbellino);

        // 2. Buscamos qué hay dentro del área en este fotograma
        Collider[] objetosDetectados = Physics.OverlapCapsule(puntoInferior, puntoSuperior, radioAtraccion);
        HashSet<Rigidbody> piedrasEnEsteFrame = new HashSet<Rigidbody>();

        foreach (Collider col in objetosDetectados)
        {
            if (col.CompareTag(tagPiedra))
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    piedrasEnEsteFrame.Add(rb);

                    // Si es una piedra nueva que acaba de entrar, la registramos y le bajamos la erosión
                    if (!piedrasAtrapadas.Contains(rb))
                    {
                        piedrasAtrapadas.Add(rb);
                        DeformacionPiedra deformacion = rb.GetComponent<DeformacionPiedra>();
                        if (deformacion != null) deformacion.multiplicadorErosion = multiplicadorErosionTornado;
                    }
                }
            }
        }

        // 3. Revisamos si alguna piedra se ha escapado (salió del cilindro por arriba, abajo, o la cogió el jugador)
        List<Rigidbody> piedrasFugadas = new List<Rigidbody>();
        foreach (Rigidbody rb in piedrasAtrapadas)
        {
            if (rb == null || !rb.gameObject.activeInHierarchy || !piedrasEnEsteFrame.Contains(rb))
            {
                piedrasFugadas.Add(rb);
            }
        }

        // Devolvemos la erosión original a las que escaparon
        foreach (Rigidbody rb in piedrasFugadas)
        {
            if (rb != null)
            {
                DeformacionPiedra deformacion = rb.GetComponent<DeformacionPiedra>();
                if (deformacion != null) deformacion.multiplicadorErosion = 1f;
            }
            piedrasAtrapadas.Remove(rb);
        }

        // 4. Aplicamos las Físicas de Torbellino a las que siguen atrapadas
        foreach (Rigidbody rb in piedrasAtrapadas)
        {
            // Cálculos planos (ignorando altura)
            Vector3 posicionPlana = new Vector3(rb.position.x, 0f, rb.position.z);
            Vector3 centroPlano = new Vector3(transform.position.x, 0f, transform.position.z);

            Vector3 direccionAlCentro = centroPlano - posicionPlana;
            float distancia = direccionAlCentro.magnitude;

            if (distancia < 0.1f) direccionAlCentro = Vector3.forward;
            direccionAlCentro.Normalize();

            // Muro invisible HORIZONTAL (No pueden escapar por los lados)
            if (distancia > radioAtraccion)
            {
                Vector3 posCorregida = centroPlano - (direccionAlCentro * radioAtraccion);
                rb.MovePosition(new Vector3(posCorregida.x, rb.position.y, posCorregida.z));
                distancia = radioAtraccion;
            }

            // Cálculo de fuerzas
            float errorDistancia = distancia - radioOjoTornado;
            float velocidadRadial = errorDistancia * 10f; // Atracción hacia el anillo

            Vector3 vectorAtraccion = direccionAlCentro * velocidadRadial;
            Vector3 direccionGiro = Vector3.Cross(direccionAlCentro, Vector3.up).normalized;
            Vector3 vectorGiro = direccionGiro * velocidadRotacion;

            Vector3 velocidadDeseada = vectorAtraccion + vectorGiro;

            // DICTADURA FÍSICA: Movimiento perfecto en X y Z, y forzamos una caída constante en Y
            rb.linearVelocity = new Vector3(velocidadDeseada.x, -velocidadDescenso, velocidadDeseada.z);
        }
    }

    // ==========================================
    // GIZMOS PARA VER EL CILINDRO EN EL EDITOR
    // ==========================================
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // Color Cyan semitransparente

        Vector3 centroAbajo = transform.position;
        Vector3 centroArriba = transform.position + (Vector3.up * alturaTorbellino);

        // Dibujar Disco Inferior (Suelo)
        DibujarCirculoGizmo(centroAbajo, radioAtraccion);

        // Dibujar Disco Superior (Techo del tornado)
        DibujarCirculoGizmo(centroArriba, radioAtraccion);

        // Dibujar Ojo del Tornado (Rojo)
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        DibujarCirculoGizmo(centroAbajo, radioOjoTornado);

        // Líneas verticales para conectar el cilindro visualmente
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawLine(centroAbajo + Vector3.right * radioAtraccion, centroArriba + Vector3.right * radioAtraccion);
        Gizmos.DrawLine(centroAbajo + Vector3.left * radioAtraccion, centroArriba + Vector3.left * radioAtraccion);
        Gizmos.DrawLine(centroAbajo + Vector3.forward * radioAtraccion, centroArriba + Vector3.forward * radioAtraccion);
        Gizmos.DrawLine(centroAbajo + Vector3.back * radioAtraccion, centroArriba + Vector3.back * radioAtraccion);
    }

    void DibujarCirculoGizmo(Vector3 centro, float radio)
    {
        int segmentos = 36;
        float angulo = 0f;
        float paso = 360f / segmentos;

        Vector3 puntoAnterior = centro + new Vector3(Mathf.Sin(0) * radio, 0, Mathf.Cos(0) * radio);

        for (int i = 1; i <= segmentos; i++)
        {
            angulo += paso;
            Vector3 puntoActual = centro + new Vector3(Mathf.Sin(angulo * Mathf.Deg2Rad) * radio, 0, Mathf.Cos(angulo * Mathf.Deg2Rad) * radio);
            Gizmos.DrawLine(puntoAnterior, puntoActual);
            puntoAnterior = puntoActual;
        }
    }
}