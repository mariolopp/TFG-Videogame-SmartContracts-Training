using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PriceManager : MonoBehaviour
{
    [Header("Liquidez inicial del pool")]
    [SerializeField] private float reserveA = 10f;
    [SerializeField] private float reserveB = 10f;

    [Header("Balances del usuario")]
    [SerializeField] private float userA = 5f;
    [SerializeField] private float userB = 5f;

    [Header("Configuración")]
    [SerializeField] private float feePercent = 1f;
    [SerializeField] private float tradeAmount = 1f;

    [Header("UI Slippage")]
    [SerializeField] private TMP_Text slippageText;
    [SerializeField] private Color slippageNegativeColor = Color.red;
    [SerializeField] private Color slippagePositiveColor = Color.green;

    [Header("Botones (ya asignados en la escena)")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button swapPriceView;

    [Header("UI (ya asignada en la escena)")]
    [SerializeField] private TMP_Text priceText;
    private bool viewAorB = true;
    [SerializeField] private TMP_Text amnt_a;
    [SerializeField] private TMP_Text amnt_b;
    [SerializeField] private TMP_Text amnt_user_a;
    [SerializeField] private TMP_Text amnt_user_b;

    [SerializeField] private CoinSwapAnimator swapAnimator;
    [SerializeField] private Image imgBTC;
    [SerializeField] private Image imgETH;
    private int XposImgA;
    private int XposImgB;

    public static event Action<float, float> OnTrade;

    // NUEVO: gating de turno
    private bool canTrade = true;

    private void Start()
    {
        // Posiciones para intercambiar sprites si se cambia la vista de precio
        XposImgA = (int)imgBTC.rectTransform.anchoredPosition.x;
        XposImgB = (int)imgETH.rectTransform.anchoredPosition.x;
        
        buyButton.onClick.AddListener(OnBuy);
        sellButton.onClick.AddListener(OnSell);
        swapPriceView.onClick.AddListener(OnView);
        
        UpdateUI();
    }

    // ---------- NUEVO: núcleo AMM reutilizable ----------
    // sellingA = true  -> se vende A(BTC) y se recibe B(ETH)  (equivale a OnSell/botón Sell_A)
    // sellingA = false -> se vende B(ETH) y se recibe A(BTC)  (equivale a OnBuy/botón Buy_A)
    private float ExecuteTrade(bool sellingA, float amountIn, bool touchUserBalances)
    {
        float reserveIn = sellingA ? reserveA : reserveB;
        float reserveOut = sellingA ? reserveB : reserveA;
        float amountOut = GetAmountOut(amountIn, reserveIn, reserveOut);

        if (sellingA)
        {
            reserveA += amountIn;
            reserveB -= amountOut;
            if (touchUserBalances) { userA -= amountIn; userB += amountOut; }
        }
        else
        {
            reserveB += amountIn;
            reserveA -= amountOut;
            if (touchUserBalances) { userB -= amountIn; userA += amountOut; }
        }
        return amountOut;
    }

    // NUEVO: usado por traders/bots. NO toca userA/userB.
    public float ExecuteExternalTrade(bool sellingA, float amountIn, bool playCoinAnimation = false)
    {
        float amountOut = ExecuteTrade(sellingA, amountIn, touchUserBalances: false);

        if (playCoinAnimation && swapAnimator != null)
        {
            if (sellingA) swapAnimator.PlayBtcToEth(amountIn, amountOut, true);
            else swapAnimator.PlayEthToBtc(amountIn, amountOut, true);
        }

        OnTrade?.Invoke(amountIn, amountOut);
        UpdateUI();
        return amountOut;
    }

    // NUEVO: liquidity providers
    public bool TryAddLiquidity(float fraction)
    {
        reserveA += reserveA * fraction;
        reserveB += reserveB * fraction;
        UpdateUI();
        return true;
    }

    public bool TryRemoveLiquidity(float fraction, float minA, float minB)
    {
        float removeA = reserveA * fraction;
        float removeB = reserveB * fraction;

        if (reserveA - removeA < minA || reserveB - removeB < minB)
            return false; // se ignora la orden completa

        reserveA -= removeA;
        reserveB -= removeB;
        UpdateUI();
        return true;
    }

    // NUEVO: gating de turno (bloquea input y visualmente desactiva botones)
    public void SetTradingEnabled(bool enabled)
    {
        canTrade = enabled;
        if (buyButton != null) buyButton.interactable = enabled;
        if (sellButton != null) sellButton.interactable = enabled;
    }

    // NUEVO: usado por el turno de conversión
    public void AddUserBTC(float amount) { userA += amount; UpdateUI(); }
    public void AddUserETH(float amount) { userB += amount; UpdateUI(); }

    // ---------- Igual que antes, pero hace la llamada a ExecuteTrade ----------
    public void OnBuy()
    {
        if (!canTrade) return;
        if (tradeAmount > userB)
        {
            Debug.LogWarning("PriceManager: B insuficiente para comprar.");
            return;
        }
        float amountOut = ExecuteTrade(sellingA: false, tradeAmount, touchUserBalances: true);
        swapAnimator.PlayEthToBtc(1f, amountOut);
        OnTrade?.Invoke(tradeAmount, amountOut);
        UpdateUI();
    }

    public void OnSell()
    {
        if (!canTrade) return;
        if (tradeAmount > userA)
        {
            Debug.LogWarning("PriceManager: A insuficiente para vender.");
            return;
        }
        float amountOut = ExecuteTrade(sellingA: true, tradeAmount, touchUserBalances: true);
        swapAnimator.PlayBtcToEth(1f, amountOut);
        OnTrade?.Invoke(tradeAmount, amountOut);
        UpdateUI();
    }

    // Se utiliza para alternar las imágenes en el caso que el usuario cambie el orden de visualización de precio
    public void OnView()
    {
        viewAorB = !viewAorB;
        if (viewAorB)
        {
            imgBTC.rectTransform.anchoredPosition = new Vector2(XposImgA, imgBTC.rectTransform.anchoredPosition.y);
            imgETH.rectTransform.anchoredPosition = new Vector2(XposImgB, imgETH.rectTransform.anchoredPosition.y);
        }
        else
        {
            imgBTC.rectTransform.anchoredPosition = new Vector2(XposImgB, imgBTC.rectTransform.anchoredPosition.y);
            imgETH.rectTransform.anchoredPosition = new Vector2(XposImgA, imgETH.rectTransform.anchoredPosition.y);
        }
        UpdateUI();
    }

    private float GetAmountOut(float amountIn, float reserveIn, float reserveOut)
    {
        float amountInWithFee = amountIn * (100f - feePercent);
        float numerator = amountInWithFee * reserveOut;
        float denominator = (reserveIn * 100f) + amountInWithFee;
        return numerator / denominator;
    }

    private float CalculateSlippagePercent(float amountIn, bool sellingA)
    {
        float reserveIn = sellingA ? reserveA : reserveB;
        float reserveOut = sellingA ? reserveB : reserveA;
        float spotPrice = reserveOut / reserveIn;
        float expectedOut = amountIn * spotPrice;
        float actualOut = GetAmountOut(amountIn, reserveIn, reserveOut);
        return ((actualOut - expectedOut) / expectedOut) * 100f;
    }

    private void UpdateSlippagePreview(bool sellingToken)
    {
        if (slippageText == null) return;
        float slippage = CalculateSlippagePercent(tradeAmount, sellingToken);
        slippageText.text = $"Slippage: {slippage:F2}%";
        slippageText.color = slippage < 0 ? slippageNegativeColor : slippagePositiveColor;
    }

    private void UpdateUI()
    {
        if (priceText != null)
            if (viewAorB) priceText.text = $"{GetPriceA():F4}"; // 1BTC = a
            else priceText.text = $"{GetPriceB():F4}";          // 1 ETH = a
        if (amnt_a != null) amnt_a.text = $"{GetReserveA():F2} BTC";
        if (amnt_b != null) amnt_b.text = $"{GetReserveB():F2} ETH";
        if (amnt_user_a != null) amnt_user_a.text = $"x{GetUserA():F2}";
        if (amnt_user_b != null) amnt_user_b.text = $"x{GetUserB():F2}";
        UpdateSlippagePreview(viewAorB);
    }

    public float GetReserveA() => reserveA;
    public float GetReserveB() => reserveB;
    public float GetUserA() => userA;
    public float GetUserB() => userB;
    public float GetPriceA() => reserveB / reserveA;
    public float GetPriceB() => reserveA / reserveB;
}