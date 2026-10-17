using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Stand genérico de acceso a un minijuego. Se encarga solo de su presentación:
/// resalte al acercarse, globo de aviso y animación de entrada + carga de escena.
/// Quién está cerca y cuándo se pulsa la tecla lo decide StandInteractor (en el jugador).
/// </summary>
public class StandInteractable : MonoBehaviour, ICameraFocusTarget
{
    // Registro de stands activos: el interactor los recorre sin buscar en la escena cada frame
    private static readonly List<StandInteractable> activeStands = new List<StandInteractable>();
    public static IReadOnlyList<StandInteractable> ActiveStands => activeStands;

    [Header("Minijuego")]
    [Tooltip("Nombre exacto de la escena del minijuego. Debe estar añadida en Build Settings.")]
    [SerializeField] private string sceneName;

    [Header("Referencias")]
    [Tooltip("Imagen del stand que avanza al acercarse y se amplía al entrar.")]
    [SerializeField] private SpriteRenderer featuredPart;
    [Tooltip("SortingGroup que agrupa las dos imágenes del stand.")]
    [SerializeField] private SortingGroup sortingGroup;
    [Tooltip("Punto delante del stand al que camina el jugador. La distancia de interacción se mide desde aquí.")]
    [SerializeField] private Transform accessPoint;
    [SerializeField] private PromptBubble prompt;
    [Tooltip("Opcional: luz del stand que se intensifica al acercarse.")]
    [SerializeField] private Light2D standLight;

    [Header("Proximidad")]
    [Tooltip("Distancia desde el punto de acceso a la que la imagen destacada avanza.")]
    [SerializeField] private float highlightRadius = 5f;
    [Tooltip("Distancia desde el punto de acceso a la que aparece el globo y se puede entrar.")]
    [SerializeField] private float interactRadius = 1.6f;

    [Header("Animación de acercamiento")]
    [Tooltip("Desplazamiento local de la imagen destacada (negativo en Y = hacia delante en vista cenital).")]
    [SerializeField] private Vector2 highlightOffset = new Vector2(0f, -0.3f);
    [SerializeField] private float highlightScale = 1.08f;
    [Tooltip("Velocidad de la interpolación (más alto = más rápido).")]
    [SerializeField] private float highlightSpeed = 8f;
    [Tooltip("Balanceo suave mientras está resaltado (0 = sin balanceo).")]
    [SerializeField] private float bobAmplitude = 0.05f;
    [SerializeField] private float bobSpeed = 2.5f;
    [SerializeField] private float lightIdleIntensity = 0.3f;
    [SerializeField] private float lightHighlightIntensity = 1.2f;

    [Header("Animación de entrada")]
    [SerializeField] private float anticipationDuration = 0.15f;
    [Tooltip("Escala a la que se encoge antes de lanzarse (anticipación).")]
    [SerializeField] private float anticipationScale = 0.9f;
    [SerializeField] private float zoomDuration = 0.8f;
    [Tooltip("Margen extra para que la imagen cubra la pantalla por completo.")]
    [SerializeField] private float zoomOverscan = 1.3f;
    [Tooltip("Sorting Layer a la que pasa el stand durante el zoom. Debe estar por encima de la del jugador.")]
    [SerializeField] private string zoomSortingLayer = "WorldUI";
    [Tooltip("Orden de dibujado durante el zoom, para quedar por delante del jugador y del entorno.")]
    [SerializeField] private int zoomSortingOrder = 1000;
    [Tooltip("Id del efecto en la SoundLibrary (vacío = sin sonido).")]
    [SerializeField] private string enterSfxId = "stand_enter";

    [Header("Bloqueo")]
    [Tooltip("Color que multiplica las imágenes del stand mientras su minijuego está bloqueado.")]
    [SerializeField] private Color lockedTint = new Color(0.35f, 0.35f, 0.38f, 1f);
    [Tooltip("Duración del paso de gris a color al desbloquearse con animación.")]
    [SerializeField] private float unlockDuration = 0.8f;
    [Tooltip("Cuánto crece la imagen destacada en el 'pop' del desbloqueo.")]
    [SerializeField] private float unlockPopScale = 0.15f;
    [Tooltip("Id del efecto al desbloquearse (vacío = sin sonido).")]
    [SerializeField] private string unlockSfxId = "coin_sound";

    private Transform featuredTransform;
    private Vector3 restLocalPosition;
    private Vector3 restLocalScale;
    private float highlight;          // 0 = reposo, 1 = resaltado (valor ya interpolado)
    private bool isHighlighted;
    private bool isEntering;

    private SpriteRenderer[] tintedRenderers;
    private Color[] originalColors;
    private float lockVisual;         // 0 = a color, 1 = gris (valor ya interpolado)
    private float unlockPop;          // 1 al desbloquearse, baja a 0 durante unlockDuration

    /// <summary>Minijuego aún no disponible: el stand se ve gris y no se puede resaltar ni entrar.</summary>
    public bool IsLocked { get; private set; }
    public float HighlightRadius => highlightRadius;
    public float InteractRadius => interactRadius;
    public string SceneName => sceneName;
    public Vector2 AccessPosition => accessPoint != null ? (Vector2)accessPoint.position : (Vector2)transform.position;
    /// <summary>Posición del globo de aviso (o del stand si no tiene). La usa la cámara para encuadrarlo.</summary>
    public Vector2 PromptPosition => prompt != null ? (Vector2)prompt.transform.position : (Vector2)transform.position;
    // La cámara se acerca a los stands disponibles y deja a la vista su globo
    bool ICameraFocusTarget.CanFocus => !IsLocked;
    Vector2 ICameraFocusTarget.FocusPoint => PromptPosition;
    public bool HasValidScene => !string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);

    private void Awake()
    {
        featuredTransform = featuredPart.transform;
        restLocalPosition = featuredTransform.localPosition;
        restLocalScale = featuredTransform.localScale;

        // Se tiñen las imágenes del stand (Visuals), no el globo de aviso ni la sombra
        Transform visuals = sortingGroup != null ? sortingGroup.transform : featuredTransform;
        tintedRenderers = visuals.GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[tintedRenderers.Length];
        for (int i = 0; i < tintedRenderers.Length; i++)
            originalColors[i] = tintedRenderers[i].color;

        // En Awake, antes de cualquier Start: PathGuide puede cambiarlo después para animar el desbloqueo
        SetLocked(!GameProgress.IsUnlocked(sceneName));
    }

    private void OnEnable()
    {
        activeStands.Add(this);
        CameraFocusTargets.Register(this);
    }

    private void OnDisable()
    {
        activeStands.Remove(this);
        CameraFocusTargets.Unregister(this);
    }

    /// <summary>Stand activo que lleva a esa escena, o null.</summary>
    public static StandInteractable Find(string sceneName)
    {
        for (int i = 0; i < activeStands.Count; i++)
            if (activeStands[i].sceneName == sceneName) return activeStands[i];
        return null;
    }

    private void Update()
    {
        if (isEntering) return; // durante la entrada manda la corrutina

        // Interpolación exponencial: suave e independiente del framerate
        float target = isHighlighted ? 1f : 0f;
        highlight = Mathf.Lerp(highlight, target, 1f - Mathf.Exp(-highlightSpeed * Time.deltaTime));

        float eased = Mathf.SmoothStep(0f, 1f, highlight);
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude * eased;

        UpdateLockVisual();
        // Sube y baja una vez (media onda de seno) mientras se desbloquea
        float pop = Mathf.Sin(unlockPop * Mathf.PI) * unlockPopScale;

        featuredTransform.localPosition = restLocalPosition + (Vector3)(highlightOffset * eased) + Vector3.up * bob;
        featuredTransform.localScale = restLocalScale * (Mathf.Lerp(1f, highlightScale, eased) + pop);

        if (standLight != null)
            standLight.intensity = Mathf.Lerp(lightIdleIntensity, lightHighlightIntensity, eased + pop) * (1f - lockVisual);
    }

    /// <summary>Bloquea o desbloquea el stand. Con animate, al desbloquear pasa de gris a color con un "pop".</summary>
    public void SetLocked(bool locked, bool animate = false)
    {
        IsLocked = locked;
        if (locked)
        {
            isHighlighted = false;
            SetPromptVisible(false);
        }

        if (animate && !locked)
        {
            unlockPop = 1f;
            if (!string.IsNullOrEmpty(unlockSfxId) && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(unlockSfxId);
        }
        else
        {
            lockVisual = locked ? 1f : 0f;
            unlockPop = 0f;
            ApplyTint();
        }
    }

    private void UpdateLockVisual()
    {
        if (unlockPop > 0f)
            unlockPop = Mathf.Max(0f, unlockPop - Time.deltaTime / unlockDuration);

        float target = IsLocked ? 1f : 0f;
        if (Mathf.Approximately(lockVisual, target)) return;
        lockVisual = Mathf.MoveTowards(lockVisual, target, Time.deltaTime / unlockDuration);
        ApplyTint();
    }

    private void ApplyTint()
    {
        for (int i = 0; i < tintedRenderers.Length; i++)
            tintedRenderers[i].color = Color.Lerp(originalColors[i], originalColors[i] * lockedTint, lockVisual);
    }

    public void SetHighlighted(bool value) => isHighlighted = value;

    public void SetPromptVisible(bool visible)
    {
        if (prompt == null) return;
        if (visible) prompt.Show();
        else prompt.Hide();
    }

    /// <summary>Lanza la animación de entrada y, al terminar, carga el minijuego.</summary>
    public void Enter()
    {
        if (isEntering) return;
        StartCoroutine(EnterRoutine());
    }

    private IEnumerator EnterRoutine()
    {
        isEntering = true;
        SetPromptVisible(false);
        if (sortingGroup != null)
        {
            sortingGroup.sortingLayerName = zoomSortingLayer;
            sortingGroup.sortingOrder = zoomSortingOrder;
        }
        if (!string.IsNullOrEmpty(enterSfxId) && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(enterSfxId);

        // 1) Anticipación: se encoge un instante antes de lanzarse, da sensación de impulso
        Vector3 fromScale = featuredTransform.localScale;
        Vector3 squashScale = fromScale * anticipationScale;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / anticipationDuration);
            featuredTransform.localScale = Vector3.Lerp(fromScale, squashScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        // 2) Zoom: crece hasta cubrir la vista de la cámara mientras se centra en ella
        Camera cam = Camera.main;
        Bounds spriteBounds = featuredPart.sprite.bounds; // espacio local, relativo al pivote
        Vector2 currentSize = Vector2.Scale(spriteBounds.size, featuredTransform.lossyScale);
        float viewHeight = cam.orthographicSize * 2f;
        float viewWidth = viewHeight * cam.aspect;
        float factor = Mathf.Max(viewWidth / currentSize.x, viewHeight / currentSize.y) * zoomOverscan;
        Vector3 toScale = squashScale * factor;
        Vector3 fromPosition = featuredTransform.position;

        t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / zoomDuration);
            float eased = t * t * t; // ease-in cúbico: arranca suave y acelera al "tragarse" la cámara

            featuredTransform.localScale = Vector3.Lerp(squashScale, toScale, eased);

            // Se lleva el centro del sprite (no su pivote) al centro de la cámara
            Vector3 pivotToCenter = Vector3.Scale(spriteBounds.center, featuredTransform.lossyScale);
            Vector3 target = cam.transform.position - pivotToCenter;
            target.z = fromPosition.z;
            featuredTransform.position = Vector3.Lerp(fromPosition, target, eased);

            yield return null;
        }

        SceneTransition.Instance.LoadScene(sceneName);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f);
        Gizmos.DrawWireSphere(AccessPosition, highlightRadius);
        Gizmos.color = new Color(0.2f, 1f, 0.4f);
        Gizmos.DrawWireSphere(AccessPosition, interactRadius);
    }
}