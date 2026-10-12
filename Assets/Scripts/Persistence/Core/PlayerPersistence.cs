using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game.Persistence
{
    public enum SyncStatus
    {
        Guest,        // sin sesión: partida nueva, no se lee ni se escribe nada
        LoggingIn,    // firmando y cargando
        Synced,       // datos cargados y último guardado confirmado
        Saving,
        SaveFailed,   // con sesión, pero el último guardado no llegó; el siguiente Save() lo reintenta
        Unavailable   // no se pudo cargar la partida: se juega igual, pero esta sesión no se guarda
    }

    /// <summary>
    /// Sesión de jugador y cola de guardado. No depende de MonoBehaviour para poder probarse en EditMode.
    ///
    /// Garantías:
    ///  - Sin sesión (invitado o fallo al cargar) nunca se llama al backend y Save() devuelve false.
    ///  - Solo se guarda después de haber cargado: nunca se pisan datos reales con una partida vacía.
    ///  - Cada guardado lleva la sesión con la que se pidió: aunque cambies de wallet mientras
    ///    está en vuelo, acaba en la dirección correcta.
    ///  - Se envían valores absolutos (no "+1 bolsa"), así que repetir un guardado es inocuo.
    ///  - Ningún método público lanza excepciones.
    /// </summary>
    public sealed class PlayerPersistence
    {
        public const int MaxAttempts = 4; // 1 intento + 3 reintentos (esperas de 1, 2 y 4 s)

        private sealed class Session
        {
            public readonly string Address;
            public readonly IWalletSigner Signer;
            public string Token;
            public Session(string address, IWalletSigner signer, string token) { Address = address; Signer = signer; Token = token; }
        }

        private sealed class PendingSave
        {
            public readonly Session Session;
            public PlayerState State;
            public PendingSave(Session session, PlayerState state) { Session = session; State = state; }
        }

        private enum SendResult { Saved, Failed, Superseded }

        private readonly IPlayerDataApi _api;
        private readonly Func<float, Task> _delay;
        private readonly Func<long> _unixNow;
        private readonly Action<string> _log;

        private Session _session;     // null => sin persistencia
        private int _generation;      // cambia en cada login/logout; invalida resultados de logins anteriores
        private readonly List<PendingSave> _queue = new List<PendingSave>();
        private bool _pumping;
        private Task _pump = Task.CompletedTask;

        public PlayerPersistence(IPlayerDataApi api, Func<float, Task> delay, Func<long> unixNow, Action<string> log = null)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
            _unixNow = unixNow ?? throw new ArgumentNullException(nameof(unixNow));
            _log = log ?? (_ => { });
        }

        /// <summary>Estado en memoria. Los minijuegos lo modifican y luego llaman a Save().</summary>
        public PlayerState State { get; private set; } = PlayerState.NewGame();
        public SyncStatus Status { get; private set; } = SyncStatus.Guest;
        public bool IsPersistent => _session != null;
        public string Address => _session?.Address;

        /// <summary>Se dispara al cambiar de jugador: login cargado, logout o vuelta a invitado.</summary>
        public event Action<PlayerState> StateReplaced;
        public event Action<SyncStatus> StatusChanged;

        // ------------------------------------------------------------------ sesión

        /// <summary>
        /// Firma con la wallet, obtiene token y carga la partida de esa dirección.
        /// true = datos cargados y se guardará. false = se juega como partida nueva sin guardar.
        /// </summary>
        public async Task<bool> LoginAsync(IWalletSigner signer)
        {
            int gen = StartOver(SyncStatus.LoggingIn);
            try
            {
                string rawAddress = signer == null ? null : await signer.GetAddressAsync();
                if (!EvmAddress.TryNormalize(rawAddress, out string address))
                    return Abort(gen, SyncStatus.Guest, "La wallet no devolvió una dirección EVM válida.");

                string token = await AuthenticateAsync(signer, address);
                if (gen != _generation) return false; // hubo otro login/logout mientras tanto
                if (token == null) return Abort(gen, SyncStatus.Unavailable, "No se pudo verificar la firma con el servidor.");

                var loaded = await WithRetryAsync(() => _api.LoadAsync(token));
                if (gen != _generation) return false;
                if (!loaded.Ok) return Abort(gen, SyncStatus.Unavailable, $"No se pudo cargar la partida ({loaded.Status}).");

                _session = new Session(address, signer, token);
                ReplaceState(loaded.Value);
                SetStatus(SyncStatus.Synced);
                return true;
            }
            catch (Exception e) // firma rechazada por el jugador, wallet desconectada...
            {
                return Abort(gen, SyncStatus.Guest, "Inicio de sesión cancelado: " + e.Message);
            }
        }

        /// <summary>Vuelve a modo invitado (partida nueva sin guardar). Los guardados ya pedidos se completan.</summary>
        public void Logout() => StartOver(SyncStatus.Guest);

        // ------------------------------------------------------------------ guardado

        /// <summary>
        /// Encola una foto del estado actual. Devuelve false si no hay sesión (no hace nada).
        /// Si ya había un guardado pendiente de la misma wallet, se sustituye por este (más reciente).
        /// </summary>
        public bool Save()
        {
            if (_session == null) return false;

            var snapshot = State.Clone().Sanitize();
            int last = _queue.Count - 1;
            if (last >= 0 && _queue[last].Session == _session) _queue[last].State = snapshot;
            else _queue.Add(new PendingSave(_session, snapshot));

            if (!_pumping) _pump = PumpAsync();
            return true;
        }

        /// <summary>Guarda y espera la confirmación. true = el servidor lo tiene.</summary>
        public async Task<bool> SaveAsync()
        {
            var session = _session;
            if (!Save()) return false;
            await FlushAsync();
            return session == _session && Status == SyncStatus.Synced;
        }

        /// <summary>Espera a que se vacíe la cola de guardado.</summary>
        public Task FlushAsync() => _pump;

        // ------------------------------------------------------------------ internos

        private async Task PumpAsync()
        {
            _pumping = true;
            try
            {
                while (_queue.Count > 0)
                {
                    var job = _queue[0];
                    _queue.RemoveAt(0);
                    var result = await SendAsync(job);
                    if (job.Session == _session && result != SendResult.Superseded)
                        SetStatus(result == SendResult.Saved ? SyncStatus.Synced : SyncStatus.SaveFailed);
                }
            }
            catch (Exception e)
            {
                _log("[Persistencia] Error inesperado guardando: " + e);
                if (_session != null) SetStatus(SyncStatus.SaveFailed);
            }
            finally
            {
                _pumping = false;
            }
        }

        private async Task<SendResult> SendAsync(PendingSave job)
        {
            if (job.Session == _session) SetStatus(SyncStatus.Saving);
            bool reauthTried = false;

            for (int attempt = 1; ; attempt++)
            {
                var result = await _api.SaveAsync(job.Session.Token, job.State);
                if (result.Ok) return SendResult.Saved;

                if (result.Status == ApiStatus.Unauthorized && !reauthTried)
                {
                    // Token caducado: una firma nueva de la MISMA dirección y se repite.
                    // Si la wallet ya es otra, el servidor rechaza la firma y no se escribe nada.
                    reauthTried = true;
                    string token = await TryAuthenticateAsync(job.Session.Signer, job.Session.Address);
                    if (token == null) break;
                    job.Session.Token = token;
                    attempt--;
                    continue;
                }

                if (!result.IsRetryable || attempt >= MaxAttempts) break;
                if (IsSuperseded(job)) return SendResult.Superseded;
                await _delay(BackoffSeconds(attempt));
                if (IsSuperseded(job)) return SendResult.Superseded;
            }

            _log($"[Persistencia] No se pudo guardar la partida de {job.Session.Address}. Se reintentará en el próximo guardado.");
            return SendResult.Failed;
        }

        // Hay en cola un estado más reciente de la misma sesión: este ya no hace falta.
        private bool IsSuperseded(PendingSave job) => _queue.Exists(p => p.Session == job.Session);

        private async Task<string> AuthenticateAsync(IWalletSigner signer, string address)
        {
            long issuedAt = _unixNow();
            string signature = await signer.SignMessageAsync(LoginMessage.Build(address, issuedAt));
            var result = await WithRetryAsync(() => _api.AuthenticateAsync(address, issuedAt, signature));
            return result.Ok ? result.Value : null;
        }

        private async Task<string> TryAuthenticateAsync(IWalletSigner signer, string address)
        {
            try { return await AuthenticateAsync(signer, address); }
            catch (Exception e) { _log("[Persistencia] No se pudo renovar la sesión: " + e.Message); return null; }
        }

        private async Task<ApiResult<T>> WithRetryAsync<T>(Func<Task<ApiResult<T>>> call)
        {
            for (int attempt = 1; ; attempt++)
            {
                var result = await call();
                if (result.Ok || !result.IsRetryable || attempt >= MaxAttempts) return result;
                await _delay(BackoffSeconds(attempt));
            }
        }

        private static float BackoffSeconds(int attempt) => (float)Math.Pow(2, attempt - 1); // 1, 2, 4 s

        private int StartOver(SyncStatus status)
        {
            _generation++;
            _session = null; // los guardados ya encolados conservan su propia sesión
            ReplaceState(PlayerState.NewGame());
            SetStatus(status);
            return _generation;
        }

        private bool Abort(int gen, SyncStatus status, string message)
        {
            _log("[Persistencia] " + message);
            if (gen == _generation) SetStatus(status);
            return false;
        }

        private void ReplaceState(PlayerState state)
        {
            State = (state ?? PlayerState.NewGame()).Sanitize();
            try { StateReplaced?.Invoke(State); }
            catch (Exception e) { _log("[Persistencia] Error en StateReplaced: " + e); }
        }

        private void SetStatus(SyncStatus status)
        {
            if (Status == status) return;
            Status = status;
            try { StatusChanged?.Invoke(status); }
            catch (Exception e) { _log("[Persistencia] Error en StatusChanged: " + e); }
        }
    }
}
