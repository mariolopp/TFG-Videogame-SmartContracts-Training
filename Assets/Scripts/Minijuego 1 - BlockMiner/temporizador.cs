using TMPro;
using UnityEngine;
using System.Collections;

public class Temporizador : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textoTemporizador;
    public float tiempoInicial = 10f; // segundos
    public BarraProgreso barraTime;          // Referencia a la barra original
    public event System.Action OnTiempoAgotado;
    public event System.Action OnTiempoReset;
    public event System.Action OnTiempoCambiado;
    private float tiempoRestante;


    void Start()
    {
        tiempoRestante = tiempoInicial;
        textoTemporizador.text = tiempoRestante.ToString();
        barraTime.Resetear(); // Asegurarnos de que la barra empieza llena
        StartCoroutine(ContarTiempo());
    }

    IEnumerator ContarTiempo()
    {
        while (tiempoRestante > 0)
        {
            yield return new WaitForSeconds(1f);
            tiempoRestante--;
            barraTime.AnadirValor(-(1f / tiempoInicial)); // Actualizar la barra
            textoTemporizador.text = tiempoRestante.ToString();
            OnTiempoCambiado?.Invoke();
        }

        // Aqu ya se acab el tiempo
        textoTemporizador.text = "0";
        Debug.Log("Tiempo agotado");

        // Notificar que el tiempo llego a 0
        OnTiempoAgotado?.Invoke();
    }
    public void Reset()
    {
        StopAllCoroutines();
        tiempoRestante = tiempoInicial;
        textoTemporizador.text = tiempoRestante.ToString();
        barraTime.Resetear(); // Reiniciar la barra
        StartCoroutine(ContarTiempo());
    }
}
