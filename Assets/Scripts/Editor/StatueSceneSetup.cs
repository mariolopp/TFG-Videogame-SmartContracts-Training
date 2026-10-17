using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Ajustes de importación de las capas de la estatua. Cualquier PNG dentro de StatueSceneSetup.SpritesFolder
/// se importa como sprite con pivote abajo-centro y los mismos píxeles por unidad: queda alineado porque tienen iguales dimensiones
/// </summary>
public class StatueSpriteImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(StatueSceneSetup.SpritesFolder + "/")) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = StatueSceneSetup.PixelsPerUnit;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;

        // El pivote es lo que garantiza la alineación: se fuerza siempre
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        importer.SetTextureSettings(settings);
    }
}

/// <summary>
/// Monta la estatua en la escena abierta (Tools > Estatua). Si ya existe, solo reasigna los sprites
/// Statue_01 ... Statue_10 de la carpeta, así que se puede volver a lanzar al cambiar las imágenes.
/// Desde línea de comandos: -executeMethod StatueSceneSetup.SetupMainScene
/// </summary>
public static class StatueSceneSetup
{
    public const string SpritesFolder = "Assets/Sprites/Environment/Statue_layers";
    public const float PixelsPerUnit = 100f;

    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    private const string PromptPrefabPath = "Assets/Prefabs/PromptBubble.prefab";
    private const string RootName = "Statue";
    private const float VisualsScale = 0.75f;   // la misma que los stands
    private static readonly Vector3 DefaultPosition = new Vector3(8f, 1.5f, 0f); // entre los tres stands

    public static string LayerPath(int level) => $"{SpritesFolder}/Statue_{level:00}.png";

    [MenuItem("Tools/Estatua/Crear o actualizar en la escena abierta")]
    public static void CreateOrUpdate()
    {
        StatueBuilder builder = Object.FindObjectOfType<StatueBuilder>(true);
        if (builder == null) builder = Create();

        int missing = AssignSprites(builder);
        EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
        Selection.activeGameObject = builder.gameObject;

        if (missing > 0)
            Debug.LogWarning($"Estatua: faltan {missing} capas en {SpritesFolder} (Statue_01.png ... Statue_10.png). " +
                             "Añádelas y vuelve a lanzar Tools > Estatua.", builder);
        else
            Debug.Log("Estatua: las 10 capas están asignadas.", builder);
    }

    [MenuItem("Tools/Estatua/Seleccionar carpeta de capas")]
    private static void SelectFolder()
    {
        EnsureFolder();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(SpritesFolder);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    /// <summary>Abre MainScene, monta o actualiza la estatua y guarda. Pensado para batchmode.</summary>
    public static void SetupMainScene()
    {
        var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        CreateOrUpdate();
        EditorSceneManager.SaveScene(scene);
    }

    private static StatueBuilder Create()
    {
        EnsureFolder();

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Crear estatua");
        root.transform.position = DefaultPosition;

        // Visuals: un SortingGroup para que las 10 capas se ordenen contra el jugador como una sola pieza
        Transform visuals = CreateChild("Visuals", root.transform, Vector3.zero);
        visuals.localScale = new Vector3(VisualsScale, VisualsScale, 1f);
        visuals.gameObject.AddComponent<SortingGroup>();

        var layers = new SpriteRenderer[StatueProgress.MaxLevel];
        for (int i = 0; i < layers.Length; i++)
        {
            layers[i] = CreateChild($"Layer_{i + 1:00}", visuals, Vector3.zero).gameObject.AddComponent<SpriteRenderer>();
            layers[i].sortingOrder = i; // cada capa por encima de las anteriores
        }

        Transform accessPoint = CreateChild("AccessPoint", root.transform, Vector3.zero);
        Transform cameraFocus = CreateChild("CameraFocus", root.transform, new Vector3(0f, 3f, 0f));
        PromptBubble prompt = CreatePrompt(root.transform);

        // Para que el jugador no atraviese la base. El tamaño se ajusta al asignar los sprites
        var collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(3f, 0.8f);
        collider.offset = new Vector2(0f, 0.4f);

        var builder = root.AddComponent<StatueBuilder>();
        var so = new SerializedObject(builder);
        SerializedProperty layersProp = so.FindProperty("layers");
        layersProp.arraySize = layers.Length;
        for (int i = 0; i < layers.Length; i++)
            layersProp.GetArrayElementAtIndex(i).objectReferenceValue = layers[i];
        so.FindProperty("visualsRoot").objectReferenceValue = visuals;
        so.FindProperty("cameraFocus").objectReferenceValue = cameraFocus;
        so.ApplyModifiedPropertiesWithoutUndo();

        var interactable = root.AddComponent<StatueInteractable>();
        so = new SerializedObject(interactable);
        so.FindProperty("builder").objectReferenceValue = builder;
        so.FindProperty("accessPoint").objectReferenceValue = accessPoint;
        so.FindProperty("prompt").objectReferenceValue = prompt;
        so.ApplyModifiedPropertiesWithoutUndo();

        FitToSprites(root.transform, collider, cameraFocus, accessPoint, prompt);
        return builder;
    }

    /// <summary>Carga Statue_NN.png en cada capa. Devuelve cuántas faltan.</summary>
    private static int AssignSprites(StatueBuilder builder)
    {
        var so = new SerializedObject(builder);
        SerializedProperty layersProp = so.FindProperty("layers");
        int missing = 0;

        for (int i = 0; i < StatueProgress.MaxLevel; i++)
        {
            var layer = i < layersProp.arraySize ? layersProp.GetArrayElementAtIndex(i).objectReferenceValue as SpriteRenderer : null;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LayerPath(i + 1));
            if (sprite == null) missing++;
            if (layer == null) continue;

            Undo.RecordObject(layer, "Asignar capas de la estatua");
            layer.sprite = sprite;
            layer.enabled = true;
            EditorUtility.SetDirty(layer);
        }
        return missing;
    }

    // Ajusta collider, foco de cámara, acceso y globo al tamaño real de la estatua (si ya hay imágenes)
    private static void FitToSprites(Transform root, BoxCollider2D collider, Transform cameraFocus, Transform accessPoint, PromptBubble prompt)
    {
        // Todas las capas tienen el mismo lienzo: con la primera basta
        Sprite firstLayer = AssetDatabase.LoadAssetAtPath<Sprite>(LayerPath(1));
        if (firstLayer == null) return;

        // Proporciones de la plataforma de Statue_01 (la capa trae su propia sombra debajo).
        // Si se cambia el arte, basta con retocar el collider a mano
        Vector2 canvas = firstLayer.rect.size / firstLayer.pixelsPerUnit * VisualsScale;
        collider.size = new Vector2(canvas.x * 0.7f, canvas.y * 0.15f);
        collider.offset = new Vector2(0f, canvas.y * 0.08f + collider.size.y * 0.5f);
        cameraFocus.localPosition = new Vector3(0f, canvas.y * 0.45f, 0f);
        accessPoint.localPosition = Vector3.zero;
        if (prompt != null) prompt.transform.localPosition = new Vector3(0f, -0.9f, 0f);
    }

    private static PromptBubble CreatePrompt(Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PromptPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"Estatua: no se encontró {PromptPrefabPath}; la estatua no tendrá globo.");
            return null;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = "PromptBubble";
        instance.transform.localPosition = new Vector3(0f, -0.9f, 0f);
        instance.transform.localScale = new Vector3(2f, 2f, 2f); // como en los stands
        return instance.GetComponent<PromptBubble>();
    }

    private static Transform CreateChild(string name, Transform parent, Vector3 localPosition)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        child.localPosition = localPosition;
        return child;
    }

    private static void EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder(SpritesFolder)) return;
        int slash = SpritesFolder.LastIndexOf('/');
        AssetDatabase.CreateFolder(SpritesFolder.Substring(0, slash), SpritesFolder.Substring(slash + 1));
    }
}
