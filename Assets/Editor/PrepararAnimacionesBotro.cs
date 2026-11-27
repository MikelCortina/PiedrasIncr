using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Configura los clips del FBX de Botro (exportado desde Blender con todas sus acciones), crea su
/// Animator Controller (Idle -> LevantarCabeza -> Hablar, y vuelta al Idle) y se lo pone al Botro de la escena.
/// </summary>
public static class PrepararAnimacionesBotro
{
    private const string RutaFbx = "Assets/Robot/Botro.fbx";
    private const string RutaControlador = "Assets/Robot/Botro.controller";
    private const string Idle = "Idle", Levantar = "LevantarCabeza", Hablar = "Hablar";
    private static readonly string[] Bucles = { Idle, Hablar };

    [MenuItem("Tools/PiedrasIncr/Botro: preparar animaciones")]
    public static void Preparar()
    {
        Dictionary<string, AnimationClip> clips = ConfigurarClips();
        if (clips == null) return;
        AnimatorController controlador = CrearControlador(clips);
        AsignarABotro(controlador);
    }

    // Los clips se llaman como las acciones de Blender (sin "Robot_Rig|"); los respaldos no se importan.
    private static Dictionary<string, AnimationClip> ConfigurarClips()
    {
        ModelImporter importador = AssetImporter.GetAtPath(RutaFbx) as ModelImporter;
        if (importador == null)
        {
            Debug.LogError("Botro: no encuentro " + RutaFbx);
            return null;
        }

        ModelImporterClipAnimation[] lista = importador.defaultClipAnimations
            .Where(c => !c.takeName.EndsWith("_respaldo"))
            .ToArray();
        foreach (ModelImporterClipAnimation clip in lista)
        {
            clip.name = clip.takeName.Contains("|") ? clip.takeName.Substring(clip.takeName.LastIndexOf('|') + 1) : clip.takeName;
            clip.loopTime = Bucles.Contains(clip.name);
        }
        importador.animationType = ModelImporterAnimationType.Generic;
        importador.clipAnimations = lista;
        importador.SaveAndReimport();

        Dictionary<string, AnimationClip> clips = AssetDatabase.LoadAllAssetsAtPath(RutaFbx)
            .OfType<AnimationClip>()
            .Where(a => !a.name.StartsWith("__preview__"))
            .ToDictionary(a => a.name);
        foreach (string necesario in new[] { Idle, Levantar, Hablar })
        {
            if (clips.ContainsKey(necesario)) continue;
            Debug.LogError("Botro: el FBX no trae la acción '" + necesario + "'. Expórtalo con 'All Actions'.");
            return null;
        }
        return clips;
    }

    private static AnimatorController CrearControlador(Dictionary<string, AnimationClip> clips)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador) != null)
            AssetDatabase.DeleteAsset(RutaControlador);
        AnimatorController controlador = AnimatorController.CreateAnimatorControllerAtPath(RutaControlador);
        controlador.AddParameter(AnimacionBotro.ParamHablando, AnimatorControllerParameterType.Bool);

        AnimatorStateMachine maquina = controlador.layers[0].stateMachine;
        AnimatorState idle = Estado(maquina, Idle, clips[Idle], new Vector3(300, 0));
        AnimatorState levantar = Estado(maquina, Levantar, clips[Levantar], new Vector3(300, 100));
        AnimatorState hablar = Estado(maquina, Hablar, clips[Hablar], new Vector3(300, 200));
        maquina.defaultState = idle;

        Transicion(idle, levantar, true, 0.15f);
        Transicion(levantar, hablar, null, 0.05f);   // al acabar de levantar la cabeza (su último frame = primero de Hablar)
        Transicion(levantar, idle, false, 0.4f);     // se calla antes de terminar de levantarla
        Transicion(hablar, idle, false, 0.6f);       // baja la cabeza fundiéndose con el Idle

        EditorUtility.SetDirty(controlador);
        AssetDatabase.SaveAssets();
        return controlador;
    }

    private static AnimatorState Estado(AnimatorStateMachine maquina, string nombre, Motion clip, Vector3 posicion)
    {
        AnimatorState estado = maquina.AddState(nombre, posicion);
        estado.motion = clip;
        return estado;
    }

    // hablando = null: por tiempo de salida (al terminar el clip); true/false: según el parámetro, sin esperar.
    private static void Transicion(AnimatorState desde, AnimatorState hacia, bool? hablando, float duracion)
    {
        AnimatorStateTransition t = desde.AddTransition(hacia);
        t.hasFixedDuration = true;
        t.duration = duracion;
        t.hasExitTime = hablando == null;
        t.exitTime = 1f;
        if (hablando != null)
            t.AddCondition(hablando.Value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, AnimacionBotro.ParamHablando);
    }

    private static void AsignarABotro(AnimatorController controlador)
    {
        GameObject botro = GameObject.Find("Botro");
        if (botro == null)
        {
            Debug.LogWarning("Botro: controlador creado en " + RutaControlador + ", pero no hay ningún 'Botro' en la escena abierta.");
            return;
        }

        Animator animator = botro.GetComponent<Animator>();
        if (animator == null) animator = Undo.AddComponent<Animator>(botro);
        Undo.RecordObject(animator, "Animaciones de Botro");
        animator.runtimeAnimatorController = controlador;
        animator.applyRootMotion = false;
        if (botro.GetComponent<AnimacionBotro>() == null) Undo.AddComponent<AnimacionBotro>(botro);

        EditorSceneManager.MarkSceneDirty(botro.scene);
        Selection.activeGameObject = botro;
        Debug.Log("Botro: animaciones listas (Idle, LevantarCabeza, Hablar). Guarda la escena.", botro);
    }
}
