using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bala : MonoBehaviour
{
    [SerializeField] private float velocidad = 20f;
    [SerializeField] private float tiempoDeVida = 5f;
    //[SerializeField] private GameObject explosionPrefab;
    [SerializeField] private int dano = 10;

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, tiempoDeVida);
    }

    // Update is called once per frame
    void Update()
    {
        // Mover la bala hacia la direcccion hacia la que fue rotada
        transform.position += transform.right * velocidad * Time.deltaTime;
    }
}
