using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Tools > PiedrasIncr > Fuente Digital-7: crear y poner en todo el juego.
///  1. Crea (una vez) la fuente TMP "Digital-7 SDF" a partir de Assets/Fonts/Digital-7/digital-7.ttf,
///     con LiberationSans de respaldo para lo que Digital-7 no tiene (Ñ, tildes, ¡ ¿).
///  2. La pone en el tema de la UI (TemaUINeon): menús, tienda holográfica, HUD con OnGUI...
///  3. En las escenas abiertas cambia a Digital-7 los textos que usaban DS-DIGI (y los campos de fuente
///     de los HUD). En las escenas sin tema (el menú principal) cambia todos los textos.
/// Se puede deshacer con Ctrl+Z (salvo la creación del asset). No guarda las escenas: guárdalas tú.
/// </summary>
public static class FuenteDigital7
{
    private const string RutaTTF = "Assets/Fonts/Digital-7/digital-7.ttf";
    private const string RutaAsset = "Assets/Fonts/Digital-7/Digital-7 SDF.asset";
    private const string RutaRespaldo = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string NombreFuenteVieja = "DS-DIGI";

    // Lo que se mete en el atlas desde el principio (el resto se añade solo al usarse).
    private const string CaracteresIniciales =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

    [MenuItem("Tools/PiedrasIncr/Fuente Digital-7: crear y poner en todo el juego")]
    private static void CrearYAplicar()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Fuente Digital-7", "Sal del modo Play y vuelve a intentarlo.", "OK");
            return;
        }

        StringBuilder informe = new StringBuilder("[FUENTE DIGITAL-7]\n");

        Font ttf = AssetDatabase.LoadAssetAtPath<Font>(RutaTTF);
        if (ttf == null)
        {
            EditorUtility.DisplayDialog("Fuente Digital-7", "No encuentro " + RutaTTF, "OK");
            return;
        }
        PrepararImportadorTTF();

        TMP_FontAsset fuente = CrearAssetSiFalta(ttf, informe);
        if (fuente == null) return;

        AplicarAlTema(fuente, ttf, informe);

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene escena = SceneManager.GetSceneAt(i);
            if (escena.isLoaded) AplicarEnEscena(escena, fuente, ttf, informe);
        }

        AssetDatabase.SaveAssets();
        Debug.Log(informe.ToString());
        EditorUtility.DisplayDialog("Fuente Digital-7",
            "Hecho. Revisa el informe de la consola y guarda las escenas que hayan cambiado (Ctrl+S).\n\n" +
            "Para el menú principal: ábrelo y vuelve a ejecutar esta herramienta.", "OK");
    }

    // La fuente para los HUD de OnGUI: si falta un carácter, que tire de Arial en vez de dejar un hueco.
    private static void PrepararImportadorTTF()
    {
        TrueTypeFontImporter importador = AssetImporter.GetAtPath(RutaTTF) as TrueTypeFontImporter;
        if (importador == null) return;

        bool cambia = importador.fontTextureCase != FontTextureCase.Dynamic ||
                      importador.fontNames == null || importador.fontNames.Length < 2;
        if (!cambia) return;

        importador.fontTextureCase = FontTextureCase.Dynamic;
        importador.includeFontData = true;
        importador.fontNames = new[] { "Digital-7", "Arial", "Liberation Sans" };
        importador.SaveAndReimport();
    }

    private static TMP_FontAsset CrearAssetSiFalta(Font ttf, StringBuilder informe)
    {
        TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RutaAsset);
        if (fuente == null)
        {
            fuente = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                                                   AtlasPopulationMode.Dynamic, true);
            if (fuente == null)
            {
                EditorUtility.DisplayDialog("Fuente Digital-7", "TextMeshPro no ha podido crear la fuente.", "OK");
                return null;
            }

            fuente.name = "Digital-7 SDF";
            AssetDatabase.CreateAsset(fuente, RutaAsset);

            // El atlas y el material van dentro del asset, como en las fuentes que crea TMP.
            if (fuente.atlasTextures != null && fuente.atlasTextures.Length > 0 && fuente.atlasTextures[0] != null)
            {
                fuente.atlasTextures[0].name = "Digital-7 SDF Atlas";
                AssetDatabase.AddObjectToAsset(fuente.atlasTextures[0], fuente);
            }
            if (fuente.material != null)
            {
                fuente.material.name = "Digital-7 SDF Material";
                AssetDatabase.AddObjectToAsset(fuente.material, fuente);
            }
            informe.AppendLine("· Creada la fuente TMP " + RutaAsset + ".");
        }

        // Respaldo para lo que Digital-7 no tiene (Ñ, tildes, ¡ ¿, ·, →).
        TMP_FontAsset respaldo = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RutaRespaldo);
        if (respaldo != null)
        {
            if (fuente.fallbackFontAssetTable == null) fuente.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (!fuente.fallbackFontAssetTable.Contains(respaldo))
            {
                fuente.fallbackFontAssetTable.Add(respaldo);
                informe.AppendLine("· Respaldo: LiberationSans SDF (para Ñ, tildes, ¡ ¿).");
            }
        }
        else
        {
            informe.AppendLine("  ! No encuentro LiberationSans SDF para el respaldo: la Ñ y las tildes saldrán como huecos.");
        }

        fuente.TryAddCharacters(CaracteresIniciales, out string faltan);

        // Tildes, Ñ, ¡ ¿: se dibujan como la letra normal (Digital-7 no las tiene).
        int tildes = SinTildes.PrepararFuente(fuente);
        if (tildes > 0) informe.AppendLine("· " + tildes + " letras con tilde (y Ñ, ¡ ¿) se dibujan como la letra normal.");

        EditorUtility.SetDirty(fuente);
        return fuente;
    }

    private static void AplicarAlTema(TMP_FontAsset fuente, Font ttf, StringBuilder informe)
    {
        TemaUINeon tema = Resources.Load<TemaUINeon>(TemaUINeon.RutaResources);
        if (tema == null)
        {
            informe.AppendLine("  ! No encuentro el TemaUINeon en Resources.");
            return;
        }

        Undo.RecordObject(tema, "Fuente Digital-7 en el tema");
        tema.fuenteTMP = fuente;
        tema.fuenteIMGUI = ttf;
        EditorUtility.SetDirty(tema);
        informe.AppendLine("· Tema de la UI (TemaUINeon): fuente TMP y fuente de los HUD → Digital-7.");
    }

    private static void AplicarEnEscena(Scene escena, TMP_FontAsset fuente, Font ttf, StringBuilder informe)
    {
        TemaUINeon tema = TemaUINeon.Actual;
        bool sinTema = tema != null && tema.escenasSinTema != null && tema.escenasSinTema.Contains(escena.name);
        int textos = 0, campos = 0;

        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            // Textos TMP.
            foreach (TMP_Text texto in raiz.GetComponentsInChildren<TMP_Text>(true))
            {
                if (texto.font == fuente) continue;
                bool esVieja = texto.font != null && texto.font.name.Contains(NombreFuenteVieja);
                if (!esVieja && !sinTema) continue;

                Undo.RecordObject(texto, "Fuente Digital-7");
                texto.font = fuente;
                texto.fontSharedMaterial = fuente.material;
                EditorUtility.SetDirty(texto);
                textos++;
            }

            // Campos de fuente (Font o TMP_FontAsset) de los scripts que apuntaban a DS-DIGI.
            foreach (MonoBehaviour script in raiz.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (script == null || script is TMP_Text) continue;
                SerializedObject so = new SerializedObject(script);
                SerializedProperty p = so.GetIterator();
                bool tocado = false;
                while (p.Next(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference || p.objectReferenceValue == null) continue;
                    Object valor = p.objectReferenceValue;
                    if (!valor.name.Contains(NombreFuenteVieja)) continue;

                    if (valor is Font) { p.objectReferenceValue = ttf; tocado = true; campos++; }
                    else if (valor is TMP_FontAsset) { p.objectReferenceValue = fuente; tocado = true; campos++; }
                }
                if (tocado) so.ApplyModifiedProperties();
            }
        }

        if (textos > 0 || campos > 0)
        {
            EditorSceneManager.MarkSceneDirty(escena);
            informe.AppendLine("· Escena '" + escena.name + "': " + textos + " textos y " + campos + " campos de fuente → Digital-7" +
                               (sinTema ? " (escena sin tema: se cambian todos los textos)." : "."));
        }
        else
        {
            informe.AppendLine("· Escena '" + escena.name + "': nada que cambiar.");
        }
    }
}
