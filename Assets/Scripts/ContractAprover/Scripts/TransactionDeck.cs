using System.Collections.Generic;
using UnityEngine;

// Leer los 3 JSON de dificultad y repartir transacciones al azar según los pesos del config, sin repetir.
public class TransactionDeck
{
    private readonly List<TransactionData>[] originals = new List<TransactionData>[3];
    private readonly List<TransactionData>[] remaining = new List<TransactionData>[3];

    public TransactionDeck(TextAsset easy, TextAsset normal, TextAsset hard)
    {
        originals[0] = Load(easy, "Fácil");
        originals[1] = Load(normal, "Normal");
        originals[2] = Load(hard, "Difícil");
        for (int i = 0; i < 3; i++)
        {
            foreach (var tx in originals[i]) tx.difficulty = i;
            remaining[i] = new List<TransactionData>(originals[i]);
        }
    }

    private static List<TransactionData> Load(TextAsset file, string name)
    {
        if (file == null)
        {
            Debug.LogWarning($"[ContractApprover] No hay JSON asignado para la dificultad {name}.");
            return new List<TransactionData>();
        }
        TransactionFile parsed = JsonUtility.FromJson<TransactionFile>(file.text);
        if (parsed == null || parsed.transactions == null)
        {
            Debug.LogError($"[ContractApprover] El JSON '{file.name}' está vacío o mal formateado.");
            return new List<TransactionData>();
        }
        return parsed.transactions;
    }

    public List<TransactionData> BuildSession(int count, ContractApproverConfig cfg)
    {
        float[] weights = { cfg.easyWeight, cfg.normalWeight, cfg.hardWeight };
        var result = new List<TransactionData>();
        for (int n = 0; n < count; n++)
        {
            TransactionData tx = Draw(weights);
            if (tx == null) break;
            result.Add(tx);
        }
        return result;
    }

    private TransactionData Draw(float[] weights)
    {
        if (AllEmpty(remaining))
        {
            if (AllEmpty(originals)) return null;
            Debug.LogWarning("[ContractApprover] Se han usado todas las transacciones: se rellenan los mazos (habrá repeticiones). Añade más al JSON o baja transactionsPerSession.");
            for (int i = 0; i < 3; i++) remaining[i] = new List<TransactionData>(originals[i]);
        }

        // Suma de pesos solo de los mazos que aún tienen cartas
        float total = 0f;
        for (int i = 0; i < 3; i++)
            if (remaining[i].Count > 0) total += Mathf.Max(0f, weights[i]);

        int pool = -1;
        if (total > 0f)
        {
            float roll = Random.value * total;
            for (int i = 0; i < 3; i++)
            {
                if (remaining[i].Count == 0) continue;
                roll -= Mathf.Max(0f, weights[i]);
                if (roll <= 0f) { pool = i; break; }
            }
        }
        if (pool == -1) // pesos a 0 o redondeo: coge el último mazo con cartas
            for (int i = 2; i >= 0; i--) if (remaining[i].Count > 0) { pool = i; break; }

        int idx = Random.Range(0, remaining[pool].Count);
        TransactionData tx = remaining[pool][idx];
        remaining[pool].RemoveAt(idx);   // descartada: no se repite
        return tx;
    }

    private static bool AllEmpty(List<TransactionData>[] lists)
    {
        foreach (var l in lists) if (l != null && l.Count > 0) return false;
        return true;
    }
}
