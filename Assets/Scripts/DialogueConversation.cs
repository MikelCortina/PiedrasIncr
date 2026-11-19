
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PiedrasIncr.Dialogue
{
    public enum DialogueNodeType
    {
        Line,
        Choices,
        Branch,
        End
    }

    public enum DialogueSpeaker
    {
        NPC,
        Player,
        Narrator
    }

    public enum DialogueLogic
    {
        AND,
        OR
    }

    public enum DialogueConditionType
    {
        CoinsAtLeast,
        HammerUnlocked,
        LauncherUnlocked,
        TornadoUnlocked,
        ProcessorUnlocked,
        BootsLevelAtLeast,
        JetpackLevelAtLeast,
        ConversationCompletedPending,
        EventCompletedPending
    }

    [Serializable]
    public class DialogueCondition
    {
        public DialogueConditionType type;
        public bool not;
        public int amount;
    }

    [Serializable]
    public class DialogueConditionSet
    {
        public DialogueLogic logic = DialogueLogic.AND;
        public bool not;
        public List<DialogueCondition> conditions =
            new List<DialogueCondition>();
    }

    [Serializable]
    public class DialogueChoice
    {
        public string text = "Nueva respuesta";
        public string nextId;
    }

    [Serializable]
    public class DialogueNode
    {
        public string id = Guid.NewGuid().ToString("N");
        public DialogueNodeType type;
        public DialogueSpeaker speaker;

        [TextArea(2, 5)]
        public string text;

        public bool pending = true;
        public string nextId;
        public string trueId;
        public string falseId;

        public DialogueConditionSet conditions =
            new DialogueConditionSet();

        public List<DialogueChoice> choices =
            new List<DialogueChoice>();

        public Vector2 graphPosition;
    }

    public sealed class DialogueConversation : ScriptableObject
    {
        public string id;
        public string title;
        public DialogueCharacter character;
        public int priority;
        public bool draft = true;
        public string startId;

        public DialogueConditionSet conditions =
            new DialogueConditionSet();

        public List<DialogueNode> nodes =
            new List<DialogueNode>();

        public DialogueNode FindNode(string nodeId)
        {
            return nodes.Find(n =>
                n != null && n.id == nodeId
            );
        }
    }
}
