using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MantenerDerecha : MonoBehaviour
{
    public float velocidad = 50f; // grados por segundo
    public Button boton; // Referencia al botón
    public BotonTransaccion botonTransaccion; // Referencia al script del botón
    public float umbralZ = 50f;  // Mitad superior (ajustar según tamaño)
    public bool cambiado = false; // Para evitar múltiples cambios
    public float cooldown = 0f; // Temporizador para controlar el tiempo en la zona
    public float Timer = 0f; // Temporizador para controlar el tiempo en la zona
    // Acceder a la velocidad de la ruleta
    public RuletaLenta ruleta; // Referencia al script de la ruleta

    private string[] posiblesTextos = { "Transfer\nFee: 1$\nGas: 1u", "Swap\nFee: 3$\nGas: 2u", "Deposit\nFee:4$\nGas: 6u" };
    private float[] posiblesValores = { 1f,1f,3f,2f,4f,6f };    // Habra que multiplicar el indice de texto por 2 y usar ese valor y su vecino siguiente

    public void Start()
    {
        if (boton == null)
        {
            Debug.LogError("El botón no está asignado en el inspector.");
        }
        boton = GetComponent<Button>();
        botonTransaccion = boton.GetComponent<BotonTransaccion>();
        ruleta = FindObjectOfType<RuletaLenta>();
        if (ruleta == null)
        {
            Debug.LogError("La referencia a la ruleta no está asignada en el inspector.");
        }
        
        cooldown = 150f/ruleta.velocidad; // Mitad del tiempo que tarda en dar una vuelta completa
        Debug.Log("Cooldown establecido en: " + cooldown + " segundos.");
    }
    void Update()
    {
        // Mantener la rotación local en 0 grados
        transform.rotation = Quaternion.identity;

        // Posición del botón en coordenadas locales de la ruleta
        Vector3 rotLocal = transform.localPosition; // Obtener la rotación local

        //Debug.Log("Rotación local del botón: " + rotLocal);
        // Si está en la mitad superior (z < umbral)

        // Cambiar valor del botón (ejemplo: si tiene un Text o TMP)
        TextMeshProUGUI texto = boton.GetComponentInChildren<TextMeshProUGUI>();
        if (texto != null && !boton.interactable && Timer>cooldown)
        {
            //int randIndex = Random.Range(0, posiblesTextos.Length);
            //texto.text = posiblesTextos[randIndex];
            //boton.interactable = true; // Reactivar el botón
            //botonTransaccion.valorUSD = botonTransaccion.posiblesValores[randIndex * 2]; // $
            //botonTransaccion.valorGas = botonTransaccion.posiblesValores[randIndex * 2 + 1]/10f; // unidades de gas

            botonTransaccion.GenerarBoton();
            Timer = 0f; // Reiniciar el temporizador
        }
        else if (texto != null && !boton.interactable)
        {
            Timer += Time.deltaTime;
        }
    }
}
