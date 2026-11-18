
using UnityEngine;
using System.Collections.Generic;

namespace PiedrasIncr.Dialogue
{
    public sealed class DialogueCatalog : ScriptableObject
    {
        public List<DialogueCharacter> characters =
            new List<DialogueCharacter>();

        public List<DialogueConversation> conversations =
            new List<DialogueConversation>();
    }

    public sealed class DialogueState
    {
        public int coins;
        public bool hammer;
        public bool launcher;
        public bool tornado;
        public bool processor;
        public int bootsLevel;
        public int jetpackLevel;

        public bool simulation = true;
        public bool hasWallet;
        public bool hasEquipment;
        public bool hasProcessor;
        public bool hasPlayer;

        public static DialogueState FromGame()
        {
            var state = new DialogueState();

            var wallet =
                Object.FindFirstObjectByType<Cartera>();

            var equipment =
                Object.FindFirstObjectByType<GestorEquipamiento>();

            var processing =
                Object.FindFirstObjectByType<MaquinaErosion>();

            var player =
                Object.FindFirstObjectByType<MovimientoPersonaje>();

            state.simulation = false;
            state.hasWallet = wallet != null;
            state.hasEquipment = equipment != null;
            state.hasProcessor = processing != null;
            state.hasPlayer = player != null;

            if (wallet != null)
                state.coins = wallet.Monedas;

            if (equipment != null)
            {
                state.hammer =
                    equipment.MartilloDesbloqueado();

                state.launcher =
                    equipment.ArmaLanzadoraDesbloqueada();

                state.tornado =
                    equipment.TorbellinoDesbloqueado();
            }

            if (processing != null)
                state.processor = processing.ProcesadoraDesbloqueada;

            if (player != null)
            {
                state.bootsLevel = player.NivelBotas;
                state.jetpackLevel = player.NivelJetpack;
            }

            return state;
        }
    }

    public static class DialogueLogicEngine
    {
        public static bool Evaluate(
            DialogueConditionSet set,
            DialogueState state,
            out string error,
            out bool passed)
        {
            error = null;
            passed = true;

            if (set == null ||
                set.conditions == null ||
                set.conditions.Count == 0)
                return true;

            bool result = set.logic == DialogueLogic.AND;

            foreach (DialogueCondition condition in set.conditions)
            {
                if (condition == null)
                    continue;

                bool value;

                if (!state.simulation)
                {
                    bool missing =
                        condition.type == DialogueConditionType.CoinsAtLeast
                            && !state.hasWallet
                        ||
                        (
                            condition.type == DialogueConditionType.HammerUnlocked
                            ||
                            condition.type == DialogueConditionType.LauncherUnlocked
                            ||
                            condition.type == DialogueConditionType.TornadoUnlocked
                        )
                            && !state.hasEquipment
                        ||
                        condition.type == DialogueConditionType.ProcessorUnlocked
                            && !state.hasProcessor
                        ||
                        (
                            condition.type == DialogueConditionType.BootsLevelAtLeast
                            ||
                            condition.type == DialogueConditionType.JetpackLevelAtLeast
                        )
                            && !state.hasPlayer;

                    if (missing)
                    {
                        error =
                            "Falta el sistema del juego para evaluar "
                            + condition.type;

                        passed = false;
                        return false;
                    }
                }

                switch (condition.type)
                {
                    case DialogueConditionType.CoinsAtLeast:
                        value = state.coins >= condition.amount;
                        break;

                    case DialogueConditionType.HammerUnlocked:
                        value = state.hammer;
                        break;

                    case DialogueConditionType.LauncherUnlocked:
                        value = state.launcher;
                        break;

                    case DialogueConditionType.TornadoUnlocked:
                        value = state.tornado;
                        break;

                    case DialogueConditionType.ProcessorUnlocked:
                        value = state.processor;
                        break;

                    case DialogueConditionType.BootsLevelAtLeast:
                        value = state.bootsLevel >= condition.amount;
                        break;

                    case DialogueConditionType.JetpackLevelAtLeast:
                        value = state.jetpackLevel >= condition.amount;
                        break;

                    default:
                        error =
                            "Condición pendiente de integración: "
                            + condition.type;

                        passed = false;
                        return false;
                }

                if (condition.not)
                    value = !value;

                if (set.logic == DialogueLogic.AND)
                    result &= value;
                else
                    result |= value;
            }

            if (set.not)
                result = !result;

            passed = result;
            return true;
        }

        public static DialogueConversation Select(
            DialogueCatalog catalog,
            DialogueCharacter character,
            DialogueState state,
            out string diagnostic)
        {
            diagnostic =
                "No hay ninguna conversación disponible.";

            if (catalog == null || character == null)
                return null;

            DialogueConversation best = null;

            foreach (DialogueConversation conversation
                     in catalog.conversations)
            {
                if (conversation == null ||
                    conversation.character != character)
                    continue;

                if (!Evaluate(
                        conversation.conditions,
                        state,
                        out string error,
                        out bool passed))
                {
                    diagnostic = error;
                    continue;
                }

                if (!passed)
                    continue;

                if (best == null ||
                    conversation.priority > best.priority ||
                    (
                        conversation.priority == best.priority &&
                        string.CompareOrdinal(
                            conversation.id,
                            best.id
                        ) < 0
                    ))
                {
                    best = conversation;
                }
            }

            if (best != null)
                diagnostic = "Seleccionada: " + best.title;

            return best;
        }

        public static DialogueNode Resolve(
            DialogueConversation conversation,
            string nodeId,
            DialogueState state,
            out string diagnostic)
        {
            diagnostic = null;

            for (int i = 0; i < 64; i++)
            {
                DialogueNode node =
                    conversation != null
                        ? conversation.FindNode(nodeId)
                        : null;

                if (node == null)
                {
                    diagnostic =
                        "Nodo inexistente: "
                        + (nodeId ?? "(vacío)");

                    return null;
                }

                if (node.type != DialogueNodeType.Branch)
                    return node;

                if (!Evaluate(
                        node.conditions,
                        state,
                        out string error,
                        out bool passed))
                {
                    diagnostic = error;
                    return null;
                }

                nodeId = passed
                    ? node.trueId
                    : node.falseId;
            }

            diagnostic =
                "Posible ciclo infinito de condiciones.";

            return null;
        }
    }
}
