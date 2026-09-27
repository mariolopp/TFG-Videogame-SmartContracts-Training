using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Todo lo "informativo" de la pantalla: puntos, vidas, reloj, mensaje de feedback y pantalla final.
// Todos los campos son opcionales
public class ContractHUD : MonoBehaviour
{
    [SerializeField] private ContractApproverConfig config;

    [Header("Marcadores")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text livesText;
    [Tooltip("Iconos de corazón: se encienden tantos como vidas")]
    [SerializeField] private GameObject[] lifeIcons;
    [SerializeField] private TMP_Text remainingText;

    [Header("Temporizador")]
    [SerializeField] private GameObject timerRoot;
    [Tooltip("Imagen con Image Type = Filled")]
    [SerializeField] private Image timerFill;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Color timerNormalColor = Color.white;
    [SerializeField] private Color timerDangerColor = new Color(0.9f, 0.3f, 0.3f);
    [Range(0f, 1f)] [SerializeField] private float dangerThreshold = 0.25f;

    [Header("Mensaje de feedback")]
    [SerializeField] private CanvasGroup toastGroup;
    [SerializeField] private TMP_Text toastText;
    [SerializeField] private Image toastBackground;

    [Header("Destello de pantalla (opcional)")]
    [SerializeField] private Image flashImage;
    [Range(0f, 1f)] [SerializeField] private float flashAlpha = 0.25f;

    [Header("Pantalla final")]
    [SerializeField] private GameObject endScreen;
    [SerializeField] private TMP_Text endTitleText;
    [SerializeField] private TMP_Text endScoreText;

    private Coroutine toastCo, flashCo;

    private void Awake()
    {
        if (toastGroup != null) { toastGroup.alpha = 0f; toastGroup.blocksRaycasts = false; }
        if (flashImage != null)
        {
            flashImage.raycastTarget = false;
            Color c = flashImage.color; c.a = 0f; flashImage.color = c;
        }
    }

    public void SetScore(int score)
    {
        if (scoreText != null) scoreText.text = score.ToString();
    }

    public void SetLives(int lives)
    {
        if (livesText != null) livesText.text = "x" + lives;
        if (lifeIcons != null)
            for (int i = 0; i < lifeIcons.Length; i++)
                if (lifeIcons[i] != null) lifeIcons[i].SetActive(i < lives);
    }

    public void SetRemaining(int remaining, int total)
    {
        if (remainingText != null) remainingText.text = $"{remaining}/{total}";
    }

    public void SetTimerVisible(bool visible)
    {
        if (timerRoot != null) timerRoot.SetActive(visible);
    }

    public void SetTimer(float remaining, float max)
    {
        float ratio = max > 0f ? Mathf.Clamp01(remaining / max) : 0f;
        Color c = ratio <= dangerThreshold ? timerDangerColor : timerNormalColor;
        if (timerFill != null) { timerFill.fillAmount = ratio; timerFill.color = c; }
        if (timerText != null) { timerText.text = Mathf.CeilToInt(remaining).ToString(); timerText.color = c; }
    }

    public void ShowToast(string message, bool correct)
    {
        if (toastGroup == null || toastText == null) return;
        if (toastCo != null) StopCoroutine(toastCo);
        toastCo = StartCoroutine(ToastRoutine(message, correct));
    }

    public void Flash(bool correct)
    {
        if (flashImage == null) return;
        if (flashCo != null) StopCoroutine(flashCo);
        flashCo = StartCoroutine(FlashRoutine(correct ? config.correctColor : config.wrongColor));
    }

    public void ShowEnd(bool survived, int score, int correct, int answered)
    {
        SetTimerVisible(false);
        if (endScreen != null) endScreen.SetActive(true);
        if (endTitleText != null) endTitleText.text = survived ? "¡Turno completado!" : "Te han vaciado la wallet";
        if (endScoreText != null) endScoreText.text = $"Puntos: {score}\nAciertos: {correct}/{answered}";
    }

    private IEnumerator ToastRoutine(string message, bool correct)
    {
        toastText.text = message;
        if (toastBackground != null)
        {
            Color c = correct ? config.correctColor : config.wrongColor;
            toastBackground.color = new Color(c.r, c.g, c.b, 0.9f);
        }
        yield return Fade(toastGroup, toastGroup.alpha, 1f, 0.15f);
        yield return new WaitForSeconds(config.toastDuration);
        yield return Fade(toastGroup, 1f, 0f, 0.3f);
    }

    private IEnumerator FlashRoutine(Color c)
    {
        float t = 0f;
        const float duration = 0.35f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(flashAlpha, 0f, t / duration);
            flashImage.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        flashImage.color = new Color(c.r, c.g, c.b, 0f);
    }

    private static IEnumerator Fade(CanvasGroup g, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            g.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        g.alpha = to;
    }
}
