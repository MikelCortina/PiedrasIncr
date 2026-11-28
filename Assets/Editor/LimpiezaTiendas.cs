using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tools > PiedrasIncr > Limpiar tiendas de la escena.
/// Deja una sola interfaz por tienda en la escena abierta (se puede deshacer con Ctrl+Z):
///  - TiendaTrabajo sale del panel viejo (Canvas/PanelTienda) a un objeto propio "Tienda" y el panel viejo se borra
///    (la tienda principal se ve con TiendaHolografica).
///  - Se borra el TerminalCompraBots: las arañas se compran solo en el GeneradorAranas (panel PanelComprarBots).
///  - El botón de comprar araña pierde su llamada del Inspector (ahora la hace GeneradorAranas por código).
///  - Se borra el AvisoTienda de la tienda de edificios: todos los avisos usan el texto de la MesaTrabajo.
/// No guarda la escena: revisa el informe de la consola y guarda tú.
/// </summary>
public static class LimpiezaTiendas
{
    private const string NombreObjetoTienda = "Tienda";
    private const string NombrePanelViejo = "PanelTienda";
    private const string NombreTerminal = "TerminalCompraBots";
    private const string NombreAvisoEdificios = "AvisoTienda";
    private const string NombrePanelBots = "PanelComprarBots";

    [MenuItem("Tools/PiedrasIncr/Limpiar tiendas de la escena")]
    private static void Limpiar()
    {
        Scene escena = SceneManager.GetActiveScene();
        if (!escena.IsValid() || Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Limpiar tiendas", "Abre la escena (fuera de Play) y vuelve a intentarlo.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Limpiar tiendas");
        int grupo = Undo.GetCurrentGroup();
        StringBuilder informe = new StringBuilder("[LIMPIAR TIENDAS] " + escena.name + "\n");

        TiendaTrabajo tienda = MoverTiendaTrabajo(escena, informe);
        QuitarLlamadasCompraBot(escena, informe);
        AsignarPanelBots(escena, informe);
        Borrar(escena, NombreTerminal, false, informe);
        Borrar(escena, NombreAvisoEdificios, true, informe);

        if (tienda == null)
            informe.AppendLine("· No hay TiendaTrabajo en la escena.");

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(escena);
        Debug.Log(informe.ToString());
        EditorUtility.DisplayDialog("Limpiar tiendas", "Hecho. Revisa el informe de la consola y guarda la escena (Ctrl+S).\nCtrl+Z lo deshace.", "OK");
    }

    // ---------- TiendaTrabajo: del panel viejo a su propio objeto ----------

    private static TiendaTrabajo MoverTiendaTrabajo(Scene escena, StringBuilder informe)
    {
        TiendaTrabajo vieja = null;
        foreach (TiendaTrabajo t in Componentes<TiendaTrabajo>(escena)) { vieja = t; break; }
        if (vieja == null) return null;

        if (!(vieja.transform is RectTransform))
        {
            informe.AppendLine("· TiendaTrabajo ya está fuera de la interfaz (" + vieja.name + "): no se mueve.");
            return vieja;
        }

        GameObject panelViejo = vieja.gameObject;

        GameObject objeto = new GameObject(NombreObjetoTienda);
        SceneManager.MoveGameObjectToScene(objeto, escena);
        Undo.RegisterCreatedObjectUndo(objeto, "Crear Tienda");
        TiendaTrabajo nueva = Undo.AddComponent<TiendaTrabajo>(objeto);
        EditorUtility.CopySerialized(vieja, nueva);

        int cambiadas = CambiarReferencias(escena, vieja, nueva);
        informe.AppendLine("· TiendaTrabajo movida de '" + Ruta(panelViejo.transform) + "' al objeto '" + NombreObjetoTienda +
                           "' (" + cambiadas + " referencias actualizadas).");

        // Lo que apunte a algo de dentro del panel viejo se queda vacío al borrarlo: se avisa.
        foreach (string aviso in ReferenciasHaciaDentro(escena, panelViejo.transform))
            informe.AppendLine("  ! " + aviso);

        if (panelViejo.name != NombrePanelViejo)
            informe.AppendLine("  ! El panel viejo se llama '" + panelViejo.name + "' (se esperaba '" + NombrePanelViejo + "').");

        Undo.DestroyObjectImmediate(panelViejo);
        informe.AppendLine("· Borrado el panel viejo de la tienda (con sus botones).");
        return nueva;
    }

    private static int CambiarReferencias(Scene escena, Object vieja, Object nueva)
    {
        int cambiadas = 0;
        foreach (Component c in Componentes<Component>(escena))
        {
            if (c == null || c == vieja || c == nueva) continue;
            SerializedObject so = new SerializedObject(c);
            SerializedProperty p = so.GetIterator();
            bool tocado = false;
            while (p.Next(true))
            {
                if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == vieja)
                {
                    p.objectReferenceValue = nueva;
                    tocado = true;
                    cambiadas++;
                }
            }
            if (tocado) so.ApplyModifiedProperties();
        }
        return cambiadas;
    }

    private static IEnumerable<string> ReferenciasHaciaDentro(Scene escena, Transform raiz)
    {
        foreach (Component c in Componentes<Component>(escena))
        {
            if (c == null || c.transform.IsChildOf(raiz)) continue;
            SerializedObject so = new SerializedObject(c);
            SerializedProperty p = so.GetIterator();
            while (p.Next(true))
            {
                if (p.propertyType != SerializedPropertyType.ObjectReference || p.objectReferenceValue == null) continue;
                Transform destino = null;
                if (p.objectReferenceValue is GameObject g) destino = g.transform;
                else if (p.objectReferenceValue is Component k) destino = k.transform;
                if (destino != null && destino.IsChildOf(raiz))
                    yield return Ruta(c.transform) + " (" + c.GetType().Name + "." + p.propertyPath + ") apuntaba a '" +
                                 Ruta(destino) + "', que se borra.";
            }
        }
    }

    // ---------- Arañas ----------

    private static void QuitarLlamadasCompraBot(Scene escena, StringBuilder informe)
    {
        foreach (Button boton in Componentes<Button>(escena))
        {
            for (int i = boton.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                if (!(boton.onClick.GetPersistentTarget(i) is TiendaTrabajo)) continue;
                if (boton.onClick.GetPersistentMethodName(i) != "ComprarBot") continue;
                Undo.RecordObject(boton, "Quitar compra del Inspector");
                UnityEventTools.RemovePersistentListener(boton.onClick, i);
                informe.AppendLine("· '" + Ruta(boton.transform) + "': quitada la llamada TiendaTrabajo.ComprarBot del Inspector (la hace GeneradorAranas).");
            }
        }
    }

    private static void AsignarPanelBots(Scene escena, StringBuilder informe)
    {
        GameObject panel = BuscarPorNombre(escena, NombrePanelBots, false);
        foreach (GeneradorAranas generador in Componentes<GeneradorAranas>(escena))
        {
            if (generador.panelCompraBots != null || panel == null) continue;
            Undo.RecordObject(generador, "Asignar panel de arañas");
            generador.panelCompraBots = panel;
            informe.AppendLine("· " + generador.name + ": asignado el panel '" + NombrePanelBots + "'.");
        }
    }

    // ---------- Utilidades ----------

    private static void Borrar(Scene escena, string nombre, bool recortarEspacios, StringBuilder informe)
    {
        GameObject objeto = BuscarPorNombre(escena, nombre, recortarEspacios);
        if (objeto == null) return;
        string ruta = Ruta(objeto.transform);
        Undo.DestroyObjectImmediate(objeto);
        informe.AppendLine("· Borrado '" + ruta + "'.");
    }

    private static GameObject BuscarPorNombre(Scene escena, string nombre, bool recortarEspacios)
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                if ((recortarEspacios ? t.name.Trim() : t.name) == nombre)
                    return t.gameObject;
        return null;
    }

    private static IEnumerable<T> Componentes<T>(Scene escena) where T : Component
    {
        foreach (GameObject raiz in escena.GetRootGameObjects())
            foreach (T c in raiz.GetComponentsInChildren<T>(true))
                yield return c;
    }

    private static string Ruta(Transform t)
    {
        string ruta = t.name;
        while (t.parent != null) { t = t.parent; ruta = t.name + "/" + ruta; }
        return ruta;
    }
}
