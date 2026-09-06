using System.Collections;
using TMPro;
using UnityEngine;

public class TurnAnnouncer : MonoBehaviour
{
    [SerializeField] private TMP_Text announcementText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float waitDuration = 1f; // Tiempo totalmente visible
    [SerializeField] private float fadeDuration = 0.5f; // Tiempo en desaparecer

    private Coroutine current;

    private void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    public void Announce(string turnName)
    {
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(AnnounceRoutine(turnName));
    }

    private IEnumerator AnnounceRoutine(string turnName)
    {
        announcementText.text = turnName;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        yield return new WaitForSeconds(waitDuration);
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }
}