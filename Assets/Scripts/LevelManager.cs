using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para recargar el nivel
using TMPro; // Necesario para usar los textos modernos de Unity

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Estado del Nivel")]
    public float currentTime = 0f;
    public bool isLevelActive = false;
    private bool isLevelFinished = false;

    [Header("Interfaz de Victoria")]
    [Tooltip("El Panel de UI que contiene el mensaje de victoria y el botón")]
    public GameObject finishPanel;
    [Tooltip("El texto donde se mostrará el tiempo final")]
    public TextMeshProUGUI timeText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Ocultar el panel de victoria al empezar el nivel
        if (finishPanel != null)
        {
            finishPanel.SetActive(false);
        }

        // Asegurarnos de que el tiempo fluye normal por si venimos de un reinicio
        Time.timeScale = 1f;
    }

    public void StartLevel()
    {
        isLevelActive = true;
        currentTime = 0f;
        Debug.Log("¡Nivel Iniciado!");
    }

    public void EndLevel()
    {
        // Evitamos que se ejecute dos veces
        if (!isLevelActive || isLevelFinished) return;

        isLevelActive = false;
        isLevelFinished = true;

        // 1. Mostrar la UI de victoria y actualizar el texto
        if (finishPanel != null) finishPanel.SetActive(true);
        if (timeText != null) timeText.text = $"TIEMPO FINAL\n{currentTime:F2} s";

        // 2. Liberar el cursor para poder pulsar el botón de reinicio
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. Apagar el script del avión para que no vuelva a bloquear el ratón
        PaperPlaneController plane = FindObjectOfType<PaperPlaneController>();
        if (plane != null) plane.enabled = false;

        // 4. Congelar el tiempo del juego
        Time.timeScale = 0f;
    }

    // Este método lo conectaremos al botón de la UI
    public void RestartLevel()
    {
        // Restauramos el tiempo a la normalidad antes de recargar
        Time.timeScale = 1f;

        // Recargamos la escena actual
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void Update()
    {
        if (isLevelActive && !isLevelFinished)
        {
            currentTime += Time.deltaTime;
        }
    }
}