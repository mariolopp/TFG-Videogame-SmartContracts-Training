using System;
using System.Collections.Generic;
using UnityEngine;

// Guarda las listas negras ACTIVAS (las que ve el jugador en ese momento), las tablas
// de fees y slippage, y va añadiendo entradas nuevas a medida que avanza la partida.
public class ReferenceDataManager : MonoBehaviour
{
    public class ActiveEntry
    {
        public BlacklistEntry entry;
        public int addedAtTurn;
    }

    private class ScheduledEntry
    {
        public BlacklistEntry entry;
        public bool isToken;
        public int turn;
    }

    [SerializeField] private TextAsset referenceJson;
    [SerializeField] private ContractApproverConfig config;

    // Se dispara cada vez que cambian las listas o avanza el turno (para refrescar la UI)
    public event Action OnListsChanged;
    public int CurrentTurn { get; private set; }

    private readonly List<ActiveEntry> scamAddresses = new List<ActiveEntry>();
    private readonly List<ActiveEntry> scamTokens = new List<ActiveEntry>();
    private readonly List<ScheduledEntry> scheduled = new List<ScheduledEntry>();
    private ReferenceDataFile data = new ReferenceDataFile();

    public IReadOnlyList<ActiveEntry> ScamAddresses => scamAddresses;
    public IReadOnlyList<ActiveEntry> ScamTokens => scamTokens;
    public IReadOnlyList<FeeLimit> FeeLimits => data.feeLimits;
    public IReadOnlyList<SlippageLimit> SlippageLimits => data.slippageLimits;

    public void Initialize()
    {
        scamAddresses.Clear();
        scamTokens.Clear();
        scheduled.Clear();
        CurrentTurn = 0;

        data = null;
        if (referenceJson != null) data = JsonUtility.FromJson<ReferenceDataFile>(referenceJson.text);
        else Debug.LogError("[ContractApprover] ReferenceDataManager no tiene asignado reference_data.json");
        if (data == null) data = new ReferenceDataFile();
        if (data.scamAddresses == null) data.scamAddresses = new List<BlacklistEntry>();
        if (data.scamTokens == null) data.scamTokens = new List<BlacklistEntry>();
        if (data.feeLimits == null) data.feeLimits = new List<FeeLimit>();
        if (data.slippageLimits == null) data.slippageLimits = new List<SlippageLimit>();

        foreach (var e in data.scamAddresses) AddToSchedule(e, false, e.unlockAfter);
        foreach (var e in data.scamTokens) AddToSchedule(e, true, e.unlockAfter);
        ApplyDue(true);
        OnListsChanged?.Invoke();
    }

    // Programa entradas para que aparezcan cuando se llegue al turno indicado.
    public void ScheduleInjection(BlacklistInjection inj, int turn)
    {
        if (inj == null) return;
        if (inj.addresses != null) foreach (var e in inj.addresses) AddToSchedule(e, false, turn);
        if (inj.tokens != null) foreach (var e in inj.tokens) AddToSchedule(e, true, turn);
    }

    /// Añade las entradas inmediatamente (se usa en el tutorial).
    public void InjectNow(BlacklistInjection inj)
    {
        ScheduleInjection(inj, CurrentTurn);
        ApplyDue(false);
        OnListsChanged?.Invoke();
    }

    /// Llamar al empezar cada transacción con el nº de transacciones ya resueltas.
    public void AdvanceTo(int turn)
    {
        CurrentTurn = turn;
        ApplyDue(false);
        OnListsChanged?.Invoke();
    }

    private void AddToSchedule(BlacklistEntry e, bool isToken, int turn)
    {
        if (e == null || string.IsNullOrWhiteSpace(e.address)) return;
        scheduled.Add(new ScheduledEntry { entry = e, isToken = isToken, turn = turn });
    }

    private void ApplyDue(bool initial)
    {
        int now = CurrentTurn;
        List<ScheduledEntry> due = scheduled.FindAll(s => s.turn <= now);
        scheduled.RemoveAll(s => s.turn <= now);

        bool added = false;
        foreach (var s in due)
        {
            List<ActiveEntry> list = s.isToken ? scamTokens : scamAddresses;
            if (Find(list, s.entry.address) != null) continue; // no duplicar
            var active = new ActiveEntry { entry = s.entry, addedAtTurn = initial ? int.MinValue / 2 : now };
            if (initial) list.Add(active);
            else list.Insert(0, active); // las novedades arriba del todo
            added = true;
        }
        if (added && !initial && config != null) ContractApproverManager.PlaySfx(config.sfxNewBlacklistEntry);
    }

    // ---------- Consultas ----------
    public bool IsScamAddress(string address) => Find(scamAddresses, address) != null;
    public bool IsScamToken(string address) => Find(scamTokens, address) != null;

    public bool IsNew(ActiveEntry e)
    {
        int turns = config != null ? config.newTagTurns : 2;
        return CurrentTurn - e.addedAtTurn < turns;
    }

    public float GetMaxFee(string txType)
    {
        foreach (var f in data.feeLimits)
            if (string.Equals(f.type?.Trim(), txType?.Trim(), StringComparison.OrdinalIgnoreCase)) return f.maxGwei;
        return config != null ? config.defaultMaxFeeGwei : 30f;
    }

    public float GetMaxSlippage(string tokenSymbol)
    {
        foreach (var s in data.slippageLimits)
            if (string.Equals(s.token?.Trim(), tokenSymbol?.Trim(), StringComparison.OrdinalIgnoreCase)) return s.maxPercent;
        return config != null ? config.defaultMaxSlippage : 1f;
    }

    public static bool SameAddress(string a, string b)
    {
        // En EVM las direcciones no distinguen mayúsculas/minúsculas
        return string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static ActiveEntry Find(List<ActiveEntry> list, string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        foreach (var e in list) if (SameAddress(e.entry.address, address)) return e;
        return null;
    }
}
