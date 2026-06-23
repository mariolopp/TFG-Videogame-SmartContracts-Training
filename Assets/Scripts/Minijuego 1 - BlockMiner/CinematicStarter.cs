using UnityEngine;
using System.Collections;

public class CinematicStarter : MonoBehaviour
{
    [Header("Archivo de Diálogo")]
    [Tooltip("Archivo JSON con los dialogos")]
    [SerializeField] private TextAsset archivoDialogoJson;

    [Header("Elementos a activar al terminar")]
    [Tooltip("Canvas a encender")]
    [SerializeField] private Canvas canvasDelJuego;
    
    [Tooltip("Temporizador a activar")]
    [SerializeField] private GameObject temporizador;

    private IEnumerator Start()
    {
        // 1. Apagamos lo que molesta
        if (canvasDelJuego != null) canvasDelJuego.gameObject.SetActive(false);
        if (temporizador != null) temporizador.gameObject.SetActive(false);

        // 2. Esperar un poco para evitar condiciones de carrera
        yield return new WaitForSeconds(0.1f);

        // 3. Iniciar el diálogo usando el singleton
        if (DialogManager.Instance != null && archivoDialogoJson != null)
        {
            DialogManager.Instance.StartDialog(archivoDialogoJson, TerminarCinematica);
        }
        else
        {
            Debug.LogError("Falta asignar el JSON o el DialogManager no está en la escena.");
        }
    }

    // 3. Activar el juego por completo al terminar el diálogo. Implicito en el callback de la llamada al dialog manager
    private void TerminarCinematica()
    {
        Debug.Log("Diálogo terminado. Encendiendo el juego...");

        // Activamos de nuevo todo lo que querías
        if (canvasDelJuego != null) canvasDelJuego.gameObject.SetActive(true);
        if (temporizador != null) temporizador.gameObject.SetActive(true);

        // Una vez terminada la cinematica, la existencia de este objeto es innecesaria, lo destruimos
        Destroy(gameObject);
    }
}