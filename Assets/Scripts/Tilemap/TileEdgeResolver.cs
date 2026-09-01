using UnityEngine;

/// <summary>Smjer u kojem se nalazi susjedni teren.</summary>
public enum EdgeDir { N, E, S, W, NE, SE, SW, NW }

/// <summary>
/// Bira rubni sprite za prijelaz izmedu terena.
///
/// Rubni sprite se uvijek crta na Shore polju — obala je vanjski prsten prema
/// oceanu i unutarnji prema kopnu, pa jedno polje pokriva oba prijelaza bez
/// diranja Land i Ocean polja.
///
/// SRP: klasa zna samo za susjede i smjerove. Ne iscrtava nista, ne poznaje
/// renderer, cita samo TilemapData i SpriteRegistry.
///
/// OGRANICENJE: tileset ima 8 komada po prijelazu (4 ruba + 4 vanjska kuta).
/// Puni autotile treba 16 komada (4-bitni) ili 47 (blob). Kad Shore polje ima
/// ocean na dvije ili vise strana — unutarnji kut — nema odgovarajuceg komada
/// pa se vraca obicno Shore polje. Vidi se na stepenastim dijelovima obale.
/// </summary>
public static class TileEdgeResolver
{
    /// <summary>Rubni sprite za polje (col,row), ili null ako ga nema.</summary>
    public static Sprite Resolve(TilemapData map, int col, int row, TileType type)
    {
        if (map == null || type != TileType.Shore) return null;

        var registry = SpriteRegistry.Instance;
        if (registry == null) return null;

        // row 0 je dno karte, pa je sjever row + 1
        bool oceanN = IsOcean(map, col,     row + 1);
        bool oceanE = IsOcean(map, col + 1, row);
        bool oceanS = IsOcean(map, col,     row - 1);
        bool oceanW = IsOcean(map, col - 1, row);

        int oceanSides = Count(oceanN, oceanE, oceanS, oceanW);

        // 1. Tocno jedna strana prema oceanu — ravan rub
        if (oceanSides == 1)
            return registry.GetShoreOceanEdge(
                oceanN ? EdgeDir.N : oceanE ? EdgeDir.E : oceanS ? EdgeDir.S : EdgeDir.W);

        if (oceanSides == 0)
        {
            // 2. Nijedna strana, ali tocno jedna dijagonala — vanjski kut
            bool ne = IsOcean(map, col + 1, row + 1);
            bool se = IsOcean(map, col + 1, row - 1);
            bool sw = IsOcean(map, col - 1, row - 1);
            bool nw = IsOcean(map, col - 1, row + 1);

            if (Count(ne, se, sw, nw) == 1)
                return registry.GetShoreOceanEdge(
                    ne ? EdgeDir.NE : se ? EdgeDir.SE : sw ? EdgeDir.SW : EdgeDir.NW);

            // 3. Unutarnja strana prstena — prijelaz prema kopnu
            bool landN = IsMainland(map, col,     row + 1);
            bool landE = IsMainland(map, col + 1, row);
            bool landS = IsMainland(map, col,     row - 1);
            bool landW = IsMainland(map, col - 1, row);

            if (Count(landN, landE, landS, landW) == 1)
                return registry.GetShoreLandEdge(
                    landN ? EdgeDir.N : landE ? EdgeDir.E : landS ? EdgeDir.S : EdgeDir.W);
        }

        return null;   // nema odgovarajuceg komada — obicno Shore polje
    }

    // ---- Private ----

    private static bool IsOcean(TilemapData map, int col, int row)
        => map.GetTile(col, row) == TileType.Ocean;

    /// <summary>Forest se racuna kao kopno — i on granici s obalom.</summary>
    private static bool IsMainland(TilemapData map, int col, int row)
    {
        var t = map.GetTile(col, row);
        return t == TileType.Land || t == TileType.Forest;
    }

    private static int Count(bool a, bool b, bool c, bool d)
        => (a ? 1 : 0) + (b ? 1 : 0) + (c ? 1 : 0) + (d ? 1 : 0);
}
