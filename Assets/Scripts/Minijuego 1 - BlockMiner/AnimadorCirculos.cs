using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EfectoDibujado : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float velocidadDibujado = 1.0f;

    private Image imagenUI;

    private void Awake()
    {
        imagenUI = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (imagenUI != null)
        {
            StopAllCoroutines();
            StartCoroutine(AnimarCirculo());
        }
    }

    /// Por seguridad, si el objeto se apaga de golpe, nos aseguramos de soltar el candado
    private void OnDisable()
    {
        if (DialogManager.Instance != null)
        {
            DialogManager.Instance.BloquearInteraccion(false);
        }
    }

    private IEnumerator AnimarCirculo()
    {
        // 1. CERRAMOS EL CANDADO EN EL MANAGER
        if (DialogManager.Instance != null)
        {
            DialogManager.Instance.BloquearInteraccion(true);
        }

        float progreso = 0f;
        imagenUI.fillAmount = 0f;

        while (progreso < 1f)
        {
            progreso += Time.deltaTime * (1f / velocidadDibujado);
            imagenUI.fillAmount = progreso;
            yield return null;
        }

        imagenUI.fillAmount = 1f;

        // 2. ABRIMOS EL CANDADO AL TERMINAR
        if (DialogManager.Instance != null)
        {
            DialogManager.Instance.BloquearInteraccion(false);
        }
    }
}