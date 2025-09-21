using UnityEngine;

public class DistribuirBotones : MonoBehaviour
{
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
}
