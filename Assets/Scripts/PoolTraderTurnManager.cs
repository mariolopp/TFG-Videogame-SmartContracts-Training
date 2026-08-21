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

        yield return actorAnimator.RunActor(tint, config.providerWalkDuration, 0.1f, () =>
        {
            if (adds) priceManager.TryAddLiquidity(fraction);
            else priceManager.TryRemoveLiquidity(fraction, config.minimumReserveBTC, config.minimumReserveETH);
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
            if (endGameScoreText != null)
                endGameScoreText.text = $"x{assetsManager.usd}";
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
}