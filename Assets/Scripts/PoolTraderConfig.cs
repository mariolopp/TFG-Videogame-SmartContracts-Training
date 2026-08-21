using UnityEngine;

[CreateAssetMenu(fileName = "PoolTraderConfig", menuName = "PoolTrader/Game Config")]
public class PoolTraderConfig : ScriptableObject
{
    [Header("Duración de partida")]
    public int totalTurns = 8;

    [Header("Turno de intercambio (jugador)")]
    public float playerTradeTurnDuration = 20f;

    [Header("Turno de conversión")]
    public float conversionTurnDuration = 10f;
    public int bagsPerBTC = 4;
    public int bagsPerETH = 2;
    public int factorBags = 1; // Comisión entre compra y venta

    [Header("Turno de traders")]
    public int minTraders = 2;
    public int maxTraders = 5;
    public float delayBetweenTraders = 1.2f;
    public float traderWalkDuration = 0.6f;
    public Vector2 traderTradeAmountRange = new Vector2(0.3f, 2.5f);

    [Header("Tendencia de mercado")]
    [Range(0.5f, 1f)] public float trendBiasProbability = 0.68f;
    [Tooltip("1.5 = la tendencia se agota al +50% del precio de inicio de partida")]
    public float trendExhaustionMultiplier = 1.5f;
    [Range(0f, 1f)] public float randomShockChance = 0.12f;
    public Vector2 shockAmountMultiplierRange = new Vector2(1.5f, 3f);

    [Header("Turno de liquidity providers")]
    [Range(0f, 1f)] public float chanceLiquidityTurnHappens = 0.7f;
    public int minLiquidityProviders = 1;
    public int maxLiquidityProviders = 2;
    [Range(0f, 1f)] public float minLiquidityChangeFraction = 0.10f;
    [Range(0f, 1f)] public float maxLiquidityChangeFraction = 0.25f;
    [Range(0f, 1f)] public float chanceProviderAdds = 0.5f;
    public float minimumReserveBTC = 7f;
    public float minimumReserveETH = 15f;
    public float providerWalkDuration = 0.6f;
    public float delayBetweenProviders = 1f;

    [Header("Colores visuales")]
    public Color colorLiquidityAdd = new Color(0.3f, 0.85f, 0.4f);
    public Color colorLiquidityRemove = new Color(0.9f, 0.3f, 0.3f);
    public Color colorTraderNeutral = new Color(0.75f, 0.75f, 0.75f);
}