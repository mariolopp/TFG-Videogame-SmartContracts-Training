using Nethereum.Util;       // Util para verificar firmas, aunque verificar en el cliente es inseguro
using System;
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
        [SerializeField] private Button signButton;
        [SerializeField] private ulong chainId = 1; // Default to Ethereum Mainnet
        

        [Header("Wallet info display")]
        [SerializeField] private GameObject LogPanel;           // Futuro panel de logs sobre la wallet en pantalla
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
                //connectWalletButton.gameObject.SetActive(true);
                //connectWalletButton.interactable = true;

                //connectWalletButton.onClick.RemoveAllListeners();
                connectWalletButton.onClick.AddListener(() =>
                {
                    Debug.Log("[WalletConnector] Button onClick listener invoked.");
                    ConnectWallet();
                    signButton.interactable = true; // Habilitar el botón de loguearse una vez conectado
                });

            }
            if (signButton == null)
            {
                Debug.LogWarning("[WalletConnector] signButton NOT assigned in Inspector.");
            }
            else
            {
                signButton.onClick.AddListener(() =>
                {
                    Debug.Log("[WalletConnector] Sign Button onClick listener invoked.");
                    Authenticate();
                });
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
        private bool WalletConnected()
        {
            var isConnected = ThirdwebManager.Instance.ActiveWallet != null;
            if (!isConnected)
            {
                // Poner como print en pantalla
                Debug.Log("Please authenticate to connect a wallet first.");
            }
            return isConnected;
        }
        public async void Authenticate()
        {
            if (!WalletConnected()) return;

            try
            {
                var wallet = ThirdwebManager.Instance.ActiveWallet;
                var address = await wallet.GetAddress();

                // 1. EL MENSAJE
                // IMPORTANTE: Debe ser IDÉNTICO letra por letra al verificar.
                string messageToSign = "Login to Unity Game\nTimestamp: " + System.DateTime.UtcNow.ToString("yyyy-MM-dd");

                // 2. LA FIRMA (El usuario firma con su Clave Privada)
                Debug.Log("Solicitando firma...");
                var signature = await wallet.PersonalSign(messageToSign);
                Debug.Log($"Firma recibida: {signature.Substring(0, 20)}...");

                // 3. VERIFICACIÓN (Recuperamos la Clave Pública/Dirección)

                // ATENCIÓN: Esta verificación se debería hacer en el servidor, no en el cliente (ya que un atacante podría modificar el cliente)
                var signer = new Nethereum.Signer.EthereumMessageSigner();
                string recoveredAddress = signer.EncodeUTF8AndEcRecover(messageToSign, signature);


                // 4. COMPARACIÓN
                // Comparamos usando la libreria Nethereum.Util para evitar problemas de mayúsculas/minúsculas
                if (recoveredAddress.IsTheSameAddress(address))
                {
                    Debug.Log("<color=green>VERIFICACIÓN EXITOSA: El usuario es legítimo.</color>");
                    //this.LogPlayground("¡Login Correcto! Bienvenido al juego.");      // Mostrar en pantalla

                    // LÓGICA DE ENTRADA AL JUEGO
                    // SceneManager.LoadScene("GameScene");
                }
                else
                {
                    Debug.LogWarning("FALLO DE VERIFICACIÓN: La firma no corresponde a esta wallet.");
                    //this.LogPlayground("Error: Firma inválida.");     // Mostrar en pantalla
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error en autenticación: {e.Message}");
                //this.LogPlayground("El usuario canceló la firma o hubo un error.");       // Mostrar en pantalla
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
                    WalletBalanceText.text = "Balance: " + balance + " ETH";
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