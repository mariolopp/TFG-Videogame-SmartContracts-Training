using System.Collections.Generic;
using UnityEngine;

// Aplica el checklist de validación a una transacción.
// El juego usa "shouldApprove" del JSON como verdad; esto sirve para:
//  - Avisar en la Console si un JSON está mal hecho (crossCheckJson)
//  - Generar una explicación automática si la transacción no trae "explanation"
public static class TransactionValidator
{
    public struct Issue
    {
        public string code;
        public string message;
        public Issue(string c, string m) { code = c; message = m; }
    }

    private const float EPS = 0.0001f;

    public static List<Issue> Validate(TransactionData tx, ReferenceDataManager refs)
    {
        var issues = new List<Issue>();
        TxSignature s = tx.signature ?? new TxSignature();
        TxRequest r = tx.request ?? new TxRequest();

        float maxFee = refs.GetMaxFee(s.type);
        if (s.gasFeeGwei > maxFee + EPS)
            issues.Add(new Issue("excessive_fee", $"La fee ({F(s.gasFeeGwei)} gwei) supera el máximo para '{s.type}' ({F(maxFee)} gwei)."));

        switch (tx.Type)
        {
            case TxType.Send: ValidateSend(s, r, refs, issues); break;
            case TxType.Swap: ValidateSwap(s, r, refs, issues); break;
            case TxType.Approve: ValidateApprove(s, r, refs, issues); break;
            default: issues.Add(new Issue("unknown_type", $"Tipo de transacción desconocido: '{s.type}'.")); break;
        }
        return issues;
    }

    private static void ValidateSend(TxSignature s, TxRequest r, ReferenceDataManager refs, List<Issue> issues)
    {
        if (refs.IsScamAddress(s.destination))
            issues.Add(new Issue("scam_destination", $"El destino {s.destination} está en la lista negra."));
        if (Has(r.destination) && !Same(s.destination, r.destination))
            issues.Add(new Issue("wrong_destination", $"El destino firmado ({s.destination}) no es el de la petición ({r.destination})."));
        if (r.amountSent > 0f && !Approx(s.amountSent, r.amountSent))
            issues.Add(new Issue("wrong_amount", $"Se envían {F(s.amountSent)} en lugar de {F(r.amountSent)}."));
        if (Has(r.tokenSent) && !SameSymbol(s.tokenSent?.symbol, r.tokenSent))
            issues.Add(new Issue("wrong_token", $"Se envía {s.tokenSent?.symbol} en lugar de {r.tokenSent}."));
        if (Has(r.tokenSentAddress) && !Same(s.tokenSent?.address, r.tokenSentAddress))
            issues.Add(new Issue("wrong_token_address", $"La dirección del token ({s.tokenSent?.address}) no coincide con la de la petición ({r.tokenSentAddress})."));
        if (refs.IsScamToken(s.tokenSent?.address))
            issues.Add(new Issue("scam_token", $"El token {s.tokenSent?.address} está en la lista de tokens scam."));
    }

    private static void ValidateSwap(TxSignature s, TxRequest r, ReferenceDataManager refs, List<Issue> issues)
    {
        if (!SameSymbol(s.tokenSent?.symbol, "ETH"))
            issues.Add(new Issue("wrong_token", "En los swaps el token de origen siempre debe ser ETH."));
        if (r.amountSent > 0f && !Approx(s.amountSent, r.amountSent))
            issues.Add(new Issue("wrong_swap_amounts", $"Se envían {F(s.amountSent)} ETH en lugar de {F(r.amountSent)}."));
        if (r.amountReceived > 0f && !Approx(s.amountReceived, r.amountReceived))
            issues.Add(new Issue("wrong_swap_amounts", $"Se reciben {F(s.amountReceived)} en lugar de {F(r.amountReceived)}."));
        if (Has(r.tokenReceived) && !SameSymbol(s.tokenReceived?.symbol, r.tokenReceived))
            issues.Add(new Issue("wrong_received_token", $"Se recibe {s.tokenReceived?.symbol} en lugar de {r.tokenReceived}."));
        if (Has(r.tokenReceivedAddress) && !Same(s.tokenReceived?.address, r.tokenReceivedAddress))
            issues.Add(new Issue("wrong_token_address", $"La dirección del token recibido ({s.tokenReceived?.address}) no es la de la petición ({r.tokenReceivedAddress})."));
        if (refs.IsScamToken(s.tokenReceived?.address))
            issues.Add(new Issue("scam_token", $"El token recibido {s.tokenReceived?.address} está en la lista de tokens scam."));
        if (refs.IsScamAddress(s.contract))
            issues.Add(new Issue("scam_contract", $"El contrato {s.contract} está en la lista negra."));
        if (Has(r.contract) && !Same(s.contract, r.contract))
            issues.Add(new Issue("wrong_contract", $"El contrato firmado ({s.contract}) no es el de la petición ({r.contract})."));
        float maxSlip = refs.GetMaxSlippage(s.tokenReceived?.symbol);
        if (s.slippage > maxSlip + EPS)
            issues.Add(new Issue("excessive_slippage", $"Slippage de {F(s.slippage)}% por encima del máximo para {s.tokenReceived?.symbol} ({F(maxSlip)}%)."));
    }

    private static void ValidateApprove(TxSignature s, TxRequest r, ReferenceDataManager refs, List<Issue> issues)
    {
        if (refs.IsScamAddress(s.contract))
            issues.Add(new Issue("scam_contract", $"El contrato {s.contract} está en la lista negra."));
        if (Has(r.contract) && !Same(s.contract, r.contract))
            issues.Add(new Issue("wrong_contract", $"El contrato autorizado ({s.contract}) no es el de la petición ({r.contract})."));
        if (Has(r.tokenSent) && !SameSymbol(s.tokenSent?.symbol, r.tokenSent))
            issues.Add(new Issue("wrong_approve_token", $"Se autoriza {s.tokenSent?.symbol} en lugar de {r.tokenSent}."));
        if (Has(r.tokenSentAddress) && !Same(s.tokenSent?.address, r.tokenSentAddress))
            issues.Add(new Issue("wrong_approve_token", $"La dirección del token autorizado ({s.tokenSent?.address}) no es la de la petición."));
        if (refs.IsScamToken(s.tokenSent?.address))
            issues.Add(new Issue("scam_token", $"El token {s.tokenSent?.address} está en la lista de tokens scam."));
        if (s.approveUnlimited)
            issues.Add(new Issue("unnecessary_approve", "Se pide una autorización ILIMITADA: nunca hace falta dar más de lo necesario."));
        else if (r.approveAmount > 0f && s.approveAmount > r.approveAmount + EPS)
            issues.Add(new Issue("unnecessary_approve", $"Se autorizan {F(s.approveAmount)} cuando solo hacen falta {F(r.approveAmount)}."));
    }

    // ---------- utilidades ----------
    private static bool Has(string v) => !string.IsNullOrWhiteSpace(v);
    private static bool Same(string a, string b) => ReferenceDataManager.SameAddress(a, b);
    private static bool SameSymbol(string a, string b) => string.Equals(a?.Trim(), b?.Trim(), System.StringComparison.OrdinalIgnoreCase);
    private static bool Approx(float a, float b) => Mathf.Abs(a - b) <= EPS * Mathf.Max(1f, Mathf.Abs(b));
    private static string F(float v) => v.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture);
}
