using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A production building on the island. Handles worker assignment,
/// hourly production, and — if IsShipyard — ship construction.
/// </summary>
public class BuildingInstance : MonoBehaviour
{
    // ---- Public state ----
    public string       DisplayName  { get; private set; }
    public ResourceType OutputType   { get; private set; }
    public bool         IsShipyard   { get; private set; }
    public bool         IsTownHall   { get; private set; }
    public Vector2      Size         { get; private set; }
    public int          MaxWorkers   { get; private set; }
    public int          AssignedWorkers => _workers.Count;
    public int          WorkersInside
    {
        get
        {
            int n = 0;
            foreach (var w in _workers)
                if (w != null && w.IsInside) n++;
            return n;
        }
    }

    // Production info (read by UI)
    public float OutputPerWorkerPerHour => EconomyCalculator.ProductionPerWorkerPerHour(OutputType);
    public float TotalOutputPerHour     => OutputPerWorkerPerHour * WorkersInside;

    // Shipyard info (read by UI)
    public float ShipProgress         => _shipProgress;
    public int   ShipCount            => _shipCount;
    public int   ShipWoodPerCycle     => EconomyCalculator.ShipWoodCost(WorkersInside);
    public int   ShipSteelPerCycle    => EconomyCalculator.ShipSteelCost(WorkersInside);
    public int   ShipClothPerCycle    => EconomyCalculator.ShipClothCost(WorkersInside);

    // ---- Private ----
    private readonly List<WorkerAgent>  _workers     = new();
    private readonly List<GameObject>   _shipObjects = new();
    private SpriteRenderer              _renderer;
    private BuildingAnimator            _animator;
    private Color                       _normalColor;
    private Color                       _selectedColor;
    private PrototypeGameController     _game;
    private float                       _shipProgress;
    private int                         _shipCount;

    // ---- Init ----

    public void Initialize(PrototypeGameController game, string displayName,
        ResourceType outputType, Color color, Vector3 position, Vector2 size, bool isShipyard = false)
    {
        _game       = game;
        DisplayName = displayName;
        OutputType  = outputType;
        IsShipyard  = isShipyard;
        MaxWorkers  = isShipyard ? BalanceConfig.ShipyardMaxWorkers : BalanceConfig.DefaultBuildingMaxWorkers;

        Size                 = size;
        transform.position   = position;
        transform.localScale = new Vector3(size.x, size.y, 1f);

        _normalColor   = color;
        _selectedColor = Color.Lerp(color, Color.white, 0.35f);

        _renderer        = gameObject.AddComponent<SpriteRenderer>();
        _renderer.sprite = SimpleShapeFactory.CreateFilledSquareSprite(color);
        _renderer.sortingOrder = 5;

        var col  = gameObject.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        _animator = gameObject.AddComponent<BuildingAnimator>();
        _animator.Setup(BuildingTypeFromResource(outputType, isShipyard), color);
        _animator.SetBuilt();
    }

    // ---- Worker management ----

    public bool TryAssignWorker()
    {
        if (IsTownHall) return false;
        if (AssignedWorkers >= MaxWorkers || !_game.CanCreateWorkerAgent()) return false;

        var agent = new GameObject($"{DisplayName}_Worker_{AssignedWorkers + 1}")
            .AddComponent<WorkerAgent>();
        agent.Initialize(_game.WorkerSpawnPoint, WorkerSlot(_workers.Count), _game.WorkerSprite);

        _workers.Add(agent);
        _game.OnWorkerAssigned();
        return true;
    }

    public bool RemoveWorker()
    {
        if (_workers.Count == 0) return false;

        var worker = _workers[_workers.Count - 1];
        _workers.RemoveAt(_workers.Count - 1);
        worker?.LeaveBuilding(_game.WorkerSpawnPoint);

        RepositionWorkers();
        _game.OnWorkerRemoved();
        return true;
    }

    private void Update()
    {
        _animator?.SetProducing(WorkersInside > 0 && !IsTownHall);
    }

    // ---- Hourly tick ----

    public void ProduceHourly()
    {
        int active = WorkersInside;
        if (active <= 0) return;

        if (IsShipyard) TickShipyard(active);
        else            _game.AddResource(OutputType, active);
    }

    // ---- Selection ----

    public void SetSelected(bool selected)
        => _renderer.color = selected ? _selectedColor : _normalColor;

    /// <summary>Mark as Town Hall — disables worker assignment and production.</summary>
    public void SetTownHall(bool value) => IsTownHall = value;

    // ---- Private ----

    private void TickShipyard(int activeWorkers)
    {
        int wood  = EconomyCalculator.ShipWoodCost(activeWorkers);
        int steel = EconomyCalculator.ShipSteelCost(activeWorkers);
        int cloth = EconomyCalculator.ShipClothCost(activeWorkers);

        if (!_game.HasResources(wood, steel, cloth)) return;

        _game.ConsumeShipResources(wood, steel, cloth);
        _shipProgress += EconomyCalculator.ShipProgress(activeWorkers);

        while (_shipProgress >= BalanceConfig.ShipProgressRequired)
        {
            _shipProgress -= BalanceConfig.ShipProgressRequired;
            _game.AddResource(ResourceType.Ships, 1);
            SpawnShip();
        }
    }

    private void SpawnShip()
    {
        // First ship gets a visual near the shipyard.
        // All subsequent ships are registered in the UI list only — no extra world objects.
        Vector3 pos = new Vector3(transform.position.x, transform.position.y - 2.2f, 0f);

        var go   = new GameObject($"Ship_{_shipCount + 1}");
        var ship = go.AddComponent<ShipInstance>();
        ship.Initialize(_game, _shipCount + 1, pos, _shipCount == 0);

        _shipObjects.Add(go);
        _game.RegisterShip(ship);
        _shipCount++;
    }

    private void RepositionWorkers()
    {
        for (int i = 0; i < _workers.Count; i++)
            _workers[i]?.SetNewTarget(WorkerSlot(i));
    }

    private static BuildingType BuildingTypeFromResource(ResourceType type, bool isShipyard)
    {
        if (isShipyard) return BuildingType.Shipyard;
        switch (type)
        {
            case ResourceType.Wood:  return BuildingType.Sawmill;
            case ResourceType.Steel: return BuildingType.Steelworks;
            case ResourceType.Cloth: return BuildingType.Fiberworks;
            case ResourceType.Food:  return BuildingType.Cookhouse;
            default:                 return BuildingType.Sawmill;
        }
    }

    private Vector3 WorkerSlot(int index)
    {
        int col = index % 5, row = index / 5;
        return transform.position + new Vector3(-0.45f + col * 0.22f, 0.35f - row * 0.22f, 0f);
    }
}
