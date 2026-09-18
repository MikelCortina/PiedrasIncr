using UnityEngine;
using System.Collections.Generic;

public class HerramientaTorbellino : MonoBehaviour
{
    [Header("Controles y Modelo")]
    public KeyCode teclaEquipar = KeyCode.Alpha4;
    public bool equipada = false;
    public GameObject modeloHerramienta;

    [Header("Animación del Modelo (Sway & Bobbing)")]
    public float intensidadSway = 0.02f;
    public float limiteSway = 0.06f;
    public float intensidadInclinacion = 2f;
    public float velocidadBobbing = 10f;
    public float intensidadBobbing = 0.015f;
    public float suavidadAnimacion = 8f;

    [Header("Raycast y Apuntado")]
    public LayerMask capaSuelo;
    public float alcanceMaximo = 100f;
    public Transform efectoVisualTorbellino;

    [Header("Visualización del Área")]
    public LineRenderer circuloAreaVisual;
    public int segmentosCirculo = 50;

    [Header("Físicas Estables del Vórtice")]
    public float radioAtraccion = 15f;
    public float radioOjoTornado = 3f;
    public float velocidadRotacion = 25f;

    public float multiplicadorErosionTornado = 0.5f;
    public string tagPiedra = "Piedra";

    [Header("Efectos en el Jugador")]
    public MovimientoPersonaje scriptMovimiento;
    public CamaraPrimeraPersona scriptCamara;

    private Camera camaraPrincipal;

    private bool torbellinoActivo = false;
    private Vector3 posicionActualTorbellino;
    private Vector3 posicionAnteriorTorbellino;
    private Vector3 velocidadTraslacionTornado;
    private HashSet<Rigidbody> piedrasAtrapadas = new HashSet<Rigidbody>();

    private float velocidadOriginalMovimiento;
    private float sensibilidadOriginalCamara;
    private bool penalizacionAplicada = false;

    private Vector3 posicionInicialModelo;
    private Quaternion rotacionInicialModelo;
    private float temporizadorBobbing = 0f;

    void Start()
    {
        camaraPrincipal = Camera.main;

        if (modeloHerramienta != null)
        {
            posicionInicialModelo = modeloHerramienta.transform.localPosition;
            rotacionInicialModelo = modeloHerramienta.transform.localRotation;
            modeloHerramienta.SetActive(equipada);
        }

        if (efectoVisualTorbellino != null) efectoVisualTorbellino.gameObject.SetActive(false);
        if (circuloAreaVisual != null)
        {
            circuloAreaVisual.positionCount = segmentosCirculo + 1;
            circuloAreaVisual.useWorldSpace = true;
            circuloAreaVisual.gameObject.SetActive(false);
        }

        if (scriptMovimiento != null) velocidadOriginalMovimiento = scriptMovimiento.velocidad;
        if (scriptCamara != null) sensibilidadOriginalCamara = scriptCamara.sensibilidadRaton;
    }

    void Update()
    {
        if (Input.GetKeyDown(teclaEquipar))
        {
            equipada = !equipada;

            if (modeloHerramienta != null)
            {
                modeloHerramienta.SetActive(equipada);

                if (equipada)
                {
                    modeloHerramienta.transform.localPosition = posicionInicialModelo;
                    modeloHerramienta.transform.localRotation = rotacionInicialModelo;
                    temporizadorBobbing = 0f;
                }
            }

            if (!equipada)
            {
                ApagarTorbellino();
            }
        }

        if (equipada)
        {
            ActualizarAnimacionModelo();
        }

        if (equipada && Input.GetMouseButton(0))
        {
            Ray ray = camaraPrincipal.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

            bool puntoValidoEncontrado = false;
            Vector3 nuevoPuntoTorbellino = posicionActualTorbellino;

            if (Physics.Raycast(ray, out RaycastHit hit, 2000f, capaSuelo))
            {
                if (hit.distance <= alcanceMaximo)
                {
                    nuevoPuntoTorbellino = hit.point;
                    puntoValidoEncontrado = true;
                }
                else
                {
                    Vector3 puntoLimiteAire = ray.GetPoint(alcanceMaximo);
                    if (Physics.Raycast(puntoLimiteAire + (Vector3.up * 50f), Vector3.down, out RaycastHit hitAbajo, 150f, capaSuelo))
                    {
                        nuevoPuntoTorbellino = hitAbajo.point;
                        puntoValidoEncontrado = true;
                    }
                    else if (torbellinoActivo) puntoValidoEncontrado = true;
                }
            }
            else
            {
                Vector3 puntoLimiteAire = ray.GetPoint(alcanceMaximo);
                if (Physics.Raycast(puntoLimiteAire, Vector3.down, out RaycastHit hitAbajo, 200f, capaSuelo))
                {
                    nuevoPuntoTorbellino = hitAbajo.point;
                    puntoValidoEncontrado = true;
                }
                else if (torbellinoActivo) puntoValidoEncontrado = true;
            }

            if (puntoValidoEncontrado)
            {
                if (!torbellinoActivo) posicionAnteriorTorbellino = nuevoPuntoTorbellino;

                posicionActualTorbellino = nuevoPuntoTorbellino;
                torbellinoActivo = true;

                if (efectoVisualTorbellino != null)
                {
                    if (!efectoVisualTorbellino.gameObject.activeSelf) efectoVisualTorbellino.gameObject.SetActive(true);
                    efectoVisualTorbellino.position = posicionActualTorbellino;
                }
                DibujarCirculo(posicionActualTorbellino);

                AplicarPenalizacionAlJugador();
            }
            else
            {
                ApagarTorbellino();
            }
        }
        else
        {
            ApagarTorbellino();
        }
    }

    void ActualizarAnimacionModelo()
    {
        if (modeloHerramienta == null) return;

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

        modeloHerramienta.transform.localPosition = Vector3.Lerp(modeloHerramienta.transform.localPosition, posicionObjetivo, Time.deltaTime * suavidadAnimacion);
        modeloHerramienta.transform.localRotation = Quaternion.Slerp(modeloHerramienta.transform.localRotation, rotacionObjetivo, Time.deltaTime * suavidadAnimacion);
    }

    void AplicarPenalizacionAlJugador()
    {
        if (!penalizacionAplicada)
        {
            if (scriptMovimiento != null) scriptMovimiento.velocidad = velocidadOriginalMovimiento / 2f;
            if (scriptCamara != null) scriptCamara.sensibilidadRaton = sensibilidadOriginalCamara / 2f;
            penalizacionAplicada = true;
        }
    }

    void RestaurarValoresJugador()
    {
        if (penalizacionAplicada)
        {
            if (scriptMovimiento != null) scriptMovimiento.velocidad = velocidadOriginalMovimiento;
            if (scriptCamara != null) scriptCamara.sensibilidadRaton = sensibilidadOriginalCamara;
            penalizacionAplicada = false;
        }
    }

    void ApagarTorbellino()
    {
        foreach (Rigidbody rb in piedrasAtrapadas)
        {
            if (rb != null)
            {
                DeformacionPiedra deformacion = rb.GetComponent<DeformacionPiedra>();
                if (deformacion != null) deformacion.multiplicadorErosion = 1f;
            }
        }

        torbellinoActivo = false;
        piedrasAtrapadas.Clear();
        velocidadTraslacionTornado = Vector3.zero;

        if (efectoVisualTorbellino != null) efectoVisualTorbellino.gameObject.SetActive(false);
        if (circuloAreaVisual != null) circuloAreaVisual.gameObject.SetActive(false);

        RestaurarValoresJugador();
    }

    void FixedUpdate()
    {
        if (torbellinoActivo)
        {
            velocidadTraslacionTornado = (posicionActualTorbellino - posicionAnteriorTorbellino) / Time.fixedDeltaTime;
            posicionAnteriorTorbellino = posicionActualTorbellino;

            ActualizarPiedras(posicionActualTorbellino);
        }
    }

    void DibujarCirculo(Vector3 centro)
    {
        if (circuloAreaVisual == null) return;
        if (!circuloAreaVisual.gameObject.activeSelf) circuloAreaVisual.gameObject.SetActive(true);

        float angulo = 0f;
        float pasoAngulo = 360f / segmentosCirculo;

        for (int i = 0; i < (segmentosCirculo + 1); i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angulo) * radioAtraccion;
            float z = Mathf.Cos(Mathf.Deg2Rad * angulo) * radioAtraccion;
            circuloAreaVisual.SetPosition(i, centro + new Vector3(x, 0.2f, z));
            angulo += pasoAngulo;
        }
    }

    void ActualizarPiedras(Vector3 centroTorbellino)
    {
        Collider[] objetosDetectados = Physics.OverlapSphere(centroTorbellino, radioAtraccion);
        foreach (Collider col in objetosDetectados)
        {
            if (col.CompareTag(tagPiedra))
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null && !piedrasAtrapadas.Contains(rb))
                {
                    piedrasAtrapadas.Add(rb);

                    DeformacionPiedra deformacion = rb.GetComponent<DeformacionPiedra>();
                    if (deformacion != null)
                    {
                        // --- NUEVO: Despertamos la piedra (le quitamos el IsKinematic y reseteamos el antijitter) ---
                        deformacion.Despertar();
                        deformacion.multiplicadorErosion = multiplicadorErosionTornado;
                    }
                }
            }
        }

        piedrasAtrapadas.RemoveWhere(rb => rb == null || !rb.gameObject.activeInHierarchy);

        Vector3 velTornadoPlana = new Vector3(velocidadTraslacionTornado.x, 0f, velocidadTraslacionTornado.z);

        foreach (Rigidbody rb in piedrasAtrapadas)
        {
            Vector3 posicionPlana = new Vector3(rb.position.x, 0f, rb.position.z);
            Vector3 centroPlano = new Vector3(centroTorbellino.x, 0f, centroTorbellino.z);

            Vector3 direccionAlCentro = centroPlano - posicionPlana;
            float distancia = direccionAlCentro.magnitude;

            if (distancia < 0.1f) direccionAlCentro = Vector3.forward;
            direccionAlCentro.Normalize();

            if (distancia > radioAtraccion)
            {
                Vector3 posCorregida = centroPlano - (direccionAlCentro * radioAtraccion);
                rb.MovePosition(new Vector3(posCorregida.x, rb.position.y, posCorregida.z));
                distancia = radioAtraccion;
            }

            float errorDistancia = distancia - radioOjoTornado;
            float velocidadRadial = errorDistancia * 10f;

            Vector3 vectorAtraccion = direccionAlCentro * velocidadRadial;
            Vector3 direccionGiro = Vector3.Cross(direccionAlCentro, Vector3.up).normalized;
            Vector3 vectorGiro = direccionGiro * velocidadRotacion;

            Vector3 velocidadDeseada = vectorAtraccion + vectorGiro + velTornadoPlana;

            rb.linearVelocity = new Vector3(velocidadDeseada.x, rb.linearVelocity.y - 2f, velocidadDeseada.z);
        }
    }
}