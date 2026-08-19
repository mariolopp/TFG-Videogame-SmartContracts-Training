using UnityEngine;
using UnityEngine.UI;

public class PoolVisualizer : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PriceManager priceManager; // arrastra aquí el GameObject que tiene PriceManager
    [SerializeField] private Image poolSpriteImage;      // arrastra aquí la Image donde se muestra el sprite del pool

    [Header("Sprites (80 frames, en orden: 1.png, 2.png, ... 80.png)")]
    [SerializeField] private Sprite[] poolFrames;
    [SerializeField] private bool assignedTokenIsA; // true si es el token A, false si es el token B DEBE ASIGNARSE EN EL INSPECTOR

    [Header("Rango de reserva para mapear a frames")]
    [SerializeField] private float minReserve = 0f;
    [SerializeField] private float maxReserve = 50f;

    [Header("Configuración del salto piecewise")]
    [SerializeField] private int totalLogicalSteps = 80; // niveles lógicos totales (1..N)
    [SerializeField] private int breakpoint = 43;         // a partir de aquí cambia el ritmo
    [SerializeField] private int blockSize = 10;          // cada nivel lógico ocupa X frames reales

    private void OnEnable()
    {
        PriceManager.OnTrade += HandleTrade;
    }

    private void OnDisable()
    {
        PriceManager.OnTrade -= HandleTrade;
    }

    private void Start()
    {
        // Pinta el frame inicial acorde al estado actual del pool
        UpdatePoolSprite();
    }

    private void HandleTrade(float amountIn, float amountOut)
    {
        UpdatePoolSprite();
    }

    private void UpdatePoolSprite()
    {
        if (poolSpriteImage == null || poolFrames == null || poolFrames.Length == 0 || priceManager == null)
            return;
        float reserve = 0f;
        if (assignedTokenIsA)
        {
            reserve = priceManager.GetReserveA();
        } else if (!assignedTokenIsA)
        {
            reserve = priceManager.GetReserveB();
        }

        float t = Mathf.InverseLerp(minReserve, maxReserve, reserve);
        int logicalIndex = Mathf.Clamp(Mathf.RoundToInt(t * (totalLogicalSteps - 1)) + 1, 1, totalLogicalSteps);

        int realFrame = LogicalToRealFrame(logicalIndex);

        int frameIndex = Mathf.Clamp(realFrame - 1, 0, poolFrames.Length - 1); // array 0-based

        poolSpriteImage.sprite = poolFrames[frameIndex];
    }

    // Convierte un índice lógico (1..N) al frame real del sprite
    private int LogicalToRealFrame(int logicalIndex)
    {
        if (logicalIndex < breakpoint)
            return logicalIndex; // 1:1 hasta el breakpoint
        // Si se pasa del frame 42 se usan los elementos del array 43 44 45 46 equivalentes a 52 62 72 y 82 respectivamente (la formula devuelve 1 mas porque el clamp de arriba resta 1)
        int clampedNumber = Mathf.Clamp((logicalIndex +3) / 10 + 39, 43, 47);   
        return clampedNumber;
    }
}