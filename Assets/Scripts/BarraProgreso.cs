using UnityEngine;
using UnityEngine.UI;

public class BarraProgreso : MonoBehaviour
{
    public Image barra;           // La imagen tipo Filled (asignar en Inspector)
    public float valorActual = 0f; // valor actual de la barra
    public float velocidad = 0.5f; // velocidad de llenado
    public bool llenadoSuave = true; // Si es true, la barra se llena suavemente, si es false, se llena instantáneamente

    // Añade un valor a la barra, se llama desde el botón
    public void AnadirValor(float valor)
    {
        valorActual += valor;
        //objetivo = Mathf.Clamp(objetivo, 0f, 1f); // Evitar que supere 1
        Debug.Log("Objetivo de la barra ahora al " + (valorActual * 100f).ToString("F1") + "%");
    }

    void Update()
    {
        if (llenadoSuave) {
            // Llenado suave de la barra
            barra.fillAmount = Mathf.MoveTowards(barra.fillAmount, valorActual, velocidad * Time.deltaTime);
        }
        else { 
            barra.fillAmount = valorActual; // Llenado instantáneo
        }
    }

    public void Resetear()
    {
        valorActual = 0f;
        barra.fillAmount = 0f;
        llenadoSuave = true; // Volver al llenado suave
    }
}
