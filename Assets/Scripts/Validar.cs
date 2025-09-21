using UnityEngine;
using UnityEngine.UI;

public class Script : MonoBehaviour
{
    public Button boton;                 // Referencia al botón
    public BarraProgreso barra;          // Referencia a la barra original
    public Transform historialPanel;     // Panel donde guardamos clones

    void Start()
    {
        boton = GetComponent<Button>();
        barra = FindObjectOfType<BarraProgreso>();
        boton.onClick.AddListener(Pulsar);
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

        // Reiniciar la barra original
        barra.Resetear();
    }
}
