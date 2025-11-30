using System;
using System.Threading.Tasks;
using Thirdweb;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Thirdweb.Unity
{
    public class WalletConnector : MonoBehaviour
    {
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button connectWalletButton;
        [SerializeField] private ulong chainId = 1; // Default to Ethereum Mainnet

        [Header("Wallet info display")]
        [SerializeField] private GameObject walletInfoPanel;
        [SerializeField] private TMP_Text WalletBalanceText;
        [SerializeField] private TMP_Text WalletAddressText;
        [SerializeField] private Button DisconnectWalletButton; // opcional boton de desconectar

        // External wallet
        [SerializeField] private WalletProvider externalWalletProvider = WalletProvider.ReownWallet;
        [SerializeField] private bool forceMetamaskOnWebGL = false;

        private void Awake()
        {
            Debug.Log("[WalletConnector] Awake - GameObject active: " + gameObject.activeInHierarchy + " script enabled: " + enabled);
        }

        private void Start()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            Debug.Log("[WalletConnector] Start called.");

            if (walletInfoPanel != null)
            {
                walletInfoPanel.SetActive(false);
            }

            if (connectWalletButton == null)
            {
                Debug.LogWarning("[WalletConnector] connectWalletButton NOT assigned in Inspector.");
            }
            else
            {
                // Asegurar que el botón es interactuable y añadir listener en runtime
                connectWalletButton.gameObject.SetActive(true);
                connectWalletButton.interactable = true;

                connectWalletButton.onClick.RemoveAllListeners();
                connectWalletButton.onClick.AddListener(() =>
                {
                    Debug.Log("[WalletConnector] Button onClick listener invoked.");
                    ConnectWallet();
                });

                Debug.Log("[WalletConnector] Listener added to connectWalletButton. interactable=" + connectWalletButton.interactable);
                if (connectWalletButton.targetGraphic != null)
                    Debug.Log("[WalletConnector] Button targetGraphic.raycastTarget = " + connectWalletButton.targetGraphic.raycastTarget);
            }

            // Comprobar existencia del EventSystem
            if (EventSystem.current == null)
            {
                Debug.LogWarning("[WalletConnector] No EventSystem found in scene. Add an EventSystem to receive UI clicks.");
            }
            else
            {
                Debug.Log("[WalletConnector] EventSystem present.");
            }
        }


        public void ConnectWallet()
        {
            Debug.Log("[WalletConnector] ConnectWallet invoked.");
            if (statusText != null)
            {
                statusText.text = "Connecting wallet...";
            }
            ConnectExternalWallet();
        }

        // Método público para test manual desde la consola o desde otro script
        public void TestInvokeConnect() => ConnectWallet();

        public async void ConnectExternalWallet()
        {
            try
            {
                if (ThirdwebManager.Instance == null)
                {
                    Debug.LogError("[WalletConnector] ThirdwebManager.Instance is null. Ensure ThirdwebManager is initialized.");
                    if (statusText != null)
                        statusText.text = "Thirdweb manager not ready.";
                    return;
                }

                var providerToUse = externalWalletProvider;

                // adding wallet options
                var options = new WalletOptions(
                    provider: providerToUse,
                    chainId: chainId,
                    reownOptions: new ReownOptions(projectId: "da496c193020edde136446be43d7f168")
                );

                Debug.Log("[WalletConnector] Calling ThirdwebManager.Instance.ConnectWallet...");
                var wallet = await ThirdwebManager.Instance.ConnectWallet(options);
                Debug.Log("[WalletConnector] ConnectWallet finished. wallet != null: " + (wallet != null));
                try {
                    var address = "Addr: " + await wallet.GetAddress();
                    Debug.Log("[WalletConnector] Wallet Address: " + address);
                    WalletAddressText.text = address;
                }
                catch (Exception e)
                {
                    Debug.LogError("Error al obtener dirección de wallet: " + e);
                }
                

                try
                {
                    float balance = (float)await wallet.GetBalance(421614);  // Saldo en ETH en arbitrum Sepolia
                    balance = balance / 1e18f; // Convertir de wei a ETH
                    WalletBalanceText.text = "Balance: " + balance;
                }
                catch (Exception e)
                {
                    Debug.LogError("Error al obtener balance: " + e);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[WalletConnector] Error connecting wallet: " + e);
                if (statusText != null)
                    statusText.text = "Error connecting wallet: " + e.Message;
            }
        }
    }
}