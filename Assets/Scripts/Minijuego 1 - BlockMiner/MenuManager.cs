using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Alternativa por índice (según el orden en Build Settings)
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
// Este script pretende aglutinar varias de las funcionalidades que podrían necesitar los botones del juego. 
// Se utiliza en todas las escenas