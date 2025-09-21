using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BotonTransaccion : MonoBehaviour
{
    public float valorGas = 0.2f;          // Valor que aporta esta transacción (0 a 1)
    public float valorUSD = 1f;          // Coste de esta transacción (no usado en este script)
    public BarraProgreso barra;          // Referencia a la barra (asignar en Inspector)
    private Button boton;
    public string[] posiblesTextos = { "Transfer\nFee: 1$\nGas: 1u", "Swap\nFee: 3$\nGas: 2u", "Deposit\nFee:4$\nGas: 6u" };
    public float[] posiblesValores = { 1f, 1f, 3f, 2f, 4f, 6f };    // Habra que multiplicar el indice de texto por 2 y usar ese valor y su vecino siguiente
    public AssetsManager assets; // Referencia al script de Assets para modificar USD

    void Start()
    {
        boton = GetComponent<Button>();
        barra = FindObjectOfType<BarraProgreso>(); // Busca la barra en la escena
        boton.onClick.AddListener(Pulsar); // Configura el OnClick automáticamente
        assets = FindObjectOfType<AssetsManager>();
        GenerarBoton();
    }

    public void GenerarBoton() {
        int index = UnityEngine.Random.Range(0, posiblesTextos.Length);
        boton.GetComponentInChildren<TextMeshProUGUI>().text = posiblesTextos[index];
        valorUSD = posiblesValores[index * 2]; // $
        valorGas = posiblesValores[index * 2 + 1] / 10f; // unidades de gas
        boton.interactable = true; // Activar el botón
    }

    void Pulsar()
    {
        if (barra != null && barra.valorActual < 1)
        {
            barra.AnadirValor(valorGas);
            //assets.usd = assets.usd + (int)valorUSD; // Añadir el valor USD al total sin notificar a la ui
            assets.AddUSD((int)valorUSD); // Notificar cambio de assets y añadir el valor usd
            Debug.Log("Añadidos " + (int)valorUSD + " USD. Total ahora: " + assets.usd + " USD.");
            boton.interactable = false; // Opcional: desactivar botón tras pulsar
        }
        else if (barra != null) { 
            Debug.Log("La barra ya está llena, no se puede añadir más valor.");
        }
    }
}
