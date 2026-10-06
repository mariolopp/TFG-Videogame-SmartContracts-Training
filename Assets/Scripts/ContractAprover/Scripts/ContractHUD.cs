using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Todo lo "informativo" de la pantalla: puntos, vidas, reloj, destello y pantalla final.
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

    [Header("Barra de vida extra")]
    [Tooltip("Imagen con Image Type = Filled. Se llena según se acerca la siguiente vida extra")]
    [SerializeField] private Image extraLifeFill;
    [SerializeField] private float extraLifeFillDuration = 0.4f;
    [SerializeField] private Color extraLifeNormalColor = new Color(0.3f, 0.85f, 0.4f);
    [Tooltip("Color del destello cuando la barra se completa y se gana la vida")]
    [SerializeField] private Color extraLifeFullColor = new Color(1f, 0.85f, 0.3f);

    [Header("Destello de pantalla (opcional)")]
    [SerializeField] private Image flashImage;
    [Range(0f, 1f)] [SerializeField] private float flashAlpha = 0.25f;

    [Header("Pantalla final")]
    [SerializeField] private GameObject endScreen;
    [SerializeField] private TMP_Text endTitleText;
    [SerializeField] private TMP_Text endScoreText;
    [SerializeField] private Button endCloseButton;

    [Header("Recompensa final (bolsas)")]
    [SerializeField] private TMP_Text endTimeText;
    [SerializeField] private TMP_Text endTimeBagsText;
    [SerializeField] private TMP_Text endPointsText;
    [SerializeField] private TMP_Text endPointsBagsText;
    [SerializeField] private TMP_Text endLivesText;
    [SerializeField] private TMP_Text endLivesBagsText;
    [SerializeField] private TMP_Text endTotalBagsText;
    [SerializeField] private float countDuration = 0.75f;
    [SerializeField] private float bagsDuration = 1f;
    [SerializeField] private float totalDuration = 2f;
    [Tooltip("Pausa entre una animación y la siguiente")]
    [SerializeField] private float stepPause = 0.25f;

    private Coroutine flashCo, extraLifeCo;
    private bool extraLifeInitialized;

    private void Awake()
    {
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
            {
                if (lifeIcons[i] == null) continue;
                // Si el corazón tiene HeartIcon, que se anime él solo
                if (lifeIcons[i].TryGetComponent(out HeartIcon heart)) heart.SetAlive(i < lives);
                else lifeIcons[i].SetActive(i < lives);
            }
    }

    // Mismo patrón que SetTimer pero animado: sube o baja hasta el nuevo valor.
    // Si se ha completado una vida, se llena hasta arriba, destella y empieza de 0 hasta el resto
    public void SetExtraLifeProgress(float progress, bool completed)
    {
        if (extraLifeFill == null) return;
        progress = Mathf.Clamp01(progress);
        if (extraLifeCo != null) StopCoroutine(extraLifeCo);

        // La primera vez (inicio de partida) se coloca sin animar
        if (!extraLifeInitialized || !isActiveAndEnabled)
        {
            extraLifeInitialized = true;
            extraLifeFill.fillAmount = progress;
            extraLifeFill.color = extraLifeNormalColor;
            return;
        }
        extraLifeCo = StartCoroutine(ExtraLifeRoutine(progress, completed));
    }

    public void SetRemaining(int remaining, int total)
    {
        if (remainingText != null) remainingText.text = $"{remaining}";
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

    public void Flash(bool correct)
    {
        if (flashImage == null) return;
        if (flashCo != null) StopCoroutine(flashCo);
        flashCo = StartCoroutine(FlashRoutine(correct ? config.correctColor : config.wrongColor));
    }

    public void ShowEnd(bool survived, int score, int correct, int answered, int lives, float timeLeft)
    {
        SetTimerVisible(false);
        if (endScreen != null) endScreen.SetActive(true);
        if (endTitleText != null) endTitleText.text = survived ? "¡Turno completado!" : "Te han vaciado la wallet";
        if (endScoreText != null) endScoreText.text = $"Puntos: {score}\nAciertos: {correct}/{answered}";

        int seconds = Mathf.FloorToInt(timeLeft);
        int bagsFromTime = Mathf.RoundToInt(seconds * config.bagsPerSecondLeft);
        int bagsFromScore = Mathf.RoundToInt(score * config.bagsPerPoint);
        int bagsFromLives = Mathf.RoundToInt(lives * config.bagsPerLife);
        int totalBags = bagsFromTime + bagsFromScore + bagsFromLives;

        if (AssetsManager.Instance != null) AssetsManager.Instance.AddUSD(totalBags);
        else Debug.LogWarning("[ContractApprover] No hay AssetsManager: las bolsas no se guardan.");

        // Se ponen a 0 antes de animar para que no se vea el valor del editor mientras esperan su turno
        foreach (var t in new[] { endTimeText, endTimeBagsText, endPointsText, endPointsBagsText,
                                  endLivesText, endLivesBagsText, endTotalBagsText })
            if (t != null) t.text = "x0";

        StartCoroutine(EndRewardRoutine(seconds, bagsFromTime, score, bagsFromScore, lives, bagsFromLives, totalBags));

        if (endCloseButton != null)
        {
            endCloseButton.onClick.RemoveAllListeners();
            endCloseButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
        }
    }

    // Cada fila en orden: primero la cantidad, luego sus bolsas. Al final el total de golpe
    private IEnumerator EndRewardRoutine(int seconds, int bagsFromTime, int score, int bagsFromScore,
                                         int lives, int bagsFromLives, int totalBags)
    {
        yield return AnimateNumber(endTimeText, seconds, countDuration);
        yield return AnimateNumber(endTimeBagsText, bagsFromTime, bagsDuration);
        yield return AnimateNumber(endPointsText, score, countDuration);
        yield return AnimateNumber(endPointsBagsText, bagsFromScore, bagsDuration);
        yield return AnimateNumber(endLivesText, lives, countDuration);
        yield return AnimateNumber(endLivesBagsText, bagsFromLives, bagsDuration);
        yield return AnimateNumber(endTotalBagsText, totalBags, totalDuration);
    }

    private IEnumerator AnimateNumber(TMP_Text text, float target, float duration)
    {
        if (text == null) yield break;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            text.text = $"x{Mathf.RoundToInt(Mathf.Lerp(0f, target, t / duration))}";
            yield return null;
        }
        text.text = $"x{Mathf.RoundToInt(target)}";
        yield return new WaitForSeconds(stepPause);
    }

    private IEnumerator ExtraLifeRoutine(float target, bool completed)
    {
        if (completed)
        {
            yield return AnimateFill(extraLifeFill.fillAmount, 1f);
            // Destello al completarse
            const float pulse = 0.35f;
            for (float t = 0f; t < pulse; t += Time.deltaTime)
            {
                extraLifeFill.color = Color.Lerp(extraLifeFullColor, extraLifeNormalColor, t / pulse);
                yield return null;
            }
            extraLifeFill.fillAmount = 0f;
        }
        extraLifeFill.color = extraLifeNormalColor;
        yield return AnimateFill(extraLifeFill.fillAmount, target);
        extraLifeCo = null;
    }

    private IEnumerator AnimateFill(float from, float to)
    {
        for (float t = 0f; t < extraLifeFillDuration; t += Time.deltaTime)
        {
            extraLifeFill.fillAmount = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / extraLifeFillDuration));
            yield return null;
        }
        extraLifeFill.fillAmount = to;
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
}
