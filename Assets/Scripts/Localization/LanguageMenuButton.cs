using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Botón del menú de inicio que muestra el idioma activo ("Español" / "English") con el icono
/// de idioma a su izquierda y abre la ventana LanguageMenu al pulsarlo.
/// </summary>
public class LanguageMenuButton : MonoBehaviour
{
    private TMP_Text label;

    /// <param name="menuTemplate">Botón cuyo estilo copia la ventana de idioma (los botones de esta son más grandes)</param>
    public static void Setup(Button button, Button menuTemplate)
    {
        if (button == null) return;
        var menuButton = button.gameObject.AddComponent<LanguageMenuButton>();
        menuButton.Build();
        button.onClick.AddListener(() =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("select_button");
            LanguageMenu.Open(menuTemplate);
        });
    }

    // Icono a la izquierda del botón y el texto en el espacio restante
    private void Build()
    {
        label = GetComponentInChildren<TMP_Text>(true);
        Sprite icon = LanguageToggleButton.LoadIcon();
        float h = ((RectTransform)transform).rect.height;

        if (icon != null)
        {
            var iconGo = new GameObject("Icono", typeof(RectTransform), typeof(Image));
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.SetParent(transform, false);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = Vector2.one * h * 0.7f;
            iconRect.anchoredPosition = new Vector2(h * 0.1f, 0f);

            Image img = iconGo.GetComponent<Image>();
            img.sprite = icon;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = label != null ? label.color : Color.black;

            if (label != null) label.rectTransform.offsetMin = new Vector2(h * 0.9f, label.rectTransform.offsetMin.y);
        }
        Refresh(LanguageManager.Current);
    }

    private void OnEnable()
    {
        LanguageManager.OnLanguageChanged += Refresh;
        if (label != null) Refresh(LanguageManager.Current);
    }

    private void OnDisable() => LanguageManager.OnLanguageChanged -= Refresh;

    private void Refresh(Language language)
    {
        if (label != null) label.text = language == Language.English ? "English" : "Español";
    }
}
