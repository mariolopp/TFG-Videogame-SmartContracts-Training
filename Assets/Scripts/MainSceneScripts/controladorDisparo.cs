using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class controladorDisparo : MonoBehaviour
{
    [SerializeField] private GameObject prefabDisparo;
    [SerializeField] private Transform puntoDisparo;
    [SerializeField] private float velocidadDisparo = 10f;
    [SerializeField] private float tiempoEntreDisparos = 0.5f;
    private float tiempoDesdeUltimoDisparo = 0f;

    public void Update()
    {
        tiempoDesdeUltimoDisparo += Time.deltaTime;
        if (Input.GetButtonDown("Fire1") && tiempoDesdeUltimoDisparo >= tiempoEntreDisparos)
        {
            Disparar();
            tiempoDesdeUltimoDisparo = 0f;
        }
    }
    private void Disparar()
    {
        // Obtener posicion mouse mundo 2d
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = puntoDisparo.position.z;

        // Direccion en plano XY
        Vector2 origen = new Vector2(puntoDisparo.position.x, puntoDisparo.position.y);
        Vector2 destino = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
        Vector2 direccionDisparo = (destino - origen).normalized;

        // Angulo en grados
        float angulo = Mathf.Atan2(direccionDisparo.y, direccionDisparo.x) * Mathf.Rad2Deg;

        // Rotar la bala hacia la dirección calculada
        Quaternion rotacion = Quaternion.Euler(0, 0, angulo);

        Instantiate(prefabDisparo, puntoDisparo.position, rotacion);
    }
}
