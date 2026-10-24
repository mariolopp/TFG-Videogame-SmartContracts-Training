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

    [Header("Estilo del HUD de turno")]
    [SerializeField] private Color colorPhaseTrading = new Color(0.36f, 0.95f, 0.69f);
    [SerializeField] private Color colorPhaseConversion = new Color(0.5f, 0.85f, 1f);
    [SerializeField] private Color colorPhaseTraders = new Color(1f, 0.69f, 0.36f);
    [SerializeField] private Color colorPhaseLiquidity = new Color(0.78f, 0.62f, 1f);
    [SerializeField] private Color colorPhaseGameOver = new Color(1f, 0.85f, 0.4f);
    [SerializeField] private Color hudOutlineColor = new Color(0.04f, 0.05f, 0.14f, 1f);
    [SerializeField] private float punchScale = 1.25f;    // tamaño del "pop" al cambiar de turno
    [SerializeField] private float punchDuration = 0.35f;

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
    [SerializeField] private float blinkSpeed = 1.5f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseAmount = 0.04f; // cuánto crece y encoge el botón (0.08 = ±8%)
    [SerializeField] private float pulseRampDuration = 12f; // segundos hasta llegar al pulso máximo
    [SerializeField] private float idleDelayBeforePulse = 6f; // segundos sin pulsar nada antes de empezar a agitarse
    private bool blinking = false;
    private Vector3 endTradingButtonScale;

    public TurnPhase CurrentPhase { get; private set; }
    public int CurrentTurn { get; private set; } = 1;

    private MarketTrend marketTrend;
    private string turnLabelEs, turnLabelEn; // el turno actual, para reescribirlo si cambia el idioma
    private int shownTurn = -1;
    private Coroutine labelPunchCo, counterPunchCo;

    private void Start()
    {
        StyleHudText(turnLabelText);
        StyleHudText(turnCounterText);
        if (assetsManager == null)
        {
            assetsManager = FindObjectOfType<AssetsManager>();
        }
        marketTrend = new MarketTrend(priceManager, config);
        priceManager.SetTradingEnabled(false);
        if (endGamePanel != null) endGamePanel.SetActive(false);
        EndConversionButton.onClick.AddListener(EndConversionTurn);
        EndTradingButton.onClick.AddListener(EndTradingTurn);
        EndConversionButton.onClick.AddListener(PlayButtonSound);
        EndTradingButton.onClick.AddListener(PlayButtonSound);
        canvasEndTradingButton.gameObject.SetActive(false);
        endTradingButtonScale = canvasEndTradingButton.transform.localScale;
        LanguageManager.OnLanguageChanged += OnLanguageChanged;
        StartCoroutine(RunGame());
    }

    private void OnDestroy() => LanguageManager.OnLanguageChanged -= OnLanguageChanged;

    private void OnLanguageChanged(Language language)
    {
        UpdateTurnCounter();
        if (turnLabelText != null && turnLabelEs != null) turnLabelText.text = LanguageManager.Pick(turnLabelEs, turnLabelEn);
    }

    private void UpdateTurnCounter()
    {
        if (turnCounterText == null) return;
        int turn = Mathf.Min(CurrentTurn, config.totalTurns);
        turnCounterText.text = $"<size=65%>{LanguageManager.Pick("TURNO", "TURN")}</size> {turn}<size=75%><alpha=#99>/{config.totalTurns}</size>";

        // Solo hace "pop" cuando cambia el número, no al cambiar de idioma
        if (turn != shownTurn)
        {
            shownTurn = turn;
            Punch(ref counterPunchCo, turnCounterText, Color.white); // el dorado lo pone el degradado del texto
        }
    }

    // Contorno oscuro + sombra para que el texto destaque sobre el panel (crea una instancia propia del material)
    private void StyleHudText(TMP_Text text)
    {
        if (text == null) return;
        text.outlineWidth = 0.2f;
        text.outlineColor = hudOutlineColor;

        Material mat = text.fontMaterial;
        mat.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        mat.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.65f));
        mat.SetFloat("_UnderlayOffsetX", 0.7f);
        mat.SetFloat("_UnderlayOffsetY", -0.7f);
        mat.SetFloat("_UnderlaySoftness", 0.25f);
        text.UpdateMeshPadding();
    }

    private void Punch(ref Coroutine co, TMP_Text text, Color targetColor)
    {
        if (text == null) return;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(PunchRoutine(text, targetColor));
    }

    // Empieza grande y en blanco y vuelve a su tamaño y color con ease-out
    private IEnumerator PunchRoutine(TMP_Text text, Color targetColor)
    {
        Transform tr = text.transform;
        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / punchDuration);
            float ease = 1f - (1f - t) * (1f - t);
            tr.localScale = Vector3.one * Mathf.Lerp(punchScale, 1f, ease);
            text.color = Color.Lerp(Color.white, targetColor, ease);
            yield return null;
        }
        tr.localScale = Vector3.one;
        text.color = targetColor;
    }

    private IEnumerator RunGame()
    {
        while (CurrentTurn <= config.totalTurns)
        {
            UpdateTurnCounter();

            yield return StartCoroutine(TradersPhase());

            priceManager.SetFee(Random.Range(config.feeMin, config.feeMax)); // Nuevo: fee aleatoria cada turno
            
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
        SetTurnLabel("Turno de intercambio", "Trading turn", colorPhaseTrading);
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
        SetTurnLabel("Turno de conversión", "Conversion turn", colorPhaseConversion);

        conversionFinished = false;

        if (conversionUI != null) conversionUI.Open(priceManager, config);

        yield return new WaitUntil(() => conversionFinished);

        if (conversionUI != null) conversionUI.Close();
    }

    private IEnumerator TradersPhase()
    {
        CurrentPhase = TurnPhase.Traders;
        SetTurnLabel("Turno de traders", "Traders turn", colorPhaseTraders);

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
        SetTurnLabel("Turno de liquidity providers", "Liquidity providers turn", colorPhaseLiquidity);

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
        SetTurnLabel("Fin de la partida", "Game over", colorPhaseGameOver);
        priceManager.SetTradingEnabled(false);

        int bagsFromBTC = Mathf.RoundToInt(priceManager.GetUserA() * config.bagsPerBTC);
        int bagsFromETH = Mathf.RoundToInt(priceManager.GetUserB() * config.bagsPerETH);

        assetsManager.AddUSD(bagsFromBTC + bagsFromETH);
        GameProgress.MarkPlayed(GameProgress.PoolTrader); // La primera vez sube la estatua y desbloquea ContractApprover
        PlayerData.Save();

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
                StartCoroutine(AnimateNumber(endGameScoreText, 0f, bagsFromBTC + bagsFromETH, 3.5f, 6f));
        }
        if (endGameCloseButton != null)
        {
            endGameCloseButton.onClick.AddListener(PlayButtonSound);
            endGameCloseButton.onClick.AddListener(() => AudioManager.Instance?.PlaySFX("level_out&in"));
            endGameCloseButton.onClick.AddListener(() => SceneManager.LoadScene("MainScene"));
        }
        yield return null;
    }

    private void SetTurnLabel(string es, string en, Color phaseColor)
    {
        turnLabelEs = es;
        turnLabelEn = en;
        string label = LanguageManager.Pick(es, en);
        if (turnLabelText != null)
        {
            turnLabelText.text = label;
            Punch(ref labelPunchCo, turnLabelText, phaseColor);
        }
        AudioManager.Instance?.PlaySFX("turn_change");
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
        float lastInputTime = Time.time;
        float ramp = 0f;

        while (blinking)
        {
            float alpha = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f;

            canvasEndTradingButton.alpha = Mathf.Lerp(0.6f, 1f, alpha);

            // Cualquier clic o tecla (comprar, vender, ver...) reinicia la espera
            if (Input.anyKeyDown) lastInputTime = Time.time;
            bool idle = Time.time - lastInputTime >= idleDelayBeforePulse;

            // Si el jugador está parado el pulso crece poco a poco hasta pulseAmount; si actúa, se calma rápido
            ramp = idle
                ? Mathf.Clamp01(ramp + Time.deltaTime / Mathf.Max(pulseRampDuration, 0.01f))
                : Mathf.MoveTowards(ramp, 0f, Time.deltaTime * 4f);
            float pulse = Mathf.Sin(Time.time * pulseSpeed);
            canvasEndTradingButton.transform.localScale = endTradingButtonScale * (1f + pulse * pulseAmount * ramp);

            yield return null;
        }

        canvasEndTradingButton.alpha = 1f;
        canvasEndTradingButton.transform.localScale = endTradingButtonScale;
    }
    private IEnumerator AnimateNumber(TMP_Text text, float startValue, float targetValue, float duration, float wait=0f)
    {
        yield return new WaitForSeconds(wait);

        AudioSource countLoop = PlayCountLoop(startValue, targetValue);
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
        if (AudioManager.Instance != null) AudioManager.Instance.StopLoopSFX(countLoop);
    }

    // Si no hay nada que contar (se queda en el mismo número) no suena
    private AudioSource PlayCountLoop(float startValue, float targetValue)
    {
        if (AudioManager.Instance == null || Mathf.RoundToInt(startValue) == Mathf.RoundToInt(targetValue)) return null;
        return AudioManager.Instance.PlayLoopSFX("scoreCounterBucle", this);
    }

    private static void PlayButtonSound()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("select_button");
    }
}
