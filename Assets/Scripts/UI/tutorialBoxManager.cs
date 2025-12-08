using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * ======================================================================================
 * GUÍA RÁPIDA DE USO (MINI-API TUTORIAL MANAGER)
 * ======================================================================================
 * Este script es un Singleton. Se puede acceder a él desde cualquier script del juego
 * sin necesidad de arrastrar referencias en el Inspector.
 * --------------------------------------------------------------------------
 * * CASO 1: MOSTRAR UN MENSAJE ÚNICO
 * --------------------------------------------------------------------------
 * TutorialManager.Instance.ShowMessage("¡Objeto Encontrado!", "Has recibido una Poción.");
 * 
 * * CASO 2: INICIAR SECUENCIA DE TUTORIAL
 * --------------------------------------------------------------------------
 * string[] titulos = { "Personaje que habla", "Advertencia", "Pista" };
 * string[] pasos   = { "Hola viajero...", "Usa WASD...", "Click para atacar..." };
 * * TutorialManager.Instance.StartSequence(titulos, pasos);
 * ======================================================================================
 */

public class TutorialManager : MonoBehaviour
{
    // --- 1. SINGLETON: Para acceder desde cualquier script ---
    public static TutorialManager Instance;
    private Action onWindowClosed; // Acción opcional al cerrar la ventana

    private void Awake()
    {
        // Si ya existe una instancia y no soy yo, me destruyo para evitar duplicados
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // Opcional: DontDestroyOnLoad(gameObject); // Descomentar para que persista al cambiar de escena
        }
    }

    [Header("UI Components")]
    [SerializeField] private CanvasGroup uiCanvasGroup;     // Control de visibilidad e interactividad
    [SerializeField] private TMP_Text titleText;            
    [SerializeField] private TMP_Text bodyText;             
    [SerializeField] private GameObject overlayBackground;  // Fondo oscuro detrás de la ventana

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.04f; // Velocidad de escritura

    // --- Variables de Estado Interno ---
    private bool isTyping = false;
    private string currentFullText = "";
    private Coroutine typingCoroutine;

    // --- NUEVAS Variables para controlar el Modo (Secuencia vs Mensaje Único) ---
    private bool isSequenceMode = false;
    private string[] pendingTitles;
    private string[] pendingSteps;
    private int currentStepIndex = 0;

    private void Start()
    {
        // IMPORTANTE: Ya no generamos datos genéricos aquí.
        // El Manager empieza "dormido" y espera a que alguien lo llame.

        // Nos aseguramos de empezar invisibles y desactivados
        uiCanvasGroup.alpha = 0;
        uiCanvasGroup.interactable = false;
        uiCanvasGroup.blocksRaycasts = false;
        overlayBackground.SetActive(false);
        gameObject.SetActive(false);
    }

    // --- 2. API PÚBLICA (Los métodos que usarán otros scripts) ---

    // Mensaje emergente, se cierra al pulsar cualquier tecla. Accion opcional al cerrarse
    public void ShowMessage(string title, string content, Action onCloseAction = null)
    {
        isSequenceMode = false; // Marcamos que NO es una secuencia
        onWindowClosed = onCloseAction;
        OpenWindow();
        SetContent(title, content);
    }

    /// <summary>
    /// Inicia una secuencia de varios pasos (Tutorial).
    /// Útil para: Explicaciones largas, Intro del juego, etc.
    /// </summary>
    public void StartSequence(string[] titles, string[] steps)
    {
        if (titles.Length != steps.Length)
        {
            Debug.LogWarning("TutorialManager: Los arrays de Títulos y Pasos no tienen la misma longitud.");
            return;
        }

        isSequenceMode = true; // Marcamos que SÍ es una secuencia
        pendingTitles = titles;
        pendingSteps = steps;
        currentStepIndex = 0;

        OpenWindow();
        SetContent(pendingTitles[0], pendingSteps[0]);
    }

    // --- 3. LÓGICA DE CONTROL (Update y Decisiones) ---

    private void Update()
    {
        // Solo detectamos input si la ventana está activa y visible
        if (gameObject.activeInHierarchy && uiCanvasGroup.alpha >= 0.9f)
        {
            if (Input.anyKeyDown)
            {
                HandleInput();
            }
        }
    }

    private void HandleInput()
    {
        if (isTyping)
        {
            // CASO A: El texto se está escribiendo -> El usuario quiere verlo completo YA.
            CompleteTextImmediately();
        }
        else
        {
            // CASO B: El texto ya terminó -> El usuario quiere avanzar o cerrar.
            if (isSequenceMode)
            {
                // Si es un tutorial largo, intentamos ir al siguiente paso
                AdvanceSequence();
            }
            else
            {
                // Si es un mensaje único, cerramos la ventana
                CloseWindow();
                if (onWindowClosed != null)
                {
                    onWindowClosed.Invoke(); // <--- EJECUTAMOS LA ACCIÓN (Ej: Abrir Wallet)
                    onWindowClosed = null;   // Limpiamos para que no se repita
                }
            }
        }
    }

    private void AdvanceSequence()
    {
        currentStepIndex++;

        // ¿Quedan más pasos en el array?
        if (currentStepIndex < pendingSteps.Length)
        {
            SetContent(pendingTitles[currentStepIndex], pendingSteps[currentStepIndex]);
        }
        else
        {
            // Fin de la secuencia
            CloseWindow();
        }
    }

    // --- 4. FUNCIONES VISUALES Y DE TEXTO (Lo que ya tenías) ---

    private void OpenWindow()
    {
        gameObject.SetActive(true);
        overlayBackground.SetActive(true);

        // Reiniciamos animaciones previas si las hubiera
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    private void CloseWindow()
    {
        // Detenemos escritura si estaba ocurriendo para evitar errores
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        StartCoroutine(FadeOut());
    }

    private void SetContent(string title, string content)
    {
        titleText.text = title;
        currentFullText = content; // Guardamos el texto completo en memoria
        bodyText.text = "";        // Limpiamos visualmente

        // Iniciar efecto de escritura
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeTextEffect(currentFullText));
    }

    private void CompleteTextImmediately()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        bodyText.text = currentFullText; // Ponemos todo el texto de golpe
        isTyping = false;                // Marcamos como terminado
    }

    // --- 5. CORRUTINAS (Animaciones) ---

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
        float duration = 0.3f;
        float currentTime = 0f;

        // Aseguramos interactividad al final
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
    }

    IEnumerator FadeOut()
    {
        uiCanvasGroup.interactable = false;
        uiCanvasGroup.blocksRaycasts = false;

        float duration = 0.25f;
        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float progress = currentTime / duration;

            uiCanvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.9f, progress); // Efecto inverso

            yield return null;
        }

        gameObject.SetActive(false);
        overlayBackground.SetActive(false);
    }
}