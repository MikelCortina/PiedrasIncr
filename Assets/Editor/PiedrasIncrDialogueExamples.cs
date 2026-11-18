
using System;
using System.Collections.Generic;
using PiedrasIncr.Dialogue;
using UnityEditor;
using UnityEngine;

public static class PiedrasIncrDialogueExamples
{
    private const string Root =
        "Assets/PiedrasIncr/Dialogue";

    private const string Characters =
        Root + "/Content/Characters";

    private const string Conversations =
        Root + "/Content/Conversations";

    private const string CatalogPath =
        Root + "/Resources/PiedrasDialogueCatalog.asset";

    [MenuItem("Tools/PiedrasIncr/Generar conversaciones de prueba")]
    public static void Generate()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Modo Play",
                "Detén la partida antes de generar los ejemplos.",
                "Aceptar"
            );
            return;
        }

        PrepareFolders();

        DialogueCatalog catalog =
            AssetDatabase.LoadAssetAtPath<DialogueCatalog>(
                CatalogPath
            );

        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<DialogueCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        DialogueCharacter botro = FindBotro();

        if (botro == null)
        {
            botro = ScriptableObject.CreateInstance<DialogueCharacter>();
            botro.id = "botro";
            botro.displayName = "Botro";

            AssetDatabase.CreateAsset(
                botro,
                AssetDatabase.GenerateUniqueAssetPath(
                    Characters + "/Botro.asset"
                )
            );
        }

        var examples = new List<DialogueConversation>
        {
            CreateWelcome(botro),
            CreateCoins(botro),
            CreateTools(botro),
            CreateProcessor(botro),
            CreateAdvanced(botro)
        };

        var existingIds = new HashSet<string>();

        foreach (string guid in AssetDatabase.FindAssets(
                     "t:DialogueConversation",
                     new[] { Conversations }))
        {
            DialogueConversation existing =
                AssetDatabase.LoadAssetAtPath<DialogueConversation>(
                    AssetDatabase.GUIDToAssetPath(guid)
                );

            if (existing != null)
                existingIds.Add(existing.id);
        }

        int created = 0;
        int skipped = 0;

        foreach (DialogueConversation example in examples)
        {
            if (existingIds.Contains(example.id))
            {
                UnityEngine.Object.DestroyImmediate(example);
                skipped++;
                continue;
            }

            string path = AssetDatabase.GenerateUniqueAssetPath(
                Conversations + "/" + example.id + ".asset"
            );

            AssetDatabase.CreateAsset(example, path);
            existingIds.Add(example.id);
            created++;
        }

        Undo.RecordObject(
            catalog,
            "Actualizar catálogo de conversaciones"
        );

        catalog.characters = new List<DialogueCharacter>();
        catalog.conversations = new List<DialogueConversation>();

        foreach (string guid in AssetDatabase.FindAssets(
                     "t:DialogueCharacter",
                     new[] { Characters }))
        {
            DialogueCharacter character =
                AssetDatabase.LoadAssetAtPath<DialogueCharacter>(
                    AssetDatabase.GUIDToAssetPath(guid)
                );

            if (character != null)
                catalog.characters.Add(character);
        }

        foreach (string guid in AssetDatabase.FindAssets(
                     "t:DialogueConversation",
                     new[] { Conversations }))
        {
            DialogueConversation conversation =
                AssetDatabase.LoadAssetAtPath<DialogueConversation>(
                    AssetDatabase.GUIDToAssetPath(guid)
                );

            if (conversation != null)
                catalog.conversations.Add(conversation);
        }

        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        PiedrasIncrDialogueEditor.Open();

        EditorUtility.DisplayDialog(
            "Conversaciones de prueba",
            "Conversaciones creadas: " + created +
            "\nYa existentes, sin modificar: " + skipped +
            "\n\nAbre Botro en Dialogue Editor para revisarlas.",
            "Aceptar"
        );
    }

    private static void PrepareFolders()
    {
        EnsureFolder("Assets", "PiedrasIncr");
        EnsureFolder("Assets/PiedrasIncr", "Dialogue");
        EnsureFolder(Root, "Content");
        EnsureFolder(Root + "/Content", "Characters");
        EnsureFolder(Root + "/Content", "Conversations");
        EnsureFolder(Root, "Resources");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;

        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static DialogueCharacter FindBotro()
    {
        foreach (string guid in AssetDatabase.FindAssets(
                     "t:DialogueCharacter",
                     new[] { Characters }))
        {
            DialogueCharacter character =
                AssetDatabase.LoadAssetAtPath<DialogueCharacter>(
                    AssetDatabase.GUIDToAssetPath(guid)
                );

            if (character != null &&
                string.Equals(
                    character.displayName,
                    "Botro",
                    StringComparison.OrdinalIgnoreCase))
            {
                return character;
            }
        }

        return null;
    }

    private static DialogueConversation NewConversation(
        DialogueCharacter character,
        string id,
        string title,
        int priority)
    {
        DialogueConversation conversation =
            ScriptableObject.CreateInstance<DialogueConversation>();

        conversation.id = id;
        conversation.title = title;
        conversation.name = title;
        conversation.character = character;
        conversation.priority = priority;
        conversation.draft = false;

        return conversation;
    }

    private static DialogueNode Line(
        string text,
        float x,
        float y)
    {
        return new DialogueNode
        {
            type = DialogueNodeType.Line,
            speaker = DialogueSpeaker.NPC,
            text = text,
            pending = false,
            graphPosition = new Vector2(x, y)
        };
    }

    private static DialogueNode Choices(
        string question,
        float x,
        float y)
    {
        return new DialogueNode
        {
            type = DialogueNodeType.Choices,
            text = question,
            pending = false,
            graphPosition = new Vector2(x, y)
        };
    }

    private static DialogueNode End(float x, float y)
    {
        return new DialogueNode
        {
            type = DialogueNodeType.End,
            pending = false,
            graphPosition = new Vector2(x, y)
        };
    }

    private static DialogueConversation Simple(
        DialogueCharacter character,
        string id,
        string title,
        int priority,
        string text)
    {
        DialogueConversation conversation =
            NewConversation(character, id, title, priority);

        DialogueNode line = Line(text, 100, 100);
        DialogueNode end = End(450, 100);

        line.nextId = end.id;
        conversation.startId = line.id;

        conversation.nodes.Add(line);
        conversation.nodes.Add(end);

        return conversation;
    }

    private static void AddCondition(
        DialogueConditionSet set,
        DialogueConditionType type,
        int amount = 0,
        bool not = false)
    {
        set.conditions.Add(
            new DialogueCondition
            {
                type = type,
                amount = amount,
                not = not
            }
        );
    }

    private static DialogueConversation CreateWelcome(
        DialogueCharacter botro)
    {
        DialogueConversation conversation =
            NewConversation(
                botro,
                "botro_demo_bienvenida",
                "DEMO - Bienvenida",
                0
            );

        DialogueNode start = Line(
            "¡Eh, tú! Sí, tú. Soy Botro. ¿Acabas de aterrizar o llevas un rato dando vueltas?",
            100, 100
        );

        DialogueNode choices = Choices(
            "¿Qué quieres saber?",
            400, 100
        );

        DialogueNode help = Line(
            "Las monedas sirven para conseguir mejoras. Echa un vistazo a la tienda y sigue explorando.",
            700, 50
        );

        DialogueNode goodbye = Line(
            "Perfecto. Si necesitas algo, ya sabes dónde encontrarme.",
            700, 250
        );

        DialogueNode end = End(1000, 150);

        start.nextId = choices.id;

        choices.choices.Add(
            new DialogueChoice
            {
                text = "¿Para qué sirven las monedas?",
                nextId = help.id
            }
        );

        choices.choices.Add(
            new DialogueChoice
            {
                text = "Nada, solo quería saludar.",
                nextId = goodbye.id
            }
        );

        help.nextId = end.id;
        goodbye.nextId = end.id;

        conversation.startId = start.id;

        conversation.nodes.AddRange(
            new[] { start, choices, help, goodbye, end }
        );

        return conversation;
    }

    private static DialogueConversation CreateCoins(
        DialogueCharacter botro)
    {
        DialogueConversation conversation =
            Simple(
                botro,
                "botro_demo_monedas",
                "DEMO - Al menos 50 monedas",
                10,
                "¡Vaya! Ya tienes al menos 50 monedas. Parece que le estás cogiendo el truco al negocio."
            );

        AddCondition(
            conversation.conditions,
            DialogueConditionType.CoinsAtLeast,
            50
        );

        return conversation;
    }

    private static DialogueConversation CreateTools(
        DialogueCharacter botro)
    {
        DialogueConversation conversation =
            Simple(
                botro,
                "botro_demo_herramientas",
                "DEMO - Arma o torbellino",
                15,
                "¡Eso ya es equipamiento serio! Veo que has desbloqueado el arma o el torbellino."
            );

        conversation.conditions.logic = DialogueLogic.OR;

        AddCondition(
            conversation.conditions,
            DialogueConditionType.LauncherUnlocked
        );

        AddCondition(
            conversation.conditions,
            DialogueConditionType.TornadoUnlocked
        );

        return conversation;
    }

    private static DialogueConversation CreateProcessor(
        DialogueCharacter botro)
    {
        DialogueConversation conversation =
            Simple(
                botro,
                "botro_demo_procesadora",
                "DEMO - Procesadora desbloqueada",
                20,
                "¡Bien hecho! Has conseguido desbloquear la procesadora. Ahora ya puedes experimentar con ella y las piedras."
            );

        AddCondition(
            conversation.conditions,
            DialogueConditionType.ProcessorUnlocked
        );

        return conversation;
    }

    private static DialogueConversation CreateAdvanced(
        DialogueCharacter botro)
    {
        DialogueConversation conversation =
            NewConversation(
                botro,
                "botro_demo_avanzado",
                "DEMO - Progreso avanzado",
                30
            );

        conversation.conditions.logic = DialogueLogic.AND;

        AddCondition(
            conversation.conditions,
            DialogueConditionType.ProcessorUnlocked
        );

        AddCondition(
            conversation.conditions,
            DialogueConditionType.CoinsAtLeast,
            50
        );

        DialogueNode start = Line(
            "¡Oye! Ya tienes la procesadora desbloqueada y al menos 50 monedas. Vas progresando rápido.",
            100, 100
        );

        DialogueNode branch = new DialogueNode
        {
            type = DialogueNodeType.Branch,
            pending = false,
            graphPosition = new Vector2(400, 100)
        };

        AddCondition(
            branch.conditions,
            DialogueConditionType.HammerUnlocked,
            0,
            true
        );

        DialogueNode noHammer = Line(
            "Eso sí, todavía no tienes el martillo. Cuando lo desbloquees podrás utilizar el modo construcción.",
            700, 50
        );

        DialogueNode hasHammer = Line(
            "¡Y encima tienes el martillo! Ya puedes aprovechar el modo construcción.",
            700, 250
        );

        DialogueNode end = End(1000, 150);

        start.nextId = branch.id;
        branch.trueId = noHammer.id;
        branch.falseId = hasHammer.id;

        noHammer.nextId = end.id;
        hasHammer.nextId = end.id;

        conversation.startId = start.id;

        conversation.nodes.AddRange(
            new[]
            {
                start,
                branch,
                noHammer,
                hasHammer,
                end
            }
        );

        return conversation;
    }
}
