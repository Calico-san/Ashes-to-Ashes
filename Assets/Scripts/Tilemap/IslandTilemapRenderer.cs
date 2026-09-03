using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Reads TilemapData and draws it into a standard Unity Tilemap ("Ground" layer)
/// instead of one GameObject+SpriteRenderer per tile. TilemapData stays the single
/// source of truth for gameplay (PlacementValidator, WorkerAgent, camera bounds all
/// read it through this class's public API, unchanged) — only the drawing mechanism
/// changed, so no other system needed to be touched.
///
/// Grid + Tilemap are created at runtime, aligned to TilemapData.Offset/TileSize,
/// same as before. Tile assets are generated on the fly from SpriteRegistry sprites
/// and cached per-sprite (one Tile per unique sprite, not per cell) — this is what
/// keeps memory sane at 1536 cells.
///
/// Attach to a GameObject in the scene, or let Bootstrapper create it.
/// </summary>
public class IslandTilemapRenderer : MonoBehaviour
{
    public static IslandTilemapRenderer Instance { get; private set; }

    [SerializeField] private TilemapData _map;

    // Placeholder colors per tile type — used only if SpriteRegistry has no sprite yet.
    private static readonly Color[] TileColors = new Color[]
    {
        new Color(0.09f, 0.28f, 0.52f, 1f),  // 0 Ocean     — blue
        new Color(0.72f, 0.62f, 0.38f, 1f),  // 1 Shore     — sand
        new Color(0.15f, 0.42f, 0.18f, 1f),  // 2 Land      — green
        new Color(0.08f, 0.28f, 0.10f, 1f),  // 3 Forest    — dark green
        new Color(0.50f, 0.48f, 0.45f, 1f),  // 4 IronMine  — grey
        new Color(0.75f, 0.15f, 0.10f, 1f),  // 5 Volcano   — red
    };

    /// <summary>Grid shared with UnityTilemapSetup so Details/Objects layers line up exactly.</summary>
    public Grid WorldGrid { get; private set; }

    private Tilemap _ground;

    // One Tile asset per unique sprite — avoids creating 1536 ScriptableObjects.
    private readonly Dictionary<Sprite, Tile> _tileCache = new();

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
        EnsureGrid();
        BuildTiles();
    }

    // -------------------------------------------------------
    // Public accessors (used by PlacementValidator, camera, workers — unchanged)
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

        if (buildingType == BuildingType.Steelworks && !IsNearIronMine(center))
            return false;

        if (buildingType == BuildingType.Sawmill && !IsNearForest(center))
            return false;

        if (buildingType == BuildingType.Shipyard && !IsNearOcean(center))
            return false;

        return true;
    }

    // -------------------------------------------------------
    // Grid / Tilemap setup
    // -------------------------------------------------------

    /// <summary>Creates the Grid + Ground Tilemap aligned to TilemapData, if not already present.</summary>
    private void EnsureGrid()
    {
        if (WorldGrid != null && _ground != null) return;

        var gridGO = new GameObject("VisualTilemap");
        gridGO.transform.SetParent(transform);

        var grid = gridGO.AddComponent<Grid>();
        grid.cellSize   = new Vector3(_map.TileSize, _map.TileSize, 0f);
        grid.cellLayout = GridLayout.CellLayout.Rectangle;
        gridGO.transform.position = new Vector3(_map.Offset.x, _map.Offset.y, 0f);
        WorldGrid = grid;

        var groundGO = new GameObject("Ground");
        groundGO.transform.SetParent(gridGO.transform, false);
        _ground = groundGO.AddComponent<Tilemap>();
        // Efektivni fps = animationFrameRate * TileAnimationData.animationSpeed.
        // Drzimo bazu na 1 da OceanFps iz SpriteRegistryja bude izravno u fps-ima.
        _ground.animationFrameRate = 1f;

        var renderer = groundGO.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = -5;
        // Non-overlapping grid — one tile per cell, so all terrain shares one sort order.
        // Buildings/workers render above it (sortingOrder 0+), same as before.
    }

    // -------------------------------------------------------
    // Private
    // -------------------------------------------------------

    private void BuildTiles()
    {
        _ground.ClearAllTiles();
        _tileCache.Clear();

        var registry = SpriteRegistry.Instance;
        if (registry == null)
            Debug.LogError("[TilemapRenderer] SpriteRegistry.Instance je null — svi tileovi ce biti placeholder boje. " +
                           "Provjeri da Bootstrapper poziva SpriteRegistry.Register() prije SetupTilemap().");

        var missing = new Dictionary<TileType, int>();
        int total = _map.Width * _map.Height;

        // Jedna animirana pločica dijeli se na sva "cista" oceanska polja. Rubna
        // polja (prijelaz prema obali) ostaju staticna jer ona nisu ocean nego Shore.
        // Vraca null ako OceanFrames nisu postavljeni — tada se koristi TileOcean.
        TileBase oceanTile = OceanRenderer.Create(registry);

        for (int row = 0; row < _map.Height; row++)
        for (int col = 0; col < _map.Width;  col++)
        {
            TileType type = _map.GetTile(col, row);

            // Animirani ocean ima prednost pred staticnim TileOcean spriteom
            if (type == TileType.Ocean && oceanTile != null)
            {
                _ground.SetTile(new Vector3Int(col, row, 0), oceanTile);
                continue;
            }

            // Prvo rubni sprite (prijelaz Shore/Ocean i Shore/Land), pa obican tile
            var sprite = TileEdgeResolver.Resolve(_map, col, row, type);
            if (sprite == null && registry != null)
                sprite = registry.GetTileSprite(type);

            bool isPlaceholder = sprite == null;
            if (isPlaceholder)
            {
                sprite = SimpleShapeFactory.CreateFilledSquareSprite(TileColorFor(type));
                missing.TryGetValue(type, out int n);
                missing[type] = n + 1;
            }

            var tile = GetOrCreateTile(sprite);
            _ground.SetTile(new Vector3Int(col, row, 0), tile);
        }

        if (missing.Count == 0)
            Debug.Log($"[TilemapRenderer] Svih {total} polja koristi sprite.");
        else
            foreach (var kv in missing)
                Debug.LogWarning($"[TilemapRenderer] {kv.Key}: {kv.Value} polja bez sprite-a, crta se placeholder boja. " +
                                 $"Provjeri polje Tile{kv.Key} u SpriteRegistry.asset.");
    }

    /// <summary>
    /// One Tile ScriptableObject per unique sprite, reused across every cell that
    /// needs it (e.g. every plain Ocean tile shares one Tile instance).
    /// </summary>
    private Tile GetOrCreateTile(Sprite sprite)
    {
        if (_tileCache.TryGetValue(sprite, out var cached))
            return cached;

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        _tileCache[sprite] = tile;
        return tile;
    }

    private static Color TileColorFor(TileType type)
    {
        int idx = (int)type;
        return idx >= 0 && idx < TileColors.Length ? TileColors[idx] : Color.magenta;
    }

    // Max distance in tiles from an IronMine for Steelworks placement
    private const int SteelworksIronMineRadius = 1;
    private const int SawmillForestRadius      = 1;

    private static bool IsTileAllowed(TileType tile, BuildingType buildingType)
    {
        switch (buildingType)
        {
            case BuildingType.Shipyard:
                return tile == TileType.Shore || tile == TileType.Land;

            case BuildingType.Steelworks:
                return tile == TileType.Land || tile == TileType.Forest;

            case BuildingType.Sawmill:
                return tile == TileType.Land || tile == TileType.Forest;

            case BuildingType.HuntersHut:
            case BuildingType.ScoutStation:
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
