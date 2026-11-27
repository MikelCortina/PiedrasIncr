using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lanza los momentos de GuionBotro que no son de la intro (compras, esferas, minuto de partida...).
/// Uso: BotroHabla.Lanzar(GuionBotro.Cuando.Compra, EconomiaDemo.Ids.Torbellino, nivel);
/// </summary>
public static class BotroHabla
{
    private static readonly HashSet<string> dichos = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        dichos.Clear();
    }

    /// <summary>Algo ha pasado en la partida: Botro dice los momentos que encajan.</summary>
    /// <param name="compra">Compra: id de la mejora.</param>
    /// <param name="numero">Compra: nivel. PiedraMetida: número de piedra. MinutoDePartida: minuto.</param>
    public static void Lanzar(GuionBotro.Cuando cuando, string compra = null, int numero = 0)
    {
        if (cuando == GuionBotro.Cuando.PasoIntro) return; // los pasos de la intro los lleva SecuenciaIntro

        foreach (GuionBotro.Momento m in GuionBotro.Momentos())
        {
            if (m == null || !m.activo || m.Disparador != cuando || !Encaja(m, compra, numero)) continue;
            if (m.soloUnaVez && !dichos.Add(m.id)) continue;

            if (BotroDice.Instancia != null) BotroDice.Instancia.StartCoroutine(Decir(m));
            else foreach (GuionBotro.Frase f in m.frases) BotroDice.Decir("momento_" + m.id, f.texto, f.duracion);
        }
    }

    private static bool Encaja(GuionBotro.Momento m, string compra, int numero)
    {
        switch (m.Disparador)
        {
            case GuionBotro.Cuando.Compra:
                return (string.IsNullOrEmpty(m.compra) || m.compra == compra) && (m.numero <= 0 || m.numero == numero);
            case GuionBotro.Cuando.PiedraMetida:
            case GuionBotro.Cuando.MinutoDePartida:
                return Mathf.Max(1, m.numero) == numero;
            default:
                return true;
        }
    }

    private static IEnumerator Decir(GuionBotro.Momento m)
    {
        if (m.retraso > 0f) yield return new WaitForSeconds(m.retraso);
        for (int i = 0; i < m.frases.Count; i++)
        {
            GuionBotro.Frase f = m.frases[i];
            if (f == null || string.IsNullOrWhiteSpace(f.texto)) continue;
            if (f.retraso > 0f) yield return new WaitForSeconds(f.retraso);
            BotroDice.Decir("momento_" + m.id + "_" + i, f.texto, f.duracion);
        }
    }
}
