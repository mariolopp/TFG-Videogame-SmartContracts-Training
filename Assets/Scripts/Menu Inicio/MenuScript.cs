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
    [SerializeField] private Button languageButton;

    private const string GameScene = "MainScene";

    public void Start()
    {
        jugarButton.onClick.AddListener(Jugar);
        jugarSinWalletButton.onClick.AddListener(jugarSinWallet);
        helpButton.onClick.AddListener(help);
        salirButton.onClick.AddListener(Salir);
        // El menú de idioma copia el estilo del botón Help
        LanguageMenuButton.Setup(languageButton, helpButton);
    }

    private async void Jugar()
    {
        AudioManager.Instance.PlaySFX("select_button");

        if (!wc.GetWalletConnected())
        {
            jugarButton.interactable = false; // Evitar múltiples clicks
            TutorialManager.Instance.ShowMessage(L("Wallet no conectada", "Wallet Not Connected"),
                L("Primero conecta tu wallet.", "Please connect your wallet first."), () =>
                {
                    wc.ConnectWallet();
                    jugarButton.interactable = true;
                });
            return;
        }

        // Ya hay partida cargada de ESTA wallet (p. ej. al volver al menú): entrar directamente.
        // Si el jugador ha cambiado de wallet, esto da false y se carga la partida de la nueva.
        if (await PersistenceLogin.HasSessionForActiveWalletAsync())
        {
            LoadGame();
            return;
        }

        jugarButton.interactable = false; // Evitar múltiples clicks
        TutorialManager.Instance.ShowMessage(L("Autenticación necesaria", "Authentication needed"),
            L("En el siguiente paso se te pedirá firmar con la wallet conectada en tu móvil",
              "In the next dialog you will be prompted to sign with your connected phone wallet"), async () =>
            {
                // Una sola firma: verifica la wallet en el servidor y carga su partida.
                bool loaded = await PersistenceLogin.LoginWithActiveWalletAsync();
                jugarButton.interactable = true;

                if (loaded)
                {
                    LoadGame();
                    return;
                }

                // Firma rechazada o servidor caído: se puede jugar igual, sin guardar.
                TutorialManager.Instance.ShowMessage(L("Progreso no disponible", "Progress not available"),
                    L("No se ha podido cargar tu progreso guardado. Puedes jugar, pero esta partida no se guardará.",
                      "We couldn't load your saved progress. You can play now, but this session won't be saved."),
                    () => LoadGame());
            });
    }

    private void jugarSinWallet()
    {
        AudioManager.Instance.PlaySFX("select_button");
        jugarSinWalletButton.interactable = false; // Evitar múltiples clicks

        PersistenceLogin.PlayAsGuest(); // partida nueva que no se lee ni se guarda
        LoadGame();
    }

    // La primera partida (estatua a 0) empieza directamente en BlockMiner;
    // al terminarlo el jugador aparece en el mapa delante de su stand
    private static void LoadGame()
    {
        if (GameProgress.IsNewGame)
        {
            StandInteractor.SetReturnStand(GameProgress.BlockMiner);
            SceneManager.LoadScene(GameProgress.BlockMiner);
            return;
        }

        SceneManager.LoadScene(GameScene);
    }

    private void help()
    {
        AudioManager.Instance.PlaySFX("select_button");
        HelpPanel.Show();
    }

    private static string L(string es, string en) => LanguageManager.Pick(es, en);

    private void Salir()
    {
        AudioManager.Instance.PlaySFX("select_button");
        Debug.Log("Saliendo...");
#if UNITY_EDITOR
        // Application.Quit no hace nada en el editor: se detiene el modo Play
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
