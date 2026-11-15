
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HologramaTrayectoria : MonoBehaviour
{
    [Header("Referencias")]
    public MaquinaPiedras maquina;

    [Header("Trayectoria")]
    [Min(0.001f)] public float masaPiedra = 1f;
    [Min(2)] public int segmentos = 60;
    [Min(0.01f)] public float intervalo = 0.05f;
    [Min(0.001f)] public float grosor = 0.06f;

    [Header("Colisiones")]
    public LayerMask capasImpacto;
    public QueryTriggerInteraction detectarTriggers =
        QueryTriggerInteraction.Ignore;

    [Header("Marcador de impacto")]
    [Min(0.01f)] public float radioMarcador = 0.4f;
    [Min(8)] public int segmentosMarcador = 48;
    [Min(0.001f)] public float grosorMarcador = 0.045f;
    [Min(0f)] public float separacionSuperficie = 0.03f;

    [Header("Animacion holografica")]
    [Min(0f)] public float velocidadPulso = 3f;
    [Range(0f, 1f)] public float intensidadPulso = 0.2f;
    [Min(0f)] public float velocidadEnergia = 2f;
    [Range(0f, 1f)] public float intensidadEnergia = 0.35f;

    [Header("Colores")]
    public Color colorHolograma =
        new Color(0f, 0.85f, 1f, 0.8f);

    [Header("Visibilidad")]
    [Min(0f)] public float duracionVisible = 1.5f;
    [Min(0f)] public float sensibilidadFuerza = 0.001f;
    [Min(0f)] public float sensibilidadRotacion = 0.05f;
    [Min(0f)] public float sensibilidadPosicion = 0.001f;

    private LineRenderer linea;
    private LineRenderer marcador;

    private float tiempoRestante;
    private float fuerzaAnterior;
    private Quaternion rotacionAnterior;
    private Vector3 posicionAnterior;

    private bool visible;
    private bool inicializado;

    void Awake()
    {
        linea = GetComponent<LineRenderer>();
        linea.useWorldSpace = true;
        linea.positionCount = 0;
        linea.enabled = false;
        linea.startWidth = grosor;
        linea.endWidth = grosor;
        linea.startColor = colorHolograma;
        linea.endColor = colorHolograma;

        CrearMarcador();
    }

    void Update()
    {
        if (maquina == null || maquina.puntoLanzamiento == null)
        {
            Ocultar();
            inicializado = false;
            return;
        }

        Transform punto = maquina.puntoLanzamiento;

        if (!inicializado)
        {
            fuerzaAnterior = maquina.fuerzaLanzamiento;
            rotacionAnterior = punto.rotation;
            posicionAnterior = punto.position;
            inicializado = true;
            return;
        }

        bool cambioFuerza =
            Mathf.Abs(maquina.fuerzaLanzamiento - fuerzaAnterior)
            > sensibilidadFuerza;

        bool cambioRotacion =
            Quaternion.Angle(punto.rotation, rotacionAnterior)
            > sensibilidadRotacion;

        bool cambioPosicion =
            Vector3.Distance(punto.position, posicionAnterior)
            > sensibilidadPosicion;

        if (cambioFuerza || cambioRotacion || cambioPosicion)
            NotificarCambioParametros();

        fuerzaAnterior = maquina.fuerzaLanzamiento;
        rotacionAnterior = punto.rotation;
        posicionAnterior = punto.position;

        if (!visible)
            return;

        DibujarTrayectoria();
        AnimarHolograma();

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante <= 0f)
            Ocultar();
    }

    void CrearMarcador()
    {
        GameObject objeto = new GameObject("MarcadorImpacto");
        objeto.transform.SetParent(transform, false);

        marcador = objeto.AddComponent<LineRenderer>();
        marcador.useWorldSpace = true;
        marcador.loop = true;
        marcador.positionCount = 0;
        marcador.startWidth = grosorMarcador;
        marcador.endWidth = grosorMarcador;
        marcador.alignment = LineAlignment.View;
        marcador.textureMode = LineTextureMode.Stretch;
        marcador.sharedMaterial = linea.sharedMaterial;
        marcador.startColor = colorHolograma;
        marcador.endColor = colorHolograma;
        marcador.enabled = false;
    }

    public void NotificarCambioParametros()
    {
        tiempoRestante = duracionVisible;
        visible = true;
        linea.enabled = true;
    }

    public void Mostrar()
    {
        NotificarCambioParametros();
    }

    public void Ocultar()
    {
        visible = false;

        if (linea != null)
        {
            linea.enabled = false;
            linea.positionCount = 0;
        }

        if (marcador != null)
        {
            marcador.enabled = false;
            marcador.positionCount = 0;
        }
    }

    void DibujarTrayectoria()
    {
        Vector3 origen = maquina.puntoLanzamiento.position;

        Vector3 velocidadInicial =
            maquina.puntoLanzamiento.forward *
            (maquina.fuerzaLanzamiento / masaPiedra);

        Vector3 gravedad = Physics.gravity;

        linea.positionCount = segmentos;

        Vector3 puntoAnterior = origen;
        linea.SetPosition(0, origen);

        int puntosDibujados = 1;
        bool hayImpacto = false;
        RaycastHit impactoFinal = default;

        for (int i = 1; i < segmentos; i++)
        {
            float tiempo = i * intervalo;

            Vector3 puntoActual =
                origen +
                velocidadInicial * tiempo +
                0.5f * gravedad * tiempo * tiempo;

            Vector3 desplazamiento = puntoActual - puntoAnterior;
            float distancia = desplazamiento.magnitude;

            if (distancia > 0.0001f &&
                Physics.Raycast(
                    puntoAnterior,
                    desplazamiento / distancia,
                    out RaycastHit impacto,
                    distancia,
                    capasImpacto,
                    detectarTriggers))
            {
                linea.SetPosition(puntosDibujados, impacto.point);
                puntosDibujados++;

                impactoFinal = impacto;
                hayImpacto = true;
                break;
            }

            linea.SetPosition(puntosDibujados, puntoActual);
            puntosDibujados++;

            puntoAnterior = puntoActual;
        }

        linea.positionCount = puntosDibujados;

        if (hayImpacto)
            DibujarMarcador(impactoFinal.point, impactoFinal.normal);
        else
            marcador.enabled = false;
    }

    void DibujarMarcador(Vector3 posicion, Vector3 normal)
    {
        marcador.enabled = true;
        marcador.loop = true;
        marcador.positionCount = segmentosMarcador;

        Vector3 centro =
            posicion + normal * separacionSuperficie;

        Vector3 ejeX =
            Vector3.ProjectOnPlane(Vector3.right, normal);

        if (ejeX.sqrMagnitude < 0.001f)
            ejeX = Vector3.ProjectOnPlane(Vector3.forward, normal);

        ejeX.Normalize();

        Vector3 ejeY =
            Vector3.Cross(normal, ejeX).normalized;

        for (int i = 0; i < segmentosMarcador; i++)
        {
            float angulo =
                i * Mathf.PI * 2f / segmentosMarcador;

            Vector3 punto =
                centro +
                ejeX * Mathf.Cos(angulo) * radioMarcador +
                ejeY * Mathf.Sin(angulo) * radioMarcador;

            marcador.SetPosition(i, punto);
        }
    }

    void AnimarHolograma()
    {
        float pulso =
            1f +
            Mathf.Sin(Time.time * velocidadPulso) *
            intensidadPulso;

        linea.startWidth = grosor * pulso;
        linea.endWidth = grosor * pulso;

        marcador.startWidth = grosorMarcador * pulso;
        marcador.endWidth = grosorMarcador * pulso;

        float energia =
            1f - intensidadEnergia +
            (0.5f + 0.5f *
            Mathf.Sin(Time.time * velocidadEnergia * 6f)) *
            intensidadEnergia;

        float desvanecimiento =
            duracionVisible > 0f
                ? Mathf.Clamp01(tiempoRestante /
                    Mathf.Min(duracionVisible, 0.4f))
                : 1f;

        Color colorActual = colorHolograma;
        colorActual.a *= energia * desvanecimiento;

        linea.startColor = colorActual;
        linea.endColor = colorActual;

        if (marcador != null)
        {
            marcador.startColor = colorActual;
            marcador.endColor = colorActual;
        }
    }
}
