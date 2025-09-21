using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;  // El jugador o personaje a seguir
    public Vector3 offset;
    public float smoothSpeed = 0.125f; // Velocidad de suavizado

    void Start()
    {
        offset = new Vector3(0, 0, -10); // Asegura que la cámara esté detrás del jugador en el eje Z
    }
    void Update()
    {
        if (target == null) return;

        // Posición deseada
        Vector3 desiredPosition = target.position + offset;
        Debug.Log("Desired Position: " + desiredPosition.ToString());
        Debug.Log("offset: " + offset.ToString());
        Debug.Log("Current Position: " + transform.position.ToString());

        // Suavizado con Lerp
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Aplicar posición
        transform.position = smoothedPosition;
    }
}