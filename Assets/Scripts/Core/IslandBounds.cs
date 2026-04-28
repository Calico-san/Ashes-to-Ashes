using UnityEngine;

/// <summary>
/// Defines the island geometry used for building placement validation.
/// Mirrors the visual island created in PrototypeBootstrapper.CreateIsland().
///
/// Three zones:
///   Land      — inside inner ellipse (kopno)
///   Shore     — between inner and outer ellipse (obala / ocean ring)
///   Ocean     — outside outer ellipse (more)
///
/// Regular buildings must be placed on Land.
/// Shipyard must be placed on Shore (touching the sea).
/// </summary>
public static class IslandBounds
{
    // Must match PrototypeBootstrapper.CreateIsland() visual scale
    private static readonly Vector2 Center      = new Vector2(0f, 0.5f);
    private static readonly Vector2 LandRadius  = new Vector2(5.2f, 4.0f);   // inner ellipse — kopno
    private static readonly Vector2 ShoreRadius = new Vector2(6.5f, 5.0f);   // outer ellipse — obala

    public enum Zone { Land, Shore, Ocean }

    /// <summary>Returns which zone a world-space point falls in.</summary>
    public static Zone GetZone(Vector2 point)
    {
        if (IsInsideEllipse(point, LandRadius))  return Zone.Land;
        if (IsInsideEllipse(point, ShoreRadius)) return Zone.Shore;
        return Zone.Ocean;
    }

    /// <summary>True if a building of given size can be placed at position.</summary>
    public static bool IsValidPlacement(Vector2 position, Vector2 size, bool isShipyard)
    {
        // Check all four corners + center of the building footprint
        var corners = GetCorners(position, size);
        foreach (var corner in corners)
        {
            var zone = GetZone(corner);
            if (isShipyard)
            {
                // Shipyard: must be on Shore, not fully in ocean or fully on land
                if (zone == Zone.Ocean) return false;
            }
            else
            {
                // Regular buildings: must be fully on land
                if (zone != Zone.Land) return false;
            }
        }

        // Shipyard: at least one corner must touch Shore
        if (isShipyard)
        {
            bool hasShore = false;
            foreach (var corner in corners)
                if (GetZone(corner) == Zone.Shore) { hasShore = true; break; }
            if (!hasShore) return false;
        }

        return true;
    }

    // ---- Helpers ----

    private static bool IsInsideEllipse(Vector2 point, Vector2 radius)
    {
        float dx = (point.x - Center.x) / radius.x;
        float dy = (point.y - Center.y) / radius.y;
        return dx * dx + dy * dy <= 1f;
    }

    private static Vector2[] GetCorners(Vector2 center, Vector2 size)
    {
        float hx = size.x * 0.5f;
        float hy = size.y * 0.5f;
        return new Vector2[]
        {
            center,
            new Vector2(center.x - hx, center.y - hy),
            new Vector2(center.x + hx, center.y - hy),
            new Vector2(center.x - hx, center.y + hy),
            new Vector2(center.x + hx, center.y + hy),
        };
    }
}
