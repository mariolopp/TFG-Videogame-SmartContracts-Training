using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// -----------------------------------------------
// Clase para gestionar el bot�n de transacci�n en el minijuego
// Gestiona la generaci�n aleatoria de nuevos valores de los botones
// (que simulan las transacciones)
// -----------------------------------------------
public class BotonTransaccion : MonoBehaviour
{
    [Serializable]
    public struct TipoTransaccion
    {
        public string nombre;
        public int fee;    // $ que aporta al validador
        public int gas;    // Unidades de gas que ocupa en el bloque
        public int peso;   // Probabilidad relativa de aparecer
        public TipoTransaccion(string nombre, int fee, int gas, int peso)
        {
            this.nombre = nombre; this.fee = fee; this.gas = gas; this.peso = peso;
        }
    }

    public float valorGas = 0.0f;           // Valor que aporta esta transaccion (0 a 1)
    public float valorUSD = 1f;             // Coste de esta transaccion
    public BarraGas barra;                  // Referencia a la barra (asignado en Inspector)
    private Button boton;

    // Sin transacciones de 1-2u, que harían trivial llenar el bloque exacto.
    // Con estos pesos, entre 5-6 cajitas hay una combinación que suma 10u un ~35-45% de las veces,
    // y encontrarla a tiempo mientras gira la ruleta deja el bloque perfecto en ~1-2 de cada 10
    public TipoTransaccion[] tipos =
    {
        new TipoTransaccion("Transfer", 1, 1, 1),
        new TipoTransaccion("Approve",  1, 2, 1),
        new TipoTransaccion("Claim",    2, 3, 4),
        new TipoTransaccion("Mint",     3, 3, 3),
        new TipoTransaccion("Swap",     3, 5, 2),
        new TipoTransaccion("Deposit",  4, 8, 2),
        new TipoTransaccion("Withdraw", 5, 9, 2),
    };
    public AssetsManager assets; // Referencia al script de Assets para modificar USD

    void Start()
    {
        boton = GetComponent<Button>();
        boton.onClick.AddListener(Pulsar);          // Si se pulsa el boton, se ejecuta Pulsar
        assets = FindObjectOfType<AssetsManager>();
        GenerarBoton();
    }

    public void GenerarBoton() {
        TipoTransaccion tipo = ElegirTipo();
        boton.GetComponentInChildren<TextMeshProUGUI>().text = $"{tipo.nombre}\nFee: {tipo.fee} <sprite=\"ETH_bag\" index=0>\nGas: {tipo.gas} <sprite=\"gas\" index=0>";
        valorUSD = tipo.fee;                              // $ que aporta al validador
        valorGas = tipo.gas / (float)barra.unidadesMax;        // fracción del bloque que ocupa (0 a 1)
        boton.interactable = true;                        // Activar el bot�n
    }

    // Si el boton se uso en este bloque, vuelve a activarse con una transaccion nueva
    public void RegenerarSiUsado()
    {
        if (boton != null && !boton.interactable) GenerarBoton();
    }

    // Sorteo ponderado por el peso de cada tipo
    private TipoTransaccion ElegirTipo()
    {
        int total = 0;
        foreach (TipoTransaccion t in tipos) total += Mathf.Max(0, t.peso);

        int r = UnityEngine.Random.Range(0, total);
        foreach (TipoTransaccion t in tipos)
        {
            r -= Mathf.Max(0, t.peso);
            if (r < 0) return t;
        }
        return tipos[tipos.Length - 1];
    }

    void Pulsar()
    {
        if (barra != null && (barra.valorActual + valorGas) <= 1.01f)
        {
            barra.AnadirValor(valorGas);
            assets.AddTempUSD((int)valorUSD); // Anyadir el valor usd del bloque
            Debug.Log("Anyadidos " + (int)valorUSD + " USD. Total ahora: " + assets.usd + " USD.");
            boton.interactable = false; // Desactivar boton tras pulsar
        }
        else if (barra != null) { 
            Debug.Log("La barra ya esta llena, no se puede anyadir mas valor.");
        }
    }
}
