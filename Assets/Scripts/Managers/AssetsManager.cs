using System.Xml.Linq;
using UnityEngine;


public class AssetsManager : MonoBehaviour
{
    public event System.Action OnAssetsChanged;
    public static AssetsManager Instance;
    public int usd = 0;
    public int eth = 0;
    public int t_usd = 0;

    private void Awake()
    {
        // Patr�n Singleton b�sico
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // No se destruye al cargar nueva escena
        }
        else
        {
            Destroy(gameObject); // Evita duplicados si vuelves a la escena inicial
        }
    }
    private void Start()
    {
        // Inicializar con 1000 USD y 0 ETH
        usd = 0;
        eth = 0;
        OnAssetsChanged?.Invoke();
    }
    public void AddUSD(int amount)
    {
        usd += amount;
        OnAssetsChanged?.Invoke();
    }

    public void AddTempUSD(int amount)
    {
        t_usd += amount;
        OnAssetsChanged?.Invoke();
    }
    public void SubmitTempUSD()
    {
        usd += t_usd;
        ResetTempUSD();
    }
    public void ResetTempUSD()
    {
        t_usd = 0;
        OnAssetsChanged?.Invoke();
    }

    public void SpendUSD(int amount)
    {
        usd -= amount;
        OnAssetsChanged?.Invoke();
    }
}
