using System;
using UnityEngine;

[DisallowMultipleComponent]
public class DesgastePiedra : MonoBehaviour
{
    [Header("Estado (0 = piedra nueva, 1 = esfera perfecta)")]
    [SerializeField, Range(0f, 1f)] private float desgaste;
    [SerializeField] private bool esferaPerfectaAlcanzada;

    [Header("Desgaste por distancia")]
    [Tooltip("Por debajo de esta velocidad la piedra no cuenta como rodando o arrastrada.")]
    [Min(0f)] public float velocidadMinimaRodando = 0.3f;
    [Tooltip("Segundos que se sigue considerando 'en contacto' tras el último contacto con suelo o cinta.")]
    [Min(0f)] public float margenContacto = 0.1f;
    [Tooltip("Saltos mayores en un solo paso de física se ignoran (teletransportes, rescates).")]
    [Min(0.1f)] public float maximoMetrosPorPaso = 3f;

    public event Action<float, float> OnDesgasteCambiado;
    public event Action OnEsferaPerfecta;
    public event Action<float> OnDesgasteRestaurado;

    public static event Action<DesgastePiedra> OnAlgunaEsferaPerfecta;
    public static event Action<DesgastePiedra, float, float> OnAlgunDesgasteCambiado;

    public float Desgaste { get { return desgaste; } }
    public bool EsEsferaPerfecta { get { return desgaste >= 1f; } }
    public bool EsferaPerfectaAlcanzada { get { return esferaPerfectaAlcanzada; } }
    public float MetrosContados { get; private set; }

    public static int PatadasMedidas { get; private set; }
    public static float MediaMetrosPorPatada { get { return PatadasMedidas > 0 ? metrosTotalesPatadas / PatadasMedidas : 0f; } }
    public static float MediaDesgastePorPatada { get { return PatadasMedidas > 0 ? desgasteTotalPatadas / PatadasMedidas : 0f; } }
    private static float metrosTotalesPatadas;
    private static float desgasteTotalPatadas;

    private Rigidbody rb;
    private Vector3 posicionAnterior;
    private bool tienePosicionAnterior;
    private float ultimoContacto = -999f;
    private float enTorbellinoHasta = -999f;
    private bool midiendoPatada;
    private float metrosPatada;
    private float desgasteInicioPatada;
    private float tiempoQuieta;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReiniciarEventos()
    {
        OnAlgunaEsferaPerfecta = null;
        OnAlgunDesgasteCambiado = null;
        PatadasMedidas = 0;
        metrosTotalesPatadas = 0f;
        desgasteTotalPatadas = 0f;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        tienePosicionAnterior = false;
    }

    public void AnadirDesgaste(float cantidad)
    {
        if (cantidad <= 0f || desgaste >= 1f) return;
        float antes = desgaste;
        desgaste = Mathf.Min(1f, desgaste + cantidad);
        if (desgaste > 0.99999f) desgaste = 1f;
        Notificar(antes);
    }

    public void AplicarGolpePatada()
    {
        if (midiendoPatada) TerminarMedicionPatada();
        midiendoPatada = true;
        metrosPatada = 0f;
        tiempoQuieta = 0f;
        desgasteInicioPatada = desgaste;
        AnadirDesgaste(ProveedorEconomia.Actual.golpePatada);
    }

    private void TerminarMedicionPatada()
    {
        midiendoPatada = false;
        PatadasMedidas++;
        metrosTotalesPatadas += metrosPatada;
        desgasteTotalPatadas += desgaste - desgasteInicioPatada;
        if (Debug.isDebugBuild && PatadasMedidas % 10 == 0)
        {
            Debug.Log("DesgastePiedra: " + PatadasMedidas + " patadas medidas · media " + MediaMetrosPorPatada.ToString("0.0") + " m rodando · " + (MediaDesgastePorPatada * 100f).ToString("0.00") + " % de desgaste por patada (Excel: 20 m · 2,5 %).");
        }
    }

    public void AplicarTorbellino(float deltaTime)
    {
        enTorbellinoHasta = Time.fixedTime + Mathf.Max(margenContacto, Time.fixedDeltaTime * 2f);
        AnadirDesgaste(ProveedorEconomia.Actual.desgasteTorbellinoPorSegundo * deltaTime);
    }

    public void Restaurar(float valor, bool esferaYaPremiada)
    {
        desgaste = Mathf.Clamp01(valor);
        esferaPerfectaAlcanzada = esferaYaPremiada || desgaste >= 1f;
        midiendoPatada = false;
        if (OnDesgasteRestaurado != null) OnDesgasteRestaurado(desgaste);
    }

    private void Notificar(float antes)
    {
        if (Mathf.Approximately(antes, desgaste)) return;

        if (OnDesgasteCambiado != null) OnDesgasteCambiado(antes, desgaste);
        if (OnAlgunDesgasteCambiado != null) OnAlgunDesgasteCambiado(this, antes, desgaste);

        if (desgaste >= 1f && !esferaPerfectaAlcanzada)
        {
            esferaPerfectaAlcanzada = true;
            if (OnEsferaPerfecta != null) OnEsferaPerfecta();
            if (OnAlgunaEsferaPerfecta != null) OnAlgunaEsferaPerfecta(this);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null || rb.isKinematic)
        {
            tienePosicionAnterior = false;
            if (midiendoPatada) TerminarMedicionPatada();
            return;
        }

        if (midiendoPatada)
        {
            tiempoQuieta = rb.linearVelocity.magnitude < velocidadMinimaRodando ? tiempoQuieta + Time.fixedDeltaTime : 0f;
            if (tiempoQuieta > 0.5f || Time.fixedTime <= enTorbellinoHasta) TerminarMedicionPatada();
        }

        Vector3 posicion = rb.position;
        if (!tienePosicionAnterior)
        {
            posicionAnterior = posicion;
            tienePosicionAnterior = true;
            return;
        }

        float metros = Vector3.Distance(posicion, posicionAnterior);
        posicionAnterior = posicion;

        if (desgaste >= 1f) return;
        if (Time.fixedTime <= enTorbellinoHasta) return;
        if (Time.fixedTime - ultimoContacto > margenContacto) return;
        if (metros > maximoMetrosPorPaso) return;
        if (rb.linearVelocity.magnitude < velocidadMinimaRodando) return;

        MetrosContados += metros;
        if (midiendoPatada) metrosPatada += metros;
        AnadirDesgaste(ProveedorEconomia.Actual.desgastePorMetro * metros);
    }

    private void OnCollisionStay(Collision colision)
    {
        RegistrarContacto(colision);
    }

    private void OnCollisionEnter(Collision colision)
    {
        RegistrarContacto(colision);
    }

    private void RegistrarContacto(Collision colision)
    {
        if (colision.gameObject.CompareTag("Player")) return;
        ultimoContacto = Time.fixedTime;
    }
}
