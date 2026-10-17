using System.Collections;
using Cinemachine;
using UnityEngine;

/// <summary>
/// Estatua del mapa construida por capas acumulativas: con nivel N se ven las N primeras.
/// Cuando el nivel sube (al comprar una mejora o al volver de un minijuego) las capas nuevas caen
/// en su sitio una a una. Al volver de un minijuego, la cámara se acerca a la estatua para enseñarlo.
/// Se monta en la escena con Tools > Estatua (StatueSceneSetup).
/// </summary>
public class StatueBuilder : MonoBehaviour
{
    // Estático: recuerda lo que ya vio el jugador entre cambios de escena, para animar solo lo nuevo
    private static int lastShownLevel = -1;
    private static string lastShownAddress;

    /// <summary>true mientras la cámara está enseñando el crecimiento. PathGuide espera a que termine.</summary>
    public static bool IsCinematicPlaying { get; private set; }

    [Header("Capas")]
    [Tooltip("Las capas de abajo a arriba (Statue_01 ... Statue_10). Todas con el mismo tamaño de lienzo y pivote.")]
    [SerializeField] private SpriteRenderer[] layers = new SpriteRenderer[StatueProgress.MaxLevel];
    [Tooltip("Opcional: se ve solo mientras no hay ninguna capa (solar en obras, cartel...).")]
    [SerializeField] private GameObject emptyVisual;
    [Tooltip("Raíz de las capas (con el SortingGroup). Se aplasta un poco cuando encaja una pieza.")]
    [SerializeField] private Transform visualsRoot;
    [Tooltip("Punto que sigue la cámara al enseñar el crecimiento. Vacío = la propia estatua.")]
    [SerializeField] private Transform cameraFocus;

    [Header("Animación de capa nueva")]
    [Tooltip("Altura desde la que cae cada capa, en unidades.")]
    [SerializeField] private float dropHeight = 1.5f;
    [SerializeField] private float dropDuration = 0.45f;
    [Tooltip("Cuánto se aplasta la estatua al encajar la pieza (0.06 = un 6%).")]
    [SerializeField] private float squashAmount = 0.06f;
    [SerializeField] private float squashDuration = 0.3f;
    [Tooltip("Pausa entre capas cuando suben varias a la vez.")]
    [SerializeField] private float delayBetweenLayers = 0.2f;
    [Tooltip("Id del efecto al encajar cada capa (vacío = sin sonido).")]
    [SerializeField] private string layerSfxId = "coin_sound";

    [Header("Cinemática al volver de un minijuego")]
    [Tooltip("Si es false, las capas nuevas aparecen igual pero sin mover la cámara ni bloquear al jugador.")]
    [SerializeField] private bool focusCameraOnGrowth = true;
    [Tooltip("Espera tras cargar la escena antes de mover la cámara.")]
    [SerializeField] private float startDelay = 0.6f;
    [Tooltip("Tiempo que se da a la cámara para llegar a la estatua.")]
    [SerializeField] private float cameraTravelTime = 1f;
    [Tooltip("Pausa sobre la estatua terminada antes de devolver la cámara al jugador.")]
    [SerializeField] private float holdOnStatue = 1f;
    [Tooltip("Tiempo que se da a la cámara para volver al jugador antes de devolverle el control.")]
    [SerializeField] private float cameraReturnTime = 0.8f;

    [Header("Editor")]
    [Tooltip("Solo en el editor, fuera de Play: capas visibles, para comprobar la alineación.")]
    [SerializeField, Range(0, StatueProgress.MaxLevel)] private int editorPreviewLevel = StatueProgress.MaxLevel;

    private Vector3[] restPositions;
    private Color[] restColors;
    private Vector3 visualsRestScale;
    private bool isRevealing;
    private bool cinematicPending;

    /// <summary>Capas visibles ahora mismo (puede ir por detrás de StatueProgress.Level mientras se anima).</summary>
    public int ShownLevel { get; private set; }
    /// <summary>true mientras hay capas cayendo o falta enseñar alguna.</summary>
    public bool IsAnimating => isRevealing || cinematicPending || ShownLevel != StatueProgress.Level;
    public Vector2 FocusPosition => cameraFocus != null ? (Vector2)cameraFocus.position : (Vector2)transform.position;

    /// <summary>
    /// Llamar justo antes de subir el nivel fuera del mapa (al terminar un minijuego). Si el jugador aún no
    /// ha pasado por el mapa en esta sesión (partida nueva: el menú abre BlockMiner directamente), apunta el
    /// nivel actual como ya visto, para que al volver se anime el crecimiento en vez de aparecer de golpe.
    /// </summary>
    public static void RememberLevelBeforeGrowth()
    {
        if (lastShownLevel >= 0 && lastShownAddress == PlayerData.Address) return; // ya sabe lo que vio
        lastShownLevel = StatueProgress.Level;
        lastShownAddress = PlayerData.Address;
    }

    private void Awake()
    {
        restPositions = new Vector3[layers.Length];
        restColors = new Color[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null) continue;
            restPositions[i] = layers[i].transform.localPosition;
            restColors[i] = layers[i].color;
        }
        if (visualsRoot != null) visualsRestScale = visualsRoot.localScale;

        // Misma wallet que la última vez: se parte de lo que ya vio y se anima el resto.
        // Primera carga o wallet distinta: se enseña tal cual, sin animación.
        int level = StatueProgress.Level;
        bool sameSession = lastShownLevel >= 0 && lastShownAddress == PlayerData.Address;
        ShowInstant(sameSession ? Mathf.Min(lastShownLevel, level) : level);

        // En Awake, antes de cualquier Start: PathGuide lo comprueba para esperar su turno
        if (ShownLevel < level && focusCameraOnGrowth)
        {
            cinematicPending = true;
            IsCinematicPlaying = true;
        }
    }

    private void Start()
    {
        if (cinematicPending) StartCoroutine(GrowthCinematic());
    }

    private void OnDestroy()
    {
        if (cinematicPending) IsCinematicPlaying = false;
    }

    private void Update()
    {
        if (isRevealing || cinematicPending) return;

        int level = StatueProgress.Level;
        if (level > ShownLevel) StartCoroutine(RevealUpToCurrent());
        else if (level < ShownLevel) ShowInstant(level); // cambio de wallet o partida nueva
    }

    private void ShowInstant(int level)
    {
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null) continue;
            layers[i].enabled = i < level;
            layers[i].transform.localPosition = restPositions[i];
            layers[i].color = restColors[i];
        }
        if (visualsRoot != null) visualsRoot.localScale = visualsRestScale;
        if (emptyVisual != null) emptyVisual.SetActive(level == 0);
        SetShownLevel(level);
    }

    private void SetShownLevel(int level)
    {
        ShownLevel = level;
        lastShownLevel = level;
        lastShownAddress = PlayerData.Address;
    }

    // ---------- Animaciones ----------

    private IEnumerator GrowthCinematic()
    {
        StandInteractor interactor = FindObjectOfType<StandInteractor>();
        playerMovement movement = interactor != null ? interactor.GetComponent<playerMovement>() : null;
        CinemachineVirtualCamera vcam = FindObjectOfType<CinemachineVirtualCamera>();

        SetPlayerControl(interactor, movement, false);
        yield return new WaitForSeconds(startDelay);

        Transform previousFollow = vcam != null ? vcam.Follow : null;
        if (vcam != null)
        {
            vcam.Follow = cameraFocus != null ? cameraFocus : transform;
            yield return new WaitForSeconds(cameraTravelTime);
        }

        yield return RevealUpToCurrent();
        yield return new WaitForSeconds(holdOnStatue);

        if (vcam != null)
        {
            vcam.Follow = previousFollow;
            yield return new WaitForSeconds(cameraReturnTime);
        }

        SetPlayerControl(interactor, movement, true);
        cinematicPending = false;
        IsCinematicPlaying = false;
    }

    private static void SetPlayerControl(StandInteractor interactor, playerMovement movement, bool enabled)
    {
        if (interactor != null) interactor.InteractionEnabled = enabled;
        if (movement != null) movement.ControlEnabled = enabled;
    }

    /// <summary>Hace caer, una a una, las capas que faltan hasta el nivel actual (también si sube mientras tanto).</summary>
    private IEnumerator RevealUpToCurrent()
    {
        isRevealing = true;
        if (emptyVisual != null) emptyVisual.SetActive(false);

        while (ShownLevel < StatueProgress.Level)
        {
            int index = ShownLevel;
            if (index < layers.Length && layers[index] != null)
                yield return DropLayer(index);
            SetShownLevel(index + 1);

            if (ShownLevel < StatueProgress.Level)
                yield return new WaitForSeconds(delayBetweenLayers);
        }

        isRevealing = false;
    }

    private IEnumerator DropLayer(int index)
    {
        SpriteRenderer layer = layers[index];
        Transform layerTransform = layer.transform;
        Vector3 rest = restPositions[index];
        Color color = restColors[index];
        layer.enabled = true;

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / dropDuration);
            float fall = t * t; // ease-in cuadrático: acelera como si cayera
            layerTransform.localPosition = rest + Vector3.up * (dropHeight * (1f - fall));
            layer.color = new Color(color.r, color.g, color.b, color.a * Mathf.Clamp01(t * 3f));
            yield return null;
        }

        layerTransform.localPosition = rest;
        layer.color = color;
        if (!string.IsNullOrEmpty(layerSfxId) && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(layerSfxId);

        // El aplastado va aparte para que la siguiente capa pueda empezar a caer mientras rebota
        if (visualsRoot != null) StartCoroutine(Squash());
    }

    private IEnumerator Squash()
    {
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / squashDuration);
            // Media onda de seno amortiguada: se ensancha y baja, y vuelve a su sitio
            float s = Mathf.Sin(t * Mathf.PI) * (1f - t) * squashAmount * 2f;
            visualsRoot.localScale = Vector3.Scale(visualsRestScale, new Vector3(1f + s, 1f - s, 1f));
            yield return null;
        }
        visualsRoot.localScale = visualsRestScale;
    }

    // ---------- Editor ----------

    private void OnValidate()
    {
        if (Application.isPlaying || layers == null) return;
        for (int i = 0; i < layers.Length; i++)
            if (layers[i] != null) layers[i].enabled = i < editorPreviewLevel;
    }
}
