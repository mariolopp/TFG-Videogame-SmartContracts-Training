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

    [Header("Botones (ya asignados en la escena)")]
    [SerializeField] private Button buyButton;   // gasta B, recibe A
    [SerializeField] private Button sellButton;  // gasta A, recibe B

    [Header("UI (ya asignada en la escena)")]
    [SerializeField] private TMP_Text priceText; // "1 A = X B"
    [SerializeField] private TMP_Text amnt_a;
    [SerializeField] private TMP_Text amnt_b;
    [SerializeField] private TMP_Text amnt_user_a;
    [SerializeField] private TMP_Text amnt_user_b;

    // Otros scripts (UI, etc.) pueden suscribirse para reaccionar a cada trade
    public static event Action<float, float> OnTrade; // (amountIn, amountOut)

    private void Start()
    {
        buyButton.onClick.AddListener(OnBuy);
        sellButton.onClick.AddListener(OnSell);
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

        OnTrade?.Invoke(tradeAmount, amountOut);
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

    private void UpdateUI()
    {
        if (priceText != null)
            priceText.text = $"1 A = {GetPrice():F4} B";
        if (amnt_a !=null)
            amnt_a.text = $"{GetReserveA():F2} A";
        if (amnt_b !=null)
            amnt_b.text = $"{GetReserveB():F2} B"; 
         if (amnt_user_a !=null)
            amnt_user_a.text = $"A ->{GetUserA():F2}";
        if (amnt_user_b !=null)
            amnt_user_b.text = $"B ->{GetUserB():F2}";
    }

    // Getters públicos por si algún otro script quiere mostrar el estado
    public float GetReserveA() => reserveA;
    public float GetReserveB() => reserveB;
    public float GetUserA() => userA;
    public float GetUserB() => userB;
    public float GetPrice() => reserveB / reserveA;
}