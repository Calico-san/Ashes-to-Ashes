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
    // Skalirano zajedno s troskovima gradnje (×7): 560 drva i dalje kupuje osam
    // Sawmillova, 140 celika i dalje jedan Shipyard plus jednu manju zgradu.
    // Hrana je nepromijenjena jer potrosnja populacije nije skalirana.
    [SerializeField] private int food  = 500;
    [SerializeField] private int wood  = 560;
    [SerializeField] private int steel = 140;
    [SerializeField] private int cloth = 105;
    [SerializeField] private int rope  = 0;
    [SerializeField] private int ships   = 0;
    [SerializeField] private int rawFood = 0;

    [Header("Hope")]
    [SerializeField, Range(0f, 100f)] private float hope = 100f;

    [Header("Time")]
    [SerializeField] private float simulationMinutesPerSecond = 3.0f;

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
    // Granice dana zive u BalanceConfigu jer o njima ovisi HoursPerDay, kojim
    // EconomyCalculator dijeli sve stope. Dvije kopije bi tiho razisle ekonomiju.
    private const float DAY_START_MINUTES = BalanceConfig.DayStartMinutes; // 06:00
    private const float DAY_END_MINUTES   = BalanceConfig.DayEndMinutes;   // 20:00

    // ---- Workers ----
    private int _freeWorkers;
    private int _freeEngineers;
    private int _evacuatedSouls;

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

    /// <summary>
    /// Djelic tekuceg sata (0..1). Cita ga BuildSlot da traka napretka tece glatko
    /// umjesto da skace na svakom satnom ticku. Jedini izvor vremena u igri.
    /// </summary>
    public float HourFraction => Mathf.Clamp01(_hourAccumulator / 60f);

    /// <summary>Duse koje su vec otplovile s otoka — konacni rezultat igre.</summary>
    public int EvacuatedSouls => _evacuatedSouls;
    public bool EvacuationStarted { get; private set; }

    public Vector3 WorkerSpawnPoint { get; private set; }
    public Sprite  WorkerSprite     { get; private set; }
    public Sprite  EngineerSprite   { get; private set; }

    public event Func<int, int, bool> DayEnding;

    public IReadOnlyList<BuildingInstance> Buildings  => _buildings;
    public IReadOnlyList<BuildSlot>        BuildSlots => _buildSlots;
    public IReadOnlyList<ShipInstance>     GetShips() => _ships;

    public int GetNextShipNumber()
    {
        int highestNumber = 0;
        foreach (var ship in _ships)
            if (ship != null)
                highestNumber = Mathf.Max(highestNumber, ship.ShipNumber);

        return highestNumber + 1;
    }

    public BuildingInstance GetShipyard()
    {
        foreach (var b in _buildings)
            if (b != null && b.IsShipyard) return b;
        return null;
    }

    // -------------------------------------------------------
    // Evidencija populacije — cita ju Town Hall ploca
    // -------------------------------------------------------

    /// <summary>Radnici rasporedeni po zgradama (bez slobodnih).</summary>
    public int EmployedWorkers
    {
        get
        {
            int n = 0;
            foreach (var b in _buildings) if (b != null) n += b.AssignedWorkers;
            return n;
        }
    }

    /// <summary>Inzenjeri rasporedeni po zgradama (bez slobodnih).</summary>
    public int EmployedEngineers
    {
        get
        {
            int n = 0;
            foreach (var b in _buildings) if (b != null) n += b.AssignedEngineers;
            return n;
        }
    }

    /// <summary>Duše ukrcane na brodove koji su još u luci.</summary>
    public int BoardedPassengers
    {
        get
        {
            int n = 0;
            foreach (var s in _ships) if (s != null && !s.IsSailing) n += s.Passengers;
            return n;
        }
    }

    /// <summary>Djeca ukrcana na brodove koji su još u luci.</summary>
    public int BoardedChildren
    {
        get
        {
            int n = 0;
            foreach (var s in _ships) if (s != null && !s.IsSailing) n += s.PassengerChildren;
            return n;
        }
    }

    /// <summary>
    /// Djeca koja se još mogu ukrcati. Ukrcana djeca ostaju u `children` i dalje
    /// jedu dok brod ne isplovi, pa se moraju odbiti ovdje umjesto pri ukrcaju.
    /// </summary>
    public int AvailableChildren => Mathf.Max(0, children - BoardedChildren);

    public float FoodConsumedPerDay
        => EconomyCalculator.FoodConsumptionPerDay(AdultPopulation, children);

    /// <summary>
    /// Dnevna proizvodnja kuhane hrane iz svih zgrada koje ju stvarno isporucuju.
    /// Hunter's Hut je izuzet — njegov OutputType je Food, ali proizvodi RawFood.
    /// </summary>
    public float FoodProducedPerDay
    {
        get
        {
            float perHour = 0f;
            foreach (var b in _buildings)
            {
                if (b == null || b.IsTownHall) continue;
                if (b.BuildingTypeEnum == BuildingType.HuntersHut) continue;
                if (b.OutputType != ResourceType.Food) continue;
                perHour += b.TotalOutputPerHour;
            }
            return perHour * BalanceConfig.HoursPerDay;
        }
    }

    public float NetFoodPerDay => FoodProducedPerDay - FoodConsumedPerDay;

    // ---- Init ----

    public void Initialize(UIController ui, Sprite workerSprite, Vector3 spawnPoint)
    {
        _ui               = ui;
        WorkerSprite      = workerSprite;
        WorkerSpawnPoint  = spawnPoint;
        _freeWorkers      = AdultPopulation - engineers;
        _freeEngineers    = engineers;
        // Isto kao radnici: art iz SpriteRegistryja, trokut samo kao placeholder.
        // GetEngineerSprite vraca art radnika ako EngineerIdle nije postavljen.
        var engineerArt = SpriteRegistry.Instance != null
            ? SpriteRegistry.Instance.GetEngineerSprite()
            : null;
        EngineerSprite    = engineerArt != null
            ? engineerArt
            : SimpleShapeFactory.CreateFilledTriangleSprite(new Color(0.3f, 0.7f, 1f, 1f));
        _simulatedMinutes = 8f * 60f; // start at 08:00
        _hourAccumulator  = 0f;
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

        ProcessShipDepartures();

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

    // ---- Rusenje i otkazivanje ----

    /// <summary>
    /// Otkazuje gradnju na odabranom gradilistu i vraca pola ulozenog materijala.
    /// Gradiliste nema radnike ni proizvodnju, pa je jedino sto treba pocistiti
    /// otisak u validatoru — inace bi polje ostalo trajno zauzeto.
    /// </summary>
    public bool CancelSelectedSlot()
    {
        var slot = _selectedSlot;
        if (slot == null) return false;
        if (slot.State != BuildSlot.SlotState.UnderConstruction) return false;

        var cost = BuildingCost.For(slot.QueuedType);
        wood  += BalanceConfig.Refund(cost.Wood);
        steel += BalanceConfig.Refund(cost.Steel);
        cloth += BalanceConfig.Refund(cost.Cloth);

        PlacementValidator.Instance?.Unregister(slot.Position, slot.Size);
        _buildSlots.Remove(slot);
        _selectedSlot = null;          // prije Destroy: SelectSlot(null) bi zvao
        Destroy(slot.gameObject);      // SetSelected na vec unistenom objektu

        RefreshUI();
        return true;
    }

    /// <summary>
    /// Rusi odabranu gradevinu i vraca pola ulozenog materijala.
    ///
    /// Radnici se NE brisu zajedno sa zgradom — WorkerAgent je zaseban GameObject
    /// koji nije dijete zgrade, pa bi ostao lebdjeti u sceni. RemoveWorker() ga
    /// posalje kuci i usput digne _freeWorkers, zato petlja umjesto Destroy.
    /// </summary>
    public bool DemolishSelectedBuilding()
    {
        var b = _selectedBuilding;
        if (b == null || b.IsTownHall) return false;

        var cost = BuildingCost.For(b.BuildingTypeEnum);
        wood  += BalanceConfig.Refund(cost.Wood);
        steel += BalanceConfig.Refund(cost.Steel);
        cloth += BalanceConfig.Refund(cost.Cloth);

        // Brodogradiliste s polozenom kobilicom: materijal za brod je vec
        // naplacen i nestao bi bez traga. Isto pravilo, pola natrag.
        if (b.IsShipyard && b.KeelLaid)
        {
            wood  += BalanceConfig.Refund(BalanceConfig.ShipWoodCost);
            steel += BalanceConfig.Refund(BalanceConfig.ShipSteelCost);
            cloth += BalanceConfig.Refund(BalanceConfig.ShipClothCost);
            rope  += BalanceConfig.Refund(BalanceConfig.ShipRopeCost);
        }

        while (b.RemoveWorker())   { }
        while (b.RemoveEngineer()) { }

        PlacementValidator.Instance?.Unregister(b.transform.position, b.Size);
        _buildings.Remove(b);
        _selectedBuilding = null;
        Destroy(b.gameObject);

        RefreshUI();
        return true;
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

    /// <summary>
    /// Igrac narucuje brod na odabranom brodogradilistu. Resursi se naplacuju
    /// tek na prvom satnom ticku, kad se polaze kobilica.
    /// </summary>
    public bool OrderShipOnSelectedBuilding()
    {
        if (_selectedBuilding == null || !_selectedBuilding.IsShipyard) return false;
        bool ok = _selectedBuilding.OrderShip();
        if (ok) RefreshUI();
        return ok;
    }

    // -------------------------------------------------------
    // Ship actions
    // Mornari su uklonjeni — brod trazi samo putnike i hranu.
    // -------------------------------------------------------

    /// <summary>
    /// Ukrca do <paramref name="count"/> duša. Prvo idu djeca: ona ne rade, pa ih
    /// evakuacija ne kosta radne snage. Tek kad djece nestane, krecu odrasli iz
    /// bazena slobodnih radnika.
    ///
    /// Ukrcani se oduzimaju iz totalPopulation — inace bi i dalje jeli, sto je
    /// ranije bio slucaj (oduzimao se samo _freeWorkers).
    /// Vraca stvarno ukrcani broj.
    /// </summary>
    public int AddPassengersToSelectedShip(int count)
    {
        if (_selectedShip == null || count <= 0 || _selectedShip.IsSailing) return 0;

        int space = _selectedShip.FreeSpace;
        if (space <= 0) return 0;

        int toBoard          = Mathf.Min(count, space);
        int childrenBoarding = Mathf.Min(toBoard, AvailableChildren);
        int adultsBoarding   = Mathf.Min(toBoard - childrenBoarding, _freeWorkers);
        int total            = childrenBoarding + adultsBoarding;
        if (total <= 0) return 0;

        _selectedShip.BoardPassengers(childrenBoarding, adultsBoarding);

        // Ukrcani odrasli prestaju biti raspoloziva radna snaga, ali populacija se
        // ne mijenja — dok je brod u luci ljudi su i dalje na otoku i jedu.
        // Populacija pada tek pri isplovljavanju (ProcessShipDepartures).
        _freeWorkers -= adultsBoarding;

        RefreshUI();
        return total;
    }

    /// <summary>Iskrca do <paramref name="count"/> putnika natrag u radnu snagu.</summary>
    public int RemovePassengersFromSelectedShip(int count)
    {
        if (_selectedShip == null || count <= 0 || _selectedShip.IsSailing) return 0;

        _selectedShip.DisembarkPassengers(count, out int ch, out int ad);
        int total = ch + ad;
        if (total <= 0) return 0;

        _freeWorkers += ad;   // djeca nisu ni bila u bazenu

        RefreshUI();
        return total;
    }

    /// <summary>Ukrca jedan korak hrane (BalanceConfig.ShipFoodLoadStep).</summary>
    public bool LoadFoodToSelectedShip() => LoadFoodToSelectedShip(BalanceConfig.ShipFoodLoadStep);

    /// <summary>Napuni brod do punog zahtjeva koliko zaliha dopusta.</summary>
    public bool LoadAllFoodToSelectedShip()
        => _selectedShip != null && LoadFoodToSelectedShip(_selectedShip.FoodMissing);

    /// <summary>
    /// Jedino mjesto na kojem se hrana skida sa skladista pri ukrcaju.
    /// ShipInstance namjerno ne dira GameController — ranije su oba oduzimala
    /// istu kolicinu, pa je klik od 10 skidao 20 hrane.
    /// </summary>
    private bool LoadFoodToSelectedShip(int requested)
    {
        if (_selectedShip == null || requested <= 0) return false;

        int amount = Mathf.Min(_selectedShip.LoadableFood(requested), food);
        if (amount <= 0) return false;

        food -= amount;
        _selectedShip.LoadFood(amount);
        RefreshUI();
        return true;
    }

    public bool UnloadFoodFromSelectedShip()
    {
        if (_selectedShip == null || _selectedShip.FoodLoaded <= 0) return false;

        int unloaded = _selectedShip.UnloadFood(BalanceConfig.ShipFoodLoadStep);
        if (unloaded <= 0) return false;

        food += unloaded;
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

    public bool TryConsumeResource(ResourceType type, int amount)
    {
        if (amount <= 0) return true;

        switch (type)
        {
            case ResourceType.Food:
                if (food < amount) return false;
                food -= amount;
                break;
            case ResourceType.Wood:
                if (wood < amount) return false;
                wood -= amount;
                break;
            case ResourceType.Steel:
                if (steel < amount) return false;
                steel -= amount;
                break;
            case ResourceType.Cloth:
                if (cloth < amount) return false;
                cloth -= amount;
                break;
            case ResourceType.Rope:
                if (rope < amount) return false;
                rope -= amount;
                break;
            case ResourceType.Ships:
                if (ships < amount) return false;
                ships -= amount;
                break;
            default:
                return false;
        }

        RefreshUI();
        return true;
    }

    public int GetManpower(ManpowerType type)
        => type == ManpowerType.Workers ? AdultPopulation - engineers : engineers;

    public bool TryConsumeManpower(ManpowerType type, int amount)
    {
        if (amount <= 0) return true;
        if (GetManpower(type) < amount) return false;

        int remaining = amount;
        if (type == ManpowerType.Workers)
        {
            int freeWorkersToConsume = Mathf.Min(_freeWorkers, remaining);
            _freeWorkers -= freeWorkersToConsume;
            remaining -= freeWorkersToConsume;
            foreach (BuildingInstance building in _buildings)
                while (remaining > 0 && building != null && building.RemoveWorker())
                {
                    _freeWorkers--;
                    remaining--;
                }
        }
        else
        {
            int freeEngineersToConsume = Mathf.Min(_freeEngineers, remaining);
            _freeEngineers -= freeEngineersToConsume;
            remaining -= freeEngineersToConsume;
            foreach (BuildingInstance building in _buildings)
                while (remaining > 0 && building != null && building.RemoveEngineer())
                {
                    _freeEngineers--;
                    remaining--;
                }
        }

        totalPopulation -= amount;
        if (type == ManpowerType.Engineers)
            engineers -= amount;

        RefreshUI();
        return true;
    }

    public void AddManpower(ManpowerType type, int amount)
    {
        if (amount <= 0) return;

        totalPopulation += amount;
        if (type == ManpowerType.Workers)
            _freeWorkers += amount;
        else
        {
            engineers += amount;
            _freeEngineers += amount;
        }

        RefreshUI();
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
            Engineers        = engineers,
            FreeWorkers      = _freeWorkers,
            FreeEngineers    = _freeEngineers,
            EvacuatedSouls   = _evacuatedSouls,
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
                // Zadrzano radi kompatibilnosti sa starim zapisima; pri ucitavanju
                // se ionako vise ne cita.
                SizeX                    = BalanceConfig.BuildingPlacementSize,
                SizeY                    = BalanceConfig.BuildingPlacementSize,
                AssignedWorkers          = b.AssignedWorkers,
                AssignedEngineers        = b.AssignedEngineers,
                IsShipyard               = b.IsShipyard,
                IsTownHall               = b.IsTownHall,
                ShipProgress             = b.ShipProgress,
                ShipCount                = b.ShipCount,
                KeelLaid                 = b.KeelLaid,
                ShipOrdered              = b.ShipOrdered,
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

        data.Events = EventManager.Instance?.BuildSaveData() ?? data.Events;

        foreach (var ship in _ships)
        {
            if (ship == null || ship.IsSailing) continue;   // brod u odlasku se ne sprema
            data.ShipList.Add(new ShipData
            {
                ShipNumber        = ship.ShipNumber,
                PositionX         = ship.transform.position.x,
                PositionY         = ship.transform.position.y,
                AssignedSailors   = 0,                       // mornari uklonjeni
                Passengers        = ship.Passengers,
                PassengerChildren = ship.PassengerChildren,
                PassengerAdults   = ship.PassengerAdults,
                FoodLoaded        = ship.FoodLoaded,
                HasVisual         = ship.HasVisual,
            });
        }

        return data;
    }

    public void ApplyLoadData(GameStateData data)
    {
        // Restore primitives
        _day              = data.Day;
        _simulatedMinutes = data.SimulatedMinutes;
        _hourAccumulator  = _simulatedMinutes % 60f;
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
        if (data.Engineers >= 0) engineers = data.Engineers;
        _freeWorkers      = data.FreeWorkers;
        _freeEngineers    = data.FreeEngineers;
        _evacuatedSouls   = data.EvacuatedSouls;

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

            // SizeX/SizeY iz zapisa se namjerno ignoriraju — otisak je uvijek 1x1.
            var building = BuildingFactory.Create(this, type,
                new Vector3(bd.PositionX, bd.PositionY, 0f),
                Vector2.one * BalanceConfig.BuildingPlacementSize);

            // Brodogradiliste: vrati napredak i stanje kobilice (O5 — ranije se gubilo,
            // a s fiksnom naplatom to bi bio gubitak cijelog troska broda).
            if (building.IsShipyard)
                building.RestoreShipyardState(bd.ShipProgress, bd.ShipCount, bd.KeelLaid, bd.ShipOrdered);

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
                new Vector3(sd.PositionX, sd.PositionY, 0f), true,
                Mathf.Max(8, 60 - (sd.ShipNumber - 1) * 4));

            // Zapisi verzije 1.0 nemaju podjelu po dobi — sve se tada vraca kao odrasli.
            int ch = sd.PassengerChildren;
            int ad = sd.PassengerAdults;
            if (ch + ad == 0 && sd.Passengers > 0) ad = sd.Passengers;

            ship.RestoreState(ch, ad, sd.FoodLoaded);
            RegisterShip(ship);
        }

        // Bez ovoga PrepareTrigger() ostaje na svjezem stanju iz pokretanja scene,
        // pa se vec odigrani dogadaji otvaraju ponovno.
        EventManager.Instance?.ApplyLoadData(data.Events);

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

    /// <summary>Koliko brodova trenutno ceka na polazak (spremni, jos u luci).</summary>
    public int ReadyShipCount
    {
        get
        {
            int n = 0;
            foreach (var s in _ships)
                if (s != null && !s.IsSailing && s.IsReadyToSail) n++;
            return n;
        }
    }

    /// <summary>
    /// Salje SVE spremne brodove jednim potezom. Tek sada duse napustaju otok:
    /// populacija pada, djeca izlaze iz brojaca i potrosnja hrane se smanjuje.
    /// Do ovog trenutka su ukrcani i dalje jeli.
    ///
    /// Svaki brod dobiva svoje mjesto u redu, pa krecu jedan za drugim i
    /// razilaze se u lepezu umjesto da putuju kao jedna mrlja.
    /// Vraca broj poslanih brodova.
    /// </summary>
    public int SailAllReadyShips()
    {
        // Brodovi stoje poredani kao karte, sve dalje od obale. Krecu OBRNUTIM
        // redom — onaj najdalji prvi — jer bi inace brod uz obalu isplovio
        // ravno kroz one koji jos cekaju iza njega.
        var ready = new List<ShipInstance>();
        for (int i = _ships.Count - 1; i >= 0; i--)
        {
            var ship = _ships[i];
            if (ship == null || ship.IsSailing || !ship.IsReadyToSail) continue;
            ready.Add(ship);
        }

        for (int q = 0; q < ready.Count; q++)
        {
            var ship = ready[q];

            totalPopulation -= ship.Passengers;
            children        -= ship.PassengerChildren;
            _evacuatedSouls += ship.Passengers;

            ship.BeginSail(q, ready.Count);
        }

        if (ready.Count > 0)
        {
            EvacuationStarted = true;
            _selectedShip = null;
            RefreshUI();
        }
        return ready.Count;
    }

    /// <summary>Brise brodove koji su otplovili dovoljno daleko.</summary>
    private void ProcessShipDepartures()
    {
        for (int i = _ships.Count - 1; i >= 0; i--)
        {
            var ship = _ships[i];
            if (ship == null) { _ships.RemoveAt(i); continue; }
            if (!ship.HasLeft) continue;

            if (_selectedShip == ship) _selectedShip = null;
            _ships.RemoveAt(i);
            Destroy(ship.gameObject);
        }
    }

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
