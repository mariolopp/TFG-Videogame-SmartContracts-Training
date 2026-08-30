using System.Xml.Linq;
using UnityEngine;


public class AssetsManager : MonoBehaviour
{
    public event System.Action OnAssetsChanged;
    public static AssetsManager Instance;
    public float usd = 0;
    public int eth = 0;
    public float t_usd = 0;

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
        usd = 0;
        eth = 0;
        OnAssetsChanged?.Invoke();
    }
    public void AddUSD(float amount)
    {
        usd += amount;
        OnAssetsChanged?.Invoke();
    }

    public void AddTempUSD(float amount)
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

    public void SpendUSD(float amount)
    {
        usd -= amount;
        OnAssetsChanged?.Invoke();
    }
    public float getUSD()
    {
        return usd;
    }
}
