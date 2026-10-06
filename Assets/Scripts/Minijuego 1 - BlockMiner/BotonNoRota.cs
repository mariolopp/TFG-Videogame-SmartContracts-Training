using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MantenerDerecha : MonoBehaviour
{
    public float velocidad = 50f; // grados por segundo
    public Button boton; // Referencia al bot�n
    public BotonTransaccion botonTransaccion; // Referencia al script del bot�n
    public float umbralZ = 50f;  // Mitad superior (ajustar seg�n tama�o)
    public bool cambiado = false; // Para evitar m�ltiples cambios
    public float cooldown = 0f; // Temporizador para controlar el tiempo en la zona
    public float Timer = 0f; // Temporizador para controlar el tiempo en la zona
    // Acceder a la velocidad de la ruleta
    public RuletaLenta ruleta; // Referencia al script de la ruleta

    public void Start()
    {
        if (boton == null)
        {
            Debug.LogError("El bot�n no est� asignado en el inspector.");
        }
        boton = GetComponent<Button>();
        botonTransaccion = boton.GetComponent<BotonTransaccion>();
        ruleta = FindObjectOfType<RuletaLenta>();
        if (ruleta == null)
        {
            Debug.LogError("La referencia a la ruleta no est� asignada en el inspector.");
        }
        
        cooldown = 150f/ruleta.velocidad; // Mitad del tiempo que tarda en dar una vuelta completa
        Debug.Log("Cooldown establecido en: " + cooldown + " segundos.");
    }
    void Update()
    {
        // Mantener la rotaci�n local en 0 grados
        transform.rotation = Quaternion.identity;

        // Posici�n del bot�n en coordenadas locales de la ruleta
        Vector3 rotLocal = transform.localPosition; // Obtener la rotaci�n local

        //Debug.Log("Rotaci�n local del bot�n: " + rotLocal);
        // Si est� en la mitad superior (z < umbral)

        // Cambiar valor del bot�n (ejemplo: si tiene un Text o TMP)
        TextMeshProUGUI texto = boton.GetComponentInChildren<TextMeshProUGUI>();
        if (texto != null && !boton.interactable && Timer>cooldown)
        {
            //int randIndex = Random.Range(0, posiblesTextos.Length);
            //texto.text = posiblesTextos[randIndex];
            //boton.interactable = true; // Reactivar el bot�n
            //botonTransaccion.valorUSD = botonTransaccion.posiblesValores[randIndex * 2]; // $
            //botonTransaccion.valorGas = botonTransaccion.posiblesValores[randIndex * 2 + 1]/10f; // unidades de gas

            botonTransaccion.GenerarBoton();
            Timer = 0f; // Reiniciar el temporizador
        }
        else if (texto != null && !boton.interactable)
        {
            Timer += Time.deltaTime;
        }
        else if (boton.interactable)
        {
            Timer = 0f; // Si se regenero antes de tiempo (al validar), el siguiente uso empieza la cuenta de cero
        }
    }
}
