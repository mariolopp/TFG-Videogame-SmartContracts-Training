using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class blockMinerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI bloquesRestantes;
    private AssetsManager assets;
    public int bRestantes = 5;  // El décimo boque es el primero
    [SerializeField] public Temporizador temporizador;
    public Validar Validar;

    void Start()
    {
        //temporizador = FindObjectOfType<Temporizador>();
        assets = FindObjectOfType<AssetsManager>();
        //bloquesRestantes = transform.Find("BloquesRestantesText").GetComponent<TextMeshProUGUI>();
        temporizador.OnTiempoCambiado += ActualizarMinerUI;
        Validar = FindObjectOfType<Validar>();
        Validar.OnValidar += DisminuirBloquesUI;
        DisminuirBloquesUI();
        //ActualizarUI(); // inicializar
    }

    private void OnDestroy()
    {
        assets.OnAssetsChanged -= ActualizarMinerUI;
        temporizador.OnTiempoCambiado -= ActualizarMinerUI;
        if (Validar != null) Validar.OnValidar -= DisminuirBloquesUI;
    }

    private void ActualizarMinerUI()
    {
        
    }
    private void DisminuirBloquesUI()
    {
        bRestantes--;
        bloquesRestantes.text = bRestantes.ToString() + "";
        // COMPROBACIÓN DE GAME OVER
        if (bRestantes <= 0)
        {
            TerminarMinijuego();
        }
    }
    private void TerminarMinijuego()
    {
        temporizador.StopAllCoroutines(); // Detener el temporizador
        Validar.boton.interactable = false; // Desactivar el botón de validar
        Time.timeScale = 0f; // Detener el tiempo del juego

        // Desactivar todos los botones de la ruleta
        RuletaLenta ruleta = FindObjectOfType<RuletaLenta>();
        if (ruleta != null && ruleta.botones != null)
        {
            foreach (Transform t in ruleta.botones)
            {
                // Desactivar el botón de la UI
                Button b = t.GetComponent<Button>();
                if (b != null)
                {
                    b.interactable = false;
                }

                // Desactivar su script "MantenerDerecha" para que no intente autoregenerarse
                MantenerDerecha scriptMantener = t.GetComponent<MantenerDerecha>();
                if (scriptMantener != null)
                {
                    scriptMantener.enabled = false;
                }
            }
            ruleta.enabled = false;
        }
        
        //if(panelEndBlockMiner != null) panelEndBlockMiner.SetActive(true); // Mostrar el panel de fin de juego


    }
}
