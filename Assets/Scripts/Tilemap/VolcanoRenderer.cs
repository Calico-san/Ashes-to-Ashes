using UnityEngine;

/// <summary>
/// Vulkan nije tile nego jedan veliki animirani objekt.
/// Skripta pronade sva Volcano polja u TilemapData, izracuna njihov omedujuci
/// pravokutnik i preko njega postavi animirani sprite iz SpriteRegistry.VolcanoFrames.
///
/// Nista se ne hardkodira — ako Tomislav pomakne ili poveca krater u
/// Custom Inspectoru karte, vulkan se sam prilagodi pri sljedecem pokretanju.
///
/// Poziva se iz PrototypeBootstrapper.SetupTilemap(), odmah nakon
/// renderer.Initialize(map).
/// </summary>
public class VolcanoRenderer : MonoBehaviour
{
    private SpriteRenderer _sr;
    private float          _timer;
    private int            _frame;

    /// <summary>Stvara vulkan iznad Volcano polja. Vraca null ako nema polja ili nema arta.</summary>
    public static VolcanoRenderer Spawn(TilemapData map)
    {
        if (map == null) return null;

        var registry = SpriteRegistry.Instance;
        if (registry == null || registry.GetVolcanoFrame(0) == null) return null;

        // Omedujuci pravokutnik svih Volcano polja
        int minCol = int.MaxValue, minRow = int.MaxValue;
        int maxCol = int.MinValue, maxRow = int.MinValue;

        for (int row = 0; row < map.Height; row++)
        for (int col = 0; col < map.Width;  col++)
        {
            if (map.GetTile(col, row) != TileType.Volcano) continue;
            if (col < minCol) minCol = col;
            if (col > maxCol) maxCol = col;
            if (row < minRow) minRow = row;
            if (row > maxRow) maxRow = row;
        }

        if (minCol > maxCol) return null;   // nema Volcano polja

        Vector2 bottomLeft = map.TileToWorld(minCol, minRow);
        Vector2 topRight   = map.TileToWorld(maxCol, maxRow);
        Vector2 center     = (bottomLeft + topRight) * 0.5f;
        float   width      = (maxCol - minCol + 1) * map.TileSize;
        float   height     = (maxRow - minRow + 1) * map.TileSize;

        var go = new GameObject("Volcano");
        go.transform.position = new Vector3(center.x, center.y, 0f);

        var vr = go.AddComponent<VolcanoRenderer>();
        vr._sr = go.AddComponent<SpriteRenderer>();
        vr._sr.sprite       = registry.GetVolcanoFrame(0);
        vr._sr.sortingOrder = 3;            // iznad polja (-5), ispod zgrada (5)
        SpriteFit.Fill(vr._sr, new Vector2(width, height));

        return vr;
    }

    private void Update()
    {
        var registry = SpriteRegistry.Instance;
        if (registry == null || _sr == null) return;
        if (registry.VolcanoFrames == null || registry.VolcanoFrames.Length < 2) return;

        float frameTime = 1f / Mathf.Max(0.1f, registry.VolcanoFps);
        _timer += Time.deltaTime;

        while (_timer >= frameTime)
        {
            _timer -= frameTime;
            _frame++;
        }

        var next = registry.GetVolcanoFrame(_frame);
        if (next != null && next != _sr.sprite)
        {
            var size   = _sr.size;      // Sliced velicina se ne smije izgubiti pri zamjeni framea
            _sr.sprite = next;
            _sr.size   = size;
        }
    }
}
