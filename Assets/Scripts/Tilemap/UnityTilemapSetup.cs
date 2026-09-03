using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Adds two purely decorative Tilemap layers ("Details", "Objects") on top of the
/// "Ground" layer that IslandTilemapRenderer already builds from TilemapData.
///
/// TOMISLAV — nakon što igra krene (Play), ovi slojevi su vidljivi u Hierarchy pod
/// istim Gridom kao i Ground. Koristis Window → 2D → Tile Palette za crtanje trave,
/// kamenja, cvijeca, stabala itd. preko postojeceg terena. Logika igre ih ne cita —
/// cisto vizualno.
///
/// Sortiranje:
///   Ground  (-5)  — teren, iscrtava IslandTilemapRenderer iz TilemapData
///   Details (0)   — trava, kamenje, cvijece
///   Objects (5)   — stabla, stijene, dekor iznad zgrada
///
/// Namjerno se NE poziva sam preko RuntimeInitializeOnLoadMethod — mora se zvati
/// iz PrototypeBootstrapper.Boot() odmah nakon SetupTilemap(), kad je
/// IslandTilemapRenderer.Instance.WorldGrid vec spreman. Ranija verzija je imala
/// svoj neovisni auto-create u Start() koji je katkad trkao ispred/iza glavnog
/// redoslijeda inicijalizacije — isti razred buga kao i Town Hall/tilemap redoslijed
/// opisan u GDD-u.
/// </summary>
public static class UnityTilemapSetup
{
    /// <summary>Call once, after IslandTilemapRenderer.Initialize() has run.</summary>
    public static void CreateDecorationLayers()
    {
        var groundRenderer = IslandTilemapRenderer.Instance;
        if (groundRenderer == null || groundRenderer.WorldGrid == null)
        {
            Debug.LogWarning("[UnityTilemapSetup] IslandTilemapRenderer/WorldGrid not ready — skipping decoration layers.");
            return;
        }

        var gridTransform = groundRenderer.WorldGrid.transform;
        if (gridTransform.Find("Details") != null) return; // already created (e.g. scene reload)

        CreateLayer(gridTransform, "Details", 0);
        CreateLayer(gridTransform, "Objects", 5);

        Debug.Log("[UnityTilemapSetup] Details/Objects layers created on shared grid. " +
                  "Use Window > 2D > Tile Palette to paint.");
    }

    private static void CreateLayer(Transform parent, string layerName, int sortingOrder)
    {
        var go = new GameObject(layerName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
    }
}
