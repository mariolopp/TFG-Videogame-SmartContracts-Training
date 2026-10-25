using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ReturnToMenu : MonoBehaviour
{
    [SerializeField] private Button backButton;

    private void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(GoToMainMenu);
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}