using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Validar : MonoBehaviour
{
    public Button boton;                 // Referencia al bot�n
    public BarraProgreso barra;          // Referencia a la barra original
    public Transform historialPanel;     // Panel donde guardamos clones
    public float tiempoMaximo = 10f;     // Tiempo m�ximo para pulsar el bot�n
    //private float temporizador = 0f;     // Temporizador interno
    [SerializeField] private Temporizador temporizador;
    public int contadorPerdidos = 0;      // Contador de bloques perdidos
    public int contadorTotales = 0;       // Contador de bloques totales (validados + perdidos)
    public int maxBloques = 5;          // M�ximo de bloques perdidos antes de game over
    [SerializeField] private AssetsManager assets; // Referencia al script de Assets para modificar USD
    public event System.Action OnValidar;

    void Start()
    {
        boton = GetComponent<Button>();
        historialPanel = GameObject.Find("HistorialPanel").transform;
        barra.Resetear(); // Asegurarnos de que la barra empieza vacia
        boton.onClick.AddListener(Pulsar);

        // Si el temporizador llega a 0 y nohas validado el bloque, lo
        temporizador.OnTiempoAgotado += PierdesBloque;  
    }
    private void Update()
    {
    }
    private IEnumerator DeshabilitarValidarUnSec()
    {
        boton.interactable = false; // Desactiva la interacción del botón
        
        yield return new WaitForSeconds(1f); // Espera exactamente 1.0 segundos
        
        boton.interactable = true;  // Vuelve a activar el botón
    }
    public void PierdesBloque()
    {
        contadorPerdidos ++; // Incrementar el contador de bloques perdidos
        barra.Resetear(); // Reiniciar la barra original
        assets.ResetTempUSD(); // Reiniciar el valor temporal de USD
        OnValidar?.Invoke();

        temporizador.Reset(); // Reiniciar el temporizador
        StartCoroutine(DeshabilitarValidarUnSec());
    }
    //public void temporizador.onTiempoAgotado += PierdesBloque;
    void Pulsar()
    {
        // Log de la validaci�n
        //Debug.Log("La barra iba al " + (barra.valorActual * 100f).ToString("F1") + "% cuando se valid�.");
        StartCoroutine(DeshabilitarValidarUnSec());
        // Crear un clon de la barra
        barra.llenadoSuave = false; // Hacer que la barra deje de llenarse suavemente
        barra.barra.fillAmount = barra.valorActual; // Asegurarnos de que la imagen muestra el fill al valor actual
        GameObject snapshot = Instantiate(barra.gameObject, historialPanel);
        snapshot.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

        // Congelarla: quitarle el script para que no se actualice m�s
        Destroy(snapshot.GetComponent<BarraProgreso>());

        // Asegurarnos de que la imagen muestra el fill al valor actual y redimensionarla
        Slider slider = snapshot.GetComponent<Slider>();
        if (slider != null)
        {
            slider.value = barra.valorActual;
        }
        
        assets.SubmitTempUSD(); // Anyadir el valor temporal de USD al total y resetearlo

        OnValidar?.Invoke();
        // Reiniciar la barra original
        barra.Resetear();
        temporizador.Reset(); // Reiniciar el temporizador
    }
}
