using UnityEngine;

public class RuletaLenta : MonoBehaviour
{
    public float velocidad = 20f; // grados por segundo
    public Transform[] botones;
    public float radio = 225f;

    void Start()
    {
        int n = botones.Length;
        for (int i = 0; i < n; i++)
        {
            float angulo = i * Mathf.PI * 2f / n;
            Vector2 pos = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * radio;
            botones[i].GetComponent<RectTransform>().anchoredPosition = pos;
        }
    }
    void Update()
    {
        // Gira la ruleta completa
        transform.Rotate(0, 0, velocidad * Time.deltaTime);
    }
}
