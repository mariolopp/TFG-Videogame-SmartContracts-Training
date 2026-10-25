using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/*
 * ======================================================================================
 * GESTOR DE DIÁLOGOS DESDE JSON
 * ======================================================================================
 * USO:
 * DialogManager.Instance.StartDialog(miArchivoJson, miArchivoJsonEn, () => {
 * Debug.Log("El diálogo ha terminado, empieza el juego.");
 * });
 * Con las dos versiones (español / inglés), si se cambia el idioma a mitad del diálogo
 * la línea actual se reescribe en el nuevo idioma. Ambos JSON deben tener las mismas líneas.
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
    public string murmullo; // Id del SFX que suena al cambiar de personaje. Solo se lee del JSON en español
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

// --- MURMULLO POR DEFECTO DE CADA PERSONAJE (si su línea del JSON no trae "murmullo") ---
[Serializable]
public class CharacterMurmur
{
    public string characterId;
    public string sfxId;
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

    [Header("Sonido")]
    [Tooltip("Suena en bucle mientras se escribe el texto (vacío = sin sonido)")]
    [SerializeField] private string typingSfxId = "escritura";
    [SerializeField] private CharacterMurmur[] defaultMurmurs =
    {
        new CharacterMurmur { characterId = "profe", sfxId = "risaProfe1" },
        new CharacterMurmur { characterId = "jugador", sfxId = "entendimientoChica3" },
    };

    // --- Variables Internas ---
    private bool isTyping = false;
    private string currentFullText = "";
    private Coroutine typingCoroutine;
    private DialogLine[] pendingDialogues;
    private DialogLine[] dialoguesEs, dialoguesEn;
    private int currentStepIndex = 0;
    private string lastSpeakerId;   // Se mantiene entre diálogos de la misma escena
    private AudioSource typingLoop;

    private void Start()
    {
        uiCanvasGroup.alpha = 0f;
        uiCanvasGroup.interactable = false;
        uiCanvasGroup.blocksRaycasts = false;
        overlayBackground.SetActive(false);
        gameObject.SetActive(false);
    }

    private void OnEnable() => LanguageManager.OnLanguageChanged += OnLanguageChanged;
    private void OnDisable()
    {
        LanguageManager.OnLanguageChanged -= OnLanguageChanged;
        StopTypingSfx();
    }

    // --- API PÚBLICA ---
    public void StartDialog(TextAsset jsonFile, Action onFinishedAction = null) => StartDialog(jsonFile, null, onFinishedAction);

    public void StartDialog(TextAsset jsonEs, TextAsset jsonEn, Action onFinishedAction = null)
    {
        StartDialog(Parse(jsonEs), Parse(jsonEn), onFinishedAction);
    }

    public void StartDialog(DialogLine[] linesEs, DialogLine[] linesEn, Action onFinishedAction = null)
    {
        if (linesEs == null || linesEs.Length == 0)
        {
            Debug.LogWarning("DialogManager: El JSON está vacío o mal formateado.");
            return;
        }
        if (linesEn != null && linesEn.Length != linesEs.Length)
        {
            Debug.LogWarning($"DialogManager: la versión en inglés tiene {linesEn.Length} líneas y la española {linesEs.Length}. Se usará solo la española.");
            linesEn = null;
        }

        onDialogFinished = onFinishedAction;
        dialoguesEs = linesEs;
        dialoguesEn = linesEn;
        pendingDialogues = LanguageManager.Pick(dialoguesEs, dialoguesEn);
        currentStepIndex = 0;

        OpenWindow();
        SetContent(pendingDialogues[0]);
    }

    private static DialogLine[] Parse(TextAsset json)
    {
        if (json == null) return null;
        // Leemos el JSON y lo convertimos a objetos de C#
        DialogData data = JsonUtility.FromJson<DialogData>(json.text);
        return data?.dialogues;
    }

    // Cambia al otro idioma y reescribe la línea actual sin volver a lanzar su evento
    private void OnLanguageChanged(Language language)
    {
        if (pendingDialogues == null) return;
        pendingDialogues = LanguageManager.Pick(dialoguesEs, dialoguesEn);
        if (currentStepIndex >= pendingDialogues.Length) return;

        DialogLine line = pendingDialogues[currentStepIndex];
        if (string.IsNullOrEmpty(line.text)) return;
        titleText.text = line.characterName;
        currentFullText = line.text;
        CompleteTextImmediately();
    }
    private static bool IsPointerOnSkipDialogOrLanguageButton()
    {
        if (EventSystem.current == null) return false;

        var pointer = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);

        foreach (var hit in hits)
        {
            var go = hit.gameObject;
            if (go == null) continue;

            // Botón de idioma
            if (go.GetComponentInParent<LanguageToggleButton>() != null)
                return true;

            // Botón de saltar diálogo
            if (go.CompareTag("SkipDialogButton"))
                return true;

            // Si el clic está sobre el propio diálogo, tampoco lo bloqueamos
            if (go.GetComponentInParent<DialogManager>() != null)
                return false;
        }

        return false;
    }

    // --- LÓGICA DE CONTROL ---
    private void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        // Cualquier tecla avanza el diálogo.
        // Si el ratón está sobre "Saltar diálogo" o "Idioma", no se avanza.
        if (Input.anyKeyDown)
        {
            if (!IsPointerOnSkipDialogOrLanguageButton())
                HandleInput();
            return;
        }

        // Clic del ratón: solo bloquea si está encima del botón de saltar diálogo o idioma.
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOnSkipDialogOrLanguageButton())
                return;

            HandleInput();
        }
    }

    private void HandleInput()
    {
        if (isAnimatingUI)
            return;

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
            PlayMurmurIfNewSpeaker(line);
        
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

    // El murmullo solo suena en el primer bocadillo de cada personaje: si habla varios seguidos no se repite
    private void PlayMurmurIfNewSpeaker(DialogLine line)
    {
        if (line.characterId == lastSpeakerId) return;
        lastSpeakerId = line.characterId;

        // El murmullo es lógica, no texto: se lee de la versión española aunque se muestre la inglesa
        DialogLine lineEs = dialoguesEs != null && currentStepIndex < dialoguesEs.Length ? dialoguesEs[currentStepIndex] : line;
        string id = lineEs.murmullo;
        if (string.IsNullOrEmpty(id) && defaultMurmurs != null)
        {
            foreach (var m in defaultMurmurs)
                if (m.characterId == line.characterId) { id = m.sfxId; break; }
        }
        if (!string.IsNullOrEmpty(id) && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(id);
    }

    private void StartTypingSfx()
    {
        if (typingLoop != null || string.IsNullOrEmpty(typingSfxId) || AudioManager.Instance == null) return;
        typingLoop = AudioManager.Instance.PlayLoopSFX(typingSfxId, this);
    }

    private void StopTypingSfx()
    {
        if (typingLoop != null && AudioManager.Instance != null) AudioManager.Instance.StopLoopSFX(typingLoop);
        typingLoop = null;
    }

    public void CompleteTextImmediately()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        StopTypingSfx();
        bodyText.text = currentFullText;
        bodyText.maxVisibleCharacters = 99999;
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
        StopTypingSfx();
        StartCoroutine(FadeOut());
    }

    // --- CORRUTINAS (Animaciones) ---
    IEnumerator TypeTextEffect(string textToType)
    {
        isTyping = true;
        // Se asigna el texto completo y se revela carácter a carácter, para que las etiquetas
        // de rich text (sprites, size...) no aparezcan a medio escribir
        bodyText.text = textToType;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();
        int totalCaracteres = bodyText.textInfo.characterCount;
        if (totalCaracteres > 0) StartTypingSfx();
        for (int i = 1; i <= totalCaracteres; i++)
        {
            bodyText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }
        StopTypingSfx();
        isTyping = false;
    }
    // --- NUEVO: Permite saltar el diálogo completo desde fuera ---
    public void StopDialog()
    {
        StopAllCoroutines();
        typingCoroutine = null;
        StopTypingSfx();
        isTyping = false;
        isAnimatingUI = false;

        if (uiCanvasGroup != null)
        {
            uiCanvasGroup.alpha = 0f;
            uiCanvasGroup.interactable = false;
            uiCanvasGroup.blocksRaycasts = false;
        }

        if (overlayBackground != null)
            overlayBackground.SetActive(false);

        gameObject.SetActive(false);

        pendingDialogues = null;
        currentStepIndex = 0;

        if (onDialogFinished != null)
        {
            var callback = onDialogFinished;
            onDialogFinished = null;
            callback.Invoke();
        }
    }
    // --- NUEVO: pausa real. No aborta el diálogo, solo detiene el "typing" y oculta la ventana ---
    public void PauseDialog()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        StopTypingSfx();
        isTyping = false;
        isAnimatingUI = true;

        uiCanvasGroup.alpha = 0f;
        uiCanvasGroup.interactable = false;
        uiCanvasGroup.blocksRaycasts = false;

        if (overlayBackground != null)
            overlayBackground.SetActive(false);
    }

    public void ResumeDialogAndAdvance()
    {
        isAnimatingUI = false;

        uiCanvasGroup.alpha = 1f;
        uiCanvasGroup.interactable = true;
        uiCanvasGroup.blocksRaycasts = true;

        if (overlayBackground != null)
            overlayBackground.SetActive(true);

        AdvanceSequence();
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
        // El overlay va antes: desactivar el gameObject detiene esta corrutina y no llegaría a ejecutarse
        overlayBackground.SetActive(false);
        gameObject.SetActive(false);
    }
}