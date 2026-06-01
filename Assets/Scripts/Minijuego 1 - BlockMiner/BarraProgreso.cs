using UnityEngine;
using UnityEngine.UI;

public class BarraProgreso : MonoBehaviour
{
    public Image barra;           // La imagen tipo Filled (asignar en Inspector)
    public float valorInicio = 0f; // Valor inicial de la barra en cada reset
    public float valorActual = 0f; // valor actual de la barra
    public float velocidad = 0.5f; // velocidad de llenado
    public bool llenadoSuave = true; // Si es true, la barra se llena suavemente, si es false, se llena instant�neamente


    // A�ade un valor a la barra, se llama desde el bot�n
    public void AnadirValor(float valor)
    {
        valorActual += valor;
        //objetivo = Mathf.Clamp(objetivo, 0f, 1f); // Evitar que supere 1
    }

    void Update()
    {
        if (llenadoSuave) {
            // Llenado suave de la barra
            barra.fillAmount = Mathf.MoveTowards(barra.fillAmount, valorActual, velocidad * Time.deltaTime);
        }
        else { 
            barra.fillAmount = valorActual; // Llenado instant�neo
        }
    }

    public void Resetear() 
    {
        valorActual = valorInicio;      // Reiniciar al valor inicial
        barra.fillAmount = valorActual; // Actualizar la barra
        llenadoSuave = true;            // Volver al llenado suave
    }
}
  