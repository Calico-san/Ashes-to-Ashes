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
    public bool         IsTownHall      { get; private set; }
    public Vector2      Size            { get; private set; }
    public BuildingType BuildingTypeEnum { get; private set; }
    public int          MaxWorkers   { get; private set; }
    public int          AssignedWorkers   => _workers.Count;
    public int          AssignedEngineers => _engineers.Count;
    public int          MaxEngineers      => Mathf.Max(1, MaxWorkers / 5); // max 20% engineers
    public bool         HasEngineer       => EngineersInside > 0;
    public int          WorkersInside
    {
        get
        {
            int n = 0;
            foreach (var w in _workers)
                if (w == null || w.IsInside) n++;
            return n;
        }
    }
    public int          EngineersInside
    {
        get
        {
            int n = 0;
            foreach (var e in _engineers)
                if (e == null || e.IsInside) n++;
            return n;
        }
    }

    // Production info (read by UI)
    public float OutputPerWorkerPerHour =>
        BuildingTypeEnum == BuildingType.HuntersHut
            ? BalanceConfig.RawFoodPerWorkerPerDay / 24f   // Hunter's Hut outputs Raw Food, not cooked Food
            : EconomyCalculator.ProductionPerWorkerPerHour(OutputType);
    public float EngineerBonus => HasEngineer
        ? (IsShipyard ? BalanceConfig.EngineerShipBonus : BalanceConfig.EngineerProductionBonus)
        : 1.0f;
    public float TotalOutputPerHour     => OutputPerWorkerPerHour * WorkersInside * EngineerBonus;

    // Shipyard info (read by UI)
    public float ShipProgress         => _shipProgress;
    public int   ShipCount            => _shipCount;
    public int   ShipWoodPerCycle     => EconomyCalculator.ShipWoodCost(WorkersInside);
    public int   ShipSteelPerCycle    => EconomyCalculator.ShipSteelCost(WorkersInside);
    public int   ShipClothPerCycle    => EconomyCalculator.ShipClothCost(WorkersInside);
    public int   ShipRopePerCycle     => EconomyCalculator.ShipRopeCost(WorkersInside);

    // ---- Private ----
    private readonly List<WorkerAgent>  _workers     = new();
    private readonly List<WorkerAgent>  _engineers   = new();
    private readonly List<GameObject>   _shipObjects = new();
    private List<Vector3>               _pathToTownHall;

    public bool HasPath => _pathToTownHall != null && _pathToTownHall.Count > 0;
    private SpriteRenderer              _renderer;
    private BuildingAnimator            _animator;
    private Color                       _normalColor;
    private Color                       _selectedColor;
    private GameController     _game;
    private float                       _shipProgress;
    private int                         _shipCount;

    // Fractional production carry-over: banks whole units, keeps the remainder
    // so sub-1.0/hour rates (e.g. Steelworks 0.083/worker/h) survive rounding.
    private float                       _outputAccumulator;   // primary output
    private float                       _ropeAccumulator;     // Fiberworks secondary output (Rope)

    // ---- Init ----

    public void Initialize(GameController game, string displayName,
        ResourceType outputType, Color color, Vector3 position, Vector2 size,
        bool isShipyard = false, BuildingType buildingType = BuildingType.Cookhouse)
    {
        _game       = game;
        DisplayName = displayName;
        OutputType  = outputType;
        IsShipyard       = isShipyard;
        BuildingTypeEnum = buildingType != BuildingType.Cookhouse
            ? buildingType
            : BuildingTypeFromResource(outputType, isShipyard);

        if (BuildingTypeEnum == BuildingType.HuntersHut || BuildingTypeEnum == BuildingType.ScoutStation)
            MaxWorkers = BalanceConfig.HuntersHutMaxWorkers;
        else
            MaxWorkers = isShipyard ? BalanceConfig.ShipyardMaxWorkers : BalanceConfig.DefaultBuildingMaxWorkers;
        // Otisak zgrade je po dizajnu UVIJEK 1x1 polje (BalanceConfig.BuildingPlacementSize).
        // Proslijedeni `size` se namjerno ignorira: dolazio je iz tri razlicita izvora
        // (TryPlaceBuilding, CompleteBuild, ApplyLoadData), a onaj iz spremljene igre je
        // mogao nositi zastarjele vrijednosti iz starijih verzija (npr. 3x4), koje su
        // zavrsavale kao localScale i mnozile velicinu sprite-a — zgrada bi izgledala
        // 3x3 polja iako je sr.size bio ispravnih 1x0.8. Jedan izvor istine to sprjecava.
        float footprint = BalanceConfig.BuildingPlacementSize;
        Size = new Vector2(footprint, footprint);

        transform.position   = position;
        transform.localScale = new Vector3(footprint, footprint, 1f);

        // Kad postoji pravi sprite, tinta mora biti bijela — inace placeholder
        // boja tipa zgrade zaprlja pixel art.
        bool hasArt    = BuildingAnimator.HasArtFor(BuildingTypeEnum);
        _normalColor   = hasArt ? Color.white : color;
        _selectedColor = hasArt
            ? new Color(1f, 0.92f, 0.55f)
            : Color.Lerp(color, Color.white, 0.35f);

        _renderer        = gameObject.AddComponent<SpriteRenderer>();
        _renderer.sprite = SimpleShapeFactory.CreateFilledSquareSprite(_normalColor);
        _renderer.sortingOrder = 5;

        var col  = gameObject.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;

        _animator = gameObject.AddComponent<BuildingAnimator>();
        _animator.Setup(BuildingTypeEnum, color);   // BuildingTypeEnum — HuntersHut i ScoutStation
                                                    // oboje daju Food pa bi ih BuildingTypeFromResource
                                                    // pogresno mapirao u Cookhouse
        _animator.SetBuilt();
    }

    // ---- Worker management ----

    /// <summary>Called by GameController after placement — stores pre-computed path.</summary>
    public void SetPath(List<Vector3> path)
    {
        _pathToTownHall = path;
    }

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

    public bool TryAssignEngineer()
    {
        if (IsTownHall) return false;
        if (AssignedEngineers >= MaxEngineers || !_game.CanCreateEngineerAgent()) return false;
        var agent = new GameObject($"{DisplayName}_Engineer_{AssignedEngineers + 1}")
            .AddComponent<WorkerAgent>();
        agent.Initialize(_game.WorkerSpawnPoint, EngineerSlot(_engineers.Count), _game.EngineerSprite, _pathToTownHall);
        _engineers.Add(agent);
        _game.OnEngineerAssigned();
        return true;
    }

    public bool RemoveEngineer()
    {
        if (_engineers.Count == 0) return false;
        var eng = _engineers[_engineers.Count - 1];
        _engineers.RemoveAt(_engineers.Count - 1);
        eng?.LeaveBuilding(_game.WorkerSpawnPoint);
        _game.OnEngineerRemoved();
        return true;
    }

    public bool TryAssignEngineerSilent()
    {
        if (AssignedEngineers >= MaxEngineers || !_game.CanCreateEngineerAgent()) return false;
        _engineers.Add(null);
        _game.OnEngineerAssigned();
        return true;
    }

    private void Update()
    {
        _animator?.SetProducing(WorkersInside > 0 && !IsTownHall);
    }

    /// <summary>
    /// Osigurac nad velicinom. Glavni popravak je u BuildingAnimator.SetState, koje
    /// vraca scale odmah nakon sto ga Unity prepise prirodnom velicinom sprite-a.
    /// Ovo hvata eventualni preostali slucaj (npr. buduce stanje koje ne prolazi
    /// kroz SetState) i ne ispisuje nista da ne zatrpava Console.
    /// </summary>
    private void LateUpdate()
    {
        float   f        = BalanceConfig.BuildingPlacementSize;
        Vector3 expected = new Vector3(f, f, 1f);

        if ((transform.localScale - expected).sqrMagnitude > 0.000001f)
            transform.localScale = expected;
    }

    /// <summary>Assign worker instantly without walk animation. Used on game load.</summary>
    public bool TryAssignWorkerSilent()
    {
        if (AssignedWorkers >= MaxWorkers || !_game.CanCreateWorkerAgent()) return false;
        _workers.Add(null); // null = worker is "inside" but no visual agent
        _game.OnWorkerAssigned();
        return true;
    }

    // ---- Hourly tick ----

    public void ProduceHourly()
    {
        int active = WorkersInside;
        if (active <= 0) return;

        float bonus = EngineerBonus;

        if (IsShipyard) { TickShipyard(active, bonus); return; }

        // Hunter's Hut — produces Raw Food at its own per-worker rate
        if (BuildingTypeEnum == BuildingType.HuntersHut)
        {
            int raw = Bank(ref _outputAccumulator, OutputPerWorkerPerHour * active * bonus);
            if (raw > 0) _game.AddRawFood(raw);
            return;
        }

        // Fiberworks — produces both Cloth and Rope from their BalanceConfig rates
        if (OutputType == ResourceType.Cloth)
        {
            float clothRate = EconomyCalculator.ProductionPerWorkerPerHour(ResourceType.Cloth);
            float ropeRate  = EconomyCalculator.ProductionPerWorkerPerHour(ResourceType.Rope);
            int cloth = Bank(ref _outputAccumulator, active * clothRate * bonus);
            int rope  = Bank(ref _ropeAccumulator,  active * ropeRate  * bonus);
            if (cloth > 0) _game.AddResource(ResourceType.Cloth, cloth);
            if (rope  > 0) _game.AddResource(ResourceType.Rope,  rope);
            return;
        }

        // Standard single-output buildings (Sawmill, Steelworks, Cookhouse, ...)
        float perHour = OutputPerWorkerPerHour * active * bonus;

        if (BuildingTypeEnum == BuildingType.Cookhouse)
        {
            // Consume raw food — CookhouseRawFoodPerWorker per worker per hour
            int rawNeeded   = Mathf.RoundToInt(active * BalanceConfig.CookhouseRawFoodPerWorker);
            int rawConsumed = _game.ConsumeRawFood(rawNeeded);
            float multiplier = rawConsumed >= rawNeeded
                ? BalanceConfig.CookhouseNormalMultiplier
                : BalanceConfig.CookhouseLowMultiplier;
            perHour *= multiplier;
        }

        int output = Bank(ref _outputAccumulator, perHour);
        if (output > 0) _game.AddResource(OutputType, output);
    }

    /// <summary>
    /// Adds fractional per-hour production to the accumulator and returns only the
    /// whole units ready to bank; the remainder carries over to the next hour.
    /// </summary>
    private static int Bank(ref float accumulator, float amount)
    {
        accumulator += amount;
        int whole = Mathf.FloorToInt(accumulator);
        accumulator -= whole;
        return whole;
    }

    // ---- Selection ----

    public void SetSelected(bool selected)
        => _renderer.color = selected ? _selectedColor : _normalColor;

    /// <summary>Mark as Town Hall — disables worker assignment and production.</summary>
    public void SetTownHall(bool value) => IsTownHall = value;

    // ---- Private ----

    private void TickShipyard(int activeWorkers, float bonus = 1f)
    {
        int wood  = EconomyCalculator.ShipWoodCost(activeWorkers);
        int steel = EconomyCalculator.ShipSteelCost(activeWorkers);
        int cloth = EconomyCalculator.ShipClothCost(activeWorkers);
        int rope  = EconomyCalculator.ShipRopeCost(activeWorkers);

        if (!_game.HasResources(wood, steel, cloth, rope)) return;

        _game.ConsumeShipResources(wood, steel, cloth, rope);
        _shipProgress += EconomyCalculator.ShipProgress(activeWorkers) * bonus;

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

    private Vector3 EngineerSlot(int index)
    {
        // Engineers stand slightly above workers, tinted differently
        return transform.position + new Vector3(-0.45f + index * 0.22f, 0.58f, 0f);
    }
}
