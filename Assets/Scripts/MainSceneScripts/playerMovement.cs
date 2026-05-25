using UnityEngine;

public class playerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator animator;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Actualizar animaciones
        // 'movement.y' vale -1 al ir hacia abajo (Frente)
        if (movement.y < -0.1f)
        {
            animator.SetBool("AndandoFrente", true);
        }
        else
        {
            animator.SetBool("AndandoFrente", false);
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}
