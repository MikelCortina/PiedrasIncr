using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct PiezaGiratoria
{
    [Tooltip("La parte del modelo que va a girar")]
    public Transform objeto;
    [Tooltip("Velocidad máxima en el eje Z para esta pieza en concreto")]
    public float velocidadMaximaZ;
}

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

    [Header("Efectos Visuales (Giro con Inercia)")]
    [Tooltip("Lista de piezas del modelo que girarán, cada una con su propia velocidad")]
    public PiezaGiratoria[] piezasGiratorias;

    public float aceleracionGiro = 5f;
    public float desaceleracionGiro = 2f;
    private float intensidadGiroActual = 0f;

    [Header("Raycast y Apuntado")]
    public LayerMask capaSuelo;
    public float alcanceMaximo = 100f;

    [Tooltip("Sistema de partículas que aparecerá en el punto donde toca el tornado (Se mueve y rota)")]
    public ParticleSystem particulasTorbellino;

    [Tooltip("Sistemas de partículas que se activan al usar la herramienta pero NO cambian su posición ni rotación (ej: humo en el motor del arma)")]
    public ParticleSystem[] particulasEstaticas;

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
    private Vector3 normalActualTorbellino = Vector3.up;

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

        if (particulasTorbellino != null)
        {
            particulasTorbellino.transform.SetParent(null);
            if (!particulasTorbellino.isPlaying) particulasTorbellino.Play(true);
            particulasTorbellino.Clear(true);
        }

        if (particulasEstaticas != null)
        {
            foreach (ParticleSystem ps in particulasEstaticas)
            {
                if (ps != null)
                {
                    if (!ps.isPlaying) ps.Play(true);
                    ps.Clear(true);
                }
            }
        }

        ControlarEmisionParticulas(false);

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
                    intensidadGiroActual = 0f;

                    // --- SOLUCIÓN: Despertar las partículas estáticas al sacar el arma ---
                    if (particulasEstaticas != null)
                    {
                        foreach (ParticleSystem ps in particulasEstaticas)
                        {
                            if (ps != null && !ps.isPlaying) ps.Play(true);
                        }
                    }
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
            ManejarGiroObjetos();
        }

        if (equipada && Input.GetMouseButton(0))
        {
            Ray ray = camaraPrincipal.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));

            bool puntoValidoEncontrado = false;
            Vector3 nuevoPuntoTorbellino = posicionActualTorbellino;
            Vector3 nuevaNormalTorbellino = normalActualTorbellino;

            if (Physics.Raycast(ray, out RaycastHit hit, 2000f, capaSuelo))
            {
                if (hit.distance <= alcanceMaximo)
                {
                    nuevoPuntoTorbellino = hit.point;
                    nuevaNormalTorbellino = hit.normal;
                    puntoValidoEncontrado = true;
                }
                else
                {
                    Vector3 puntoLimiteAire = ray.GetPoint(alcanceMaximo);
                    if (Physics.Raycast(puntoLimiteAire + (Vector3.up * 50f), Vector3.down, out RaycastHit hitAbajo, 150f, capaSuelo))
                    {
                        nuevoPuntoTorbellino = hitAbajo.point;
                        nuevaNormalTorbellino = hitAbajo.normal;
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
                    nuevaNormalTorbellino = hitAbajo.normal;
                    puntoValidoEncontrado = true;
                }
                else if (torbellinoActivo) puntoValidoEncontrado = true;
            }

            if (puntoValidoEncontrado)
            {
                if (!torbellinoActivo) posicionAnteriorTorbellino = nuevoPuntoTorbellino;

                posicionActualTorbellino = nuevoPuntoTorbellino;
                normalActualTorbellino = nuevaNormalTorbellino;
                torbellinoActivo = true;

                if (particulasTorbellino != null)
                {
                    particulasTorbellino.transform.position = posicionActualTorbellino;
                    particulasTorbellino.transform.up = normalActualTorbellino;
                }

                ControlarEmisionParticulas(true);
                DibujarCirculo(posicionActualTorbellino, normalActualTorbellino);
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

    void ManejarGiroObjetos()
    {
        if (piezasGiratorias == null || piezasGiratorias.Length == 0) return;

        if (torbellinoActivo && Input.GetMouseButton(0))
        {
            intensidadGiroActual = Mathf.Lerp(intensidadGiroActual, 1f, Time.deltaTime * aceleracionGiro);
        }
        else
        {
            intensidadGiroActual = Mathf.Lerp(intensidadGiroActual, 0f, Time.deltaTime * desaceleracionGiro);
        }

        if (intensidadGiroActual > 0.01f)
        {
            foreach (PiezaGiratoria pieza in piezasGiratorias)
            {
                if (pieza.objeto != null)
                {
                    float velocidadActual = pieza.velocidadMaximaZ * intensidadGiroActual;
                    pieza.objeto.Rotate(0f, 0f, velocidadActual * Time.deltaTime, Space.Self);
                }
            }
        }
    }

    void ControlarEmisionParticulas(bool activar)
    {
        if (particulasTorbellino != null)
        {
            ParticleSystem[] sistemasHijos = particulasTorbellino.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in sistemasHijos)
            {
                // --- SOLUCIÓN: Si vamos a emitir y estaba apagado, lo arrancamos ---
                if (activar && !ps.isPlaying) ps.Play(false);

                var emision = ps.emission;
                if (emision.enabled != activar)
                {
                    emision.enabled = activar;
                }
            }
        }

        if (particulasEstaticas != null)
        {
            foreach (ParticleSystem psEstatica in particulasEstaticas)
            {
                if (psEstatica != null)
                {
                    ParticleSystem[] estaticasHijas = psEstatica.GetComponentsInChildren<ParticleSystem>();
                    foreach (ParticleSystem psHija in estaticasHijas)
                    {
                        // --- SOLUCIÓN: Hacemos lo mismo con todos los hijos estáticos ---
                        if (activar && !psHija.isPlaying) psHija.Play(false);

                        var emision = psHija.emission;
                        if (emision.enabled != activar)
                        {
                            emision.enabled = activar;
                        }
                    }
                }
            }
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

        ControlarEmisionParticulas(false);

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

    void DibujarCirculo(Vector3 centro, Vector3 normal)
    {
        if (circuloAreaVisual == null) return;
        if (!circuloAreaVisual.gameObject.activeSelf) circuloAreaVisual.gameObject.SetActive(true);

        float angulo = 0f;
        float pasoAngulo = 360f / segmentosCirculo;

        Quaternion rotacionPlano = Quaternion.FromToRotation(Vector3.up, normal);

        for (int i = 0; i < (segmentosCirculo + 1); i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angulo) * radioAtraccion;
            float z = Mathf.Cos(Mathf.Deg2Rad * angulo) * radioAtraccion;

            Vector3 posicionRelativa = new Vector3(x, 0.2f, z);
            Vector3 posicionRotada = rotacionPlano * posicionRelativa;

            circuloAreaVisual.SetPosition(i, centro + posicionRotada);
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