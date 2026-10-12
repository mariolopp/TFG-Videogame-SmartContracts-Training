using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Persistence;
using NUnit.Framework;

// Window > General > Test Runner > EditMode > Run All.
// Usan un backend falso en memoria que se comporta como el Worker (token ligado a la dirección
// que firmó), así que prueban la lógica del cliente sin red ni wallets reales.
public class PlayerPersistenceTests
{
    private static readonly string A = "0x" + new string('a', 40);
    private static readonly string B = "0x" + new string('b', 40);

    private FakeBackend _backend;
    private PlayerPersistence _p;

    [SetUp]
    public void SetUp()
    {
        _backend = new FakeBackend();
        _p = new PlayerPersistence(_backend, _ => Task.CompletedTask, () => 1_700_000_000);
    }

    [Test]
    public void DosWallets_NoSeMezclan_YCadaUnaRecuperaLoSuyo()
    {
        Assert.IsTrue(Run(_p.LoginAsync(new FakeWallet(A))));
        Assert.AreEqual(0, _p.State.bags, "wallet nueva = partida nueva");
        _p.State.bags = 10; _p.State.buildingProgress = 0.25f;
        Assert.IsTrue(Run(_p.SaveAsync()));

        Assert.IsTrue(Run(_p.LoginAsync(new FakeWallet(B))));
        Assert.AreEqual(0, _p.State.bags, "B no hereda nada de A");
        Assert.AreEqual(0f, _p.State.buildingProgress);
        _p.State.bags = 3; _p.State.buildingProgress = 0.9f;
        Assert.IsTrue(Run(_p.SaveAsync()));

        Assert.IsTrue(Run(_p.LoginAsync(new FakeWallet(A))));
        Assert.AreEqual(10, _p.State.bags);
        Assert.AreEqual(0.25f, _p.State.buildingProgress);

        Assert.AreEqual(10, _backend.Rows[A].bags);
        Assert.AreEqual(3, _backend.Rows[B].bags);
    }

    [Test]
    public void GuardadoEnVueloDeA_TerminaEnA_AunqueSeCambieAB()
    {
        Run(_p.LoginAsync(new FakeWallet(A)));
        _backend.HoldSaves = true;               // el servidor tarda en responder
        _p.State.bags = 10;
        _p.Save();

        Run(_p.LoginAsync(new FakeWallet(B)));   // se cambia de wallet con el guardado pendiente
        _backend.ReleaseSaves();

        Assert.AreEqual(10, _backend.Rows[A].bags);
        Assert.IsFalse(_backend.Rows.ContainsKey(B), "B no debe recibir datos de A");
        Assert.AreEqual(0, _p.State.bags);
    }

    [Test]
    public void Direccion_NoDistingueMayusculas()
    {
        string mixed = "0x" + new string('C', 20) + new string('c', 20);
        Run(_p.LoginAsync(new FakeWallet(mixed)));
        _p.State.bags = 7;
        Run(_p.SaveAsync());

        Run(_p.LoginAsync(new FakeWallet(mixed.ToLowerInvariant())));
        Assert.AreEqual(7, _p.State.bags);
        Assert.AreEqual(1, _backend.Rows.Count);
    }

    [Test]
    public void Invitado_NoLeeNiEscribeNada()
    {
        _p.Logout();
        _p.State.bags = 99;
        Assert.IsFalse(_p.Save());
        Assert.IsFalse(Run(_p.SaveAsync()));
        Assert.AreEqual(0, _backend.Calls);
    }

    [Test]
    public void ServidorCaido_ElJuegoSigue_YNoSeGuardaEncimaDeNada()
    {
        _backend.Down = true;
        Assert.IsFalse(Run(_p.LoginAsync(new FakeWallet(A))));
        Assert.AreEqual(SyncStatus.Unavailable, _p.Status);
        Assert.IsFalse(_p.IsPersistent);

        _p.State.bags = 5;            // se puede seguir jugando
        Assert.IsFalse(_p.Save());    // pero no se escribe una partida que no se llegó a cargar
    }

    [Test]
    public void FirmaRechazada_SeJuegaComoInvitado()
    {
        Assert.IsFalse(Run(_p.LoginAsync(new FakeWallet(A) { RejectSignature = true })));
        Assert.AreEqual(SyncStatus.Guest, _p.Status);
        Assert.AreEqual(0, _backend.Calls);
    }

    [Test]
    public void GuardadoRepetido_EsIdempotente()
    {
        Run(_p.LoginAsync(new FakeWallet(A)));
        _p.State.bags = 42; _p.State.buildingProgress = 0.5f;
        Run(_p.SaveAsync());
        Run(_p.SaveAsync());

        Run(_p.LoginAsync(new FakeWallet(A)));
        Assert.AreEqual(42, _p.State.bags);
        Assert.AreEqual(0.5f, _p.State.buildingProgress);
    }

    [Test]
    public void FalloTemporal_SeReintenta()
    {
        Run(_p.LoginAsync(new FakeWallet(A)));
        _backend.FailNextSaves = 2;   // dos errores de red y luego responde
        _p.State.bags = 8;
        Assert.IsTrue(Run(_p.SaveAsync()));
        Assert.AreEqual(8, _backend.Rows[A].bags);
    }

    // Con los dobles síncronos de abajo, las tareas terminan en la misma llamada.
    private static T Run<T>(Task<T> task)
    {
        Assert.IsTrue(task.IsCompleted, "la tarea debería haber terminado");
        return task.Result;
    }

    // ------------------------------------------------------------------ dobles

    private sealed class FakeWallet : IWalletSigner
    {
        private readonly string _address;
        public bool RejectSignature;
        public FakeWallet(string address) { _address = address; }

        public Task<string> GetAddressAsync() => Task.FromResult(_address);

        public Task<string> SignMessageAsync(string message)
        {
            if (RejectSignature) return Task.FromException<string>(new System.Exception("User rejected"));
            return Task.FromResult(_address.ToLowerInvariant() + "|" + message); // "firma" ligada a la dirección
        }
    }

    private sealed class FakeBackend : IPlayerDataApi
    {
        public readonly Dictionary<string, PlayerState> Rows = new Dictionary<string, PlayerState>();
        public int Calls;
        public bool Down;
        public bool HoldSaves;
        public int FailNextSaves;
        private readonly List<System.Action> _held = new List<System.Action>();

        public Task<ApiResult<string>> AuthenticateAsync(string address, long issuedAt, string signature)
        {
            Calls++;
            if (Down) return Task.FromResult(ApiResult<string>.Fail(ApiStatus.NetworkError));
            // Igual que el Worker: la firma tiene que ser de la dirección declarada.
            bool valid = signature == address + "|" + LoginMessage.Build(address, issuedAt);
            return Task.FromResult(valid ? ApiResult<string>.Success("tok:" + address) : ApiResult<string>.Fail(ApiStatus.Unauthorized));
        }

        public Task<ApiResult<PlayerState>> LoadAsync(string token)
        {
            Calls++;
            if (Down) return Task.FromResult(ApiResult<PlayerState>.Fail(ApiStatus.NetworkError));
            string address = token.Substring(4); // la dirección sale del token, nunca del cliente
            var state = Rows.TryGetValue(address, out var row) ? row.Clone() : PlayerState.NewGame();
            return Task.FromResult(ApiResult<PlayerState>.Success(state));
        }

        public Task<ApiResult<PlayerState>> SaveAsync(string token, PlayerState state)
        {
            Calls++;
            if (Down) return Task.FromResult(ApiResult<PlayerState>.Fail(ApiStatus.NetworkError));
            if (FailNextSaves > 0) { FailNextSaves--; return Task.FromResult(ApiResult<PlayerState>.Fail(ApiStatus.NetworkError)); }

            string address = token.Substring(4);
            var copy = state.Clone();
            if (!HoldSaves)
            {
                Rows[address] = copy;
                return Task.FromResult(ApiResult<PlayerState>.Success(copy));
            }
            var tcs = new TaskCompletionSource<ApiResult<PlayerState>>();
            _held.Add(() => { Rows[address] = copy; tcs.SetResult(ApiResult<PlayerState>.Success(copy)); });
            return tcs.Task;
        }

        public void ReleaseSaves()
        {
            HoldSaves = false;
            var pending = new List<System.Action>(_held);
            _held.Clear();
            foreach (var complete in pending) complete();
        }
    }
}
