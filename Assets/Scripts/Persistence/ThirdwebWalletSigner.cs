using System;
using System.Threading.Tasks;
using Game.Persistence;
using Thirdweb;        // IThirdwebWallet (.NET SDK, igual en Unity SDK v5 y v6)
using Thirdweb.Unity;  // ThirdwebManager
using UnityEngine;

/// <summary>Adapta la wallet activa de thirdweb (MetaMask vía WalletConnect/Reown) a IWalletSigner.</summary>
public sealed class ThirdwebWalletSigner : IWalletSigner
{
    private readonly IThirdwebWallet _wallet;

    public ThirdwebWalletSigner(IThirdwebWallet wallet)
    {
        _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
    }

    public Task<string> GetAddressAsync() => _wallet.GetAddress();
    public Task<string> SignMessageAsync(string message) => _wallet.PersonalSign(message);
}

/// <summary>Puente entre el flujo de login de thirdweb y la persistencia.</summary>
public static class PersistenceLogin
{
    /// <summary>
    /// Pide UNA firma a la wallet conectada y carga su partida.
    /// Sustituye a la firma que hacía WalletConnector.Authenticate().
    /// true = partida de la wallet cargada; false = se juega como invitado (nada se guarda).
    /// </summary>
    public static async Task<bool> LoginWithActiveWalletAsync()
    {
        var service = PlayerDataService.Instance;
        if (service == null) return false;

        IThirdwebWallet wallet = ThirdwebManager.Instance?.ActiveWallet;
        if (wallet == null)
        {
            Debug.LogWarning("[Persistencia] No hay wallet activa");
            service.Core.Logout();
            return false;
        }

        return await service.Core.LoginAsync(new ThirdwebWalletSigner(wallet));
    }

    /// <summary>
    /// true si la sesión de persistencia es de la wallet que está conectada ahora mismo.
    /// Si el jugador ha cambiado de wallet devuelve false, y hay que llamar a LoginWithActiveWalletAsync().
    /// </summary>
    public static async Task<bool> HasSessionForActiveWalletAsync()
    {
        if (!PlayerData.IsPersistent) return false;
        try
        {
            var wallet = ThirdwebManager.Instance?.ActiveWallet;
            if (wallet == null) return false;
            string raw = await wallet.GetAddress();
            return EvmAddress.TryNormalize(raw, out string active) && active == PlayerData.Address;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Jugar sin wallet o al desconectarla: partida nueva que no se guarda.</summary>
    public static void PlayAsGuest() => PlayerDataService.Instance?.Core.Logout();
}
