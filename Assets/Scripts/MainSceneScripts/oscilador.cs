using UnityEngine;
using UnityEngine.EventSystems;

public class oscilador : MonoBehaviour, IPointerClickHandler
{
    public float maxAngle = 15f;       // Ángulo máximo en grados
    public float swingFrequency = 2f;  // Velocidad de oscilación (oscilaciones por segundo)
    public float duration = 5f;        // Duración total del balanceo en segundos

    private bool isSwinging = false;
    private float elapsed = 0f;
    private Quaternion originalRotation;

    void Start()
    {
        originalRotation = transform.rotation;
    }

    void Update()
    {
        if (isSwinging)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;

            if (normalizedTime >= 1f)
            {
                // Fin del balanceo: reset y stop
                isSwinging = false;
                transform.rotation = originalRotation;
                elapsed = 0f;
                return;
            }

            // Amplitud decrece linealmente de maxAngle a 0
            float currentAngle = Mathf.Lerp(maxAngle, 0f, normalizedTime);

            // Ángulo actual con oscilación seno (péndulo)
            float angle = Mathf.Sin(elapsed * swingFrequency * Mathf.PI * 2) * currentAngle;

            // Aplica rotación solo en Z para efecto péndulo
            transform.rotation = originalRotation * Quaternion.Euler(0, 0, angle);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isSwinging)
        {
            isSwinging = true;
            elapsed = 0f;
        }
    }
}
