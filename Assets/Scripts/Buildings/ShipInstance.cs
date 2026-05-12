using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A completed ship. Manages its own sailors (workers from the global pool),
/// passenger boarding, and food loading.
/// </summary>
public class ShipInstance : MonoBehaviour
{
    public string DisplayName   { get; private set; }
    public int    ShipNumber    { get; private set; }
    public int    MaxSailors    => BalanceConfig.ShipMaxSailors;
    public int    MaxPassengers => BalanceConfig.ShipMaxPassengers;
    public int    RequiredFood  => BalanceConfig.ShipFoodRequired;

    public int  AssignedSailors => _sailors.Count;
    public int  Passengers      { get; private set; }
    public int  FoodLoaded      { get; private set; }

    public int SailorsInside
    {
        get { int n = 0; foreach (var s in _sailors) if (s != null && s.IsInside) n++; return n; }
    }

    public bool HasVisual { get; private set; }

    public bool IsReadyToSail =>
        AssignedSailors > 0 && Passengers > 0 && FoodLoaded >= RequiredFood;

    // ---- Private ----
    private readonly List<WorkerAgent> _sailors = new();
    private GameController    _game;
    private SpriteRenderer             _hullRenderer;
    private Color                      _normalColor;
    private Color                      _selectedColor;

    private static readonly Vector2 IslandCenter = new Vector2(0f, 0.5f);

    // ---- Init ----

    public void Initialize(GameController game, int shipNumber, Vector3 position, bool hasVisual = true)
    {
        _game       = game;
        ShipNumber  = shipNumber;
        DisplayName = $"Ship {shipNumber}";

        transform.position = position;

        HasVisual = hasVisual;
        if (hasVisual)
        {
            FaceIsland();
            BuildVisual();
            var col  = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.5f, 0.5f);
        }
    }

    /// <summary>Restore passengers and food from save — no worker agents spawned.</summary>
    public void RestoreState(int sailors, int passengers, int foodLoaded)
    {
        // Sailors restored as count only — no walk animation on load
        for (int i = 0; i < sailors; i++) _sailors.Add(null);
        Passengers = passengers;
        FoodLoaded = foodLoaded;
    }

    // ---- Sailor management ----

    public bool TryAssignSailor()
    {
        if (AssignedSailors >= MaxSailors || !_game.CanCreateWorkerAgent()) return false;

        float offsetX = (AssignedSailors - MaxSailors * 0.5f + 0.5f) * 0.18f;
        Vector3 slot  = transform.position + new Vector3(offsetX, -0.05f, 0f);

        var agent = new GameObject($"Sailor_{ShipNumber}_{AssignedSailors + 1}")
            .AddComponent<WorkerAgent>();
        agent.Initialize(_game.WorkerSpawnPoint, slot, _game.WorkerSprite);

        _sailors.Add(agent);
        _game.OnWorkerAssigned();
        return true;
    }

    public bool RemoveSailor()
    {
        if (_sailors.Count == 0) return false;

        var sailor = _sailors[_sailors.Count - 1];
        _sailors.RemoveAt(_sailors.Count - 1);
        sailor?.LeaveBuilding(_game.WorkerSpawnPoint);

        _game.OnWorkerRemoved();
        return true;
    }

    // ---- Cargo ----

    /// <summary>Board up to <paramref name="count"/> passengers. Returns actual boarded.</summary>
    public int BoardPassengers(int count)
    {
        int boarded = Mathf.Min(count, MaxPassengers - Passengers);
        Passengers += boarded;
        return boarded;
    }

    /// <summary>Load food from game pool. Returns amount actually loaded.</summary>
    public int LoadFood(int requested)
    {
        int canLoad = Mathf.Min(requested, RequiredFood - FoodLoaded);
        if (canLoad <= 0 || !_game.TryConsumeFood(canLoad)) return 0;
        FoodLoaded += canLoad;
        return canLoad;
    }

    public bool LoadAllFood() => FoodLoaded >= RequiredFood || LoadFood(RequiredFood - FoodLoaded) > 0;

    public void DisembarkPassengers(int count)
    {
        Passengers = Mathf.Max(0, Passengers - count);
    }

    public void UnloadFood(int amount)
    {
        FoodLoaded = Mathf.Max(0, FoodLoaded - amount);
    }

    // ---- Selection ----

    public void SetSelected(bool selected)
    {
        if (_hullRenderer != null)
            _hullRenderer.color = selected ? _selectedColor : _normalColor;
    }

    // ---- Visual ----

    private void FaceIsland()
    {
        Vector2 dir = IslandCenter - new Vector2(transform.position.x, transform.position.y);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f);
    }

    private void BuildVisual()
    {
        const float s = 0.50f;
        _normalColor   = new Color(0.55f, 0.38f, 0.18f);
        _selectedColor = Color.Lerp(_normalColor, Color.white, 0.4f);

        _hullRenderer = SpawnPart("Hull", Vector3.zero,
            new Vector3(1.8f * s, 0.7f * s, 1f), _normalColor, 8);

        SpawnPart("Mast", new Vector3(0f, 0.3f * s, 0f),
            new Vector3(0.08f * s, 1.0f * s, 1f), new Color(0.25f, 0.18f, 0.10f), 10);

        SpawnPart("Sail", new Vector3(0f, 0.65f * s, 0f),
            new Vector3(0.35f * s, 0.9f * s, 1f), new Color(0.92f, 0.92f, 0.88f), 9);

        var lbl = new GameObject("Label");
        lbl.transform.SetParent(transform);
        lbl.transform.localPosition = new Vector3(0f, -0.28f, 0f);
        lbl.transform.localRotation = Quaternion.identity;
        lbl.transform.localScale    = Vector3.one;
        var tmp = lbl.AddComponent<TMPro.TextMeshPro>();
        tmp.text                    = DisplayName;
        tmp.fontSize                = 1.3f;
        tmp.alignment               = TMPro.TextAlignmentOptions.Center;
        tmp.color                   = Color.white;
        tmp.sortingOrder            = 11;
        tmp.rectTransform.sizeDelta = new Vector2(2f, 0.6f);
    }

    private SpriteRenderer SpawnPart(string partName, Vector3 localPos, Vector3 scale, Color color, int sortOrder)
    {
        var go = new GameObject(partName);
        go.transform.SetParent(transform);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = scale;
        var sr          = go.AddComponent<SpriteRenderer>();
        sr.sprite       = SimpleShapeFactory.CreateFilledSquareSprite(color);
        sr.sortingOrder = sortOrder;
        return sr;
    }
}
