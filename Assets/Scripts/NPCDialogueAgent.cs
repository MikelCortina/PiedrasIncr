
using UnityEngine;
using PiedrasIncr.Dialogue;

[DisallowMultipleComponent]
public sealed class NPCDialogueAgent : MonoBehaviour
{
    public DialogueCharacter character;
    public float interactionDistance = 6f;
    public Camera playerCamera;
    public MonoBehaviour cameraController;

    private DialogueConversation conversation;
    private DialogueNode currentNode;
    private DialogueState state;
    private MovimientoPersonaje playerMovement;
    private bool movementWasEnabled;
    private bool cameraWasEnabled;
    private bool looking;
    private bool cursorWasVisible;
    private CursorLockMode previousCursorLock;
    private string error;

    private void Update()
    {
        if (conversation != null)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Close();

            return;
        }

        if (playerCamera == null)
            playerCamera = Camera.main;

        var existingMenu = FindFirstObjectByType<MenuBotUI>();

        if (existingMenu != null && existingMenu.EstaAbierto())
        {
            looking = false;
            return;
        }

        looking = IsBeingLookedAt();

        if (looking && Input.GetKeyDown(KeyCode.E))
            Open();
    }

    private bool IsBeingLookedAt()
    {
        if (playerCamera == null)
            return false;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            interactionDistance,
            ~0,
            QueryTriggerInteraction.Collide
        );

        System.Array.Sort(
            hits,
            (a, b) => a.distance.CompareTo(b.distance)
        );

        foreach (RaycastHit hit in hits)
        {
            NPCDialogueAgent agent =
                hit.collider.GetComponentInParent<NPCDialogueAgent>();

            if (agent != null)
                return agent == this;

            if (!hit.collider.isTrigger)
                return false;
        }

        return false;
    }

    private void Open()
    {
        DialogueCatalog catalog =
            Resources.Load<DialogueCatalog>(
                "PiedrasDialogueCatalog"
            );

        state = DialogueState.FromGame();

        conversation = DialogueLogicEngine.Select(
            catalog,
            character,
            state,
            out error
        );

        if (conversation == null)
        {
            Debug.LogWarning(name + ": " + error);
            return;
        }

        currentNode = DialogueLogicEngine.Resolve(
            conversation,
            conversation.startId,
            state,
            out error
        );

        if (currentNode == null ||
            currentNode.type == DialogueNodeType.End)
        {
            Debug.LogWarning(
                "Dialogue: " + (error ?? "Conversación vacía")
            );

            conversation = null;
            return;
        }

        previousCursorLock = Cursor.lockState;
        cursorWasVisible = Cursor.visible;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        playerMovement =
            FindFirstObjectByType<MovimientoPersonaje>();

        if (playerMovement != null)
        {
            movementWasEnabled = playerMovement.enabled;
            playerMovement.enabled = false;
        }

        if (cameraController == null)
        {
            var botMenu = FindFirstObjectByType<MenuBotUI>();

            if (botMenu != null)
                cameraController =
                    botMenu.controladorMiradaJugador;
        }

        if (cameraController != null)
        {
            cameraWasEnabled = cameraController.enabled;
            cameraController.enabled = false;
        }
    }

    private void Jump(string nodeId)
    {
        currentNode = DialogueLogicEngine.Resolve(
            conversation,
            nodeId,
            state,
            out error
        );

        if (currentNode == null)
        {
            Debug.LogWarning("Dialogue: " + error);
            Close();
        }
        else if (currentNode.type == DialogueNodeType.End)
        {
            Close();
        }
    }

    private void Close()
    {
        if (conversation == null)
            return;

        conversation = null;
        currentNode = null;
        state = null;

        if (playerMovement != null)
            playerMovement.enabled = movementWasEnabled;

        if (cameraController != null)
            cameraController.enabled = cameraWasEnabled;

        Cursor.lockState = previousCursorLock;
        Cursor.visible = cursorWasVisible;
    }

    private void OnDisable()
    {
        Close();
    }

    private void OnGUI()
    {
        if (conversation == null)
        {
            if (!looking || character == null)
                return;

            GUI.Box(
                new Rect(
                    Screen.width / 2f - 135f,
                    Screen.height - 95f,
                    270f,
                    35f
                ),
                "E - Hablar con " + character.displayName
            );

            return;
        }

        float width = Mathf.Min(
            Screen.width - 32f,
            720f
        );

        float height =
            currentNode != null &&
            currentNode.type == DialogueNodeType.Choices
                ? 310f
                : 230f;

        GUILayout.BeginArea(
            new Rect(
                (Screen.width - width) * 0.5f,
                Screen.height - height - 24f,
                width,
                height
            ),
            GUI.skin.window
        );

        GUILayout.Label(
            character != null
                ? character.displayName
                : "NPC",
            GUI.skin.box
        );

        GUILayout.Space(8f);

        if (currentNode != null)
        {
            if (currentNode.type == DialogueNodeType.Line)
            {
                string speaker =
                    currentNode.speaker == DialogueSpeaker.Player
                        ? "Jugador"
                        : currentNode.speaker == DialogueSpeaker.Narrator
                            ? "Narrador"
                            : character.displayName;

                GUILayout.Label(speaker + ":");

                GUILayout.Label(
                    string.IsNullOrWhiteSpace(currentNode.text)
                        ? "[Texto pendiente]"
                        : currentNode.text,
                    GUILayout.MinHeight(80f)
                );

                if (GUILayout.Button(
                        "Continuar",
                        GUILayout.Height(34f)))
                {
                    Jump(currentNode.nextId);
                }
            }
            else if (currentNode.type == DialogueNodeType.Choices)
            {
                GUILayout.Label(
                    string.IsNullOrWhiteSpace(currentNode.text)
                        ? "Elige una respuesta:"
                        : currentNode.text
                );

                foreach (DialogueChoice choice in currentNode.choices)
                {
                    if (choice == null)
                        continue;

                    if (GUILayout.Button(
                            choice.text,
                            GUILayout.Height(32f)))
                    {
                        Jump(choice.nextId);
                        break;
                    }
                }
            }
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(
                "Cerrar conversación",
                GUILayout.Height(27f)))
        {
            Close();
        }

        GUILayout.EndArea();
    }
}
