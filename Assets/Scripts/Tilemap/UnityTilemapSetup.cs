using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Creates Unity Tilemap GameObjects that align perfectly with our TilemapData grid.
/// Visual only — gameplay PlacementValidator still reads TilemapData, not these tilemaps.
///
/// TOMISLAV — ovo kreira Grid i Tilemap layere u sceni.
/// Nakon što se scena pokrene (Play), Tilemap layeri su vidljivi u Hierarchy.
/// Koristis Window → 2D → Tile Palette za crtanje.
///
/// Layeri (sorting order):
///   Ground  (-5)  — tlo, more, obala
///   Details (0)   — trava, kamenje, cvijeće
///   Objects (5)   — stabla, stijene, zgrade-dekoracije
///
/// Grid offset i cell size moraju odgovarati TilemapData.Offset i TileSize.
/// </summary>
public class UnityTilemapSetup : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindFirstObjectByType<UnityTilemapSetup>() != null) return;
        new GameObject("UnityTilemapSetup").AddComponent<UnityTilemapSetup>();
    }

    private void Awake()
    {
        // Wait for TilemapData to be loaded by Bootstrapper
    }

    private void Start()
    {
        CreateTilemapGrid();
    }

    private void CreateTilemapGrid()
    {
        // Use TilemapData settings so Unity grid aligns with gameplay grid
        float tileSize = 1f;
        var   offset   = new Vector3(-24f, -16f, 0f);

        var map = IslandTilemapRenderer.Instance?.Map;
        if (map != null)
        {
            tileSize = map.TileSize;
            offset   = new Vector3(map.Offset.x, map.Offset.y, 0f);
        }

        // Root Grid GameObject
        var gridGO   = new GameObject("VisualTilemap");
        var grid     = gridGO.AddComponent<Grid>();
        grid.cellSize    = new Vector3(tileSize, tileSize, 0f);
        grid.cellLayout  = GridLayout.CellLayout.Rectangle;
        gridGO.transform.position = offset;

        // Three visual layers
        CreateLayer(gridGO.transform, "Ground",  -5);
        CreateLayer(gridGO.transform, "Details",  0);
        CreateLayer(gridGO.transform, "Objects",  5);

        Debug.Log("[UnityTilemapSetup] Visual tilemap layers created. " +
                  "Use Window > 2D > Tile Palette to paint.");
    }

    private static Tilemap CreateLayer(Transform parent, string layerName, int sortingOrder)
    {
        var go       = new GameObject(layerName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        var tilemap  = go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;

        return tilemap;
    }
}
