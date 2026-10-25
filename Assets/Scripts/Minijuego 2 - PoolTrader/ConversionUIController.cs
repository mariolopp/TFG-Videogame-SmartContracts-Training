using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConversionUIController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    // Cada botón tiene un texto para las bolsas y otro para la moneda, así no se desplazan al cambiar los precios
    [SerializeField] private Button BTCToBagsButton;
    [SerializeField] private TextMeshProUGUI BTCToBagsCoinText;
    [SerializeField] private TextMeshProUGUI BTCToBagsBagsText;
    [SerializeField] private Button ETHToBagsButton;
    [SerializeField] private TextMeshProUGUI ETHToBagsCoinText;
    [SerializeField] private TextMeshProUGUI ETHToBagsBagsText;
    [SerializeField] private Button bagsToBTCButton;
    [SerializeField] private TextMeshProUGUI bagsToBTCBagsText;
    [SerializeField] private TextMeshProUGUI bagsToBTCCoinText;
    [SerializeField] private Button bagsToETHButton;
    [SerializeField] private TextMeshProUGUI bagsToETHBagsText;
    [SerializeField] private TextMeshProUGUI bagsToETHCoinText;
    [SerializeField] private TMP_Text bagsRemainingText; // Bolsas que quedan por gastar este turno (x/max)

    [SerializeField] private AssetsManager assets;
    private PriceManager priceManager;
    private PoolTraderConfig config;
    private float bagsSpentThisTurn;

    public void Open(PriceManager pm, PoolTraderConfig cfg)
    {
        if (assets == null)
        {
            assets = FindObjectOfType<AssetsManager>();
        }
        priceManager = pm;
        config = cfg;
        bagsSpentThisTurn = 0;

        if (panel != null) panel.SetActive(true);

        bagsToBTCButton.onClick.RemoveAllListeners();
        bagsToBTCBagsText.text = $"x{config.bagsPerBTC}";
        bagsToBTCCoinText.text = "x1";
        bagsToBTCButton.onClick.AddListener(ConvertBagsToBTC);
        bagsToBTCButton.onClick.AddListener(PlayButtonSound);

        bagsToETHButton.onClick.RemoveAllListeners();
        bagsToETHBagsText.text = $"x{config.bagsPerETH}";
        bagsToETHCoinText.text = "x1";
        bagsToETHButton.onClick.AddListener(ConvertBagsToETH);
        bagsToETHButton.onClick.AddListener(PlayButtonSound);

        BTCToBagsButton.onClick.RemoveAllListeners();
        BTCToBagsCoinText.text = "x1";
        BTCToBagsBagsText.text = $"x{config.bagsPerBTCSell}";
        BTCToBagsButton.onClick.AddListener(ConvertBTCToBags);
        BTCToBagsButton.onClick.AddListener(PlayButtonSound);

        ETHToBagsButton.onClick.RemoveAllListeners();
        ETHToBagsCoinText.text = "x1";
        ETHToBagsBagsText.text = $"x{config.bagsPerETHSell}";
        ETHToBagsButton.onClick.AddListener(ConvertETHToBags);
        ETHToBagsButton.onClick.AddListener(PlayButtonSound);

        assets.OnAssetsChanged += RefreshUI;
        RefreshUI();
    }

    public void Close()
    {
        if (assets != null) assets.OnAssetsChanged -= RefreshUI;
        if (panel != null) panel.SetActive(false);
    }

    // Compra 1 BTC a cambio del nº de bolsas establecido
    private void ConvertBagsToBTC()
    {
        float bags = config.bagsPerBTC;
        if (!CanBuy(bags)) return;
        bagsSpentThisTurn += bags;
        assets.SpendUSD(bags);
        priceManager.AddUserBTC(bags / config.bagsPerBTC);
    }

    // Compra 1 ETH a cambio del nº de bolsas establecido
    private void ConvertBagsToETH()
    {
        float bags = config.bagsPerETH;
        if (!CanBuy(bags)) return;
        bagsSpentThisTurn += bags;
        assets.SpendUSD(bags);
        priceManager.AddUserETH(bags / config.bagsPerETH);
    }

    // Comprueba saldo y el límite de bolsas gastadas en compras durante este turno
    private bool CanBuy(float bags)
    {
        return bags > 0
            && bags <= assets.usd
            && bagsSpentThisTurn + bags <= config.maxBagsPerConversionTurn;
    }

    private void ConvertBTCToBags()
    {
        float userBTC = priceManager.GetUserA();
        if (userBTC <= 0f) return;
        priceManager.AddUserBTC(-1f);
        assets.AddUSD(config.bagsPerBTCSell);
    }

    private void ConvertETHToBags()
    {
        float userETH = priceManager.GetUserB();
        if (userETH <= 0f) return;
        priceManager.AddUserETH(-1f);
        assets.AddUSD(config.bagsPerETHSell);
    }

    private int ParseBags(TMP_InputField field)
    {
        if (field == null || string.IsNullOrEmpty(field.text)) return 0;
        int.TryParse(field.text, out int value);
        return value;
    }

    private void RefreshUI()
    {
        //if (bagsBalanceText != null) bagsBalanceText.text = assets.GetUSD().ToString("F2");
        if (bagsRemainingText != null)
        {
            float remaining = config.maxBagsPerConversionTurn - bagsSpentThisTurn;
            bagsRemainingText.text = $"{remaining}/{config.maxBagsPerConversionTurn}";
        }
        bagsToBTCButton.interactable = CanBuy(config.bagsPerBTC);
        bagsToETHButton.interactable = CanBuy(config.bagsPerETH);
    }

    private static void PlayButtonSound()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("select_button");
    }
}
