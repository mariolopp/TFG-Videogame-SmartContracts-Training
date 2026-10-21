using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ventana del botón Ayuda: imagen con los pasos para conectar MetaMask del móvil al juego.
/// Las imágenes se generan con Tools/HelpGuide/build_help.py y están en Resources/Help.
/// Se pinta en tres capas: fondo (help_es/en) -> capturas opcionales -> marcos de los dispositivos (help_es/en_frames).
/// Para poner capturas basta con dejar paso1 ... paso5 en Resources/Help/Capturas;
/// si falta alguna se ve la ilustración que ya trae la imagen.
/// </summary>
public class HelpPanel : MonoBehaviour
{
    private const string Folder = "Help/";
    private const float FadeDuration = 0.2f;
    private const float InputDelay = 0.3f; // para que el mismo clic que la abre no la cierre

    // Formato de help_slots.json: rect de cada pantalla en píxeles de la imagen, con origen arriba a la izquierda
    [Serializable] private class SlotData { public float width, height; public Slot[] slots; }
    [Serializable] private class Slot { public string name; public float x, y, w, h; }

    private CanvasGroup group;
    private float openedAt;
    private bool closing;

    public static void Show()
    {
        if (FindObjectOfType<HelpPanel>() != null) return;

        var panel = new GameObject("HelpPanel").AddComponent<HelpPanel>();
        if (!panel.Build()) Destroy(panel.gameObject);
    }

    private bool Build()
    {
        string lang = LanguageManager.IsEnglish ? "en" : "es";
        Texture2D background = Load("help_" + lang, "help_es");
        if (background == null)
        {
            Debug.LogWarning("[HelpPanel] No se encuentra Resources/Help/help_es. Genera las imágenes con Tools/HelpGuide/build_help.py");
            return false;
        }

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        // Fondo oscuro que tapa el menú y bloquea sus botones mientras la ayuda está abierta
        var dim = CreateChild<Image>("Dim", transform);
        Stretch(dim.rectTransform);
        dim.color = new Color(0.02f, 0.04f, 0.08f, 0.85f);

        // Zona con un pequeño margen donde la imagen se encaja sin deformarse
        var area = CreateChild<RectTransform>("Area", transform);
        area.anchorMin = new Vector2(0.02f, 0.02f);
        area.anchorMax = new Vector2(0.98f, 0.98f);
        area.offsetMin = area.offsetMax = Vector2.zero;

        var guide = CreateChild<RawImage>("Guide", area);
        Stretch(guide.rectTransform);
        guide.texture = background;
        guide.raycastTarget = false;
        var fitter = guide.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = (float)background.width / background.height;

        var json = Resources.Load<TextAsset>(Folder + "help_slots");
        if (json != null)
        {
            var data = JsonUtility.FromJson<SlotData>(json.text);
            foreach (var slot in data.slots) AddScreenshot(guide.rectTransform, slot, data.width, data.height);
        }

        // Los marcos van encima de las capturas para taparles las esquinas
        Texture2D frames = Load("help_" + lang + "_frames", "help_es_frames");
        if (frames != null)
        {
            var overlay = CreateChild<RawImage>("Frames", guide.rectTransform);
            Stretch(overlay.rectTransform);
            overlay.texture = frames;
            overlay.raycastTarget = false;
        }

        openedAt = Time.unscaledTime;
        StartCoroutine(Fade(0f, 1f, null));
        return true;
    }

    private void Update()
    {
        if (closing || Time.unscaledTime - openedAt < InputDelay) return;
        if (Input.anyKeyDown) Close(); // cualquier tecla o clic
    }

    private void Close()
    {
        closing = true;
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("select_button");
        StartCoroutine(Fade(1f, 0f, () => Destroy(gameObject)));
    }

    private static void AddScreenshot(RectTransform parent, Slot slot, float width, float height)
    {
        var tex = Resources.Load<Texture2D>(Folder + "Capturas/" + slot.name);
        if (tex == null) return; // sin captura se queda la ilustración

        var img = CreateChild<RawImage>(slot.name, parent);
        img.raycastTarget = false;
        img.texture = tex;
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(slot.x / width, 1f - (slot.y + slot.h) / height);
        rt.anchorMax = new Vector2((slot.x + slot.w) / width, 1f - slot.y / height);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Recorta la captura desde el centro para llenar la pantalla sin deformarla
        float slotAspect = slot.w / slot.h;
        float texAspect = (float)tex.width / tex.height;
        img.uvRect = texAspect > slotAspect
            ? new Rect((1f - slotAspect / texAspect) / 2f, 0f, slotAspect / texAspect, 1f)
            : new Rect(0f, (1f - texAspect / slotAspect) / 2f, 1f, texAspect / slotAspect);
    }

    private IEnumerator Fade(float from, float to, Action onDone)
    {
        float t = 0f;
        while (t < FadeDuration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / FadeDuration);
            yield return null;
        }
        group.alpha = to;
        onDone?.Invoke();
    }

    private static Texture2D Load(string name, string fallback)
    {
        var tex = Resources.Load<Texture2D>(Folder + name);
        return tex != null ? tex : Resources.Load<Texture2D>(Folder + fallback);
    }

    private static T CreateChild<T>(string name, Transform parent) where T : Component
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var existing = go.GetComponent<T>(); // RectTransform ya viene con el GameObject
        return existing != null ? existing : go.AddComponent<T>();
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
