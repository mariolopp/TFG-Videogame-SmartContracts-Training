using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class blockMinerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI bloquesRestantes;
    [SerializeField] private TextMeshProUGUI tempUSD; // USD que se obtendrán al validar
    private AssetsManager assets;
    public int bRestantes = 10;  // El décimo boque es el primero
    [SerializeField] public Temporizador temporizador;
    [SerializeField] private GameObject panelEndBlockMiner; // Panel de fin de juego
    [SerializeField] private Button endScreenButton; // Botón para cerrar el panel de fin de juego
    public Validar Validar;

    [Header("UI Fin de partida")]
    [SerializeField] private TMP_Text endGameBagsText;        // Bolsas ganadas con las fees
    [SerializeField] private TMP_Text endGameTimeText;        // Segundos ahorrados
    [SerializeField] private TMP_Text endGameTimeBagsText;    // Bolsas que otorga el tiempo ahorrado
    [SerializeField] private TMP_Text endGameBlocksText;      // Bloques validados completos
    [SerializeField] private TMP_Text endGameBlocksBagsText;  // Bolsas que otorgan los bloques completos
    [SerializeField] private TMP_Text endGameScoreText;       // Puntuación global (bolsa grande)

    [Header("Puntuación final")]
    [SerializeField] private float bagsPerSecond = 0.1f;  // Bolsas extra por cada segundo ahorrado
    [SerializeField] private float bagsPerBlock = 10f;    // Bolsas extra por cada bloque completo
    public float BagsPerBlock => bagsPerBlock;

    void Start()
    {
        // Restaurar el tiempo al entrar a la escena
        Time.timeScale = 1f;
        //temporizador = FindObjectOfType<Temporizador>();
        assets = FindObjectOfType<AssetsManager>();
        //bloquesRestantes = transform.Find("BloquesRestantesText").GetComponent<TextMeshProUGUI>();
        assets.OnAssetsChanged += ActualizarMinerUI;
        //temporizador.OnTiempoCambiado += ActualizarMinerUI;
        Validar = FindObjectOfType<Validar>();
        Validar.OnValidar += DisminuirBloquesUI;
        DisminuirBloquesUI();
        //ActualizarUI(); // inicializar
    }

    private void OnDestroy()
    {
        assets.OnAssetsChanged -= ActualizarMinerUI;
        //temporizador.OnTiempoCambiado -= ActualizarMinerUI;
        if (Validar != null) Validar.OnValidar -= DisminuirBloquesUI;
    }

    private void ActualizarMinerUI()
    {
        tempUSD.text = "+" + assets.t_usd.ToString() + " <sprite=\"ETH_bag\" index=0>"; // Mostrar el valor USD temporal
    }
    private void DisminuirBloquesUI()
    {
        bRestantes--;
        bloquesRestantes.text = bRestantes.ToString() + "";
        // COMPROBACIÓN DE GAME OVER
        if (bRestantes <= 0)
        {
            TerminarMinijuego();
        }
    }
    private void TerminarMinijuego()
    {
        temporizador.StopAllCoroutines(); // Detener el temporizador
        Validar.boton.interactable = false; // Desactivar el botón de validar
        Time.timeScale = 0f; // Detener el tiempo del juego

        // Desactivar todos los botones de la ruleta
        RuletaLenta ruleta = FindObjectOfType<RuletaLenta>();
        if (ruleta != null && ruleta.botones != null)
        {
            foreach (Transform t in ruleta.botones)
            {
                // Desactivar el botón de la UI
                Button b = t.GetComponent<Button>();
                if (b != null)
                {
                    b.interactable = false;
                }

                // Desactivar su script "MantenerDerecha" para que no intente autoregenerarse
                MantenerDerecha scriptMantener = t.GetComponent<MantenerDerecha>();
                if (scriptMantener != null)
                {
                    scriptMantener.enabled = false;
                }
            }
            ruleta.enabled = false;
        }

        // Estadísticas de la partida
        int bolsasGanadas = Mathf.RoundToInt(Validar.usdGanados); // USD de las fees de los bloques validados
        int segundosAhorrados = Mathf.RoundToInt(Validar.segundosAhorrados);
        int bloquesCompletos = Validar.contadorCompletos;

        int bolsasTiempo = Mathf.RoundToInt(segundosAhorrados * bagsPerSecond);
        int bolsasBloques = Mathf.RoundToInt(bloquesCompletos * bagsPerBlock);
        int puntuacionTotal = bolsasGanadas + bolsasTiempo + bolsasBloques;
        Debug.Log($"[BlockMiner] USD: {bolsasGanadas} | Segundos ahorrados: {Validar.segundosAhorrados} (+{bolsasTiempo}) | Bloques completos: {bloquesCompletos} (+{bolsasBloques}) | Total: {puntuacionTotal}");

        assets.AddUSD(bolsasTiempo + bolsasBloques); // Las bolsas ganadas ya se sumaron al validar cada bloque

        if(panelEndBlockMiner != null)
        {
            panelEndBlockMiner.SetActive(true); // Mostrar el panel de fin de juego

            // Orden: bolsas fees -> tiempo -> bolsas tiempo -> bloques -> bolsas bloques -> puntuación global
            if (endGameBagsText != null)
                StartCoroutine(AnimateNumber(endGameBagsText, 0f, bolsasGanadas, 1.5f));
            if (endGameTimeText != null)
                StartCoroutine(AnimateNumber(endGameTimeText, 0f, segundosAhorrados, 1.5f, 1.75f));
            if (endGameTimeBagsText != null)
                StartCoroutine(AnimateNumber(endGameTimeBagsText, 0f, bolsasTiempo, 1f, 3.5f));
            if (endGameBlocksText != null)
                StartCoroutine(AnimateNumber(endGameBlocksText, 0f, bloquesCompletos, 1f, 4.75f));
            if (endGameBlocksBagsText != null)
                StartCoroutine(AnimateNumber(endGameBlocksBagsText, 0f, bolsasBloques, 1f, 6f));
            if (endGameScoreText != null)
                StartCoroutine(AnimateNumber(endGameScoreText, 0f, puntuacionTotal, 3f, 7.25f));
        }

        if (endScreenButton != null)
        {
            endScreenButton.onClick.AddListener(() => SceneManager.LoadScene("PoolTrader"));
        }

    }

    // Usa tiempo no escalado porque Time.timeScale = 0 al terminar el minijuego
    private IEnumerator AnimateNumber(TMP_Text text, float startValue, float targetValue, float duration, float wait = 0f, string prefix = "x", string suffix = "")
    {
        text.text = $"{prefix}{Mathf.RoundToInt(startValue)}{suffix}";

        yield return new WaitForSecondsRealtime(wait);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float currentValue = Mathf.Lerp(startValue, targetValue, t);

            text.text = $"{prefix}{Mathf.RoundToInt(currentValue)}{suffix}";

            yield return null;
        }

        text.text = $"{prefix}{Mathf.RoundToInt(targetValue)}{suffix}";
    }
}
