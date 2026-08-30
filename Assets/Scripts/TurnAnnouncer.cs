using System.Collections;
using TMPro;
using UnityEngine;

public class TurnAnnouncer : MonoBehaviour
{
    [SerializeField] private TMP_Text announcementText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float totalDuration = 1.5f; // visible + fade, empieza a apagarse desde ya

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

        float t = 0f;
        while (t < totalDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / totalDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }
}