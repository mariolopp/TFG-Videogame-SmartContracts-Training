using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ventana de selección de idioma que se abre desde el botón de idioma del menú de inicio.
/// Se construye por código la primera vez, copiando el botón que se le pasa para mantener
/// el mismo estilo, así que no hay que montarla en la escena.
/// </summary>
public class LanguageMenu : MonoBehaviour
{
    private static LanguageMenu instance;

    private TMP_Text title, spanishLabel, englishLabel, closeLabel;

    public static void Open(Button template)
    {
        if (instance == null) instance = Build(template);
        if (instance == null) return;
        instance.gameObject.SetActive(true);
        instance.transform.SetAsLastSibling();   // por encima del resto del menú
    }

    public void Close() => gameObject.SetActive(false);

    private static LanguageMenu Build(Button template)
    {
        Canvas canvas = template != null ? template.GetComponentInParent<Canvas>() : null;
        if (canvas == null) return null;

        // Fondo oscuro a pantalla completa que tapa (y bloquea) el menú
        var panel = new GameObject("MenuIdioma", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)panel.transform;
        rect.SetParent(canvas.rootCanvas.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);
        // Canvas propio por encima de todo: el botón de idioma está en otro canvas de la escena
        var overlay = panel.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = 100;
        panel.AddComponent<GraphicRaycaster>();

        var menu = panel.AddComponent<LanguageMenu>();
        Vector2 size = ((RectTransform)template.transform).rect.size;
        float step = size.y * 1.4f;

        menu.title = CreateTitle(template, rect, new Vector2(0f, step * 1.5f), size);
        menu.spanishLabel = CreateButton(template, rect, new Vector2(0f, step * 0.5f), size, () => LanguageManager.SetLanguage(Language.Spanish));
        menu.englishLabel = CreateButton(template, rect, new Vector2(0f, -step * 0.5f), size, () => LanguageManager.SetLanguage(Language.English));
        menu.closeLabel = CreateButton(template, rect, new Vector2(0f, -step * 1.5f), size, menu.Close);
        menu.Refresh(LanguageManager.Current);
        return menu;
    }

    private static TMP_Text CreateTitle(Button template, RectTransform parent, Vector2 pos, Vector2 size)
    {
        TMP_Text source = template.GetComponentInChildren<TMP_Text>(true);
        TMP_Text label = Instantiate(source, parent);
        Place(label.rectTransform, pos, new Vector2(size.x * 2f, size.y));
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        return label;
    }

    private static TMP_Text CreateButton(Button template, RectTransform parent, Vector2 pos, Vector2 size, Action onClick)
    {
        Button button = Instantiate(template, parent);
        Place((RectTransform)button.transform, pos, size);
        button.onClick = new Button.ButtonClickedEvent();   // sin los listeners del botón original
        button.onClick.AddListener(() =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("select_button");
            onClick();
        });
        return button.GetComponentInChildren<TMP_Text>(true);
    }

    private static void Place(RectTransform rect, Vector2 pos, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
    }

    private void OnEnable()
    {
        LanguageManager.OnLanguageChanged += Refresh;
        if (title != null) Refresh(LanguageManager.Current);
    }

    private void OnDisable() => LanguageManager.OnLanguageChanged -= Refresh;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    private void Refresh(Language language)
    {
        title.text = LanguageManager.Pick("Idioma", "Language");
        closeLabel.text = LanguageManager.Pick("Volver", "Back");
        spanishLabel.text = "Español";
        englishLabel.text = "English";
        // El idioma activo se marca en negrita y subrayado
        spanishLabel.fontStyle = language == Language.Spanish ? FontStyles.Bold | FontStyles.Underline : FontStyles.Normal;
        englishLabel.fontStyle = language == Language.English ? FontStyles.Bold | FontStyles.Underline : FontStyles.Normal;
    }
}
