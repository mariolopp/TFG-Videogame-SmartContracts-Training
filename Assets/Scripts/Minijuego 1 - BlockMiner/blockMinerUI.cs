using TMPro;
using UnityEngine;

public class blockMinerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI bloquesRestantes;
    private AssetsManager assets;
    public int bRestantes = 5;
    [SerializeField] public Temporizador temporizador;
    public Validar Validar;

    void Start()
    {
        //temporizador = FindObjectOfType<Temporizador>();
        assets = FindObjectOfType<AssetsManager>();
        //bloquesRestantes = transform.Find("BloquesRestantesText").GetComponent<TextMeshProUGUI>();
        temporizador.OnTiempoCambiado += ActualizarMinerUI;
        Validar = FindObjectOfType<Validar>();
        Validar.OnValidar += ActualizarBloquesUI;
        ActualizarBloquesUI();
        //ActualizarUI(); // inicializar
    }

    private void OnDestroy()
    {
        assets.OnAssetsChanged -= ActualizarMinerUI;
    }

    private void ActualizarMinerUI()
    {
        
    }
    private void ActualizarBloquesUI()
    {
        bRestantes--;
        bloquesRestantes.text = bRestantes.ToString() + "bloques restantes";
    }
}
