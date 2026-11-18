
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PiedrasIncr.Dialogue;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class PiedrasIncrDialogueEditor : EditorWindow
{
    private const string Root =
        "Assets/PiedrasIncr/Dialogue";

    private const string CharacterFolder =
        Root + "/Content/Characters";

    private const string ConversationFolder =
        Root + "/Content/Conversations";

    private const string CatalogPath =
        Root + "/Resources/PiedrasDialogueCatalog.asset";

    private readonly List<DialogueCharacter> characters =
        new List<DialogueCharacter>();

    private readonly List<DialogueConversation> conversations =
        new List<DialogueConversation>();

    private DialogueCatalog catalog;
    private DialogueCharacter activeCharacter;
    private DialogueConversation activeConversation;
    private DialogueConversation simulatedConversation;
    private DialogueNode simulatedNode;

    private readonly DialogueState simulation =
        new DialogueState();

    private string simulationMessage;
    private string search = "";

    private Vector2 sidebarScroll;
    private Vector2 contentScroll;

    private int sidebarTab;
    private int contentTab;

    private DialogueNodeType nodeTypeToAdd =
        DialogueNodeType.Line;

    [MenuItem("Tools/PiedrasIncr/Dialogue Editor")]
    public static void Open()
    {
        var window =
            GetWindow<PiedrasIncrDialogueEditor>(
                "PiedrasIncr Dialogue Editor"
            );

        window.minSize = new Vector2(900f, 550f);
        window.Show();
    }

    private void OnEnable()
    {
        PrepareFolders();
        LoadAssets();

        if (characters.Count == 0)
            CreateCharacter("Botro");

        if (activeCharacter == null && characters.Count > 0)
            activeCharacter = characters[0];

        Undo.undoRedoPerformed += Repaint;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= Repaint;
        AssetDatabase.SaveAssets();
    }

    private void OnProjectChange()
    {
        LoadAssets();
        Repaint();
    }

    private static void Folder(string parent, string name)
    {
        string path = parent + "/" + name;

        if (AssetDatabase.IsValidFolder(path))
            return;

        string guid = AssetDatabase.CreateFolder(
            parent,
            name
        );

        if (string.IsNullOrEmpty(guid))
            throw new InvalidOperationException(
                "No se pudo crear " + path
            );
    }

    private void PrepareFolders()
    {
        Folder("Assets", "PiedrasIncr");
        Folder("Assets/PiedrasIncr", "Dialogue");
        Folder(Root, "Runtime");
        Folder(Root + "/Runtime", "Data");
        Folder(Root + "/Runtime", "Logic");
        Folder(Root + "/Runtime", "Integration");
        Folder(Root + "/Runtime", "UI");
        Folder(Root, "Editor");
        Folder(Root, "Resources");
        Folder(Root, "Content");
        Folder(Root + "/Content", "Characters");
        Folder(Root + "/Content", "Conversations");

        catalog =
            AssetDatabase.LoadAssetAtPath<DialogueCatalog>(
                CatalogPath
            );

        if (catalog == null)
        {
            catalog = CreateInstance<DialogueCatalog>();

            AssetDatabase.CreateAsset(
                catalog,
                CatalogPath
            );

            AssetDatabase.SaveAssets();
        }
    }

    private void LoadAssets()
    {
        characters.Clear();
        conversations.Clear();

        foreach (string guid in AssetDatabase.FindAssets(
                     "t:DialogueCharacter",
                     new[] { CharacterFolder }))
        {
            var asset =
                AssetDatabase.LoadAssetAtPath<DialogueCharacter>(
                    AssetDatabase.GUIDToAssetPath(guid)
                );

            if (asset != null)
                characters.Add(asset);
        }

        foreach (string guid in AssetDatabase.FindAssets(
                     "t:DialogueConversation",
                     new[] { ConversationFolder }))
        {
            var asset =
                AssetDatabase.LoadAssetAtPath<DialogueConversation>(
                    AssetDatabase.GUIDToAssetPath(guid)
                );

            if (asset != null)
                conversations.Add(asset);
        }

        characters.Sort(
            (a, b) => string.CompareOrdinal(a.id, b.id)
        );

        conversations.Sort(
            (a, b) => string.CompareOrdinal(a.id, b.id)
        );

        if (catalog == null)
        {
            catalog =
                AssetDatabase.LoadAssetAtPath<DialogueCatalog>(
                    CatalogPath
                );
        }

        if (catalog == null)
            return;

        if (!catalog.characters.SequenceEqual(characters) ||
            !catalog.conversations.SequenceEqual(conversations))
        {
            Undo.RecordObject(
                catalog,
                "Actualizar catálogo de diálogos"
            );

            catalog.characters =
                new List<DialogueCharacter>(characters);

            catalog.conversations =
                new List<DialogueConversation>(conversations);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }
    }

    private static string Slug(string value)
    {
        string clean = Regex.Replace(
            (value ?? "nuevo").ToLowerInvariant(),
            "[^a-z0-9]+",
            "_"
        ).Trim('_');

        return string.IsNullOrEmpty(clean)
            ? "nuevo"
            : clean;
    }

    private DialogueCharacter CreateCharacter(string name)
    {
        var asset = CreateInstance<DialogueCharacter>();

        asset.id =
            Slug(name) + "_" +
            Guid.NewGuid().ToString("N").Substring(0, 6);

        asset.displayName = name;

        string path = AssetDatabase.GenerateUniqueAssetPath(
            CharacterFolder + "/" + Slug(name) + ".asset"
        );

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        LoadAssets();

        activeCharacter = asset;
        activeConversation = null;

        return asset;
    }

    private void CreateConversation()
    {
        if (activeCharacter == null)
        {
            EditorUtility.DisplayDialog(
                "Falta un personaje",
                "Selecciona o crea un personaje primero.",
                "Aceptar"
            );

            return;
        }

        var asset = CreateInstance<DialogueConversation>();

        asset.id =
            "conversacion_" +
            Guid.NewGuid().ToString("N").Substring(0, 8);

        asset.title = "Nueva conversación";
        asset.character = activeCharacter;

        DialogueNode beginning = new DialogueNode
        {
            type = DialogueNodeType.Line,
            speaker = DialogueSpeaker.NPC
        };

        DialogueNode ending = new DialogueNode
        {
            type = DialogueNodeType.End,
            pending = false
        };

        beginning.nextId = ending.id;
        asset.startId = beginning.id;

        asset.nodes.Add(beginning);
        asset.nodes.Add(ending);

        string path = AssetDatabase.GenerateUniqueAssetPath(
            ConversationFolder + "/" + asset.id + ".asset"
        );

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        LoadAssets();

        activeConversation = asset;
        contentTab = 0;
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(
                   EditorStyles.toolbar))
        {
            GUILayout.Label(
                "PIEDRASINCR  /  DIALOGUE EDITOR",
                EditorStyles.boldLabel
            );

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(
                    "Guardar assets",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(115f)))
            {
                AssetDatabase.SaveAssets();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawSidebar();

            GUILayout.Box(
                GUIContent.none,
                GUILayout.Width(1f),
                GUILayout.ExpandHeight(true)
            );

            using (new EditorGUILayout.VerticalScope())
            {
                contentScroll =
                    EditorGUILayout.BeginScrollView(contentScroll);

                if (activeConversation != null)
                {
                    DrawConversation();
                }
                else if (activeCharacter != null)
                {
                    DrawCharacter();
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Selecciona o crea un personaje.",
                        MessageType.Info
                    );
                }

                EditorGUILayout.EndScrollView();
            }
        }
    }

    private void DrawSidebar()
    {
        using (new EditorGUILayout.VerticalScope(
                   GUILayout.Width(260f)))
        {
            sidebarTab = GUILayout.Toolbar(
                sidebarTab,
                new[] { "Conversaciones", "Personajes" }
            );

            search = EditorGUILayout.TextField(
                "Buscar",
                search
            );

            if (GUILayout.Button(
                    "+ Crear personaje",
                    GUILayout.Height(28f)))
            {
                CreateCharacter("Nuevo personaje");
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button(
                    "+ Crear conversación",
                    GUILayout.Height(28f)))
            {
                CreateConversation();
                GUIUtility.ExitGUI();
            }

            sidebarScroll =
                EditorGUILayout.BeginScrollView(sidebarScroll);

            if (sidebarTab == 0)
            {
                foreach (var conversation in conversations)
                {
                    if (conversation == null)
                        continue;

                    string character =
                        conversation.character != null
                            ? conversation.character.displayName
                            : "SIN NPC";

                    string label =
                        conversation.title + "  [" + character + "]";

                    if (!Matches(label + " " + conversation.id))
                        continue;

                    if (GUILayout.Toggle(
                            activeConversation == conversation,
                            label,
                            "Button"))
                    {
                        if (activeConversation != conversation)
                        {
                            activeConversation = conversation;
                            activeCharacter = conversation.character;
                            simulatedConversation = null;
                            contentTab = 0;
                        }
                    }
                }
            }
            else
            {
                foreach (var character in characters)
                {
                    if (character == null)
                        continue;

                    if (!Matches(
                            character.displayName + " " + character.id))
                        continue;

                    if (GUILayout.Toggle(
                            activeCharacter == character &&
                            activeConversation == null,
                            character.displayName,
                            "Button"))
                    {
                        activeCharacter = character;
                        activeConversation = null;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private bool Matches(string value)
    {
        return string.IsNullOrWhiteSpace(search) ||
               value.IndexOf(
                   search,
                   StringComparison.OrdinalIgnoreCase
               ) >= 0;
    }

    private void DrawCharacter()
    {
        EditorGUILayout.LabelField(
            "PERSONAJE",
            EditorStyles.largeLabel
        );

        var so = new SerializedObject(activeCharacter);
        so.Update();

        EditorGUILayout.PropertyField(
            so.FindProperty("displayName"),
            new GUIContent("Nombre visible")
        );

        EditorGUILayout.PropertyField(
            so.FindProperty("id"),
            new GUIContent("ID estable")
        );

        if (so.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(activeCharacter);
            AssetDatabase.SaveAssets();
        }

        EditorGUILayout.Space(12f);

        EditorGUILayout.HelpBox(
            "Un personaje puede tener múltiples conversaciones. " +
            "Su ID debe ser único.",
            MessageType.Info
        );

        if (activeCharacter.displayName == "Botro")
        {
            if (GUILayout.Button(
                    "Conectar Botro de esta escena",
                    GUILayout.Height(34f)))
            {
                BindBotro();
            }
        }

        if (GUILayout.Button(
                "Eliminar personaje",
                GUILayout.Height(30f)))
        {
            if (conversations.Any(
                    c => c != null &&
                         c.character == activeCharacter))
            {
                EditorUtility.DisplayDialog(
                    "No se puede eliminar",
                    "Hay conversaciones vinculadas a este personaje.",
                    "Aceptar"
                );
            }
            else
            {
                DeleteAsset(activeCharacter);
            }
        }
    }

    private void DrawConversation()
    {
        EditorGUILayout.LabelField(
            "CONVERSACIÓN",
            EditorStyles.largeLabel
        );

        contentTab = GUILayout.Toolbar(
            contentTab,
            new[] { "Editar", "Simular", "Validar" }
        );

        EditorGUILayout.Space(8f);

        if (contentTab == 1)
        {
            DrawSimulator();
            return;
        }

        if (contentTab == 2)
        {
            List<string> issues = Validate(activeConversation);

            foreach (string issue in issues)
            {
                EditorGUILayout.HelpBox(
                    issue,
                    MessageType.Warning
                );
            }

            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No se han detectado problemas estructurales.",
                    MessageType.Info
                );
            }

            return;
        }

        var so = new SerializedObject(activeConversation);
        so.Update();

        EditorGUILayout.PropertyField(
            so.FindProperty("title"),
            new GUIContent("Título")
        );

        EditorGUILayout.PropertyField(
            so.FindProperty("id"),
            new GUIContent("ID único")
        );

        EditorGUILayout.PropertyField(
            so.FindProperty("character"),
            new GUIContent("Personaje")
        );

        EditorGUILayout.PropertyField(
            so.FindProperty("priority"),
            new GUIContent("Prioridad")
        );

        EditorGUILayout.PropertyField(
            so.FindProperty("draft"),
            new GUIContent("Borrador")
        );

        EditorGUILayout.Space(8f);

        EditorGUILayout.LabelField(
            "CONDICIONES PARA ACTIVARLA",
            EditorStyles.boldLabel
        );

        DrawConditions(
            so.FindProperty("conditions"),
            so
        );

        EditorGUILayout.Space(12f);

        EditorGUILayout.LabelField(
            "FLUJO DE DIÁLOGO",
            EditorStyles.boldLabel
        );

        DrawTarget(
            so.FindProperty("startId"),
            "Nodo inicial"
        );

        DrawNodes(
            so,
            so.FindProperty("nodes")
        );

        if (so.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(activeConversation);
            AssetDatabase.SaveAssets();
        }

        EditorGUILayout.Space(12f);

        if (GUILayout.Button(
                "Eliminar conversación",
                GUILayout.Height(30f)))
        {
            DeleteAsset(activeConversation);
        }
    }

    private void DrawNodes(
        SerializedObject so,
        SerializedProperty nodes)
    {
        for (int i = 0; i < nodes.arraySize; i++)
        {
            SerializedProperty node =
                nodes.GetArrayElementAtIndex(i);

            var type = node.FindPropertyRelative("type");
            string id = node.FindPropertyRelative("id").stringValue;

            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(
                        (i + 1) + " · " +
                        (DialogueNodeType)type.enumValueIndex,
                        EditorStyles.boldLabel
                    );

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(
                            "Eliminar",
                            GUILayout.Width(85f)))
                    {
                        so.ApplyModifiedProperties();
                        RemoveNode(id);
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.SelectableLabel(
                    id,
                    EditorStyles.miniLabel,
                    GUILayout.Height(17f)
                );

                EditorGUILayout.PropertyField(
                    type,
                    new GUIContent("Tipo de nodo")
                );

                var nodeType =
                    (DialogueNodeType)type.enumValueIndex;

                if (nodeType == DialogueNodeType.Line)
                {
                    EditorGUILayout.PropertyField(
                        node.FindPropertyRelative("speaker"),
                        new GUIContent("Habla")
                    );

                    EditorGUILayout.PropertyField(
                        node.FindPropertyRelative("text"),
                        new GUIContent("Texto")
                    );

                    EditorGUILayout.PropertyField(
                        node.FindPropertyRelative("pending"),
                        new GUIContent("Texto pendiente")
                    );

                    DrawTarget(
                        node.FindPropertyRelative("nextId"),
                        "Después ir a"
                    );

                    if (GUILayout.Button(
                            "+ Añadir línea después"))
                    {
                        so.ApplyModifiedProperties();
                        InsertLine(id);
                        GUIUtility.ExitGUI();
                    }
                }
                else if (nodeType == DialogueNodeType.Choices)
                {
                    EditorGUILayout.PropertyField(
                        node.FindPropertyRelative("text"),
                        new GUIContent("Pregunta")
                    );

                    SerializedProperty choices =
                        node.FindPropertyRelative("choices");

                    for (int j = 0; j < choices.arraySize; j++)
                    {
                        var choice =
                            choices.GetArrayElementAtIndex(j);

                        using (new EditorGUILayout.VerticalScope(
                                   EditorStyles.helpBox))
                        {
                            EditorGUILayout.PropertyField(
                                choice.FindPropertyRelative("text"),
                                new GUIContent(
                                    "Respuesta " + (j + 1)
                                )
                            );

                            DrawTarget(
                                choice.FindPropertyRelative("nextId"),
                                "Conectar a"
                            );

                            if (GUILayout.Button(
                                    "Eliminar respuesta"))
                            {
                                choices.DeleteArrayElementAtIndex(j);
                                so.ApplyModifiedProperties();
                                AssetDatabase.SaveAssets();
                                GUIUtility.ExitGUI();
                            }
                        }
                    }

                    if (GUILayout.Button("+ Añadir respuesta"))
                    {
                        choices.arraySize++;

                        var added = choices.GetArrayElementAtIndex(
                            choices.arraySize - 1
                        );

                        added.FindPropertyRelative("text").stringValue =
                            "Nueva respuesta";

                        added.FindPropertyRelative("nextId").stringValue =
                            "";

                        so.ApplyModifiedProperties();
                        AssetDatabase.SaveAssets();
                        GUIUtility.ExitGUI();
                    }
                }
                else if (nodeType == DialogueNodeType.Branch)
                {
                    DrawConditions(
                        node.FindPropertyRelative("conditions"),
                        so
                    );

                    DrawTarget(
                        node.FindPropertyRelative("trueId"),
                        "Si se cumple"
                    );

                    DrawTarget(
                        node.FindPropertyRelative("falseId"),
                        "Si no se cumple"
                    );
                }
                else
                {
                    EditorGUILayout.LabelField(
                        "Fin de la conversación",
                        EditorStyles.miniLabel
                    );
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            nodeTypeToAdd =
                (DialogueNodeType)EditorGUILayout.EnumPopup(
                    nodeTypeToAdd
                );

            if (GUILayout.Button(
                    "+ Añadir nodo",
                    GUILayout.Width(150f)))
            {
                so.ApplyModifiedProperties();
                AddNode(nodeTypeToAdd);
                GUIUtility.ExitGUI();
            }
        }
    }

    private void DrawTarget(
        SerializedProperty field,
        string label)
    {
        var names = new List<string>
        {
            "(Sin conectar)"
        };

        var ids = new List<string>
        {
            ""
        };

        foreach (var node in activeConversation.nodes)
        {
            if (node == null)
                continue;

            names.Add(
                node.type + " · " +
                node.id.Substring(
                    0,
                    Math.Min(8, node.id.Length)
                )
            );

            ids.Add(node.id);
        }

        int selected = ids.IndexOf(field.stringValue);

        if (selected < 0)
        {
            names.Add(
                "ENLACE ROTO: " + field.stringValue
            );

            ids.Add(field.stringValue);

            selected = ids.Count - 1;
        }

        field.stringValue = ids[
            EditorGUILayout.Popup(
                label,
                selected,
                names.ToArray()
            )
        ];
    }

    private static void DrawConditions(
        SerializedProperty set,
        SerializedObject so)
    {
        EditorGUILayout.PropertyField(
            set.FindPropertyRelative("logic"),
            new GUIContent("Combinar reglas")
        );

        EditorGUILayout.PropertyField(
            set.FindPropertyRelative("not"),
            new GUIContent("NOT del conjunto")
        );

        var conditions =
            set.FindPropertyRelative("conditions");

        for (int i = 0; i < conditions.arraySize; i++)
        {
            var entry = conditions.GetArrayElementAtIndex(i);

            using (new EditorGUILayout.HorizontalScope(
                       EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(
                    entry.FindPropertyRelative("type"),
                    GUIContent.none
                );

                EditorGUILayout.PropertyField(
                    entry.FindPropertyRelative("not"),
                    new GUIContent("NOT"),
                    GUILayout.Width(65f)
                );

                var type = (DialogueConditionType)
                    entry.FindPropertyRelative("type").enumValueIndex;

                if (type == DialogueConditionType.CoinsAtLeast ||
                    type == DialogueConditionType.BootsLevelAtLeast ||
                    type == DialogueConditionType.JetpackLevelAtLeast)
                {
                    EditorGUILayout.PropertyField(
                        entry.FindPropertyRelative("amount"),
                        GUIContent.none,
                        GUILayout.Width(65f)
                    );
                }

                if (GUILayout.Button(
                        "X",
                        GUILayout.Width(26f)))
                {
                    conditions.DeleteArrayElementAtIndex(i);
                    so.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    GUIUtility.ExitGUI();
                }
            }
        }

        if (GUILayout.Button("+ Añadir condición"))
        {
            conditions.arraySize++;

            var entry = conditions.GetArrayElementAtIndex(
                conditions.arraySize - 1
            );

            entry.FindPropertyRelative("type").enumValueIndex = 0;
            entry.FindPropertyRelative("not").boolValue = false;
            entry.FindPropertyRelative("amount").intValue = 0;

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            GUIUtility.ExitGUI();
        }
    }

    private void InsertLine(string afterId)
    {
        Undo.RecordObject(
            activeConversation,
            "Insertar línea"
        );

        int index = activeConversation.nodes.FindIndex(
            n => n.id == afterId
        );

        if (index < 0)
            return;

        var previous = activeConversation.nodes[index];

        var node = new DialogueNode
        {
            type = DialogueNodeType.Line,
            nextId = previous.nextId
        };

        previous.nextId = node.id;

        activeConversation.nodes.Insert(
            index + 1,
            node
        );

        SaveConversation();
    }

    private void AddNode(DialogueNodeType type)
    {
        Undo.RecordObject(
            activeConversation,
            "Añadir nodo de diálogo"
        );

        activeConversation.nodes.Add(
            new DialogueNode
            {
                type = type,
                pending = type != DialogueNodeType.End
            }
        );

        SaveConversation();
    }

    private void RemoveNode(string id)
    {
        if (!EditorUtility.DisplayDialog(
                "Eliminar nodo",
                "Las conexiones a este nodo quedarán marcadas como rotas.",
                "Eliminar",
                "Cancelar"))
        {
            return;
        }

        Undo.RecordObject(
            activeConversation,
            "Eliminar nodo de diálogo"
        );

        activeConversation.nodes.RemoveAll(
            n => n.id == id
        );

        SaveConversation();
    }

    private void SaveConversation()
    {
        EditorUtility.SetDirty(activeConversation);
        AssetDatabase.SaveAssets();
    }

    private void DrawSimulator()
    {
        EditorGUILayout.HelpBox(
            "Valores de prueba: no escriben en partidas ni guardados.",
            MessageType.Info
        );

        simulation.coins = EditorGUILayout.IntField(
            "Monedas",
            simulation.coins
        );

        simulation.hammer = EditorGUILayout.Toggle(
            "Martillo",
            simulation.hammer
        );

        simulation.launcher = EditorGUILayout.Toggle(
            "Arma",
            simulation.launcher
        );

        simulation.tornado = EditorGUILayout.Toggle(
            "Torbellino",
            simulation.tornado
        );

        simulation.processor = EditorGUILayout.Toggle(
            "Procesadora",
            simulation.processor
        );

        simulation.bootsLevel = EditorGUILayout.IntSlider(
            "Botas",
            simulation.bootsLevel,
            0,
            3
        );

        simulation.jetpackLevel = EditorGUILayout.IntSlider(
            "Jetpack",
            simulation.jetpackLevel,
            0,
            4
        );

        if (GUILayout.Button(
                "Simular selección del NPC",
                GUILayout.Height(32f)))
        {
            simulatedConversation = DialogueLogicEngine.Select(
                catalog,
                activeConversation.character,
                simulation,
                out simulationMessage
            );

            string diagnostic = null;

            simulatedNode =
                simulatedConversation != null
                    ? DialogueLogicEngine.Resolve(
                        simulatedConversation,
                        simulatedConversation.startId,
                        simulation,
                        out diagnostic
                    )
                    : null;

            if (simulatedConversation != null &&
                simulatedNode == null)
            {
                simulationMessage = diagnostic;
            }
        }

        EditorGUILayout.Space(10f);

        EditorGUILayout.HelpBox(
            simulationMessage ??
            "Pulsa Simular selección del NPC.",
            MessageType.Info
        );

        if (simulatedConversation == null ||
            simulatedNode == null)
        {
            return;
        }

        EditorGUILayout.LabelField(
            simulatedConversation.title,
            EditorStyles.boldLabel
        );

        if (simulatedNode.type == DialogueNodeType.End)
        {
            EditorGUILayout.HelpBox(
                "Fin de la conversación.",
                MessageType.Info
            );

            return;
        }

        EditorGUILayout.LabelField(
            simulatedNode.text ?? "",
            EditorStyles.wordWrappedLabel
        );

        if (simulatedNode.type == DialogueNodeType.Line)
        {
            if (GUILayout.Button("Siguiente"))
            {
                SimulateJump(simulatedNode.nextId);
            }
        }
        else if (simulatedNode.type == DialogueNodeType.Choices)
        {
            foreach (var choice in simulatedNode.choices)
            {
                if (choice != null &&
                    GUILayout.Button(choice.text))
                {
                    SimulateJump(choice.nextId);
                    break;
                }
            }
        }
    }

    private void SimulateJump(string next)
    {
        simulatedNode = DialogueLogicEngine.Resolve(
            simulatedConversation,
            next,
            simulation,
            out string diagnostic
        );

        if (simulatedNode == null)
            simulationMessage = diagnostic;
    }

    private List<string> Validate(
        DialogueConversation conversation)
    {
        var errors = new List<string>();

        if (conversation.character == null)
            errors.Add("No hay NPC asignado.");

        if (string.IsNullOrWhiteSpace(conversation.id))
            errors.Add("ID de conversación vacío.");

        if (conversations.Count(
                c => c != null &&
                     c.id == conversation.id) > 1)
        {
            errors.Add(
                "ID duplicado en otras conversaciones."
            );
        }

        if (conversation.character != null &&
            characters.Count(
                c => c != null &&
                     c.id == conversation.character.id) > 1)
        {
            errors.Add(
                "ID de personaje duplicado."
            );
        }

        var ids = new HashSet<string>();

        foreach (DialogueNode node in conversation.nodes)
        {
            if (node == null)
                continue;

            if (string.IsNullOrWhiteSpace(node.id) ||
                !ids.Add(node.id))
            {
                errors.Add(
                    "ID de nodo vacío o duplicado."
                );
            }

            if (node.type == DialogueNodeType.Line &&
                string.IsNullOrWhiteSpace(node.text))
            {
                errors.Add(
                    "Texto vacío: " + node.id
                );
            }

            if (node.type == DialogueNodeType.Line &&
                node.pending)
            {
                errors.Add(
                    "Texto pendiente: " + node.id
                );
            }

            if (node.type == DialogueNodeType.Choices &&
                (node.choices == null ||
                 node.choices.Count == 0))
            {
                errors.Add(
                    "Nodo de respuestas sin opciones: " +
                    node.id
                );
            }

            if (node.type == DialogueNodeType.Branch)
                CheckConditions(node.conditions, errors);
        }

        CheckConditions(
            conversation.conditions,
            errors
        );

        var byId = conversation.nodes
            .Where(
                n => n != null &&
                     !string.IsNullOrWhiteSpace(n.id)
            )
            .GroupBy(n => n.id)
            .ToDictionary(
                g => g.Key,
                g => g.First()
            );

        var reached = new HashSet<string>();
        var waiting = new Queue<string>();

        waiting.Enqueue(
            conversation.startId ?? ""
        );

        bool hasEnd = false;

        while (waiting.Count > 0)
        {
            string id = waiting.Dequeue();

            if (!reached.Add(id))
                continue;

            if (!byId.TryGetValue(
                    id,
                    out DialogueNode node))
            {
                errors.Add(
                    "Conexión rota o vacía: " + id
                );

                continue;
            }

            if (node.type == DialogueNodeType.End)
            {
                hasEnd = true;
                continue;
            }

            if (node.type == DialogueNodeType.Line)
            {
                waiting.Enqueue(
                    node.nextId ?? ""
                );
            }

            if (node.type == DialogueNodeType.Branch)
            {
                waiting.Enqueue(
                    node.trueId ?? ""
                );

                waiting.Enqueue(
                    node.falseId ?? ""
                );
            }

            if (node.type == DialogueNodeType.Choices &&
                node.choices != null)
            {
                foreach (var choice in node.choices)
                {
                    waiting.Enqueue(
                        choice != null
                            ? choice.nextId ?? ""
                            : ""
                    );
                }
            }
        }

        if (!hasEnd)
        {
            errors.Add(
                "No se alcanza ningún nodo Fin desde el inicio."
            );
        }

        foreach (DialogueNode node in conversation.nodes)
        {
            if (node != null &&
                !reached.Contains(node.id))
            {
                errors.Add(
                    "Nodo inaccesible: " + node.id
                );
            }
        }

        return errors.Distinct().ToList();
    }

    private static void CheckConditions(
        DialogueConditionSet set,
        List<string> errors)
    {
        if (set == null || set.conditions == null)
            return;

        foreach (var condition in set.conditions)
        {
            if (condition != null &&
                (
                    condition.type ==
                        DialogueConditionType.ConversationCompletedPending
                    ||
                    condition.type ==
                        DialogueConditionType.EventCompletedPending
                ))
            {
                errors.Add(
                    "Condición pendiente de integración: " +
                    condition.type
                );
            }
        }
    }

    private void DeleteAsset(UnityEngine.Object asset)
    {
        if (asset == null)
            return;

        if (!EditorUtility.DisplayDialog(
                "Eliminar asset",
                "Se enviará a la papelera: " + asset.name,
                "Eliminar",
                "Cancelar"))
        {
            return;
        }

        string path = AssetDatabase.GetAssetPath(asset);

        if (!AssetDatabase.MoveAssetToTrash(path))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "No se pudo eliminar: " + path,
                "Aceptar"
            );

            return;
        }

        activeConversation = null;
        activeCharacter = null;
        simulatedConversation = null;

        LoadAssets();

        if (characters.Count > 0)
            activeCharacter = characters[0];

        GUIUtility.ExitGUI();
    }

    private void BindBotro()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Modo Play",
                "Detén el juego antes de configurar el NPC.",
                "Aceptar"
            );

            return;
        }

        GameObject robot = GameObject.Find("Botro");

        if (robot == null)
        {
            EditorUtility.DisplayDialog(
                "Botro no encontrado",
                "Abre la escena Piedra, donde aparece Botro en la jerarquía.",
                "Aceptar"
            );

            return;
        }

        NPCDialogueAgent agent =
            robot.GetComponent<NPCDialogueAgent>();

        if (agent == null)
            agent = Undo.AddComponent<NPCDialogueAgent>(robot);

        Undo.RecordObject(
            agent,
            "Asignar NPC conversacional"
        );

        agent.character = activeCharacter;

        EditorUtility.SetDirty(agent);

        PrefabUtility.RecordPrefabInstancePropertyModifications(
            agent
        );

        if (robot.GetComponent<Collider>() == null)
        {
            SphereCollider sphere =
                Undo.AddComponent<SphereCollider>(robot);

            Renderer[] renderers =
                robot.GetComponentsInChildren<Renderer>();

            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;

                foreach (Renderer renderer in renderers)
                    bounds.Encapsulate(renderer.bounds);

                sphere.center =
                    robot.transform.InverseTransformPoint(
                        bounds.center
                    );

                sphere.radius = Mathf.Max(
                    0.75f,
                    bounds.extents.magnitude
                );
            }
            else
            {
                sphere.center = Vector3.up;
                sphere.radius = 1.5f;
            }

            sphere.isTrigger = true;

            PrefabUtility.RecordPrefabInstancePropertyModifications(
                sphere
            );
        }

        Selection.activeGameObject = robot;

        EditorSceneManager.MarkSceneDirty(
            robot.scene
        );

        EditorUtility.DisplayDialog(
            "Botro vinculado",
            "Se ha añadido NPCDialogueAgent a la instancia de la escena. " +
            "El prefab original no se ha editado.",
            "Aceptar"
        );
    }
}
