using Thirdweb.Unity;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    [SerializeField] private WalletConnector wc; // Elemento UI para ETH
    [SerializeField] private Button jugarButton;
    [SerializeField] private Button salirButton;
    [SerializeField] private Button helpButton;
    [SerializeField] private Button jugarSinWalletButton;

    public void Start()
    {
        jugarButton.onClick.AddListener(Jugar);
        jugarSinWalletButton.onClick.AddListener(jugarSinWallet);
        helpButton.onClick.AddListener(help);
        salirButton.onClick.AddListener(Salir);
    }

    private void Jugar()
    {
        AudioManager.Instance.PlaySFX("select_button");
        // Si se recibe una partida vac�a de la wallet del mongoDB, iniciar tutorial - por implementar
        if (wc.GetWalletAuthenticated())
        {
            Debug.Log("Iniciando juego...");
            SceneManager.LoadScene("BlockMiner");
        }
        else if (wc.GetWalletConnected())
        {
            jugarButton.interactable = false; // Evitar m�ltiples clicks
            TutorialManager.Instance.ShowMessage("Authentication needed",
                "In the next dialog you will be prompted to sign with your connected phone wallet", () =>
                {
                    // Este c�digo solo se ejecutar� cuando el usuario pulse una tecla para cerrar el mensaje
                    wc.Authenticate();
                    jugarButton.interactable = true; // Rehabilitar el bot�n
                });
        }
        // Si se recibe una partida guardada, cargarla directamente hacia la escena principal del juego - por implementar
        else if (false)
        {
            // Por implementar
        }
        else
        {
            jugarButton.interactable = false; // Evitar m�ltiples clicks
            TutorialManager.Instance.ShowMessage("Wallet Not Connected",
                "Please connect your wallet first.", () =>
                {
                    // Este c�digo solo se ejecutar� cuando el usuario pulse una tecla para cerrar el mensaje
                    wc.ConnectWallet();
                    jugarButton.interactable = true; // Rehabilitar el bot�n
                });
        }
        
    }
    private void jugarSinWallet()
    {
        AudioManager.Instance.PlaySFX("select_button");
        // Si se recibe una partida vaca de la wallet del mongoDB, iniciar tutorial - por implementar
        
        jugarSinWalletButton.interactable = false; // Evitar mltiples clicks
        Debug.Log("Iniciando juego...");
        SceneManager.LoadScene("PoolTrader");
        
    }
    private void help()
    {
        AudioManager.Instance.PlaySFX("select_button");
        Debug.Log("Abriendo ayuda...");
        // Por implementar
    }
    private void Salir()
    {
        AudioManager.Instance.PlaySFX("select_button");
        Debug.Log("Saliendo...");
        Application.Quit();
    }
}
