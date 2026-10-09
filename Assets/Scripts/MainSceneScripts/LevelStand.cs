using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Stand de seleccion de nivel. Se compone de:
//  - Base: parte fija del stand (con collider solido para que el jugador no la atraviese).
//  - Parte que se acerca: se agranda cuando el jugador entra en el area del stand.
//  - Indicador: muestra la tecla que hay que pulsar para entrar al minijuego.
// El area del stand es el Collider2D (trigger) de este mismo GameObject.
[RequireComponent(typeof(Collider2D))]
public class LevelStand : MonoBehaviour
{
    [Header("Minijuego")]
    public string sceneName = "BlockMiner";      // Escena a cargar (debe estar en Build Settings)
    public string displayName = "BlockMiner";    // Nombre que aparece en el indicador
    public KeyCode interactKey = KeyCode.E;

    [Header("Partes del stand")]
    public Transform approachPart;               // Parte que se acerca/agranda
    public GameObject promptIndicator;           // Indicador con la tecla
    public TMP_Text promptLabel;                 // Texto del indicador (opcional)

    [Header("Animacion")]
    public float highlightScale = 1.35f;         // Multiplicador de escala al acercarse
    public float scaleSpeed = 10f;
    public float promptBobAmplitude = 0.08f;
    public float promptBobFrequency = 2f;

    // Solo un stand puede estar activo a la vez (el ultimo en el que entro el jugador)
    private static LevelStand activeStand;

    private Vector3 approachBaseScale;
    private Vector3 promptBasePos;
    private Vector3 promptBaseScale;
    private float promptVisibility;              // 0 = oculto, 1 = visible
    private bool playerInside;
    private bool loading;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (approachPart != null) approachBaseScale = approachPart.localScale;

        if (promptIndicator != null)
        {
            promptBasePos = promptIndicator.transform.localPosition;
            promptBaseScale = promptIndicator.transform.localScale;
            promptIndicator.transform.localScale = Vector3.zero;
            promptIndicator.SetActive(false);
        }

        if (promptLabel != null)
            promptLabel.text = $"<b>[{interactKey}]</b>  Jugar {displayName}";
    }

    void Update()
    {
        bool highlighted = playerInside && activeStand == this;

        // Parte que se acerca: se agranda de forma suave
        if (approachPart != null)
        {
            Vector3 target = highlighted ? approachBaseScale * highlightScale : approachBaseScale;
            approachPart.localScale = Vector3.Lerp(approachPart.localScale, target, Time.deltaTime * scaleSpeed);
        }

        // Indicador: aparece escalando y flota ligeramente
        if (promptIndicator != null)
        {
            promptVisibility = Mathf.MoveTowards(promptVisibility, highlighted ? 1f : 0f, Time.deltaTime * scaleSpeed * 0.5f);
            promptIndicator.SetActive(promptVisibility > 0f);

            float eased = 1f - Mathf.Pow(1f - promptVisibility, 3f);
            promptIndicator.transform.localScale = promptBaseScale * eased;
            promptIndicator.transform.localPosition = promptBasePos
                + Vector3.up * Mathf.Sin(Time.time * promptBobFrequency * Mathf.PI * 2f) * promptBobAmplitude;
        }

        if (highlighted && !loading && Input.GetKeyDown(interactKey))
            EnterMinigame();
    }

    private void EnterMinigame()
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[LevelStand] La escena '{sceneName}' no existe o no esta en Build Settings.");
            return;
        }

        loading = true;
        StartCoroutine(LoadAfterPunch());
    }

    // Pequeno "golpe" visual antes de cambiar de escena
    private IEnumerator LoadAfterPunch()
    {
        if (approachPart != null)
        {
            Vector3 from = approachPart.localScale;
            Vector3 to = approachBaseScale * (highlightScale + 0.15f);
            for (float t = 0f; t < 0.15f; t += Time.deltaTime)
            {
                approachPart.localScale = Vector3.Lerp(from, to, t / 0.15f);
                yield return null;
            }
        }

        SceneManager.LoadScene(sceneName);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        activeStand = this;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
        if (activeStand == this) activeStand = null;
    }

    void OnDisable()
    {
        playerInside = false;
        if (activeStand == this) activeStand = null;
    }
}
