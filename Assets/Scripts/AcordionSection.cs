using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AccordionSection : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Button headerButton;
    [SerializeField] private LayoutElement contentLayoutElement;
    [SerializeField] private GameObject contentPanel;
    [SerializeField] private RectTransform flechaIcon;
    [SerializeField] private RectTransform layoutRoot;
    [SerializeField] private CanvasGroup contentCanvasGroup; // opcional, para fade

    [Header("Configuración")]
    [SerializeField] private float duracionAnimacion = 0.3f;
    [SerializeField] private float alturaAbierta = 140f;
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public bool EstaAbierto { get; private set; } = false;
    public System.Action<AccordionSection> OnAbierto;

    private Coroutine animCoroutine;

    private void Awake()
    {
        contentLayoutElement.preferredHeight = 0f;
        contentPanel.SetActive(false);
        if (contentCanvasGroup != null) contentCanvasGroup.alpha = 0f;
        headerButton.onClick.AddListener(AlPulsarHeader);
    }

    private void AlPulsarHeader()
    {
        if (EstaAbierto) Plegar();
        else
        {
            OnAbierto?.Invoke(this);
            Desplegar();
        }
    }

    public void Desplegar()
    {
        EstaAbierto = true;
        contentPanel.SetActive(true);
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(Animar(alturaAbierta, 1f));
        if (flechaIcon != null) flechaIcon.localEulerAngles = new Vector3(0, 0, 180);
    }

    public void Plegar()
    {
        EstaAbierto = false;
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(Animar(0f, 0f, () => contentPanel.SetActive(false)));
        if (flechaIcon != null) flechaIcon.localEulerAngles = Vector3.zero;
    }

    private IEnumerator Animar(float alturaObjetivo, float alphaObjetivo, System.Action alTerminar = null)
    {
        float alturaInicial = contentLayoutElement.preferredHeight;
        float alphaInicial = contentCanvasGroup != null ? contentCanvasGroup.alpha : 1f;
        float tiempo = 0f;

        while (tiempo < duracionAnimacion)
        {
            tiempo += Time.deltaTime;
            float progresoLineal = tiempo / duracionAnimacion;
            float progresoSuave = easing.Evaluate(progresoLineal);

            contentLayoutElement.preferredHeight = Mathf.Lerp(alturaInicial, alturaObjetivo, progresoSuave);
            if (contentCanvasGroup != null)
                contentCanvasGroup.alpha = Mathf.Lerp(alphaInicial, alphaObjetivo, progresoSuave);

            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
            yield return null;
        }

        contentLayoutElement.preferredHeight = alturaObjetivo;
        if (contentCanvasGroup != null) contentCanvasGroup.alpha = alphaObjetivo;
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        alTerminar?.Invoke();
    }
}