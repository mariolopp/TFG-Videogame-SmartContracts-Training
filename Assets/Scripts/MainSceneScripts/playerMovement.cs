using System;
using UnityEngine;

/// <summary>
/// Movimiento top-down del jugador con Rigidbody2D dinámico (sin gravedad), para que
/// los colliders sólidos del escenario (stands, obstáculos) lo frenen.
/// Además del control por teclado admite:
///  - Límite a la zona jugable (clamp contra el rectángulo de un collider).
///  - Bloqueo del control (transiciones, cinemáticas).
///  - Movimiento automático hasta un punto (WalkTo), usado por los stands.
/// </summary>
public class playerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("Límites del mapa")]
    [Tooltip("Collider que delimita la zona jugable (el mismo que usa el CinemachineConfiner2D). Se usa su rectángulo envolvente.")]
    [SerializeField] private Collider2D mapBounds;
    [Tooltip("Margen interior para que el sprite no quede cortado en el borde.")]
    [SerializeField] private Vector2 boundsPadding = new Vector2(0.7f, 0.5f);

    [Header("Movimiento automático")]
    [Tooltip("Distancia a la que se considera alcanzado el destino de WalkTo.")]
    [SerializeField] private float arrivalThreshold = 0.02f;
    [Tooltip("Si algo bloquea el camino durante este tiempo, se da el destino por alcanzado (evita quedarse atascado).")]
    [SerializeField] private float autoWalkStuckTime = 0.4f;

    // Hashes de los parámetros del Animator (evita buscar por string cada frame)
    private static readonly int WalkingFront = Animator.StringToHash("WalkingFront");
    private static readonly int WalkingBack = Animator.StringToHash("WalkingBack");
    private static readonly int WalkingLeft = Animator.StringToHash("WalkingLeft");
    private static readonly int WalkingRight = Animator.StringToHash("WalkingRight");

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movement;

    private bool isAutoWalking;
    private Vector2 autoWalkTarget;
    private Action onAutoWalkArrived;
    private float lastAutoWalkDistance;
    private float stuckTimer;

    /// <summary>Si es false se ignora el teclado. No afecta a WalkTo.</summary>
    public bool ControlEnabled { get; set; } = true;

    /// <summary>true si el jugador se está moviendo con el teclado (no con WalkTo ni quieto).</summary>
    public bool IsMovingByInput => !isAutoWalking && movement != Vector2.zero;

    private void Awake()
    {
        // En Awake (no en Start) para que otros scripts puedan usar Teleport/WalkTo desde su Start
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Un cuerpo Kinematic atraviesa los colliders; Dynamic sin gravedad ni giro choca con ellos
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        if (isAutoWalking)
        {
            movement = (autoWalkTarget - rb.position).normalized;
        }
        else if (ControlEnabled)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");
            // Sin esto, en diagonal se avanza √2 veces más rápido que en recto
            movement = Vector2.ClampMagnitude(movement, 1f);
        }
        else
        {
            movement = Vector2.zero;
        }

        UpdateAnimation(movement);
    }

    private void FixedUpdate()
    {
        float step = moveSpeed * Time.fixedDeltaTime;
        // Tras un choque la física deja velocidad residual: sin esto el jugador seguiría deslizando
        rb.velocity = Vector2.zero;

        if (isAutoWalking)
        {
            // MoveTowards nunca se pasa del destino: la llegada es exacta
            Vector2 next = Vector2.MoveTowards(rb.position, autoWalkTarget, step);
            rb.MovePosition(next);

            float distance = Vector2.Distance(rb.position, autoWalkTarget);
            if (distance <= arrivalThreshold + step)
            {
                FinishAutoWalk();
                return;
            }

            // Si un collider impide avanzar, no se espera para siempre
            stuckTimer = lastAutoWalkDistance - distance < step * 0.1f ? stuckTimer + Time.fixedDeltaTime : 0f;
            lastAutoWalkDistance = distance;
            if (stuckTimer >= autoWalkStuckTime)
                FinishAutoWalk();
        }
        else
        {
            rb.MovePosition(ClampToBounds(rb.position + movement * step));
        }
    }

    /// <summary>Camina sola hasta target (limitado a la zona jugable) y llama a onArrived al llegar.</summary>
    public void WalkTo(Vector2 target, Action onArrived = null)
    {
        autoWalkTarget = ClampToBounds(target);
        onAutoWalkArrived = onArrived;
        lastAutoWalkDistance = float.MaxValue;
        stuckTimer = 0f;
        isAutoWalking = true;
    }

    /// <summary>Coloca al jugador en una posición al instante (p. ej. al volver de un minijuego).</summary>
    public void Teleport(Vector2 position)
    {
        position = ClampToBounds(position);
        rb.position = position;
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    private void FinishAutoWalk()
    {
        isAutoWalking = false;
        movement = Vector2.zero;
        UpdateAnimation(movement);

        Action callback = onAutoWalkArrived;
        onAutoWalkArrived = null;
        callback?.Invoke();
    }

    private Vector2 ClampToBounds(Vector2 position)
    {
        if (mapBounds == null) return position;

        Bounds b = mapBounds.bounds;
        return new Vector2(
            Mathf.Clamp(position.x, b.min.x + boundsPadding.x, b.max.x - boundsPadding.x),
            Mathf.Clamp(position.y, b.min.y + boundsPadding.y, b.max.y - boundsPadding.y));
    }

    /// <summary>
    /// Elige la animación según el eje dominante. En empate (diagonal de teclado) gana el vertical,
    /// igual que en la versión anterior; así el movimiento automático, que rara vez es recto,
    /// muestra la animación lateral cuando avanza sobre todo en horizontal.
    /// </summary>
    private void UpdateAnimation(Vector2 direction)
    {
        bool moving = direction.sqrMagnitude > 0.0001f;
        bool vertical = Mathf.Abs(direction.y) >= Mathf.Abs(direction.x);

        animator.SetBool(WalkingFront, moving && vertical && direction.y < 0f);
        animator.SetBool(WalkingBack, moving && vertical && direction.y > 0f);
        animator.SetBool(WalkingLeft, moving && !vertical && direction.x < 0f);
        animator.SetBool(WalkingRight, moving && !vertical && direction.x > 0f);
    }
}