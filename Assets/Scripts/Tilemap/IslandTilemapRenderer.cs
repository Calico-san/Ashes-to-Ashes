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

    private const float TileOverscan = 1.01f;

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

    // Podloga ispod Grounda. Novi sprajtovi (drvo, rudnik, vulkan) crtaju samo
    // svoj motiv i imaju prozirnu pozadinu, pa bi se kroz njih vidjela boja
    // pozadine kamere. Ova mapa im podmece Land sprite.
    private Tilemap _groundBase;

    /// <summary>
    /// Tipovi ciji sprajt ne pokriva cijelo polje (prozirna pozadina), pa im treba
    /// Land ispod. Ocean i Shore imaju pune sprajtove; Land je i sam podloga.
    /// Doda li Tomislav jos koji prozirni sprajt, dopise se ovdje.
    /// </summary>
    private static readonly TileType[] NeedsLandBase =
    {
        TileType.Forest,
        TileType.IronMine,
        TileType.Volcano,
    };

    /// <summary>
    /// Tipovi koje ne crta tilemap nego zaseban objekt u velicini zgrade
    /// (IronMineRenderer, VolcanoRenderer). Njihovo polje na Ground sloju ostaje
    /// prazno da se art ne crta dvaput — vidi se samo Land iz podloge.
    /// </summary>
    private static readonly TileType[] DrawnAsObject =
    {
        TileType.IronMine,
    };

    private static bool Contains(TileType[] set, TileType type)
    {
        for (int i = 0; i < set.Length; i++)
            if (set[i] == type) return true;
        return false;
    }


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

        // Svako polje koje otisak pokriva mora biti dopusteno. Ranije se
        // uzorkovalo pet tocaka, sto je bilo tocno samo za otisak 1x1;
        // brodogradiliste je 2x2, pa se ide preko svih pokrivenih polja.
        if (!TryGetFootprint(center, size, out int minCol, out int minRow,
                                             out int maxCol, out int maxRow))
            return false;

        for (int row = minRow; row <= maxRow; row++)
        for (int col = minCol; col <= maxCol; col++)
        {
            if (!_map.InBounds(col, row)) return false;
            if (!IsTileAllowed(_map.GetTile(col, row), buildingType)) return false;
        }

        if (buildingType == BuildingType.Steelworks
            && !IsNear(TileType.IronMine, minCol, minRow, maxCol, maxRow, SteelworksIronMineRadius))
            return false;

        if (buildingType == BuildingType.Sawmill
            && !IsNear(TileType.Forest, minCol, minRow, maxCol, maxRow, SawmillForestRadius))
            return false;

        if (buildingType == BuildingType.Shipyard
            && !IsNear(TileType.Ocean, minCol, minRow, maxCol, maxRow, ShipyardOceanRadius))
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

        // Podloga ispod Grounda. O redoslijedu crtanja odlucuje sortingOrder
        // (-6 < -5), ne redoslijed djece u hijerarhiji.
        var baseGO = new GameObject("GroundBase");
        baseGO.transform.SetParent(gridGO.transform, false);
        _groundBase = baseGO.AddComponent<Tilemap>();
        baseGO.AddComponent<TilemapRenderer>().sortingOrder = -6;

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
        _groundBase?.ClearAllTiles();
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
        TileBase oceanTile = OceanRenderer.Create(registry, TileOverscan);

        // Podloga za prozirne sprajtove. Jedan Tile za cijelu kartu — dijeli se
        // kroz _tileCache, pa ne kosta nista dodatno.
        Tile landBaseTile = null;
        if (_groundBase != null)
        {
            var landSprite = registry != null ? registry.GetTileSprite(TileType.Land) : null;
            if (landSprite == null)
                landSprite = SimpleShapeFactory.CreateFilledSquareSprite(TileColorFor(TileType.Land));
            landBaseTile = GetOrCreateTile(landSprite);
        }

        for (int row = 0; row < _map.Height; row++)
        for (int col = 0; col < _map.Width;  col++)
        {
            TileType type = _map.GetTile(col, row);
            var pos = new Vector3Int(col, row, 0);

            // Prozirni sprajtovi (drvo, rudnik, vulkan) bi inace pokazali boju
            // pozadine kamere umjesto kopna.
            if (landBaseTile != null && Contains(NeedsLandBase, type))
                _groundBase.SetTile(pos, landBaseTile);

            // Rudnik crta IronMineRenderer u velicini zgrade. Polje ostaje prazno
            // da se art ne crta dvaput, jednom malen u tileu i jednom velik.
            if (Contains(DrawnAsObject, type))
                continue;

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

        // Tilemap crta sprite u njegovoj prirodnoj velicini (pikseli / PPU), a ne
        // u velicini polja. Sprite od 16px pri PPU 32 zato zauzme pola polja i
        // izgleda sitno. Skaliranjem pločice velicina uvoza arta prestaje biti
        // bitna — isti razlog zbog kojeg SpriteFit postoji za obicne renderere.
        float scale = CellFitScale(sprite);
        if (!Mathf.Approximately(scale, 1f))
        {
            tile.transform = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            tile.flags     = TileFlags.LockTransform | TileFlags.LockColor;
        }

        _tileCache[sprite] = tile;
        return tile;
    }

    /// <summary>
    /// Koliko treba skalirati sprite da njegova duza stranica tocno popuni polje.
    /// Jednoliko po obje osi — art se ne razvlaci, kvadratni sprite popuni polje,
    /// a nekvadratni ostane u omjeru i dotakne rub duzom stranicom.
    /// </summary>
    private float CellFitScale(Sprite sprite)
    {
        if (sprite == null || _map == null) return 1f;

        float ppu = sprite.pixelsPerUnit > 0f ? sprite.pixelsPerUnit : 100f;
        float natural = Mathf.Max(sprite.rect.width, sprite.rect.height) / ppu;
        if (natural <= 0.0001f) return 1f;

        return _map.TileSize / natural * TileOverscan;
    }

    private static Color TileColorFor(TileType type)
    {
        int idx = (int)type;
        return idx >= 0 && idx < TileColors.Length ? TileColors[idx] : Color.magenta;
    }

    // Max distance in tiles from an IronMine for Steelworks placement
    private const int SteelworksIronMineRadius = 1;
    private const int SawmillForestRadius      = 1;
    private const int ShipyardOceanRadius      = 1;

    /// <summary>
    /// Na cemu gradevina smije stajati. Suma NIJE gradiva ni za koga — ostaje
    /// samo uvjet blizine za Sawmill, koji tako mora stajati uz sumu, a ne na njoj.
    /// </summary>
    private static bool IsTileAllowed(TileType tile, BuildingType buildingType)
    {
        switch (buildingType)
        {
            case BuildingType.Shipyard:
                return tile == TileType.Shore || tile == TileType.Land;

            default:
                return tile == TileType.Land;
        }
    }

    /// <summary>
    /// Polja koja otisak pokriva. Rubovi se uvlace za djelic polja jer bi tocno
    /// na granici zaokruzivanje u WorldToTile ocitalo susjedno polje, pa bi se
    /// ispravna pozicija uz rub nasumicno odbijala.
    /// </summary>
    private bool TryGetFootprint(Vector2 center, Vector2 size,
                                 out int minCol, out int minRow,
                                 out int maxCol, out int maxRow)
    {
        minCol = minRow = maxCol = maxRow = 0;
        if (_map == null) return false;

        const float Inset = 0.05f;
        float hx = Mathf.Max(0f, size.x * 0.5f - Inset);
        float hy = Mathf.Max(0f, size.y * 0.5f - Inset);

        var lo = _map.WorldToTile(new Vector2(center.x - hx, center.y - hy));
        var hi = _map.WorldToTile(new Vector2(center.x + hx, center.y + hy));

        minCol = Mathf.Min(lo.x, hi.x);  maxCol = Mathf.Max(lo.x, hi.x);
        minRow = Mathf.Min(lo.y, hi.y);  maxRow = Mathf.Max(lo.y, hi.y);
        return true;
    }

    /// <summary>
    /// True ako trazeno polje postoji unutar <paramref name="radius"/> polja od
    /// BILO KOJEG polja otiska. Mjeri se od ruba otiska, ne od sredista — inace
    /// brodogradiliste 2x2 nikad ne bi bilo "uz more" jer mu je srediste uvijek
    /// najmanje jedno polje od ruba.
    /// </summary>
    private bool IsNear(TileType wanted, int minCol, int minRow, int maxCol, int maxRow, int radius)
    {
        if (_map == null) return false;

        for (int row = minRow - radius; row <= maxRow + radius; row++)
        for (int col = minCol - radius; col <= maxCol + radius; col++)
            if (_map.GetTile(col, row) == wanted) return true;

        return false;
    }
}
