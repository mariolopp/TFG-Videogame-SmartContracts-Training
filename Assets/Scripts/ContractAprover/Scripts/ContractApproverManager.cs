using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ContractApproverManager : MonoBehaviour
{
    private enum Decision { None, Approve, Reject, Timeout, Skipped }

    [Header("Configuración")]
    [SerializeField] private ContractApproverConfig config;

    [Header("Archivos JSON de transacciones")]
    [SerializeField] private TextAsset tutorialJson;
    [SerializeField] private TextAsset easyJson;
    [SerializeField] private TextAsset normalJson;
    [SerializeField] private TextAsset hardJson;

    [Header("Sistemas")]
    [SerializeField] private ReferenceDataManager references;
    [SerializeField] private ContractScoreManager score;
    [SerializeField] private ContractCardStack cards;
    [SerializeField] private ContractHUD hud;
    [SerializeField] private DialogManager dialogManager;
    [SerializeField] private ReferenceListUI[] referenceLists;

    [Header("Botones")]
    [SerializeField] private Button approveButton;
    [SerializeField] private Button rejectButton;
    [SerializeField] private Button skipTutorialButton;

    [Header("Tutorial")]
    [Tooltip("Objetos que solo deben verse durante el tutorial (personaje, 'pulse cualquier tecla'...)")]
    [SerializeField] private GameObject[] tutorialOnlyObjects;
    [Tooltip("Fondo oscuro del diálogo. Se recorta por la derecha cuando el profe abre un desplegable")]
    [SerializeField] private RectTransform tutorialOverlay;
    [SerializeField] private float overlayCutRight = 700f;
    [SerializeField] private float overlayCutDuration = 0.25f;

    [Header("Opciones")]
    [SerializeField] private bool autoStart = true;
    [Tooltip("Flecha derecha = aprobar, flecha izquierda = rechazar, espacio = abrir panel")]
    [SerializeField] private bool keyboardShortcuts = true;

    public event Action<int, bool> OnSessionEnded; // Fin de partida

    private Decision decision = Decision.None;
    private bool acceptingInput;
    private bool inTutorial;
    private bool skipRequested;
    private bool dialogRunning;
    private bool lastWasCorrect;
    private int answered;
    private int correctAnswers;
    private Vector2 overlayBaseSize, overlayBasePos;
    private Coroutine overlayCo;

    // ------------------------------- ciclo de vida ----------------------------------- 
    private void Awake()
    {
        if (tutorialOverlay != null)
        {
            overlayBaseSize = tutorialOverlay.sizeDelta;
            overlayBasePos = tutorialOverlay.anchoredPosition;
        }
        if (approveButton != null) approveButton.onClick.AddListener(Approve);
        if (rejectButton != null) rejectButton.onClick.AddListener(Reject);
        if (skipTutorialButton != null)
        {
            skipTutorialButton.onClick.AddListener(SkipTutorial);
            skipTutorialButton.gameObject.SetActive(false);
        }
        if (score != null && hud != null)
        {
            score.OnScoreChanged += hud.SetScore;
            score.OnLivesChanged += hud.SetLives;
        }
    }

    private void OnEnable() { DialogManager.OnDialogEvent += HandleDialogEvent; }
    private void OnDisable() { DialogManager.OnDialogEvent -= HandleDialogEvent; }

    private void Start()
    {
        Time.timeScale = 1f;
        if (autoStart) StartGame();
    }

    private void Update()
    {
        if (!keyboardShortcuts || !acceptingInput) return;
        if (Input.GetKeyDown(KeyCode.Space) && cards.Front != null) cards.Front.Reveal();
        else if (Input.GetKeyDown(KeyCode.RightArrow)) Approve();
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) Reject();
    }

    // ---------------------------- API pública -------------------------------------- 
    public void StartGame()
    {
        StopAllCoroutines();
        StartCoroutine(GameRoutine());
    }

    public void Approve() => Decide(Decision.Approve);
    public void Reject() => Decide(Decision.Reject);

    public void SkipTutorial()
    {
        if (!inTutorial) return;
        skipRequested = true;
        DialogManager dm = dialogManager != null ? dialogManager : DialogManager.Instance;
        if (dialogRunning && dm != null) dm.StopDialog();
        if (acceptingInput) { decision = Decision.Skipped; acceptingInput = false; }
    }


    public void RestartScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    public static void PlaySfx(string id)
    {
        if (string.IsNullOrEmpty(id) || AudioManager.Instance == null) return;
        AudioManager.Instance.PlaySFX(id);
    }

    // ----------------------------- flujo principal ------------------------------------- 
    private IEnumerator GameRoutine()
    {
        yield return null; // espera 1 frame para que DialogManager y el resto hagan su Start()

        SetButtonsInteractable(false);
        hud.SetTimerVisible(false);
        references.Initialize();
        score.ResetSession(config);
        answered = 0;
        correctAnswers = 0;

        if (config.playTutorial && tutorialJson != null)
            yield return TutorialRoutine();

        var deck = new TransactionDeck(easyJson, normalJson, hardJson);
        List<TransactionData> session = deck.BuildSession(config.transactionsPerSession, config);
        if (session.Count == 0)
        {
            Debug.LogError("[ContractApprover] No hay transacciones. Revisa que los JSON estén asignados y bien formados.");
            yield break;
        }

        // Programa las entradas de lista negra que necesita cada fraude, unos turnos antes de que salga, para no ser obvio
        for (int i = 0; i < session.Count; i++)
        {
            int turn = Mathf.Max(0, i - config.blacklistLeadTurns);
            references.ScheduleInjection(session[i].requiredBlacklist, turn);
        }

        for (int i = 0; i < session.Count; i++)
        {
            references.AdvanceTo(i);
            hud.SetRemaining(session.Count - i, session.Count);
            float limit = Mathf.Max(config.minTime, config.startTime - config.timeDecreasePerTx * i);

            yield return PlayTransaction(session[i], limit, true, true, null);

            if (score.IsDead) break;
        }

        EndGame(!score.IsDead);
    }

    private IEnumerator PlayTransaction(TransactionData tx, float limit, bool useTimer, bool applyScore, DialogLine[] preDialogues)
    {
        decision = Decision.None;
        yield return cards.Present(tx);
        CrossCheck(tx);

        if (preDialogues != null && preDialogues.Length > 0)
            yield return PlayDialogue(preDialogues);

        if (inTutorial && skipRequested)
        {
            yield return cards.Dismiss();
            yield break;
        }

        // ---- Espera de decisión ----
        float timeLeft = limit;
        hud.SetTimerVisible(useTimer);
        if (useTimer) hud.SetTimer(timeLeft, limit);
        SetButtonsInteractable(true);
        acceptingInput = true;

        while (decision == Decision.None)
        {
            bool running = useTimer && !dialogRunning && (!config.timerStartsOnReveal || cards.Front.IsRevealed);
            if (running)
            {
                timeLeft -= Time.deltaTime;
                hud.SetTimer(Mathf.Max(0f, timeLeft), limit);
                if (timeLeft <= 0f) decision = Decision.Timeout;
            }
            yield return null;
        }

        acceptingInput = false;
        SetButtonsInteractable(false);

        if (decision == Decision.Skipped)
        {
            yield return cards.Dismiss();
            yield break;
        }

        // ---- Resolución ----
        bool approved = decision == Decision.Approve;
        bool timeout = decision == Decision.Timeout;
        bool correct = !timeout && approved == tx.shouldApprove;
        lastWasCorrect = correct;

        if (applyScore) ApplyScore(approved, timeout, correct);
        if (!inTutorial) { answered++; if (correct) correctAnswers++; }

        // En el tutorial la explicación la da el profe con dialoguesIfCorrect/IfWrong
        if (!inTutorial) hud.ShowToast(BuildFeedback(tx, approved, timeout, correct), correct);
        hud.Flash(correct);
        PlaySfx(correct ? config.sfxCorrect : config.sfxWrong);

        yield return cards.Resolve(approved, correct, timeout);
    }

    private void Decide(Decision d)
    {
        if (!acceptingInput) return;
        if (cards.Front == null || !cards.Front.IsRevealed) return; // primero hay que abrir el panel
        decision = d;
        acceptingInput = false;
    }

    private void ApplyScore(bool approved, bool timeout, bool correct)
    {
        if (correct) { score.AddPoints(config.pointsCorrect); return; }
        if (timeout) { score.LoseLives(config.livesLostOnTimeout); return; }
        if (approved) score.LoseLives(config.livesLostOnBadApprove);        // firmó un fraude
        else
        {
            score.RemovePoints(config.pointsLostOnGoodReject);              // rechazó una legítima
            score.LoseLives(config.livesLostOnGoodReject);                  // Actualmente no se pierden vidas por rechazar legítima, pero se puede cambiar en config
        }
    }

    private string BuildFeedback(TransactionData tx, bool approved, bool timeout, bool correct)
    {
        string why = !string.IsNullOrWhiteSpace(tx.explanation) ? tx.explanation : AutoExplanation(tx);
        if (timeout) return "¡Se acabó el tiempo! " + (tx.shouldApprove ? "Era una transacción legítima." : "Era fraudulenta: " + why);
        if (correct && approved) return "Firma correcta. " + why;
        if (correct) return "¡Fraude detectado! " + why;
        if (approved) return "¡Has firmado un fraude! " + why;
        return "Era legítima y la has rechazado. " + why;
    }

    private string AutoExplanation(TransactionData tx)
    {
        var issues = TransactionValidator.Validate(tx, references);
        if (issues.Count == 0) return "Todo coincidía con la petición.";
        var sb = new StringBuilder();
        foreach (var i in issues) sb.Append(i.message).Append(' ');
        return sb.ToString().Trim();
    }

    private void CrossCheck(TransactionData tx)
    {
        if (!config.crossCheckJson) return;
        var issues = TransactionValidator.Validate(tx, references);
        bool autoValid = issues.Count == 0;
        if (autoValid == tx.shouldApprove) return;

        var sb = new StringBuilder();
        foreach (var i in issues) sb.Append("\n - ").Append(i.code).Append(": ").Append(i.message);
        Debug.LogWarning($"[ContractApprover] Transacción '{tx.id}': el JSON dice shouldApprove={tx.shouldApprove.ToString().ToLower()} " +
                         $"pero el validador {(autoValid ? "no encuentra ningún problema" : "encuentra:" + sb)}");
    }

    private void EndGame(bool survived)
    {
        acceptingInput = false;
        SetButtonsInteractable(false);
        hud.ShowEnd(survived, score.Score, correctAnswers, answered);
        OnSessionEnded?.Invoke(score.Score, survived);
    }

    // ----------------------------- tutorial ------------------------------------- 
    private IEnumerator TutorialRoutine()
    {
        TutorialFile file = JsonUtility.FromJson<TutorialFile>(tutorialJson.text);
        if (file == null || file.steps == null || file.steps.Count == 0)
        {
            Debug.LogWarning("[ContractApprover] tutorial.json vacío o mal formado: se salta el tutorial.");
            yield break;
        }

        inTutorial = true;
        skipRequested = false;
        SetObjectsActive(tutorialOnlyObjects, true);
        if (skipTutorialButton != null) skipTutorialButton.gameObject.SetActive(true);

        foreach (TutorialStep step in file.steps)
        {
            if (skipRequested) break;
            string kind = (step.kind ?? "").Trim().ToLowerInvariant();

            if (kind == "dialogue")
            {
                yield return PlayDialogue(step.dialogues);
            }
            else if (kind == "transaction" && step.transaction != null)
            {
                references.InjectNow(step.transaction.requiredBlacklist);
                yield return PlayTransaction(step.transaction, config.tutorialTime, config.tutorialHasTimer,
                                             config.tutorialAffectsScore, step.dialogues);
                if (skipRequested) break;
                yield return PlayDialogue(lastWasCorrect ? step.dialoguesIfCorrect : step.dialoguesIfWrong);
            }
        }

        if (!config.tutorialAffectsScore) score.ResetSession(config);
        SetObjectsActive(tutorialOnlyObjects, false);
        if (skipTutorialButton != null) skipTutorialButton.gameObject.SetActive(false);
        CloseAllSections();
        SetOverlayCut(false);
        inTutorial = false;
    }

    private IEnumerator PlayDialogue(DialogLine[] lines)
    {
        if (lines == null || lines.Length == 0 || skipRequested) yield break;
        DialogManager dm = dialogManager != null ? dialogManager : DialogManager.Instance;
        if (dm == null)
        {
            Debug.LogWarning("[ContractApprover] No hay DialogManager en la escena: se saltan los diálogos.");
            yield break;
        }

        bool finished = false;
        dialogRunning = true;
        // Tu DialogManager lee un TextAsset: creamos uno al vuelo con estas líneas
        var asset = new TextAsset(JsonUtility.ToJson(new DialogData { dialogues = lines }));
        dm.StartDialog(asset, () => finished = true);
        while (!finished) yield return null;
        dialogRunning = false;
        Destroy(asset);
    }

    // Posibles eventos para el campo evento del json
    //   "abrirPanel:addresses" / "abrirPanel:tokens" / "abrirPanel:fees" / "abrirPanel:slippage"
    //   "cerrarPaneles"
    private void HandleDialogEvent(string ev)
    {
        if (string.IsNullOrEmpty(ev)) return;
        const string open = "abrirPanel:";
        if (ev.StartsWith(open))
        {
            OpenSection(ev.Substring(open.Length).Trim());
            SetOverlayCut(true);
        }
        else if (ev == "cerrarPaneles")
        {
            CloseAllSections();
            SetOverlayCut(false);
        }
    }

    // Recorta el overlay por la derecha dejando fijo su borde izquierdo, para iluminar los desplegables
    private void SetOverlayCut(bool cut)
    {
        if (tutorialOverlay == null) return;
        float amount = cut ? overlayCutRight : 0f;
        Vector2 size = overlayBaseSize - new Vector2(amount, 0f);
        Vector2 pos = overlayBasePos - new Vector2(amount * (1f - tutorialOverlay.pivot.x), 0f);

        if (overlayCo != null) StopCoroutine(overlayCo);
        overlayCo = StartCoroutine(AnimateOverlay(size, pos));
    }

    private IEnumerator AnimateOverlay(Vector2 size, Vector2 pos)
    {
        Vector2 fromSize = tutorialOverlay.sizeDelta, fromPos = tutorialOverlay.anchoredPosition;
        for (float t = 0f; t < overlayCutDuration; t += Time.deltaTime)
        {
            float e = Mathf.SmoothStep(0f, 1f, t / overlayCutDuration);
            tutorialOverlay.sizeDelta = Vector2.Lerp(fromSize, size, e);
            tutorialOverlay.anchoredPosition = Vector2.Lerp(fromPos, pos, e);
            yield return null;
        }
        tutorialOverlay.sizeDelta = size;
        tutorialOverlay.anchoredPosition = pos;
        overlayCo = null;
    }

    public void OpenSection(string id)
    {
        if (referenceLists == null) return;
        var targets = new HashSet<AccordionSection>();
        foreach (var l in referenceLists)
            if (l != null && l.Section != null && string.Equals(l.SectionId, id, StringComparison.OrdinalIgnoreCase))
                targets.Add(l.Section);

        foreach (var l in referenceLists)
            if (l != null && l.Section != null && !targets.Contains(l.Section) && l.Section.EstaAbierto)
                l.Section.Plegar();
        foreach (var s in targets)
            if (!s.EstaAbierto) s.Desplegar();
    }

    public void CloseAllSections()
    {
        if (referenceLists == null) return;
        foreach (var l in referenceLists)
            if (l != null && l.Section != null && l.Section.EstaAbierto) l.Section.Plegar();
    }

    // ----------------------------- utilidades ------------------------------------- 
    private void SetButtonsInteractable(bool value)
    {
        if (approveButton != null) approveButton.interactable = value;
        if (rejectButton != null) rejectButton.interactable = value;
    }

    private static void SetObjectsActive(GameObject[] objs, bool value)
    {
        if (objs == null) return;
        foreach (var o in objs) if (o != null) o.SetActive(value);
    }
}
