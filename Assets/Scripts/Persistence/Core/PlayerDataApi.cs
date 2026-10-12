using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Persistence
{
    /// <summary>
    /// Acceso a datos por HTTP con UnityWebRequest (funciona en Windows, Mac, Android, iOS y WebGL).
    /// Nunca lanza: todos los fallos se devuelven como ApiStatus.
    /// </summary>
    public sealed class PlayerDataApi : IPlayerDataApi
    {
        [Serializable] private class AuthRequest { public string address; public long issuedAt; public string signature; }
        [Serializable] private class AuthResponse { public string token; }

        private readonly string _baseUrl;
        private readonly int _timeoutSeconds;

        public PlayerDataApi(string baseUrl, int timeoutSeconds = 8)
        {
            _baseUrl = (baseUrl ?? string.Empty).TrimEnd('/');
            _timeoutSeconds = Math.Max(1, timeoutSeconds);
        }

        public async Task<ApiResult<string>> AuthenticateAsync(string normalizedAddress, long issuedAt, string signature)
        {
            string body = JsonUtility.ToJson(new AuthRequest { address = normalizedAddress, issuedAt = issuedAt, signature = signature });
            var (status, text) = await SendAsync("POST", "/auth", body, null);
            if (status != ApiStatus.Ok) return ApiResult<string>.Fail(status);

            var dto = TryParse<AuthResponse>(text);
            return string.IsNullOrEmpty(dto?.token)
                ? ApiResult<string>.Fail(ApiStatus.ServerError)
                : ApiResult<string>.Success(dto.token);
        }

        public async Task<ApiResult<PlayerState>> LoadAsync(string token)
        {
            var (status, text) = await SendAsync("GET", "/state", null, token);
            return ToState(status, text);
        }

        public async Task<ApiResult<PlayerState>> SaveAsync(string token, PlayerState state)
        {
            var (status, text) = await SendAsync("PUT", "/state", JsonUtility.ToJson(state), token);
            return ToState(status, text);
        }

        private static ApiResult<PlayerState> ToState(ApiStatus status, string text)
        {
            if (status != ApiStatus.Ok) return ApiResult<PlayerState>.Fail(status);
            var state = TryParse<PlayerState>(text);
            return state == null
                ? ApiResult<PlayerState>.Fail(ApiStatus.ServerError)
                : ApiResult<PlayerState>.Success(state.Sanitize());
        }

        private async Task<(ApiStatus status, string body)> SendAsync(string method, string path, string json, string token)
        {
            try
            {
                using (var req = new UnityWebRequest(_baseUrl + path, method))
                {
                    req.downloadHandler = new DownloadHandlerBuffer();
                    if (json != null)
                    {
                        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                        req.SetRequestHeader("Content-Type", "application/json");
                    }
                    if (token != null) req.SetRequestHeader("Authorization", "Bearer " + token);
                    req.timeout = _timeoutSeconds;

                    await AsTask(req.SendWebRequest());

                    if (req.result == UnityWebRequest.Result.ConnectionError) return (ApiStatus.NetworkError, null);

                    long code = req.responseCode;
                    string text = req.downloadHandler?.text;
                    if (code >= 200 && code < 300) return (ApiStatus.Ok, text);
                    if (code == 401) return (ApiStatus.Unauthorized, text);
                    if (code == 408 || code == 429) return (ApiStatus.ServerError, text);
                    if (code >= 400 && code < 500) return (ApiStatus.Rejected, text);
                    return (ApiStatus.ServerError, text);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Persistencia] {method} {path} falló: {e.Message}");
                return (ApiStatus.NetworkError, null);
            }
        }

        private static Task AsTask(UnityWebRequestAsyncOperation op)
        {
            if (op.isDone) return Task.CompletedTask;
            var tcs = new TaskCompletionSource<bool>();
            op.completed += _ => tcs.TrySetResult(true);
            return tcs.Task;
        }

        private static T TryParse<T>(string json) where T : class
        {
            try { return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<T>(json); }
            catch { return null; }
        }
    }
}
