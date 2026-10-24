using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

/// <summary>
/// Camino guía hacia el siguiente minijuego por jugar (ver GameProgress).
/// La 1º vez que se termina un minijuego, al volver al mapa se dibuja un rastro
/// de puntos desde su stand hasta el del siguiente, con la cámara siguiendo el trazo. Al llegar,
/// el stand de destino pasa de gris a color.
/// La ruta se calcula sobre el Tilemap de caminos, sigue funcionando si se cambia el mapa
/// </summary>
public class PathGuide : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Tilemap de los caminos. La ruta va por el centro de los caminos de 2 casillas de ancho.")]
    [SerializeField] private Tilemap pathTilemap;
    [Tooltip("Opcional: si se deja vacío se busca en la escena.")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;

    [Header("Rastro")]
    [SerializeField] private Color dotColor = new Color(1f, 0.92f, 0.5f, 0.9f);
    [Tooltip("Diámetro de cada punto, en unidades.")]
    [SerializeField] private float dotSize = 0.3f;
    [Tooltip("Distancia entre puntos, en unidades.")]
    [SerializeField] private float dotSpacing = 0.6f;
    [Tooltip("Opcional: material de los puntos (p. ej. Sprite-Unlit-Default para que no les afecte la luz).")]
    [SerializeField] private Material dotMaterial;
    [Tooltip("Si es true, el rastro se sigue viendo (ya dibujado, sin animación) hasta que se juegue el minijuego de destino.")]
    [SerializeField] private bool keepUntilPlayed = true;

    [Header("Animación")]
    [Tooltip("Espera tras cargar la escena antes de empezar a dibujar.")]
    [SerializeField] private float startDelay = 0.8f;
    [Tooltip("Velocidad del trazo, en unidades por segundo.")]
    [SerializeField] private float drawSpeed = 9f;
    [SerializeField] private float dotPopDuration = 0.25f;
    [Tooltip("Pausa sobre el stand desbloqueado antes de devolver la cámara al jugador.")]
    [SerializeField] private float holdOnTarget = 1.2f;
    [Tooltip("Tiempo que se da a la cámara para volver al jugador antes de devolverle el control.")]
    [SerializeField] private float cameraReturnTime = 0.8f;
    [Tooltip("Id del efecto que suena en bucle mientras se dibuja (vacío = sin sonido).")]
    [SerializeField] private string drawSfxId = "scoreCounterBucle";

    [Header("Onda de brillo")]
    [Tooltip("Velocidad a la que los destellos recorren el rastro hacia el destino (0 = sin destellos).")]
    [SerializeField] private float pulseSpeed = 5f;
    [Tooltip("Distancia entre destellos, en unidades.")]
    [SerializeField] private float pulseSpacing = 6f;
    [Tooltip("Cuánto crece un punto al pasar el destello.")]
    [SerializeField] private float pulseScale = 0.5f;

    // Los puntos comparten capa con el jugador pero un orden por encima de los caminos. Con la
    // ordenación por eje Y del Renderer 2D, colocar su SortingGroup muy arriba hace que queden
    // siempre detrás del jugador y de los stands, y por delante del suelo
    private const float SortingGroupY = 10000f;
    private const int DotTextureSize = 32;

    private static readonly Vector3Int[] Directions =
        { Vector3Int.right, Vector3Int.left, Vector3Int.up, Vector3Int.down };

    private readonly List<Vector2> route = new List<Vector2>();
    private float routeLength;

    private Transform[] dots;
    private float[] dotDistances;   // posición de cada punto a lo largo de la ruta
    private float[] dotSpawnTimes;  // float.MaxValue = aún no dibujado

    private IEnumerator Start()
    {
        // Se consume siempre, aunque luego no haya nada que dibujar, para que no se quede pendiente
        bool reveal = GameProgress.ConsumeNewUnlock();
        if (!GameProgress.TryGetGuidePath(out string fromScene, out string toScene)) yield break;
        if (!reveal && !keepUntilPlayed) yield break;

        StandInteractable from = StandInteractable.Find(fromScene);
        StandInteractable to = StandInteractable.Find(toScene);
        if (pathTilemap == null || from == null || to == null)
        {
            Debug.LogWarning($"PathGuide: falta el Tilemap de caminos o el stand de '{fromScene}' / '{toScene}'.", this);
            yield break;
        }

        if (!BuildRoute(from.AccessPosition, to.AccessPosition))
        {
            Debug.LogWarning($"PathGuide: no hay camino en el Tilemap entre '{fromScene}' y '{toScene}'.", this);
            yield break;
        }

        CreateDots();

        // Si la estatua acaba de crecer, primero se enseña eso y después el camino
        while (StatueBuilder.IsCinematicPlaying) yield return null;

        if (reveal) yield return RevealRoutine(to);
        else SpawnDotsUpTo(routeLength, Time.time - dotPopDuration); // ya dibujado: sin "pop"
    }

    private IEnumerator RevealRoutine(StandInteractable target)
    {
        StandInteractor interactor = FindObjectOfType<StandInteractor>();
        playerMovement movement = interactor != null ? interactor.GetComponent<playerMovement>() : null;
        CinemachineVirtualCamera vcam = virtualCamera != null ? virtualCamera : FindObjectOfType<CinemachineVirtualCamera>();

        // El destino ya está desbloqueado en el progreso: se vuelve a poner gris para desbloquearlo al llegar el trazo
        target.SetLocked(true);
        SetPlayerControl(interactor, movement, false);

        yield return new WaitForSeconds(startDelay);

        // La cámara sigue la punta del trazo
        Transform tip = new GameObject("PathTip").transform;
        tip.SetParent(transform, false);
        tip.position = route[0];
        Transform previousFollow = vcam != null ? vcam.Follow : null;
        if (vcam != null) vcam.Follow = tip;

        AudioSource drawLoop = !string.IsNullOrEmpty(drawSfxId) && AudioManager.Instance != null
            ? AudioManager.Instance.PlayLoopSFX(drawSfxId, this) : null;

        float drawn = 0f;
        while (drawn < routeLength)
        {
            drawn = Mathf.Min(routeLength, drawn + drawSpeed * Time.deltaTime);
            tip.position = PointAt(drawn);
            SpawnDotsUpTo(drawn, Time.time);
            yield return null;
        }
        if (AudioManager.Instance != null) AudioManager.Instance.StopLoopSFX(drawLoop);

        target.SetLocked(false, animate: true);
        yield return new WaitForSeconds(holdOnTarget);

        if (vcam != null) vcam.Follow = previousFollow;
        yield return new WaitForSeconds(cameraReturnTime);

        SetPlayerControl(interactor, movement, true);
        Destroy(tip.gameObject);
    }

    private static void SetPlayerControl(StandInteractor interactor, playerMovement movement, bool enabled)
    {
        if (interactor != null) interactor.InteractionEnabled = enabled;
        if (movement != null) movement.ControlEnabled = enabled;
    }

    private void Update()
    {
        if (dots == null) return;

        float now = Time.time;
        for (int i = 0; i < dots.Length; i++)
        {
            if (dotSpawnTimes[i] == float.MaxValue) continue;

            float appear = EaseOutBack(Mathf.Clamp01((now - dotSpawnTimes[i]) / dotPopDuration));

            // Destellos que avanzan hacia el destino: indican hacia dónde ir
            float pulse = 0f;
            if (pulseSpeed > 0f)
            {
                float phase = (dotDistances[i] - now * pulseSpeed) / pulseSpacing * 2f * Mathf.PI;
                pulse = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(phase), 6f); // potencia alta: destellos cortos
            }

            dots[i].localScale = Vector3.one * (dotSize * appear * (1f + pulseScale * pulse));
        }
    }

    // ---------- Ruta sobre el Tilemap ----------

    /// <summary>
    /// Busca la ruta más corta por los caminos (BFS). Cada nodo es la esquina común de un bloque de 2x2
    /// casillas de camino: en caminos de 2 casillas de ancho cae justo en el centro, así el rastro va centrado.
    /// </summary>
    private bool BuildRoute(Vector2 from, Vector2 to)
    {
        route.Clear();
        if (!TryFindNearestNode(from, out Vector3Int start) || !TryFindNearestNode(to, out Vector3Int goal))
            return false;

        var previous = new Dictionary<Vector3Int, Vector3Int> { [start] = start };
        var queue = new Queue<Vector3Int>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            Vector3Int node = queue.Dequeue();
            if (node == goal) break;

            foreach (Vector3Int direction in Directions)
            {
                Vector3Int next = node + direction;
                if (previous.ContainsKey(next) || !IsNode(next)) continue;
                previous[next] = node;
                queue.Enqueue(next);
            }
        }
        if (!previous.ContainsKey(goal)) return false;

        var nodes = new List<Vector3Int>();
        for (Vector3Int node = goal; node != start; node = previous[node]) nodes.Add(node);
        nodes.Add(start);
        nodes.Reverse();

        // Solo los extremos y las esquinas: los tramos rectos se rellenan al repartir los puntos
        for (int i = 0; i < nodes.Count; i++)
        {
            bool isCorner = i == 0 || i == nodes.Count - 1 || nodes[i] - nodes[i - 1] != nodes[i + 1] - nodes[i];
            if (isCorner) route.Add(NodeToWorld(nodes[i]));
        }
        route.Add(to); // último tramo hasta delante del stand

        routeLength = 0f;
        for (int i = 1; i < route.Count; i++) routeLength += Vector2.Distance(route[i - 1], route[i]);
        return true;
    }

    private bool IsNode(Vector3Int cell)
    {
        return pathTilemap.HasTile(cell)
            && pathTilemap.HasTile(cell + Vector3Int.right)
            && pathTilemap.HasTile(cell + Vector3Int.up)
            && pathTilemap.HasTile(cell + Vector3Int.right + Vector3Int.up);
    }

    // La esquina superior derecha de la casilla es el centro del bloque de 2x2
    private Vector2 NodeToWorld(Vector3Int cell) => pathTilemap.CellToWorld(cell + Vector3Int.right + Vector3Int.up);

    private bool TryFindNearestNode(Vector2 position, out Vector3Int nearest)
    {
        nearest = default;
        float bestDistance = float.MaxValue;
        foreach (Vector3Int cell in pathTilemap.cellBounds.allPositionsWithin)
        {
            if (!IsNode(cell)) continue;
            float distance = (NodeToWorld(cell) - position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = cell;
            }
        }
        return bestDistance < float.MaxValue;
    }

    private Vector2 PointAt(float distance)
    {
        for (int i = 1; i < route.Count; i++)
        {
            float segment = Vector2.Distance(route[i - 1], route[i]);
            if (distance <= segment) return Vector2.Lerp(route[i - 1], route[i], segment > 0f ? distance / segment : 1f);
            distance -= segment;
        }
        return route[route.Count - 1];
    }

    // ---------- Puntos ----------

    private void CreateDots()
    {
        Transform container = new GameObject("PathDots").transform;
        container.SetParent(transform, false);
        container.position = new Vector3(0f, SortingGroupY, 0f);

        SortingGroup group = container.gameObject.AddComponent<SortingGroup>();
        if (pathTilemap.TryGetComponent(out TilemapRenderer pathRenderer))
        {
            group.sortingLayerID = pathRenderer.sortingLayerID;
            group.sortingOrder = pathRenderer.sortingOrder + 1;
        }

        Sprite sprite = CreateDotSprite();
        int count = Mathf.Max(1, Mathf.FloorToInt(routeLength / dotSpacing) + 1);
        dots = new Transform[count];
        dotDistances = new float[count];
        dotSpawnTimes = new float[count];

        for (int i = 0; i < count; i++)
        {
            var renderer = new GameObject("Dot").AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = dotColor;
            if (dotMaterial != null) renderer.sharedMaterial = dotMaterial;

            dots[i] = renderer.transform;
            dots[i].SetParent(container, false);
            dotDistances[i] = Mathf.Min(i * dotSpacing, routeLength);
            dots[i].position = PointAt(dotDistances[i]);
            dots[i].localScale = Vector3.zero;
            dotSpawnTimes[i] = float.MaxValue;
        }
    }

    private void SpawnDotsUpTo(float distance, float spawnTime)
    {
        for (int i = 0; i < dots.Length; i++)
            if (dotSpawnTimes[i] == float.MaxValue && dotDistances[i] <= distance)
                dotSpawnTimes[i] = spawnTime;
    }

    // Círculo de bordes suaves generado por código: no hace falta ningún asset
    private static Sprite CreateDotSprite()
    {
        var texture = new Texture2D(DotTextureSize, DotTextureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float radius = DotTextureSize * 0.5f;
        var pixels = new Color32[DotTextureSize * DotTextureSize];
        for (int y = 0; y < DotTextureSize; y++)
        {
            for (int x = 0; x < DotTextureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius)) / radius;
                float alpha = Mathf.Clamp01((1f - distance) / 0.35f); // borde difuminado en el 35% exterior
                pixels[y * DotTextureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();

        // pixelsPerUnit = tamaño de la textura: el sprite mide 1 unidad y se escala con dotSize
        return Sprite.Create(texture, new Rect(0, 0, DotTextureSize, DotTextureSize), new Vector2(0.5f, 0.5f), DotTextureSize);
    }

    // Sobrepasa ligeramente el tamaño final y vuelve: efecto "pop" (igual que PromptBubble)
    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float p = x - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }
}
