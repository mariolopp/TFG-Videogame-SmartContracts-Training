using TMPro;
using UnityEngine;
using System.Collections;

public class Temporizador : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textoTemporizador;
    public int tiempoInicial = 10; // segundos
    public event System.Action OnTiempoAgotado;
    public event System.Action OnTiempoReset;
    public event System.Action OnTiempoCambiado;
    private int tiempoRestante;


    void Start()
    {
        tiempoRestante = tiempoInicial;
        textoTemporizador.text = tiempoRestante.ToString();
        StartCoroutine(ContarTiempo());
    }

    IEnumerator ContarTiempo()
    {
        while (tiempoRestante > 0)
        {
            yield return new WaitForSeconds(1f);
            tiempoRestante--;
            textoTemporizador.text = tiempoRestante.ToString();
            OnTiempoCambiado?.Invoke();
        }

        // Aqu� ya se acab� el tiempo
        textoTemporizador.text = "0";
        Debug.Log("Tiempo agotado");

        // Notificar que el tiempo llego a 0
        OnTiempoAgotado?.Invoke();
    }
    public void Reset()
    {
        tiempoRestante = tiempoInicial;
        textoTemporizador.text = tiempoRestante.ToString();
        StopAllCoroutines();
        StartCoroutine(ContarTiempo());
    }
}
