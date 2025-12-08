using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private CanvasGroup uiCanvasGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private GameObject overlayBackground;

    [Header("Settings")]
    [SerializeField] private float typingSpeed = 0.04f; // Velocidad de escritura (0.02 = rápido, 0.1 = lento)

    [Header("Content")]
    [TextArea(3, 5)]
    public string[] tutorialSteps;
    public string[] tutorialTitles;

    private int currentStepIndex = 0;

    // Variables efecto de escritura
    private bool isTyping = false;
    private string currentFullText = "";
    private Coroutine typingCoroutine;

    private void Start()
    {
        // --- Datos genéricos ---
        int totalPasos = 5;
        tutorialTitles = new string[totalPasos];
        tutorialSteps = new string[totalPasos];

        // Este for será reemplazado por otro que leea desde el archivo del guion del juego
        for (int i = 0; i < totalPasos; i++)
        {
            tutorialTitles[i] = $"Tutorial - Paso {i + 1}";
            tutorialSteps[i] = $"Esta es la descripción genérica para el paso número {i + 1}. Fíjate cómo el texto aparece letra por letra. Si pulsas una tecla ahora, se completará de golpe.";
        }

        // Estado inicial invisible
        uiCanvasGroup.alpha = 0;
        overlayBackground.SetActive(false);
        gameObject.SetActive(false);

        //Invoke(nameof(StartTutorial), 1f);
    }

    private void Update()
    {
        // Lógica de interacción: Esperamos a que la caja sea visible (Alpha > 0.9)
        if (gameObject.activeInHierarchy && uiCanvasGroup.alpha >= 0.9f)
        {
            if (Input.anyKeyDown)
            {
                if (isTyping)
                {
                    // CASO A: Si el texto se está escribiendo -> Lo completamos de golpe
                    CompleteTextImmediately();
                }
                else
                {
                    // CASO B: Si ya terminó de escribirse -> Pasamos a la siguiente página
                    NextStep();
                }
            }
        }
    }

    public void StartTutorial()
    {
        currentStepIndex = 0;
        overlayBackground.SetActive(true);
        gameObject.SetActive(true);

        StartCoroutine(FadeIn());

        // Iniciamos el contenido del primer paso
        UpdateContent();
    }

    private void NextStep()
    {
        // Solo avanzamos si NO estamos escribiendo
        if (!isTyping)
        {
            currentStepIndex++;

            if (currentStepIndex < tutorialSteps.Length)
            {
                UpdateContent();
            }
            else
            {
                StartCoroutine(FadeOut());
            }
        }
    }

    private void UpdateContent()
    {
        // 1. Actualizar Título (directo, sin animación)
        if (currentStepIndex < tutorialTitles.Length)
            titleText.text = tutorialTitles[currentStepIndex];

        // 2. Preparar el Texto del Cuerpo
        currentFullText = tutorialSteps[currentStepIndex]; // Guardamos el texto completo en memoria
        bodyText.text = "";                                // Limpiamos la caja visualmente

        // 3. Iniciar la animación de escritura
        // Si había una animación anterior ejecutándose, la paramos
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeTextEffect(currentFullText));
    }

    // --- Corrutina de Máquina de Escribir ---
    IEnumerator TypeTextEffect(string textToType)
    {
        isTyping = true;

        // Convertimos el string a array de caracteres y los ponemos uno a uno
        foreach (char letter in textToType.ToCharArray())
        {
            bodyText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false; // Marcamos como finalizado cuando acaba el bucle
    }

    // --- Función para saltar la animación ---
    private void CompleteTextImmediately()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        bodyText.text = currentFullText; // Ponemos todo el texto
        isTyping = false; // Marcamos como finalizado
    }

    // --- Animaciones Visuales ---
    IEnumerator FadeIn()
    {
        float duration = 0.4f;
        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            uiCanvasGroup.alpha = Mathf.Lerp(0f, 1f, currentTime / duration);
            transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, currentTime / duration);
            yield return null;
        }
        uiCanvasGroup.alpha = 1f;
        uiCanvasGroup.interactable = true;
        uiCanvasGroup.blocksRaycasts = true;
    }

    IEnumerator FadeOut()
    {
        uiCanvasGroup.interactable = false;
        float duration = 0.3f;
        float currentTime = 0f;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            uiCanvasGroup.alpha = Mathf.Lerp(1f, 0f, currentTime / duration);
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.9f, currentTime / duration);
            yield return null;
        }

        gameObject.SetActive(false);
        overlayBackground.SetActive(false);
    }
}