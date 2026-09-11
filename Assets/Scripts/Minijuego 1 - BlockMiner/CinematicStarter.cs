using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;

// NUEVO: Una pequeña lista para emparejar eventos con partes de la UI
[Serializable]
public class AparicionProgresiva
{
    public string nombreEvento;     // Ej: "marcarVida"
    public CanvasGroup elementoUI;  // El panel que tiene la barra de vida
}

public class CinematicStarter : MonoBehaviour
{
    [Header("Archivo de Diálogo")]
    [SerializeField] private TextAsset archivoDialogoJson;

    [Header("Gestor de Tutorial (Círculos)")]
    [SerializeField] private Transform contenedorCirculos;

    [Header("Cambios en la UI durante la cinemática")]
    [HideInInspector] public bool esperandoAccion = false;
    private Button botonActual;

    [Tooltip("Partes del Canvas que irán apareciendo con cada evento")]
    [SerializeField] private AparicionProgresiva[] elementosQueAparecen;

    [Header("Elementos a Activar al Final de la Cinemática")]
    [SerializeField] private GameObject dialogueCharacter; 
    [SerializeField] private GameObject gameCanvas;
    // Nota: Ya no apagamos el Canvas entero aquí, porque lo necesitamos encendido para mostrar sus partes poco a poco.

    [SerializeField] private GameObject footText;

    
    [Header("Botón de Saltar")]
    [SerializeField] private Button botonSaltar;

    private void OnEnable()
    {
        DialogManager.OnDialogEvent += EscucharEvento;
    }

    private void OnDisable()
    {
        DialogManager.OnDialogEvent -= EscucharEvento;
    }

    

    private IEnumerator Start()
    {
        ApagarTodosLosCirculos();
        AudioManager.Instance.PlaySFX("wosh");
        foreach (var item in elementosQueAparecen)
        {
            if (item.elementoUI != null)
            {
                item.elementoUI.alpha = 0f;
                item.elementoUI.interactable = false;
                item.elementoUI.blocksRaycasts = false;
            }
        }

        if (botonSaltar != null)
        {
            botonSaltar.onClick.AddListener(SaltarDialogo);
        }

        yield return new WaitForSeconds(0.1f);

        if (DialogManager.Instance != null && archivoDialogoJson != null)
        {
            DialogManager.Instance.StartDialog(archivoDialogoJson, TerminarCinematica);
        }
    }

    // NUEVO: Salta directamente al final de la cinemática
    public void SaltarDialogo()
    {
        // Paramos cualquier corrutina propia (fundidos de elementosQueAparecen, etc.)
        StopAllCoroutines();

        // Forzamos que toda la UI progresiva aparezca de golpe
        foreach (var item in elementosQueAparecen)
        {
            if (item.elementoUI != null)
            {
                item.elementoUI.alpha = 1f;
                item.elementoUI.interactable = false;
                item.elementoUI.blocksRaycasts = true;
            }
        }

        // Esto detiene el DialogManager y dispara TerminarCinematica automáticamente
        if (DialogManager.Instance != null)
        {
            DialogManager.Instance.StopDialog();
        }
        else
        {
            // Fallback por si DialogManager no existiera
            TerminarCinematica();
        }
    }

    // En función del evento del diálogo se mostrará un objeto y circulo distinto
    private void EscucharEvento(string nombreEvento)
    {
        ApagarTodosLosCirculos();
        if (string.IsNullOrEmpty(nombreEvento)) return;

        if (nombreEvento.StartsWith("esperarAccion"))
        {
            GameObject button = GameObject.FindGameObjectWithTag(nombreEvento);

            if (button != null)
            {
                botonActual = button.GetComponent<Button>();

                if (botonActual != null)
                {
                    botonActual.gameObject.SetActive(true); // lo activamos, por si estaba oculto
                    botonActual.GetComponents<CanvasGroup>()[0].interactable = true; // aseguramos que sea interactuable

                    if (DialogManager.Instance != null)
                    {
                        esperandoAccion = true;
                        DialogManager.Instance.PauseDialog(); // pausa + oculta la ventana
                    }

                    botonActual.onClick.AddListener(AlPulsarBotonTemporal);

                    Debug.Log("Se encontró un objeto con el tag: " + nombreEvento + " y se asignó la función para reanudar el diálogo al pulsarlo");

                    StartCoroutine(EsperarHastaContinuar());
                }
            }
            else
            {
                Debug.LogWarning("No se encontró ningún objeto con el tag: " + nombreEvento);
            }
        }
        else
        {
            if (contenedorCirculos != null)
            {
                Transform circuloDeseado = contenedorCirculos.Find(nombreEvento);
                if (circuloDeseado != null) circuloDeseado.gameObject.SetActive(true);
            }

            foreach (var item in elementosQueAparecen)
            {
                if (item.nombreEvento == nombreEvento && item.elementoUI != null)
                {
                    if (item.elementoUI.alpha == 0f)
                    {
                        StartCoroutine(AparecerElementoUI(item.elementoUI));
                    }
                }
            }
        }
    }

    // NUEVA corrutina: solo se encarga de esperar
    private IEnumerator EsperarHastaContinuar()
    {
        yield return new WaitUntil(() => !esperandoAccion);
        DialogManager.Instance.ResumeDialogAndAdvance();
    }

    // Corrutina que hace el fundido poco a poco
    private IEnumerator AparecerElementoUI(CanvasGroup grupo)
    {
        // Bloqueamos los clics para que el jugador disfrute de la aparición
        if (DialogManager.Instance != null) DialogManager.Instance.BloquearInteraccion(true);

        float duracion = 1f; // Un segundo en aparecer
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            grupo.alpha = tiempo / duracion;
            yield return null;
        }

        // Lo dejamos 100% visible y utilizable
        grupo.alpha = 1f;
        grupo.interactable = false;
        grupo.blocksRaycasts = true;

        if (DialogManager.Instance != null) DialogManager.Instance.BloquearInteraccion(false);
    }

    private void ApagarTodosLosCirculos()
    {
        if (contenedorCirculos == null) return;
        foreach (Transform hijo in contenedorCirculos)
        {
            hijo.gameObject.SetActive(false);
        }
    }

    private void AlPulsarBotonTemporal()
    {
        esperandoAccion = false;

        if (botonActual != null)
        {
            botonActual.onClick.RemoveListener(AlPulsarBotonTemporal);
            botonActual.GetComponents<CanvasGroup>()[0].interactable = false; // aseguramos que no sea interactuable
            //botonActual.gameObject.SetActive(false); // lo apagamos hasta la próxima vez que haga falta
        }
    }
    private void TerminarCinematica()
    {
        ApagarTodosLosCirculos();

        // Al terminar activamos el resto de la UI y desactivamos el sprite del dialogante
        if (footText != null) footText.SetActive(false);
        if (dialogueCharacter != null) dialogueCharacter.SetActive(false);
        if (botonSaltar != null) botonSaltar.gameObject.SetActive(false);
        
        if (gameCanvas != null)
        { 
            // Encender absolutamente todos los objetos (hijos, nietos, etc.)
            Transform[] todosLosDescendientes = gameCanvas.GetComponentsInChildren<Transform>(true);
            foreach (Transform objeto in todosLosDescendientes)
            {
                objeto.gameObject.SetActive(true);
            }

            // Buscar todos los CanvasGroup de la UI y forzar que sean interactuables
            CanvasGroup[] todosLosGrupos = gameCanvas.GetComponentsInChildren<CanvasGroup>(true);
            foreach (CanvasGroup grupo in todosLosGrupos)
            {
                grupo.alpha = 1f;             // 100% de visibilidad
                grupo.interactable = true;    // Permite interactuar (pulsar botones, ruleta, etc.)
                grupo.blocksRaycasts = true;  // Permite que los elementos detecten el ratón/toques
            }
        }

        Destroy(gameObject);
    }
}