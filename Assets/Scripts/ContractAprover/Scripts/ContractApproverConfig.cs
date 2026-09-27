using UnityEngine;

[CreateAssetMenu(fileName = "ContractApproverConfig", menuName = "ContractApprover/Game Config")]
public class ContractApproverConfig : ScriptableObject
{
    [Header("Sesión")]
    [Tooltip("Cuántas transacciones tiene la partida (sin contar el tutorial)")]
    public int transactionsPerSession = 15;
    public bool playTutorial = true;

    [Header("Probabilidad de dificultad (pesos relativos)")]
    public float easyWeight = 50f;
    public float normalWeight = 30f;
    public float hardWeight = 20f;

    [Header("Tiempo por transacción (segundos)")]
    public float startTime = 30f;
    public float minTime = 8f;
    [Tooltip("Segundos que se restan al límite en cada transacción nueva")]
    public float timeDecreasePerTx = 1.5f;
    [Tooltip("Si está marcado, el reloj no corre hasta que el jugador abre el panel de la firma")]
    public bool timerStartsOnReveal = false;

    [Header("Tutorial")]
    public bool tutorialHasTimer = false;
    public float tutorialTime = 60f;
    public bool tutorialAffectsScore = false;

    [Header("Vidas")]
    public int startLives = 3;
    public int maxLives = 5;
    [Tooltip("Vidas que se pierden al APROBAR una transacción fraudulenta")]
    public int livesLostOnBadApprove = 1;
    [Tooltip("Vidas que se pierden al RECHAZAR una transacción legítima")]
    public int livesLostOnGoodReject = 0;
    public int livesLostOnTimeout = 1;

    [Header("Puntos")]
    public int pointsCorrect = 100;
    public int pointsLostOnGoodReject = 50;
    [Tooltip("Cada vez que se acumulan estos puntos se gana una vida extra sin superar maxLives")]
    public int pointsForExtraLife = 1000;

    [Header("Listas negras")]
    [Tooltip("Cuántas transacciones ANTES de que aparezca un fraude se añade su dirección a la lista")]
    public int blacklistLeadTurns = 3;
    [Tooltip("Durante cuántas transacciones se marca una entrada como NUEVO")]
    public int newTagTurns = 2;

    [Header("Valores por defecto si falta algo en reference_data.json")]
    public float defaultMaxSlippage = 1f;
    public float defaultMaxFeeGwei = 30f;

    [Header("Animación y feedback")]
    public float cardMoveDuration = 0.3f;
    public float swipeDuration = 0.35f;
    public float stampHoldTime = 0.6f;
    public float toastDuration = 3f;
    public Color correctColor = new Color(0.3f, 0.85f, 0.4f);
    public Color wrongColor = new Color(0.9f, 0.3f, 0.3f);

    [Header("Sonidos (IDs de SoundLibrary)")]
    public string sfxCorrect = "";
    public string sfxWrong = "";
    public string sfxNewBlacklistEntry = "";

    [Header("Debug")]
    [Tooltip("Comprobador de cada transacción con el validador automático para avisar en la Console si el JSON parece incoherente")]
    public bool crossCheckJson = true;
}
