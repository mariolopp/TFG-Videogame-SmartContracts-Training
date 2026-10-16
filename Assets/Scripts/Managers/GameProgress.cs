using UnityEngine;

/// <summary>
/// Avance del jugador por los minijuegos, deducido del crecimiento de la estatua
/// (PlayerData.BuildingProgress): 0 = partida nueva, 0.1 = ha jugado BlockMiner,
/// 0.2 = además PoolTrader, 0.3 = además ContractApprover. Por encima de 0.3 son mejoras
/// de la estatua compradas con bolsas, que aquí no cuentan.
/// Los minijuegos se desbloquean en orden
/// </summary>
public static class GameProgress
{
    public const string BlockMiner = "BlockMiner";
    public const string PoolTrader = "PoolTrader";
    public const string ContractApprover = "ContractApprover";

    // Orden de desbloqueo. Jugar cada uno por primera vez sube la estatua ProgressPerMinigame
    private static readonly string[] Minigames = { BlockMiner, PoolTrader, ContractApprover };
    private const float ProgressPerMinigame = 0.1f;
    // Margen por ser un float para redondear
    private const float Epsilon = 0.001f;

    // Estático: se marca en el minijuego y lo consume MainScene al cargar
    private static bool newUnlockPending;

    /// <summary>Cuántos minijuegos se han jugado ya (0 a 3).</summary>
    public static int PlayedCount =>
        Mathf.Clamp(Mathf.FloorToInt((PlayerData.BuildingProgress + Epsilon) / ProgressPerMinigame), 0, Minigames.Length);

    /// <summary>true si el jugador no ha jugado nada todavía: la partida empieza directamente en BlockMiner.</summary>
    public static bool IsNewGame => PlayedCount == 0;

    /// <summary>Un minijuego está disponible si ya se jugó o es el siguiente en orden. Escenas fuera de la lista: siempre.</summary>
    public static bool IsUnlocked(string sceneName)
    {
        int index = System.Array.IndexOf(Minigames, sceneName);
        return index < 0 || index <= PlayedCount;
    }

    /// <summary>
    /// Llamar al terminar un minijuego, antes de PlayerData.Save(). Si es la primera vez que se juega
    /// sube la estatua al nivel siguiente y deja pendiente la animación del camino en el mapa.
    /// </summary>
    public static void MarkPlayed(string sceneName)
    {
        // Solo cuenta el siguiente en orden: repetir uno ya jugado (o abrir otro desde el editor) no cambia nada
        int index = System.Array.IndexOf(Minigames, sceneName);
        if (index < 0 || index != PlayedCount) return;

        PlayerData.BuildingProgress = Mathf.Max(PlayerData.BuildingProgress, (index + 1) * ProgressPerMinigame);
        newUnlockPending = index + 1 < Minigames.Length;
    }

    /// <summary>true (una sola vez) si se acaba de desbloquear un minijuego y hay que animar su camino.</summary>
    public static bool ConsumeNewUnlock()
    {
        bool pending = newUnlockPending;
        newUnlockPending = false;
        return pending;
    }

    /// <summary>Tramo del camino guía: del último minijuego jugado al siguiente por jugar. false si no hay ninguno.</summary>
    public static bool TryGetGuidePath(out string fromScene, out string toScene)
    {
        int played = PlayedCount;
        bool exists = played > 0 && played < Minigames.Length;
        fromScene = exists ? Minigames[played - 1] : null;
        toScene = exists ? Minigames[played] : null;
        return exists;
    }
}
