using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// -----------------------------------------------
// Clase para gestionar el bot�n de transacci�n en el minijuego
// Gestiona la generaci�n aleatoria de nuevos valores de los botones
// (que simulan las transacciones)
// -----------------------------------------------
public class BotonTransaccion : MonoBehaviour
{
    public float valorGas = 0.0f;           // Valor que aporta esta transacci�n (0 a 1)
    public float valorUSD = 1f;             // Coste de esta transacci�n (no usado en este script)
    public BarraProgreso barra;             // Referencia a la barra (asignar en Inspector)
    private Button boton;
    public string[] posiblesTextos = { "Transfer\nFee: 1$\nGas: 1u", "Swap\nFee: 3$\nGas: 2u", "Deposit\nFee:4$\nGas: 6u" };
    public float[] posiblesValores = { 1f, 1f, 3f, 2f, 4f, 6f };    // Multiplicar el indice de texto por 2 y usar ese valor y el vecino que lo sigue
    public AssetsManager assets; // Referencia al script de Assets para modificar USD

    void Start()
    {
        boton = GetComponent<Button>();
        boton.onClick.AddListener(Pulsar);          // Si se pulsa el boton, se ejecuta Pulsar
        assets = FindObjectOfType<AssetsManager>();
        GenerarBoton();
    }

    public void GenerarBoton() {
        int index = UnityEngine.Random.Range(0, posiblesTextos.Length);
        boton.GetComponentInChildren<TextMeshProUGUI>().text = posiblesTextos[index];
        valorUSD = posiblesValores[index * 2];            // $ que aporta al validador (1 a 4)
        valorGas = posiblesValores[index * 2 + 1] / 10f;  // unidades de gas que ocupa (0 a 1)
        boton.interactable = true;                        // Activar el bot�n
    }

    void Pulsar()
    {
        if (barra != null && (barra.valorActual + valorGas) <= 1.01f)
        {
            barra.AnadirValor(valorGas);
            //assets.usd = assets.usd + (int)valorUSD; // A�adir el valor USD al total sin notificar a la ui
            assets.AddUSD((int)valorUSD); // Notificar cambio de assets y a�adir el valor usd
            Debug.Log("A�adidos " + (int)valorUSD + " USD. Total ahora: " + assets.usd + " USD.");
            boton.interactable = false; // Opcional: desactivar bot�n tras pulsar
        }
        else if (barra != null) { 
            Debug.Log("La barra ya est� llena, no se puede a�adir m�s valor.");
        }
    }
}
