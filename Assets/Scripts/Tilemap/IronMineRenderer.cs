using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rudnik nije tile nego objekt u velicini zgrade, isto kao vulkan.
///
/// Tomislav ga i dalje boja po polju u Custom Inspectoru karte — nista se ne
/// mijenja u nacinu rada. Skripta samo pronade skupine susjednih IronMine polja
/// i preko svake postavi jedan sprite umjesto sest malih.
///
/// Zasto objekt, a ne tile: sprite rudnika ima prozirnu pozadinu i crta se manji
/// od polja. Kao tile bi ostao sitan i kroz njega bi se vidjela podloga; kao
/// objekt se razvlaci preko cijele skupine, kao i svaka zgrada.
///
/// Gameplay se ne dira. TilemapData i dalje drzi IronMine po poljima, pa
/// IslandTilemapRenderer.IsNearIronMine i validacija Steelworksa rade isto.
///
/// Poziva se iz PrototypeBootstrapper.SetupTilemap(), odmah uz VolcanoRenderer.
/// </summary>
public static class IronMineRenderer
{
    private const int SortingOrder = 3;   // iznad polja (-5 i -6), ispod zgrada (5)

    /// <summary>
    /// Stvara jedan objekt po skupini susjednih IronMine polja.
    /// Vraca broj stvorenih rudnika; 0 ako karta nema polja ili nema arta.
    /// </summary>
    public static int SpawnAll(TilemapData map)
    {
        if (map == null) return 0;

        var registry = SpriteRegistry.Instance;
        var sprite   = registry != null ? registry.TileIronMine : null;
        if (sprite == null)
        {
            Debug.LogWarning("[IronMineRenderer] TileIronMine nije postavljen u SpriteRegistry.asset — " +
                             "rudnici se nece nacrtati.");
            return 0;
        }

        float scale   = registry.IronMineScale > 0f ? registry.IronMineScale : 1f;
        var   visited = new bool[map.Width * map.Height];
        int   spawned = 0;

        for (int row = 0; row < map.Height; row++)
        for (int col = 0; col < map.Width;  col++)
        {
            if (map.GetTile(col, row) != TileType.IronMine) continue;
            if (visited[row * map.Width + col])            continue;

            SpawnGroup(map, col, row, visited, sprite, scale, ++spawned);
        }

        return spawned;
    }

    /// <summary>
    /// Obilazak u sirinu preko susjednih IronMine polja (4 smjera). Skupina od
    /// jednog polja daje rudnik velicine jedne zgrade; vece nacrtano podrucje
    /// daje veci rudnik — velicinu odreduje karta, ne kod.
    /// </summary>
    private static void SpawnGroup(TilemapData map, int startCol, int startRow,
                                   bool[] visited, Sprite sprite, float scale, int index)
    {
        int minCol = startCol, maxCol = startCol;
        int minRow = startRow, maxRow = startRow;

        var queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startCol, startRow));
        visited[startRow * map.Width + startCol] = true;

        while (queue.Count > 0)
        {
            var c = queue.Dequeue();

            if (c.x < minCol) minCol = c.x;
            if (c.x > maxCol) maxCol = c.x;
            if (c.y < minRow) minRow = c.y;
            if (c.y > maxRow) maxRow = c.y;

            TryEnqueue(map, visited, queue, c.x + 1, c.y);
            TryEnqueue(map, visited, queue, c.x - 1, c.y);
            TryEnqueue(map, visited, queue, c.x, c.y + 1);
            TryEnqueue(map, visited, queue, c.x, c.y - 1);
        }

        Vector2 bottomLeft = map.TileToWorld(minCol, minRow);
        Vector2 topRight   = map.TileToWorld(maxCol, maxRow);
        Vector2 center     = (bottomLeft + topRight) * 0.5f;

        float boxWidth  = (maxCol - minCol + 1) * map.TileSize;
        float boxHeight = (maxRow - minRow + 1) * map.TileSize;

        // Omjer stranica sprajta se cuva. Art rudnika je siri nego visi (2:1), pa
        // bi razvlacenje na kvadrat polja zguzvalo sliku. Umjesto toga se sprajt
        // poveca dok ne prekrije cijelo nacrtano podrucje, a visak prelazi preko
        // ruba — kao object-fit: cover. Jedno polje i sprajt 2:1 daju rudnik
        // sirok dva polja, visok jedno.
        float aspect = sprite.rect.height > 0f ? sprite.rect.width / sprite.rect.height : 1f;
        if (aspect <= 0f) aspect = 1f;

        float height = Mathf.Max(boxHeight, boxWidth / aspect) * scale;
        float width  = height * aspect;

        var go = new GameObject($"IronMine_{index}");
        go.transform.position = new Vector3(center.x, center.y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.sortingOrder = SortingOrder;
        SpriteFit.Fill(sr, new Vector2(width, height));
    }

    private static void TryEnqueue(TilemapData map, bool[] visited,
                                   Queue<Vector2Int> queue, int col, int row)
    {
        if (!map.InBounds(col, row))                   return;
        if (visited[row * map.Width + col])            return;
        if (map.GetTile(col, row) != TileType.IronMine) return;

        visited[row * map.Width + col] = true;
        queue.Enqueue(new Vector2Int(col, row));
    }
}
