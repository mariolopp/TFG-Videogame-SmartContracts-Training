using System;
using UnityEngine;
using UnityEngine.UI;

public class Validar : MonoBehaviour
{
    public Button boton;                 // Referencia al botón
    public BarraProgreso barra;          // Referencia a la barra original
    public Transform historialPanel;     // Panel donde guardamos clones
    public float tiempoMaximo = 10f;     // Tiempo máximo para pulsar el botón
    private float temporizador = 0f;     // Temporizador interno
    public int contadorBloques = 0;      // Contador de bloques perdidos
    public int maxBloques = 5;          // Máximo de bloques perdidos antes de game over
    public event System.Action OnValidar;

    void Start()
    {
        boton = GetComponent<Button>();
        barra = FindObjectOfType<BarraProgreso>();
        historialPanel = GameObject.Find("HistorialPanel").transform;
        boton.onClick.AddListener(Pulsar);

    }
    private void Update()
    {
        temporizador += Time.deltaTime;
        if (temporizador >= tiempoMaximo)
        {
            PierdesBloque();
        }
    }

    public void PierdesBloque()
    {
        contadorBloques ++; // Incrementar el contador de bloques perdidos
        barra.Resetear(); // Reiniciar la barra original
    }

    void Pulsar()
    {
        // Log de la validación
        Debug.Log("La barra iba al " + (barra.valorActual * 100f).ToString("F1") + "% cuando se validó.");

        // Crear un clon de la barra
        barra.llenadoSuave = false; // Hacer que la barra deje de llenarse suavemente
        barra.barra.fillAmount = barra.valorActual; // Asegurarnos de que la imagen muestra el fill al valor actual
        GameObject snapshot = Instantiate(barra.gameObject, historialPanel);
        snapshot.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
        // Congelarla: quitarle el script para que no se actualice más
        Destroy(snapshot.GetComponent<BarraProgreso>());

        // Asegurarnos de que la imagen muestra el fill al valor actual y redimensionarla
        Slider slider = snapshot.GetComponent<Slider>();
        if (slider != null)
        {
            slider.value = barra.valorActual;
        }
        OnValidar?.Invoke();
        // Reiniciar la barra original
        barra.Resetear();
    }
}
