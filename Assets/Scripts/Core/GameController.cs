using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central game state. Owns resources, time, population, buildings, build slots and ships.
/// All other systems communicate through this controller.
/// </summary>
public class GameController : MonoBehaviour
{
    [Header("Population")]
    [SerializeField] private int totalPopulation = 1100;
    [SerializeField] private int children        = 440;
    [SerializeField] private int engineers       = 165; // 25% of adults

    [Header("Starting Resources")]
    [SerializeField] private int food  = 500;
    [SerializeField] private int wood  = 80;
    [SerializeField] private int steel = 20;
    [SerializeField] private int cloth = 15;
    [SerializeField] private int rope  = 0;
    [SerializeField] private int ships   = 0;
    [SerializeField] private int rawFood = 0;

    [Header("Hope")]
    [SerializeField, Range(0f, 100f)] private float hope = 100f;

    [Header("Time")]
    [SerializeField] private float simulationMinutesPerSecond = 3.0f; // 8 min/day at 1x

    // ---- References ----
    private UIController _ui;

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
    private const float DAY_START_MINUTES = 6f * 60f; // 06:00
    private const float DAY_END_MINUTES = 20f * 60f; // 20:00

    // ---- Workers ----
    private int _freeWorkers;
    private int _freeEngineers;

    // ---- Public properties ----
    public int   TotalPopulation  => totalPopulation;
    public int   Children         => children;
    public int   AdultPopulation  => totalPopulation - children;
    public int   FreeWorkers      => _freeWorkers;
    public int   FreeEngineers    => _freeEngineers;
    public int   Engineers        => engineers;
    public int   Food             => food;
    public int   Wood             => wood;
    public int   Steel            => steel;
    public int   Cloth            => cloth;
    public int   Rope             => rope;
    public int   Ships            => ships;
    public int   RawFood          => rawFood;
    public float Hope             => hope;
    public int   Day              => _day;
    public int   Hour             => Mathf.FloorToInt(_simulatedMinutes / 60f) % 24;
    public int   Minute           => Mathf.FloorToInt(_simulatedMinutes) % 60;
    public int   SpeedMultiplier  => _speedMultiplier;
    public float SimulatedMinutes => _simulatedMinutes;
    public Vector3 WorkerSpawnPoint { get; private set; }
    public Sprite  WorkerSprite     { get; private set; }
    public Sprite  EngineerSprite   { get; private set; }

    public event Func<int, int, bool> DayEnding;

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

    public void Initialize(UIController ui, Sprite workerSprite, Vector3 spawnPoint)
    {
        _ui               = ui;
        WorkerSprite      = workerSprite;
        WorkerSpawnPoint  = spawnPoint;
        _freeWorkers      = AdultPopulation - engineers;
        _freeEngineers    = engineers;
        EngineerSprite    = SimpleShapeFactory.CreateFilledTriangleSprite(new Color(0.3f, 0.7f, 1f, 1f));
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

        if (_simulatedMinutes >= DAY_END_MINUTES)
        {
            _simulatedMinutes = DAY_END_MINUTES;
            _hourAccumulator = _simulatedMinutes % 60f;

            if (DayEnding?.Invoke(_day, Hour) == true)
            {
                RefreshUI();
                return;
            }

            _day++;
            _simulatedMinutes = DAY_START_MINUTES;
            _hourAccumulator = _simulatedMinutes % 60f;
            _speedMultiplier = 1;
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

    public bool CanCreateWorkerAgent()   => _freeWorkers   > 0;
    public bool CanCreateEngineerAgent() => _freeEngineers > 0;
    public void OnWorkerAssigned()   { _freeWorkers   = Mathf.Max(0, _freeWorkers   - 1); RefreshUI(); }
    public void OnWorkerRemoved()    { _freeWorkers++;                                     RefreshUI(); }
    public void OnEngineerAssigned() { _freeEngineers = Mathf.Max(0, _freeEngineers - 1); RefreshUI(); }
    public void OnEngineerRemoved()  { _freeEngineers++;                                   RefreshUI(); }

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

    /// <summary>
    /// Upgrade selected building to next tier.
    /// MISLAV — implementiraj upgrade logiku ovdje.
    /// Troškovi nadogradnje idu u BalanceConfig.cs (dogovori s Igorom).
    /// </summary>
    public bool TryUpgradeSelectedBuilding()
    {
        if (_selectedBuilding == null) return false;

        // Can't upgrade Shipyard or Town Hall
        if (_selectedBuilding.IsShipyard || _selectedBuilding.IsTownHall) return false;

        // Check resources
        if (!CanAffordUpgrade(_selectedBuilding)) return false;

        // Deduct cost
        wood -= BalanceConfig.UpgradeWoodCost;
        steel -= BalanceConfig.UpgradeSteelCost;
        cloth -= BalanceConfig.UpgradeClothCost;

        // Apply upgrade
        _selectedBuilding.UpgradeBuilding();

        Debug.Log($"[GameController] {_selectedBuilding.DisplayName} upgraded!");
        RefreshUI();
        return true;
    }

    /// <summary>True if player can afford upgrade for this building.</summary>
    public bool CanAffordUpgrade(BuildingInstance building)
    {
        if (building == null) return false;
        if (building.IsShipyard || building.IsTownHall) return false;

        // Check if building is already max upgraded (optional: set a max level)
        if (building.UpgradeLevel >= 3) return false; // Max 3 upgrades

        return HasResources(
            BalanceConfig.UpgradeWoodCost,
            BalanceConfig.UpgradeSteelCost,
            BalanceConfig.UpgradeClothCost
        );
    }

    public bool AssignEngineerToSelectedBuilding()
    {
        bool ok = _selectedBuilding != null && _selectedBuilding.TryAssignEngineer();
        if (ok) RefreshUI();
        return ok;
    }

    public bool RemoveEngineerFromSelectedBuilding()
    {
        bool ok = _selectedBuilding != null && _selectedBuilding.RemoveEngineer();
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
        if (validator != null && !validator.IsValidForType(worldPosition, size, type))
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

    public bool AddPassengerToSelectedShip()
    {
        if (_selectedShip == null || _freeWorkers <= 0) return false;
        if (_selectedShip.Passengers >= _selectedShip.MaxPassengers) return false;
        _selectedShip.BoardPassengers(1);
        _freeWorkers--;
        RefreshUI();
        return true;
    }

    public bool RemovePassengerFromSelectedShip()
    {
        if (_selectedShip == null || _selectedShip.Passengers <= 0) return false;
        _selectedShip.DisembarkPassengers(1);
        _freeWorkers++;
        RefreshUI();
        return true;
    }

    public bool LoadFoodToSelectedShip()
    {
        if (_selectedShip == null) return false;
        int step = BalanceConfig.ShipFoodLoadStep;
        if (food < step || _selectedShip.FoodLoaded >= _selectedShip.RequiredFood) return false;
        int loaded = _selectedShip.LoadFood(step);
        food -= loaded;
        RefreshUI();
        return loaded > 0;
    }

    public bool UnloadFoodFromSelectedShip()
    {
        if (_selectedShip == null || _selectedShip.FoodLoaded <= 0) return false;
        int step = Mathf.Min(BalanceConfig.ShipFoodLoadStep, _selectedShip.FoodLoaded);
        _selectedShip.UnloadFood(step);
        food += step;
        RefreshUI();
        return true;
    }

    // ---- Resource access ----

    public void AddRawFood(int amount) { rawFood = Mathf.Max(0, rawFood + amount); RefreshUI(); }
    public int  ConsumeRawFood(int amount) {
        int consumed = Mathf.RoundToInt(Mathf.Min(rawFood, amount));
        rawFood = Mathf.Max(0, rawFood - consumed);
        return consumed;
    }

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

    // ---- Hope ----

    public void SetHope(float value)
    {
        hope = Mathf.Clamp(value, 0f, 100f);
        RefreshUI();
    }

    public void ChangeHope(float amount) => SetHope(hope + amount);

    // ---- Time control ----

    public void SetSpeed(int speed) { _speedMultiplier = Mathf.Clamp(speed, 0, 5); RefreshUI(); }

    // ---- Save / Load ----

    public GameStateData BuildSaveData()
    {
        var data = new GameStateData
        {
            Day              = _day,
            SimulatedMinutes = _simulatedMinutes,
            SpeedMultiplier  = _speedMultiplier,
            Food             = food,
            Wood             = wood,
            Steel            = steel,
            Cloth            = cloth,
            Rope             = rope,
            Ships            = ships,
            RawFood          = rawFood,
            TotalPopulation  = totalPopulation,
            Children         = children,
            FreeWorkers      = _freeWorkers,
            FreeEngineers    = _freeEngineers,
        };

        foreach (var b in _buildings)
        {
            if (b == null) continue;
            data.Buildings.Add(new BuildingData
            {
                DisplayName              = b.DisplayName,
                BuildingTypeName         = b.BuildingTypeEnum.ToString(),
                ResourceTypeName         = b.OutputType.ToString(),
                PositionX                = b.transform.position.x,
                PositionY                = b.transform.position.y,
                // Zadrzano radi kompatibilnosti sa SaveVersion 1.0; zapisuje se
                // konstanta jer se pri ucitavanju ionako vise ne cita.
                SizeX                    = BalanceConfig.BuildingPlacementSize,
                SizeY                    = BalanceConfig.BuildingPlacementSize,
                AssignedWorkers          = b.AssignedWorkers,
                AssignedEngineers        = b.AssignedEngineers,
                IsShipyard               = b.IsShipyard,
                IsTownHall               = b.IsTownHall,
                ShipProgress             = b.ShipProgress,
                ShipCount                = b.ShipCount,
            });
        }

        foreach (var s in _buildSlots)
        {
            // Only save slots that are actively under construction
            if (s == null) continue;
            if (s.State != BuildSlot.SlotState.UnderConstruction) continue;
            if (s.ConstructionHoursRemaining <= 0f) continue;

            data.BuildSlots.Add(new BuildSlotData
            {
                PositionX                  = s.Position.x,
                PositionY                  = s.Position.y,
                SizeX                      = BalanceConfig.BuildingPlacementSize,
                SizeY                      = BalanceConfig.BuildingPlacementSize,
                QueuedTypeName             = s.QueuedType.ToString(),
                ConstructionHoursRemaining = s.ConstructionHoursRemaining,
                ConstructionHoursTotal     = s.ConstructionHoursTotal,
            });
        }

        foreach (var ship in _ships)
        {
            if (ship == null) continue;
            data.ShipList.Add(new ShipData
            {
                ShipNumber      = ship.ShipNumber,
                PositionX       = ship.transform.position.x,
                PositionY       = ship.transform.position.y,
                AssignedSailors = ship.AssignedSailors,
                Passengers      = ship.Passengers,
                FoodLoaded      = ship.FoodLoaded,
                HasVisual       = ship.HasVisual,
            });
        }

        return data;
    }

    public void ApplyLoadData(GameStateData data)
    {
        // Restore primitives
        _day              = data.Day;
        _simulatedMinutes = data.SimulatedMinutes;
        _speedMultiplier  = data.SpeedMultiplier;
        food              = data.Food;
        wood              = data.Wood;
        steel             = data.Steel;
        cloth             = data.Cloth;
        rope              = data.Rope;
        ships             = data.Ships;
        rawFood           = data.RawFood;
        totalPopulation   = data.TotalPopulation;
        children          = data.Children;
        _freeWorkers      = data.FreeWorkers;
        _freeEngineers    = data.FreeEngineers;

        // Destroy existing dynamic objects
        foreach (var b in _buildings)
            if (b != null && !b.IsTownHall) Destroy(b.gameObject);
        _buildings.Clear();

        foreach (var s in _buildSlots)
            if (s != null) Destroy(s.gameObject);
        _buildSlots.Clear();

        foreach (var ship in _ships)
            if (ship != null) Destroy(ship.gameObject);
        _ships.Clear();

        PlacementValidator.Instance?.ClearAll();

        // Re-register Town Hall footprint after clearing validator
        var townHall = FindTownHall();
        if (townHall != null)
        {
            _buildings.Add(townHall);
            PlacementValidator.Instance?.Register(townHall.transform.position, townHall.Size);
        }

        // Re-create buildings
        foreach (var bd in data.Buildings)
        {
            if (bd.IsTownHall) continue; // Town Hall is static, not re-created

            var type = Enum.TryParse<BuildingType>(bd.BuildingTypeName, out var bt)
                ? bt : BuildingType.Sawmill;

            // SizeX/SizeY iz zapisa se namjerno ignoriraju. Stariji zapisi nose
            // velicine koje su dolazile iz prirodne velicine sprite-a (npr. 3x4 =
            // construction_building 48x64 @ PPU 16); one su zavrsavale kao
            // localScale i mnozile vec ispravno skaliran sprite, pa je zgrada
            // izgledala 3x3 polja. Otisak je uvijek 1x1 — vidi BuildingInstance.
            var building = BuildingFactory.Create(this, type,
                new Vector3(bd.PositionX, bd.PositionY, 0f),
                Vector2.one * BalanceConfig.BuildingPlacementSize);

            // Restore workers (spawn silently — no walk animation on load)
            for (int i = 0; i < bd.AssignedWorkers; i++)
                building.TryAssignWorkerSilent();
            for (int i = 0; i < bd.AssignedEngineers; i++)
                building.TryAssignEngineerSilent();

            RegisterBuilding(building);
        }

        // Re-create build slots — only slots that are UnderConstruction
        foreach (var sd in data.BuildSlots)
        {
            // Skip empty slots (no queued type = was never started)
            if (string.IsNullOrEmpty(sd.QueuedTypeName)) continue;
            if (!Enum.TryParse<BuildingType>(sd.QueuedTypeName, out var qt)) continue;
            if (sd.ConstructionHoursRemaining <= 0f) continue;

            var go   = new GameObject("BuildSlot");
            var slot = go.AddComponent<BuildSlot>();
            slot.Initialize(this,
                new Vector3(sd.PositionX, sd.PositionY, 0f),
                Vector2.one * BalanceConfig.BuildingPlacementSize);   // isti razlog kao gore
            slot.RestoreConstruction(qt, sd.ConstructionHoursRemaining, sd.ConstructionHoursTotal);
            RegisterBuildSlot(slot);
        }

        // Re-create ships
        foreach (var sd in data.ShipList)
        {
            var go   = new GameObject($"Ship_{sd.ShipNumber}");
            var ship = go.AddComponent<ShipInstance>();
            ship.Initialize(this, sd.ShipNumber,
                new Vector3(sd.PositionX, sd.PositionY, 0f), sd.HasVisual);
            ship.RestoreState(sd.AssignedSailors, sd.Passengers, sd.FoodLoaded);
            RegisterShip(ship);
        }

        _selectedBuilding = null;
        _selectedSlot     = null;
        _selectedShip     = null;
        RefreshUI();
    }

    public bool SaveGame(int slot = 0)
    {
        var data = BuildSaveData();
        return SaveSystem.Save(data, slot);
    }

    public bool LoadGame(int slot = 0)
    {
        var data = SaveSystem.Load(slot);
        if (data == null) return false;
        ApplyLoadData(data);
        return true;
    }

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

        // Create the real building (+ label) via the factory
        var building = BuildingFactory.Create(this, type, position, size);
        RegisterBuilding(building);

        RefreshUI();
    }

    private BuildingInstance FindTownHall()
    {
        foreach (var b in FindObjectsByType<BuildingInstance>(FindObjectsSortMode.None))
            if (b != null && b.IsTownHall) return b;
        return null;
    }

    private void RefreshUI() => _ui?.Refresh(this, _selectedBuilding, _selectedShip, _selectedSlot);
}
