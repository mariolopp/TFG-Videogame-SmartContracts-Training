using Cinemachine;
using UnityEngine;

/// <summary>
/// Desplaza la cámara en vertical hacia el stand para que se lea un poco mejor el texto del globo
/// Además acerca ligeramente la cámara (zoom) a medida que el jugador se aproxima al stand.
/// Funciona con cualquier ICameraFocusTarget registrado (stands, estatua...).
/// </summary>
[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CameraStandFocus : MonoBehaviour
{
    private struct Focus
    {
        public float offsetY;
        public float weight;
    }

    [Tooltip("Margen (en unidades) entre el bocadillo de texto y el borde superior/inferior de la pantalla.")]
    [SerializeField] private float screenMargin = 1.2f;
    [Tooltip("Fracción de la distancia vertical al globo que la cámara se adelanta aunque el globo ya se vea (0 = solo lo justo).")]
    [SerializeField, Range(0f, 1f)] private float lookFraction = 0.3f;
    [Tooltip("Proporción del radio de resalte del stand a partir de la cual el desplazamiento es completo.")]
    [SerializeField, Range(0f, 1f)] private float fullEffectAt = 0.6f;
    [Tooltip("Desplazamiento vertical máximo de la cámara.")]
    [SerializeField] private float maxOffset = 4f;
    [Tooltip("Velocidad de la interpolación (más alto = más rápido).")]
    [SerializeField] private float smoothSpeed = 3f;
    [Tooltip("Zoom máximo al estar junto al stand (0.1 = la vista se reduce un 10%).")]
    [SerializeField, Range(0f, 0.5f)] private float maxZoom = 0.1f;

    private CinemachineVirtualCamera vcam;
    private CinemachineFramingTransposer framing;
    private float baseOffsetY;
    private float baseOrthoSize;
    private float currentOffsetY;
    private float currentZoom;

    private void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
        framing = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (framing == null)
        {
            Debug.LogError("CameraStandFocus: la Virtual Camera necesita Body = Framing Transposer.", this);
            enabled = false;
            return;
        }
        baseOffsetY = framing.m_TrackedObjectOffset.y;
        baseOrthoSize = vcam.m_Lens.OrthographicSize;
    }

    private void OnDisable()
    {
        if (framing == null) return;
        currentOffsetY = 0f;
        currentZoom = 0f;
        Apply();
    }

    // En Update: el CinemachineBrain calcula la cámara en LateUpdate y ya usa el valor nuevo
    private void Update()
    {
        Transform target = vcam.Follow;
        if (target == null) return;

        Focus focus = ComputeFocus(target.position);
        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        currentOffsetY = Mathf.Lerp(currentOffsetY, focus.offsetY, t);
        currentZoom = Mathf.Lerp(currentZoom, maxZoom * focus.weight, t);
        Apply();
    }

    private Focus ComputeFocus(Vector2 playerPosition)
    {
        // Objetivo con más influencia: el más metido dentro de su radio de resalte
        ICameraFocusTarget best = null;
        float bestWeight = 0f;

        var targets = CameraFocusTargets.All;
        for (int i = 0; i < targets.Count; i++)
        {
            ICameraFocusTarget target = targets[i];
            if (!target.CanFocus) continue;
            float radius = target.HighlightRadius;
            float distance = Vector2.Distance(playerPosition, target.AccessPosition);
            if (distance >= radius) continue;

            float weight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(radius, radius * fullEffectAt, distance));
            if (weight > bestWeight)
            {
                best = target;
                bestWeight = weight;
            }
        }

        if (best == null) return default;

        float delta = best.FocusPoint.y - playerPosition.y;
        // Con el tamaño que tendrá la vista ya con zoom, para que el bocadillo no se salga al acercarse
        float zoomedSize = baseOrthoSize * (1f - maxZoom * bestWeight);
        float halfHeight = Mathf.Max(0f, zoomedSize - screenMargin);

        // Lo justo para que el bocadillo entre en pantalla con margen...
        float needed = delta > 0f ? Mathf.Max(0f, delta - halfHeight) : Mathf.Min(0f, delta + halfHeight);
        float look = delta * lookFraction;
        float desired = delta > 0f ? Mathf.Max(needed, look) : Mathf.Min(needed, look);

        return new Focus
        {
            offsetY = Mathf.Clamp(desired * bestWeight, -maxOffset, maxOffset),
            weight = bestWeight
        };
    }

    private void Apply()
    {
        Vector3 offset = framing.m_TrackedObjectOffset;
        offset.y = baseOffsetY + currentOffsetY;
        framing.m_TrackedObjectOffset = offset;

        vcam.m_Lens.OrthographicSize = baseOrthoSize * (1f - currentZoom);
    }
}
