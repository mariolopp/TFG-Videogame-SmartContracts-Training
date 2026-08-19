using System.Collections;
using UnityEngine;

public class FadeInOnEnable : MonoBehaviour
{
    [SerializeField] private CanvasGroup grupo;
    [SerializeField] private float duracion = 0.5f;

    private void OnEnable()
    {
        if (grupo == null) grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            grupo.alpha = Mathf.Clamp01(tiempo / duracion);
            yield return null;
        }
        grupo.alpha = 1f;
    }
}