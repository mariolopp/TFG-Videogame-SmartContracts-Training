using UnityEngine;
using UnityEngine.UI; // Necesario para interactuar con componentes de UI

public class RuletaLenta : MonoBehaviour
{
    public float velocidad = 20f; // grados por segundo
    public Transform[] botones;
    public float radio = 225f;

    [Header("Configuración de la Cadena")]
    public GameObject prefabCadena; // Asigna aquí el Prefab que creaste en el Paso 1
    public float grosorCadena = 20f; // Altura (grosor) de la imagen de la cadena en la UI

    void Start()
    {
        int n = botones.Length;
        Vector2[] posicionesBotones = new Vector2[n];

        // 1. Posicionar primero todos los botones y guardar sus posiciones
        for (int i = 0; i < n; i++)
        {
            float angulo = i * Mathf.PI * 2f / n;
            Vector2 pos = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo)) * radio;
            botones[i].gameObject.SetActive(true);
            botones[i].GetComponent<RectTransform>().anchoredPosition = pos;
            
            posicionesBotones[i] = pos; // Guardamos la posición para el cálculo de las cadenas
        }

        // 2. Generar las cadenas entre los botones
        if (prefabCadena != null)
        {
            for (int i = 0; i < n; i++)
            {
                Vector2 posA = posicionesBotones[i];
                // El siguiente botón. Si es el último (n-1), se conecta con el primero (0) gracias al operador %
                Vector2 posB = posicionesBotones[(i + 1) % n]; 

                // Instanciar la cadena como hija de la ruleta (transform) para que gire con ella
                GameObject nuevaCadena = Instantiate(prefabCadena, transform);
                RectTransform rectCadena = nuevaCadena.GetComponent<RectTransform>();

                // Colocar la cadena justo debajo de los botones en la jerarquía para que no los tape visualmente
                nuevaCadena.transform.SetAsFirstSibling();

                // A. Posicionar la cadena en el punto medio exacto entre el Botón A y el Botón B
                rectCadena.anchoredPosition = (posA + posB) / 2f;

                // B. Calcular la dirección y el ángulo de rotación necesario
                Vector2 direccion = posB - posA;
                float anguloGrados = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
                rectCadena.localRotation = Quaternion.Euler(0, 0, anguloGrados);

                // C. Ajustar el tamaño (Ancho = distancia entre botones, Alto = grosor de la cadena)
                float distanciaEntreBotones = Vector2.Distance(posA, posB);
                rectCadena.sizeDelta = new Vector2(distanciaEntreBotones, grosorCadena);
            }
        }
        else 
        {
            Debug.LogWarning("No se ha asignado el prefab de la cadena en el inspector de la Ruleta.");
        }
    }

    void Update()
    {
        // Gira la ruleta completa (los botones y las cadenas girarán juntos al ser hijos de este objeto)
        transform.Rotate(0, 0, velocidad * Time.deltaTime);
    }
}