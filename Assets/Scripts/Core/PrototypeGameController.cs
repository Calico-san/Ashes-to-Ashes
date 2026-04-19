using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central game state. Owns resources, time, population, buildings and ships.
/// All other systems communicate through this controller.
/// </summary>
public class PrototypeGameController : MonoBehaviour
{
    [Header("Population")]
    [SerializeField] private int totalPopulation = 40;
    [SerializeField] private int children        = 8;

    [Header("Starting Resources")]
    [SerializeField] private int food  = 20;
    [SerializeField] private int wood  = 10;
    [SerializeField] private int steel = 5;
    [SerializeField] private int cloth = 5;
    [SerializeField] private int ships = 0;

    [Header("Time")]
    [SerializeField] private float simulationMinutesPerSecond = 12f;

    // ---- References ----
    private PrototypeUIController _ui;

    // ---- Collections ----
    private readonly List<BuildingInstance> _buildings = new();
    private readonly List<ShipInstance>     _ships     = new();

    // ---- Selection ----
    private BuildingInstance _selectedBuilding;
    private ShipInstance     _selectedShip;

    // ---- Time ----
    private float _simulatedMinutes;
    private float _hourAccumulator;
    private int   _day             = 1;
    private int   _speedMultiplier = 1;

    // ---- Workers ----
    private int _freeWorkers;

    // ---- Public properties ----
    public int   TotalPopulation  => totalPopulation;
    public int   Children         => children;
    public int   AdultPopulation  => totalPopulation - children;
    public int   FreeWorkers      => _freeWorkers;
    public int   Food             => food;
    public int   Wood             => wood;
    public int   Steel            => steel;
    public int   Cloth            => cloth;
    public int   Ships            => ships;
    public int   Day              => _day;
    public int   Hour             => Mathf.FloorToInt(_simulatedMinutes / 60f) % 24;
    public int   Minute           => Mathf.FloorToInt(_simulatedMinutes) % 60;
    public int   SpeedMultiplier  => _speedMultiplier;
    public Vector3 WorkerSpawnPoint { get; private set; }
    public Sprite  WorkerSprite     { get; private set; }

    public IReadOnlyList<BuildingInstance> Buildings => _buildings;

    public BuildingInstance GetShipyard()
    {
        foreach (var b in _buildings)
            if (b != null && b.IsShipyard) return b;
        return null;
    }
    public IReadOnlyList<ShipInstance>     GetShips() => _ships;

    // ---- Init ----

    public void Initialize(PrototypeUIController ui, Sprite workerSprite, Vector3 spawnPoint)
    {
        _ui              = ui;
        WorkerSprite     = workerSprite;
        WorkerSpawnPoint = spawnPoint;
        _freeWorkers     = AdultPopulation;
        _simulatedMinutes = 8f * 60f; // start at 08:00
        RefreshUI();
    }

    // ---- Unity ----

    private void Update()
    {
        if (_speedMultiplier == 0) return;

        float delta = Time.deltaTime * simulationMinutesPerSecond * _speedMultiplier;
        _simulatedMinutes += delta;
        _hourAccumulator  += delta;

        while (_hourAccumulator >= 60f)
        {
            _hourAccumulator -= 60f;
            TickHour();
        }

        if (_simulatedMinutes >= 24f * 60f)
        {
            _simulatedMinutes -= 24f * 60f;
            _day++;
        }

        RefreshUI();
    }

    // ---- Registration ----

    public void RegisterBuilding(BuildingInstance b)
        { if (!_buildings.Contains(b)) _buildings.Add(b); }

    public void RegisterShip(ShipInstance s)
        { if (!_ships.Contains(s)) { _ships.Add(s); RefreshUI(); } }

    // ---- Selection ----

    public BuildingInstance GetSelectedBuilding() => _selectedBuilding;
    public ShipInstance     GetSelectedShip()     => _selectedShip;

    public void SelectBuilding(BuildingInstance b)
    {
        if (_selectedBuilding == b) return;
        _selectedBuilding?.SetSelected(false);
        _selectedBuilding = b;
        _selectedBuilding?.SetSelected(true);
        RefreshUI();
    }

    public void SelectShip(ShipInstance s)
    {
        if (_selectedShip == s) return;
        _selectedShip?.SetSelected(false);
        _selectedShip = s;
        _selectedShip?.SetSelected(true);
        RefreshUI();
    }

    // ---- Worker pool ----

    public bool CanCreateWorkerAgent() => _freeWorkers > 0;

    public void OnWorkerAssigned() { _freeWorkers = Mathf.Max(0, _freeWorkers - 1); RefreshUI(); }
    public void OnWorkerRemoved()  { _freeWorkers++;                                  RefreshUI(); }

    // ---- Building actions ----

    public bool AssignWorkerToSelectedBuilding()
    {
        bool ok = _selectedBuilding != null && _selectedBuilding.TryAssignWorker();
        if (ok) RefreshUI();
        return ok;
    }

    public bool RemoveWorkerFromSelectedBuilding()
    {
        bool ok = _selectedBuilding != null && _selectedBuilding.RemoveWorker();
        if (ok) RefreshUI();
        return ok;
    }

    // ---- Ship actions ----

    public bool AssignSailorToSelectedShip()
    {
        bool ok = _selectedShip != null && _selectedShip.TryAssignSailor();
        if (ok) RefreshUI();
        return ok;
    }

    public bool RemoveSailorFromSelectedShip()
    {
        bool ok = _selectedShip != null && _selectedShip.RemoveSailor();
        if (ok) RefreshUI();
        return ok;
    }

    // ---- Resource access ----

    public void AddResource(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Food:  food  += amount; break;
            case ResourceType.Wood:  wood  += amount; break;
            case ResourceType.Steel: steel += amount; break;
            case ResourceType.Cloth: cloth += amount; break;
            case ResourceType.Ships: ships += amount; break;
        }
        RefreshUI();
    }

    public bool HasResources(int wood, int steel, int cloth)
        => this.wood >= wood && this.steel >= steel && this.cloth >= cloth;

    public void ConsumeShipResources(int wood, int steel, int cloth)
    {
        this.wood  -= wood;
        this.steel -= steel;
        this.cloth -= cloth;
        RefreshUI();
    }

    public bool TryConsumeFood(int amount)
    {
        if (food < amount) return false;
        food -= amount;
        RefreshUI();
        return true;
    }

    // ---- Time control ----

    public void SetSpeed(int speed) { _speedMultiplier = Mathf.Clamp(speed, 0, 3); RefreshUI(); }

    // ---- Private ----

    private void TickHour()
    {
        // Population food consumption
        int consumed = Mathf.RoundToInt(EconomyCalculator.FoodConsumptionPerHour(AdultPopulation, children));
        food = Mathf.Max(0, food - consumed);

        // Building production
        foreach (var b in _buildings)
            b?.ProduceHourly();
    }

    private void RefreshUI() => _ui?.Refresh(this, _selectedBuilding, _selectedShip);
}
