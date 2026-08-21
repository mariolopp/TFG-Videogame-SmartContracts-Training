using UnityEngine;

public class MarketTrend
{
    public enum TrendAsset { BTC, ETH }

    private readonly PriceManager priceManager;
    private readonly PoolTraderConfig config;
    private readonly float baselinePrice;
    private bool trendExhausted;

    public TrendAsset CurrentTrend { get; }

    public MarketTrend(PriceManager priceManager, PoolTraderConfig config)
    {
        this.priceManager = priceManager;
        this.config = config;
        CurrentTrend = (Random.value < 0.5f) ? TrendAsset.BTC : TrendAsset.ETH;
        baselinePrice = CurrentTrend == TrendAsset.BTC ? priceManager.GetPriceA() : priceManager.GetPriceB();
    }

    private void UpdateExhaustion()
    {
        if (trendExhausted) return;
        float currentPrice = CurrentTrend == TrendAsset.BTC ? priceManager.GetPriceA() : priceManager.GetPriceB();
        if (currentPrice >= baselinePrice * config.trendExhaustionMultiplier)
            trendExhausted = true;
    }

    // true = esta operación va "a favor" de la tendencia dominante
    public bool ShouldFollowTrend()
    {
        UpdateExhaustion();
        if (trendExhausted) return Random.value < 0.5f;
        return Random.value < config.trendBiasProbability;
    }

    public bool IsTrendExhausted()
    {
        UpdateExhaustion();
        return trendExhausted;
    }

    public bool RollShockEvent() => Random.value < config.randomShockChance;
}