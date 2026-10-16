using UnityEngine;

/// <summary>
/// Controlador de proximidad e interacción del jugador con los stands.
/// Cada frame activa el resalte de los stands cercanos, muestra el globo solo en el
/// más cercano y, al pulsar Espacio o Enter, lleva al jugador hasta él y lanza la entrada.
/// </summary>
[RequireComponent(typeof(playerMovement))]
public class StandInteractor : MonoBehaviour
{
    [Tooltip("Al volver de un minijuego, el jugador aparece en el punto de acceso del stand más este desplazamiento.")]
    [SerializeField] private Vector2 returnOffset = new Vector2(0f, -1f);

    // Estático: sobrevive al cambio de escena (el jugador se destruye y se vuelve a crear)
    private static string pendingReturnScene;

    private playerMovement movement;
    private StandInteractable currentStand;
    private bool isEntering;

    /// <summary>Si es false no se resalta ni se entra en ningún stand (cinemáticas, como el camino guía).</summary>
    public bool InteractionEnabled { get; set; } = true;

    /// <summary>
    /// Al volver al mapa, el jugador aparecerá delante del stand de esa escena. Lo hace solo al entrar
    /// por un stand; llamarlo a mano sirve cuando se llega al minijuego por otra vía (la primera partida).
    /// </summary>
    public static void SetReturnStand(string sceneName) => pendingReturnScene = sceneName;

    private void Awake()
    {
        movement = GetComponent<playerMovement>();
    }

    private void Start()
    {
        // Algunos minijuegos dejan Time.timeScale a 0 en su pantalla final; sin esto
        // FixedUpdate no se ejecuta y el jugador anima pero no se desplaza
        Time.timeScale = 1f;

        if (pendingReturnScene != null)
        {
            StandInteractable stand = StandInteractable.Find(pendingReturnScene);
            if (stand != null) movement.Teleport(stand.AccessPosition + returnOffset);
            pendingReturnScene = null;
        }
    }

    private void Update()
    {
        if (isEntering) return;

        if (!InteractionEnabled)
        {
            ClearProximity();
            return;
        }

        UpdateProximity();

        if (currentStand != null && InteractPressed() && !IsDialogOpen())
            EnterStand(currentStand);
    }

    private void UpdateProximity()
    {
        Vector2 playerPosition = transform.position;
        StandInteractable closest = null;
        float closestDistance = float.MaxValue;

        // Bucle for (no foreach) para no generar basura en memoria cada frame
        var stands = StandInteractable.ActiveStands;
        for (int i = 0; i < stands.Count; i++)
        {
            StandInteractable stand = stands[i];
            if (stand.IsLocked) continue; // SetLocked ya le quita el resalte y el globo

            // Resalte e interacción se miden desde el punto de acceso (delante del stand):
            // así no se puede entrar desde detrás y el paseo automático es corto
            float accessDistance = Vector2.Distance(playerPosition, stand.AccessPosition);
            stand.SetHighlighted(accessDistance <= stand.HighlightRadius);

            if (accessDistance <= stand.InteractRadius && accessDistance < closestDistance)
            {
                closest = stand;
                closestDistance = accessDistance;
            }
        }

        // Un único globo visible aunque los radios de varios stands se solapen
        if (closest != currentStand)
        {
            if (currentStand != null) currentStand.SetPromptVisible(false);
            currentStand = closest;
            if (currentStand != null) currentStand.SetPromptVisible(true);
        }
    }

    private void ClearProximity()
    {
        var stands = StandInteractable.ActiveStands;
        for (int i = 0; i < stands.Count; i++)
            stands[i].SetHighlighted(false);

        if (currentStand != null) currentStand.SetPromptVisible(false);
        currentStand = null;
    }

    private void EnterStand(StandInteractable stand)
    {
        if (!stand.HasValidScene)
        {
            Debug.LogError($"StandInteractor: la escena '{stand.SceneName}' de '{stand.name}' no existe o no está en Build Settings.", stand);
            return;
        }

        isEntering = true;
        stand.SetPromptVisible(false);
        movement.ControlEnabled = false; // la escena cambia al final, no hace falta reactivarlo
        pendingReturnScene = stand.SceneName;
        movement.WalkTo(stand.AccessPosition, stand.Enter);
    }

    private static bool InteractPressed()
    {
        return Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter);
    }

    // Evita que la misma pulsación que avanza un diálogo meta al jugador en un stand
    private static bool IsDialogOpen()
    {
        return DialogManager.Instance != null && DialogManager.Instance.gameObject.activeInHierarchy;
    }
}