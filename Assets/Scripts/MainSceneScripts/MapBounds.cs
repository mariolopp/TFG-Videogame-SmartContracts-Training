using UnityEngine;
using UnityEngine.Tilemaps;

// Genera cuatro muros solidos alrededor del mapa para que el personaje no pueda salirse.
// Si se asigna un Tilemap, los limites se ajustan automaticamente a sus celdas pintadas;
// si no, se usa el rectangulo definido a mano (en coordenadas de mundo).
public class MapBounds : MonoBehaviour
{
    [Header("Limites")]
    public Tilemap tilemap;                                  // Opcional: ajusta los limites al tilemap
    public Rect bounds = new Rect(-10f, -6f, 20f, 12f);      // Usado si no hay tilemap
    public float padding = 0f;                               // Margen hacia dentro desde el borde
    public float wallThickness = 2f;

    void Awake()
    {
        Rect r = GetWorldRect();

        CreateWall("Wall_Top",    new Vector2(r.center.x, r.yMax + wallThickness / 2f), new Vector2(r.width + wallThickness * 2f, wallThickness));
        CreateWall("Wall_Bottom", new Vector2(r.center.x, r.yMin - wallThickness / 2f), new Vector2(r.width + wallThickness * 2f, wallThickness));
        CreateWall("Wall_Left",   new Vector2(r.xMin - wallThickness / 2f, r.center.y), new Vector2(wallThickness, r.height));
        CreateWall("Wall_Right",  new Vector2(r.xMax + wallThickness / 2f, r.center.y), new Vector2(wallThickness, r.height));
    }

    public Rect GetWorldRect()
    {
        Rect r = bounds;

        if (tilemap != null)
        {
            tilemap.CompressBounds();
            Bounds lb = tilemap.localBounds;
            Vector3 min = tilemap.transform.TransformPoint(lb.min);
            Vector3 max = tilemap.transform.TransformPoint(lb.max);
            r = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        r.xMin += padding; r.yMin += padding;
        r.xMax -= padding; r.yMax -= padding;
        return r;
    }

    private void CreateWall(string wallName, Vector2 center, Vector2 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(transform, false);
        wall.transform.position = center;

        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;
    }

    void OnDrawGizmos()
    {
        Rect r = GetWorldRect();
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(r.center, r.size);
    }
}
