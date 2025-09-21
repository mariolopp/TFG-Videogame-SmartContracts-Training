using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    public void Jugar()
    {
        Debug.Log("Iniciando juego...");
        SceneManager.LoadScene("Tutorial");
    }
    public void Salir()
    {
        Debug.Log("Saliendo...");
        Application.Quit();
    }
}
