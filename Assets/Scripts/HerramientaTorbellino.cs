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
    [Header("Modelo")]
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

    [Tooltip("Sistemas de partículas que se activan al usar la herramienta pero NO cambian su posición ni rotación")]
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

    [Header("Mejora - Radio del Torbellino")]
    [SerializeField, Range(1, 3)]
    private int nivelRadio = 1;

    [Header("Radio de Atracci�n")]
    public float radioAtraccionNivel1 = 8f;
    public float radioAtraccionNivel2 = 12f;
    public float radioAtraccionNivel3 = 18f;

    [Header("Radio del Ojo")]
    public float radioOjoNivel1 = 1.5f;
    public float radioOjoNivel2 = 2.25f;
    public float radioOjoNivel3 = 3.4f;


    [Header("Mejora - Alcance del Torbellino")]
    [SerializeField, Range(1, 3)]
    private int nivelAlcance = 1;

    public float alcanceNivel1 = 20f;
    public float alcanceNivel2 = 30f;
    public float alcanceNivel3 = 45f;

    [Header("Mejora - Movilidad del Torbellino")]
    [SerializeField, Range(1, 3)]
    private int nivelMovilidad = 1;

    [Range(0.1f, 1f)]
    public float movilidadNivel1 = 0.50f;

    [Range(0.1f, 1f)]
    public float movilidadNivel2 = 0.75f;

    [Range(0.1f, 1f)]
    public float movilidadNivel3 = 1.00f;

    [SerializeField]
    private float multiplicadorMovilidadActual = 0.50f;

    public int NivelMovilidad => nivelMovilidad;

    public int NivelAlcance => nivelAlcance;
    public int NivelRadio => nivelRadio;
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


    private void OnValidate()
    {
        nivelRadio = Mathf.Clamp(nivelRadio, 1, 3);
        nivelAlcance = Mathf.Clamp(nivelAlcance, 1, 3);
        nivelMovilidad = Mathf.Clamp(nivelMovilidad, 1, 3);

        AplicarNivelRadio();
        AplicarNivelAlcance();
        AplicarNivelMovilidad();
    }
    void Start()
    {
        AplicarNivelRadio();
        AplicarNivelAlcance();
        AplicarNivelMovilidad();
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

    // --- NUEVO: Función pública llamada por GestorEquipamiento.cs ---
    public void SetEquipada(bool estado)
    {
        if (equipada == estado) return;
        equipada = estado;

        if (modeloHerramienta != null)
        {
            modeloHerramienta.SetActive(equipada);

            if (equipada)
            {
                modeloHerramienta.transform.localPosition = posicionInicialModelo;
                modeloHerramienta.transform.localRotation = rotacionInicialModelo;
                temporizadorBobbing = 0f;
                intensidadGiroActual = 0f;

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

    void Update()
    {
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
        if (scriptMovimiento != null)
        {
            scriptMovimiento.velocidad =
                velocidadOriginalMovimiento * multiplicadorMovilidadActual;
        }

        if (scriptCamara != null)
        {
            scriptCamara.sensibilidadRaton =
                sensibilidadOriginalCamara * multiplicadorMovilidadActual;
        }

        penalizacionAplicada = true;
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

    public bool MejorarRadio()
    {
        if (nivelRadio >= 3)
        {
            return false;
        }

        nivelRadio++;

        AplicarNivelRadio();

        Debug.Log(
            "Radio del Torbellino mejorado a nivel " +
            nivelRadio +
            " | Radio: " +
            radioAtraccion
        );

        return true;
    }

    private void AplicarNivelRadio()
    {
        switch (nivelRadio)
        {
            case 1:
                radioAtraccion = radioAtraccionNivel1;
                radioOjoTornado = radioOjoNivel1;
                break;

            case 2:
                radioAtraccion = radioAtraccionNivel2;
                radioOjoTornado = radioOjoNivel2;
                break;

            case 3:
                radioAtraccion = radioAtraccionNivel3;
                radioOjoTornado = radioOjoNivel3;
                break;
        }

        Debug.Log(
            "Torbellino Nivel " + nivelRadio +
            " | Radio Atracci�n: " + radioAtraccion +
            " | Radio Ojo: " + radioOjoTornado
        );
    }

    public bool RadioAlMaximo()
    {
        return nivelRadio >= 3;
    }

    public bool MejorarAlcance()
    {
        if (nivelAlcance >= 3)
        {
            return false;
        }

        nivelAlcance++;

        AplicarNivelAlcance();

        Debug.Log(
            "Alcance del Torbellino mejorado a nivel " +
            nivelAlcance +
            " | Alcance: " +
            alcanceMaximo
        );

        return true;
    }

    private void AplicarNivelAlcance()
    {
        switch (nivelAlcance)
        {
            case 1:
                alcanceMaximo = alcanceNivel1;
                break;

            case 2:
                alcanceMaximo = alcanceNivel2;
                break;

            case 3:
                alcanceMaximo = alcanceNivel3;
                break;
        }
    }

    public bool MejorarMovilidad()
    {
        if (nivelMovilidad >= 3)
            return false;

        nivelMovilidad++;

        AplicarNivelMovilidad();

        Debug.Log(
            "Movilidad del Torbellino mejorada a nivel " +
            nivelMovilidad +
            " | Multiplicador: " +
            multiplicadorMovilidadActual
        );

        return true;
    }

    private void AplicarNivelMovilidad()
    {
        switch (nivelMovilidad)
        {
            case 1:
                multiplicadorMovilidadActual = movilidadNivel1;
                break;

            case 2:
                multiplicadorMovilidadActual = movilidadNivel2;
                break;

            case 3:
                multiplicadorMovilidadActual = movilidadNivel3;
                break;
        }
    }

    public bool MovilidadAlMaximo()
    {
        return nivelMovilidad >= 3;
    }

    public bool AlcanceAlMaximo()
    {
        return nivelAlcance >= 3;
    }
}

