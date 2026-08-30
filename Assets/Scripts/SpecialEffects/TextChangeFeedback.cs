using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TextChangeFeedback : MonoBehaviour
{
    [SerializeField] private RectTransform[] objectsToAnimate;
    [SerializeField] private float scaleMultiplier = 1.15f;
    [SerializeField] private float duration = 0.15f;

    private TMP_Text text;
    private string previousText;

    private Dictionary<RectTransform, Coroutine> activeAnimations = new();

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
        previousText = text.text;
    }

    private void Update()
    {
        if (text.text != previousText)
        {
            previousText = text.text;
            PlayFeedback();
        }
    }

    private void PlayFeedback()
    {
        foreach (RectTransform obj in objectsToAnimate)
        {
            if (obj == null)
                continue;

            // Si ya se estaba animando, detenerla
            if (activeAnimations.TryGetValue(obj, out Coroutine coroutine))
            {
                StopCoroutine(coroutine);
            }

            // Volver inmediatamente al tamaño original
            obj.localScale = Vector3.one;

            // Empezar una nueva animación
            Coroutine newCoroutine = StartCoroutine(Pop(obj));
            activeAnimations[obj] = newCoroutine;
        }
    }

    private IEnumerator Pop(RectTransform obj)
    {
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = originalScale * scaleMultiplier;

        float halfDuration = duration / 2f;
        float timer = 0f;

        // CRECER
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;

            float t = timer / halfDuration;

            obj.localScale = Vector3.Lerp(
                originalScale,
                targetScale,
                t
            );

            yield return null;
        }

        timer = 0f;

        // VOLVER
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;

            float t = timer / halfDuration;

            obj.localScale = Vector3.Lerp(
                targetScale,
                originalScale,
                t
            );

            yield return null;
        }

        obj.localScale = originalScale;

        activeAnimations.Remove(obj);
    }
}