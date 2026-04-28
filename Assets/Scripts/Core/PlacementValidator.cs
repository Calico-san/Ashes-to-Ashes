using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks placed buildings and validates that new placements
/// do not overlap existing ones.
///
/// SRP: only handles overlap detection.
///      Island zone validation is in IslandBounds.
/// </summary>
public class PlacementValidator : MonoBehaviour
{
    public static PlacementValidator Instance { get; private set; }

    private readonly List<Rect> _occupied = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Register a placed building footprint.</summary>
    public void Register(Vector2 position, Vector2 size)
    {
        _occupied.Add(RectFrom(position, size));
    }

    /// <summary>Unregister (e.g. when a BuildSlot is destroyed).</summary>
    public void Unregister(Vector2 position, Vector2 size)
    {
        var r = RectFrom(position, size);
        for (int i = _occupied.Count - 1; i >= 0; i--)
            if (Approximately(_occupied[i], r)) { _occupied.RemoveAt(i); return; }
    }

    /// <summary>True if position+size does not overlap any registered footprint.</summary>
    public bool IsOpen(Vector2 position, Vector2 size)
    {
        var candidate = RectFrom(position, size);
        foreach (var r in _occupied)
            if (r.Overlaps(candidate)) return false;
        return true;
    }

    /// <summary>Full validity check: zone + overlap.</summary>
    public bool IsValid(Vector2 position, Vector2 size, bool isShipyard)
    {
        return IslandBounds.IsValidPlacement(position, size, isShipyard)
            && IsOpen(position, size);
    }

    // ---- Helpers ----

    private static Rect RectFrom(Vector2 center, Vector2 size)
        => new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);

    private static bool Approximately(Rect a, Rect b)
        => Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y)
        && Mathf.Approximately(a.width, b.width) && Mathf.Approximately(a.height, b.height);
}
