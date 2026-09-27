using System.Collections;
using TMPro;
using UnityEngine;

// Gestiona las dos copias del panel (delante / detrás) estilo Tinder:
// la de delante sale deslizada a izquierda o derecha y la de detrás sube a ocupar su sitio.
public class ContractCardStack : MonoBehaviour
{
    [SerializeField] private ContractApproverConfig config;
    [SerializeField] private ContractCardUI cardA;
    [SerializeField] private ContractCardUI cardB;

    [Header("Texto de la petición")]
    [SerializeField] private TMP_Text requestText;

    [Header("Pose de la carta de detrás")]
    [SerializeField] private float backScale = 0.92f;
    [SerializeField] private Vector2 backOffset = new Vector2(0f, -25f);

    [Header("Salida de la carta")]
    [SerializeField] private float offscreenDistance = 1500f;
    [SerializeField] private float swipeRotation = 12f;

    private ContractCardUI front, back;
    private Vector2 basePos;

    public ContractCardUI Front => front;

    private void Awake()
    {
        basePos = cardA.Rect.anchoredPosition;   // la posición de la carta A en el editor es la "buena"
        front = cardA;
        back = cardB;
        PlaceAsBack(back);
        front.Clear();
        front.HideResult();
        front.Group.alpha = 0f;
        front.SetInteractable(false);
        if (requestText != null) requestText.text = "";
    }

    // Sube la carta de detrás, la rellena con la transacción y escribe la petición
    public IEnumerator Present(TransactionData tx)
    {
        ContractCardUI incoming = back;
        ContractCardUI outgoing = front;

        incoming.Fill(tx);
        incoming.HideResult();
        incoming.SetCovered(true);
        incoming.transform.SetAsLastSibling();   // dibujada por encima

        PlaceAsBack(outgoing);                   // la vieja pasa a ser la de detrás (vacía y tapada)
        outgoing.Group.alpha = 0f;

        front = incoming;
        back = outgoing;

        if (requestText != null) requestText.text = tx.requestText;

        Vector2 p0 = front.Rect.anchoredPosition;
        float s0 = front.Rect.localScale.x;
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.01f, config.cardMoveDuration));
            float e = 1f - (1f - t) * (1f - t);  // ease-out
            front.Rect.anchoredPosition = Vector2.Lerp(p0, basePos, e);
            front.Rect.localScale = Vector3.one * Mathf.Lerp(s0, 1f, e);
            back.Group.alpha = e;
            yield return null;
        }
        front.SetInteractable(true);
    }

    /// Muestra el sello, sacude si hay error y lanza la carta fuera
    public IEnumerator Resolve(bool approved, bool correct, bool timeout)
    {
        front.SetInteractable(false);
        front.Reveal();
        front.ShowResult(approved, correct, timeout, config);

        yield return Punch(front.Rect, 0.15f, 0.06f);
        if (!correct) yield return Shake(front.Rect, 0.3f, 14f);
        yield return new WaitForSeconds(config.stampHoldTime);
        yield return SwipeOut(front, approved ? 1 : -1);
    }

    // Quita la carta sin sello (al saltar el tutorial).
    public IEnumerator Dismiss()
    {
        front.SetInteractable(false);
        yield return SwipeOut(front, -1);
    }

    private void PlaceAsBack(ContractCardUI c)
    {
        c.HideResult();
        c.Clear();
        c.SetCovered(true);
        c.Rect.anchoredPosition = basePos + backOffset;
        c.Rect.localScale = Vector3.one * backScale;
        c.Rect.localRotation = Quaternion.identity;
        c.Group.alpha = 1f;
        c.SetInteractable(false);
        c.transform.SetAsFirstSibling();         // dibujada por debajo
    }

    private IEnumerator SwipeOut(ContractCardUI c, int dir)
    {
        Vector2 start = c.Rect.anchoredPosition;
        Vector2 end = start + new Vector2(dir * offscreenDistance, -80f);
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.01f, config.swipeDuration));
            float e = t * t;                      // ease-in
            c.Rect.anchoredPosition = Vector2.Lerp(start, end, e);
            c.Rect.localRotation = Quaternion.Euler(0f, 0f, -dir * swipeRotation * e);
            c.Group.alpha = 1f - e;
            yield return null;
        }
    }

    private static IEnumerator Punch(RectTransform r, float duration, float amount)
    {
        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / duration);
            r.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * amount);
            yield return null;
        }
        r.localScale = Vector3.one;
    }

    private static IEnumerator Shake(RectTransform r, float duration, float strength)
    {
        Vector2 origin = r.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float damper = 1f - t / duration;
            r.anchoredPosition = origin + new Vector2(Mathf.Sin(t * 60f) * strength * damper, 0f);
            yield return null;
        }
        r.anchoredPosition = origin;
    }
}
