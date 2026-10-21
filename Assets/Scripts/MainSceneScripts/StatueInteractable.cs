using TMPro;
using UnityEngine;

/// <summary>
/// Compra de mejoras de la estatua. Al acercarse, el globo muestra el estado de la estatua y,
/// cuando ya se han jugado todos los minijuegos, el coste del siguiente nivel; con Espacio o Enter
/// se compra (StatueProgress). Si no llegan las bolsas, el globo tiembla.
/// Usa el mismo jugador e input que los stands (StandInteractor) y les cede la tecla si hay uno cerca.
/// </summary>
public class StatueInteractable : MonoBehaviour, ICameraFocusTarget
{
    [Header("Referencias")]
    [SerializeField] private StatueBuilder builder;
    [Tooltip("Punto delante de la estatua desde el que se mide la distancia de interacción.")]
    [SerializeField] private Transform accessPoint;
    [SerializeField] private PromptBubble prompt;

    [Header("Proximidad")]
    [Tooltip("Distancia desde el punto de acceso a la que la cámara empieza a acercarse a la estatua (CameraStandFocus).")]
    [SerializeField] private float highlightRadius = 5f;
    [Tooltip("Distancia desde el punto de acceso a la que aparece el globo y se puede comprar.")]
    [SerializeField] private float interactRadius = 2.5f;

    [Header("Textos del globo")]
    [Tooltip("Mientras quedan minijuegos por jugar.")]
    [SerializeField, TextArea(2, 4)] private string lockedText =
        "<b>Estatua</b>\n<size=70%><color=#94A3B8>Juega a los minijuegos para construirla</color></size>";
    [Tooltip("{0} = nivel siguiente, {1} = nivel máximo, {2} = coste en bolsas.")]
    [SerializeField, TextArea(2, 4)] private string upgradeText =
        "<b>Mejorar estatua</b>  <size=70%><color=#94A3B8>Nivel {0}/{1}</color></size>\n" +
        "<size=80%>{2} bolsas</size>\n" +
        "<size=70%><color=#94A3B8>Pulsa</color> <color=#FBBF24>Espacio</color> <color=#94A3B8>o</color> <color=#FBBF24>Enter</color></size>";
    [SerializeField, TextArea(2, 4)] private string completeText =
        "<b>¡Estatua completada!</b>";

    [Header("Textos del globo en inglés")]
    [SerializeField, TextArea(2, 4)] private string lockedTextEn =
        "<b>Statue</b>\n<size=70%><color=#94A3B8>Play the minigames to build it</color></size>";
    [SerializeField, TextArea(2, 4)] private string upgradeTextEn =
        "<b>Upgrade statue</b>  <size=70%><color=#94A3B8>Level {0}/{1}</color></size>\n" +
        "<size=80%>{2} bags</size>\n" +
        "<size=70%><color=#94A3B8>Press</color> <color=#FBBF24>Space</color> <color=#94A3B8>or</color> <color=#FBBF24>Enter</color></size>";
    [SerializeField, TextArea(2, 4)] private string completeTextEn =
        "<b>Statue completed!</b>";
    [Tooltip("Color del coste cuando llegan las bolsas.")]
    [SerializeField] private Color affordableColor = new Color32(0xFB, 0xBF, 0x24, 0xFF);
    [Tooltip("Color del coste cuando no llegan las bolsas.")]
    [SerializeField] private Color unaffordableColor = new Color32(0xF8, 0x71, 0x71, 0xFF);

    [Header("Feedback")]
    [Tooltip("Id del efecto al comprar (vacío = sin sonido).")]
    [SerializeField] private string buySfxId = "validarButtonSound";
    [Tooltip("Id del efecto si no llegan las bolsas (vacío = sin sonido).")]
    [SerializeField] private string deniedSfxId = "";
    [SerializeField] private float shakeDuration = 0.35f;
    [SerializeField] private float shakeAmplitude = 0.12f;

    private StandInteractor interactor;
    private TMP_Text promptText;
    private Transform[] shakeTargets;   // hijos del globo (fondo y texto)
    private Vector3[] shakeRest;
    private string currentText;
    private bool promptVisible;
    private float shakeTimer;

    public Vector2 AccessPosition => accessPoint != null ? (Vector2)accessPoint.position : (Vector2)transform.position;
    public float HighlightRadius => highlightRadius;
    // Durante la cinemática de crecimiento la cámara ya está sobre la estatua: no se suma el desplazamiento
    public bool CanFocus => !StatueBuilder.IsCinematicPlaying;
    /// <summary>La cámara intenta dejar a la vista el foco de la estatua (su CameraFocus, hacia el centro).</summary>
    public Vector2 FocusPoint => builder != null ? builder.FocusPosition : (Vector2)transform.position;

    private void Awake()
    {
        if (builder == null) builder = GetComponent<StatueBuilder>();
        if (prompt != null)
        {
            promptText = prompt.GetComponentInChildren<TMP_Text>(true);
            Transform bubble = prompt.transform;
            shakeTargets = new Transform[bubble.childCount];
            shakeRest = new Vector3[bubble.childCount];
            for (int i = 0; i < bubble.childCount; i++)
            {
                shakeTargets[i] = bubble.GetChild(i);
                shakeRest[i] = shakeTargets[i].localPosition;
            }
        }
    }

    private void OnEnable() => CameraFocusTargets.Register(this);
    private void OnDisable() => CameraFocusTargets.Unregister(this);

    private void Start()
    {
        interactor = FindObjectOfType<StandInteractor>();
        if (interactor == null) Debug.LogWarning("StatueInteractable: no hay jugador (StandInteractor) en la escena.", this);
    }

    private void Update()
    {
        UpdateShake();

        bool inRange = CanInteract()
            && Vector2.Distance(interactor.transform.position, AccessPosition) <= interactRadius;
        SetPromptVisible(inRange);
        if (!inRange) return;

        RefreshText();

        // Mientras caen las capas de la compra anterior no se acepta otra, para que se vean una a una
        if (StandInteractor.InteractPressed() && !builder.IsAnimating)
            TryBuy();
    }

    private bool CanInteract()
    {
        return interactor != null
            && interactor.InteractionEnabled          // cinemáticas (camino guía, crecimiento de la estatua)
            && !interactor.HasStandInRange            // si hay un stand cerca, la tecla es suya
            && !StatueBuilder.IsCinematicPlaying
            && !StandInteractor.IsDialogOpen();
    }

    private void TryBuy()
    {
        switch (StatueProgress.TryBuyNextLevel())
        {
            case StatueProgress.PurchaseResult.Bought:
                PlaySfx(buySfxId);
                break; // StatueBuilder detecta el nivel nuevo y hace caer la capa

            case StatueProgress.PurchaseResult.NotEnoughBags:
                PlaySfx(deniedSfxId);
                shakeTimer = shakeDuration;
                break;

            case StatueProgress.PurchaseResult.Unavailable:
                Debug.LogWarning("StatueInteractable: no hay AssetsManager (¿se abrió la escena sin pasar por el menú?).", this);
                break;
        }
    }

    private void RefreshText()
    {
        if (promptText == null) return;

        string text;
        if (StatueProgress.IsComplete) text = LanguageManager.Pick(completeText, completeTextEn);
        else if (!StatueProgress.CanBuyUpgrade) text = LanguageManager.Pick(lockedText, lockedTextEn);
        else
        {
            string color = ColorUtility.ToHtmlStringRGB(StatueProgress.CanAffordNextUpgrade ? affordableColor : unaffordableColor);
            string cost = $"<color=#{color}>{StatueProgress.NextUpgradeCost}</color>";
            text = string.Format(LanguageManager.Pick(upgradeText, upgradeTextEn), StatueProgress.Level + 1, StatueProgress.MaxLevel, cost);
        }

        // Solo se reasigna si cambia: TextMeshPro regenera la malla en cada asignación
        if (text == currentText) return;
        currentText = text;
        promptText.text = text;
    }

    private void SetPromptVisible(bool visible)
    {
        if (prompt == null || visible == promptVisible) return;
        promptVisible = visible;
        if (visible) prompt.Show();
        else prompt.Hide();
    }

    // Se mueven los hijos y no el globo: PromptBubble recoloca su propio transform cada frame
    private void UpdateShake()
    {
        if (shakeTargets == null || shakeTimer <= 0f) return;

        shakeTimer = Mathf.Max(0f, shakeTimer - Time.deltaTime);
        float strength = shakeTimer / shakeDuration;
        float offset = Mathf.Sin(Time.time * 60f) * shakeAmplitude * strength;
        for (int i = 0; i < shakeTargets.Length; i++)
            shakeTargets[i].localPosition = shakeRest[i] + Vector3.right * offset;
    }

    private static void PlaySfx(string id)
    {
        if (!string.IsNullOrEmpty(id) && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(id);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f);
        Gizmos.DrawWireSphere(AccessPosition, highlightRadius);
        Gizmos.color = new Color(0.2f, 1f, 0.4f);
        Gizmos.DrawWireSphere(AccessPosition, interactRadius);
    }
}
