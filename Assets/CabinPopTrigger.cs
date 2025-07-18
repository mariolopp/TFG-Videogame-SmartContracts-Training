using UnityEngine;

public class CabinPopTrigger : MonoBehaviour
{
    public GameObject popupUI;  // Panel que aparece

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            popupUI.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            popupUI.SetActive(false);
        }
    }
}
