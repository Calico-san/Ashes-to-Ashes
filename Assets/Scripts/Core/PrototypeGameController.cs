using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central game state. Owns resources, time, population, buildings, build slots and ships.
/// All other systems communicate through this controller.
/// </summary>
public class PrototypeGameController : MonoBehaviour
{
    [Header("Population")]
    [SerializeField] private int totalPopulation = 40;
    [SerializeField] private int children        = 8;

    [Header("Starting Resources")]
    [SerializeField] private int food  = 20;
    [SerializeField] private int wood  = 30;
    [SerializeField] private int steel = 5;
    [SerializeField] private int cloth = 5;
    [SerializeField] private int rope  = 5;
    [SerializeField] private int ships = 0;

    [Header("Time")]
    [SerializeField] private float simulationMinutesPerSecond = 12f;

    // ---- References ----
    private PrototypeUIController _ui;

    // ---- Collections ----
    private readonly List<BuildingInstance> _buildings  = new();
    private readonly List<BuildSlot>        _buildSlots = new();
    private readonly List<ShipInstance>     _ships      = new();

    // ---- Selection ----
    private BuildingInstance _selectedBuilding;
    private BuildSlot        _selectedSlot;
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
    public int   Rope             => rope;
    public int   Ships            => ships;
    public int   Day              => _day;
    public int   Hour             => Mathf.FloorToInt(_simulatedMinutes / 60f) % 24;
    public int   Minute           => Mathf.FloorToInt(_simulatedMinutes) % 60;
    public int   SpeedMultiplier  => _speedMultiplier;
    public Vector3 WorkerSpawnPoint { get; private set; }
    public Sprite  WorkerSprite     { get; private set; }

    public IReadOnlyList<BuildingInstance> Buildings  => _buildings;
    public IReadOnlyList<BuildSlot>        BuildSlots => _buildSlots;
    public IReadOnlyList<ShipInstance>     GetShips() => _ships;

    public BuildingInstance GetShipyard()
    {
        foreach (var b in _buildings)
            if (b != null && b.IsShipyard) return b;
        return null;
    }

    // ---- Init ----

    public void Initialize(PrototypeUIController ui, Sprite workerSprite, Vector3 spawnPoint)
    {
        _ui               = ui;
        WorkerSprite      = workerSprite;
        WorkerSpawnPoint  = spawnPoint;
        _freeWorkers      = AdultPopulation;
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
    {
        if (_buildings.Contains(b)) return;
        _buildings.Add(b);
        PlacementValidator.Instance?.Register(
            b.transform.position, b.Size);
    }

    public void RegisterBuildSlot(BuildSlot slot)
    {
        if (_buildSlots.Contains(slot)) return;
        _buildSlots.Add(slot);
        PlacementValidator.Instance?.Register(
            slot.Position, slot.Size);
    }

    public void RegisterShip(ShipInstance s)
        { if (!_ships.Contains(s)) { _ships.Add(s); RefreshUI(); } }

    // ---- Selection ----

    public BuildingInstance GetSelectedBuilding() => _selectedBuilding;
    public BuildSlot        GetSelectedSlot()     => _selectedSlot;
    public ShipInstance     GetSelectedShip()     => _selectedShip;

    public void SelectBuilding(BuildingInstance b)
    {
        if (_selectedBuilding == b) return;
        _selectedBuilding?.SetSelected(false);
        _selectedBuilding = b;
        _selectedBuilding?.SetSelected(true);
        if (b != null) { SelectSlot(null); }
        RefreshUI();
    }

    public void SelectSlot(BuildSlot slot)
    {
        if (_selectedSlot == slot) return;
        _selectedSlot?.SetSelected(false);
        _selectedSlot = slot;
        _selectedSlot?.SetSelected(true);
        if (slot != null) { SelectBuilding(null); SelectShip(null); }
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

    // ---- Build slot actions ----

    /// <summary>
    /// Place a new building of given type at world position.
    /// Validates zone, overlap and resources. Returns false if invalid.
    /// </summary>
    public bool TryPlaceBuilding(BuildingType type, Vector3 worldPosition)
    {
        var cost = BuildingCost.For(type);
        if (!cost.CanAfford(wood, steel, cloth)) return false;

        var size      = new Vector2(BalanceConfig.BuildingPlacementSize,
                                    BalanceConfig.BuildingPlacementSize);
        bool shipyard = type == BuildingType.Shipyard;

        var validator = PlacementValidator.Instance;
        if (validator != null && !validator.IsValid(worldPosition, size, shipyard))
            return false;

        wood  -= cost.Wood;
        steel -= cost.Steel;
        cloth -= cost.Cloth;

        // Create BuildSlot at chosen position — construction begins immediately
        var go   = new GameObject("BuildSlot");
        var slot = go.AddComponent<BuildSlot>();
        slot.Initialize(this, worldPosition, new Vector2(size.x, size.y));
        RegisterBuildSlot(slot);
        slot.StartConstruction(type);

        SelectSlot(null);
        RefreshUI();
        return true;
    }

    /// <summary>Legacy — kept for UI back-compat, delegates to TryPlaceBuilding.</summary>
    public bool TryBuildOnSelectedSlot(BuildingType type)
    {
        if (_selectedSlot == null) return false;
        return TryPlaceBuilding(type, _selectedSlot.Position);
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
            case ResourceType.Rope:  rope  += amount; break;
            case ResourceType.Ships: ships += amount; break;
        }
        RefreshUI();
    }

    public bool HasResources(int w, int s, int c, int r = 0)
        => wood >= w && steel >= s && cloth >= c && rope >= r;

    public void ConsumeShipResources(int w, int s, int c, int r = 0)
    {
        wood  -= w;
        steel -= s;
        cloth -= c;
        rope  -= r;
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
        // Food consumption
        int consumed = Mathf.RoundToInt(EconomyCalculator.FoodConsumptionPerHour(AdultPopulation, children));
        food = Mathf.Max(0, food - consumed);

        // Building production
        foreach (var b in _buildings)
            b?.ProduceHourly();

        // Construction progress
        TickConstruction();
    }

    private void TickConstruction()
    {
        for (int i = _buildSlots.Count - 1; i >= 0; i--)
        {
            var slot = _buildSlots[i];
            if (slot == null) continue;
            if (!slot.TickHour()) continue;

            // Construction done — replace slot with real building
            CompleteBuild(slot);
        }
    }

    private void CompleteBuild(BuildSlot slot)
    {
        var type     = slot.QueuedType;
        var position = slot.Position;
        var size     = slot.Size;

        // Deselect if this slot was selected
        if (_selectedSlot == slot)
        {
            _selectedSlot = null;
        }

        PlacementValidator.Instance?.Unregister(position, size);
        _buildSlots.Remove(slot);
        Destroy(slot.gameObject);

        // Create the real building
        var (displayName, outputType, color) = BuildingMeta(type);
        bool isShipyard = type == BuildingType.Shipyard;

        var go       = new GameObject(displayName);
        var building = go.AddComponent<BuildingInstance>();
        building.Initialize(this, displayName, outputType, color, position, size, isShipyard);
        RegisterBuilding(building);

        // Label
        var lbl = new GameObject(displayName + "Label");
        lbl.transform.SetParent(go.transform, false);
        lbl.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        var tmp = lbl.AddComponent<TMPro.TextMeshPro>();
        tmp.text        = displayName;
        tmp.fontSize    = 1.8f;
        tmp.alignment   = TMPro.TextAlignmentOptions.Center;
        tmp.color       = Color.white;
        tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);
        tmp.sortingOrder = 20;

        RefreshUI();
    }

    private static (string name, ResourceType type, Color color) BuildingMeta(BuildingType t)
    {
        switch (t)
        {
            case BuildingType.Sawmill:     return ("Sawmill",     ResourceType.Wood,  new Color(0.22f, 0.65f, 0.25f));
            case BuildingType.Steelworks:  return ("Steelworks",  ResourceType.Steel, new Color(0.55f, 0.55f, 0.58f));
            case BuildingType.Fiberworks:  return ("Fiberworks", ResourceType.Cloth, new Color(0.70f, 0.44f, 0.74f));
            case BuildingType.Cookhouse:   return ("Cookhouse",   ResourceType.Food,  new Color(0.82f, 0.53f, 0.20f));
            case BuildingType.Shipyard:    return ("Shipyard",    ResourceType.Ships, new Color(0.25f, 0.38f, 0.82f));
            default:                       return ("Building",     ResourceType.Wood,  Color.white);
        }
    }

    private void RefreshUI() => _ui?.Refresh(this, _selectedBuilding, _selectedShip, _selectedSlot);
}
