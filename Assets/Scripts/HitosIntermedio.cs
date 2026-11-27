using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detecta lo que pasa en la partida y se lo cuenta a Botro (BotroHabla.Lanzar): esferas perfectas, compras,
/// piedras metidas, minuto de partida, pocas piedras y fin del tutorial.
/// QUÉ dice Botro y en cuál de estos momentos está en GuionBotro (Tools > PiedrasIncr > Botro: qué dice y cuándo).
/// Además hace los efectos que no son de Botro: la diana parpadea con pocas piedras y la luna reacciona al Arma Lunar.
/// </summary>
public class HitosIntermedio : MonoBehaviour
{
    // Lo que dice Botro y cuándo: GuionBotro (Tools > PiedrasIncr > Botro: qué dice y cuándo).

    [Header("Faltan piedras")]
    [Tooltip("Con este número de piedras (o menos) en el mundo, Botro avisa.")]
    [Min(0)] public int umbralPocasPiedras = 2;
    [Tooltip("No avisa hasta haber metido este número de piedras en el agujero (para no saltar al empezar).")]
    [Min(0)] public int piedrasTragadasAntesDeAvisar = 6;
    [Tooltip("Vacío = el LED de la antena de la diana (ConstruccionDianaBloopMonedas).")]
    public ControladorNeon neonDiana;
    public int parpadeosDiana = 8;
    public float intensidadParpadeo = 4f;

    [Header("Arma Lunar")]
    [Tooltip("Vacío = se busca en la escena.")]
    public GestorFasesLunares gestorLuna;

    private readonly HashSet<string> dichos = new HashSet<string>();
    private int piedrasTragadas;
    private int ultimoMinuto;

    private void OnEnable()
    {
        DesgastePiedra.OnAlgunaEsferaPerfecta += AlConseguirEsfera;
        EventosEconomia.OnCompraRealizada += AlComprar;
        AgujeroSimple.OnPiedraTragada += AlTragar;
        SecuenciaIntro.OnTutorialTerminado += AlTerminarTutorial;
    }

    private void OnDisable()
    {
        DesgastePiedra.OnAlgunaEsferaPerfecta -= AlConseguirEsfera;
        EventosEconomia.OnCompraRealizada -= AlComprar;
        AgujeroSimple.OnPiedraTragada -= AlTragar;
        SecuenciaIntro.OnTutorialTerminado -= AlTerminarTutorial;
    }

    private void Update()
    {
        int minuto = Mathf.FloorToInt(Time.timeSinceLevelLoad / 60f);
        if (minuto <= ultimoMinuto) return;
        ultimoMinuto = minuto;
        BotroHabla.Lanzar(GuionBotro.Cuando.MinutoDePartida, null, minuto);
    }

    private void AlTerminarTutorial()
    {
        BotroHabla.Lanzar(GuionBotro.Cuando.TutorialTerminado);
    }

    private void AlConseguirEsfera(DesgastePiedra piedra)
    {
        BotroHabla.Lanzar(GuionBotro.Cuando.EsferaPerfecta);
    }

    private void AlComprar(string id, int nivel, int coste)
    {
        BotroHabla.Lanzar(GuionBotro.Cuando.Compra, id, nivel);

        // Arma Lunar: la luna reacciona (sin explicación de Botro, salvo que se le ponga un momento).
        if (id == EconomiaDemo.Ids.ArmaLunar && dichos.Add("arma_lunar"))
        {
            if (gestorLuna == null) gestorLuna = FindFirstObjectByType<GestorFasesLunares>();
            if (gestorLuna != null) gestorLuna.ReaccionarSinAvanzar();
        }
    }

    private void AlTragar(AgujeroSimple agujero, float pureza, float gasolina)
    {
        piedrasTragadas++;
        BotroHabla.Lanzar(GuionBotro.Cuando.PiedraMetida, null, piedrasTragadas);
        if (dichos.Contains("faltan_piedras") || piedrasTragadas < piedrasTragadasAntesDeAvisar) return;
        if (!SecuenciaIntro.TutorialTerminado) return;

        // La piedra recién tragada aún cuenta hasta el final del frame: se comprueba en el siguiente.
        StartCoroutine(ComprobarPocasPiedras());
    }

    private IEnumerator ComprobarPocasPiedras()
    {
        yield return null;
        if (GestorPiedras.Instancia == null || dichos.Contains("faltan_piedras")) yield break;
        if (GestorPiedras.Instancia.CantidadPiedras > umbralPocasPiedras) yield break;

        dichos.Add("faltan_piedras");
        BotroHabla.Lanzar(GuionBotro.Cuando.QuedanPocasPiedras);
        yield return ParpadearDiana();
    }

    private IEnumerator ParpadearDiana()
    {
        if (neonDiana == null)
        {
            ConstruccionDianaBloopMonedas diana = FindFirstObjectByType<ConstruccionDianaBloopMonedas>();
            if (diana != null) neonDiana = diana.ledAntena;
        }
        if (neonDiana == null) yield break;

        float original = neonDiana.intensidad;
        for (int i = 0; i < parpadeosDiana; i++)
        {
            neonDiana.intensidad = intensidadParpadeo;
            yield return new WaitForSeconds(0.25f);
            neonDiana.intensidad = 0f;
            yield return new WaitForSeconds(0.2f);
        }
        neonDiana.intensidad = original;
    }
}
