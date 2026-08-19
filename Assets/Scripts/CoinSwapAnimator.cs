using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoinSwapAnimator : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RectTransform pozoCentral;
    [SerializeField] private RectTransform spawnPointEntrada;
    [SerializeField] private RectTransform spawnPointSalida;
    [SerializeField] private GameObject coinPrefab;   // Image + CanvasGroup
    [SerializeField] private GameObject floatingTextPrefab; // TMP_Text + CanvasGroup

    [Header("Sprites")]
    [SerializeField] private Sprite btcSprite;
    [SerializeField] private Sprite ethSprite;

    [Header("Timings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float moveDuration = 0.4f;
    [SerializeField] private float delayBetweenOutCoins = 0.1f;
    [SerializeField] private float textFloatDistance = 40f;

    [Header("Colores")]
    [SerializeField] private Color colorNegativo = new Color(0.9f, 0.3f, 0.3f);
    [SerializeField] private Color colorPositivo = new Color(0.3f, 0.85f, 0.4f);

    public System.Action OnSwapStarted;
    public System.Action OnSwapFinished;

    // Atajos
    public void PlayBtcToEth(float btcAmount, float ethAmount)
        => PlaySwap("BTC", btcAmount, btcSprite, "ETH", ethAmount, ethSprite);

    public void PlayEthToBtc(float ethAmount, float btcAmount)
        => PlaySwap("ETH", ethAmount, ethSprite, "BTC", btcAmount, btcSprite);

    /// <summary>
    /// inAmount y outAmount son las cantidades REALES (para el texto).
    /// El número de monedas dibujadas saliendo se capa a 10 automáticamente.
    /// </summary>
    public void PlaySwap(string inName, float inAmount, Sprite inSprite,
                          string outName, float outAmount, Sprite outSprite)
    {
        StartCoroutine(SwapRoutine(inName, inAmount, inSprite, outName, outAmount, outSprite));
    }

    private IEnumerator SwapRoutine(string inName, float inAmount, Sprite inSprite,
                                     string outName, float outAmount, Sprite outSprite)
    {
        OnSwapStarted?.Invoke();

        // Moneda que entra: "-<inAmount> <inName>"
        yield return StartCoroutine(AnimateCoinWithLabel(
            inSprite, spawnPointEntrada.position, pozoCentral.position,
            FormatAmount(inAmount, negative: true), colorNegativo));

        // Monedas que salen (visual capado a 10, texto usa outAmount real solo en la primera)
        int visualCount = Mathf.Clamp(Mathf.Max(1, Mathf.RoundToInt(outAmount)), 1, 10);

        for (int i = 0; i < visualCount; i++)
        {
            bool showLabel = i == 0; // el texto con la cantidad total solo aparece una vez
            StartCoroutine(AnimateCoinWithLabel(
                outSprite, pozoCentral.position, spawnPointSalida.position,
                showLabel ? FormatAmount(outAmount, negative: false) : null,
                colorPositivo));

            if (i < visualCount - 1)
                yield return new WaitForSeconds(delayBetweenOutCoins);
        }

        yield return new WaitForSeconds(moveDuration + fadeDuration);

        OnSwapFinished?.Invoke();
    }

    private string FormatAmount(float amount, bool negative)
    {
        string sign = negative ? "-" : "+";
        // Sin decimales inútiles: 1 -> "1", 3.47 -> "3.47"
        string num = amount % 1f == 0f ? amount.ToString("0") : amount.ToString("0.##");
        return $"{sign}{num}";
    }

    private IEnumerator AnimateCoinWithLabel(Sprite sprite, Vector3 from, Vector3 to, string label, Color labelColor)
    {
        // Moneda
        GameObject coinGo = Instantiate(coinPrefab, pozoCentral.parent);
        RectTransform coinRt = coinGo.GetComponent<RectTransform>();
        Image img = coinGo.GetComponent<Image>();
        CanvasGroup coinCg = coinGo.GetComponent<CanvasGroup>();

        img.sprite = sprite;
        coinRt.position = from;
        coinCg.alpha = 0f;

        // Texto flotante (opcional)
        RectTransform textRt = null;
        CanvasGroup textCg = null;
        Vector3 textStart = from + Vector3.up * 20f;

        if (!string.IsNullOrEmpty(label) && floatingTextPrefab != null)
        {
            GameObject textGo = Instantiate(floatingTextPrefab, pozoCentral.parent);
            textRt = textGo.GetComponent<RectTransform>();
            textCg = textGo.GetComponent<CanvasGroup>();
            TMP_Text tmp = textGo.GetComponent<TMP_Text>();

            tmp.text = label;
            tmp.color = labelColor;
            textRt.position = textStart;
            textCg.alpha = 0f;
        }

        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float p = t / moveDuration;

            coinRt.position = Vector3.Lerp(from, to, p);
            coinCg.alpha = Mathf.Lerp(0f, 1f, p / 0.5f);

            if (textRt != null)
            {
                textRt.position = textStart + Vector3.up * (textFloatDistance * p);
                textCg.alpha = Mathf.Lerp(0f, 1f, p / 0.5f);
            }

            yield return null;
        }

        coinRt.position = to;
        coinCg.alpha = 1f;

        float ft = 0f;
        while (ft < fadeDuration)
        {
            ft += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, ft / fadeDuration);
            coinCg.alpha = a;
            if (textCg != null) textCg.alpha = a;
            yield return null;
        }

        Destroy(coinGo);
        if (textRt != null) Destroy(textRt.gameObject);
    }
}