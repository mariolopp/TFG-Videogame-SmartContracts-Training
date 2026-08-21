using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MarketActorAnimator : MonoBehaviour
{
    [Tooltip("Prefab con RectTransform + Image + CanvasGroup (la silueta)")]
    [SerializeField] private GameObject actorPrefab;
    [SerializeField] private RectTransform actorsParent;
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private RectTransform poolPoint;

    public IEnumerator RunActor(Color tint, float walkDuration, float actionDelay, Action onArriveAtPool)
    {
        GameObject actor = Instantiate(actorPrefab, actorsParent);
        RectTransform rt = actor.GetComponent<RectTransform>();
        CanvasGroup cg = actor.GetComponent<CanvasGroup>();
        Image img = actor.GetComponent<Image>();
        if (img != null) img.color = tint;
        if (cg != null) cg.alpha = 0f;
        rt.position = spawnPoint.position;

        yield return Move(rt, cg, spawnPoint.position, poolPoint.position, walkDuration, fadeIn: true);

        onArriveAtPool?.Invoke();

        if (actionDelay > 0f) yield return new WaitForSeconds(actionDelay);

        yield return Move(rt, cg, poolPoint.position, spawnPoint.position, walkDuration, fadeIn: false);

        Destroy(actor);
    }

    private IEnumerator Move(RectTransform rt, CanvasGroup cg, Vector3 from, Vector3 to, float duration, bool fadeIn)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            rt.position = Vector3.Lerp(from, to, p);
            if (cg != null) cg.alpha = fadeIn ? Mathf.Lerp(0f, 1f, p) : Mathf.Lerp(1f, 0f, p);
            yield return null;
        }
        rt.position = to;
        if (cg != null) cg.alpha = fadeIn ? 1f : 0f;
    }
}