using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// La fuente del juego (Digital-7) no tiene tildes, Ñ ni ¡ ¿. En vez de sacarlas con otra fuente,
/// se dibujan como la letra normal: "HOLOGRÁFICA" se ve "HOLOGRAFICA".
///  - Textos TMP: la fuente del tema usa el dibujo de la letra base para cada letra con tilde
///    (se hace al empezar la partida y lo guarda también la herramienta Fuente Digital-7).
///  - Textos de OnGUI (HUD): Quitar(texto) antes de dibujarlos.
/// Se desactiva con 'Quitar Tildes' en el TemaUINeon.
/// </summary>
public static class SinTildes
{
    // Letra con tilde (o sin equivalente en Digital-7) → la que se dibuja.
    private static readonly Dictionary<char, char> Equivalencias = new Dictionary<char, char>
    {
        { 'á', 'a' }, { 'é', 'e' }, { 'í', 'i' }, { 'ó', 'o' }, { 'ú', 'u' }, { 'ü', 'u' },
        { 'à', 'a' }, { 'è', 'e' }, { 'ì', 'i' }, { 'ò', 'o' }, { 'ù', 'u' },
        { 'Á', 'A' }, { 'É', 'E' }, { 'Í', 'I' }, { 'Ó', 'O' }, { 'Ú', 'U' }, { 'Ü', 'U' },
        { 'À', 'A' }, { 'È', 'E' }, { 'Ì', 'I' }, { 'Ò', 'O' }, { 'Ù', 'U' },
        { 'ñ', 'n' }, { 'Ñ', 'N' },
        { '¡', '!' }, { '¿', '?' },
        { '·', '-' }, { '→', '>' }, { '←', '<' }, { '…', '.' }
    };

    private static bool Activo
    {
        get
        {
            TemaUINeon tema = TemaUINeon.Actual;
            return tema == null || tema.quitarTildes;
        }
    }

    /// <summary>Texto sin tildes (para los HUD de OnGUI). Si está desactivado en el tema, lo devuelve igual.</summary>
    public static string Quitar(string texto)
    {
        if (string.IsNullOrEmpty(texto) || !Activo) return texto;

        StringBuilder sb = null;
        for (int i = 0; i < texto.Length; i++)
        {
            if (!Equivalencias.TryGetValue(texto[i], out char base_)) continue;
            if (sb == null) sb = new StringBuilder(texto);
            sb[i] = base_;
        }
        return sb != null ? sb.ToString() : texto;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AlEmpezar()
    {
        TemaUINeon tema = TemaUINeon.Actual;
        if (tema != null && tema.quitarTildes) PrepararFuente(tema.fuenteTMP);
    }

    /// <summary>
    /// Hace que en esta fuente cada letra con tilde use el dibujo de su letra base.
    /// Devuelve cuántas letras ha añadido (0 si ya estaban).
    /// </summary>
    public static int PrepararFuente(TMP_FontAsset fuente)
    {
        if (fuente == null) return 0;

        // Las letras base tienen que estar en la fuente para poder copiar su dibujo.
        StringBuilder bases = new StringBuilder();
        foreach (char b in Equivalencias.Values) bases.Append(b);
        fuente.TryAddCharacters(bases.ToString());

        int anadidas = 0;
        foreach (KeyValuePair<char, char> par in Equivalencias)
        {
            uint conTilde = par.Key;
            TMP_Character existente;
            if (fuente.characterLookupTable.TryGetValue(conTilde, out existente) && existente.glyph != null &&
                existente.textAsset == fuente)
                continue;

            TMP_Character letraBase;
            if (!fuente.characterLookupTable.TryGetValue(par.Value, out letraBase) || letraBase.glyph == null) continue;

            TMP_Character nueva = new TMP_Character(conTilde, fuente, letraBase.glyph);
            fuente.characterTable.Add(nueva);
            fuente.characterLookupTable[conTilde] = nueva;
            anadidas++;
        }
        return anadidas;
    }
}
