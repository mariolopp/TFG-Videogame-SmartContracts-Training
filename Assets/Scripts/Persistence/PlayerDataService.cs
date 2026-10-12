using System;
using System.Collections;
using System.Threading.Tasks;
using Game.Persistence;
using UnityEngine;

/// <summary>
/// Se crea solo al arrancar el juego (no hay que ponerlo en ninguna escena) y vive toda la partida.
/// Conecta la persistencia con AssetsManager, que es donde el juego lleva las bolsas.
/// </summary>
public class PlayerDataService : MonoBehaviour
{
    // URL del Worker desplegado. Para probar con `wrangler dev` usa "http://127.0.0.1:8787"
    // (y activa Project Settings > Player > Allow downloads over HTTP solo mientras pruebas).
    public const string BackendUrl = "https://game-persistence.tutorialblockchain.workers.dev";
    private const int RequestTimeoutSeconds = 8;

    public static PlayerDataService Instance { get; private set; }
    public PlayerPersistence Core { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject(nameof(PlayerDataService));
        DontDestroyOnLoad(go);
        go.AddComponent<PlayerDataService>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Core = new PlayerPersistence(
            new PlayerDataApi(BackendUrl, RequestTimeoutSeconds),
            Delay,
            () => DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Debug.LogWarning);

        Core.StateReplaced += PushToGame;
    }

    // Guardado "de cortesía" al ir a segundo plano (móvil) o cerrar. Puede no completarse:
    // los minijuegos deben llamar a PlayerData.Save() en sus puntos de control.
    private void OnApplicationPause(bool paused) { if (paused) PlayerData.Save(); }
    private void OnApplicationQuit() => PlayerData.Save();

    /// <summary>Al cambiar de jugador, el juego pasa a mostrar los valores de esa wallet.</summary>
    private static void PushToGame(PlayerState state)
    {
        if (AssetsManager.Instance != null) AssetsManager.Instance.SetUSD(state.bags);
    }

    /// <summary>Antes de guardar, recoge del juego las bolsas actuales.</summary>
    internal void PullFromGame()
    {
        if (AssetsManager.Instance != null)
            Core.State.bags = Mathf.Max(0, Mathf.RoundToInt(AssetsManager.Instance.GetUSD()));
    }

    // Espera basada en corrutina: Task.Delay no funciona en WebGL. Realtime para que
    // Time.timeScale = 0 (pausas, pantallas de fin) no congele los reintentos.
    private Task Delay(float seconds)
    {
        var tcs = new TaskCompletionSource<bool>();
        StartCoroutine(DelayRoutine(seconds, tcs));
        return tcs.Task;
    }

    private static IEnumerator DelayRoutine(float seconds, TaskCompletionSource<bool> tcs)
    {
        yield return new WaitForSecondsRealtime(seconds);
        tcs.TrySetResult(true);
    }
}

/// <summary>
/// API para minijuegos y escenas. Todos los métodos son seguros de llamar siempre:
/// sin sesión o sin servidor no hacen nada y nunca lanzan excepciones.
/// </summary>
public static class PlayerData
{
    private static PlayerPersistence Core => PlayerDataService.Instance != null ? PlayerDataService.Instance.Core : null;

    /// <summary>true si hay una wallet con la partida cargada (lo que se guarde, se persiste).</summary>
    public static bool IsPersistent => Core?.IsPersistent ?? false;

    /// <summary>Dirección normalizada de la sesión actual, o null.</summary>
    public static string Address => Core?.Address;

    public static SyncStatus Status => Core?.Status ?? SyncStatus.Guest;

    /// <summary>Bolsas cargadas/guardadas por última vez. Durante el juego la cifra viva está en AssetsManager.</summary>
    public static int Bags => Core?.State.bags ?? 0;

    /// <summary>Progreso del edificio (0 a 1). Asignarlo no guarda; llama a Save() cuando toque.</summary>
    public static float BuildingProgress
    {
        get => Core?.State.buildingProgress ?? 0f;
        set { if (Core != null) Core.State.buildingProgress = Mathf.Clamp01(value); }
    }

    /// <summary>
    /// Guarda ahora el estado del jugador (bolsas de AssetsManager + progreso del edificio).
    /// No bloquea: el envío y los reintentos van en segundo plano.
    /// Devuelve false si no hay sesión (invitado): no se escribe nada.
    /// </summary>
    public static bool Save()
    {
        try
        {
            var service = PlayerDataService.Instance;
            if (service == null || !service.Core.IsPersistent) return false;
            service.PullFromGame();
            return service.Core.Save();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Persistencia] Save() falló: " + e.Message);
            return false;
        }
    }

    /// <summary>Como Save(), pero espera la confirmación del servidor (p. ej. antes de volver al menú).</summary>
    public static async Task<bool> SaveAsync()
    {
        try
        {
            var service = PlayerDataService.Instance;
            if (service == null || !service.Core.IsPersistent) return false;
            service.PullFromGame();
            return await service.Core.SaveAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Persistencia] SaveAsync() falló: " + e.Message);
            return false;
        }
    }
}
