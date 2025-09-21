using UnityEngine;

public class RuletaLenta : MonoBehaviour
{
    public float velocidad = 20f; // grados por segundo

    void Update()
    {
        // Gira la ruleta completa
        transform.Rotate(0, 0, velocidad * Time.deltaTime);
    }
}
