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
            issues.Add(new Issue("excessive_fee", L($"La fee ({F(s.gasFeeGwei)} gwei) supera el máximo para '{s.type}' ({F(maxFee)} gwei).", $"The fee ({F(s.gasFeeGwei)} gwei) exceeds the maximum for '{s.type}' ({F(maxFee)} gwei).")));

        switch (tx.Type)
        {
            case TxType.Send: ValidateSend(s, r, refs, issues); break;
            case TxType.Swap: ValidateSwap(s, r, refs, issues); break;
            case TxType.Approve: ValidateApprove(s, r, refs, issues); break;
            default: issues.Add(new Issue("unknown_type", L($"Tipo de transacción desconocido: '{s.type}'.", $"Unknown transaction type: '{s.type}'."))); break;
        }
        return issues;
    }

    private static void ValidateSend(TxSignature s, TxRequest r, ReferenceDataManager refs, List<Issue> issues)
    {
        if (refs.IsScamAddress(s.destination))
            issues.Add(new Issue("scam_destination", L($"El destino {s.destination} está en la lista negra.", $"The destination {s.destination} is blacklisted.")));
        if (Has(r.destination) && !Same(s.destination, r.destination))
            issues.Add(new Issue("wrong_destination", L($"El destino firmado ({s.destination}) no es el de la petición ({r.destination}).", $"The signed destination ({s.destination}) is not the requested one ({r.destination}).")));
        if (r.amountSent > 0f && !Approx(s.amountSent, r.amountSent))
            issues.Add(new Issue("wrong_amount", L($"Se envían {F(s.amountSent)} en lugar de {F(r.amountSent)}.", $"It sends {F(s.amountSent)} instead of {F(r.amountSent)}.")));
        if (Has(r.tokenSent) && !SameSymbol(s.tokenSent?.symbol, r.tokenSent))
            issues.Add(new Issue("wrong_token", L($"Se envía {s.tokenSent?.symbol} en lugar de {r.tokenSent}.", $"It sends {s.tokenSent?.symbol} instead of {r.tokenSent}.")));
        if (Has(r.tokenSentAddress) && !Same(s.tokenSent?.address, r.tokenSentAddress))
            issues.Add(new Issue("wrong_token_address", L($"La dirección del token ({s.tokenSent?.address}) no coincide con la de la petición ({r.tokenSentAddress}).", $"The token address ({s.tokenSent?.address}) does not match the requested one ({r.tokenSentAddress}).")));
        if (refs.IsScamToken(s.tokenSent?.address))
            issues.Add(new Issue("scam_token", L($"El token {s.tokenSent?.address} está en la lista de tokens scam.", $"The token {s.tokenSent?.address} is on the scam tokens list.")));
    }

    private static void ValidateSwap(TxSignature s, TxRequest r, ReferenceDataManager refs, List<Issue> issues)
    {
        if (!SameSymbol(s.tokenSent?.symbol, "ETH"))
            issues.Add(new Issue("wrong_token", L("En los swaps el token de origen siempre debe ser ETH.", "In swaps the source token must always be ETH.")));
        if (r.amountSent > 0f && !Approx(s.amountSent, r.amountSent))
            issues.Add(new Issue("wrong_swap_amounts", L($"Se envían {F(s.amountSent)} ETH en lugar de {F(r.amountSent)}.", $"It sends {F(s.amountSent)} ETH instead of {F(r.amountSent)}.")));
        if (r.amountReceived > 0f && !Approx(s.amountReceived, r.amountReceived))
            issues.Add(new Issue("wrong_swap_amounts", L($"Se reciben {F(s.amountReceived)} en lugar de {F(r.amountReceived)}.", $"It receives {F(s.amountReceived)} instead of {F(r.amountReceived)}.")));
        if (Has(r.tokenReceived) && !SameSymbol(s.tokenReceived?.symbol, r.tokenReceived))
            issues.Add(new Issue("wrong_received_token", L($"Se recibe {s.tokenReceived?.symbol} en lugar de {r.tokenReceived}.", $"It receives {s.tokenReceived?.symbol} instead of {r.tokenReceived}.")));
        if (Has(r.tokenReceivedAddress) && !Same(s.tokenReceived?.address, r.tokenReceivedAddress))
            issues.Add(new Issue("wrong_token_address", L($"La dirección del token recibido ({s.tokenReceived?.address}) no es la de la petición ({r.tokenReceivedAddress}).", $"The received token address ({s.tokenReceived?.address}) is not the requested one ({r.tokenReceivedAddress}).")));
        if (refs.IsScamToken(s.tokenReceived?.address))
            issues.Add(new Issue("scam_token", L($"El token recibido {s.tokenReceived?.address} está en la lista de tokens scam.", $"The received token {s.tokenReceived?.address} is on the scam tokens list.")));
        if (refs.IsScamAddress(s.contract))
            issues.Add(new Issue("scam_contract", L($"El contrato {s.contract} está en la lista negra.", $"The contract {s.contract} is blacklisted.")));
        if (Has(r.contract) && !Same(s.contract, r.contract))
            issues.Add(new Issue("wrong_contract", L($"El contrato firmado ({s.contract}) no es el de la petición ({r.contract}).", $"The signed contract ({s.contract}) is not the requested one ({r.contract}).")));
        float maxSlip = refs.GetMaxSlippage(s.tokenReceived?.symbol);
        if (s.slippage > maxSlip + EPS)
            issues.Add(new Issue("excessive_slippage", L($"Slippage de {F(s.slippage)}% por encima del máximo para {s.tokenReceived?.symbol} ({F(maxSlip)}%).", $"Slippage of {F(s.slippage)}% above the maximum for {s.tokenReceived?.symbol} ({F(maxSlip)}%).")));
    }

    private static void ValidateApprove(TxSignature s, TxRequest r, ReferenceDataManager refs, List<Issue> issues)
    {
        if (refs.IsScamAddress(s.contract))
            issues.Add(new Issue("scam_contract", L($"El contrato {s.contract} está en la lista negra.", $"The contract {s.contract} is blacklisted.")));
        if (Has(r.contract) && !Same(s.contract, r.contract))
            issues.Add(new Issue("wrong_contract", L($"El contrato autorizado ({s.contract}) no es el de la petición ({r.contract}).", $"The approved contract ({s.contract}) is not the requested one ({r.contract}).")));
        if (Has(r.tokenSent) && !SameSymbol(s.tokenSent?.symbol, r.tokenSent))
            issues.Add(new Issue("wrong_approve_token", L($"Se autoriza {s.tokenSent?.symbol} en lugar de {r.tokenSent}.", $"It approves {s.tokenSent?.symbol} instead of {r.tokenSent}.")));
        if (Has(r.tokenSentAddress) && !Same(s.tokenSent?.address, r.tokenSentAddress))
            issues.Add(new Issue("wrong_approve_token", L($"La dirección del token autorizado ({s.tokenSent?.address}) no es la de la petición.", $"The approved token address ({s.tokenSent?.address}) is not the requested one.")));
        if (refs.IsScamToken(s.tokenSent?.address))
            issues.Add(new Issue("scam_token", L($"El token {s.tokenSent?.address} está en la lista de tokens scam.", $"The token {s.tokenSent?.address} is on the scam tokens list.")));
        if (s.approveUnlimited)
            issues.Add(new Issue("unnecessary_approve", L("Se pide una autorización ILIMITADA: nunca hace falta dar más de lo necesario.", "It asks for an UNLIMITED approval: you never need to give more than necessary.")));
        else if (r.approveAmount > 0f && s.approveAmount > r.approveAmount + EPS)
            issues.Add(new Issue("unnecessary_approve", L($"Se autorizan {F(s.approveAmount)} cuando solo hacen falta {F(r.approveAmount)}.", $"It approves {F(s.approveAmount)} when only {F(r.approveAmount)} is needed.")));
    }

    // ---------- utilidades ----------
    private static bool Has(string v) => !string.IsNullOrWhiteSpace(v);
    private static bool Same(string a, string b) => ReferenceDataManager.SameAddress(a, b);
    private static bool SameSymbol(string a, string b) => string.Equals(a?.Trim(), b?.Trim(), System.StringComparison.OrdinalIgnoreCase);
    private static bool Approx(float a, float b) => Mathf.Abs(a - b) <= EPS * Mathf.Max(1f, Mathf.Abs(b));
    private static string L(string es, string en) => LanguageManager.Pick(es, en);
    private static string F(float v) => v.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture);
}
