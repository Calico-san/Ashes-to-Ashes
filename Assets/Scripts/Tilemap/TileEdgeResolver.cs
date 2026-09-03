using UnityEngine;

/// <summary>
/// Bira rubni sprite za prijelaz izmedu terena.
///
/// Rubni sprite se uvijek crta na Shore polju — obala je vanjski prsten prema
/// oceanu i unutarnji prema kopnu, pa jedno polje pokriva oba prijelaza bez
/// diranja Land i Ocean polja.
///
/// Set je 4-bitni: svaki od cetiri kardinalna susjeda pali jedan bit, pa maska
/// ide 0..15 i pokriva sve kombinacije, ukljucujuci unutarnje kutove i uske
/// prevlake. Prethodna verzija je imala samo 8 komada i vracala goli pijesak cim
/// je ocean bio na dvije strane — na stepenastoj obali to je bila vecina polja.
///
/// SRP: klasa zna samo za susjede i maske. Ne iscrtava nista, ne poznaje
/// renderer, cita samo TilemapData i SpriteRegistry.
/// </summary>
public static class TileEdgeResolver
{
    public const int BitN = 1;
    public const int BitE = 2;
    public const int BitS = 4;
    public const int BitW = 8;

    /// <summary>Rubni sprite za polje (col,row), ili null ako ga nema.</summary>
    public static Sprite Resolve(TilemapData map, int col, int row, TileType type)
    {
        if (map == null || type != TileType.Shore) return null;

        var registry = SpriteRegistry.Instance;
        if (registry == null) return null;

        // Ocean ima prednost: rub prema moru je vizualno vazniji od ruba prema travi
        int oceanMask = NeighbourMask(map, col, row, ocean: true);
        if (oceanMask != 0)
            return registry.GetShoreOceanEdge(oceanMask);

        int landMask = NeighbourMask(map, col, row, ocean: false);
        if (landMask != 0)
            return registry.GetShoreLandEdge(landMask);

        return null;   // obala okruzena obalom — obicno Shore polje
    }

    // ---- Private ----

    /// <summary>row 0 je dno karte, pa je sjever row + 1.</summary>
    private static int NeighbourMask(TilemapData map, int col, int row, bool ocean)
    {
        int mask = 0;
        if (Matches(map, col,     row + 1, ocean)) mask |= BitN;
        if (Matches(map, col + 1, row,     ocean)) mask |= BitE;
        if (Matches(map, col,     row - 1, ocean)) mask |= BitS;
        if (Matches(map, col - 1, row,     ocean)) mask |= BitW;
        return mask;
    }

    private static bool Matches(TilemapData map, int col, int row, bool ocean)
    {
        var t = map.GetTile(col, row);
        return ocean
            ? t == TileType.Ocean
            : t == TileType.Land || t == TileType.Forest;   // Forest je takoder kopno
    }
}
