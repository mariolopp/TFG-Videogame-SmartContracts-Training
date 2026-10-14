using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Transición entre escenas con fundido a negro. Persiste entre escenas para hacer
/// también el fundido de entrada en la escena nueva. No hay que colocarlo en ninguna
/// escena: se crea solo la primera vez que se accede a Instance.
/// Uso: SceneTransition.Instance.LoadScene("NombreEscena");
/// </summary>
[DisallowMultipleComponent]
public class SceneTransition : MonoBehaviour
{
    private const float FadeDuration = 0.35f;

    private static SceneTransition instance;
    public static SceneTransition Instance
    {
        get
        {
            if (instance == null)
                instance = new GameObject(nameof(SceneTransition)).AddComponent<SceneTransition>();
            return instance;
        }
    }

    public bool IsTransitioning { get; private set; }

    private CanvasGroup fadeGroup;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
    }

    public void LoadScene(string sceneName)
    {
        if (IsTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        IsTransitioning = true;
        fadeGroup.blocksRaycasts = true; // bloquea clics en la UI durante la transición

        yield return Fade(0f, 1f);

        AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName);
        while (!loading.isDone) yield return null;

        yield return Fade(1f, 0f);

        fadeGroup.blocksRaycasts = false;
        IsTransitioning = false;
    }

    // Tiempo sin escalar: funciona aunque algún minijuego deje Time.timeScale a 0
    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / FadeDuration);
            fadeGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
    }

    // Canvas overlay con una imagen negra a pantalla completa, creado por código
    private void BuildOverlay()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000; // por encima de cualquier otro Canvas
        gameObject.AddComponent<GraphicRaycaster>();

        fadeGroup = gameObject.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;

        Image image = new GameObject("Fade").AddComponent<Image>();
        image.color = Color.black;
        RectTransform rect = image.rectTransform;
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}