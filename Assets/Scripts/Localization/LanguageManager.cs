using System;
using UnityEngine;

public enum Language { Spanish, English }

/// <summary>
/// Idioma activo del juego. Se guarda en PlayerPrefs, así que se mantiene entre escenas y partidas.
/// Cada JSON con texto tiene su versión en inglés al lado con el sufijo "_en"
/// (dialogues_blockMiner.json / dialogues_blockMiner_en.json) y los scripts eligen con Pick().
/// Si falta la versión en inglés se usa la española.
/// </summary>
public static class LanguageManager
{
    private const string PrefKey = "language";

    // Se dispara al cambiar de idioma para que lo que ya está en pantalla se vuelva a escribir
    public static event Action<Language> OnLanguageChanged;

    private static Language? current;

    public static Language Current
    {
        get
        {
            // Primera vez sin preferencia guardada: el idioma del sistema (español o, si no, inglés)
            if (current == null)
            {
                Language byDefault = Application.systemLanguage == SystemLanguage.Spanish ? Language.Spanish : Language.English;
                current = (Language)PlayerPrefs.GetInt(PrefKey, (int)byDefault);
            }
            return current.Value;
        }
    }

    public static bool IsEnglish => Current == Language.English;

    // Código corto para mostrar en los botones
    public static string Code => IsEnglish ? "EN" : "ES";

    public static void SetLanguage(Language language)
    {
        if (language == Current) return;
        current = language;
        PlayerPrefs.SetInt(PrefKey, (int)language);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke(language);
    }

    public static void Toggle() => SetLanguage(IsEnglish ? Language.Spanish : Language.English);

    public static TextAsset Pick(TextAsset spanish, TextAsset english) => IsEnglish && english != null ? english : spanish;

    public static string Pick(string spanish, string english) => IsEnglish && !string.IsNullOrEmpty(english) ? english : spanish;

    public static T[] Pick<T>(T[] spanish, T[] english) => IsEnglish && english != null ? english : spanish;
}
