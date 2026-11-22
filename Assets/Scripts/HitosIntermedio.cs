using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// INTERMEDIO: una línea corta de Botro la primera vez que pasa cada hito (hoja INTERMEDIO, minuto aprox.).
/// Todo por eventos de F1–F6, sin temporizadores:
///   primera esfera perfecta (2:00) · Fusil Monedas (2:54) · Torbellino (4:48) · primeras Paredes (5:06)
///   faltan piedras → la diana parpadea y Botro sugiere su mejora (7:12) · primera Cinta (9:30)
///   Arma Lunar → la luna se resiente con temblor y sonido raro, SIN explicación (13:36)
///   Fisionador "0 / 8" (27:30) · misil listo (29:42)
/// Cada hito se dice una sola vez por partida.
/// </summary>
public class HitosIntermedio : MonoBehaviour
{
    [Header("Líneas de Botro")]
    [TextArea(1, 3)] public string lineaEsferaPerfecta = "¡Una esfera perfecta! Esas son las que de verdad valen.";
    [TextArea(1, 3)] public string lineaFusil = "¿Un fusil de monedas? Dispárale monedas a las máquinas y pagarás desde lejos.";
    [TextArea(1, 3)] public string lineaTorbellino = "Con el torbellino mueves un montón de piedras a la vez. Úsalo con cabeza.";
    [TextArea(1, 3)] public string lineaParedes = "Unas paredes bien puestas y las piedras irán justo a donde quieras.";
    [TextArea(1, 3)] public string lineaFaltanPiedras = "Te estás quedando sin piedras... Mira la diana: si la mejoras, caerán más.";
    [TextArea(1, 3)] public string lineaCinta = "¡Una cinta! Deja que trabaje ella mientras tú vas a por más.";
    [TextArea(1, 3)] public string lineaFisionador = "El Fisionador ya está en marcha. Ocho esferas perfectas y tendrás algo muy gordo.";
    [TextArea(1, 3)] public string lineaMisil = "¡El misil está listo! Llévalo al lanzador. Ya sabes a dónde apuntar.";

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

    private void OnEnable()
    {
        DesgastePiedra.OnAlgunaEsferaPerfecta += AlConseguirEsfera;
        EventosEconomia.OnCompraRealizada += AlComprar;
        AgujeroSimple.OnPiedraTragada += AlTragar;
    }

    private void OnDisable()
    {
        DesgastePiedra.OnAlgunaEsferaPerfecta -= AlConseguirEsfera;
        EventosEconomia.OnCompraRealizada -= AlComprar;
        AgujeroSimple.OnPiedraTragada -= AlTragar;
    }

    private void Decir(string id, string texto)
    {
        if (!dichos.Add(id)) return;
        BotroDice.Decir("hito_" + id, texto);
    }

    private void AlConseguirEsfera(DesgastePiedra piedra)
    {
        Decir("esfera", lineaEsferaPerfecta);
    }

    private void AlComprar(string id, int nivel, int coste)
    {
        switch (id)
        {
            case EconomiaDemo.Ids.FusilMonedas: Decir("fusil", lineaFusil); break;
            case EconomiaDemo.Ids.Torbellino: Decir("torbellino", lineaTorbellino); break;
            case EconomiaDemo.Ids.Pared: Decir("paredes", lineaParedes); break;
            case EconomiaDemo.Ids.Cinta: Decir("cinta", lineaCinta); break;
            case EconomiaDemo.Ids.Fisionador: Decir("fisionador", lineaFisionador); break;
            case EconomiaDemo.Ids.MisilGrande: Decir("misil", lineaMisil); break;
            case EconomiaDemo.Ids.ArmaLunar:
                // Sin explicación: solo la luna reacciona.
                if (dichos.Add("arma_lunar"))
                {
                    if (gestorLuna == null) gestorLuna = FindFirstObjectByType<GestorFasesLunares>();
                    if (gestorLuna != null) gestorLuna.ReaccionarSinAvanzar();
                }
                break;
        }
    }

    private void AlTragar(AgujeroSimple agujero, float pureza, float gasolina)
    {
        piedrasTragadas++;
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

        Decir("faltan_piedras", lineaFaltanPiedras);
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
