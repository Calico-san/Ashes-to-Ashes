using UnityEngine;

/// <summary>
/// Reads TilemapData and creates one colored SpriteRenderer quad per tile.
/// Each TileType has a placeholder color — replaced by Tomislav's sprites later
/// via SpriteRegistry (same pattern as buildings).
///
/// Attach to a GameObject in the scene, or let Bootstrapper create it.
/// </summary>
public class IslandTilemapRenderer : MonoBehaviour
{
    public static IslandTilemapRenderer Instance { get; private set; }

    [SerializeField] private TilemapData _map;

    // Placeholder colors per tile type
    private static readonly Color[] TileColors = new Color[]
    {
        new Color(0.09f, 0.28f, 0.52f, 1f),  // 0 Ocean     — blue
        new Color(0.72f, 0.62f, 0.38f, 1f),  // 1 Shore     — sand
        new Color(0.15f, 0.42f, 0.18f, 1f),  // 2 Land      — green
        new Color(0.08f, 0.28f, 0.10f, 1f),  // 3 Forest    — dark green
        new Color(0.50f, 0.48f, 0.45f, 1f),  // 4 IronMine  — grey
        new Color(0.75f, 0.15f, 0.10f, 1f),  // 5 Volcano   — red
    };

    private GameObject[] _tileObjects;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // -------------------------------------------------------
    // Init
    // -------------------------------------------------------

    public void Initialize(TilemapData map)
    {
        _map = map;
        if (_map == null) { Debug.LogWarning("[TilemapRenderer] No TilemapData assigned."); return; }
        BuildTiles();
    }

    // -------------------------------------------------------
    // Public accessors (used by PlacementValidator)
    // -------------------------------------------------------

    public TilemapData Map => _map;

    public TileType GetTileAtWorld(Vector2 worldPos)
        => _map != null ? _map.GetTileAtWorld(worldPos) : TileType.Ocean;

    /// <summary>True if the building footprint is entirely valid for this building type.</summary>
    public bool IsValidPlacement(Vector2 center, Vector2 size, BuildingType buildingType)
    {
        if (_map == null) return false;

        var corners = GetFootprintSamples(center, size);
        foreach (var pt in corners)
        {
            var tile = _map.GetTileAtWorld(pt);
            if (!IsTileAllowed(tile, buildingType)) return false;
        }

        // Steelworks: must be within 1 tile of IronMine
        if (buildingType == BuildingType.Steelworks && !IsNearIronMine(center))
            return false;

        // Sawmill: must be within 1 tile of Forest
        if (buildingType == BuildingType.Sawmill && !IsNearForest(center))
            return false;

        // Shipyard: must be within 1 tile of Ocean
        if (buildingType == BuildingType.Shipyard && !IsNearOcean(center))
            return false;

        return true;
    }

    // -------------------------------------------------------
    // Private
    // -------------------------------------------------------

    private void BuildTiles()
    {
        // Clear old tiles if rebuilding
        if (_tileObjects != null)
            foreach (var o in _tileObjects)
                if (o != null) Destroy(o);

        int total = _map.Width * _map.Height;
        _tileObjects = new GameObject[total];

        var parent = new GameObject("Tiles");
        parent.transform.SetParent(transform);

        for (int row = 0; row < _map.Height; row++)
        for (int col = 0; col < _map.Width;  col++)
        {
            int      idx  = row * _map.Width + col;
            TileType type = _map.GetTile(col, row);
            Vector2  pos  = _map.TileToWorld(col, row);

            var go = new GameObject($"Tile_{col}_{row}");
            go.transform.SetParent(parent.transform);
            go.transform.position   = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(_map.TileSize, _map.TileSize, 1f);

            var sr      = go.AddComponent<SpriteRenderer>();
            var tileSprite = SpriteRegistry.Instance?.GetTileSprite(type);
            sr.sprite   = tileSprite != null
                ? tileSprite
                : SimpleShapeFactory.CreateFilledSquareSprite(TileColorFor(type));
            sr.sortingOrder = SortOrderFor(type);

            _tileObjects[idx] = go;
        }
    }

    private static Color TileColorFor(TileType type)
    {
        int idx = (int)type;
        return idx >= 0 && idx < TileColors.Length ? TileColors[idx] : Color.magenta;
    }

    private static int SortOrderFor(TileType type)
    {
        switch (type)
        {
            case TileType.Ocean:   return -20;
            case TileType.Shore:   return -10;
            default:               return -5;
        }
    }

    // Max distance in tiles from an IronMine for Steelworks placement
    private const int SteelworksIronMineRadius = 1;
    private const int SawmillForestRadius      = 1;

    private static bool IsTileAllowed(TileType tile, BuildingType buildingType)
    {
        switch (buildingType)
        {
            case BuildingType.Shipyard:
                // Shipyard on Shore or Land — must also be near Ocean (checked in IsValidPlacement)
                return tile == TileType.Shore || tile == TileType.Land;

            case BuildingType.Steelworks:
                // Steelworks on Land — must be near IronMine (proximity check in IsValidPlacement)
                return tile == TileType.Land || tile == TileType.Forest;

            case BuildingType.Sawmill:
                // Sawmill on Land or Forest — must be near Forest (proximity check in IsValidPlacement)
                return tile == TileType.Land || tile == TileType.Forest;

            default:
                return tile == TileType.Land || tile == TileType.Forest;
        }
    }

    /// <summary>True if any Forest tile exists within radius tiles of center.</summary>
    private bool IsNearForest(Vector2 worldCenter)
    {
        if (_map == null) return false;
        var tile = _map.WorldToTile(worldCenter);
        int r    = SawmillForestRadius;
        for (int dr = -r; dr <= r; dr++)
        for (int dc = -r; dc <= r; dc++)
            if (_map.GetTile(tile.x + dc, tile.y + dr) == TileType.Forest)
                return true;
        return false;
    }

    /// <summary>True if any Ocean tile exists within radius 1 of center.</summary>
    private bool IsNearOcean(Vector2 worldCenter)
    {
        if (_map == null) return false;
        var tile = _map.WorldToTile(worldCenter);
        for (int dr = -1; dr <= 1; dr++)
        for (int dc = -1; dc <= 1; dc++)
            if (_map.GetTile(tile.x + dc, tile.y + dr) == TileType.Ocean)
                return true;
        return false;
    }

    /// <summary>True if any IronMine tile exists within radius tiles of center.</summary>
    private bool IsNearIronMine(Vector2 worldCenter)
    {
        if (_map == null) return false;
        var  tile   = _map.WorldToTile(worldCenter);
        int  r      = SteelworksIronMineRadius;
        for (int dr = -r; dr <= r; dr++)
        for (int dc = -r; dc <= r; dc++)
        {
            if (_map.GetTile(tile.x + dc, tile.y + dr) == TileType.IronMine)
                return true;
        }
        return false;
    }

    private static Vector2[] GetFootprintSamples(Vector2 center, Vector2 size)
    {
        float hx = size.x * 0.45f;
        float hy = size.y * 0.45f;
        return new[]
        {
            center,
            new Vector2(center.x - hx, center.y - hy),
            new Vector2(center.x + hx, center.y - hy),
            new Vector2(center.x - hx, center.y + hy),
            new Vector2(center.x + hx, center.y + hy),
        };
    }
}
