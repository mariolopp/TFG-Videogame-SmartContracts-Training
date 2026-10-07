using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BloqueFX : MonoBehaviour
{
    [Header("Barras")]
    [SerializeField] private BarraProgreso barraBloque;        // Barra llenada con el gas de los bloques
    [SerializeField] private BarraProgreso barraTiempo;
    [SerializeField] private Temporizador temporizador;

    [Header("Aviso de tiempo agotándose")]
    [SerializeField] private float segundosAviso = 3f;         // Empieza a temblar a los 3s
    [SerializeField] private float temblorMinimo = 1.5f;
    [SerializeField] private float temblorMaximo = 5f;

    [Header("Contenedor de barras")]
    [SerializeField] private Transform contenedorBarras;       // Para animaciones de la barra en su conjunto
    [SerializeField] private float escalaLleno = 1.15f;
    [SerializeField] private float pulsoLleno = 0.03f;
    [SerializeField] private float velocidadAgrandar = 8f;
    [SerializeField] private float escalaEpica = 1.45f;
    [SerializeField] private float giroEpico = 12f;
    [SerializeField] private float saltoEpico = 30f;
    [SerializeField] private float duracionEpica = 0.9f;

    [Header("Validar sin completar (rebote leve)")]
    [SerializeField] private float escalaLeve = 1.08f;
    [SerializeField] private float giroLeve = 2f;
    [SerializeField] private float saltoLeve = 8f;
    [SerializeField] private float duracionLeve = 0.5f;

    [Header("Texto flotante")]
    [SerializeField] private TMP_Text textoFlotante;          // Se crea en ejecución, aunque se puede asignar
    [SerializeField] private TMP_FontAsset fuente;
    [SerializeField] private float tamanoFuente = 56f;
    [SerializeField] private float subida = 120f;
    [SerializeField] private float duracionTexto = 1.4f;

    [Header("Bloque completo")]
    [SerializeField] private string mensaje = "¡BLOQUE PERFECTO!";
    [SerializeField] private Color colorDorado = new Color(1f, 0.82f, 0.2f);
    [SerializeField] private float intensidadSacudida = 10f;
    [SerializeField] private float duracionSacudida = 0.35f;
    [SerializeField] private float escalaRebote = 1.5f;
    [SerializeField] private float duracionRebote = 0.4f;
    [SerializeField] private float escalaReboteBarras = 1.12f;
    [SerializeField] private string sfxId = "coin_sound";

    [Header("Bloque perdido")]
    [SerializeField] private string mensajePerdido = "¡BLOQUE PERDIDO!";
    [SerializeField] private Color colorRojo = new Color(1f, 0.25f, 0.25f);
    [SerializeField] private float intensidadSacudidaPerdido = 18f;
    [SerializeField] private float duracionPerdido = 0.6f;
    [SerializeField] private int parpadeosPerdido = 3;
    [SerializeField] private float subidaPerdido = 180f;
    [SerializeField] private float duracionTextoPerdido = 2.2f;
    [Tooltip("Id del sonido en la SoundLibrary. Vacío = sin sonido")]
    [SerializeField] private string sfxIdPerdido = "";

    // Estado original para restaurar si se solapan
    private readonly Dictionary<Transform, Vector3> posicionesOriginales = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Vector3> escalasOriginales = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Image, Color> coloresOriginales = new Dictionary<Image, Color>();
    private readonly Dictionary<(Object, string), Coroutine> rutinas = new Dictionary<(Object, string), Coroutine>();

    // Estado del contenedor
    private Vector3 escalaBaseContenedor;
    private Vector3 posicionBaseContenedor;
    private Quaternion rotacionBaseContenedor;
    private float factorContenedor = 1f;
    private bool animacionEpica = false;

    // Posición de cada barra antes de temblar
    private readonly Dictionary<Transform, Vector3> basesTemblor = new Dictionary<Transform, Vector3>();

    private void Start()
    {
        if (contenedorBarras == null) return;
        escalaBaseContenedor = contenedorBarras.localScale;
        posicionBaseContenedor = contenedorBarras.localPosition;
        rotacionBaseContenedor = contenedorBarras.localRotation;
    }

    private void Update()
    {
        ActualizarContenedor();
        ActualizarTemblor();
    }

    // Mientras la barra está llena, crece y late. Asi el jugador se da cuenta que hay que validarlo
    private void ActualizarContenedor()
    {
        if (contenedorBarras == null || animacionEpica) return;

        bool lleno = barraBloque != null && barraBloque.valorActual >= 0.999f;
        float objetivo = lleno ? escalaLleno + Mathf.Sin(Time.unscaledTime * 6f) * pulsoLleno : 1f;

        factorContenedor = Mathf.Lerp(factorContenedor, objetivo, 1f - Mathf.Exp(-velocidadAgrandar * Time.unscaledDeltaTime));
        contenedorBarras.localScale = escalaBaseContenedor * factorContenedor;
    }

    // Las barras aumentan temblor a menos tiempo queda
    private void ActualizarTemblor()
    {
        float restante = temporizador != null ? temporizador.TiempoRestante : 0f;
        bool avisar = Time.timeScale > 0f && restante > 0f && restante <= segundosAviso;
        if (!avisar)
        {
            DetenerTemblor();
            return;
        }

        float urgencia = segundosAviso > 1f ? 1f - (restante - 1f) / (segundosAviso - 1f) : 1f;
        float intensidad = Mathf.Lerp(temblorMinimo, temblorMaximo, Mathf.Clamp01(urgencia));

        foreach (BarraProgreso b in new[] { barraBloque, barraTiempo })
        {
            if (b == null) continue;
            Transform t = b.transform;
            if (posicionesOriginales.ContainsKey(t)) continue; // hay otra sacudida en curso

            if (!basesTemblor.ContainsKey(t)) basesTemblor[t] = t.localPosition;
            t.localPosition = basesTemblor[t] + (Vector3)(Random.insideUnitCircle * intensidad);
        }
    }

    private void DetenerTemblor()
    {
        foreach (var par in basesTemblor)
            if (par.Key != null) par.Key.localPosition = par.Value;
        basesTemblor.Clear();
    }

    // ---------- Bloque completo ----------

    public void Completo(GameObject snapshot, string nombreRelleno)
    {
        DetenerTemblor(); // Devolver las barras a su sitio antes de otras animaciones
        if (!string.IsNullOrEmpty(sfxId)) AudioManager.Instance?.PlaySFX(sfxId);

        if (contenedorBarras != null) Lanzar(contenedorBarras, "epica", MovimientoEpico(escalaEpica, giroEpico, saltoEpico, duracionEpica));

        foreach (BarraProgreso b in new[] { barraBloque, barraTiempo })
        {
            if (b == null) continue;
            Lanzar(b.transform, "color", Destello(b, colorDorado, duracionSacudida * 2f, 1));
            Lanzar(b.transform, "escala", Rebote(b.transform, escalaReboteBarras, duracionRebote));
        }
        if (barraBloque != null)
        {
            Lanzar(barraBloque.transform, "posicion", Sacudir(barraBloque.transform, intensidadSacudida, duracionSacudida, false));

            string texto = mensaje;
            blockMinerUI ui = FindObjectOfType<blockMinerUI>();
            if (ui != null) texto += $"\n+{Mathf.RoundToInt(ui.BagsPerBlock)} <sprite=\"ETH_bag\" index=0>";
            Lanzar(this, "texto", TextoFlotante(barraBloque.transform, texto, colorDorado, subida, duracionTexto, true));
        }

        if (snapshot != null) StartCoroutine(BloqueDorado(snapshot, nombreRelleno));
    }

    // ---------- Bloque validado sin completar ----------

    public void Validado()
    {
        if (contenedorBarras != null) Lanzar(contenedorBarras, "epica", MovimientoEpico(escalaLeve, giroLeve, saltoLeve, duracionLeve));
    }

    // ---------- Bloque perdido ----------

    public void Perdido(float usdPerdidos)
    {
        DetenerTemblor(); // Devolver las barras a su sitio antes de las otras animaciones
        if (!string.IsNullOrEmpty(sfxIdPerdido)) AudioManager.Instance?.PlaySFX(sfxIdPerdido);

        foreach (BarraProgreso b in new[] { barraBloque, barraTiempo })
        {
            if (b == null) continue;
            Lanzar(b.transform, "color", Destello(b, colorRojo, duracionPerdido, parpadeosPerdido));
            Lanzar(b.transform, "posicion", Sacudir(b.transform, intensidadSacudidaPerdido, duracionPerdido, true));
        }

        Transform ancla = barraBloque != null ? barraBloque.transform : barraTiempo != null ? barraTiempo.transform : null;
        if (ancla != null)
        {
            string texto = mensajePerdido;
            if (usdPerdidos > 0f) texto += $"\n-{Mathf.RoundToInt(usdPerdidos)} <sprite=\"ETH_bag\" index=0>";
            Lanzar(this, "texto", TextoFlotante(ancla, texto, colorRojo, subidaPerdido, duracionTextoPerdido, false));
        }
    }

    // ---------- Animaciones ----------

    private void Lanzar(Object clave, string tipo, IEnumerator rutina)
    {
        var k = (clave, tipo);
        if (rutinas.TryGetValue(k, out Coroutine c) && c != null) StopCoroutine(c);
        rutinas[k] = StartCoroutine(rutina);
    }

    // Se encoje, sale disparado hacia arriba y vuelve a su sitio
    private IEnumerator MovimientoEpico(float escala, float giro, float salto, float duracion)
    {
        animacionEpica = true;
        float inicio = factorContenedor;
        float encogido = 1f - (escala - 1f) * 0.27f; // 0.88 con la escala epica
        float anticipacion = duracion * 0.15f;
        float explosion = duracion * 0.2f;
        float asentar = duracion - anticipacion - explosion;

        // Se encoje
        float t = 0f;
        while (t < anticipacion)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / anticipacion);
            AplicarContenedor(Mathf.Lerp(inicio, encogido, p), 0f, -salto * 0.3f * p);
            yield return null;
        }

        // Crecer y girar
        t = 0f;
        while (t < explosion)
        {
            t += Time.unscaledDeltaTime;
            float p = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / explosion), 3f); // ease out
            AplicarContenedor(Mathf.Lerp(encogido, escala, p), giro * p, Mathf.Lerp(-salto * 0.3f, salto, p));
            yield return null;
        }

        // Volver a su sitio
        t = 0f;
        while (t < asentar)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / asentar);
            float amortiguado = Mathf.Exp(-5f * p) * Mathf.Cos(p * Mathf.PI * 3f);
            AplicarContenedor(1f + (escala - 1f) * amortiguado, giro * amortiguado, salto * amortiguado);
            yield return null;
        }

        AplicarContenedor(1f, 0f, 0f);
        factorContenedor = 1f;
        animacionEpica = false;
    }

    private void AplicarContenedor(float factor, float giro, float altura)
    {
        if (contenedorBarras == null) return;
        contenedorBarras.localScale = escalaBaseContenedor * factor;
        contenedorBarras.localRotation = rotacionBaseContenedor * Quaternion.Euler(0f, 0f, giro);
        contenedorBarras.localPosition = posicionBaseContenedor + Vector3.up * altura;
    }

    private IEnumerator Sacudir(Transform t, float intensidad, float duracion, bool soloHorizontal)
    {
        if (!posicionesOriginales.ContainsKey(t)) posicionesOriginales[t] = t.localPosition;
        Vector3 origen = posicionesOriginales[t];

        float tiempo = 0f;
        while (tiempo < duracion && t != null)
        {
            tiempo += Time.unscaledDeltaTime;
            float fuerza = intensidad * (1f - tiempo / duracion); // se va calmando
            Vector3 offset = soloHorizontal
                ? Vector3.right * Mathf.Sin(tiempo * 60f) * fuerza   // "no" con la cabeza
                : (Vector3)(Random.insideUnitCircle * fuerza);
            t.localPosition = origen + offset;
            yield return null;
        }
        if (t != null) t.localPosition = origen;
        posicionesOriginales.Remove(t);
    }

    private IEnumerator Rebote(Transform t, float escala, float duracion)
    {
        if (!escalasOriginales.ContainsKey(t)) escalasOriginales[t] = t.localScale;
        Vector3 baseEscala = escalasOriginales[t];

        float tiempo = 0f;
        while (tiempo < duracion && t != null)
        {
            tiempo += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(tiempo / duracion);
            t.localScale = baseEscala * (1f + (escala - 1f) * Mathf.Sin(p * Mathf.PI));
            yield return null;
        }
        if (t != null) t.localScale = baseEscala;
        escalasOriginales.Remove(t);
    }

    // Tiñe momentaneamente la barra
    private IEnumerator Destello(BarraProgreso barra, Color color, float duracion, int parpadeos)
    {
        Image[] imagenes = barra.GetComponentsInChildren<Image>(true);
        foreach (Image img in imagenes)
            if (!coloresOriginales.ContainsKey(img)) coloresOriginales[img] = img.color;

        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(tiempo / duracion);
            float intensidad = Mathf.Abs(Mathf.Sin(p * Mathf.PI * parpadeos)) * (1f - p * 0.5f);

            foreach (Image img in imagenes)
            {
                if (img == null) continue;
                Color original = coloresOriginales[img];
                Color objetivo = new Color(color.r, color.g, color.b, original.a);
                img.color = Color.Lerp(original, objetivo, intensidad);
            }
            yield return null;
        }

        foreach (Image img in imagenes)
        {
            if (img == null) continue;
            img.color = coloresOriginales[img];
            coloresOriginales.Remove(img);
        }
    }

    private IEnumerator TextoFlotante(Transform ancla, string mensajeTexto, Color colorTexto, float desplazamiento, float duracion, bool parpadeo)
    {
        TMP_Text texto = textoFlotante != null ? textoFlotante : CrearTexto(ancla);
        if (texto == null) yield break;

        RectTransform rt = texto.rectTransform;
        texto.gameObject.SetActive(true);
        rt.SetAsLastSibling();
        rt.position = ancla.position;
        Vector3 inicio = rt.localPosition;
        texto.text = mensajeTexto;

        Color color = colorTexto;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracion);

            float escala = p < 0.2f
                ? Mathf.Sin(p / 0.2f * Mathf.PI * 0.75f) / Mathf.Sin(Mathf.PI * 0.75f) * 1.3f
                : Mathf.Lerp(1.3f, 1f, Mathf.Clamp01((p - 0.2f) / 0.1f));
            rt.localScale = Vector3.one * escala;

            rt.localPosition = inicio + Vector3.up * (desplazamiento * (1f - (1f - p) * (1f - p)));
            color.a = p < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (p - 0.6f) / 0.4f);

            texto.color = parpadeo
                ? Color.Lerp(color, new Color(1f, 1f, 1f, color.a), (Mathf.Sin(t * 25f) + 1f) * 0.25f)
                : color;

            yield return null;
        }

        texto.gameObject.SetActive(false);
        rt.localPosition = inicio;
    }

    private IEnumerator BloqueDorado(GameObject snapshot, string nombreRelleno)
    {
        // Teñir el relleno del bloque guardado en el historial
        foreach (Image img in snapshot.GetComponentsInChildren<Image>(true))
        {
            if (img.name == nombreRelleno || img.name == nombreRelleno + "(Clone)") img.color = colorDorado;
        }

        Vector3 escalaBase = snapshot.transform.localScale;
        float t = 0f;
        while (t < duracionRebote)
        {
            if (snapshot == null) yield break; // puede destruirse al salir del historial
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracionRebote);
            float rebote = 1f + (escalaRebote - 1f) * Mathf.Sin(p * Mathf.PI) * (1f - p * 0.5f);
            snapshot.transform.localScale = escalaBase * rebote;
            yield return null;
        }
        if (snapshot != null) snapshot.transform.localScale = escalaBase;
    }

    private TMP_Text CrearTexto(Transform ancla)
    {
        Canvas canvas = ancla.GetComponentInParent<Canvas>();
        if (canvas == null) return null;

        GameObject go = new GameObject("TextoBloqueFX", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(canvas.rootCanvas.transform, false);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (fuente != null) tmp.font = fuente;
        tmp.fontSize = tamanoFuente;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        tmp.rectTransform.sizeDelta = new Vector2(800f, 200f);

        textoFlotante = tmp; // se reutiliza en los siguientes bloques
        return tmp;
    }
}
