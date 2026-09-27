using TMPro;
using UnityEngine;

// Va en el prefab de cada fila
public class ReferenceRowUI : MonoBehaviour
{
    [SerializeField] private TMP_Text mainText;       // ej. la dirección
    [SerializeField] private TMP_Text secondaryText;  // ej. "Drainer FakeAirdrop" (opcional)
    [SerializeField] private GameObject newTag;       // etiqueta "NUEVO" (opcional)

    public void Set(string main, string secondary, bool isNew)
    {
        if (mainText != null) mainText.text = main;
        if (secondaryText != null)
        {
            secondaryText.gameObject.SetActive(!string.IsNullOrEmpty(secondary));
            secondaryText.text = secondary;
        }
        if (newTag != null) newTag.SetActive(isNew);
    }
}
