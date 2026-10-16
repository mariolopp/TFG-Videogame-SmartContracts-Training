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
        // Sin AssetsManager (p. ej. al abrir la escena directamente sin pasar por el menú)
        if (assets == null)
        {
            Debug.LogWarning("AssetsUI: no hay AssetsManager en escena.", this);
            return;
        }
        assets.OnAssetsChanged += ActualizarUI;
        ActualizarUI(); // inicializar
    }

    private void OnDestroy()
    {
        if (assets != null) assets.OnAssetsChanged -= ActualizarUI;
    }

    private void ActualizarUI()
    {
        Debug.Log("Actualizando UI: " + assets.usd + " USD");
        usdText.text = assets.usd + "";
        //ethText.text = assets.eth + " ETH";
    }
}
