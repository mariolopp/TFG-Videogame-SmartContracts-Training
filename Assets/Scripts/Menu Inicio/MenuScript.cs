using Thirdweb.Unity;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    [SerializeField] private WalletConnector wc; // Elemento UI para ETH
    [SerializeField] private Button jugarButton;
    [SerializeField] private Button salirButton;

    public void Start()
    {
        jugarButton.onClick.AddListener(Jugar);
        salirButton.onClick.AddListener(Salir);
    }

    private void Jugar()
    {
        // Si se recibe una partida vacía de la wallet del mongoDB, iniciar tutorial - por implementar
        if (wc.GetWalletAuthenticated())
        {
            Debug.Log("Iniciando juego...");
            SceneManager.LoadScene("BlockMiner");
        }
        else if (wc.GetWalletConnected())
        {
            jugarButton.interactable = false; // Evitar múltiples clicks
            TutorialManager.Instance.ShowMessage("Authentication needed",
                "In the next dialog you will be prompted to sign with your connected phone wallet", () =>
                {
                    // Este código solo se ejecutará cuando el usuario pulse una tecla para cerrar el mensaje
                    wc.Authenticate();
                    jugarButton.interactable = true; // Rehabilitar el botón
                });
        }
        // Si se recibe una partida guardada, cargarla directamente hacia la escena principal del juego - por implementar
        else if (false)
        {
            // Por implementar
        }
        else
        {
            jugarButton.interactable = false; // Evitar múltiples clicks
            TutorialManager.Instance.ShowMessage("Wallet Not Connected",
                "Please connect your wallet first.", () =>
                {
                    // Este código solo se ejecutará cuando el usuario pulse una tecla para cerrar el mensaje
                    wc.ConnectWallet();
                    jugarButton.interactable = true; // Rehabilitar el botón
                });
        }
        
    }

    private void Salir()
    {
        Debug.Log("Saliendo...");
        Application.Quit();
    }
}
