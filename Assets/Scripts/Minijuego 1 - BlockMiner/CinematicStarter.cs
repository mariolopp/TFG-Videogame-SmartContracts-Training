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

    [Header("Apariciones Progresivas de UI")]
    [Tooltip("Añade aquí las partes del Canvas que irán apareciendo con cada evento")]
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

    
    private void EscucharEvento(string nombreEvento)
    {
        ApagarTodosLosCirculos();
        if (string.IsNullOrEmpty(nombreEvento)) return;

        // A. Mostrar el círculo correspondiente (tu código original)
        if (contenedorCirculos != null)
        {
            Transform circuloDeseado = contenedorCirculos.Find(nombreEvento);
            if (circuloDeseado != null) circuloDeseado.gameObject.SetActive(true);
        }

        // B. Buscar si ese evento también debe hacer aparecer algo de la interfaz
        foreach (var item in elementosQueAparecen)
        {
            if (item.nombreEvento == nombreEvento && item.elementoUI != null)
            {
                // Si la opacidad es 0, hacemos la animación para que aparezca
                if (item.elementoUI.alpha == 0f)
                {
                    StartCoroutine(AparecerElementoUI(item.elementoUI));
                }
            }
        }
    }

    // Corrutina que hace el fundido poco a poco
    private IEnumerator AparecerElementoUI(CanvasGroup grupo)
    {
        // Bloqueamos los clics para que el jugador disfrute de la aparición
        if (DialogManager.Instance != null) DialogManager.Instance.BloquearInteraccion(true);

        float duracion = 0.5f; // Medio segundo en aparecer
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
    private void TerminarCinematica()
    {
        ApagarTodosLosCirculos();

        // Al terminar activamos el resto de la UI y desactivamos el sprite del dialogante
        if (footText != null) footText.SetActive(false);
        if (dialogueCharacter != null) dialogueCharacter.SetActive(false);
        
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