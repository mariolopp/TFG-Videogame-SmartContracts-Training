using UnityEngine;

/// <summary>
/// Niveles de la estatua del mapa, guardados en PlayerData.BuildingProgress (0 a 1).
/// Cada 0.1 es una capa más: nivel 0 = solar vacío, nivel 10 = estatua completa.
/// Los primeros niveles salen de jugar cada minijuego por primera vez (GameProgress); los
/// siguientes se compran con bolsas: el primero cuesta BaseUpgradeCost y cada uno dobla al anterior
/// (nivel 4 = 100, 5 = 200 ... 10 = 6400).
/// </summary>
public static class StatueProgress
{
    public const int MaxLevel = 10;
    public const int BaseUpgradeCost = 100;
    // Margen por ser un float, en fracción de nivel (el mismo que usa GameProgress)
    private const float Epsilon = 0.01f;

    public enum PurchaseResult
    {
        Bought,
        NotEnoughBags,
        Locked,
        Completed,
        Unavailable
    }

    /// <summary>Capas construidas (0 a 10).</summary>
    public static int Level =>
        Mathf.Clamp(Mathf.FloorToInt(PlayerData.BuildingProgress * MaxLevel + Epsilon), 0, MaxLevel);

    /// <summary>Niveles que se consiguen jugando: uno por minijuego.</summary>
    public static int FreeLevels => GameProgress.MinigameCount;

    public static bool IsComplete => Level >= MaxLevel;

    /// <summary>true si ya se han jugado todos los minijuegos y queda alguna mejora por comprar.</summary>
    public static bool CanBuyUpgrade => Level >= FreeLevels && !IsComplete;

    /// <summary>Bolsas que cuesta llegar a ese nivel. Los niveles de los minijuegos no cuestan nada.</summary>
    public static int CostOfLevel(int level) =>
        level <= FreeLevels ? 0 : BaseUpgradeCost << (level - FreeLevels - 1);

    /// <summary>Coste de la siguiente mejora (0 si ya está completa).</summary>
    public static int NextUpgradeCost => IsComplete ? 0 : CostOfLevel(Level + 1);

    public static bool CanAffordNextUpgrade =>
        AssetsManager.Instance != null && AssetsManager.Instance.GetUSD() >= NextUpgradeCost;

    /// <summary>
    /// Compra el siguiente nivel: descuenta las bolsas, sube el progreso y guarda.
    /// Si no se puede, no cambia nada y devuelve el motivo.
    /// </summary>
    public static PurchaseResult TryBuyNextLevel()
    {
        if (IsComplete) return PurchaseResult.Completed;
        if (!CanBuyUpgrade) return PurchaseResult.Locked;

        AssetsManager assets = AssetsManager.Instance;
        if (assets == null || PlayerDataService.Instance == null) return PurchaseResult.Unavailable;

        int nextLevel = Level + 1;
        int cost = CostOfLevel(nextLevel);
        if (assets.GetUSD() < cost) return PurchaseResult.NotEnoughBags;

        assets.SpendUSD(cost);
        PlayerData.BuildingProgress = nextLevel / (float)MaxLevel;
        PlayerData.Save(); // recoge las bolsas de AssetsManager y el progreso en un único guardado
        return PurchaseResult.Bought;
    }
}
