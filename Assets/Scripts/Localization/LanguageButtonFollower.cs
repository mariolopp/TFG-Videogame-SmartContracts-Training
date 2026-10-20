using UnityEngine;

/// <summary>Se añade al botón de saltar para que el de idioma aparezca y desaparezca con él.</summary>
public class LanguageButtonFollower : MonoBehaviour
{
    public GameObject target;

    private void OnEnable()
    {
        if (target != null) target.SetActive(true);
    }

    private void OnDisable()
    {
        if (target != null && gameObject.scene.isLoaded) target.SetActive(false);
    }
}
