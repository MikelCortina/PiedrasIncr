
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class PiedrasIncrDiagnosticoArana : EditorWindow
{
    private static readonly string[] Nombres =
    {
        "Leg_FL", "Leg_ML", "Leg_BL",
        "Leg_FR", "Leg_MR", "Leg_BR"
    };

    private GameObject bot;
    private bool dibujarEnScene = true;
    private bool grabando;
    private float inicioGrabacion;

    private readonly Dictionary<string, Estadistica> datos =
        new Dictionary<string, Estadistica>();

    private class Estadistica
    {
        public int muestras;
        public int sinApoyo;
        public int pasos;
        public bool pasoAnterior;
        public float extensionMaxima;
        public float distanciaMaximaTarget;
    }

    [MenuItem("Tools/PiedrasIncr/Diagnostico de Arana")]
    public static void Abrir()
    {
        GetWindow<PiedrasIncrDiagnosticoArana>(
            "Diagnostico Arana"
        );
    }

    private void OnEnable()
    {
        EditorApplication.update += Actualizar;
        SceneView.duringSceneGui += DibujarEnEscena;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Actualizar;
        SceneView.duringSceneGui -= DibujarEnEscena;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "DIAGNOSTICO DE LAS SEIS PATAS",
            EditorStyles.boldLabel
        );

        bot = (GameObject)EditorGUILayout.ObjectField(
            "Bot",
            bot,
            typeof(GameObject),
            true
        );

        if (GUILayout.Button("Usar bot seleccionado"))
        {
            GameObject seleccionado =
                Selection.activeGameObject;

            if (seleccionado != null)
            {
                BotRecolector recolector =
                    seleccionado.GetComponentInParent<
                        BotRecolector>();

                if (recolector != null)
                    bot = recolector.gameObject;
            }
        }

        dibujarEnScene = EditorGUILayout.Toggle(
            "Dibujar articulaciones",
            dibujarEnScene
        );

        if (bot == null)
        {
            EditorGUILayout.HelpBox(
                "Selecciona el objeto Bot de la araña.",
                MessageType.Info
            );

            return;
        }

        EditorGUILayout.Space(8);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(
                "Entra en Play para medir la animacion real.",
                MessageType.Info
            );
        }

        EditorGUILayout.LabelField(
            "ESTADO ACTUAL",
            EditorStyles.boldLabel
        );

        foreach (string nombre in Nombres)
        {
            PataProcedural pata = BuscarPata(nombre);

            if (!ReferenciasValidas(pata))
            {
                EditorGUILayout.LabelField(
                    nombre + ": faltan referencias"
                );
                continue;
            }

            float extension = CalcularExtension(pata);

            string estado = pata.EstaDandoPaso
                ? "PASO"
                : "APOYO";

            if (extension >= 97f)
                estado += " / MUY ESTIRADA";

            if (!pata.UltimoObjetivoConApoyo)
                estado += " / SIN APOYO DETECTADO";

            EditorGUILayout.LabelField(
                nombre.Replace("Leg_", "") +
                " | " + extension.ToString("F1") +
                "% | " + estado
            );
        }

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "GRABACION DE MOVIMIENTO",
            EditorStyles.boldLabel
        );

        using (new EditorGUI.DisabledScope(
            !Application.isPlaying || grabando))
        {
            if (GUILayout.Button(
                "Grabar 10 segundos",
                GUILayout.Height(34)))
            {
                datos.Clear();

                inicioGrabacion = Time.time;
                grabando = true;
            }
        }

        if (grabando)
        {
            float restante = Mathf.Max(
                0f,
                10f - (Time.time - inicioGrabacion)
            );

            EditorGUILayout.HelpBox(
                "Grabando... " +
                restante.ToString("F1") +
                " segundos restantes.",
                MessageType.Info
            );
        }

        if (GUILayout.Button(
            "Copiar informe de diagnostico",
            GUILayout.Height(30)))
        {
            EditorGUIUtility.systemCopyBuffer =
                CrearInforme();

            Debug.Log(
                "[ARANA] Informe copiado al portapapeles."
            );
        }

        EditorGUILayout.HelpBox(
            "Verde: apoyo detectado. " +
            "Amarillo: pata dando un paso. " +
            "Rojo: ultimo chequeo de apoyo fallido. " +
            "Las mediciones no modifican el bot.",
            MessageType.None
        );
    }

    private void Actualizar()
    {
        if (grabando && Application.isPlaying)
        {
            if (Time.time - inicioGrabacion >= 10f)
            {
                grabando = false;

                Debug.Log(
                    "[ARANA] DIAGNOSTICO COMPLETADO: " +
                    "pulsa Copiar informe de diagnostico."
                );
            }
            else
            {
                TomarMuestra();
            }
        }
        else if (grabando && !Application.isPlaying)
        {
            grabando = false;
        }

        Repaint();
    }

    private void TomarMuestra()
    {
        foreach (string nombre in Nombres)
        {
            PataProcedural pata = BuscarPata(nombre);

            if (!ReferenciasValidas(pata))
                continue;

            if (!datos.TryGetValue(
                nombre,
                out Estadistica e))
            {
                e = new Estadistica();
                datos[nombre] = e;
            }

            e.muestras++;

            float extension = CalcularExtension(pata);

            e.extensionMaxima = Mathf.Max(
                e.extensionMaxima,
                extension
            );

            float distanciaTarget = Vector3.Distance(
                pata.pie.position,
                pata.objetivo.position
            );

            e.distanciaMaximaTarget = Mathf.Max(
                e.distanciaMaximaTarget,
                distanciaTarget
            );

            if (!pata.UltimoObjetivoConApoyo)
                e.sinApoyo++;

            bool paso = pata.EstaDandoPaso;

            if (paso && !e.pasoAnterior)
                e.pasos++;

            e.pasoAnterior = paso;
        }
    }

    private PataProcedural BuscarPata(string nombre)
    {
        if (bot == null)
            return null;

        PataProcedural[] patas =
            bot.GetComponentsInChildren<PataProcedural>(
                true
            );

        foreach (PataProcedural pata in patas)
        {
            if (pata.gameObject.name == nombre)
                return pata;
        }

        return null;
    }

    private static bool ReferenciasValidas(
        PataProcedural pata)
    {
        return pata != null &&
               pata.cadera != null &&
               pata.rodilla != null &&
               pata.pie != null &&
               pata.objetivo != null;
    }

    private static float CalcularExtension(
        PataProcedural pata)
    {
        float superior = Vector3.Distance(
            pata.cadera.position,
            pata.rodilla.position
        );

        float inferior = Vector3.Distance(
            pata.rodilla.position,
            pata.pie.position
        );

        float total = superior + inferior;

        if (total <= 0.001f)
            return 0f;

        return 100f * Vector3.Distance(
            pata.cadera.position,
            pata.pie.position
        ) / total;
    }

    private void DibujarEnEscena(SceneView escena)
    {
        if (!dibujarEnScene || bot == null)
            return;

        foreach (string nombre in Nombres)
        {
            PataProcedural pata = BuscarPata(nombre);

            if (!ReferenciasValidas(pata))
                continue;

            Color color = Color.green;

            if (pata.EstaDandoPaso)
                color = Color.yellow;

            if (!pata.UltimoObjetivoConApoyo)
                color = Color.red;

            Handles.color = color;

            Handles.DrawAAPolyLine(
                4f,
                pata.cadera.position,
                pata.rodilla.position,
                pata.pie.position
            );

            Handles.color = Color.cyan;

            Handles.DrawDottedLine(
                pata.pie.position,
                pata.objetivo.position,
                4f
            );

            Handles.DrawWireDisc(
                pata.objetivo.position,
                Vector3.up,
                0.12f
            );

            Handles.Label(
                pata.pie.position + Vector3.up * 0.2f,
                nombre.Replace("Leg_", "")
            );
        }
    }

    private string CrearInforme()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("PIEDRASINCR - DIAGNOSTICO ARANA");
        sb.AppendLine("Bot: " +
            (bot != null ? bot.name : "NINGUNO"));

        sb.AppendLine("Play: " + Application.isPlaying);
        sb.AppendLine();

        if (bot == null)
            return sb.ToString();

        NavMeshAgent agente =
            bot.GetComponent<NavMeshAgent>();

        if (agente != null && agente.isOnNavMesh)
        {
            sb.AppendLine(
                "Velocidad NavMesh: " +
                agente.velocity.magnitude.ToString("F3")
            );
            sb.AppendLine(
                "NavMesh detenido: " + agente.isStopped
            );
        }

        sb.AppendLine();

        foreach (string nombre in Nombres)
        {
            PataProcedural pata = BuscarPata(nombre);

            sb.AppendLine("[" + nombre + "]");

            if (!ReferenciasValidas(pata))
            {
                sb.AppendLine("REFERENCIAS INCOMPLETAS");
                sb.AppendLine();
                continue;
            }

            float longitudSuperior = Vector3.Distance(
                pata.cadera.position,
                pata.rodilla.position
            );

            float longitudInferior = Vector3.Distance(
                pata.rodilla.position,
                pata.pie.position
            );

            sb.AppendLine(
                "Longitud superior actual: " +
                longitudSuperior.ToString("F3")
            );

            sb.AppendLine(
                "Longitud inferior actual: " +
                longitudInferior.ToString("F3")
            );

            sb.AppendLine(
                "Extension actual: " +
                CalcularExtension(pata).ToString("F1") + "%"
            );

            sb.AppendLine(
                "En paso: " + pata.EstaDandoPaso
            );

            sb.AppendLine(
                "Ultimo apoyo valido: " +
                pata.UltimoObjetivoConApoyo
            );

            sb.AppendLine(
                "Cadera mundo: " + pata.cadera.position
            );

            sb.AppendLine(
                "Rodilla mundo: " + pata.rodilla.position
            );

            sb.AppendLine(
                "Pie mundo: " + pata.pie.position
            );

            sb.AppendLine(
                "Target mundo: " + pata.objetivo.position
            );

            sb.AppendLine(
                "Direccion rodilla: " +
                pata.direccionRodilla
            );

            if (datos.TryGetValue(
                nombre,
                out Estadistica e))
            {
                sb.AppendLine(
                    "Muestras: " + e.muestras
                );

                sb.AppendLine(
                    "Pasos detectados: " + e.pasos
                );

                sb.AppendLine(
                    "Extension maxima: " +
                    e.extensionMaxima.ToString("F1") + "%"
                );

                sb.AppendLine(
                    "Max distancia pie-target: " +
                    e.distanciaMaximaTarget.ToString("F3")
                );

                sb.AppendLine(
                    "Muestras sin apoyo: " + e.sinApoyo
                );
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
