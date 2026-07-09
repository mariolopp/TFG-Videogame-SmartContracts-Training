using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * ======================================================================================
 * GESTOR DE DIÁLOGOS DESDE JSON
 * ======================================================================================
 * USO:
 * DialogManager.Instance.StartDialog(miArchivoJson, () => {
 * Debug.Log("El diálogo ha terminado, empieza el juego.");
 * });
 * ======================================================================================
 */

// --- CLASES PARA LEER EL JSON ---
[Serializable]
public class DialogLine
{
    public string characterId;
    public string characterName;
    public string text;
    public string evento;   // Activar evento externo
}

[Serializable]
public class DialogData
{
    public DialogLine[] dialogues;
}

// --- CLASE PARA ASIGNAR FOTOS EN EL INSPECTOR ---
[Serializable]
public class CharacterPortrait
{
    public string characterId; // Ej: "profe", "jugador"
    public Sprite portraitSprite;
}

public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance;
    
    // Megáfono al que otros scripts pueden suscribirse para ejecutar acciones ante un string concreto
    public static event Action<string> OnDialogEvent;
    private Action onDialogFinished;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    [Header("UI Components")]
    [SerializeField] private CanvasGroup uiCanvasGroup;
    [SerializeField] private TMP_Text titleText;            // Nombre del personaje
    [SerializeField] private TMP_Text bodyText;             // Texto del diálogo
    [SerializeField] private Image portraitImage;           // Foto del personaje
    [SerializeField] private GameObject overlayBackground;
    private bool isAnimatingUI = false;                     // Evitar que se pueda pasar durante animaciones  

    [Header("Base de Datos de Personajes")]
    [Tooltip("Asigna aquí la ID del JSON y la foto correspondiente a cada personaje")]
    [SerializeField] private CharacterPortrait[] characterPortraits;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.04f;

    // --- Variables Internas ---
    private bool isTyping = false;
    private string currentFullText = "";
    private Coroutine typingCoroutine;
    private DialogLine[] pendingDialogues;
    private int currentStepIndex = 0;

    private void Start()
    {
        uiCanvasGroup.alpha = 0;
        uiCanvasGroup.interactable = false;
        uiCanvasGroup.blocksRaycasts = false;
        overlayBackground.SetActive(false);
        gameObject.SetActive(false);
    }

    // --- API PÚBLICA ---
    public void StartDialog(TextAsset jsonFile, Action onFinishedAction = null)
    {
        // Leemos el JSON y lo convertimos a objetos de C#
        DialogData data = JsonUtility.FromJson<DialogData>(jsonFile.text);

        if (data == null || data.dialogues.Length == 0)
        {
            Debug.LogWarning("DialogManager: El JSON está vacío o mal formateado.");
            return;
        }

        onDialogFinished = onFinishedAction;
        pendingDialogues = data.dialogues;
        currentStepIndex = 0;

        OpenWindow();
        SetContent(pendingDialogues[0]);
    }

    // --- LÓGICA DE CONTROL ---
    private void Update()
    {
        if (gameObject.activeInHierarchy)
        {
            // Detecta click de cualquier tecla o mouse
            if (Input.GetMouseButtonDown(0) || Input.anyKeyDown)
            {
                HandleInput();
            }
        }
    }

    private void HandleInput()
    {
        if (isAnimatingUI) return; // Evitar que se pueda pasar durante animaciones
        if (isTyping)
        {
            CompleteTextImmediately();
        }
        else
        {
            AdvanceSequence();
        }
    }

    private void AdvanceSequence()
    {
        currentStepIndex++;

        if (currentStepIndex < pendingDialogues.Length)
        {
            SetContent(pendingDialogues[currentStepIndex]);
        }
        else
        {
            CloseWindow();
            if (onDialogFinished != null)
            {
                onDialogFinished.Invoke();
                onDialogFinished = null;
            }
        }
    }
    // Permite a otros scripts bloquear o desbloquear la posibilidad de saltar el diálogo
    public void BloquearInteraccion(bool bloquear)
    {
        isAnimatingUI = bloquear;
    }

    // --- FUNCIONES VISUALES ---
    private void SetContent(DialogLine line)
    {
        // Si el JSON es un texto vacío, no se mostrará la ventana en ese paso.
        // esta pensado para si se marcan cosas en pantalla, que no las tape el texto.
        if (string.IsNullOrEmpty(line.text))
        {
            // Si no hay texto, volvemos la ventana invisible
            uiCanvasGroup.alpha = 0f;
            isTyping = false;
            bodyText.text = "";
        }
        else
        {
            // Si hay texto, nos aseguramos de que la ventana sea visible
            uiCanvasGroup.alpha = 1f;
            
            titleText.text = line.characterName;
            currentFullText = line.text;
            bodyText.text = "";
        
            // Buscar y asignar la foto del personaje basada en su characterId
            Sprite foundSprite = null;
            foreach (var portrait in characterPortraits)
            {
                if (portrait.characterId == line.characterId)
                {
                    foundSprite = portrait.portraitSprite;
                    break;
                }
            }

            if (foundSprite != null)
            {
                portraitImage.sprite = foundSprite;
                portraitImage.gameObject.SetActive(true);
            }
            else
            {
                // Si el personaje no tiene foto, ocultamos la imagen
                portraitImage.gameObject.SetActive(false);
            }

            if (typingCoroutine != null) StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeTextEffect(currentFullText));
        }
        // Mandar el evento al CinematicStarter. 
        // Si el JSON no tiene evento, le mandamos un texto vacío ("") para que sepa que debe apagar todo.
        if (OnDialogEvent != null)
        {
            OnDialogEvent.Invoke(line.evento ?? ""); 
        }
    }

    private void CompleteTextImmediately()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        bodyText.text = currentFullText;
        isTyping = false;
    }

    private void OpenWindow()
    {
        gameObject.SetActive(true);
        overlayBackground.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    private void CloseWindow()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        StartCoroutine(FadeOut());
    }

    // --- CORRUTINAS (Animaciones) ---
    IEnumerator TypeTextEffect(string textToType)
    {
        isTyping = true;
        foreach (char letter in textToType.ToCharArray())
        {
            bodyText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    IEnumerator FadeIn()
    {
        isAnimatingUI = true;
        float duration = 0.3f;
        float currentTime = 0f;
        uiCanvasGroup.interactable = true;
        uiCanvasGroup.blocksRaycasts = true;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float progress = currentTime / duration;
            uiCanvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
            transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, progress);
            yield return null;
        }
        uiCanvasGroup.alpha = 1f;
        isAnimatingUI = false;
    }

    IEnumerator FadeOut()
    {
        isAnimatingUI = true;
        uiCanvasGroup.interactable = false;
        uiCanvasGroup.blocksRaycasts = false;
        float duration = 0.25f;
        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float progress = currentTime / duration;
            uiCanvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.9f, progress);
            yield return null;
        }
        isAnimatingUI = false;
        gameObject.SetActive(false);
        overlayBackground.SetActive(false);
    }
}