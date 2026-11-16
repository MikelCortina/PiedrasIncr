using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PiedrasIncr.Audio.EditorTools
{
    // Desplegable con los nombres de la lista para los campos marcados con [SoundName].
    [CustomPropertyDrawer(typeof(SoundNameAttribute))]
    public sealed class SoundNameDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SoundLibrary libreria = Resources.Load<SoundLibrary>("SoundLibrary");

            if (property.propertyType != SerializedPropertyType.String || libreria == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            List<string> opciones = new List<string> { "(ninguno)" };

            foreach (SoundEvent sonido in libreria.Sonidos)
            {
                if (sonido != null && !string.IsNullOrEmpty(sonido.Nombre))
                    opciones.Add(sonido.Nombre);
            }

            string actual = property.stringValue;
            int indice = 0;

            if (!string.IsNullOrEmpty(actual))
            {
                indice = opciones.IndexOf(actual);

                if (indice < 0)
                {
                    opciones.Add(actual + " (no existe)");
                    indice = opciones.Count - 1;
                }
            }

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            int nuevo = EditorGUI.Popup(position, label.text, indice, opciones.ToArray());

            if (EditorGUI.EndChangeCheck())
                property.stringValue = nuevo == 0 ? string.Empty : opciones[nuevo];

            EditorGUI.EndProperty();
        }
    }

    // Anade al Inspector de la SoundLibrary un apartado para escuchar los sonidos.
    [CustomEditor(typeof(SoundLibrary))]
    public sealed class SoundLibraryEditor : UnityEditor.Editor
    {
        private static AudioSource fuente;
        private int seleccionado;

        public override void OnInspectorGUI()
        {
            SoundLibrary libreria = (SoundLibrary)target;

            DibujarPrueba(libreria);
            EditorGUILayout.Space();
            DrawDefaultInspector();
        }

        private void OnDisable()
        {
            Parar();
        }

        private void DibujarPrueba(SoundLibrary libreria)
        {
            EditorGUILayout.LabelField("Probar sonido", EditorStyles.boldLabel);

            IReadOnlyList<SoundEvent> sonidos = libreria.Sonidos;

            if (sonidos.Count == 0)
            {
                EditorGUILayout.HelpBox("La lista esta vacia.", MessageType.Info);
                return;
            }

            string[] nombres = new string[sonidos.Count];

            for (int i = 0; i < sonidos.Count; i++)
            {
                bool sinNombre = sonidos[i] == null || string.IsNullOrEmpty(sonidos[i].Nombre);
                nombres[i] = sinNombre ? "(sin nombre " + i + ")" : sonidos[i].Nombre;
            }

            seleccionado = Mathf.Clamp(seleccionado, 0, sonidos.Count - 1);
            seleccionado = EditorGUILayout.Popup("Sonido", seleccionado, nombres);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Probar"))
                Probar(libreria, sonidos[seleccionado]);

            if (GUILayout.Button("Parar"))
                Parar();

            EditorGUILayout.EndHorizontal();
        }

        private static void Probar(SoundLibrary libreria, SoundEvent sonido)
        {
            if (sonido == null) return;

            AudioClip clip = sonido.ElegirClip();

            if (clip == null)
            {
                Debug.LogWarning("[Audio] Ese sonido no tiene clips asignados.");
                return;
            }

            if (fuente == null)
            {
                GameObject go = EditorUtility.CreateGameObjectWithHideFlags(
                    "PruebaSonidoEditor",
                    HideFlags.HideAndDontSave,
                    typeof(AudioSource));

                fuente = go.GetComponent<AudioSource>();
                fuente.playOnAwake = false;
            }

            // Se escucha siempre en 2D: es para oir clip, volumen y pitch, no la distancia.
            fuente.Stop();
            fuente.clip = clip;
            fuente.volume = sonido.ElegirVolumen();
            fuente.pitch = sonido.ElegirPitch();
            fuente.loop = false;
            fuente.spatialBlend = 0f;
            fuente.outputAudioMixerGroup = libreria.ObtenerGrupo(sonido.Bus);
            fuente.Play();
        }

        private static void Parar()
        {
            if (fuente != null) fuente.Stop();
        }
    }
}