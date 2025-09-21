using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HistorialManager : MonoBehaviour
{
    public Transform historialPanel; // Panel con Vertical Layout
    public GameObject miniBarPrefab; // Prefab creado
    public float escala = 0.5f;      // Tamaño de la barra

    public void GuardarBarra(float fillAmount)
    {
        GameObject miniBar = Instantiate(miniBarPrefab, historialPanel);
        miniBar.transform.localScale = Vector3.one * escala;

        // Ajustar barra roja según fillAmount
        Image barraRoja = miniBar.transform.Find("BarraRoja").GetComponent<Image>();
        barraRoja.fillAmount = fillAmount;

        // Ajustar texto
        TextMeshProUGUI texto = miniBar.transform.Find("Texto").GetComponent<TextMeshProUGUI>();
        texto.text = (fillAmount * 100f).ToString("F0") + "%";
    }
}
