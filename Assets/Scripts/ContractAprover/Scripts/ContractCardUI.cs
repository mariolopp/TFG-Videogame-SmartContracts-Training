using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Una "fila" del panel: etiqueta + valor. Se oculta entera si no aplica al tipo de transacción.
[Serializable]
public class CardInfoRow
{
    [Tooltip("Objeto padre de la fila entera (oculto si no aplica). Si se deja vacío se oculta solo el texto del valor.")]
    public GameObject root;
    [Tooltip("Texto de la etiqueta (opcional). Si se asigna, el script escribe la etiqueta según el tipo de transacción.")]
    public TMP_Text label;
    [Tooltip("Texto donde se escribe el valor")]
    public TMP_Text value;

    public void Show(string labelText, string valueText)
    {
        if (root != null) root.SetActive(true);
        else if (value != null) value.gameObject.SetActive(true);
        if (label != null && labelText != null) label.text = labelText;
        if (value != null) value.text = valueText;
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
        else if (value != null) value.gameObject.SetActive(false);
    }
}

// Va en el panel de la firma. Sabe pintarse con los datos de una transacción,
// y mostrar el sello de resultado
[RequireComponent(typeof(CanvasGroup))]
 public class ContractCardUI : MonoBehaviour
{
    [Header("Filas de datos de la firma")]
    [SerializeField] private CardInfoRow typeRow;
    [SerializeField] private CardInfoRow tokenSentRow;
    [SerializeField] private CardInfoRow amountSentRow;
    [SerializeField] private CardInfoRow tokenReceivedRow;
    [SerializeField] private CardInfoRow amountReceivedRow;
    [SerializeField] private CardInfoRow destinationRow;
    [SerializeField] private CardInfoRow contractRow;
    [SerializeField] private CardInfoRow slippageRow;
    [SerializeField] private CardInfoRow feeRow;

    [Header("Sello de resultado (todos opcionales)")]
    [SerializeField] private GameObject stampApproved;
    [SerializeField] private GameObject stampRejected;
    [SerializeField] private Image resultOverlay;
    [Range(0f, 1f)] [SerializeField] private float overlayAlpha = 0.25f;
    [SerializeField] private TMP_Text resultLabel;

    [Header("Tapa 'Pulsa para revisar la firma' (opcional)")]
    [SerializeField] private GameObject cover;
    [SerializeField] private Button coverButton;

    [Header("Color del texto de direcciones")]
    [SerializeField] private string addressColorHex = "#9AA4B2";

    private RectTransform rect;
    private CanvasGroup group;
    public RectTransform Rect => rect != null ? rect : (rect = (RectTransform)transform);
    public CanvasGroup Group => group != null ? group : (group = GetComponent<CanvasGroup>());
    public bool IsRevealed { get; private set; } = true;

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private void Awake()
    {
        if (coverButton != null) coverButton.onClick.AddListener(Reveal);
        HideResult();
    }

    public void Fill(TransactionData tx)
    {
        Clear();
        TxSignature s = tx.signature;
        string sentSym = s.tokenSent != null ? s.tokenSent.symbol : "";

        switch (tx.Type)
        {
            case TxType.Send:
                typeRow.Show("SEND","[ERC-20]");
                tokenSentRow.Show("Token enviado", Token(s.tokenSent));
                amountSentRow.Show("Cantidad", $"{Amount(s.amountSent)} {sentSym}");
                destinationRow.Show("Destino", s.destination);
                break;

            case TxType.Swap:
                typeRow.Show("SWAP","[ERC-20]");
                tokenSentRow.Show("Envías", Token(s.tokenSent));
                amountSentRow.Show("Cantidad enviada", $"{Amount(s.amountSent)} {sentSym}");
                tokenReceivedRow.Show("Recibes", Token(s.tokenReceived));
                amountReceivedRow.Show("Cantidad a recibir", $"{Amount(s.amountReceived)} {s.tokenReceived?.symbol}");
                contractRow.Show("Contrato (router)", s.contract);
                slippageRow.Show("Slippage máx.", $"{s.slippage.ToString("0.##", Inv)} %");
                break;

            case TxType.Approve:
                typeRow.Show("APPROVE","[ERC-20]");
                tokenSentRow.Show("Token a autorizar", Token(s.tokenSent));
                amountSentRow.Show("Cantidad autorizada", s.approveUnlimited ? "ILIMITADA" : $"{Amount(s.approveAmount)} {sentSym}");
                contractRow.Show("Contrato autorizado", s.contract);
                break;

            default:
                typeRow.Show("UNKNOWN?","[ERC-20]");
                break;
        }
        feeRow.Show("Gas fee", $"{s.gasFeeGwei.ToString("0.##", Inv)} gwei");
    }

    public void Clear()
    {
        typeRow.Hide(); tokenSentRow.Hide(); amountSentRow.Hide(); tokenReceivedRow.Hide();
        amountReceivedRow.Hide(); destinationRow.Hide(); contractRow.Hide(); slippageRow.Hide(); feeRow.Hide();
    }

    public void SetCovered(bool covered)
    {
        IsRevealed = !covered || cover == null;
        if (cover != null) cover.SetActive(covered);
    }

    public void Reveal()
    {
        if (IsRevealed) return;
        IsRevealed = true;
        if (cover != null) cover.SetActive(false);
    }

    public void ShowResult(bool approved, bool correct, bool timeout, ContractApproverConfig cfg)
    {
        if (stampApproved != null) stampApproved.SetActive(approved && !timeout);
        if (stampRejected != null) stampRejected.SetActive(!approved && !timeout);

        Color c = correct ? cfg.correctColor : cfg.wrongColor;
        if (resultOverlay != null)
        {
            resultOverlay.gameObject.SetActive(true);
            resultOverlay.color = new Color(c.r, c.g, c.b, overlayAlpha);
        }
        if (resultLabel != null)
        {
            resultLabel.gameObject.SetActive(true);
            resultLabel.text = timeout ? "¡TIEMPO!" : (correct ? "¡CORRECTO!" : "¡ERROR!");
            resultLabel.color = c;
        }
    }

    public void HideResult()
    {
        if (stampApproved != null) stampApproved.SetActive(false);
        if (stampRejected != null) stampRejected.SetActive(false);
        if (resultOverlay != null) resultOverlay.gameObject.SetActive(false);
        if (resultLabel != null) resultLabel.gameObject.SetActive(false);
    }

    public void SetInteractable(bool value)
    {
        Group.interactable = value;
        Group.blocksRaycasts = value;
    }

    // ---------- formato ----------
    private string Token(TokenInfo t)
    {
        if (t == null || string.IsNullOrEmpty(t.symbol)) return "-";
        return $"{t.symbol}\n<size=75%><color={addressColorHex}>{t.address}</color></size>";
    }

    private static string Amount(float v) => v.ToString("0.########", Inv);
}
