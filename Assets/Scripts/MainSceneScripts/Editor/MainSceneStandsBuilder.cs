using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Herramienta de editor: monta en la escena abierta los limites del mapa y un stand por minijuego.
// Menu: Tools > MainScene > Crear stands y limites
// Los sprites son placeholders (sprite UI de Unity); se pueden sustituir despues en cada SpriteRenderer.
public static class MainSceneStandsBuilder
{
    private struct StandDef
    {
        public string scene;
        public string title;
        public Color color;
        public Vector2 offset;   // Posicion relativa al jugador

        public StandDef(string scene, string title, Color color, Vector2 offset)
        {
            this.scene = scene; this.title = title; this.color = color; this.offset = offset;
        }
    }

    private static readonly StandDef[] Stands =
    {
        new StandDef("BlockMiner",       "BlockMiner",        new Color(0.95f, 0.65f, 0.20f), new Vector2(-5f, 3f)),
        new StandDef("PoolTrader",       "PoolTrader",        new Color(0.30f, 0.70f, 0.95f), new Vector2( 0f, 3f)),
        new StandDef("ContractApprover", "Contract Approver", new Color(0.55f, 0.85f, 0.40f), new Vector2( 5f, 3f)),
    };

    [MenuItem("Tools/MainScene/Crear stands y limites")]
    public static void Build()
    {
        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector2 origin = player != null ? (Vector2)player.transform.position : Vector2.zero;

        // Limites del mapa
        ReplaceRoot("MapBounds");
        GameObject boundsGo = new GameObject("MapBounds");
        Undo.RegisterCreatedObjectUndo(boundsGo, "Crear limites");
        MapBounds mapBounds = boundsGo.AddComponent<MapBounds>();
        mapBounds.tilemap = Object.FindObjectOfType<Tilemap>();
        if (mapBounds.tilemap == null)
            mapBounds.bounds = new Rect(origin.x - 10f, origin.y - 6f, 20f, 12f);

        // Stands
        ReplaceRoot("Stands");
        GameObject standsRoot = new GameObject("Stands");
        Undo.RegisterCreatedObjectUndo(standsRoot, "Crear stands");

        foreach (StandDef def in Stands)
            CreateStand(def, standsRoot.transform, origin + def.offset, sprite);

        // El antiguo trigger de la cabina queda desactivado (no se borra)
        GameObject cabin = GameObject.Find("CabinTrigger");
        if (cabin != null)
        {
            Undo.RecordObject(cabin, "Desactivar CabinTrigger");
            cabin.SetActive(false);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = standsRoot;
        Debug.Log("[MainSceneStandsBuilder] Stands y limites creados. Recuerda guardar la escena.");
    }

    private static void ReplaceRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) Undo.DestroyObjectImmediate(existing);
    }

    private static void CreateStand(StandDef def, Transform parent, Vector2 position, Sprite sprite)
    {
        // Raiz: area del stand (trigger) delante de la base
        GameObject root = new GameObject("Stand_" + def.scene);
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        BoxCollider2D area = root.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = new Vector2(3.4f, 2.6f);
        area.offset = new Vector2(0f, -1.2f);

        // Base (solida)
        GameObject baseGo = CreateSprite("Base", root.transform, new Vector2(0f, 0f), new Vector2(2.6f, 1.2f),
                                         sprite, new Color(0.25f, 0.22f, 0.28f), 1);
        baseGo.AddComponent<BoxCollider2D>().size = new Vector2(2.6f, 1.2f);

        // Parte que se acerca
        GameObject approach = CreateSprite("ParteQueSeAcerca", root.transform, new Vector2(0f, 1.3f), new Vector2(2f, 1.6f),
                                           sprite, def.color, 2);
        CreateText("Titulo", approach.transform, Vector2.zero, new Vector2(1.9f, 1.5f), def.title, 3.2f, Color.black, 3);

        // Indicador con la tecla
        GameObject prompt = CreateSprite("Indicador", root.transform, new Vector2(0f, 2.9f), new Vector2(3.6f, 0.7f),
                                         sprite, new Color(0f, 0f, 0f, 0.8f), 10);
        TMP_Text label = CreateText("Texto", prompt.transform, Vector2.zero, new Vector2(3.5f, 0.7f),
                                    "[E] Jugar " + def.title, 2.6f, Color.white, 11);

        LevelStand stand = root.AddComponent<LevelStand>();
        stand.sceneName = def.scene;
        stand.displayName = def.title;
        stand.approachPart = approach.transform;
        stand.promptIndicator = prompt;
        stand.promptLabel = label;
    }

    private static GameObject CreateSprite(string name, Transform parent, Vector2 localPos, Vector2 size,
                                           Sprite sprite, Color color, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return go;
    }

    private static TMP_Text CreateText(string name, Transform parent, Vector2 localPos, Vector2 size,
                                       string text, float fontSize, Color color, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.rectTransform.sizeDelta = size;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 1f;
        tmp.fontSizeMax = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.sortingOrder = sortingOrder;
        return tmp;
    }
}
