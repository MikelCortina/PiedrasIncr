using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Aplica el tema al cargar cada escena, según las listas de TemaUINeon:
//  - rutasPaneles: placa del escáner + animación + contenido.
//  - rutasSoloContenido: solo textos, botones, sliders... (p. ej. todo el Canvas del HUD).
//  - componentesSoloContenido: objetos con ese componente (útil para prefabs como PiedrasOptionsMenu).
// Así no hace falta modificar las escenas (menos conflictos de merge entre el equipo).
public static class ArranqueTemaUI
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;

        for (int i = 0; i < SceneManager.sceneCount; i++)
            AplicarEnEscena(SceneManager.GetSceneAt(i));
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo) => AplicarEnEscena(escena);

    public static void AplicarEnEscena(Scene escena)
    {
        TemaUINeon tema = TemaUINeon.Actual;
        if (!tema.aplicarAutomaticamente || !escena.isLoaded) return;
        // Escenas cuya UI se ajusta a mano en el editor (p. ej. el menú principal).
        if (tema.escenasSinTema != null && tema.escenasSinTema.Contains(escena.name)) return;

        GameObject[] raices = escena.GetRootGameObjects();
        int paneles = 0, contenidos = 0;

        // Primero los paneles (placa + animación); después el contenido suelto.
        foreach (string ruta in tema.rutasPaneles)
        {
            Transform panel = Buscar(raices, ruta);
            if (panel == null) continue;
            AplicadorTemaUI.Aplicar(panel, tema, true);
            paneles++;
        }

        foreach (string ruta in tema.rutasSoloContenido)
        {
            Transform raiz = Buscar(raices, ruta);
            if (raiz == null) continue;
            AplicadorTemaUI.Aplicar(raiz, tema, false);
            contenidos++;
        }

        if (tema.componentesSoloContenido.Count > 0)
        {
            HashSet<string> tipos = new HashSet<string>(tema.componentesSoloContenido);
            foreach (GameObject raiz in raices)
                foreach (MonoBehaviour mb in raiz.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (mb == null || !tipos.Contains(mb.GetType().Name)) continue;
                    AplicadorTemaUI.Aplicar(mb.transform, tema, false);
                    contenidos++;
                }
        }

        Debug.Log($"[TemaUI] Escena '{escena.name}': tema aplicado a {paneles} paneles y {contenidos} zonas de contenido " +
                  $"(fuente {(tema.fuenteTMP != null ? tema.fuenteTMP.name : "sin cambiar")}).");
    }

    // Busca "Raiz/Hijo/Nieto" incluyendo objetos inactivos (GameObject.Find no los encuentra).
    static Transform Buscar(GameObject[] raices, string ruta)
    {
        if (string.IsNullOrEmpty(ruta)) return null;

        int barra = ruta.IndexOf('/');
        string nombreRaiz = barra < 0 ? ruta : ruta.Substring(0, barra);

        foreach (GameObject raiz in raices)
        {
            if (raiz.name != nombreRaiz) continue;
            if (barra < 0) return raiz.transform;

            Transform encontrado = raiz.transform.Find(ruta.Substring(barra + 1));
            if (encontrado != null) return encontrado;
        }
        return null;
    }
}
