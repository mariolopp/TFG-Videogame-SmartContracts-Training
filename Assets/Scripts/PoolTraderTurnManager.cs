using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PoolTraderTurnManager : MonoBehaviour
{
    public enum TurnPhase { PlayerTrading, Conversion, Traders, LiquidityProviders, GameOver }

    [Header("Configuración")]
    [SerializeField] private PoolTraderConfig config;

    [Header("Referencias del juego")]
    [SerializeField] private PriceManager priceManager;
    [SerializeField] private AssetsManager assetsManager;
    [SerializeField] private MarketActorAnimator actorAnimator;
    [SerializeField] private CoinSwapAnimator coinSwapAnimator;
    [SerializeField] private ConversionUIController conversionUI;

    [Header("UI de turno")]
    [SerializeField] private TMP_Text turnLabelText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text turnCounterText;
    [SerializeField] private Button EndConversionButton;
    [SerializeField] private Button EndTradingButton;

    [Header("UI Fin de partida")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private Button endGameCloseButton;
    [SerializeField] private TMP_Text endGameScoreText;
    [SerializeField] private TMP_Text endGameBagsBTCText;
    [SerializeField] private TMP_Text endGameBagsETHText;
    [SerializeField] private TMP_Text endGameNumBTCText;
    [SerializeField] private TMP_Text endGameNumETHText;

    [Header("Estado interno")]
    private bool conversionFinished = false;
    private bool tradingFinished = false;

    [Header("Animaciones recordatorio")]
    [SerializeField] private CanvasGroup canvasEndTradingButton;
    [SerializeField] private float blinkSpeed = 3f;
    private bool blinking = false;

    public TurnPhase CurrentPhase { get; private set; }
    public int CurrentTurn { get; private set; } = 1;

    private MarketTrend marketTrend;

    private void Start()
    {
        marketTrend = new MarketTrend(priceManager, config);
        priceManager.SetTradingEnabled(false);
        if (endGamePanel != null) endGamePanel.SetActive(false);
        EndConversionButton.onClick.AddListener(EndConversionTurn);
        EndTradingButton.onClick.AddListener(EndTradingTurn);
        canvasEndTradingButton.gameObject.SetActive(false);
        StartCoroutine(RunGame());
    }

    private IEnumerator RunGame()
    {
        while (CurrentTurn <= config.totalTurns)
        {
            if (turnCounterText != null)
                turnCounterText.text = $"Turno {CurrentTurn} / {config.totalTurns}";

            yield return StartCoroutine(TradersPhase());
            yield return StartCoroutine(PlayerTradingPhase());

            if (CurrentTurn < config.totalTurns)    // No se ejecuta en el último turno
            {
                yield return StartCoroutine(ConversionPhase());
                yield return StartCoroutine(LiquidityProvidersPhase());
            }
            CurrentTurn++;
        }

        yield return StartCoroutine(EndGame());
    }

    private IEnumerator PlayerTradingPhase()
    {
        CurrentPhase = TurnPhase.PlayerTrading;
        SetTurnLabel("Turno de intercambio");
        priceManager.SetTradingEnabled(true);
        canvasEndTradingButton.gameObject.SetActive(true);

        tradingFinished = false;
        StartCoroutine(BlinkButton());

        yield return new WaitUntil(() => tradingFinished);

        blinking = false;
        priceManager.SetTradingEnabled(false);
    }

    private IEnumerator ConversionPhase()
    {
        CurrentPhase = TurnPhase.Conversion;
        SetTurnLabel("Turno de conversión");

        conversionFinished = false;

        if (conversionUI != null) conversionUI.Open(priceManager, config);

        yield return new WaitUntil(() => conversionFinished);

        if (conversionUI != null) conversionUI.Close();
    }

    private IEnumerator TradersPhase()
    {
        CurrentPhase = TurnPhase.Traders;
        SetTurnLabel("Turno de traders");

        int traderCount = Random.Range(config.minTraders, config.maxTraders + 1);
        bool shockThisTurn = marketTrend.RollShockEvent();

        for (int i = 0; i < traderCount; i++)
        {
            yield return StartCoroutine(RunSingleTrader(shockThisTurn));
            yield return new WaitForSeconds(config.delayBetweenTraders);
        }
    }

    private IEnumerator RunSingleTrader(bool isShockTurn)
    {
        bool followTrend = marketTrend.ShouldFollowTrend();
        bool buyBTC;

        if (isShockTurn)
            // en un shock, la presión va predominantemente EN CONTRA de la tendencia habitual
            buyBTC = marketTrend.CurrentTrend != MarketTrend.TrendAsset.BTC;
        else if (followTrend)
            buyBTC = marketTrend.CurrentTrend == MarketTrend.TrendAsset.BTC;
        else
            buyBTC = Random.value < 0.5f;

        float amount = Random.Range(config.traderTradeAmountRange.x, config.traderTradeAmountRange.y);
        if (isShockTurn)
            amount *= Random.Range(config.shockAmountMultiplierRange.x, config.shockAmountMultiplierRange.y);

        // buyBTC = true  -> gasta ETH para llevarse BTC (equivale a pulsar Buy_A / OnBuy)
        // buyBTC = false -> gasta BTC para llevarse ETH (equivale a pulsar Sell_A / OnSell)
        yield return actorAnimator.RunActor(config.colorTraderNeutral, config.traderWalkDuration, 2f,
            () => priceManager.ExecuteExternalTrade(sellingA: !buyBTC, amountIn: amount, playCoinAnimation: true));
    }

    private IEnumerator LiquidityProvidersPhase()
    {
        CurrentPhase = TurnPhase.LiquidityProviders;
        SetTurnLabel("Turno de liquidity providers");

        if (Random.value <= config.chanceLiquidityTurnHappens)
        {
            int providerCount = Random.Range(config.minLiquidityProviders, config.maxLiquidityProviders + 1);
            for (int i = 0; i < providerCount; i++)
            {
                yield return StartCoroutine(RunSingleProvider());
                yield return new WaitForSeconds(config.delayBetweenProviders);
            }
        }
    }

    private IEnumerator RunSingleProvider()
    {
        bool adds = Random.value < config.chanceProviderAdds;
        float fraction = Random.Range(config.minLiquidityChangeFraction, config.maxLiquidityChangeFraction);
        Color tint = adds ? config.colorLiquidityAdd : config.colorLiquidityRemove;

        yield return actorAnimator.RunActor(tint, config.providerWalkDuration, 1.5f, () =>
        {
            float btcBefore = priceManager.GetReserveA();
            float ethBefore = priceManager.GetReserveB();

            bool success = adds
                ? priceManager.TryAddLiquidity(fraction)
                : priceManager.TryRemoveLiquidity(fraction, config.minimumReserveBTC, config.minimumReserveETH);

            //if (!success) return; // orden ignorada (mínimo violado) -> sin animación de monedas

            float btcDelta = Mathf.Abs(priceManager.GetReserveA() - btcBefore);
            float ethDelta = Mathf.Abs(priceManager.GetReserveB() - ethBefore);

            if (adds) coinSwapAnimator.PlayAddLiquidity(btcDelta, ethDelta);
            else if (fraction > 0) coinSwapAnimator.PlayRemoveLiquidity(btcDelta, ethDelta);
        });
    }

    private IEnumerator EndGame()
    {
        CurrentPhase = TurnPhase.GameOver;
        SetTurnLabel("Fin de la partida");
        priceManager.SetTradingEnabled(false);

        int bagsFromBTC = Mathf.RoundToInt(priceManager.GetUserA() * config.bagsPerBTC);
        int bagsFromETH = Mathf.RoundToInt(priceManager.GetUserB() * config.bagsPerETH);

        assetsManager.AddUSD(bagsFromBTC + bagsFromETH);

        if (endGamePanel != null)
        {
            endGamePanel.SetActive(true);
            if (endGameNumETHText !=null)
                //endGameNumETHText.text = $"x{priceManager.GetUserB()}";
                StartCoroutine(AnimateNumber(endGameNumETHText, 0f, priceManager.GetUserB(), 1f));
            if (endGameBagsETHText != null)
                //endGameBagsETHText.text = $"x{bagsFromETH}"; 
                StartCoroutine(AnimateNumber(endGameBagsETHText, 0f, bagsFromETH, 1.5f, 1.25f));   
            if (endGameNumBTCText !=null)
                //endGameNumBTCText.text = $"x{priceManager.GetUserA()}";
                StartCoroutine(AnimateNumber(endGameNumBTCText, 0f, priceManager.GetUserA(), 1f, 3f));
            if (endGameBagsBTCText != null)
                //endGameBagsBTCText.text = $"x{bagsFromBTC}";
                StartCoroutine(AnimateNumber(endGameBagsBTCText, 0f, bagsFromBTC, 1.5f, 4.25f));
            if (endGameScoreText != null)
                //endGameScoreText.text = $"x{assetsManager.usd}";
                StartCoroutine(AnimateNumber(endGameScoreText, 0f, assetsManager.usd, 3.5f, 6f));
        }
        if (endGameCloseButton != null)
        {
            endGameCloseButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
        }
        yield return null;
    }

    private void SetTurnLabel(string label)
    {
        if (turnLabelText != null) turnLabelText.text = label;
    }

    public void EndConversionTurn()
    {
        conversionFinished = true;
    }
    public void EndTradingTurn()
    {
        tradingFinished = true;
        canvasEndTradingButton.gameObject.SetActive(false);
    }
    private IEnumerator BlinkButton()
    {
        blinking = true;

        while (blinking)
        {
            float alpha = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f;

            canvasEndTradingButton.alpha = Mathf.Lerp(0.6f, 1f, alpha);

            yield return null;
        }

        canvasEndTradingButton.alpha = 1f;
    }
    private IEnumerator AnimateNumber(TMP_Text text, float startValue, float targetValue, float duration, float wait=0f)
    {
        yield return new WaitForSeconds(wait);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float currentValue = Mathf.Lerp(startValue, targetValue, t);

            text.text = $"x{Mathf.RoundToInt(currentValue)}";

            yield return null;
        }

        text.text = $"x{Mathf.RoundToInt(targetValue)}";
    }
}