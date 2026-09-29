using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Animación de un icono de vida. Se pone en cada corazón del HUD
// ContractHUD llama a SetAlive y el propio icono se encarga de aparecer/desaparecer
public class HeartIcon : MonoBehaviour
{
    [Tooltip("Lo que se anima. Si el corazón está dentro de un LayoutGroup conviene que sea un hijo, para que el layout no pise la posición")]
    [SerializeField] private RectTransform visual;
    [Tooltip("Imagen a la que se le cambia el color en los destellos")]
    [SerializeField] private Graphic graphic;
    [Tooltip("Sigue animando aunque Time.timeScale sea 0 (pantalla final, pausa...)")]
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Perder vida")]
    [SerializeField] private Color loseFlashColor = Color.white;
    [SerializeField] private float loseDuration = 0.6f;
    [SerializeField] private float shakeStrength = 12f;
    [SerializeField] private float fallDistance = 60f;
    [SerializeField] private float spinDegrees = 120f;

    [Header("Ganar vida")]
    [SerializeField] private Color gainFlashColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private float gainDuration = 0.5f;
    [SerializeField] private int heartbeats = 2;

    private CanvasGroup group;
    private Vector3 baseScale;
    private Vector2 basePos;
    private Color baseColor;
    private bool alive = true;
    private bool initialized;
    private Coroutine anim;

    private float Dt => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private void Awake()
    {
        if (visual == null) visual = (RectTransform)transform;
        if (graphic == null) graphic = visual.GetComponent<Graphic>();
        group = visual.GetComponent<CanvasGroup>();
        if (group == null) group = visual.gameObject.AddComponent<CanvasGroup>();

        baseScale = visual.localScale;
        basePos = visual.anchoredPosition;
        if (graphic != null) baseColor = graphic.color;
    }

    // La primera llamada solo coloca el estado, sin animación (inicio de partida)
    public void SetAlive(bool value)
    {
        if (!initialized)
        {
            initialized = true;
            alive = value;
            ResetVisual();
            gameObject.SetActive(value);
            return;
        }
        if (value == alive) return;
        alive = value;

        gameObject.SetActive(true); // hace falta estar activo para lanzar la corrutina
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(alive ? GainRoutine() : LoseRoutine());
    }

    private void ResetVisual()
    {
        visual.localScale = baseScale;
        visual.anchoredPosition = basePos;
        visual.localRotation = Quaternion.identity;
        group.alpha = 1f;
        if (graphic != null) graphic.color = baseColor;
    }

    // Golpe + temblor + destello, y luego cae girando mientras se desvanece
    private IEnumerator LoseRoutine()
    {
        ResetVisual();

        // 1) Impacto: crece de golpe, tiembla y se pone blanco
        float hit = loseDuration * 0.35f;
        for (float t = 0f; t < hit; t += Dt)
        {
            float k = t / hit;
            float punch = 1f + Mathf.Sin(k * Mathf.PI) * 0.35f;
            visual.localScale = baseScale * punch;
            visual.anchoredPosition = basePos + Random.insideUnitCircle * shakeStrength * (1f - k);
            if (graphic != null) graphic.color = Color.Lerp(loseFlashColor, baseColor, k);
            yield return null;
        }

        // 2) Se rompe: encoge, cae, gira y se desvanece
        float fall = loseDuration - hit;
        float dir = Random.value < 0.5f ? -1f : 1f;
        for (float t = 0f; t < fall; t += Dt)
        {
            float k = t / fall;
            float ease = k * k; // acelera como si cayera
            visual.localScale = baseScale * Mathf.Lerp(1f, 0.3f, ease);
            visual.anchoredPosition = basePos + new Vector2(dir * 15f * k, -fallDistance * ease);
            visual.localRotation = Quaternion.Euler(0f, 0f, dir * spinDegrees * ease);
            group.alpha = 1f - ease;
            if (graphic != null) graphic.color = Color.Lerp(baseColor, Color.gray, k);
            yield return null;
        }

        ResetVisual();
        anim = null;
        gameObject.SetActive(false);
    }

    // Aparece desde 0 con rebote y da un par de latidos brillando
    private IEnumerator GainRoutine()
    {
        ResetVisual();
        group.alpha = 0f;
        visual.localScale = Vector3.zero;

        // 1) Pop con rebote y un pequeño giro
        for (float t = 0f; t < gainDuration; t += Dt)
        {
            float k = t / gainDuration;
            visual.localScale = baseScale * EaseOutBack(k);
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-25f, 0f, k));
            group.alpha = Mathf.Clamp01(k * 3f);
            if (graphic != null) graphic.color = Color.Lerp(gainFlashColor, baseColor, k * 0.5f);
            yield return null;
        }
        visual.localRotation = Quaternion.identity;
        group.alpha = 1f;

        // 2) Latidos (bum-bum) mientras recupera su color
        const float beat = 0.22f;
        for (int b = 0; b < heartbeats; b++)
        {
            for (float t = 0f; t < beat; t += Dt)
            {
                float k = t / beat;
                visual.localScale = baseScale * (1f + Mathf.Sin(k * Mathf.PI) * 0.18f);
                if (graphic != null)
                    graphic.color = Color.Lerp(gainFlashColor, baseColor, 0.5f + 0.5f * (b + k) / heartbeats);
                yield return null;
            }
        }

        ResetVisual();
        anim = null;
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float p = x - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }
}
