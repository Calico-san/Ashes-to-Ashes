using UnityEngine;

/// <summary>
/// Tile types used in the island tilemap.
/// Value matches the integer used in TilemapData.tiles array.
/// </summary>
public enum TileType
{
    Ocean    = 0,   // deep blue  — ships, Shipyard
    Shore    = 1,   // sand/gold  — Shipyard placement
    Land     = 2,   // green      — all buildings
    Forest   = 3,   // dark green — -15% production debuff to adjacent buildings
    IronMine = 4,   // grey       — only Steelworks allowed
    Volcano  = 5,   // red        — impassable, game over zone
}

/// <summary>
/// Holds the island map as a flat int array (row-major).
/// Tomislav edits this ScriptableObject in the Inspector.
///
/// Create via: Assets > Create > Ashes > TilemapData
///
/// Coordinate system:
///   tiles[row * Width + col]
///   row 0 = bottom row, row Height-1 = top row
///   col 0 = left,       col Width-1  = right
///
/// World position of tile (col, row):
///   x = col * TileSize + OffsetX
///   y = row * TileSize + OffsetY
/// </summary>
[CreateAssetMenu(fileName = "IslandMap", menuName = "Ashes/TilemapData")]
public class TilemapData : ScriptableObject
{
    [Header("Map dimensions")]
    public int Width  = 48;
    public int Height = 32;

    [Header("World space")]
    [Tooltip("Size of one tile in world units.")]
    public float TileSize = 1f;
    [Tooltip("World position of tile (0,0) bottom-left corner.")]
    public Vector2 Offset = new Vector2(-24f, -16f);

    [Header("Map data — int per tile (TileType enum value)")]
    [Tooltip("Flat array, row-major. Length must equal Width * Height.")]
    public int[] tiles;

    // ---- Accessors ----

    public TileType GetTile(int col, int row)
    {
        if (!InBounds(col, row)) return TileType.Ocean;
        return (TileType)tiles[row * Width + col];
    }

    public void SetTile(int col, int row, TileType type)
    {
        if (!InBounds(col, row)) return;
        tiles[row * Width + col] = (int)type;
    }

    public bool InBounds(int col, int row)
        => col >= 0 && col < Width && row >= 0 && row < Height;

    /// <summary>World position of tile center.</summary>
    public Vector2 TileToWorld(int col, int row)
        => new Vector2(Offset.x + col * TileSize + TileSize * 0.5f,
                       Offset.y + row * TileSize + TileSize * 0.5f);

    /// <summary>Tile coordinates for a world position.</summary>
    public Vector2Int WorldToTile(Vector2 world)
        => new Vector2Int(
            Mathf.FloorToInt((world.x - Offset.x) / TileSize),
            Mathf.FloorToInt((world.y - Offset.y) / TileSize));

    public TileType GetTileAtWorld(Vector2 world)
    {
        var t = WorldToTile(world);
        return GetTile(t.x, t.y);
    }

    /// <summary>Ensure tiles array has correct length. Call after changing Width/Height.</summary>
    public void Resize()
    {
        int needed = Width * Height;
        if (tiles == null || tiles.Length != needed)
        {
            var old = tiles ?? new int[0];
            tiles = new int[needed]; // defaults to 0 = Ocean
            int copy = Mathf.Min(old.Length, needed);
            for (int i = 0; i < copy; i++) tiles[i] = old[i];
        }
    }

    /// <summary>Placeholder island — used when no custom map is set.</summary>
    public void GeneratePlaceholder()
    {
        Resize();
        float cx = Width  * 0.5f;
        float cy = Height * 0.5f;
        float rx = Width  * 0.38f;
        float ry = Height * 0.38f;
        float shoreRx = rx + 1.5f;
        float shoreRy = ry + 1.5f;

        for (int row = 0; row < Height; row++)
        for (int col = 0; col < Width;  col++)
        {
            float dx = (col + 0.5f - cx) / rx;
            float dy = (row + 0.5f - cy) / ry;
            float dxS = (col + 0.5f - cx) / shoreRx;
            float dyS = (row + 0.5f - cy) / shoreRy;
            float dist  = dx*dx + dy*dy;
            float distS = dxS*dxS + dyS*dyS;

            TileType t;
            if      (dist  <= 0.10f) t = TileType.Volcano;
            else if (dist  <= 1.00f) t = TileType.Land;
            else if (distS <= 1.00f) t = TileType.Shore;
            else                     t = TileType.Ocean;

            SetTile(col, row, t);
        }

        // Iron mine patch — right side of island
        int mineCol = Mathf.RoundToInt(cx + rx * 0.55f);
        int mineRow = Mathf.RoundToInt(cy);
        for (int dr = -1; dr <= 1; dr++)
        for (int dc = -1; dc <= 1; dc++)
            SetTile(mineCol + dc, mineRow + dr, TileType.IronMine);

        // Forest patch — left side
        int forestCol = Mathf.RoundToInt(cx - rx * 0.5f);
        int forestRow = Mathf.RoundToInt(cy + ry * 0.3f);
        for (int dr = -2; dr <= 2; dr++)
        for (int dc = -2; dc <= 2; dc++)
            SetTile(forestCol + dc, forestRow + dr, TileType.Forest);
    }
}
