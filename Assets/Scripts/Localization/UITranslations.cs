using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Traduce los textos fijos de la interfaz (los escritos en las escenas y prefabs).
/// Al cargar cada escena y al cambiar de idioma recorre todos los textos TextMeshPro y cambia
/// cada línea que esté en la tabla por su versión en el idioma activo. La tabla sirve en los dos
/// sentidos, así que da igual si en la escena el texto se escribió en español o en inglés.
/// Para traducir un texto nuevo de una escena basta con añadir su pareja { español, inglés } a la tabla.
/// Los textos que se escriben desde código usan LanguageManager.Pick(español, inglés).
/// </summary>
public static class UITranslations
{
    // { español, inglés }. Se compara línea a línea (sin espacios al principio ni al final)
    private static readonly string[,] Pairs =
    {
        // Comunes: diálogos, avisos y pantallas de fin
        { "Saltar diálogo", "Skip dialogue" },
        { "Continuar", "Continue" },
        { "Finalizar", "Finish" },
        { "Sugerencia:", "Hint:" },
        { "Pulse cualquier tecla para continuar...", "Press any key to continue..." },

        // Menú de inicio
        { "Jugar", "Play" },
        { "Jugar (sin wallet)", "Play (no wallet)" },
        { "Ayuda", "Help" },
        { "Salir del juego", "Close game" },
        { "Conectar wallet", "Connect wallet" },
        { "Iniciar sesión", "Sign-in" },
        { "Dirección:", "Addr:" },
        { "Saldo:", "Balance:" },

        // Mapa: globos de los stands
        { "<size=70%><color=#94A3B8>Pulsa</color> <color=#FBBF24>Espacio</color> <color=#94A3B8>o</color> <color=#FBBF24>Enter</color></size>",
          "<size=70%><color=#94A3B8>Press</color> <color=#FBBF24>Space</color> <color=#94A3B8>or</color> <color=#FBBF24>Enter</color></size>" },

        // BlockMiner
        { "Validar", "Validate" },

        // PoolTrader
        { "Turno", "Turn" },
        { "Finalizar turno", "End turn" },
        { "Comprar BTC (vender ETH)", "Buy BTC (sell ETH)" },
        { "Vender BTC (comprar ETH)", "Sell BTC (buy ETH)" },
        { "Límite máx.", "Max limit" },
        { "Bolsas -> Monedas", "Bags -> Coins" },
        { "Monedas -> Bolsas", "Coins -> Bags" },
        { "Convertir a bolsas", "Convert to bags" },
        { "Convertir a BTC", "Convert to BTC" },
        { "Convertir a ETH", "Convert to ETH" },
        { "Cambio final", "Final rates" },

        // ContractApprover
        { "Aceptar", "Accept" },
        { "Rechazar", "Reject" },
        { "Petición", "Request" },
        { "Reportar scam", "Report scam" },
        { "Contratos scam", "Scam contracts" },
        { "Tokens scam", "Scam tokens" },
        { "Fees aceptables", "Affordable fees" },
        { "Slippage aceptable", "Affordable slippage" },
        { "1. Info de los tokens", "1. Tokens info" },
        { "2. Contrato/destinatario", "2. Contract/receiver" },
        { "3. Parámetros de la red", "3. Chain parameters" },
        { "Contrato:", "Contract:" },
    };

    private static readonly Dictionary<string, string> toEnglish = new Dictionary<string, string>();
    private static readonly Dictionary<string, string> toSpanish = new Dictionary<string, string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        toEnglish.Clear();
        toSpanish.Clear();
        for (int i = 0; i < Pairs.GetLength(0); i++)
        {
            toEnglish[Pairs[i, 0]] = Pairs[i, 1];
            toSpanish[Pairs[i, 1]] = Pairs[i, 0];
        }
        SceneManager.sceneLoaded += (scene, mode) => TranslateAll();
        LanguageManager.OnLanguageChanged += language => TranslateAll();
    }

    /// <summary>Devuelve el texto en el idioma activo. Las líneas que no están en la tabla se quedan igual.</summary>
    public static string Translate(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        Dictionary<string, string> table = LanguageManager.IsEnglish ? toEnglish : toSpanish;
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (table.TryGetValue(lines[i].Trim(), out string translated)) lines[i] = translated;
        }
        return string.Join("\n", lines);
    }

    // Todos los textos cargados, también los de objetos desactivados (paneles, pantallas de fin...)
    private static void TranslateAll()
    {
        foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string translated = Translate(text.text);
            // Solo se reasigna si cambia: TextMeshPro regenera la malla en cada asignación
            if (translated != text.text) text.text = translated;
        }
    }
}
