using System.Globalization;
using UnityEngine;

// Uno por desplegable. Rellena su lista con filas cada vez que cambian los datos.
public class ReferenceListUI : MonoBehaviour
{
    public enum ListKind { ScamAddresses, ScamTokens, FeeLimits, SlippageLimits }

    [SerializeField] private ReferenceDataManager references;
    [SerializeField] private ListKind kind;
    [Tooltip("Nombre corto para abrir este panel desde los diálogos: evento 'abrirPanel:<id>'")]
    [SerializeField] private string sectionId = "addresses";

    [Header("Dónde se crean las filas")]
    [SerializeField] private Transform rowsParent;
    [SerializeField] private ReferenceRowUI rowPrefab;

    [Header("Aviso de novedades (opcional)")]
    [SerializeField] private AccordionSection section;
    [SerializeField] private GameObject headerBadge;

    public string SectionId => sectionId;
    public AccordionSection Section => section;

    private int lastCount = 0;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private void Awake()
    {
        if (references != null) references.OnListsChanged += Rebuild;
        if (headerBadge != null) headerBadge.SetActive(false);
    }

    private void OnDestroy()
    {
        if (references != null) references.OnListsChanged -= Rebuild;
    }

    private void Start() => Rebuild();

    private void Update()
    {
        // Al abrir el desplegable, el aviso se da por visto
        if (headerBadge != null && headerBadge.activeSelf && section != null && section.EstaAbierto)
            headerBadge.SetActive(false);
    }

    public void Rebuild()
    {
        if (rowsParent == null || rowPrefab == null || references == null) return;

        for (int i = rowsParent.childCount - 1; i >= 0; i--)
            Destroy(rowsParent.GetChild(i).gameObject);

        int count = 0;
        bool anyNew = false;
        switch (kind)
        {
            case ListKind.ScamAddresses:
                foreach (var a in references.ScamAddresses)
                {
                    bool isNew = references.IsNew(a);
                    anyNew |= isNew;
                    AddRow(a.entry.address, a.entry.label, isNew);
                    count++;
                }
                break;
            case ListKind.ScamTokens:
                foreach (var a in references.ScamTokens)
                {
                    bool isNew = references.IsNew(a);
                    anyNew |= isNew;
                    AddRow(a.entry.address, a.entry.label, isNew);
                    count++;
                }
                break;
            case ListKind.FeeLimits:
                foreach (var f in references.FeeLimits)
                {
                    AddRow(TypeName(f.type), $"máx. {f.maxGwei.ToString("0.##", Inv)} gwei", false);
                    count++;
                }
                break;
            case ListKind.SlippageLimits:
                foreach (var s in references.SlippageLimits)
                {
                    AddRow(s.token, $"máx. {s.maxPercent.ToString("0.##", Inv)} %", false);
                    count++;
                }
                break;
        }

        if (headerBadge != null && count > lastCount && anyNew && (section == null || !section.EstaAbierto))
            headerBadge.SetActive(true);
        lastCount = count;
    }

    private void AddRow(string main, string secondary, bool isNew)
    {
        ReferenceRowUI row = Instantiate(rowPrefab, rowsParent);
        row.gameObject.SetActive(true);
        row.Set(main, secondary, isNew);
    }

    private static string TypeName(string type)
    {
        switch ((type ?? "").Trim().ToLowerInvariant())
        {
            case "send": return "Envío";
            case "swap": return "Swap";
            case "approve": return "Approve";
            default: return type;
        }
    }
}
