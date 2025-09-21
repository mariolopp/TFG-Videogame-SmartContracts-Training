using TMPro;
using UnityEngine;

public class AssetsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI usdText;
    //[SerializeField] private TextMeshProUGUI ethText;
    private AssetsManager assets;

    void Start()
    {
        assets = FindObjectOfType<AssetsManager>();
        assets.OnAssetsChanged += ActualizarUI;
        ActualizarUI(); // inicializar
    }

    private void OnDestroy()
    {
        assets.OnAssetsChanged -= ActualizarUI;
    }

    private void ActualizarUI()
    {
        Debug.Log("Actualizando UI: " + assets.usd + " USD");
        usdText.text = assets.usd + " USD";
        //ethText.text = assets.eth + " ETH";
    }
}
