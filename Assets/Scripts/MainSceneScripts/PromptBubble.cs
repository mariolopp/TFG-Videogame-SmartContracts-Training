using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Globo de aviso en el mundo (sprite + TextMeshPro hijos). Aparece con un "pop"
/// elástico, flota suavemente y se desactiva solo al terminar de ocultarse.
/// Reutilizable para cualquier elemento interactuable (stands, edificio...).
/// </summary>
public class PromptBubble : MonoBehaviour
{
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float floatAmplitude = 0.08f;
    [SerializeField] private float floatSpeed = 3f;
    [Tooltip("Opcional: Sorting Layer del globo. Si no existe se queda en la suya y basta con el orden de abajo.")]
    [SerializeField] private string sortingLayer = "WorldUI";
    [Tooltip("Orden del globo dentro de su capa. Debe ser mayor que el del jugador para quedar siempre por delante.")]
    [SerializeField] private int sortingOrder = 500;

    private Vector3 baseScale;
    private Vector3 basePosition;
    private float visibility; // 0 = oculto, 1 = visible
    private bool shouldShow;

    private void Awake()
    {
        baseScale = transform.localScale;
        basePosition = transform.localPosition;
        transform.localScale = Vector3.zero;
        ApplySortingLayer();
    }

    // Un SortingGroup hace que fondo y texto se ordenen como una sola pieza: sin él, cada uno se
    // ordena por su propia altura y el jugador puede quedar entre ambos. Dentro del grupo se
    // mantiene su orden relativo (el texto encima del fondo).
    private void ApplySortingLayer()
    {
        if (!TryGetComponent(out SortingGroup group))
            group = gameObject.AddComponent<SortingGroup>();

        int layerId = SortingLayer.NameToID(sortingLayer);
        if (SortingLayer.IsValid(layerId))
            group.sortingLayerID = layerId;
        group.sortingOrder = sortingOrder;
    }

    private void Update()
    {
        visibility = Mathf.MoveTowards(visibility, shouldShow ? 1f : 0f, Time.deltaTime / popDuration);

        transform.localScale = baseScale * EaseOutBack(visibility);
        transform.localPosition = basePosition + Vector3.up * (Mathf.Sin(Time.time * floatSpeed) * floatAmplitude);

        if (!shouldShow && visibility <= 0f)
            gameObject.SetActive(false);
    }

    public void Show()
    {
        shouldShow = true;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        shouldShow = false;
    }

    // Sobrepasa ligeramente el tamaño final y vuelve: efecto "pop"
    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float p = x - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }
}