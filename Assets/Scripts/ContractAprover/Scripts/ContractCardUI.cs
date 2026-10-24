using System;
using System.Collections;
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

    // Pose de los sellos en el editor (a la que llegan al terminar de caer)
    private Vector3 approvedScale, rejectedScale;
    private Quaternion approvedRotation, rejectedRotation;

    private void Awake()
    {
        if (stampApproved != null) { approvedScale = stampApproved.transform.localScale; approvedRotation = stampApproved.transform.localRotation; }
        if (stampRejected != null) { rejectedScale = stampRejected.transform.localScale; rejectedRotation = stampRejected.transform.localRotation; }

        if (coverButton != null)
        {
            coverButton.onClick.AddListener(Reveal);
            coverButton.onClick.AddListener(() => ContractApproverManager.PlaySfx("ca_contract_open"));
        }
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
                tokenSentRow.Show(L("Token enviado", "Token sent"), Token(s.tokenSent));
                amountSentRow.Show(L("Cantidad", "Amount"), $"{Amount(s.amountSent)} {sentSym}");
                destinationRow.Show(L("Destino", "Destination"), s.destination);
                break;

            case TxType.Swap:
                typeRow.Show("SWAP","[ERC-20]");
                tokenSentRow.Show(L("Envías", "You send"), Token(s.tokenSent));
                amountSentRow.Show(L("Cantidad enviada", "Amount sent"), $"{Amount(s.amountSent)} {sentSym}");
                tokenReceivedRow.Show(L("Recibes", "You receive"), Token(s.tokenReceived));
                amountReceivedRow.Show(L("Cantidad a recibir", "Amount to receive"), $"{Amount(s.amountReceived)} {s.tokenReceived?.symbol}");
                contractRow.Show(L("Contrato (router)", "Contract (router)"), s.contract);
                slippageRow.Show(L("Slippage máx.", "Max slippage"), $"{s.slippage.ToString("0.##", Inv)} %");
                break;

            case TxType.Approve:
                typeRow.Show("APPROVE","[ERC-20]");
                tokenSentRow.Show(L("Token a autorizar", "Token to approve"), Token(s.tokenSent));
                amountSentRow.Show(L("Cantidad autorizada", "Approved amount"), s.approveUnlimited ? L("ILIMITADA", "UNLIMITED") : $"{Amount(s.approveAmount)} {sentSym}");
                contractRow.Show(L("Contrato autorizado", "Approved contract"), s.contract);
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

    public void ShowStamp(bool approved, bool timeout)
    {
        if (stampApproved != null) stampApproved.SetActive(approved && !timeout);
        if (stampRejected != null) stampRejected.SetActive(!approved && !timeout);
    }

    // Tinte y texto de resultado
    public void ShowVerdict(bool correct, bool timeout, ContractApproverConfig cfg)
    {
        Color c = correct ? cfg.correctColor : cfg.wrongColor;
        if (resultOverlay != null)
        {
            resultOverlay.gameObject.SetActive(true);
            resultOverlay.color = new Color(c.r, c.g, c.b, overlayAlpha);
        }
        if (resultLabel != null)
        {
            resultLabel.gameObject.SetActive(true);
            resultLabel.text = timeout ? L("¡TIEMPO!", "TIME'S UP!") : (correct ? L("¡CORRECTO!", "CORRECT!") : L("¡ERROR!", "WRONG!"));
            resultLabel.color = c;
        }
    }

    /// Anima el sello activo: cae grande y girado, golpea la carta (onImpact) y se asienta con un pequeño rebote
    public IEnumerator PlayStamp(ContractApproverConfig cfg, Action onImpact)
    {
        GameObject stamp = null;
        Vector3 endScale = Vector3.one;
        Quaternion baseRot = Quaternion.identity;
        if (stampApproved != null && stampApproved.activeSelf) { stamp = stampApproved; endScale = approvedScale; baseRot = approvedRotation; }
        else if (stampRejected != null && stampRejected.activeSelf) { stamp = stampRejected; endScale = rejectedScale; baseRot = rejectedRotation; }
        if (stamp == null) { onImpact?.Invoke(); yield break; }

        Transform tr = stamp.transform;
        CanvasGroup cg = stamp.GetComponent<CanvasGroup>();
        if (cg == null) cg = stamp.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;

        Quaternion endRot = baseRot * Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-cfg.stampRandomTilt, cfg.stampRandomTilt));
        Quaternion startRot = endRot * Quaternion.Euler(0f, 0f, cfg.stampStartRotation);
        Vector3 startScale = endScale * cfg.stampStartScale;
        Vector3 squashScale = endScale * 0.88f;

        // Caída: acelera hasta aplastarse un poco contra la carta
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.01f, cfg.stampDropDuration));
            float e = t * t * t;                  // ease-in
            tr.localScale = Vector3.LerpUnclamped(startScale, squashScale, e);
            tr.localRotation = Quaternion.Slerp(startRot, endRot, e);
            cg.alpha = Mathf.Clamp01(t * 2.5f);
            yield return null;
        }

        onImpact?.Invoke();

        // Rebote: del aplastamiento a su tamaño, pasándose un poco
        t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.01f, cfg.stampSettleDuration));
            float e = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f);  // ease-out back
            tr.localScale = Vector3.LerpUnclamped(squashScale, endScale, e);
            yield return null;
        }
        tr.localScale = endScale;
        tr.localRotation = endRot;
        cg.alpha = 1f;
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

    private static string L(string es, string en) => LanguageManager.Pick(es, en);
}
