using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Game.Persistence
{
    /// <summary>
    /// Todo lo que se guarda por jugador. Los nombres de campo son los del JSON del backend.
    /// </summary>
    [Serializable]
    public sealed class PlayerState
    {
        public int bags;                 // número de bolsas
        public float buildingProgress;   // 0.0 = sin empezar, 1.0 = terminado

        public static PlayerState NewGame() => new PlayerState();

        public PlayerState Clone() => new PlayerState { bags = bags, buildingProgress = buildingProgress };

        /// <summary>Deja los valores dentro del rango que acepta el servidor.</summary>
        public PlayerState Sanitize()
        {
            if (bags < 0) bags = 0;
            buildingProgress = float.IsNaN(buildingProgress) ? 0f : Math.Max(0f, Math.Min(1f, buildingProgress));
            return this;
        }
    }

    /// <summary>Normalización de direcciones EVM: '0x' + 40 hex en minúsculas.</summary>
    public static class EvmAddress
    {
        private static readonly Regex Normalized = new Regex("^0x[0-9a-f]{40}$", RegexOptions.CultureInvariant);

        public static bool TryNormalize(string raw, out string normalized)
        {
            normalized = null;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            string candidate = raw.Trim().ToLowerInvariant();
            if (!Normalized.IsMatch(candidate)) return false;
            normalized = candidate;
            return true;
        }
    }

    /// <summary>
    /// Mensaje que firma la wallet. DEBE coincidir carácter a carácter con backend/src/message.js.
    /// </summary>
    public static class LoginMessage
    {
        public const string GameName = "MyGame";

        public static string Build(string normalizedAddress, long issuedAtUnixSeconds) =>
            "Sign in to " + GameName + "\n" +
            "Address: " + normalizedAddress + "\n" +
            "Issued at: " + issuedAtUnixSeconds.ToString(CultureInfo.InvariantCulture) + "\n" +
            "This signature does not send a transaction or cost gas.";
    }

    /// <summary>Lo mínimo que se necesita de una wallet. Implementado con thirdweb en ThirdwebWalletSigner.</summary>
    public interface IWalletSigner
    {
        Task<string> GetAddressAsync();
        Task<string> SignMessageAsync(string message); // EIP-191 personal_sign
    }

    public enum ApiStatus
    {
        Ok,
        Unauthorized,   // 401: firma o token no válidos / caducados
        Rejected,       // otros 4xx: datos inválidos, no tiene sentido reintentar
        ServerError,    // 5xx, 408, 429 o respuesta ilegible: reintentable
        NetworkError    // sin conexión o timeout: reintentable
    }

    public readonly struct ApiResult<T>
    {
        public readonly ApiStatus Status;
        public readonly T Value;

        private ApiResult(ApiStatus status, T value) { Status = status; Value = value; }

        public bool Ok => Status == ApiStatus.Ok;
        public bool IsRetryable => Status == ApiStatus.ServerError || Status == ApiStatus.NetworkError;

        public static ApiResult<T> Success(T value) => new ApiResult<T>(ApiStatus.Ok, value);
        public static ApiResult<T> Fail(ApiStatus status) => new ApiResult<T>(status, default);
    }

    /// <summary>Contrato HTTP con el backend. Ninguna implementación debe lanzar excepciones.</summary>
    public interface IPlayerDataApi
    {
        Task<ApiResult<string>> AuthenticateAsync(string normalizedAddress, long issuedAt, string signature);
        Task<ApiResult<PlayerState>> LoadAsync(string token);
        Task<ApiResult<PlayerState>> SaveAsync(string token, PlayerState state);
    }
}
