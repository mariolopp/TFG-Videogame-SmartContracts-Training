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
    [SerializeField] private float feePercent = 1f;   // 1%
    [SerializeField] private float tradeAmount = 1f;  // unidades por cada click

    [Header("UI Slippage")]
    [SerializeField] private TMP_Text slippageText;
    [SerializeField] private Color slippageNegativeColor = Color.red;
    [SerializeField] private Color slippagePositiveColor = Color.green;

    [Header("Botones (ya asignados en la escena)")]
    [SerializeField] private Button buyButton;   // gasta B, recibe A
    [SerializeField] private Button sellButton;  // gasta A, recibe B
    [SerializeField] private Button swapPriceView;  // Alternar la visualización del precio de 1 A = x B -> 1B = xA

    [Header("UI (ya asignada en la escena)")]
    [SerializeField] private TMP_Text priceText; // "1 A = X B"
    private bool viewAorB = true;       // True en caso de que se muestre 1 A = xB, false en caso de 1 B = x A
    [SerializeField] private TMP_Text amnt_a;
    [SerializeField] private TMP_Text amnt_b;
    [SerializeField] private TMP_Text amnt_user_a;
    [SerializeField] private TMP_Text amnt_user_b;
    
    [SerializeField] private CoinSwapAnimator swapAnimator; 

    // Otros scripts (UI, etc.) pueden suscribirse para reaccionar a cada trade
    public static event Action<float, float> OnTrade; // (amountIn, amountOut)

    private void Start()
    {
        buyButton.onClick.AddListener(OnBuy);
        sellButton.onClick.AddListener(OnSell);
        swapPriceView.onClick.AddListener(OnView);
        UpdateUI();
    }

    // Compra A pagando con B
    public void OnBuy()
    {
        if (tradeAmount > userB)
        {
            Debug.LogWarning("PriceManager: B insuficiente para comprar.");
            return;
        }

        float amountOut = GetAmountOut(tradeAmount, reserveB, reserveA);

        userB -= tradeAmount;
        userA += amountOut;
        reserveB += tradeAmount;
        reserveA -= amountOut;

        swapAnimator.PlayEthToBtc(1f, amountOut); // Visualiza la animación de swap de ETH a BTC

        OnTrade?.Invoke(tradeAmount, amountOut);
        UpdateUI();
    }

    // Vende A recibiendo B
    public void OnSell()
    {
        if (tradeAmount > userA)
        {
            Debug.LogWarning("PriceManager: A insuficiente para vender.");
            return;
        }

        float amountOut = GetAmountOut(tradeAmount, reserveA, reserveB);

        userA -= tradeAmount;
        userB += amountOut;
        reserveA += tradeAmount;
        reserveB -= amountOut;

        swapAnimator.PlayBtcToEth(1f, amountOut); // Visualiza la animación de swap de BTC a ETH

        OnTrade?.Invoke(tradeAmount, amountOut);
        UpdateUI();
    }

    public void OnView()
    {
        viewAorB = !viewAorB;
        UpdateUI();
    }

    // Fórmula AMM de producto constante (x*y=k) con comisión
    // amountOut = (amountIn*(100-fee)*reserveOut) / (reserveIn*100 + amountIn*(100-fee))
    private float GetAmountOut(float amountIn, float reserveIn, float reserveOut)
    {
        float amountInWithFee = amountIn * (100f - feePercent);
        float numerator = amountInWithFee * reserveOut;
        float denominator = (reserveIn * 100f) + amountInWithFee;
        return numerator / denominator;
    }
    // Calcula el % de slippage para un trade hipotético de A -> B o B -> A
    // direction: true = vendes A y recibes B, false = vendes B y recibes A
    private float CalculateSlippagePercent(float amountIn, bool sellingA)
    {
        float reserveIn = sellingA ? reserveA : reserveB;
        float reserveOut = sellingA ? reserveB : reserveA;

        float spotPrice = reserveOut / reserveIn; // precio marginal actual
        float expectedOut = amountIn * spotPrice;  // lo que "debería" dar el precio spot
        float actualOut = GetAmountOut(amountIn, reserveIn, reserveOut); // lo que realmente da la curva

        // % de diferencia respecto a lo esperado (negativo = recibes menos)
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
            if (viewAorB)
                priceText.text = $"1 BTC = {GetPriceA():F4} ETH";
            else if(!viewAorB)
                priceText.text = $"1 ETH = {GetPriceB():F4} BTC";
        if (amnt_a !=null)
            amnt_a.text = $"{GetReserveA():F2} BTC";
        if (amnt_b !=null)
            amnt_b.text = $"{GetReserveB():F2} ETH"; 
         if (amnt_user_a !=null)
            amnt_user_a.text = $"BTC ->{GetUserA():F2}";
        if (amnt_user_b !=null)
            amnt_user_b.text = $"ETH ->{GetUserB():F2}";
        UpdateSlippagePreview(viewAorB);
    }

    // Getters públicos por si algún otro script quiere mostrar el estado
    public float GetReserveA() => reserveA;
    public float GetReserveB() => reserveB;
    public float GetUserA() => userA;
    public float GetUserB() => userB;
    public float GetPriceA() => reserveB / reserveA;
    public float GetPriceB() => reserveA / reserveB;
}