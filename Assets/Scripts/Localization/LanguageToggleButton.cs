using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Botón con el icono de idioma que alterna entre español e inglés.
/// No hace falta colocarlo en la escena: CreateNextTo() copia el botón de "Saltar diálogo"
/// (mismo estilo), lo pone a su izquierda y lo enciende/apaga a la vez que él.
/// </summary>
public class LanguageToggleButton : MonoBehaviour
{
    private const string IconPath = "UI/language_icon";   // Assets/Resources/UI/language_icon.png
    private const float Spacing = 15f;                     // separación con el botón de saltar
    private const float LabelSizeRatio = 0.35f;            // tamaño del texto "ES"/"EN" respecto a la altura del botón

    private TMP_Text label;

    public static LanguageToggleButton CreateNextTo(Button skipButton)
    {
        if (skipButton == null) return null;

        GameObject clone = Instantiate(skipButton.gameObject, skipButton.transform.parent);
        clone.name = "Idioma";
        clone.transform.SetSiblingIndex(skipButton.transform.GetSiblingIndex() + 1);

        // Instantiate copia también los listeners del inspector: se quitan para que no salte el diálogo
        Button button = clone.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();

        var toggle = clone.AddComponent<LanguageToggleButton>();
        toggle.Build((RectTransform)skipButton.transform);
        button.onClick.AddListener(toggle.OnClick);

        skipButton.gameObject.AddComponent<LanguageButtonFollower>().target = clone;
        return toggle;
    }

    /// <summary>True si el ratón o el dedo está encima de un botón de idioma (para que el click no avance el diálogo).</summary>
    public static bool IsUnderPointer()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return false;
        var results = new List<RaycastResult>();
        es.RaycastAll(new PointerEventData(es) { position = Input.mousePosition }, results);
        return results.Count > 0 && results[0].gameObject.GetComponentInParent<LanguageToggleButton>() != null;
    }

    // Botón cuadrado-alargado a la izquierda del de saltar: icono a la izquierda y "ES"/"EN" a la derecha
    private void Build(RectTransform skipRect)
    {
        var rect = (RectTransform)transform;
        float h = skipRect.rect.height;
        float w = h * 1.6f;
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        float scale = skipRect.localScale.x;
        float left = skipRect.anchoredPosition.x - skipRect.rect.width * skipRect.pivot.x * scale;
        rect.anchoredPosition = new Vector2(left - Spacing - w * (1f - rect.pivot.x) * scale, skipRect.anchoredPosition.y);

        label = GetComponentInChildren<TMP_Text>(true);
        Sprite icon = LoadIcon();
        float textLeft = 0f;

        if (icon != null)
        {
            var iconGo = new GameObject("Icono", typeof(RectTransform), typeof(Image));
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.SetParent(transform, false);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = Vector2.one * h * 0.6f;
            iconRect.anchoredPosition = new Vector2(h * 0.2f, 0f);

            Image img = iconGo.GetComponent<Image>();
            img.sprite = icon;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = label != null ? label.color : Color.white;
            textLeft = h * 0.8f;
        }

        if (label != null)
        {
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(textLeft, 0f);
            labelRect.offsetMax = new Vector2(-h * 0.1f, 0f);
            label.margin = Vector4.zero;
            label.alignment = TextAlignmentOptions.Center;
            // Tamaño fijo proporcional al botón: con el autoajuste copiado de "Saltar diálogo" salía demasiado grande
            label.enableAutoSizing = false;
            label.fontSize = h * LabelSizeRatio;
        }
        Refresh(LanguageManager.Current);
    }

    public static Sprite LoadIcon()
    {
        Sprite sprite = Resources.Load<Sprite>(IconPath);
        if (sprite != null) return sprite;
        // Por si la imagen no se importó como Sprite
        Texture2D tex = Resources.Load<Texture2D>(IconPath);
        return tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)) : null;
    }

    private void OnEnable()
    {
        LanguageManager.OnLanguageChanged += Refresh;
        if (label != null) Refresh(LanguageManager.Current);
    }

    private void OnDisable() => LanguageManager.OnLanguageChanged -= Refresh;

    private void OnClick()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("select_button");
        LanguageManager.Toggle();
    }

    private void Refresh(Language language)
    {
        if (label != null) label.text = LanguageManager.Code;
    }
}
