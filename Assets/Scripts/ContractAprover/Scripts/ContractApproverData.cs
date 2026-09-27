using System;
using System.Collections.Generic;

// =====================================================================================
//  ContractApproverData.cs
//  "Moldes" que JsonUtility usa para convertir los JSON del minijuego en objetos C#.
//  NO son componentes: no se añaden a ningún GameObject. Solo tienen que existir.
//  IMPORTANTE: los nombres de las variables deben coincidir EXACTAMENTE con las claves
//  del JSON (mayúsculas incluidas), o JsonUtility las dejará vacías sin avisar.
// =====================================================================================

public enum TxType { Send, Swap, Approve, Unknown }

[Serializable]
public class TokenInfo
{
    public string symbol;   // "XXXX"
    public string address;  // "0xA0b86991c6218b36"  ("native" si es ETH)
}

// Lo que el usuario PIDE en su mensaje, en versión estructurada.
// El jugador NO lo ve así (ve requestText). Sirve para que el validador automático
// compruebe que el JSON está bien construido. Deja vacío/0 lo que la petición no diga.
[Serializable]
public class TxRequest
{
    public string tokenSent;
    public string tokenSentAddress;
    public float amountSent;
    public string tokenReceived;
    public string tokenReceivedAddress;
    public float amountReceived;
    public string destination;
    public string contract;
    public float approveAmount;
}

// Lo que se va a firmar realmente
[Serializable]
public class TxSignature
{
    public string type;                                   // "send" | "swap" | "approve"
    public TokenInfo tokenSent = new TokenInfo();         // send: token enviado | swap: siempre ETH | approve: token autorizado
    public float amountSent;                              // send/swap
    public TokenInfo tokenReceived = new TokenInfo();     // solo swap
    public float amountReceived;                          // solo swap
    public string destination;                            // solo send
    public string contract;                               // swap: router | approve: contrato autorizado (spender)
    public float slippage;                                // solo swap, en %
    public float gasFeeGwei;                              // todas
    public float approveAmount;                           // solo approve
    public bool approveUnlimited;                         // solo approve (true = cantidad ilimitada)
}

[Serializable]
public class BlacklistEntry
{
    public string address;      // Direccion scam (0x...) o token scam (0x... o "native")
    public string label;        // Descripción corta de porque es scam ("Drainer FakeAirdrop")
    public int unlockAfter;     // Solo en reference_data.json: nº de transacciones resueltas para que aparezca
}

[Serializable]
public class BlacklistInjection
{
    public List<BlacklistEntry> addresses = new List<BlacklistEntry>();
    public List<BlacklistEntry> tokens = new List<BlacklistEntry>();
}

[Serializable]
public class TransactionData
{
    public string id;
    public string requestText;                          // Texto que aparece en el hueco de "Petición"
    public TxRequest request = new TxRequest();
    public TxSignature signature = new TxSignature();
    public bool shouldApprove;                           // Idicador de si aprobarla es correcto o no
    public string errorCode;                             // "none", "scam_destination", ... (informativo)
    public string explanation;                           // Se muestra al jugador tras decidir
    public BlacklistInjection requiredBlacklist = new BlacklistInjection(); // Entradas que DEBEN estar en las listas para que esta transacción sea justa

    public TxType Type
    {
        get
        {
            string t = signature != null && signature.type != null ? signature.type.Trim().ToLowerInvariant() : "";
            switch (t)
            {
                case "send": return TxType.Send;
                case "swap": return TxType.Swap;
                case "approve": return TxType.Approve;
                default: return TxType.Unknown;
            }
        }
    }
}

[Serializable]
public class TransactionFile
{
    public List<TransactionData> transactions = new List<TransactionData>();
}

[Serializable]
public class FeeLimit
{
    public string type;      // "send" | "swap" | "approve"
    public float maxGwei;
}

[Serializable]
public class SlippageLimit
{
    public string token;     // "USDC"
    public float maxPercent;
}

[Serializable]
public class ReferenceDataFile
{
    public List<BlacklistEntry> scamAddresses = new List<BlacklistEntry>();
    public List<BlacklistEntry> scamTokens = new List<BlacklistEntry>();
    public List<FeeLimit> feeLimits = new List<FeeLimit>();
    public List<SlippageLimit> slippageLimits = new List<SlippageLimit>();
}

// ---------- Tutorial ----------
// Reutiliza DialogLine (de tu DialogManager.cs) para que los diálogos tengan el mismo formato.
[Serializable]
public class TutorialStep
{
    public string kind;                        // "dialogue" | "transaction"
    public DialogLine[] dialogues;             // dialogue: lo que se dice | transaction: se dice DESPUÉS de mostrar el panel y ANTES de decidir
    public TransactionData transaction;        // solo si kind = "transaction"
    public DialogLine[] dialoguesIfCorrect;    // solo transaction: tras acertar
    public DialogLine[] dialoguesIfWrong;      // solo transaction: tras fallar
}

[Serializable]
public class TutorialFile
{
    public List<TutorialStep> steps = new List<TutorialStep>();
}
