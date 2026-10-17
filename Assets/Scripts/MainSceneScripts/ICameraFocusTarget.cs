using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Elemento del mapa hacia el que CameraStandFocus desplaza y acerca la cámara cuando el jugador
/// se aproxima (stands, estatua...). Se registra en CameraFocusTargets al activarse.
/// </summary>
public interface ICameraFocusTarget
{
    /// <summary>Si es false la cámara lo ignora (stand bloqueado, cinemática en curso...).</summary>
    bool CanFocus { get; }
    /// <summary>Distancia desde AccessPosition a la que empieza el efecto.</summary>
    float HighlightRadius { get; }
    Vector2 AccessPosition { get; }
    /// <summary>Punto que la cámara intenta dejar a la vista (globo de un stand, centro de la estatua...).</summary>
    Vector2 FocusPoint { get; }
}

/// <summary>Registro de objetivos activos: la cámara los recorre sin buscar en la escena cada frame.</summary>
public static class CameraFocusTargets
{
    private static readonly List<ICameraFocusTarget> targets = new List<ICameraFocusTarget>();
    public static IReadOnlyList<ICameraFocusTarget> All => targets;

    public static void Register(ICameraFocusTarget target)
    {
        if (!targets.Contains(target)) targets.Add(target);
    }

    public static void Unregister(ICameraFocusTarget target) => targets.Remove(target);
}
