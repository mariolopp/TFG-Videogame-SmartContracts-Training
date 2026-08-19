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

    [Header("Depósitos")]
    [SerializeField] private RectTransform depositoBtc;
    [SerializeField] private RectTransform depositoEth;

    [Header("Timings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float moveDuration = 0.4f;
    [SerializeField] private float delayBetweenOutCoins = 0.1f;
    [SerializeField] private float textFloatDistance = 40f;

    [Header("Colores")]
    [SerializeField] private Color colorNegativo = new Color(0.9f, 0.3f, 0.3f);
    [SerializeField] private Color colorPositivo = new Color(0.3f, 0.85f, 0.4f);

    [Header("Escala moneda saliente")]
    [SerializeField] private float startScale = 1f;
    [SerializeField] private float endScale = 0.1f;

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

        RectTransform depositoIn = GetDeposito(inName);
        RectTransform depositoOut = GetDeposito(outName);

        // Moneda que entra: spawnEntrada -> pozo -> depositoIn (fade out al llegar al depósito)
        Vector3[] rutaEntrada = {
            spawnPointEntrada.position,
            pozoCentral.position,
            depositoIn.position
        };

        // Moneda que entra: fade out DURANTE el movimiento hacia el depósito
        yield return StartCoroutine(AnimateCoinMultiPoint(
            inSprite, rutaEntrada, FormatAmount(inAmount, negative: true), colorNegativo,
            scaleUp: false, labelAtStart: true, fadeOutDuringLastSegment: true));

        yield return new WaitForSeconds(delayBetweenOutCoins);

        int visualCount = Mathf.Clamp(Mathf.Max(1, Mathf.RoundToInt(outAmount)), 1, 10);

        for (int i = 0; i < visualCount; i++)
        {
            bool showLabel = i == 0;

            Vector3[] rutaSalida = {
                depositoOut.position,
                pozoCentral.position,
                spawnPointSalida.position
            };

            // Moneda que sale: se mantiene visible hasta llegar, luego fade out normal
            StartCoroutine(AnimateCoinMultiPoint(
                outSprite, rutaSalida,
                showLabel ? FormatAmount(outAmount, negative: false) : null,
                colorPositivo, scaleUp: true, labelAtStart: false, fadeOutDuringLastSegment: false));

            if (i < visualCount - 1)
                yield return new WaitForSeconds(delayBetweenOutCoins);
        }

        yield return new WaitForSeconds((moveDuration * 2) + fadeDuration);

        OnSwapFinished?.Invoke();
    }
    
    private RectTransform GetDeposito(string coinName)
    {
        if (coinName == "BTC") return depositoBtc;
        if (coinName == "ETH") return depositoEth;
        Debug.LogWarning($"No hay depósito configurado para '{coinName}'");
        return pozoCentral; // fallback de seguridad
    }

    private string FormatAmount(float amount, bool negative)
    {
        string sign = negative ? "-" : "+";
        // Sin decimales inútiles: 1 -> "1", 3.47 -> "3.47"
        string num = amount % 1f == 0f ? amount.ToString("0") : amount.ToString("0.##");
        return $"{sign}{num}";
    }
    private IEnumerator AnimateCoinMultiPoint(Sprite sprite, Vector3[] points, string label, Color labelColor, bool scaleUp, bool labelAtStart, bool fadeOutDuringLastSegment)
    {
        GameObject coinGo = Instantiate(coinPrefab, pozoCentral.parent);
        RectTransform coinRt = coinGo.GetComponent<RectTransform>();
        Image img = coinGo.GetComponent<Image>();
        CanvasGroup coinCg = coinGo.GetComponent<CanvasGroup>();

        img.sprite = sprite;
        coinRt.position = points[0];
        coinCg.alpha = 0f;

        RectTransform textRt = null;
        CanvasGroup textCg = null;
        Vector3 textStart = Vector3.zero;

        int segments = points.Length - 1;

        for (int seg = 0; seg < segments; seg++)
        {
            Vector3 from = points[seg];
            Vector3 to = points[seg + 1];
            bool isFirstSegment = seg == 0;
            bool isLastSegment = seg == segments - 1;
            bool isLabelSegment = labelAtStart ? isFirstSegment : isLastSegment;

            if (isLabelSegment && !string.IsNullOrEmpty(label) && floatingTextPrefab != null)
            {
                Vector3 anchor = labelAtStart ? from : to;
                textStart = anchor + Vector3.up * 20f;

                GameObject textGo = Instantiate(floatingTextPrefab, pozoCentral.parent);
                textRt = textGo.GetComponent<RectTransform>();
                textCg = textGo.GetComponent<CanvasGroup>();
                TMP_Text tmp = textGo.GetComponent<TMP_Text>();

                if (textCg == null) textCg = textGo.AddComponent<CanvasGroup>();

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

                if (isFirstSegment)
                    coinCg.alpha = Mathf.Lerp(0f, 1f, p / 0.5f);
                else if (isLastSegment && fadeOutDuringLastSegment)
                    coinCg.alpha = Mathf.Lerp(1f, 0f, p); // se apaga mientras se mueve
                else
                    coinCg.alpha = 1f;

                if (scaleUp && isLastSegment)
                {
                    float s = Mathf.Lerp(startScale, endScale, p);
                    coinRt.localScale = new Vector3(s, s, 1f);
                }

                if (textRt != null && isLabelSegment)
                {
                    textRt.position = textStart + Vector3.up * (textFloatDistance * p);
                    textCg.alpha = Mathf.Lerp(0f, 1f, p / 0.5f);
                }

                yield return null;
            }

            coinRt.position = to;
        }

        // Si ya se apagó durante el movimiento, no hace falta el fade out extra al final
        if (!fadeOutDuringLastSegment)
        {
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
        }

        Destroy(coinGo);
        if (textRt != null) Destroy(textRt.gameObject);
    }
}