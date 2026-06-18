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
        // 1. EVALUAR DIRECCIÓN: FRENTE (Hacia abajo)
        if (movement.y < -0.01f)
        {
            animator.SetBool("WalkingFront", true);
            animator.SetBool("WalkingLeft", false); // Apagamos la otra por seguridad
            animator.SetBool("WalkingRight", false);
        }
        // 2. EVALUAR DIRECCIÓN: IZQUIERDA
        else if (movement.x < -0.01f)
        {
            animator.SetBool("WalkingLeft", true);
            animator.SetBool("WalkingFront", false); // Apagamos la otra por seguridad
            animator.SetBool("WalkingRight", false);
        }
        // 3. EVALUAR DIRECCIÓN: DERECHA
        else if (movement.x > 0.01f)
        {
            animator.SetBool("WalkingLeft", false);
            animator.SetBool("WalkingFront", false); // Apagamos la otra por seguridad
            animator.SetBool("WalkingRight", true);
        }
        // 3. SI ESTÁ QUIETO (No pulsa ni abajo ni izquierda)
        else
        {
            animator.SetBool("WalkingFront", false);
            animator.SetBool("WalkingLeft", false);
            animator.SetBool("WalkingRight", false);
        }

        
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}
