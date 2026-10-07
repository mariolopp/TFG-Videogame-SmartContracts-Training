using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConversionUIController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button BTCToBagsButton;
    [SerializeField] private TextMeshProUGUI BTCToBagsText;
    [SerializeField] private Button ETHToBagsButton;
    [SerializeField] private TextMeshProUGUI ETHToBagsText;
    [SerializeField] private Button bagsToBTCButton;
    [SerializeField] private TextMeshProUGUI bagsToBTCText;
    [SerializeField] private Button bagsToETHButton;
    [SerializeField] private TextMeshProUGUI bagsToETHText;
    [SerializeField] private TMP_Text bagsRemainingText; // Bolsas que quedan por gastar este turno (x/max)

    [SerializeField] private AssetsManager assets;
    private PriceManager priceManager;
    private PoolTraderConfig config;
    private int bagsSpentThisTurn;

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
        bagsToBTCText.text = $"x{config.bagsPerBTC}              x1";
        bagsToBTCButton.onClick.AddListener(ConvertBagsToBTC);

        bagsToETHButton.onClick.RemoveAllListeners();
        bagsToETHText.text = $"x{config.bagsPerETH}              x1";
        bagsToETHButton.onClick.AddListener(ConvertBagsToETH);

        BTCToBagsButton.onClick.RemoveAllListeners();
        BTCToBagsText.text = $"x1              x{config.bagsPerBTC-config.factorBags}";
        BTCToBagsButton.onClick.AddListener(ConvertBTCToBags);

        ETHToBagsButton.onClick.RemoveAllListeners();
        ETHToBagsText.text = $"x1              x{config.bagsPerETH-config.factorBags}";
        ETHToBagsButton.onClick.AddListener(ConvertETHToBags);

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
        int bags = config.bagsPerBTC;
        if (!CanBuy(bags)) return;
        bagsSpentThisTurn += bags;
        assets.SpendUSD(bags);
        priceManager.AddUserBTC(bags / config.bagsPerBTC);
    }

    // Compra 1 ETH a cambio del nº de bolsas establecido
    private void ConvertBagsToETH()
    {
        int bags = config.bagsPerETH;
        if (!CanBuy(bags)) return;
        bagsSpentThisTurn += bags;
        assets.SpendUSD(bags);
        priceManager.AddUserETH(bags / config.bagsPerETH);
    }

    // Comprueba saldo y el límite de bolsas gastadas en compras durante este turno
    private bool CanBuy(int bags)
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
        assets.AddUSD(config.bagsPerBTC-config.factorBags);
    }

    private void ConvertETHToBags()
    {
        float userETH = priceManager.GetUserB();
        if (userETH <= 0f) return;
        priceManager.AddUserETH(-1f);
        assets.AddUSD(config.bagsPerETH-config.factorBags);
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
            int remaining = config.maxBagsPerConversionTurn - bagsSpentThisTurn;
            bagsRemainingText.text = $"{remaining}/{config.maxBagsPerConversionTurn}";
        }
        bagsToBTCButton.interactable = CanBuy(config.bagsPerBTC);
        bagsToETHButton.interactable = CanBuy(config.bagsPerETH);
    }
}